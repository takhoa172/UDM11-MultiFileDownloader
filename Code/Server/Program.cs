using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using Shared;
using Server;

TcpListener listener = new(IPAddress.Any, ServerConfig.Port);
listener.Start();

ServerLogger.LogServerStart(ServerConfig.Port);

// ─────────────────────────────────────────────────────────────
//  SPEED TEST
// ─────────────────────────────────────────────────────────────

if (args.Length > 0 && args[0] == "--speedtest")
{
    RateLimiter rl = new RateLimiter(5 * 1024 * 1024);

    long totalBytes = 30L * 1024 * 1024;

    Stopwatch sw = Stopwatch.StartNew();

    const int chunk = 64 * 1024;

    for (long sent = 0; sent < totalBytes; sent += chunk)
    {
        await rl.ThrottleAsync(
            (int)Math.Min(chunk, totalBytes - sent));
    }

    sw.Stop();

    double mbps =
        totalBytes /
        1024.0 /
        1024.0 /
        sw.Elapsed.TotalSeconds;

    Console.WriteLine(
        $"--- Speed test: {mbps:F2} MB/s (gioi han 5 MB/s) ---");

    return;
}

// ─────────────────────────────────────────────────────────────
//  ACCEPT LOOP
// ─────────────────────────────────────────────────────────────

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();

    _ = Task.Run(() => HandleClientAsync(client));
}

// ─────────────────────────────────────────────────────────────
//  HANDLE CLIENT
// ─────────────────────────────────────────────────────────────

static async Task HandleClientAsync(TcpClient client)
{
    string clientIp =
        client.Client.RemoteEndPoint?.ToString() ?? "unknown";

    bool connectionCounted = false;

    bool isMainConnection = false;
    bool isDownloadConnection = false;

    string connectionId = Guid.NewGuid().ToString("N");
    ClientSessionContext session = new(connectionId);

    UploadHandler? uploadHandler = null;

    try
    {
        using (client)
        await using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new(stream))
        {
            SocketTimeoutManager.Apply(
                client,
                ServerConfig.ReadTimeoutMs,
                ServerConfig.WriteTimeoutMs);

            while (true)
            {
                string? line;

                try
                {
                    using CancellationTokenSource readCts =
                        SocketTimeoutManager.CreateLinkedCts(
                            ServerConfig.ReadTimeoutMs);

                    line = await reader.ReadLineAsync(readCts.Token);
                }
                catch (OperationCanceledException)
                {
                    ServerLogger.LogWarning(
                        $"[{clientIp}] Timeout khi cho Server nhan request.");

                    break;
                }

                if (line is null)
                {
                    ServerLogger.LogInfo(isDownloadConnection
                        ? $"[{clientIp}] Download connection dong sau khi xu ly request."
                        : $"[{clientIp}] Client chu dong dong ket noi.");
                    break;
                }

                string? rawError = PacketValidator.ValidateRaw(line);

                if (rawError != null)
                {
                    ServerLogger.LogWarning(
                        $"[{clientIp}] Goi tin bi tu choi ({rawError})");

                    await SendPacketAsync(
                        stream,
                        new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = rawError,
                            Message = "Gói tin không hợp lệ"
                        });

                    continue;
                }

                ProtocolPacket request;

                try
                {
                    request = PacketHelper.Decode(line);
                }
                catch (Exception ex)
                {
                    ServerLogger.LogWarning(
                        $"[{clientIp}] Goi tin sai dinh dang: {ex.Message}");

                    await SendPacketAsync(
                        stream,
                        new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "400_BAD_REQUEST",
                            Message = "Gói tin không hợp lệ."
                        });

                    continue;
                }

                string? reqError = PacketValidator.ValidateRequest(request);

                if (reqError != null)
                {
                    ServerLogger.LogWarning(
                        $"[{clientIp}] Lenh khong hop le ({reqError})");

                    await SendPacketAsync(
                        stream,
                        new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = reqError,
                            Message = "Lệnh không hợp lệ"
                        });

                    continue;
                }

                if (!connectionCounted)
                {
                    connectionCounted = true;

                    // Log moi ket noi "chinh" (khong phai ket noi tai file
                    // ngan han). Lenh dau tien co the la LOGIN/REGISTER/GET_LIST...
                    if (request.Command != PacketCommand.DOWNLOAD_REQ)
                    {
                        isMainConnection = true;
                        ServerLogger.ClientConnected(clientIp, silent: false);
                    }
                }

                if (request.Command != PacketCommand.PING)
                {
                    ServerLogger.LogInfo(
                        $"[{clientIp}] Nhan lenh {request.Command}");
                }

                if (request.Command == PacketCommand.DOWNLOAD_REQ)
                    isDownloadConnection = true;

                uploadHandler = await ProcessRequestAsync(
                    stream,
                    request,
                    clientIp,
                    uploadHandler,
                    session);
            }
        }
    }
    catch (IOException ex)
    {
        ServerLogger.LogError(
            $"[{clientIp}] Mat ket noi: {ex.Message}");
    }
    catch (SocketException ex)
    {
        ServerLogger.LogError(
            $"[{clientIp}] Loi socket: {ex.Message}");
    }
    catch (OperationCanceledException)
    {
        ServerLogger.LogWarning(
            $"[{clientIp}] Tac vu bi timeout.");
    }
    catch (Exception ex)
    {
        ServerLogger.LogError(
            $"[{clientIp}] Loi xu ly client: {ex.Message}");
    }
    finally
    {
        uploadHandler?.Dispose();
        SessionManager.End(connectionId);

        if (connectionCounted && isMainConnection)
        {
            ServerLogger.ClientDisconnected(clientIp, silent: false);
        }
    }
}
// ─────────────────────────────────────────────────────────────
//  PROCESS REQUEST
// ─────────────────────────────────────────────────────────────

static async Task<UploadHandler?> ProcessRequestAsync(
    NetworkStream stream,
    ProtocolPacket request,
    string clientIp,
    UploadHandler? uploadHandler,
    ClientSessionContext session)
{
    try
    {
        if (request.Command == PacketCommand.DOWNLOAD_REQ &&
            string.IsNullOrWhiteSpace(session.Username) &&
            SessionManager.TryValidateToken(
                request.Username,
                request.Token,
                out string downloadUsername))
        {
            // DownloadManager uses short-lived connections. Authorize them
            // with the main login token without creating a second account session.
            session.Username = downloadUsername;
            session.Token = request.Token;
        }

        if (RequiresAuthenticatedSession(request.Command) &&
            string.IsNullOrWhiteSpace(session.Username))
        {
            await SendPacketAsync(stream, new ProtocolPacket
            {
                Command = PacketCommand.ERROR_RESP,
                ErrorCode = "401_NOT_AUTHENTICATED",
                Message = "Vui lòng đăng nhập trước."
            });
            return uploadHandler;
        }

        if (request.Command == PacketCommand.CHANGE_PASSWORD &&
            !string.Equals(request.Username, session.Username, StringComparison.OrdinalIgnoreCase))
        {
            await SendPacketAsync(stream, new ProtocolPacket
            {
                Command = PacketCommand.ERROR_RESP,
                ErrorCode = "403_SESSION_USER_MISMATCH",
                Message = "Phiên đăng nhập không hợp lệ."
            });
            return uploadHandler;
        }

        switch (request.Command)
        {
            case PacketCommand.GET_LIST:
                await SendFileListAsync(stream);
                break;

            case PacketCommand.PING:
                await SendPacketAsync(stream, new ProtocolPacket
                {
                    Command = PacketCommand.PONG
                });
                break;

            case PacketCommand.DOWNLOAD_REQ:
                {
                    ServerLogger.LogInfo(
                        $"[{clientIp}] Yeu cau tai file: {request.FileName}");

                    if (string.IsNullOrWhiteSpace(request.FileName))
                    {
                        await SendPacketAsync(
                            stream,
                            new ProtocolPacket
                            {
                                Command = PacketCommand.ERROR_RESP,
                                ErrorCode = "400_BAD_REQUEST",
                                Message = "Tên tệp không hợp lệ."
                            });

                        break;
                    }

                    string safeFileName = Path.GetFileName(request.FileName);

                    if (string.IsNullOrWhiteSpace(safeFileName))
                    {
                        await SendPacketAsync(
                            stream,
                            new ProtocolPacket
                            {
                                Command = PacketCommand.ERROR_RESP,
                                ErrorCode = "400_BAD_REQUEST",
                                Message = "Tên tệp không hợp lệ."
                            });

                        break;
                    }

                    string filePath = Path.Combine(
                        ServerConfig.StoragePath,
                        safeFileName);

                using CancellationTokenSource downloadCts =
                    SocketTimeoutManager.CreateLinkedCts(30 * 60 * 1000);

                    await FileStreamer.StreamFileAsync(
                        stream,
                        filePath,
                        safeFileName,
                        request.RequestedRateBytesPerSecond,
                        downloadCts.Token);

                    break;
                }

            // Protocol & dispatch foundation owned by Nguyen Duc Duy.
            // Feature handlers are implemented by the corresponding owners
            // and will be connected here when their branches are merged.
            case PacketCommand.REGISTER:
            case PacketCommand.LOGIN:
            case PacketCommand.CHANGE_PASSWORD:
            case PacketCommand.RESET_PASSWORD:
            case PacketCommand.CHECK_USER:
            case PacketCommand.LOGOUT:
                await AuthHandler.HandleAsync(stream, request, session);
                break;

            case PacketCommand.UPLOAD_REQ:
                uploadHandler ??= new UploadHandler(stream, ServerConfig.StoragePath);
                await uploadHandler.HandleUploadRequestAsync(request);
                break;

            case PacketCommand.UPLOAD_CHUNK:
                if (uploadHandler is null)
                {
                    await SendPacketAsync(stream, new ProtocolPacket
                    {
                        Command = PacketCommand.ERROR_RESP,
                        ErrorCode = "400_UPLOAD_NOT_STARTED",
                        Message = "Chưa có phiên tải lên."
                    });
                }
                else
                {
                    await uploadHandler.HandleUploadChunkAsync(request);
                }
                break;

            case PacketCommand.UPLOAD_DONE:
                if (uploadHandler is null)
                {
                    await SendPacketAsync(stream, new ProtocolPacket
                    {
                        Command = PacketCommand.ERROR_RESP,
                        ErrorCode = "400_UPLOAD_NOT_STARTED",
                        Message = "Chưa có phiên tải lên."
                    });
                }
                else
                {
                    await uploadHandler.HandleUploadDoneAsync(request);
                }
                break;

            case PacketCommand.RENAME_FILE:
                await SendPacketAsync(
                    stream,
                    FileManager.RenameFile(
                        ServerConfig.StoragePath,
                        request.FileName,
                        request.NewFileName));
                break;

            case PacketCommand.DELETE_FILE:
                await SendPacketAsync(
                    stream,
                    FileManager.DeleteFile(
                        ServerConfig.StoragePath,
                        request.FileName));
                break;

            case PacketCommand.SET_RATE_LIMIT:
                await SettingHandler.HandleAsync(stream, request);
                break;

            default:
                await SendPacketAsync(
                    stream,
                    new ProtocolPacket
                    {
                        Command = PacketCommand.ERROR_RESP,
                        ErrorCode = "400_BAD_COMMAND",
                        Message = "Lệnh không được máy chủ hỗ trợ."
                    });

                break;
        }
    }
    catch (OperationCanceledException)
    {
        ServerLogger.LogWarning(
            $"[{clientIp}] Request bi timeout hoac bi huy.");
    }
    catch (Exception ex)
    {
        ServerLogger.LogError(
            $"[{clientIp}] Loi xu ly request: {ex.Message}");

        try
        {
            await SendPacketAsync(
                stream,
                new ProtocolPacket
                {
                    Command = PacketCommand.ERROR_RESP,
                    ErrorCode = "500_REQUEST_ERROR",
                    Message =
                        "Lỗi xử lý yêu cầu. Yêu cầu này đã kết thúc, máy chủ vẫn tiếp tục hoạt động."
                });
        }
        catch
        {
        }
    }

    return uploadHandler;
}

static bool RequiresAuthenticatedSession(PacketCommand command)
{
    return command is
        PacketCommand.GET_LIST or
        PacketCommand.PING or
        PacketCommand.DOWNLOAD_REQ or
        PacketCommand.UPLOAD_REQ or
        PacketCommand.UPLOAD_CHUNK or
        PacketCommand.UPLOAD_DONE or
        PacketCommand.RENAME_FILE or
        PacketCommand.DELETE_FILE or
        PacketCommand.SET_RATE_LIMIT or
        PacketCommand.CHANGE_PASSWORD or
        PacketCommand.LOGOUT;
}

// ─────────────────────────────────────────────────────────────
//  SEND FILE LIST
// ─────────────────────────────────────────────────────────────

static async Task SendFileListAsync(NetworkStream stream)
{
    List<ServerFileInfo> files = FileScanner.Scan(ServerConfig.StoragePath);

    string fileList = string.Join('\n',
        files.Select(f => $"{f.FileName}|{f.FileSize}|{f.FileHash}"));

    var packet = new ProtocolPacket
    {
        Command = PacketCommand.FILE_CHUNK,
        FileName = "file-list.txt",
        DataBase64 = PacketHelper.EncodeTextData(fileList),
        IsLastChunk = true
    };

    await SendPacketAsync(stream, packet);
}

// ─────────────────────────────────────────────────────────────
//  SEND PACKET
// ─────────────────────────────────────────────────────────────

static async Task SendPacketAsync(NetworkStream stream, ProtocolPacket packet)
{
    byte[] data = PacketHelper.Encode(packet);
    await stream.WriteAsync(data);
    await stream.FlushAsync();
}

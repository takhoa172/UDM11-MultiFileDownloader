using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.IO;
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
        $"--- Speed test: {mbps:F2} MB/s (giới hạn 5 MB/s) ---");

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
    string? connectedUsername = null;

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
                        $"[{clientIp}] Timeout khi cho Server nhận request.");

                    break;
                }

                if (line is null)
                {
                    break;
                }

                string? rawError = PacketValidator.ValidateRaw(line);

                if (rawError != null)
                {
                    ServerLogger.LogWarning(
                        $"[{clientIp}] Gói tin bị từ chối ({rawError})");

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
                        $"[{clientIp}] Gói tin sai định dạng: {ex.Message}");

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
                        $"[{clientIp}] Lệnh không hợp lệ ({reqError})");

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
                    if (request.Command == PacketCommand.DOWNLOAD_REQ)
                    {
                        isMainConnection = false;
                    }
                    else
                    {
                        isMainConnection = true;
                        ServerLogger.ClientConnected(clientIp, silent: false);
                    }
                }

                if (request.Command != PacketCommand.PING)
                {
                    ServerLogger.LogInfo(
                        $"[{clientIp}] Nhận lệnh {request.Command}");
                }

                connectedUsername = await ProcessRequestAsync(stream, request, clientIp, reader, connectedUsername);
            }
        }
    }
    catch (IOException ex)
    {
        ServerLogger.LogError(
            $"[{clientIp}] Mất kết nối: {ex.Message}");
    }
    catch (SocketException ex)
    {
        ServerLogger.LogError(
            $"[{clientIp}] Lỗi socket: {ex.Message}");
    }
    catch (OperationCanceledException)
    {
        ServerLogger.LogWarning(
            $"[{clientIp}] Tác vụ bị timeout.");
    }
    catch (Exception ex)
    {
        ServerLogger.LogError(
            $"[{clientIp}] Lỗi xử lý client: {ex.Message}");
    }
    finally
    {
        if (connectionCounted && isMainConnection)
        {
            ServerLogger.ClientDisconnected(clientIp, silent: false);
        }
    }
}
// ─────────────────────────────────────────────────────────────
//  PROCESS REQUEST
// ─────────────────────────────────────────────────────────────

static async Task<string?> ProcessRequestAsync(
    NetworkStream stream,
    ProtocolPacket request,
    string clientIp,
    StreamReader mainReader,
    string? connectedUsername)
{
    try
    {
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
                        $"[{clientIp}] Yêu cầu tải file: {request.FileName}");

                    if (string.IsNullOrWhiteSpace(request.FileName))
                    {
                        await SendPacketAsync(
                            stream,
                            new ProtocolPacket
                            {
                                Command = PacketCommand.ERROR_RESP,
                                ErrorCode = "400_BAD_REQUEST",
                                Message = "Tên file không hợp lệ."
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
                                Message = "Tên file không hợp lệ."
                            });

                        break;
                    }

                    string filePath = Path.Combine(
                        ServerConfig.StoragePath,
                        safeFileName);

                using CancellationTokenSource downloadCts =
                    SocketTimeoutManager.CreateLinkedCts(30 * 60 * 1000);

                    RateLimiter? userLimiter = connectedUsername != null
                        ? ServerConfig.GetUserLimiter(connectedUsername, UserStore.GetSpeedLimit(connectedUsername))
                        : null;

                    await FileStreamer.StreamFileAsync(
                        stream,
                        filePath,
                        safeFileName,
                        request.Offset,
                        downloadCts.Token,
                        userLimiter);

                    break;
                }

            case PacketCommand.LOGIN:
                {
                    try
                    {
                        var result = UserStore.Login(
                            request.Username ?? "",
                            request.Password ?? "");

                        ServerLogger.LogInfo(
                            $"[{clientIp}] Đăng nhập: {request.Username} -> {(result.Success ? "OK" : "FAIL")}");

                        if (result.Success)
                            connectedUsername = request.Username;

                        await SendPacketAsync(
                            stream,
                            new ProtocolPacket
                            {
                                Command = result.Success
                                    ? PacketCommand.PONG
                                    : PacketCommand.ERROR_RESP,
                                ErrorCode = result.Success ? null : "401_LOGIN_FAILED",
                                Message = result.Message
                            });
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.LogError(
                            $"[{clientIp}] Lỗi xử lý đăng nhập: {ex.Message}");

                        await SendPacketAsync(
                            stream,
                            new ProtocolPacket
                            {
                                Command = PacketCommand.ERROR_RESP,
                                ErrorCode = "500_SERVER_ERROR",
                                Message = "Lỗi server khi đăng nhập."
                            });
                    }

                    break;
                }

            case PacketCommand.REGISTER:
                {
                    try
                    {
                        var result = UserStore.Register(
                            request.Username ?? "",
                            request.Password ?? "");

                        ServerLogger.LogInfo(
                            $"[{clientIp}] Đăng ký: {request.Username} -> {(result.Success ? "OK" : "FAIL")}");

                        await SendPacketAsync(
                            stream,
                            new ProtocolPacket
                            {
                                Command = result.Success
                                    ? PacketCommand.PONG
                                    : PacketCommand.ERROR_RESP,
                                ErrorCode = result.Success ? null : "400_REGISTER_FAILED",
                                Message = result.Message
                            });
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.LogError(
                            $"[{clientIp}] Lỗi xử lý đăng ký: {ex.Message}");

                        await SendPacketAsync(
                            stream,
                            new ProtocolPacket
                            {
                                Command = PacketCommand.ERROR_RESP,
                                ErrorCode = "500_SERVER_ERROR",
                                Message = "Lỗi server khi đăng ký."
                            });
                    }

                    break;
                }

            case PacketCommand.FORGOT_PASSWORD:
                {
                    try
                    {
                        var users = Server.UserStore.LoadUsersForCheck();
                        var user = users.FirstOrDefault(u =>
                            u.Username.Equals(request.Username ?? "", StringComparison.OrdinalIgnoreCase));

                        if (user == null)
                        {
                            await SendPacketAsync(stream, new ProtocolPacket
                            {
                                Command = PacketCommand.ERROR_RESP,
                                ErrorCode = "400_USER_NOT_FOUND",
                                Message = "Tên tài khoản không tồn tại."
                            });
                        }
                        else
                        {
                            ServerLogger.LogInfo(
                                $"[{clientIp}] Quên mật khẩu: {request.Username} -> OK");
                            await SendPacketAsync(stream, new ProtocolPacket
                            {
                                Command = PacketCommand.PONG,
                                Message = "Tài khoản hợp lệ. Vui lòng nhập mật khẩu mới."
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.LogError(
                            $"[{clientIp}] Lỗi xử lý quên mật khẩu: {ex.Message}");
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "500_SERVER_ERROR",
                            Message = "Lỗi server khi xác nhận tài khoản."
                        });
                    }

                    break;
                }

            case PacketCommand.RESET_PASSWORD:
                {
                    try
                    {
                        var result = UserStore.ResetPassword(
                            request.Username ?? "",
                            request.Password ?? "");

                        ServerLogger.LogInfo(
                            $"[{clientIp}] Đặt lại mật khẩu: {request.Username} -> {(result.Success ? "OK" : "FAIL")}");

                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = result.Success
                                ? PacketCommand.PONG
                                : PacketCommand.ERROR_RESP,
                            ErrorCode = result.Success ? null : "400_RESET_FAILED",
                            Message = result.Message
                        });
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.LogError(
                            $"[{clientIp}] Lỗi đặt lại mật khẩu: {ex.Message}");
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "500_SERVER_ERROR",
                            Message = "Lỗi server khi đặt lại mật khẩu."
                        });
                    }

                    break;
                }

            case PacketCommand.UPLOAD_REQ:
                {
                    ServerLogger.LogInfo(
                        $"[{clientIp}] Yêu cầu tải lên file: {request.FileName}");

                    if (string.IsNullOrWhiteSpace(request.FileName))
                    {
                        await SendPacketAsync(
                            stream,
                            new ProtocolPacket
                            {
                                Command = PacketCommand.ERROR_RESP,
                                ErrorCode = "400_BAD_REQUEST",
                                Message = "Tên file không hợp lệ."
                            });
                        break;
                    }

                    string safeFileName = Path.GetFileName(request.FileName);
                    string filePath = Path.Combine(
                        ServerConfig.StoragePath, safeFileName);

                    if (File.Exists(filePath))
                    {
                        string nameNoExt = Path.GetFileNameWithoutExtension(safeFileName);
                        string ext = Path.GetExtension(safeFileName);
                        int n = 1;
                        do
                        {
                            safeFileName = $"{nameNoExt}({n}){ext}";
                            filePath = Path.Combine(ServerConfig.StoragePath, safeFileName);
                            n++;
                        } while (File.Exists(filePath));

                        ServerLogger.LogInfo(
                            $"[{clientIp}] File trùng, đổi tên thành: {safeFileName}");
                    }

                    await SendPacketAsync(stream, new ProtocolPacket
                    {
                        Command = PacketCommand.PONG,
                        FileName = safeFileName
                    });

                    try
                    {
                        using var fileStream = new FileStream(
                            filePath,
                            FileMode.Create,
                            FileAccess.Write,
                            FileShare.None,
                            ServerConfig.BufferSize,
                            useAsync: true);

                        while (true)
                        {
                            string? chunkLine = await mainReader.ReadLineAsync();
                            if (chunkLine == null)
                                throw new IOException("Client đóng kết nối.");

                            ProtocolPacket chunk = PacketHelper.Decode(chunkLine);

                            if (!string.IsNullOrEmpty(chunk.DataBase64))
                            {
                                byte[] data = PacketHelper.DecodeBinaryData(chunk.DataBase64);
                                await fileStream.WriteAsync(data, 0, data.Length);
                            }

                            if (chunk.IsLastChunk)
                                break;
                        }

                        ServerLogger.LogInfo(
                            $"[{clientIp}] Tải lên thành công: {safeFileName}");

                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.PONG,
                            Message = $"Tải lên thành công: {safeFileName}"
                        });
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.LogError(
                            $"[{clientIp}] Lỗi tải lên: {ex.Message}");

                        try
                        {
                            if (File.Exists(filePath))
                                File.Delete(filePath);
                        }
                        catch { }

                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "500_UPLOAD_ERROR",
                            Message = $"Lỗi tải lên: {ex.Message}"
                        });
                    }

                    break;
                }

            case PacketCommand.DELETE_REQ:
                {
                    ServerLogger.LogInfo(
                        $"[{clientIp}] Yêu cầu xóa file: {request.FileName}");

                    if (string.IsNullOrWhiteSpace(request.FileName))
                    {
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "400_BAD_REQUEST",
                            Message = "Tên file không hợp lệ."
                        });
                        break;
                    }

                    string safeFileName = Path.GetFileName(request.FileName);
                    string filePath = Path.Combine(
                        ServerConfig.StoragePath, safeFileName);

                    if (!File.Exists(filePath))
                    {
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "404_NOT_FOUND",
                            Message = $"File '{safeFileName}' không tồn tại trên Server."
                        });
                        break;
                    }

                    try
                    {
                        File.Delete(filePath);
                        ServerLogger.LogInfo(
                            $"[{clientIp}] Đã xóa file: {safeFileName}");
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.PONG,
                            Message = $"Đã xóa file: {safeFileName}"
                        });
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.LogError(
                            $"[{clientIp}] Lỗi xóa file: {ex.Message}");
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "500_DELETE_ERROR",
                            Message = $"Lỗi xóa file: {ex.Message}"
                        });
                    }

                    break;
                }

            case PacketCommand.RENAME_REQ:
                {
                    ServerLogger.LogInfo(
                        $"[{clientIp}] Yêu cầu đổi tên file: {request.FileName} -> {request.NewFileName}");

                    if (string.IsNullOrWhiteSpace(request.FileName) ||
                        string.IsNullOrWhiteSpace(request.NewFileName))
                    {
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "400_BAD_REQUEST",
                            Message = "Tên file không hợp lệ."
                        });
                        break;
                    }

                    string safeOldName = Path.GetFileName(request.FileName);
                    string safeNewName = Path.GetFileName(request.NewFileName);
                    string oldPath = Path.Combine(ServerConfig.StoragePath, safeOldName);
                    string newPath = Path.Combine(ServerConfig.StoragePath, safeNewName);

                    if (!File.Exists(oldPath))
                    {
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "404_NOT_FOUND",
                            Message = $"File '{safeOldName}' không tồn tại trên Server."
                        });
                        break;
                    }

                    if (File.Exists(newPath))
                    {
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "409_CONFLICT",
                            Message = $"File '{safeNewName}' đã tồn tại trên Server."
                        });
                        break;
                    }

                    try
                    {
                        File.Move(oldPath, newPath);
                        ServerLogger.LogInfo(
                            $"[{clientIp}] Đã đổi tên: {safeOldName} -> {safeNewName}");
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.PONG,
                            Message = $"Đã đổi tên: {safeOldName} -> {safeNewName}"
                        });
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.LogError(
                            $"[{clientIp}] Lỗi đổi tên file: {ex.Message}");
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "500_RENAME_ERROR",
                            Message = $"Lỗi đổi tên file: {ex.Message}"
                        });
                    }

                    break;
                }

            case PacketCommand.SET_SPEED:
                {
                    if (connectedUsername == null)
                    {
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "401_UNAUTHORIZED",
                            Message = "Vui lòng đăng nhập trước."
                        });
                        break;
                    }

                    try
                    {
                        long speedMBs = request.SpeedLimitMBs ?? 5;
                        var result = UserStore.SetSpeedLimit(connectedUsername, speedMBs);

                        ServerLogger.LogInfo(
                            $"[{clientIp}] Đặt tốc độ tải: {connectedUsername} -> {speedMBs} MB/s");

                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = result.Success
                                ? PacketCommand.PONG
                                : PacketCommand.ERROR_RESP,
                            ErrorCode = result.Success ? null : "400_SET_SPEED_FAILED",
                            Message = result.Message,
                            SpeedLimitMBs = speedMBs
                        });
                    }
                    catch (Exception ex)
                    {
                        ServerLogger.LogError(
                            $"[{clientIp}] Lỗi đặt tốc độ tải: {ex.Message}");
                        await SendPacketAsync(stream, new ProtocolPacket
                        {
                            Command = PacketCommand.ERROR_RESP,
                            ErrorCode = "500_SERVER_ERROR",
                            Message = "Lỗi server khi đặt tốc độ tải."
                        });
                    }

                    break;
                }

            default:
                await SendPacketAsync(
                    stream,
                    new ProtocolPacket
                    {
                        Command = PacketCommand.ERROR_RESP,
                        ErrorCode = "400_BAD_COMMAND",
                        Message = "Lệnh không được Server hỗ trợ."
                    });

                break;
        }
    }
    catch (OperationCanceledException)
    {
        ServerLogger.LogWarning(
            $"[{clientIp}] Request bị timeout hoặc bị hủy.");
    }
    catch (Exception ex)
    {
        ServerLogger.LogError(
            $"[{clientIp}] Lỗi xử lý request: {ex.Message}");

        try
        {
            await SendPacketAsync(
                stream,
                new ProtocolPacket
                {
                    Command = PacketCommand.ERROR_RESP,
                    ErrorCode = "500_REQUEST_ERROR",
                    Message =
                        "Lỗi xử lý request. Request này đã kết thúc, Server vẫn tiếp tục hoạt động."
                });
        }
        catch
        {
        }
    }

    return connectedUsername;
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

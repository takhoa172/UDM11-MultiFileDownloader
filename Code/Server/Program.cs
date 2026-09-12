using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using Shared;
using Server;

const int ServerPort = 8080;

const int ClientReadTimeoutMs = 30000;
const int ClientWriteTimeoutMs = 30000;

TcpListener listener = new(IPAddress.Any, ServerPort);
listener.Start();

ServerLogger.LogServerStart(ServerPort);

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

    try
    {
        using (client)
        await using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new(stream))
        {
            client.ReceiveTimeout = ClientReadTimeoutMs;
            client.SendTimeout = ClientWriteTimeoutMs;

            while (true)
            {
                string? line;

                try
                {
                    using CancellationTokenSource readCts =
                        new(TimeSpan.FromMilliseconds(ClientReadTimeoutMs));

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
                            Message = "Goi tin khong hop le"
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
                            Message = "Goi tin khong hop le."
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
                            Message = "Lenh khong hop le"
                        });

                    continue;
                }

                if (!connectionCounted)
                {
                    connectionCounted = true;

                    if (request.Command == PacketCommand.GET_LIST)
                    {
                        isMainConnection = true;
                        ServerLogger.ClientConnected(clientIp, silent: false);
                    }
                    else
                    {
                        isMainConnection = false;
                        ServerLogger.ClientConnected(clientIp, silent: true);
                    }
                }

                ServerLogger.LogInfo(
                    $"[{clientIp}] Nhan lenh {request.Command}");

                await ProcessRequestAsync(stream, request, clientIp);
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
        if (connectionCounted)
        {
            if (isMainConnection)
            {
                ServerLogger.ClientDisconnected(clientIp, silent: false);
            }
            else
            {
                ServerLogger.ClientDisconnected(clientIp, silent: true);
            }
        }
    }
}
// ─────────────────────────────────────────────────────────────
//  PROCESS REQUEST
// ─────────────────────────────────────────────────────────────

static async Task ProcessRequestAsync(
    NetworkStream stream,
    ProtocolPacket request,
    string clientIp)
{
    try
    {
        switch (request.Command)
        {
            case PacketCommand.GET_LIST:
                await SendFileListAsync(stream);
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
                                Message = "Ten file khong hop le."
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
                                Message = "Ten file khong hop le."
                            });

                        break;
                    }

                    string filePath = Path.Combine(
                        ServerConfig.StoragePath,
                        safeFileName);

                    using CancellationTokenSource downloadCts =
                        new(TimeSpan.FromMinutes(30));

                    await FileStreamer.StreamFileAsync(
                        stream,
                        filePath,
                        safeFileName,
                        downloadCts.Token);

                    break;
                }

            default:
                await SendPacketAsync(
                    stream,
                    new ProtocolPacket
                    {
                        Command = PacketCommand.ERROR_RESP,
                        ErrorCode = "400_BAD_COMMAND",
                        Message = "Lenh khong duoc Server ho tro."
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
                        "Loi xu ly request. Request nay da ket thuc, Server van tiep tuc hoat dong."
                });
        }
        catch
        {
        }
    }
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
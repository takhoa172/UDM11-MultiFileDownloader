using System.Net;
using System.Net.Sockets;
using Shared;
using Server;
using System.Diagnostics;
using System.Text;

const int ServerPort = 8080;

TcpListener listener = new(IPAddress.Any, ServerPort);
listener.Start();

ServerLogger.LogServerStart(ServerPort);

//RateLimiter downloadLimiter = new RateLimiter(5 * 1024 * 1024);
if (args.Length > 0 && args[0] == "--speedtest")
{
    RateLimiter rl = new RateLimiter(5 * 1024 * 1024);
    long totalBytes = 30L * 1024 * 1024;
    Stopwatch sw = Stopwatch.StartNew();
    const int chunk = 64 * 1024;
    for (long sent = 0; sent < totalBytes; sent += chunk)
        await rl.ThrottleAsync((int)Math.Min(chunk, totalBytes - sent));
    sw.Stop();
    double mbps = totalBytes / 1024.0 / 1024.0 / sw.Elapsed.TotalSeconds;
    Console.WriteLine($"--- Speed test: {mbps:F2} MB/s (gioi han 5 MB/s) ---");
    return;
}

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => HandleClientAsync(client));
}

static async Task HandleClientAsync(TcpClient client)
{
    string clientIp = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
    ServerLogger.ClientConnected(clientIp);

    try
    {
        using (client)
        await using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new(stream))
        {
            while (true)
            {
                string? line = await reader.ReadLineAsync();
                if (line is null)
                {
                    //ServerLogger.ClientDisconnected(clientIp, "Client ngat ket noi.");
                    break;
                }

                string? rawError = PacketValidator.ValidateRaw(line);
                if (rawError != null)
                {
                    ServerLogger.LogWarning($"[{clientIp}] Goi tin bi tu choi ({rawError})");
                    await SendPacketAsync(stream, new ProtocolPacket
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
                    ServerLogger.LogWarning($"[{clientIp}] Goi tin sai dinh dang: {ex.Message}");
                    await SendPacketAsync(stream, new ProtocolPacket
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
                    ServerLogger.LogWarning($"[{clientIp}] Lenh khong hop le ({reqError})");
                    await SendPacketAsync(stream, new ProtocolPacket
                    {
                        Command = PacketCommand.ERROR_RESP,
                        ErrorCode = reqError,
                        Message = "Lenh khong hop le"
                    });
                    continue;
                }

                ServerLogger.LogInfo($"[{clientIp}] Nhan lenh {request.Command}");
                await ProcessRequestAsync(stream, request, clientIp);
            }
        }
    }
    catch (IOException ex)
    {
        ServerLogger.LogError($"[{clientIp}] Mat ket noi: {ex.Message}");
    }
    catch (SocketException ex)
    {
        ServerLogger.LogError($"[{clientIp}] Loi socket: {ex.Message}");
    }
    catch (Exception ex)
    {
        ServerLogger.LogError($"[{clientIp}] Loi xu ly client: {ex.Message}");
    }
    finally
    {
        ServerLogger.ClientDisconnected(clientIp);
    }
}

static async Task ProcessRequestAsync(NetworkStream stream, ProtocolPacket request, string clientIp)
{
    switch (request.Command)
    {
        case PacketCommand.GET_LIST:
            await SendFileListAsync(stream);
            break;

        case PacketCommand.DOWNLOAD_REQ:
            ServerLogger.LogInfo($"[{clientIp}] Yeu cau tai file: {request.FileName}");
            await SendSampleFileAsync(stream, request.FileName);
            break;

        default:
            await SendPacketAsync(stream, new ProtocolPacket
            {
                Command = PacketCommand.ERROR_RESP,
                ErrorCode = "400_BAD_COMMAND",
                Message = "Lenh khong duoc Server ho tro."
            });
            break;
    }
}

static async Task SendFileListAsync(NetworkStream stream)
{
    List<ServerFileInfo> files =
        FileScanner.Scan(ServerConfig.StoragePath);

    string fileList = string.Join(
        '\n',
        files.Select(file =>
            $"{file.FileName}|{file.FileSize}|{file.FileHash}"));

    await SendPacketAsync(stream, new ProtocolPacket
    {
        Command = PacketCommand.FILE_CHUNK,
        FileName = "file-list.txt",
        DataBase64 = PacketHelper.EncodeTextData(fileList),
        IsLastChunk = true
    });
}

static async Task SendSampleFileAsync(NetworkStream stream, string? fileName)
{
    if (string.IsNullOrWhiteSpace(fileName) || !GetSampleFiles().TryGetValue(fileName, out string? content))
    {
        await SendPacketAsync(stream, new ProtocolPacket
        {
            Command = PacketCommand.ERROR_RESP,
            ErrorCode = "404_NOT_FOUND",
            Message = "Khong tim thay file yeu cau."
        });
        return;
    }

    ServerLogger.LogDownload(fileName, content.Length);
    byte[] contentBytes = Encoding.UTF8.GetBytes(content);
    string fileHash = HashHelper.CalculateSha256(contentBytes);

    if (Environment.GetEnvironmentVariable("DEMO_CORRUPT_HASH") == "1")
        fileHash = (fileHash[0] == '0' ? "1" : "0") + fileHash.Substring(1);
    await SendBytesAsync(stream, PacketHelper.Encode(new ProtocolPacket
    {
        Command = PacketCommand.FILE_CHUNK,
        FileName = fileName,
        DataBase64 = PacketHelper.EncodeTextData(content),
        FileHash = fileHash,
        IsLastChunk = true
    }), ServerConfig.DownloadLimiter);
} 
 
static async Task SendBytesAsync(NetworkStream stream, byte[] data, RateLimiter limiter)
{
    const int chunkSize = 64 * 1024;
    for (int offset = 0; offset < data.Length; offset += chunkSize)
    {
        int len = Math.Min(chunkSize, data.Length - offset);
        await limiter.ThrottleAsync(len);
        await stream.WriteAsync(data.AsMemory(offset, len));
        await stream.FlushAsync();
    }
}
static async Task SendPacketAsync(NetworkStream stream, ProtocolPacket packet)
{
    byte[] data = PacketHelper.Encode(packet);
    await stream.WriteAsync(data);
    await stream.FlushAsync();
}

static Dictionary<string, string> GetSampleFiles() => new()
{
    ["tailieu_mang.txt"] = "Noi dung mau cua file tai lieu mang.",
    ["bao_cao_tien_do.txt"] = "Ban demo Core TCP Socket va Protocol.",
    ["huong_dan_test.txt"] = "Chay Server truoc, sau do chay Client de ket noi."
};


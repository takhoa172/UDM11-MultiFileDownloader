using System.Net;
using System.Net.Sockets;
using Shared;

const int ServerPort = 8080;

TcpListener listener = new(IPAddress.Any, ServerPort);
listener.Start();

Log("SERVER", $"Dang lang nghe tai cong {ServerPort}.");

while (true)
{
    TcpClient client = await listener.AcceptTcpClientAsync();
    _ = Task.Run(() => HandleClientAsync(client));
}

static async Task HandleClientAsync(TcpClient client)
{
    string clientIp = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
    Log(clientIp, "Client ket noi.");

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
                    Log(clientIp, "Client ngat ket noi.");
                    break;
                }

                ProtocolPacket request;
                try
                {
                    request = PacketHelper.Decode(line);
                }
                catch (Exception ex)
                {
                    Log(clientIp, $"Goi tin sai dinh dang: {ex.Message}");
                    await SendPacketAsync(stream, new ProtocolPacket
                    {
                        Command = PacketCommand.ERROR_RESP,
                        ErrorCode = "400_BAD_REQUEST",
                        Message = "Goi tin khong hop le."
                    });
                    continue;
                }

                Log(clientIp, $"Nhan lenh {request.Command}.");
                await ProcessRequestAsync(stream, request, clientIp);
            }
        }
    }
    catch (IOException ex)
    {
        Log(clientIp, $"Mat ket noi dot ngot: {ex.Message}");
    }
    catch (SocketException ex)
    {
        Log(clientIp, $"Loi socket: {ex.Message}");
    }
    catch (Exception ex)
    {
        Log(clientIp, $"Loi xu ly client: {ex.Message}");
    }
    finally
    {
        Log(clientIp, "Da dong ket noi client.");
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
            Log(clientIp, $"Yeu cau tai file: {request.FileName}");
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
    string fileList = string.Join('\n', GetSampleFiles().Keys);
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

    await SendPacketAsync(stream, new ProtocolPacket
    {
        Command = PacketCommand.FILE_CHUNK,
        FileName = fileName,
        DataBase64 = PacketHelper.EncodeTextData(content),
        IsLastChunk = true
    });
}

static async Task SendPacketAsync(NetworkStream stream, ProtocolPacket packet)
{
    byte[] data = PacketHelper.Encode(packet);
    await stream.WriteAsync(data);
    await stream.FlushAsync();
}

static void Log(string source, string message)
{
    Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{source}] {message}");
}

static Dictionary<string, string> GetSampleFiles() => new()
{
    ["tailieu_mang.txt"] = "Noi dung mau cua file tai lieu mang.",
    ["bao_cao_tien_do.txt"] = "Ban demo Core TCP Socket va Protocol.",
    ["huong_dan_test.txt"] = "Chay Server truoc, sau do chay Client de ket noi."
};


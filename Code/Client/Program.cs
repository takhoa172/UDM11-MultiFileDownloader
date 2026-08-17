using System.Net.Sockets;
using Shared;

Console.Write("Nhap IP Server (vi du 127.0.0.1): ");
string serverIp = ReadRequiredInput();

Console.Write("Nhap Port Server (vi du 8080): ");
int serverPort = ReadPort();

try
{
    using TcpClient client = new();
    Console.WriteLine("Dang ket noi Server...");
    await client.ConnectAsync(serverIp, serverPort);
    Console.WriteLine("Da ket noi Server.");

    await using NetworkStream stream = client.GetStream();
    using StreamReader reader = new(stream);

    await SendPacketAsync(stream, new ProtocolPacket
    {
        Command = PacketCommand.GET_LIST
    });

    ProtocolPacket listResponse = await ReadPacketAsync(reader);
    if (listResponse.Command == PacketCommand.ERROR_RESP)
    {
        Console.WriteLine($"Server bao loi: {listResponse.ErrorCode} - {listResponse.Message}");
        return;
    }

    string fileList = PacketHelper.DecodeTextData(listResponse.DataBase64);
    Console.WriteLine("\nDanh sach file Server gui ve:");
    Console.WriteLine(fileList);

    // Cho phep tai thu 1 file mau de chung minh DOWNLOAD_REQ va FILE_CHUNK.
    Console.Write("\nNhap ten file muon tai thu (Enter de bo qua): ");
    string? fileName = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(fileName))
    {
        await SendPacketAsync(stream, new ProtocolPacket
        {
            Command = PacketCommand.DOWNLOAD_REQ,
            FileName = fileName.Trim()
        });

        ProtocolPacket downloadResponse = await ReadPacketAsync(reader);
        if (downloadResponse.Command == PacketCommand.ERROR_RESP)
        {
            Console.WriteLine($"Server bao loi: {downloadResponse.ErrorCode} - {downloadResponse.Message}");
            return;
        }

        string fileContent = PacketHelper.DecodeTextData(downloadResponse.DataBase64);
        Console.WriteLine($"\nNoi dung FILE_CHUNK cua {downloadResponse.FileName}:");
        Console.WriteLine(fileContent);
    }

    Console.WriteLine("\nNhan Enter de thoat Client.");
    Console.ReadLine();
}
catch (SocketException ex)
{
    Console.WriteLine($"Khong ket noi duoc Server: {ex.Message}");
}
catch (IOException ex)
{
    Console.WriteLine($"Ket noi bi ngat: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Loi Client: {ex.Message}");
}

static string ReadRequiredInput()
{
    while (true)
    {
        string? input = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(input))
        {
            return input.Trim();
        }

        Console.Write("Gia tri khong duoc rong, nhap lai: ");
    }
}

static int ReadPort()
{
    while (true)
    {
        string input = ReadRequiredInput();
        if (int.TryParse(input, out int port) && port is > 0 and <= 65535)
        {
            return port;
        }

        Console.Write("Port khong hop le, nhap lai: ");
    }
}

static async Task SendPacketAsync(NetworkStream stream, ProtocolPacket packet)
{
    byte[] data = PacketHelper.Encode(packet);
    await stream.WriteAsync(data);
    await stream.FlushAsync();
}

static async Task<ProtocolPacket> ReadPacketAsync(StreamReader reader)
{
    string? line = await reader.ReadLineAsync();
    if (line is null)
    {
        throw new IOException("Server da dong ket noi.");
    }

    return PacketHelper.Decode(line);
}

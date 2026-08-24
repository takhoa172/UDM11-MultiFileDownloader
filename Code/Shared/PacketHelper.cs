using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared;

public enum PacketCommand
{
    GET_LIST,
    DOWNLOAD_REQ,
    FILE_CHUNK,
    ERROR_RESP
}

public sealed class ProtocolPacket
{
    public PacketCommand Command { get; set; }
    public string? FileName { get; set; }
    public string? ErrorCode { get; set; }
    public string? Message { get; set; }
    public string? DataBase64 { get; set; }
    public bool IsLastChunk { get; set; } = true;
}

public static class PacketHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static byte[] Encode(ProtocolPacket packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        // Moi goi tin la 1 dong JSON, ket thuc bang newline de tach goi tren TCP.
        string json = JsonSerializer.Serialize(packet, JsonOptions);
        return Encoding.UTF8.GetBytes(json + "\n");
    }

    public static ProtocolPacket Decode(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);

        string json = Encoding.UTF8.GetString(data);
        return Decode(json);
    }

    public static ProtocolPacket Decode(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
        {
            throw new InvalidDataException("Goi tin rong.");
        }

        ProtocolPacket? packet = JsonSerializer.Deserialize<ProtocolPacket>(data.Trim(), JsonOptions);
        if (packet is null)
        {
            throw new InvalidDataException("Khong doc duoc goi tin.");
        }

        return packet;
    }

    public static string EncodeTextData(string text)
    {
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(text));
    }

    public static string DecodeTextData(string? dataBase64)
    {
        if (string.IsNullOrWhiteSpace(dataBase64))
        {
            return string.Empty;
        }

        byte[] bytes = Convert.FromBase64String(dataBase64);
        return Encoding.UTF8.GetString(bytes);
    }

    public static string EncodeBinaryData(byte[] bytes)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        return Convert.ToBase64String(bytes);
    }

    public static byte[] DecodeBinaryData(string? dataBase64)
    {
        if (string.IsNullOrWhiteSpace(dataBase64))
        {
            return Array.Empty<byte>();
        }

        return Convert.FromBase64String(dataBase64);
    }
}

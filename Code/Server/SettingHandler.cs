using System.Net.Sockets;
using Shared;

namespace Server;

public static class SettingHandler
{
    public static async Task HandleAsync(NetworkStream stream, ProtocolPacket request)
    {
        string? error = PacketValidator.ValidateSetting(request.RequestedRateBytesPerSecond);

        byte[] data = PacketHelper.Encode(new ProtocolPacket
        {
            Command = PacketCommand.SETTING_RESP,
            Success = error == null,
            Message = error ?? "Da cap nhat toc do."
        });

        using CancellationTokenSource cts =
            new(TimeSpan.FromMilliseconds(ServerConfig.WriteTimeoutMs));

        await stream.WriteAsync(data.AsMemory(), cts.Token);
        await stream.FlushAsync(cts.Token);
    }
}

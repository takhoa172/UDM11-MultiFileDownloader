using System.Net.Sockets;
using Shared;

namespace Server;

public static class AuthHandler
{
    public static async Task HandleAsync(NetworkStream stream, ProtocolPacket request)
    {
        switch (request.Command)
        {
            case PacketCommand.REGISTER:
                await HandleRegisterAsync(stream, request);
                break;
            case PacketCommand.LOGIN:
                await HandleLoginAsync(stream, request);
                break;
            case PacketCommand.CHANGE_PASSWORD:
                await HandleChangePasswordAsync(stream, request);
                break;
            default:
                await SendAuthResponseAsync(stream, false, "400_BAD_COMMAND", null);
                break;
        }
    }

    private static async Task HandleRegisterAsync(NetworkStream stream, ProtocolPacket request)
    {
        string? error = PacketValidator.ValidateRegister(request.Username, request.PasswordHash);
        if (error != null)
        {
            await SendAuthResponseAsync(stream, false, error, null);
            return;
        }

        bool ok = UserStore.Register(request.Username!, request.PasswordHash!);
        ServerLogger.LogInfo($"[AUTH] Dang ky '{request.Username}' - {(ok ? "OK" : "FAIL")}");

        await SendAuthResponseAsync(stream, ok,
            ok ? "Dang ky thanh cong." : "Ten dang nhap da ton tai.", null);
    }

    private static async Task HandleLoginAsync(NetworkStream stream, ProtocolPacket request)
    {
        string? error = PacketValidator.ValidateLogin(request.Username, request.PasswordHash);
        if (error != null)
        {
            await SendAuthResponseAsync(stream, false, error, null);
            return;
        }

        bool ok = UserStore.ValidateLogin(request.Username!, request.PasswordHash!);
        ServerLogger.LogInfo($"[AUTH] Dang nhap '{request.Username}' - {(ok ? "OK" : "FAIL")}");

        string? token = ok ? Guid.NewGuid().ToString("N") : null;

        await SendAuthResponseAsync(stream, ok,
            ok ? "Dang nhap thanh cong." : "Sai ten dang nhap hoac mat khau.", token);
    }

    private static async Task HandleChangePasswordAsync(NetworkStream stream, ProtocolPacket request)
    {
        string? error = PacketValidator.ValidateChangePassword(request.Username, request.NewPasswordHash);
        if (error != null)
        {
            await SendAuthResponseAsync(stream, false, error, null);
            return;
        }

        if (string.IsNullOrWhiteSpace(request.PasswordHash))
        {
            await SendAuthResponseAsync(stream, false, "400_INVALID_CREDENTIALS", null);
            return;
        }

        if (!UserStore.ValidateLogin(request.Username!, request.PasswordHash!))
        {
            ServerLogger.LogInfo($"[AUTH] Doi mat khau '{request.Username}' - FAIL (sai mat khau cu)");
            await SendAuthResponseAsync(stream, false, "Sai mat khau hien tai.", null);
            return;
        }

        bool ok = UserStore.ChangePassword(request.Username!, request.NewPasswordHash!);
        ServerLogger.LogInfo($"[AUTH] Doi mat khau '{request.Username}' - {(ok ? "OK" : "FAIL")}");

        await SendAuthResponseAsync(stream, ok,
            ok ? "Doi mat khau thanh cong." : "Khong tim thay nguoi dung.", null);
    }

    private static async Task SendAuthResponseAsync(
        NetworkStream stream, bool success, string message, string? token)
    {
        byte[] data = PacketHelper.Encode(new ProtocolPacket
        {
            Command = PacketCommand.AUTH_RESP,
            Success = success,
            Message = message,
            Token = token
        });

        using CancellationTokenSource cts =
            new(TimeSpan.FromMilliseconds(ServerConfig.WriteTimeoutMs));

        await stream.WriteAsync(data.AsMemory(), cts.Token);
        await stream.FlushAsync(cts.Token);
    }
}

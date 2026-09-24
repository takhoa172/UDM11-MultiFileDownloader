using System.Net.Sockets;
using Shared;

namespace Server;

public static class AuthHandler
{
    public static async Task HandleAsync(
        NetworkStream stream,
        ProtocolPacket request,
        ClientSessionContext session)
    {
        switch (request.Command)
        {
            case PacketCommand.REGISTER:
                await HandleRegisterAsync(stream, request);
                break;
            case PacketCommand.LOGIN:
                await HandleLoginAsync(stream, request, session);
                break;
            case PacketCommand.CHANGE_PASSWORD:
                await HandleChangePasswordAsync(stream, request);
                break;
            case PacketCommand.RESET_PASSWORD:
                await HandleResetPasswordAsync(stream, request);
                break;
            case PacketCommand.CHECK_USER:
                await HandleCheckUserAsync(stream, request);
                break;
            case PacketCommand.LOGOUT:
                await HandleLogoutAsync(stream, session);
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
            ok ? "Đăng ký thành công." : "Tên đăng nhập đã tồn tại.", null);
    }

    private static async Task HandleLoginAsync(
        NetworkStream stream,
        ProtocolPacket request,
        ClientSessionContext session)
    {
        string? error = PacketValidator.ValidateLogin(request.Username, request.PasswordHash);
        if (error != null)
        {
            await SendAuthResponseAsync(stream, false, error, null);
            return;
        }

        bool ok = UserStore.ValidateLogin(request.Username!, request.PasswordHash!);
        ServerLogger.LogInfo($"[AUTH] Dang nhap '{request.Username}' - {(ok ? "OK" : "FAIL")}");

        string? token = null;
        if (ok)
        {
            if (!SessionManager.TryStart(request.Username!, session.ConnectionId, out string createdToken))
            {
                ServerLogger.LogInfo(
                    $"[AUTH] Dang nhap '{request.Username}' - FAIL (tai khoan dang co phien)");

                await SendAuthResponseAsync(
                    stream,
                    false,
                    "Tài khoản đang được đăng nhập trên thiết bị khác.",
                    null,
                    "409_SESSION_ACTIVE");
                return;
            }

            token = createdToken;
            session.Username = request.Username!.Trim();
            session.Token = token;
        }

        await SendAuthResponseAsync(stream, ok,
            ok ? "Đăng nhập thành công." : "Sai tên đăng nhập hoặc mật khẩu.", token);
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
            await SendAuthResponseAsync(stream, false, "Sai mật khẩu hiện tại.", null);
            return;
        }

        bool ok = UserStore.ChangePassword(request.Username!, request.NewPasswordHash!);
        ServerLogger.LogInfo($"[AUTH] Doi mat khau '{request.Username}' - {(ok ? "OK" : "FAIL")}");

        await SendAuthResponseAsync(stream, ok,
            ok ? "Đổi mật khẩu thành công." : "Không tìm thấy người dùng.", null);
    }

    private static async Task HandleResetPasswordAsync(NetworkStream stream, ProtocolPacket request)
    {
        string? error = PacketValidator.ValidateChangePassword(request.Username, request.NewPasswordHash);
        if (error != null)
        {
            await SendAuthResponseAsync(stream, false, error, null);
            return;
        }

        bool ok = UserStore.ChangePassword(request.Username!, request.NewPasswordHash!);
        ServerLogger.LogInfo($"[AUTH] Dat lai mat khau '{request.Username}' - {(ok ? "OK" : "FAIL")}");

        await SendAuthResponseAsync(stream, ok,
            ok ? "Đặt lại mật khẩu thành công." : "Không tìm thấy người dùng.", null);
    }

    private static async Task HandleCheckUserAsync(NetworkStream stream, ProtocolPacket request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            await SendAuthResponseAsync(stream, false, "400_INVALID_USERNAME", null);
            return;
        }

        bool exists = UserStore.UserExists(request.Username);
        ServerLogger.LogInfo(
            $"[AUTH] Kiem tra tai khoan '{request.Username}' - {(exists ? "CO" : "KHONG")}");

        await SendAuthResponseAsync(
            stream,
            exists,
            exists ? "Tài khoản tồn tại." : "Tài khoản không tồn tại.",
            null,
            exists ? null : "404_USER_NOT_FOUND");
    }

    private static async Task HandleLogoutAsync(
        NetworkStream stream,
        ClientSessionContext session)
    {
        if (string.IsNullOrWhiteSpace(session.Username))
        {
            await SendAuthResponseAsync(
                stream,
                false,
                "Phiên đăng nhập không tồn tại.",
                null,
                "401_NOT_AUTHENTICATED");
            return;
        }

        string username = session.Username;
        SessionManager.End(session.ConnectionId);
        session.Username = null;
        session.Token = null;

        ServerLogger.LogInfo($"[AUTH] Dang xuat '{username}' - OK");
        await SendAuthResponseAsync(stream, true, "Đăng xuất thành công.", null);
    }

    private static async Task SendAuthResponseAsync(
        NetworkStream stream,
        bool success,
        string message,
        string? token,
        string? errorCode = null)
    {
        byte[] data = PacketHelper.Encode(new ProtocolPacket
        {
            Command = PacketCommand.AUTH_RESP,
            Success = success,
            Message = message,
            Token = token,
            ErrorCode = errorCode
        });

        using CancellationTokenSource cts =
            new(TimeSpan.FromMilliseconds(ServerConfig.WriteTimeoutMs));

        await stream.WriteAsync(data.AsMemory(), cts.Token);
        await stream.FlushAsync(cts.Token);
    }
}

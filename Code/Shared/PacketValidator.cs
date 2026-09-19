using System;

namespace Shared;

public static class PacketValidator
{
    public const int MaxPacketBytes = 1024 * 1024;
    public const int MaxFileNameLength = 255;

    public static string? ValidateRaw(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return "400_BAD_REQUEST";
        if (raw.Length > MaxPacketBytes)
            return "413_TOO_LARGE";

        return null;
    }

    public static string? ValidateRequest(ProtocolPacket packet)
    {
        if (packet is null)
            return "400_BAD_REQUEST";
        switch (packet.Command)
        {
            case PacketCommand.GET_LIST:
                return null;
            case PacketCommand.PING:
                return null;
            case PacketCommand.DOWNLOAD_REQ:
                return ValidateFileName(packet.FileName);
            case PacketCommand.LOGIN:
                return ValidateLogin(packet.Username, packet.Password);
            case PacketCommand.REGISTER:
                return ValidateRegister(packet.Username, packet.Password);
            case PacketCommand.FORGOT_PASSWORD:
                return ValidateForgotPassword(packet.Username);
            case PacketCommand.RESET_PASSWORD:
                return ValidateResetPassword(packet.Username, packet.Password);
            case PacketCommand.UPLOAD_REQ:
                return ValidateFileName(packet.FileName);
            case PacketCommand.DELETE_REQ:
                return ValidateFileName(packet.FileName);
            case PacketCommand.RENAME_REQ:
                return ValidateRename(packet.FileName, packet.NewFileName);
            case PacketCommand.SET_SPEED:
                return ValidateSetSpeed(packet.SpeedLimitMBs);
            default:
                return "400_BAD_COMMAND";
        }
    }

    public static string? ValidateLogin(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
            return "400_INVALID_USERNAME";
        if (string.IsNullOrWhiteSpace(password) || password.Length > 100)
            return "400_INVALID_PASSWORD";
        return null;
    }

    public static string? ValidateRegister(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
            return "400_INVALID_USERNAME";
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6 || password.Length > 100)
            return "400_INVALID_PASSWORD";
        return null;
    }

    public static string? ValidateForgotPassword(string? username)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
            return "400_INVALID_USERNAME";
        return null;
    }

    public static string? ValidateResetPassword(string? username, string? password)
    {
        if (string.IsNullOrWhiteSpace(username) || username.Length > 50)
            return "400_INVALID_USERNAME";
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6 || password.Length > 100)
            return "400_INVALID_PASSWORD";
        return null;
    }

    public static string? ValidateFileName(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return "400_INVALID_FILENAME";
        if (fileName.Length > MaxFileNameLength)
            return "400_INVALID_FILENAME";
        if (fileName.Contains("..") || fileName.Contains("\\") || fileName.Contains('/'))
            return "400_INVALID_FILENAME";
        if (fileName.IndexOf('\0') >= 0)
            return "400_INVALID_FILENAME";

        return null;
    }

    public static string? ValidateRename(string? oldName, string? newName)
    {
        string? err = ValidateFileName(oldName);
        if (err != null) return err;

        err = ValidateFileName(newName);
        if (err != null) return err;

        if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase))
            return "400_SAME_NAME";

        return null;
    }

    public static string? ValidateSetSpeed(long? speedMBs)
    {
        if (speedMBs == null)
            return "400_INVALID_SPEED";
        if (speedMBs != 1 && speedMBs != 5 && speedMBs != 10)
            return "400_INVALID_SPEED";
        return null;
    }
}

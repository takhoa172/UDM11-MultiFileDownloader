using System;
using System.Collections.Generic;
using System.Text;

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
            case PacketCommand.DOWNLOAD_REQ:
                return ValidateFileName(packet.FileName);
            default:
                return "400_BAD_COMMAND";
        }
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
}

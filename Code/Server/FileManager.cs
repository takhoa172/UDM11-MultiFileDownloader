using Shared;

namespace Server;

public static class FileManager
{
    public static ProtocolPacket RenameFile(
        string storagePath,
        string? oldFileName,
        string? newFileName)
    {
        string? oldNameError = PacketValidator.ValidateFileName(oldFileName);

        if (oldNameError != null)
        {
            return CreateError(
                oldNameError,
                "Tên file cũ không hợp lệ.");
        }

        string? newNameError = PacketValidator.ValidateFileName(newFileName);

        if (newNameError != null)
        {
            return CreateError(
                newNameError,
                "Tên file mới không hợp lệ.");
        }

        if (!PacketValidator.HasSameFileExtension(oldFileName!, newFileName!))
        {
            return CreateError(
                "400_EXTENSION_CHANGE",
                "Khong duoc thay doi dinh dang file.");
        }

        string oldPath = Path.Combine(
            storagePath,
            oldFileName!);

        string newPath = Path.Combine(
            storagePath,
            newFileName!);

        try
        {
            if (!File.Exists(oldPath))
            {
                return CreateError(
                    "404_FILE_NOT_FOUND",
                    "Không tìm thấy file cần đổi tên.");
            }

            if (File.Exists(newPath))
            {
                return CreateError(
                    "409_FILE_EXISTS",
                    "Tên file mới đã tồn tại.");
            }

            File.Move(oldPath, newPath);

            return new ProtocolPacket
            {
                Command = PacketCommand.RENAME_FILE,
                Success = true,
                FileName = oldFileName,
                NewFileName = newFileName,
                Message = "Đổi tên file thành công."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return CreateError(
                "403_ACCESS_DENIED",
                "Không có quyền đổi tên file.");
        }
        catch (IOException)
        {
            return CreateError(
                "409_FILE_OPERATION_FAILED",
                "Không thể đổi tên file.");
        }
        catch (Exception ex)
        {
            FileErrorInfo error = FileErrorHandler.FromException(
                ex,
                oldFileName!,
                "FileManager");

            return CreateError(
                error.ErrorCode,
                error.Message);
        }
    }

    public static ProtocolPacket DeleteFile(
        string storagePath,
        string? fileName)
    {
        string? fileNameError =
            PacketValidator.ValidateFileName(fileName);

        if (fileNameError != null)
        {
            return CreateError(
                fileNameError,
                "Tên file không hợp lệ.");
        }

        string filePath = Path.Combine(
            storagePath,
            fileName!);

        try
        {
            if (!File.Exists(filePath))
            {
                return CreateError(
                    "404_FILE_NOT_FOUND",
                    "Không tìm thấy file cần xóa.");
            }

            File.Delete(filePath);

            return new ProtocolPacket
            {
                Command = PacketCommand.DELETE_FILE,
                Success = true,
                FileName = fileName,
                Message = "Xóa file thành công."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return CreateError(
                "403_ACCESS_DENIED",
                "Không có quyền xóa file.");
        }
        catch (IOException)
        {
            return CreateError(
                "409_FILE_OPERATION_FAILED",
                "Không thể xóa file.");
        }
        catch (Exception ex)
        {
            FileErrorInfo error = FileErrorHandler.FromException(
                ex,
                fileName!,
                "FileManager");

            return CreateError(
                error.ErrorCode,
                error.Message);
        }
    }

    private static ProtocolPacket CreateError(
        string errorCode,
        string message)
    {
        return new ProtocolPacket
        {
            Command = PacketCommand.ERROR_RESP,
            Success = false,
            ErrorCode = errorCode,
            Message = message
        };
    }
}

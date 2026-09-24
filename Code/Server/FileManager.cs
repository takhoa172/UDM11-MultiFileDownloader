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
                "Tên tệp cũ không hợp lệ.");
        }

        string? newNameError = PacketValidator.ValidateFileName(newFileName);

        if (newNameError != null)
        {
            return CreateError(
                newNameError,
                "Tên tệp mới không hợp lệ.");
        }

        if (!PacketValidator.HasSameFileExtension(oldFileName!, newFileName!))
        {
            return CreateError(
                "400_EXTENSION_CHANGE",
                "Không được thay đổi định dạng tệp.");
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
                    "Không tìm thấy tệp cần đổi tên.");
            }

            bool caseOnlyRename =
                string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(oldPath, newPath, StringComparison.Ordinal);

            if (!caseOnlyRename && File.Exists(newPath))
            {
                return CreateError(
                    "409_FILE_EXISTS",
                    "Tên tệp mới đã tồn tại.");
            }

            if (caseOnlyRename)
            {
                string tempPath =
                    oldPath + "." + Guid.NewGuid().ToString("N") + ".renaming";

                File.Move(oldPath, tempPath);
                File.Move(tempPath, newPath);
            }
            else
            {
                File.Move(oldPath, newPath);
            }

            return new ProtocolPacket
            {
                Command = PacketCommand.RENAME_FILE,
                Success = true,
                FileName = oldFileName,
                NewFileName = newFileName,
                Message = "Đổi tên tệp thành công."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return CreateError(
                "403_ACCESS_DENIED",
                "Không có quyền đổi tên tệp.");
        }
        catch (IOException)
        {
            return CreateError(
                "409_FILE_OPERATION_FAILED",
                "Không thể đổi tên tệp.");
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
                "Tên tệp không hợp lệ.");
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
                    "Không tìm thấy tệp cần xóa.");
            }

            File.Delete(filePath);

            return new ProtocolPacket
            {
                Command = PacketCommand.DELETE_FILE,
                Success = true,
                FileName = fileName,
                Message = "Xóa tệp thành công."
            };
        }
        catch (UnauthorizedAccessException)
        {
            return CreateError(
                "403_ACCESS_DENIED",
                "Không có quyền xóa tệp.");
        }
        catch (IOException)
        {
            return CreateError(
                "409_FILE_OPERATION_FAILED",
                "Không thể xóa tệp.");
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

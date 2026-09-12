using System.IO;

namespace Client;

public enum FileConflictMode
{
    AutoRename,
    Overwrite,
    Skip
}

public static class FileConflictManager
{
    public static string? ResolveTargetPath(
        string folderPath,
        string fileName,
        FileConflictMode mode)
    {
        Directory.CreateDirectory(folderPath);

        string targetPath = Path.Combine(folderPath, fileName);

        if (!File.Exists(targetPath))
            return targetPath;

        switch (mode)
        {
            case FileConflictMode.Overwrite:
                return targetPath;

            case FileConflictMode.Skip:
                return null;

            case FileConflictMode.AutoRename:
                return GetAutoRenamePath(folderPath, fileName);

            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }
    }

    private static string GetAutoRenamePath(
        string folderPath,
        string fileName)
    {
        string name = Path.GetFileNameWithoutExtension(fileName);
        string extension = Path.GetExtension(fileName);

        int number = 1;

        while (true)
        {
            string newFileName =
                $"{name}({number}){extension}";

            string newPath =
                Path.Combine(folderPath, newFileName);

            if (!File.Exists(newPath))
                return newPath;

            number++;
        }
    }
}
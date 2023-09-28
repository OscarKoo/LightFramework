namespace Dao.LightFramework.Common.Utilities;

public static class File2
{
    public static void CreateDirectoryIfNotExists(DirectoryInfo directory)
    {
        if (directory == null || directory.Exists)
            return;

        CreateDirectoryIfNotExists(directory.Parent);

        directory.Create();
    }

    public static void Move(string sourceFileName, string destFileName)
    {
        var newFile = new FileInfo(destFileName);
        if (newFile.Exists)
            newFile.Delete();
        else
            CreateDirectoryIfNotExists(newFile.Directory);
        File.Move(sourceFileName, destFileName);
    }
}
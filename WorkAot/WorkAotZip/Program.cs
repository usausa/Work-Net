using System.IO.Compression;

if ((args.Length == 1) && args[0].EndsWith(".zip"))
{
    ZipFile.ExtractToDirectory(args[0], Environment.GetFolderPath(Environment.SpecialFolder.Desktop));
}
else if (args.Length >= 2)
{
    var dir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    using var zipFile = new FileStream(Path.ChangeExtension(Path.Combine(dir, Path.GetFileNameWithoutExtension(args[0])), "zip"), FileMode.Create);
    using var archive = new ZipArchive(zipFile, ZipArchiveMode.Create);
    foreach (var path in args)
    {
        if (File.Exists(path))
        {
            archive.CreateEntryFromFile(path, Path.GetFileName(path));
        }
        else if (Directory.Exists(path))
        {
            AddDirectoryToZip(archive, path, Path.GetFileName(path));
        }
    }
}

static void AddDirectoryToZip(ZipArchive archive, string sourceDir, string entryName)
{
    var dir = new DirectoryInfo(sourceDir);

    foreach (var file in dir.GetFiles())
    {
        var entryPath = Path.Combine(entryName, file.Name);
        archive.CreateEntryFromFile(file.FullName, entryPath);
    }

    foreach (var subDir in dir.GetDirectories())
    {
        var subDirEntryName = Path.Combine(entryName, subDir.Name);
        AddDirectoryToZip(archive, subDir.FullName, subDirEntryName);
    }
}

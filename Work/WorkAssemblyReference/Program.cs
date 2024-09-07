using System.Diagnostics;
using System.Reflection;

var file = new FileInfo(args[0]);
Process(file, [], 0);

static void Process(FileInfo fi, string[] path, int depth)
{
    if (fi.Directory is null)
    {
        return;
    }

    var assembly = Assembly.LoadFile(fi.FullName);
    foreach (var assemblyName in assembly.GetReferencedAssemblies())
    {
        var fic = FindLocation(assemblyName, [.. path, fi.Directory.FullName]);
        if (fic is not null)
        {
            Process(fic, path, depth + 1);
        }
        else
        {
            Debug.WriteLine($"{new string(' ', depth)}{assemblyName.Name} {assemblyName.Version}");
        }
    }
}

static FileInfo? FindLocation(AssemblyName assemblyName, string[] path)
{
    foreach (var dir in path)
    {
        var di = new DirectoryInfo(dir);
        if (!di.Exists)
        {
            continue;
        }

        var fi = di.EnumerateFiles($"{assemblyName.Name}.dll").FirstOrDefault();
        if (fi != null)
        {
            return fi;
        }

        fi = di.EnumerateFiles($"{assemblyName.Name}.exe").FirstOrDefault();
        if (fi != null)
        {
            return fi;
        }
    }

    return null;
}

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

// TODO Not working
var target = Path.GetFullPath(args[0]);
var file = new FileInfo(target);
var targetAssembly = Assembly.LoadFile(file.FullName);
var context = AssemblyLoadContext.GetLoadContext(targetAssembly)!;

var references = new Dictionary<string, AssemblyName>();
Process(references, file, [], 0);

context.Resolving += (ctx, name) =>
{
    if (references.TryGetValue(name.Name!, out var reference))
    {
        Console.WriteLine($"Resolve OK: {name.Name}");
        return Assembly.Load(reference);
    }

    Console.WriteLine($"Resolve NG: {name.Name}");
    return null;
};

// Main
var assembly = context.LoadFromAssemblyPath(target);
Debug.WriteLine("----");
foreach (var type in assembly.GetExportedTypes())
{
    Debug.WriteLine($"Target: {type}");
}
Debug.WriteLine("----");

static void Process(Dictionary<string, AssemblyName> references, FileInfo fi, string[] path, int depth)
{
    if (fi.Directory is null)
    {
        return;
    }

    var assembly = Assembly.LoadFile(fi.FullName);
    foreach (var assemblyName in assembly.GetReferencedAssemblies())
    {
        Debug.WriteLine($"{new string(' ', depth)}{assemblyName.Name} {assemblyName.Version}");
        references[assemblyName.Name!] = assemblyName;

        var fic = FindLocation(assemblyName, [.. path, fi.Directory.FullName]);
        if (fic is not null)
        {
            Process(references, fic, path, depth + 1);
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

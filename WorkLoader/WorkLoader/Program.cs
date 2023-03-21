namespace WorkLoader;

using System.Reflection;
using System.Runtime.Loader;

internal class Program
{
    static void Main()
    {
        var referenceFile = @"..\..\..\..\WorkLoader.TargetApp\obj\Debug\net7.0-windows\Reference.txt";
        var targetFile = @"..\..\..\..\WorkLoader.TargetApp\bin\Debug\net7.0-windows\WorkLoader.TargetApp.dll";

        var references = File.ReadAllLines(Path.GetFullPath(referenceFile))
            .Select(x => new Reference(x))
            .ToDictionary(x => x.Name);

        // TODO Path

        var target = Path.GetFullPath(targetFile);
        var targetAssembly = Assembly.LoadFile(target);
        var context = AssemblyLoadContext.GetLoadContext(targetAssembly);
        context!.Resolving += (_, name) =>
        {
            if (references.TryGetValue(name.Name!, out var reference))
            {
                return context.LoadFromAssemblyPath(reference.FilePath);
            }

            return null;
        };

        var assembly = context.LoadFromAssemblyPath(target);
        foreach (var type in assembly.GetExportedTypes())
        {
            System.Diagnostics.Debug.WriteLine(type);
        }
    }
}

public class Reference
{
    public string Name { get; }

    public string FilePath { get; }

    public Reference(string filePath)
    {
        Name = Path.GetFileNameWithoutExtension(filePath);
        FilePath = filePath;
    }
}

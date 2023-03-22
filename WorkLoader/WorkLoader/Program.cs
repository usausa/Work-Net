namespace WorkLoader;

using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

internal class Program
{
    static void Main()
    {
        var referenceFile = @"..\..\..\..\WorkLoader.TargetApp\obj\Debug\net7.0-windows\Reference.txt";
        var targetFile = @"..\..\..\..\WorkLoader.TargetApp\bin\Debug\net7.0-windows\WorkLoader.TargetApp.dll";
        //var referenceFile = @"..\..\..\..\WorkLoader.TargetLibrary\obj\Debug\net7.0\Reference.txt";
        //var targetFile = @"..\..\..\..\WorkLoader.TargetLibrary\bin\Debug\net7.0\WorkLoader.TargetLibrary.dll";

        var references = File.ReadAllLines(Path.GetFullPath(referenceFile))
            .Select(x => new { Name = Path.GetFileNameWithoutExtension(x), FilePath = x })
            .ToDictionary(x => x.Name);

        var target = Path.GetFullPath(targetFile);
        var targetAssembly = Assembly.LoadFile(target);
        var context = AssemblyLoadContext.GetLoadContext(targetAssembly);
        context!.Resolving += (_, name) =>
        {
            if (references.TryGetValue(name.Name!, out var reference))
            {
                Debug.WriteLine($@"* Find reference: {name.FullName} {reference.FilePath}");

                try
                {
                    return context.LoadFromAssemblyPath(reference.FilePath);
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e);
                    throw;
                }
            }

            Debug.WriteLine($@"* Failed: {name.FullName}");

            return null;
        };

        var assembly = context.LoadFromAssemblyPath(target);
        foreach (var type in assembly.ExportedTypes)
        {
            Debug.WriteLine($"* Type: {type}");
        }
    }
}

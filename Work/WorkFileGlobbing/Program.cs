using System.Diagnostics;
using Microsoft.Extensions.FileSystemGlobbing;
using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

var matcher = new Matcher();
//matcher.AddInclude("*.dll");
matcher.AddInclude("**/*");
matcher.AddExclude("**/*.pdb");

var result = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(".")));
foreach (var file in result.Files)
{
    Debug.WriteLine(file.Path);
}

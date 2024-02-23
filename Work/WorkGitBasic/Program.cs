// https://github.com/libgit2/libgit2sharp/wiki

using System.Diagnostics;

using LibGit2Sharp;

var repo = new Repository("D:\\GitHub\\Smart-Net");

foreach (var branch in repo.Branches)
{
    Debug.WriteLine("----------");
    Debug.WriteLine(branch.FriendlyName);
    Debug.WriteLine(branch.CanonicalName);
    Debug.WriteLine($"IsRemote: {branch.IsRemote}");
    Debug.WriteLine($"IsTracking: {branch.IsTracking}");
}

// log
Debug.WriteLine("=========");
foreach (var c in repo.Commits.Take(15))
{
    Debug.WriteLine($"commit {c.Id}");
    if (c.Parents.Count() > 1)
    {
        Debug.WriteLine("Merge: {0}", String.Join(" ", c.Parents.Select(p => p.Id.Sha[..7]).ToArray()));
    }

    Debug.WriteLine($"Author: {c.Author.Name} <{c.Author.Email}>");
    Debug.WriteLine($"Date: {c.Author.When:yyyy/MM/dd HH:mm:ss}");
    Debug.WriteLine(c.Message);
    Debug.WriteLine("----------");
}

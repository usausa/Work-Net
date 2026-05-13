namespace WorkSkiaGitMaui.Data;

using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public static class PseudoRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static async Task<(IReadOnlyList<PseudoCommit> Commits, IReadOnlyList<PseudoRef> Refs)> LoadAsync()
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync("repository.json").ConfigureAwait(false);
        var data = await JsonSerializer.DeserializeAsync<RepositoryData>(stream, JsonOptions).ConfigureAwait(false);
        return (data!.Commits, data.Refs);
    }

    private sealed class RepositoryData
    {
        public List<PseudoCommit> Commits { get; set; } = [];
        public List<PseudoRef> Refs { get; set; } = [];
    }
}


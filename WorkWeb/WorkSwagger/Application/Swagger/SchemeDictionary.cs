namespace WorkSwagger.Application.Swagger;

public static class SchemeDictionary
{
    private static readonly Dictionary<string, SchemeEntry> Entries = new(StringComparer.OrdinalIgnoreCase)
    {
        { "ItemCode", new("アイテムコード") { Example = "12345" } },
        { "Description", new("詳細") { Example = "詳細情報" } }
    };

    public static SchemeEntry? Lookup(string name) => Entries.GetValueOrDefault(name);
}

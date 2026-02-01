namespace MyPgsql.Binary;

/// <summary>
/// PostgreSQL接続文字列ビルダー
/// </summary>
public sealed class PgConnectionStringBuilder
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = "";
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";

    public PgConnectionStringBuilder() { }

    public PgConnectionStringBuilder(string connectionString)
    {
        Parse(connectionString);
    }

    private void Parse(string connectionString)
    {
        foreach (var part in connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var idx = part.IndexOf('=');
            if (idx <= 0) continue;

            var key = part[..idx].Trim().ToLowerInvariant();
            var value = part[(idx + 1)..].Trim();

            switch (key)
            {
                case "host" or "server":
                    Host = value;
                    break;
                case "port":
                    Port = int.Parse(value);
                    break;
                case "database" or "db":
                    Database = value;
                    break;
                case "username" or "user" or "uid":
                    Username = value;
                    break;
                case "password" or "pwd":
                    Password = value;
                    break;
            }
        }
    }

    public override string ToString()
    {
        return $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
    }
}

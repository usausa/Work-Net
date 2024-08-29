namespace WorkAnalyze;

using Microsoft.Data.SqlClient;

using System.Diagnostics;

public static class Program
{
    public static void Main()
    {
        using var con = new SqlConnection("Data Source=db-server;Initial Catalog=Test;User ID=test;Password=test;TrustServerCertificate=True;");
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = "SET STATISTICS XML ON; SELECT * FROM Data WHERE Id = 1; SET STATISTICS XML OFF;";

        var reader = cmd.ExecuteReader();
        do
        {
            while (reader.Read())
            {
                if (reader.FieldCount > 0)
                {
                    Debug.WriteLine(reader.GetName(0));
                    Debug.WriteLine(reader.GetString(0));
                }
            }
        }
        while (reader.NextResult());
    }
}

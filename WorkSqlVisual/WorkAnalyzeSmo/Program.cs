namespace WorkAnalyzeSmo;

using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Management.Common;
using Microsoft.SqlServer.Management.Smo;

internal class Program
{
    public static void Main()
    {
        var connectionString = "Data Source=db-server;Initial Catalog=Test;User ID=test;Password=test;TrustServerCertificate=True;";
        var connection = new ServerConnection(new SqlConnection(connectionString));
        var server = new Server(connection);

        var query = "SELECT * FROM Data WHERE Id = 1";
        var executionPlan = server.ConnectionContext.ExecuteWithResults("SET SHOWPLAN_XML ON; " + query + "; SET SHOWPLAN_XML OFF;");

        foreach (var row in executionPlan.Tables[0].Rows)
        {
            Console.WriteLine(row);
        }
    }
}

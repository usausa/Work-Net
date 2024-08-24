using System.Data.Common;

using Microsoft.Data.SqlClient;

DbProviderFactories.RegisterFactory("Microsoft.Data.SqlClient", SqlClientFactory.Instance);

var factory = DbProviderFactories.GetFactory("Microsoft.Data.SqlClient");
using var con = factory.CreateConnection();


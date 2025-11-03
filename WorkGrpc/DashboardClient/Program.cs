using DashboardContract;
using Grpc.Net.Client;

using ProtoBuf.Grpc.Client;

var channel = GrpcChannel.ForAddress("http://localhost:5001");

var client = channel.CreateGrpcService<IDataApi>();

var input = Console.ReadLine();
 await client.SendDataAsync(new DataRequest { Value = Int32.TryParse(input, out var value) ? value : 0 }).ConfigureAwait(false);

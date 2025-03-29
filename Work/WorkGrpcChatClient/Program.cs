using Grpc.Net.Client;

using ProtoBuf.Grpc.Client;

using WorkGrpcChatContract;

var channel = GrpcChannel.ForAddress("http://localhost:5000");

var client = channel.CreateGrpcService<IHelloService>();
var reply = await client.HelloAsync(new HelloRequest { Name = "うさうさ" }).ConfigureAwait(false);
Console.WriteLine(reply.Message);

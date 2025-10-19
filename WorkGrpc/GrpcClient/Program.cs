using Grpc.Net.Client;

using GrpcService1;

var channel = GrpcChannel.ForAddress("http://localhost:5000", new GrpcChannelOptions
{
    //Credentials = Grpc.Core.ChannelCredentials.Insecure
});
var client = new Greeter.GreeterClient(channel);

var reply = await client.SayHelloAsync(new HelloRequest { Name = "Hello" });

Console.WriteLine(reply.Message);
Console.ReadLine();

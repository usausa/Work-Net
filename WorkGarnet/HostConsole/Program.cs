using Garnet;

using var server = new GarnetServer(args);
server.Start();

Thread.Sleep(Timeout.Infinite);

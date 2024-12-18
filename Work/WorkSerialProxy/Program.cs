using System.Buffers;
using System.Diagnostics;
using System.IO.Ports;
// ReSharper disable AccessToDisposedClosure

using var port1 = new SerialPort("COM12");
port1.DtrEnable = true;
port1.RtsEnable = true;
port1.ReadTimeout = 1000;
port1.WriteTimeout = 1000;
port1.BaudRate = 115200;
port1.StopBits = StopBits.One;
port1.Parity = Parity.None;

using var port2 = new SerialPort("COM4");
port2.DtrEnable = true;
port2.RtsEnable = true;
port2.ReadTimeout = 1000;
port2.WriteTimeout = 1000;
port2.BaudRate = 115200;
port2.StopBits = StopBits.One;
port2.Parity = Parity.None;

port1.DataReceived += (_, _) =>
{
    var length = port1.BytesToRead;
    var buffer = ArrayPool<byte>.Shared.Rent(length);
    var read = port1.Read(buffer, 0, length);
    Debug.WriteLine("ReadFrom:COM12 " + read);
    port2.Write(buffer, 0, read);
    ArrayPool<byte>.Shared.Return(buffer);
};
port2.DataReceived += (_, _) =>
{
    var length = port2.BytesToRead;
    var buffer = ArrayPool<byte>.Shared.Rent(length);
    var read = port2.Read(buffer, 0, length);
    Debug.WriteLine("ReadFrom:COM4 " + read);
    port1.Write(buffer, 0, read);
    ArrayPool<byte>.Shared.Return(buffer);
};

port1.Open();
port2.Open();

Console.ReadLine();

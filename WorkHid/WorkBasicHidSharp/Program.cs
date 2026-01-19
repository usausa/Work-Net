using HidSharp;

// HORI Gamepad
const int vid = 0x0F0D;
const int pid = 0x006D;

Console.Clear();

// HIDデバイスを列挙してVID/PID一致を探す
var list = DeviceList.Local;
var device = list.GetHidDevices(vid, pid).FirstOrDefault();
if (device == null)
{
    Console.WriteLine("デバイスが見つかりません。VID/PIDを確認してください。");
    return;
}

Console.WriteLine($"Device: {device.GetProductName()} / / {device.DevicePath}");

var maxInput = device.GetMaxInputReportLength();
var maxOutput = device.GetMaxOutputReportLength();
Console.WriteLine($"MaxInputReportLength: {maxInput}");
Console.WriteLine($"MaxOutputReportLength: {maxOutput}");

if (!device.TryOpen(out HidStream stream))
{
    Console.WriteLine("オープンに失敗しました(他プロセスが掴んでいる/権限など)。");
    return;
}


using (stream)
{
    // タイムアウト設定(無限待ちしたいなら InfiniteTimeout)
    stream.ReadTimeout = Timeout.Infinite;

    Console.WriteLine("読み取り開始（Ctrl+Cで終了）");

    var stop = false;
    Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop = true; };

    // HIDは先頭1バイトがReport IDの場合あり（0のこともある）
    var buffer = new byte[maxInput];
    while (!stop)
    {
        int read;
        try
        {
            read = stream.Read(buffer, 0, buffer.Length);
        }
        catch (TimeoutException)
        {
            continue;
        }

        if (read > 0)
        {
            Console.SetCursorPosition(0, 4);
            Console.WriteLine(BitConverter.ToString(buffer.Take(read).ToArray()));
        }
    }
}

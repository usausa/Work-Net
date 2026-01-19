using HidSharp;

// HORI Gamepad
Console.Clear();

var devices = DeviceList.Local.GetHidDevices()
    .Where(d =>
    {
        try
        {
            var desc = d.GetReportDescriptor();
            foreach (var item in desc.DeviceItems)
            {
                foreach (uint usage in item.Usages.GetAllValues())
                {
                    // Generic Desktop (0x01) の Joystick(0x04) / GamePad(0x05)
                    var page = (ushort)(usage >> 16);
                    var id = (ushort)(usage & 0xFFFF);
                    if (page == 0x01 && (id == 0x04 || id == 0x05))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Ignore
        }
        return false;
    })
    .ToList();

if (devices.Count == 0)
{
    Console.WriteLine("デバイスが見つかりません。VID/PIDを確認してください。");
    return;
}

Console.WriteLine("候補一覧:");
for (var i = 0; i < devices.Count; i++)
{
    var d = devices[i];
    Console.WriteLine($"[{i}] VID={d.VendorID:X4} PID={d.ProductID:X4} {d.GetProductName()}");
}

Console.Write("開く番号を入力: ");
if (!Int32.TryParse(Console.ReadLine(), out var index) || index < 0 || index >= devices.Count)
{
    Console.WriteLine("不正な入力です。");
    return;
}

var device = devices[index];

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

    var (_, top) = Console.GetCursorPosition();

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
            Console.SetCursorPosition(0, top);
            Console.WriteLine(BitConverter.ToString(buffer.Take(read).ToArray()));
        }
    }
}

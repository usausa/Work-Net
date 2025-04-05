namespace WorkMidi;

using Windows.Devices.Enumeration;
using Windows.Devices.Midi;

internal class Program
{
    static async Task Main()
    {
        using var midiInput = new ModernMidiInput();

        // 利用可能なMIDIデバイスを取得
        Console.WriteLine("利用可能なMIDIデバイスを検索中...");
        var devices = await midiInput.GetAvailableMidiInputDevicesAsync();

        if (devices.Count == 0)
        {
            Console.WriteLine("MIDIデバイスが見つかりませんでした。");
            return;
        }

        // 最初に見つかったデバイスを使用
        var selectedDevice = devices.First(x => x.Name.Contains("nano"));
        Console.WriteLine($"選択されたデバイス: {selectedDevice.Name}");

        // デバイスの初期化
        await midiInput.InitializeDeviceAsync(selectedDevice.Id);

        Console.WriteLine("MIDI入力の監視を開始しました。終了するには何かキーを押してください...");
        Console.ReadKey();
    }
}

public class ModernMidiInput : IDisposable
{
    private MidiInPort? midiInPort;
    private List<DeviceInformation> availableDevices = default!;

    public async Task<List<DeviceInformation>> GetAvailableMidiInputDevicesAsync()
    {
        // MIDIデバイスのセレクター文字列
        var midiInputSelector = MidiInPort.GetDeviceSelector();

        // 利用可能なMIDIデバイスを検索
        var devices = await DeviceInformation.FindAllAsync(midiInputSelector);
        availableDevices = new List<DeviceInformation>();

        foreach (var device in devices)
        {
            availableDevices.Add(device);
            Console.WriteLine($"デバイスID: {device.Id}");
            Console.WriteLine($"デバイス名: {device.Name}");
            Console.WriteLine($"接続状態: {(device.IsEnabled ? "有効" : "無効")}");
            Console.WriteLine("-------------------");
        }

        return availableDevices;
    }

    public async Task InitializeDeviceAsync(string deviceId)
    {
        try
        {
            // 指定されたデバイスIDでMIDIポートを開く
            midiInPort = await MidiInPort.FromIdAsync(deviceId);

            if (midiInPort == null)
            {
                Console.WriteLine("MIDIデバイスのオープンに失敗しました。アプリケーションの権限を確認してください。");
                return;
            }

            // MIDIメッセージ受信時のイベントハンドラーを設定
            midiInPort.MessageReceived += MidiInPort_MessageReceived;

            Console.WriteLine($"MIDIデバイス '{deviceId}' の初期化が完了しました。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MIDIデバイスの初期化中にエラーが発生しました: {ex.Message}");
        }
    }

    private void MidiInPort_MessageReceived(MidiInPort sender, MidiMessageReceivedEventArgs args)
    {
        var message = args.Message;

        switch (message.Type)
        {
            case MidiMessageType.NoteOn:
                var noteOnMessage = (MidiNoteOnMessage)message;
                Console.WriteLine($"ノートオン - チャンネル: {noteOnMessage.Channel + 1}, " +
                                $"ノート番号: {noteOnMessage.Note}, " +
                                $"ベロシティ: {noteOnMessage.Velocity}");
                break;

            case MidiMessageType.NoteOff:
                var noteOffMessage = (MidiNoteOffMessage)message;
                Console.WriteLine($"ノートオフ - チャンネル: {noteOffMessage.Channel + 1}, " +
                                $"ノート番号: {noteOffMessage.Note}, " +
                                $"ベロシティ: {noteOffMessage.Velocity}");
                break;

            case MidiMessageType.ControlChange:
                var ccMessage = (MidiControlChangeMessage)message;
                Console.WriteLine($"コントロールチェンジ - チャンネル: {ccMessage.Channel + 1}, " +
                                $"コントローラー: {ccMessage.Controller}, " +
                                $"値: {ccMessage.ControlValue}");
                break;

            case MidiMessageType.ProgramChange:
                var pcMessage = (MidiProgramChangeMessage)message;
                Console.WriteLine($"プログラムチェンジ - チャンネル: {pcMessage.Channel + 1}, " +
                                $"プログラム: {pcMessage.Program}");
                break;

            case MidiMessageType.PitchBendChange:
                var pbMessage = (MidiPitchBendChangeMessage)message;
                Console.WriteLine($"ピッチベンド - チャンネル: {pbMessage.Channel + 1}, " +
                                $"値: {pbMessage.Bend}");
                break;
        }
    }

    public void Dispose()
    {
        if (midiInPort != null)
        {
            midiInPort.MessageReceived -= MidiInPort_MessageReceived;
            midiInPort.Dispose();
            midiInPort = null;
        }
    }
}

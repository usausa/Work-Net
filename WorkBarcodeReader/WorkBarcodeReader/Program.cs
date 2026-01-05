using System.Runtime.InteropServices;
using System.Text;

namespace WorkBarcodeReader
{
    // P/Invoke宣言を分離
    internal static unsafe class NativeMethods
    {
        // Window関連
        public const int WS_OVERLAPPEDWINDOW = 0x00CF0000;
        public const int SW_HIDE = 0;
        public const int WM_DESTROY = 0x0002;
        public const int WM_CLOSE = 0x0010;
        public const int WM_INPUT = 0x00FF;
        public const int CS_HREDRAW = 0x0002;
        public const int CS_VREDRAW = 0x0001;
        public const int CW_USEDEFAULT = unchecked((int)0x80000000);
        public const int COLOR_WINDOW = 5;
        public const int IDC_ARROW = 32512;

        // Raw Input関連
        public const int RIDEV_INPUTSINK = 0x00000100;
        public const int RIDEV_REMOVE = 0x00000001;
        public const int RID_INPUT = 0x10000003;
        public const int RID_HEADER = 0x10000005;

        public const int RIM_TYPEMOUSE = 0;
        public const int RIM_TYPEKEYBOARD = 1;
        public const int RIM_TYPEHID = 2;

        public const int RIDI_DEVICENAME = 0x20000007;
        public const int RIDI_DEVICEINFO = 0x2000000b;

        public const int RI_KEY_MAKE = 0;
        public const int RI_KEY_BREAK = 1;
        public const int RI_KEY_E0 = 2;
        public const int RI_KEY_E1 = 4;

        // Virtual Key Codes
        public const int VK_RETURN = 0x0D;
        public const int VK_SHIFT = 0x10;
        public const int VK_CONTROL = 0x11;
        public const int VK_MENU = 0x12; // ALT

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public char* lpszMenuName;
            public char* lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICELIST
        {
            public IntPtr hDevice;
            public uint dwType;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTDEVICE
        {
            public ushort usUsagePage;
            public ushort usUsage;
            public uint dwFlags;
            public IntPtr hwndTarget;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUTHEADER
        {
            public uint dwType;
            public uint dwSize;
            public IntPtr hDevice;
            public IntPtr wParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWKEYBOARD
        {
            public ushort MakeCode;
            public ushort Flags;
            public ushort Reserved;
            public ushort VKey;
            public uint Message;
            public uint ExtraInformation;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RAWINPUT
        {
            public RAWINPUTHEADER header;
            public RAWKEYBOARD keyboard;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO_KEYBOARD
        {
            public uint dwType;
            public uint dwSubType;
            public uint dwKeyboardMode;
            public uint dwNumberOfFunctionKeys;
            public uint dwNumberOfIndicators;
            public uint dwNumberOfKeysTotal;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RID_DEVICE_INFO
        {
            public uint cbSize;
            public uint dwType;
            public RID_DEVICE_INFO_KEYBOARD keyboard;
        }

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Window API
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern ushort RegisterClassEx(ref WNDCLASSEX lpwcx);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(
            uint dwExStyle, char* lpClassName, char* lpWindowName, uint dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWndParent, IntPtr hMenu, IntPtr hInstance, IntPtr lpParam);

        [DllImport("user32.dll")]
        public static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

        [DllImport("user32.dll")]
        public static extern bool TranslateMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern IntPtr DispatchMessage(ref MSG lpMsg);

        [DllImport("user32.dll")]
        public static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        public static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool UnregisterClass(char* lpClassName, IntPtr hInstance);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        public static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        // Raw Input API
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputDeviceList(
            [Out] RAWINPUTDEVICELIST* pRawInputDeviceList,
            ref uint puiNumDevices,
            uint cbSize);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern uint GetRawInputDeviceInfo(
            IntPtr hDevice,
            uint uiCommand,
            IntPtr pData,
            ref uint pcbSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool RegisterRawInputDevices(
            RAWINPUTDEVICE* pRawInputDevices,
            uint uiNumDevices,
            uint cbSize);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetRawInputData(
            IntPtr hRawInput,
            uint uiCommand,
            IntPtr pData,
            ref uint pcbSize,
            uint cbSizeHeader);

        [DllImport("user32.dll")]
        public static extern short GetKeyState(int nVirtKey);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int ToUnicode(
            uint wVirtKey,
            uint wScanCode,
            byte* lpKeyState,
            char* pwszBuff,
            int cchBuff,
            uint wFlags);
    }

    // キーボードデバイス情報
    public class KeyboardDeviceInfo
    {
        public IntPtr DeviceHandle { get; set; }
        public string DeviceName { get; set; }
        public string FriendlyName { get; set; }
        public uint DeviceType { get; set; }
        public uint SubType { get; set; }

        public override string ToString()
        {
            return $"{FriendlyName} ({DeviceName})";
        }
    }

    // バーコード読み取りイベント引数
    public class BarcodeReadEventArgs : EventArgs
    {
        public string Barcode { get; set; }
        public DateTime Timestamp { get; set; }
        public IntPtr DeviceHandle { get; set; }

        public BarcodeReadEventArgs(string barcode, IntPtr deviceHandle)
        {
            Barcode = barcode;
            DeviceHandle = deviceHandle;
            Timestamp = DateTime.Now;
        }
    }

    // バーコードリーダークラス
    public sealed unsafe class BarcodeReader : IDisposable
    {
        private EventWindow eventWindow;
        private IntPtr targetDeviceHandle;
        private string targetDeviceName;
        private StringBuilder currentBarcode;
        private bool isRunning;
        private bool disposed;
        private System.Threading.Thread messageLoopThread;

        public event EventHandler<BarcodeReadEventArgs> BarcodeRead;

        public bool IsRunning => isRunning;
        public string TargetDeviceName => targetDeviceName;

        // コンストラクタ：デバイスハンドルで指定
        public BarcodeReader(IntPtr deviceHandle)
        {
            targetDeviceHandle = deviceHandle;
            targetDeviceName = GetDeviceName(deviceHandle);
            currentBarcode = new StringBuilder();
            disposed = false;
            isRunning = false;
        }

        // コンストラクタ：デバイス名で指定
        public BarcodeReader(string deviceName)
        {
            var devices = GetKeyboardDevices();
            var device = devices.FirstOrDefault(d =>
                d.DeviceName.Equals(deviceName, StringComparison.OrdinalIgnoreCase) ||
                d.FriendlyName.Equals(deviceName, StringComparison.OrdinalIgnoreCase));

            if (device == null)
            {
                throw new ArgumentException($"Device not found: {deviceName}");
            }

            targetDeviceHandle = device.DeviceHandle;
            targetDeviceName = device.DeviceName;
            currentBarcode = new StringBuilder();
            disposed = false;
            isRunning = false;
        }

        // キーボードデバイス一覧を取得するヘルパーメソッド
        public static List<KeyboardDeviceInfo> GetKeyboardDevices()
        {
            var devices = new List<KeyboardDeviceInfo>();
            uint deviceCount = 0;

            // デバイス数を取得
            uint result = NativeMethods.GetRawInputDeviceList(null, ref deviceCount,
                (uint)sizeof(NativeMethods.RAWINPUTDEVICELIST));

            if (deviceCount == 0)
                return devices;

            // デバイスリストを取得
            var deviceList = stackalloc NativeMethods.RAWINPUTDEVICELIST[(int)deviceCount];
            result = NativeMethods.GetRawInputDeviceList(deviceList, ref deviceCount,
                (uint)sizeof(NativeMethods.RAWINPUTDEVICELIST));

            if (result == unchecked((uint)-1))
            {
                throw new InvalidOperationException($"GetRawInputDeviceList failed. Error: {Marshal.GetLastWin32Error()}");
            }

            // キーボードデバイスのみをフィルタ
            for (int i = 0; i < deviceCount; i++)
            {
                if (deviceList[i].dwType == NativeMethods.RIM_TYPEKEYBOARD)
                {
                    var deviceInfo = new KeyboardDeviceInfo
                    {
                        DeviceHandle = deviceList[i].hDevice,
                        DeviceType = deviceList[i].dwType,
                        DeviceName = GetDeviceName(deviceList[i].hDevice)
                    };

                    // デバイス情報を取得
                    uint size = (uint)sizeof(NativeMethods.RID_DEVICE_INFO);
                    var info = stackalloc NativeMethods.RID_DEVICE_INFO[1];
                    info->cbSize = size;

                    uint result2 = NativeMethods.GetRawInputDeviceInfo(
                        deviceList[i].hDevice,
                        NativeMethods.RIDI_DEVICEINFO,
                        (IntPtr)info,
                        ref size);

                    if (result2 != unchecked((uint)-1))
                    {
                        deviceInfo.SubType = info->keyboard.dwSubType;
                        deviceInfo.FriendlyName = ExtractFriendlyName(deviceInfo.DeviceName);
                    }

                    devices.Add(deviceInfo);
                }
            }

            return devices;
        }

        // デバイス名を取得
        private static string GetDeviceName(IntPtr deviceHandle)
        {
            uint size = 0;
            NativeMethods.GetRawInputDeviceInfo(deviceHandle, NativeMethods.RIDI_DEVICENAME, IntPtr.Zero, ref size);

            if (size == 0)
                return "Unknown";

            IntPtr buffer = Marshal.AllocHGlobal((int)size * 2);
            try
            {
                uint result = NativeMethods.GetRawInputDeviceInfo(deviceHandle, NativeMethods.RIDI_DEVICENAME, buffer, ref size);
                if (result == unchecked((uint)-1))
                    return "Unknown";

                return Marshal.PtrToStringUni(buffer);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // フレンドリ名を抽出
        private static string ExtractFriendlyName(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return "Unknown";

            // デバイス名から簡易的なフレンドリ名を生成
            var parts = deviceName.Split(new[] { '#', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1)
            {
                return parts[1];
            }
            return deviceName;
        }

        // 開始
        public void Start()
        {
            if (isRunning)
            {
                throw new InvalidOperationException("BarcodeReader is already running");
            }

            // メッセージループを別スレッドで実行
            messageLoopThread = new System.Threading.Thread(MessageLoopThreadProc)
            {
                IsBackground = true,
                Name = "BarcodeReader MessageLoop"
            };
            messageLoopThread.Start();

            isRunning = true;
            Console.WriteLine($"[BarcodeReader] Started for device: {targetDeviceName}");
        }

        // 停止
        public void Stop()
        {
            if (!isRunning)
                return;

            isRunning = false;

            if (eventWindow != null)
            {
                eventWindow.Quit();
            }

            if (messageLoopThread != null && messageLoopThread.IsAlive)
            {
                messageLoopThread.Join(1000);
            }

            Console.WriteLine("[BarcodeReader] Stopped");
        }

        // メッセージループスレッド
        private void MessageLoopThreadProc()
        {
            try
            {
                // イベントウィンドウを作成
                eventWindow = new EventWindow("BarcodeReaderWindow", visible: false);
                eventWindow.Initialize();

                // Raw Inputデバイスを登録
                RegisterRawInputDevice(eventWindow.Handle);

                // メッセージハンドラを設定
                eventWindow.MessageReceived += OnWindowMessage;

                // メッセージループ実行
                eventWindow.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BarcodeReader] Error in message loop: {ex.Message}");
            }
            finally
            {
                if (eventWindow != null)
                {
                    UnregisterRawInputDevice(eventWindow.Handle);
                    eventWindow.Dispose();
                    eventWindow = null;
                }
            }
        }

        // Raw Inputデバイスを登録
        private void RegisterRawInputDevice(IntPtr hwnd)
        {
            var rid = stackalloc NativeMethods.RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01; // Generic Desktop Controls
            rid[0].usUsage = 0x06;     // Keyboard
            rid[0].dwFlags = NativeMethods.RIDEV_INPUTSINK;
            rid[0].hwndTarget = hwnd;

            if (!NativeMethods.RegisterRawInputDevices(rid, 1, (uint)sizeof(NativeMethods.RAWINPUTDEVICE)))
            {
                throw new InvalidOperationException($"RegisterRawInputDevices failed. Error: {Marshal.GetLastWin32Error()}");
            }

            Console.WriteLine("[BarcodeReader] Raw Input device registered");
        }

        // Raw Inputデバイスを解除
        private void UnregisterRawInputDevice(IntPtr hwnd)
        {
            var rid = stackalloc NativeMethods.RAWINPUTDEVICE[1];
            rid[0].usUsagePage = 0x01;
            rid[0].usUsage = 0x06;
            rid[0].dwFlags = NativeMethods.RIDEV_REMOVE;
            rid[0].hwndTarget = IntPtr.Zero;

            NativeMethods.RegisterRawInputDevices(rid, 1, (uint)sizeof(NativeMethods.RAWINPUTDEVICE));
            Console.WriteLine("[BarcodeReader] Raw Input device unregistered");
        }

        // ウィンドウメッセージハンドラ
        private IntPtr OnWindowMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == NativeMethods.WM_INPUT)
            {
                ProcessRawInput(lParam);
                return IntPtr.Zero;
            }

            return NativeMethods.DefWindowProc(hwnd, msg, wParam, lParam);
        }

        // Raw Input処理
        private void ProcessRawInput(IntPtr lParam)
        {
            uint size = 0;
            NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, IntPtr.Zero, ref size,
                (uint)sizeof(NativeMethods.RAWINPUTHEADER));

            if (size == 0)
                return;

            IntPtr buffer = Marshal.AllocHGlobal((int)size);
            try
            {
                uint result = NativeMethods.GetRawInputData(lParam, NativeMethods.RID_INPUT, buffer, ref size,
                    (uint)sizeof(NativeMethods.RAWINPUTHEADER));

                if (result == unchecked((uint)-1))
                    return;

                var raw = (NativeMethods.RAWINPUT*)buffer;

                // キーボード入力のみ処理
                if (raw->header.dwType != NativeMethods.RIM_TYPEKEYBOARD)
                    return;

                // 対象デバイスからの入力のみ処理
                if (targetDeviceHandle != IntPtr.Zero && raw->header.hDevice != targetDeviceHandle)
                    return;

                // キーが押された時のみ処理（離された時は無視）
                if ((raw->keyboard.Flags & NativeMethods.RI_KEY_BREAK) != 0)
                    return;

                ProcessKeyPress(raw->keyboard.VKey, raw->header.hDevice);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        // キー入力処理
        private void ProcessKeyPress(ushort vKey, IntPtr deviceHandle)
        {
            // Enterキーでバーコード確定
            if (vKey == NativeMethods.VK_RETURN)
            {
                if (currentBarcode.Length > 0)
                {
                    string barcode = currentBarcode.ToString();
                    currentBarcode.Clear();

                    Console.WriteLine($"[BarcodeReader] Barcode read: {barcode}");

                    // イベント発行
                    BarcodeRead?.Invoke(this, new BarcodeReadEventArgs(barcode, deviceHandle));
                }
                return;
            }

            // 修飾キーは無視
            if (vKey == NativeMethods.VK_SHIFT || vKey == NativeMethods.VK_CONTROL || vKey == NativeMethods.VK_MENU)
                return;

            // 仮想キーを文字に変換
            char ch = VirtualKeyToChar(vKey);
            if (ch != '\0')
            {
                currentBarcode.Append(ch);
                Console.Write(ch); // デバッグ用
            }
        }

        // 仮想キーを文字に変換
        private char VirtualKeyToChar(ushort vKey)
        {
            var keyState = stackalloc byte[256];

            // 現在のキーボード状態を取得
            for (int i = 0; i < 256; i++)
            {
                keyState[i] = (byte)((NativeMethods.GetKeyState(i) & 0x80) != 0 ? 0x80 : 0);
            }

            var buffer = stackalloc char[2];
            int result = NativeMethods.ToUnicode(vKey, 0, keyState, buffer, 2, 0);

            if (result > 0)
            {
                return buffer[0];
            }

            return '\0';
        }

        // Dispose実装
        public void Dispose()
        {
            if (!disposed)
            {
                Stop();
                disposed = true;
            }
        }
    }

    // EventWindowクラスの拡張（メッセージハンドラを外部から設定可能に）
    public sealed unsafe class EventWindow : IDisposable
    {
        private IntPtr handle;
        private IntPtr hInstance;
        private string className;
        private string windowName;
        private NativeMethods.WndProc wndProcDelegate;
        private ushort classAtom;
        private bool disposed;
        private bool isVisible;
        private bool isInitialized;

        public IntPtr Handle => handle;
        public bool IsVisible => isVisible;
        public bool IsInitialized => isInitialized;

        // メッセージハンドラデリゲート
        public delegate IntPtr MessageHandler(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);
        public event MessageHandler MessageReceived;

        public EventWindow(string name, bool visible = false)
        {
            windowName = name;
            className = $"EventWindow_{name}_{Guid.NewGuid():N}";
            isVisible = visible;
            hInstance = NativeMethods.GetModuleHandle(null);
            disposed = false;
            isInitialized = false;
            handle = IntPtr.Zero;
            classAtom = 0;
        }

        public void Initialize()
        {
            if (isInitialized)
            {
                throw new InvalidOperationException("Window is already initialized");
            }

            wndProcDelegate = WndProc;

            fixed (char* classNamePtr = className)
            {
                NativeMethods.WNDCLASSEX wndClass = new NativeMethods.WNDCLASSEX
                {
                    cbSize = (uint)sizeof(NativeMethods.WNDCLASSEX),
                    style = NativeMethods.CS_HREDRAW | NativeMethods.CS_VREDRAW,
                    lpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProcDelegate),
                    cbClsExtra = 0,
                    cbWndExtra = 0,
                    hInstance = hInstance,
                    hIcon = IntPtr.Zero,
                    hCursor = NativeMethods.LoadCursor(IntPtr.Zero, NativeMethods.IDC_ARROW),
                    hbrBackground = (IntPtr)(NativeMethods.COLOR_WINDOW + 1),
                    lpszMenuName = null,
                    lpszClassName = classNamePtr,
                    hIconSm = IntPtr.Zero
                };

                classAtom = NativeMethods.RegisterClassEx(ref wndClass);
                if (classAtom == 0)
                {
                    throw new InvalidOperationException($"RegisterClassEx failed. Error: {Marshal.GetLastWin32Error()}");
                }
            }

            fixed (char* classNamePtr = className)
            fixed (char* windowNamePtr = windowName)
            {
                uint style = NativeMethods.WS_OVERLAPPEDWINDOW;

                handle = NativeMethods.CreateWindowEx(
                    0, classNamePtr, windowNamePtr, style,
                    NativeMethods.CW_USEDEFAULT, NativeMethods.CW_USEDEFAULT,
                    800, 600,
                    IntPtr.Zero, IntPtr.Zero, hInstance, IntPtr.Zero);

                if (handle == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    fixed (char* classNamePtr2 = className)
                    {
                        NativeMethods.UnregisterClass(classNamePtr2, hInstance);
                    }
                    classAtom = 0;
                    throw new InvalidOperationException($"CreateWindowEx failed. Error: {error}");
                }
            }

            NativeMethods.UpdateWindow(handle);
            NativeMethods.ShowWindow(handle, isVisible ? 5 : NativeMethods.SW_HIDE);

            isInitialized = true;
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            // 外部ハンドラがあれば優先
            if (MessageReceived != null)
            {
                var result = MessageReceived(hWnd, msg, wParam, lParam);
                if (msg != NativeMethods.WM_CLOSE && msg != NativeMethods.WM_DESTROY)
                {
                    return result;
                }
            }

            switch (msg)
            {
                case NativeMethods.WM_CLOSE:
                    NativeMethods.DestroyWindow(hWnd);
                    return IntPtr.Zero;

                case NativeMethods.WM_DESTROY:
                    NativeMethods.PostQuitMessage(0);
                    return IntPtr.Zero;

                default:
                    return NativeMethods.DefWindowProc(hWnd, msg, wParam, lParam);
            }
        }

        public void SetVisible(bool visible)
        {
            if (!isInitialized)
                throw new InvalidOperationException("Window not initialized. Call Initialize() first.");

            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("Window not created");

            isVisible = visible;
            NativeMethods.ShowWindow(handle, visible ? 5 : NativeMethods.SW_HIDE);
        }

        public void Run()
        {
            if (!isInitialized)
                throw new InvalidOperationException("Window not initialized. Call Initialize() first.");

            NativeMethods.MSG msg;
            while (NativeMethods.GetMessage(out msg, IntPtr.Zero, 0, 0))
            {
                NativeMethods.TranslateMessage(ref msg);
                NativeMethods.DispatchMessage(ref msg);
            }
        }

        public void Quit(int exitCode = 0)
        {
            NativeMethods.PostQuitMessage(exitCode);
        }

        public void Dispose()
        {
            if (!disposed)
            {
                if (handle != IntPtr.Zero)
                {
                    NativeMethods.DestroyWindow(handle);
                    handle = IntPtr.Zero;
                }

                if (classAtom != 0)
                {
                    fixed (char* classNamePtr = className)
                    {
                        NativeMethods.UnregisterClass(classNamePtr, hInstance);
                    }
                    classAtom = 0;
                }

                isInitialized = false;
                disposed = true;
            }
        }

        ~EventWindow()
        {
            Dispose();
        }
    }

    // 使用例
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Barcode Reader Sample ===\n");

            // キーボードデバイス一覧を表示
            Console.WriteLine("Available keyboard devices:");
            var devices = BarcodeReader.GetKeyboardDevices();
            for (int i = 0; i < devices.Count; i++)
            {
                Console.WriteLine($"  [{i}] {devices[i]}");
                Console.WriteLine($"      Handle: 0x{devices[i].DeviceHandle:X}");
                Console.WriteLine($"      Type: {devices[i].DeviceType}, SubType: {devices[i].SubType}");
            }

            if (devices.Count == 0)
            {
                Console.WriteLine("No keyboard devices found.");
                return;
            }

            Console.WriteLine("\nSelect device index (or press Enter to use all keyboards): ");
            string input = Console.ReadLine();

            IntPtr selectedDevice = IntPtr.Zero;
            if (!string.IsNullOrEmpty(input) && int.TryParse(input, out int index) && index >= 0 && index < devices.Count)
            {
                selectedDevice = devices[index].DeviceHandle;
                Console.WriteLine($"Selected: {devices[index]}");
            }
            else
            {
                Console.WriteLine("Monitoring all keyboard devices");
            }

            // バーコードリーダーを作成
            using (var reader = new BarcodeReader(selectedDevice))
            {
                // イベントハンドラを登録
                reader.BarcodeRead += (sender, e) =>
                {
                    Console.WriteLine($"\n*** BARCODE READ ***");
                    Console.WriteLine($"Barcode: {e.Barcode}");
                    Console.WriteLine($"Time: {e.Timestamp:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine($"Device: 0x{e.DeviceHandle:X}");
                    Console.WriteLine("********************\n");
                };

                // 開始
                reader.Start();

                Console.WriteLine("\nBarcode reader started. Scan a barcode or press 'Q' to quit.\n");

                // キー入力待機
                while (true)
                {
                    var key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Q)
                    {
                        break;
                    }
                }

                // 停止
                reader.Stop();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}

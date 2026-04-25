using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace GlobalHookLib
{
    public enum MyKeys
    {
        // Буквы
        A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46, G = 0x47,
        H = 0x48, I = 0x49, J = 0x4A, K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E,
        O = 0x4F, P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54, U = 0x55,
        V = 0x56, W = 0x57, X = 0x58, Y = 0x59, Z = 0x5A,

        // Цифры
        D0 = 0x30, D1 = 0x31, D2 = 0x32, D3 = 0x33, D4 = 0x34,
        D5 = 0x35, D6 = 0x36, D7 = 0x37, D8 = 0x38, D9 = 0x39,

        // Функциональные клавиши
        F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73, F5 = 0x74,
        F6 = 0x75, F7 = 0x76, F8 = 0x77, F9 = 0x78, F10 = 0x79,
        F11 = 0x7A, F12 = 0x7B,

        // Управляющие клавиши
        Space = 0x20,
        Enter = 0x0D,
        Escape = 0x1B,
        Backspace = 0x08,
        Tab = 0x09,
        Shift = 0x10,
        Control = 0x11,
        Alt = 0x12,
        CapsLock = 0x14,

        // Стрелки
        LeftArrow = 0x25,
        UpArrow = 0x26,
        RightArrow = 0x27,
        DownArrow = 0x28,

        // Дополнительные клавиши
        Home = 0x24,
        End = 0x23,
        PageUp = 0x21,
        PageDown = 0x22,
        Insert = 0x2D,
        Delete = 0x2E,

        // Символы
        OemPeriod = 0xBE,      // .
        OemComma = 0xBC,       // ,
        OemMinus = 0xBD,       // -
        OemPlus = 0xBB,        // +
        OemQuestion = 0xBF,    // /
        OemTilde = 0xC0,       // `
        OemOpenBrackets = 0xDB, // [
        OemCloseBrackets = 0xDD, // ]
        OemQuotes = 0xDE,      // '
        OemBackslash = 0xE2,   // \
        OemSemicolon = 0xBA    // ;
    }

    // Класс аргументов события
    public class KeyEventArgs : EventArgs
    {
        public MyKeys Key { get; private set; }
        public int KeyCode { get; private set; }
        public bool IsAltPressed { get; set; }
        public bool IsCtrlPressed { get; set; }
        public bool IsShiftPressed { get; set; }

        public KeyEventArgs(MyKeys key, int keyCode)
        {
            Key = key;
            KeyCode = keyCode;
            IsAltPressed = false;
            IsCtrlPressed = false;
            IsShiftPressed = false;
        }
    }

    // Основной класс хука
    public class KeyboardHook : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private LowLevelKeyboardProc _proc;
        private IntPtr _hookID = IntPtr.Zero;
        private bool _isDisposed = false;
        private bool _isStarted = false;

        // Событие, которое вызывается при нажатии клавиши
        public event EventHandler<KeyEventArgs> KeyPressed;

        public bool IsStarted => _isStarted;

        public KeyboardHook()
        {
            _proc = HookCallback;
        }

        // Запуск хука
        public void Start()
        {
            if (_isDisposed)
                throw new ObjectDisposedException("KeyboardHook");

            if (_hookID == IntPtr.Zero)
            {
                _hookID = SetHook(_proc);
                if (_hookID == IntPtr.Zero)
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Exception($"Не удалось установить хук. Ошибка: {error}. Запустите программу от имени администратора.");
                }
                _isStarted = true;
            }
        }

        // Остановка хука
        public void Stop()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
                _isStarted = false;
            }
        }

        // Установка хука
        private IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        // Callback хука
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            try
            {
                if (nCode >= 0 && !_isDisposed && _isStarted)
                {
                    if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
                    {
                        int vkCode = Marshal.ReadInt32(lParam);
                        MyKeys key = (MyKeys)vkCode;

                        // Создаем аргументы события
                        var args = new KeyEventArgs(key, vkCode);

                        // Вызываем событие (синхронно, но быстро)
                        EventHandler<KeyEventArgs> handler = KeyPressed;
                        if (handler != null)
                        {
                            handler(this, args);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Логируем ошибку в отладочную консоль
                Debug.WriteLine($"HookCallback error: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }

            // Всегда передаем управление дальше
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        #region WinAPI импорты

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook,
            LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        // Освобождение ресурсов
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _isDisposed = true;
                Stop();
            }
            GC.SuppressFinalize(this);
        }

        ~KeyboardHook()
        {
            Dispose();
        }
    }
}
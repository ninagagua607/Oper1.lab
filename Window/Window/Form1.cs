using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Window
{
    public partial class Form1 : Form
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool SetWindowText(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        // Активации окна для кнопки "показать окно"
        [DllImport("user32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        private const int SW_HIDE = 0;  // скрыть окно
        private const int SW_SHOW = 5;   // показать окно в текущем состояниии
        private const int SW_RESTORE = 9;  // Восстановить свернутое окно

        private class WindowInfo
        {
            public IntPtr Handle { get; set; }
            public string Title { get; set; }
            public string ProcessName { get; set; }

            public override string ToString()
            {
                return string.IsNullOrEmpty(Title) ? $"[Без заголовка] - {ProcessName}" : $"{Title} - {ProcessName}";
            }
        }

        private List<WindowInfo> windows_1 = new List<WindowInfo>();

        public Form1()
        {
            InitializeComponent();

            btnRefresh.Click += btnRefresh_Click;
            btnHide.Click += (s, e) => HideSelectedWindow();
            btnShow.Click += (s, e) => ShowSelectedWindow();
            btnRename.Click += (s, e) => RenameSelectedWindow();

            this.Load += (s, e) => RefreshWindowList();
        }

        private List<WindowInfo> GetAllWindows()
        {
            var windows = new List<WindowInfo>();

            EnumWindows((hWnd, lParam) =>
            {
                if (IsWindowVisible(hWnd))
                {
                    int titleLength = GetWindowTextLength(hWnd);

                    if (titleLength > 0)
                    {
                        var titleBuilder = new StringBuilder(titleLength + 1);
                        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity);
                        string title = titleBuilder.ToString();

                        string processName = "Неизвестно";
                        try
                        {
                            GetWindowThreadProcessId(hWnd, out uint processId);
                            if (processId != 0)
                            {
                                var process = Process.GetProcessById((int)processId);
                                processName = process.ProcessName;
                            }
                        }
                        catch { }

                        windows.Add(new WindowInfo
                        {
                            Handle = hWnd,
                            Title = title,
                            ProcessName = processName
                        });
                    }
                }
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        private void RefreshWindowList()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                windows_1 = GetAllWindows();

                lstWindows.Items.Clear();
                foreach (var window in windows_1)
                {
                    lstWindows.Items.Add(window);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnRefresh_Click(object sender, EventArgs e)  // обработка кнопки "обновить"
        {
            RefreshWindowList();
        }

        private void HideSelectedWindow() // Hide Selected - скрыть выбранное
        {
            if (lstWindows.SelectedItem == null)
            {
                MessageBox.Show("Выберите окно из списка.", "Нет выбора",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedWindow = (WindowInfo)lstWindows.SelectedItem;

            if (ShowWindow(selectedWindow.Handle, SW_HIDE))
            {
                RefreshWindowList();
            }
            else
            {
                MessageBox.Show("Не удалось скрыть окно.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ShowSelectedWindow()  // показать - show
        {
            if (lstWindows.SelectedItem == null)
            {
                MessageBox.Show("Выберите окно из списка.", "Нет выбора",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedWindow = (WindowInfo)lstWindows.SelectedItem;

            ShowWindowAsync(selectedWindow.Handle, SW_RESTORE);

            BringWindowToTop(selectedWindow.Handle);

            SetForegroundWindow(selectedWindow.Handle);

            RefreshWindowList();
        }

        private void RenameSelectedWindow()
        {
            if (lstWindows.SelectedItem == null)
            {
                MessageBox.Show("Выберите окно из списка.", "Нет выбора",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string newTitle = txtNewTitle.Text.Trim();

            if (string.IsNullOrEmpty(newTitle))
            {
                MessageBox.Show("Введите новое название окна.", "Нет названия",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var selectedWindow = (WindowInfo)lstWindows.SelectedItem;

            if (SetWindowText(selectedWindow.Handle, newTitle))
            {
                RefreshWindowList();
                txtNewTitle.Clear();
            }
            else
            {
                MessageBox.Show("Не удалось переименовать окно.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
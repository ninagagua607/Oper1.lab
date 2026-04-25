using System;
using System.Windows.Forms;
using GlobalHookLib;
using Microsoft.Win32;

namespace MainApp
{
    public partial class Form1 : Form
    {
        private KeyboardHook _hook;

        // Константы для USB устройств
        private const int WM_DEVICECHANGE = 0x0219;
        private const int DBT_DEVICEARRIVAL = 0x8000;
        private const int DBT_DEVICEREMOVECOMPLETE = 0x8004;

        // Для отслеживания состояния зарядки (чтобы не дублировать сообщения)
        private bool _lastPowerState = false;  // false = отключена, true = подключена
        private DateTime _lastPowerEventTime = DateTime.MinValue;
        private const int POWER_EVENT_DELAY_MS = 2000;  // Задержка 2 секунды между сообщениями

        public Form1()
        {
            InitializeComponent();
            _hook = new KeyboardHook();
            _hook.KeyPressed += OnKeyPressed;

            SystemEvents.PowerModeChanged += OnPowerModeChanged;
        }

        // ==================== ОБРАБОТКА КЛАВИАТУРЫ ====================
        private void OnKeyPressed(object sender, GlobalHookLib.KeyEventArgs e)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(() => OnKeyPressed(sender, e)));
                return;
            }

            try
            {
                string log = $"{DateTime.Now:HH:mm:ss} – Нажата клавиша: {e.Key}";
                listBox1.Items.Insert(0, log);

                if (listBox1.Items.Count > 100)
                    listBox1.Items.RemoveAt(100);
            }
            catch (Exception ex)
            {
                listBox1.Items.Insert(0, $"Ошибка: {ex.Message}");
            }
        }

        // ==================== ОБРАБОТКА USB УСТРОЙСТВ ====================
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);

            if (m.Msg == WM_DEVICECHANGE)
            {
                switch ((int)m.WParam)
                {
                    case DBT_DEVICEARRIVAL:
                        AddLogMessage("🔌 USB устройство подключено");
                        break;

                    case DBT_DEVICEREMOVECOMPLETE:
                        AddLogMessage("⚠️ USB устройство отключено");
                        break;
                }
            }
        }

        // ==================== ОБРАБОТКА ЗАРЯДКИ (без дублирования) ====================
        private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.StatusChange)
            {
                // Защита от частых вызовов
                if ((DateTime.Now - _lastPowerEventTime).TotalMilliseconds < POWER_EVENT_DELAY_MS)
                    return;

                CheckPowerStatus();
                _lastPowerEventTime = DateTime.Now;
            }
        }

        // Проверка текущего статуса зарядки
        private void CheckPowerStatus()
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(CheckPowerStatus));
                return;
            }

            PowerStatus power = SystemInformation.PowerStatus;
            bool isPlugged = (power.PowerLineStatus == PowerLineStatus.Online);

            // Проверяем, изменилось ли состояние
            if (isPlugged != _lastPowerState)
            {
                _lastPowerState = isPlugged;

                if (isPlugged)
                {
                    AddLogMessage("🔋 Зарядка подключена");
                }
                else
                {
                    AddLogMessage("🔋 Зарядка отключена");
                }
            }
        }

        // Вспомогательный метод для добавления сообщений в лог
        private void AddLogMessage(string message)
        {
            if (listBox1.InvokeRequired)
            {
                listBox1.Invoke(new Action(() => AddLogMessage(message)));
                return;
            }

            string log = $"{DateTime.Now:HH:mm:ss} – {message}";
            listBox1.Items.Insert(0, log);

            if (listBox1.Items.Count > 100)
                listBox1.Items.RemoveAt(100);
        }

        // ==================== УПРАВЛЕНИЕ ХУКОМ ====================
        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                _hook.Start();
                btnStart.Enabled = false;
                btnStop.Enabled = true;
                AddLogMessage("✅ Хук клавиатуры запущен");

                // Устанавливаем начальное состояние зарядки
                PowerStatus power = SystemInformation.PowerStatus;
                _lastPowerState = (power.PowerLineStatus == PowerLineStatus.Online);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске хука: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try
            {
                _hook.Stop();
                btnStart.Enabled = true;
                btnStop.Enabled = false;
                AddLogMessage("⛔ Хук клавиатуры остановлен");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при остановке хука: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==================== ОСВОБОЖДЕНИЕ РЕСУРСОВ ====================
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _hook?.Dispose();
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            base.OnFormClosed(e);
        }
    }
}
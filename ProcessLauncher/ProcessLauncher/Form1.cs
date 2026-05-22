using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace ProcessLauncher
{
    public class Form1 : Form
    {
        // Элементы интерфейса
        private TextBox txtFilePath;
        private Button btnBrowse;
        private Button btnRun;
        private Button btnStop;
        private ComboBox cmbExamples;
        private TextBox txtArguments;
        private RichTextBox txtOutput;
        private Label lblStatus;
        private GroupBox groupBoxInput;
        private GroupBox groupBoxOutput;
        private ProgressBar progressBar;
        private CheckBox chkShowTimestamp;
        private CheckBox chkAutoScroll;
        private NumericUpDown nudTimeout;
        private OpenFileDialog openFileDialog;
        private ComboBox cmbEncoding; // Выбор кодировки

        // Процесс
        private Process currentProcess;
        private StringBuilder outputBuilder;
        private StringBuilder errorBuilder;
        private bool isProcessRunning = false;

        public Form1()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "Запуск программ и утилит с перехватом вывода";
            this.Size = new Size(1100, 850);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 240, 240);
            this.MinimumSize = new Size(1000, 750);

            // ========== ГРУППА НАСТРОЕК ЗАПУСКА ==========
            groupBoxInput = new GroupBox()
            {
                Text = "Настройки запуска",
                Location = new Point(12, 12),
                Size = new Size(1060, 260), // Увеличено для размещения выбора кодировки
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.White
            };

            // Строка 1: Выбор программы
            Label lblFile = new Label()
            {
                Text = "Программа (.exe, .bat, .cmd):",
                Location = new Point(15, 35),
                Size = new Size(200, 25),
                Font = new Font("Segoe UI", 9)
            };

            txtFilePath = new TextBox()
            {
                Location = new Point(220, 33),
                Size = new Size(600, 27),
                Font = new Font("Segoe UI", 9)
            };

            btnBrowse = new Button()
            {
                Text = "Обзор...",
                Location = new Point(830, 31),
                Size = new Size(100, 30),
                BackColor = Color.FromArgb(200, 200, 200),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9)
            };
            btnBrowse.Click += BtnBrowse_Click;

            // Строка 2: Примеры утилит
            Label lblExamples = new Label()
            {
                Text = "Примеры утилит:",
                Location = new Point(15, 75),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };

            cmbExamples = new ComboBox()
            {
                Location = new Point(220, 73),
                Size = new Size(350, 28),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbExamples.Items.AddRange(new object[] {
                "ipconfig /all",
                "ping google.com -n 4",
                "ping 8.8.8.8 -n 4",
                "systeminfo",
                "tasklist",
                "dir C:\\Windows\\System32\\drivers\\etc",
                "netstat -an",
                "whoami",
                "ver",
                "chcp",  // Покажет текущую кодовую страницу
                "powershell Get-Process"
            });
            cmbExamples.SelectedIndexChanged += CmbExamples_SelectedIndexChanged;

            Button btnApplyExample = new Button()
            {
                Text = "Применить",
                Location = new Point(580, 71),
                Size = new Size(80, 30),
                BackColor = Color.FromArgb(220, 230, 250),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8)
            };
            btnApplyExample.Click += (s, e) => CmbExamples_SelectedIndexChanged(null, null);

            // Строка 3: Аргументы
            Label lblArgs = new Label()
            {
                Text = "Аргументы:",
                Location = new Point(15, 115),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };

            txtArguments = new TextBox()
            {
                Location = new Point(220, 113),
                Size = new Size(600, 27),
                Font = new Font("Segoe UI", 9),
                PlaceholderText = "Введите аргументы командной строки"
            };

            // Строка 4: Таймаут и чекбоксы
            Label lblTimeout = new Label()
            {
                Text = "Таймаут (сек):",
                Location = new Point(15, 155),
                Size = new Size(100, 25),
                Font = new Font("Segoe UI", 9)
            };

            nudTimeout = new NumericUpDown()
            {
                Location = new Point(220, 153),
                Size = new Size(80, 27),
                Minimum = 0,
                Maximum = 300,
                Value = 30,
                Font = new Font("Segoe UI", 9)
            };

            Label lblTimeoutNote = new Label()
            {
                Text = "(0 - без таймаута)",
                Location = new Point(310, 155),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.Gray
            };

            chkShowTimestamp = new CheckBox()
            {
                Text = "Показывать время",
                Location = new Point(480, 153),
                Size = new Size(140, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9)
            };

            chkAutoScroll = new CheckBox()
            {
                Text = "Автопрокрутка",
                Location = new Point(650, 153),
                Size = new Size(120, 25),
                Checked = true,
                Font = new Font("Segoe UI", 9)
            };

            // Строка 5: ВЫБОР КОДИРОВКИ (НОВОЕ!)
            Label lblEncoding = new Label()
            {
                Text = "Кодировка вывода:",
                Location = new Point(15, 195),
                Size = new Size(120, 25),
                Font = new Font("Segoe UI", 9)
            };

            cmbEncoding = new ComboBox()
            {
                Location = new Point(220, 193),
                Size = new Size(200, 28),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbEncoding.Items.AddRange(new object[] {
                "CP866 (русская DOS)",
                "UTF-8",
                "Windows-1251 (русская ANSI)",
                "CP437 (English DOS)",
                "Автоопределение"
            });
            cmbEncoding.SelectedIndex = 0; // По умолчанию CP866 для русских консольных команд

            // ========== КНОПКИ УПРАВЛЕНИЯ ==========
            btnRun = new Button()
            {
                Text = "▶ ЗАПУСТИТЬ",
                Location = new Point(220, 225),
                Size = new Size(200, 45),
                BackColor = Color.FromArgb(76, 175, 80),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnRun.Click += BtnRun_Click;

            btnStop = new Button()
            {
                Text = "■ ОСТАНОВИТЬ",
                Location = new Point(440, 225),
                Size = new Size(200, 45),
                BackColor = Color.FromArgb(244, 67, 54),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.White,
                Enabled = false,
                Cursor = Cursors.Hand
            };
            btnStop.Click += BtnStop_Click;

            progressBar = new ProgressBar()
            {
                Location = new Point(660, 235),
                Size = new Size(250, 25),
                Style = ProgressBarStyle.Marquee,
                Visible = false
            };

            groupBoxInput.Controls.AddRange(new Control[] {
                lblFile, txtFilePath, btnBrowse,
                lblExamples, cmbExamples, btnApplyExample,
                lblArgs, txtArguments,
                lblTimeout, nudTimeout, lblTimeoutNote,
                chkShowTimestamp, chkAutoScroll,
                lblEncoding, cmbEncoding,
                btnRun, btnStop, progressBar
            });

            // ========== ГРУППА ВЫВОДА ==========
            groupBoxOutput = new GroupBox()
            {
                Text = "Вывод программы (stdout / stderr)",
                Location = new Point(12, 280),
                Size = new Size(1060, 510),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.White
            };

            // Панель инструментов
            Panel outputToolbar = new Panel()
            {
                Location = new Point(10, 25),
                Size = new Size(1040, 40),
                BackColor = Color.FromArgb(245, 245, 245),
                BorderStyle = BorderStyle.FixedSingle
            };

            Button btnClearOutput = new Button()
            {
                Text = "🗑 Очистить вывод",
                Location = new Point(10, 7),
                Size = new Size(130, 26),
                BackColor = Color.FromArgb(255, 193, 7),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                Cursor = Cursors.Hand
            };
            btnClearOutput.Click += (s, e) => ClearOutput();

            Button btnCopyOutput = new Button()
            {
                Text = "📋 Копировать вывод",
                Location = new Point(150, 7),
                Size = new Size(130, 26),
                BackColor = Color.FromArgb(33, 150, 243),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCopyOutput.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(txtOutput.Text))
                {
                    Clipboard.SetText(txtOutput.Text);
                    UpdateStatus("✅ Вывод скопирован в буфер обмена", Color.Green);
                }
            };

            Button btnSaveOutput = new Button()
            {
                Text = "💾 Сохранить вывод",
                Location = new Point(290, 7),
                Size = new Size(130, 26),
                BackColor = Color.FromArgb(156, 39, 176),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnSaveOutput.Click += (s, e) =>
            {
                SaveFileDialog saveDialog = new SaveFileDialog();
                saveDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
                saveDialog.DefaultExt = "txt";
                saveDialog.FileName = $"output_{DateTime.Now:yyyyMMdd_HHmmss}.txt";

                if (saveDialog.ShowDialog() == DialogResult.OK)
                {
                    File.WriteAllText(saveDialog.FileName, txtOutput.Text, Encoding.UTF8);
                    UpdateStatus($"✅ Вывод сохранён в {saveDialog.FileName}", Color.Green);
                }
            };

            outputToolbar.Controls.AddRange(new Control[] { btnClearOutput, btnCopyOutput, btnSaveOutput });

            txtOutput = new RichTextBox()
            {
                Location = new Point(10, 75),
                Size = new Size(1040, 390),
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.FromArgb(0, 255, 0),
                WordWrap = false
            };

            lblStatus = new Label()
            {
                Text = "✅ Готов к работе. Выберите программу для запуска.",
                Location = new Point(10, 470),
                Size = new Size(1040, 35),
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.DarkBlue,
                BackColor = Color.FromArgb(255, 255, 200),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(5),
                BorderStyle = BorderStyle.FixedSingle
            };

            groupBoxOutput.Controls.AddRange(new Control[] { outputToolbar, txtOutput, lblStatus });

            this.Controls.AddRange(new Control[] { groupBoxInput, groupBoxOutput });

            openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Исполняемые файлы (*.exe;*.bat;*.cmd;*.com)|*.exe;*.bat;*.cmd;*.com|Все файлы (*.*)|*.*";
            openFileDialog.Title = "Выберите программу для запуска";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);

            outputBuilder = new StringBuilder();
            errorBuilder = new StringBuilder();

            AddInfoText();
            this.FormClosing += Form1_FormClosing;
        }

        // Получение кодировки из выбранного пункта
        private Encoding GetSelectedEncoding()
        {
            switch (cmbEncoding.SelectedIndex)
            {
                case 0: return Encoding.GetEncoding(866); // CP866 (русская DOS)
                case 1: return Encoding.UTF8;
                case 2: return Encoding.GetEncoding(1251); // Windows-1251
                case 3: return Encoding.GetEncoding(437); // CP437 (English DOS)
                default: return Encoding.GetEncoding(866);
            }
        }

        private void AddInfoText()
        {
            txtOutput.AppendText("╔══════════════════════════════════════════════════════════════════════════════════════════════════════════════════╗\r\n");
            txtOutput.AppendText("║                                         ИНФОРМАЦИЯ О ПРОГРАММЕ                                                       ║\r\n");
            txtOutput.AppendText("╠══════════════════════════════════════════════════════════════════════════════════════════════════════════════════╣\r\n");
            txtOutput.AppendText("║                                                                                                                  ║\r\n");
            txtOutput.AppendText("║  Эта программа позволяет:                                                                                       ║\r\n");
            txtOutput.AppendText("║  • Запускать любые консольные утилиты (.exe, .bat, .cmd)                                                         ║\r\n");
            txtOutput.AppendText("║  • Перехватывать и отображать вывод в реальном времени                                                           ║\r\n");
            txtOutput.AppendText("║  • Правильно отображать русские буквы (выберите кодировку CP866 для консольных команд)                            ║\r\n");
            txtOutput.AppendText("║  • Управлять процессом (остановка по таймауту или вручную)                                                       ║\r\n");
            txtOutput.AppendText("║                                                                                                                  ║\r\n");
            txtOutput.AppendText("║  ⚠ ВАЖНО: Для русских консольных команд (ipconfig, ping, systeminfo)                                            ║\r\n");
            txtOutput.AppendText("║           выберите кодировку 'CP866 (русская DOS)'!                                                              ║\r\n");
            txtOutput.AppendText("║                                                                                                                  ║\r\n");
            txtOutput.AppendText("╚══════════════════════════════════════════════════════════════════════════════════════════════════════════════════╝\r\n\r\n");
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                txtFilePath.Text = openFileDialog.FileName;
            }
        }

        private void CmbExamples_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbExamples.SelectedItem == null) return;

            string selected = cmbExamples.SelectedItem.ToString();
            int spaceIndex = selected.IndexOf(' ');

            if (spaceIndex > 0)
            {
                string program = selected.Substring(0, spaceIndex);
                string args = selected.Substring(spaceIndex + 1);
                txtFilePath.Text = program;
                txtArguments.Text = args;
            }
            else
            {
                txtFilePath.Text = selected;
                txtArguments.Text = "";
            }
        }

        private async void BtnRun_Click(object sender, EventArgs e)
        {
            string filePath = txtFilePath.Text.Trim();
            string arguments = txtArguments.Text.Trim();

            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Выберите или укажите программу для запуска!",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (currentProcess != null && !currentProcess.HasExited)
            {
                StopProcess();
                await System.Threading.Tasks.Task.Delay(500);
            }

            ClearOutput();
            outputBuilder.Clear();
            errorBuilder.Clear();

            btnRun.Enabled = false;
            btnStop.Enabled = true;
            progressBar.Visible = true;
            isProcessRunning = true;

            UpdateStatus("🚀 Запуск программы...", Color.Orange);

            try
            {
                await RunProcessAsync(filePath, arguments);
            }
            catch (Exception ex)
            {
                AppendOutput($"[ОШИБКА] {ex.Message}\r\n", Color.Red);
                UpdateStatus($"❌ Ошибка: {ex.Message}", Color.Red);
            }
            finally
            {
                btnRun.Enabled = true;
                btnStop.Enabled = false;
                progressBar.Visible = false;
                isProcessRunning = false;

                if (currentProcess != null)
                {
                    currentProcess.Dispose();
                    currentProcess = null;
                }
            }
        }

        private System.Threading.Tasks.Task RunProcessAsync(string fileName, string arguments)
        {
            return System.Threading.Tasks.Task.Run(() =>
            {
                currentProcess = new Process();
                currentProcess.StartInfo.FileName = fileName;
                currentProcess.StartInfo.Arguments = arguments;
                currentProcess.StartInfo.UseShellExecute = false;
                currentProcess.StartInfo.CreateNoWindow = true;
                currentProcess.StartInfo.RedirectStandardOutput = true;
                currentProcess.StartInfo.RedirectStandardError = true;

                // Устанавливаем правильную кодировку!
                Encoding encoding = GetSelectedEncoding();
                currentProcess.StartInfo.StandardOutputEncoding = encoding;
                currentProcess.StartInfo.StandardErrorEncoding = encoding;

                currentProcess.OutputDataReceived += OnOutputDataReceived;
                currentProcess.ErrorDataReceived += OnErrorDataReceived;

                try
                {
                    currentProcess.Start();

                    UpdateStatus($"✅ Процесс запущен (PID: {currentProcess.Id})", Color.Green);
                    AppendOutput($"\r\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n", Color.Cyan);
                    AppendOutput($"🔹 ЗАПУСК: {fileName} {arguments} (PID: {currentProcess.Id})\r\n", Color.Cyan);
                    AppendOutput($"🔹 КОДИРОВКА: {cmbEncoding.SelectedItem}\r\n", Color.Cyan);
                    AppendOutput($"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n\r\n", Color.Cyan);

                    currentProcess.BeginOutputReadLine();
                    currentProcess.BeginErrorReadLine();

                    int timeoutSeconds = (int)nudTimeout.Value;
                    if (timeoutSeconds > 0)
                    {
                        if (currentProcess.WaitForExit(timeoutSeconds * 1000))
                        {
                            currentProcess.WaitForExit();
                            UpdateStatus($"🏁 Процесс завершился с кодом: {currentProcess.ExitCode}",
                                currentProcess.ExitCode == 0 ? Color.Green : Color.Orange);
                        }
                        else
                        {
                            currentProcess.Kill();
                            currentProcess.WaitForExit();
                            UpdateStatus($"⏰ Процесс остановлен по таймауту ({timeoutSeconds} сек)", Color.Red);
                            AppendOutput($"\r\n[!!!] ПРОЦЕСС ОСТАНОВЛЕН ПО ТАЙМАУТУ ({timeoutSeconds} сек)\r\n", Color.Red);
                        }
                    }
                    else
                    {
                        currentProcess.WaitForExit();
                        UpdateStatus($"🏁 Процесс завершился с кодом: {currentProcess.ExitCode}",
                            currentProcess.ExitCode == 0 ? Color.Green : Color.Orange);
                    }

                    AppendOutput($"\r\n━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\r\n", Color.Cyan);
                    AppendOutput($"🔸 ЗАВЕРШЕНИЕ: Код возврата {currentProcess.ExitCode}\r\n", Color.Cyan);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Ошибка запуска: {ex.Message}", Color.Red);
                    throw;
                }
                finally
                {
                    currentProcess.OutputDataReceived -= OnOutputDataReceived;
                    currentProcess.ErrorDataReceived -= OnErrorDataReceived;
                }
            });
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                AppendOutput($"{e.Data}\r\n", Color.LimeGreen);
                outputBuilder.AppendLine(e.Data);
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                AppendOutput($"{e.Data}\r\n", Color.Yellow);
                errorBuilder.AppendLine(e.Data);
            }
        }

        private void AppendOutput(string text, Color color)
        {
            if (txtOutput.InvokeRequired)
            {
                txtOutput.Invoke(new Action(() => AppendOutput(text, color)));
                return;
            }

            string timestamp = chkShowTimestamp.Checked ? $"[{DateTime.Now:HH:mm:ss}] " : "";
            string formattedText = timestamp + text;

            txtOutput.SelectionStart = txtOutput.TextLength;
            txtOutput.SelectionLength = 0;
            txtOutput.SelectionColor = color;
            txtOutput.AppendText(formattedText);

            if (chkAutoScroll.Checked)
            {
                txtOutput.ScrollToCaret();
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => UpdateStatus(message, color)));
                return;
            }

            lblStatus.Text = message;
            lblStatus.ForeColor = color;
        }

        private void ClearOutput()
        {
            if (txtOutput.InvokeRequired)
            {
                txtOutput.Invoke(new Action(ClearOutput));
                return;
            }

            txtOutput.Clear();
            AddInfoText();
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            StopProcess();
        }

        private void StopProcess()
        {
            if (currentProcess != null && !currentProcess.HasExited)
            {
                try
                {
                    currentProcess.Kill();
                    currentProcess.WaitForExit(5000);
                    AppendOutput($"\r\n[!!!] ПРОЦЕСС ОСТАНОВЛЕН ПОЛЬЗОВАТЕЛЕМ\r\n", Color.Red);
                    UpdateStatus("⏹️ Процесс остановлен пользователем", Color.Red);
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ Ошибка при остановке: {ex.Message}", Color.Red);
                }
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (isProcessRunning && currentProcess != null && !currentProcess.HasExited)
            {
                var result = MessageBox.Show("Процесс всё ещё запущен. Остановить его и выйти?",
                    "Подтверждение выхода", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (result == DialogResult.Yes)
                {
                    StopProcess();
                    System.Threading.Thread.Sleep(500);
                }
                else
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
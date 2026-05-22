using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FileLauncher
{
    public partial class Form1 : Form
    {
        // WinAPI импорты
        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int ShellExecute(
            IntPtr hwnd,
            string lpOperation,
            string lpFile,
            string lpParameters,
            string lpDirectory,
            int nShowCmd
        );

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool CreateProcess(
            string lpApplicationName,
            string lpCommandLine,
            IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes,
            bool bInheritHandles,
            uint dwCreationFlags,
            IntPtr lpEnvironment,
            string lpCurrentDirectory,
            ref STARTUPINFO lpStartupInfo,
            out PROCESS_INFORMATION lpProcessInformation
        );

        [StructLayout(LayoutKind.Sequential)]
        private struct STARTUPINFO
        {
            public int cb;
            public string lpReserved;
            public string lpDesktop;
            public string lpTitle;
            public uint dwX;
            public uint dwY;
            public uint dwXSize;
            public uint dwYSize;
            public uint dwXCountChars;
            public uint dwYCountChars;
            public uint dwFillAttribute;
            public uint dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public uint dwProcessId;
            public uint dwThreadId;
        }

        private const int SW_SHOWNORMAL = 1;

        // Элементы управления
        private TextBox txtFilePath;
        private TextBox txtArguments;
        private Button btnBrowse;
        private Button btnShellExecute;
        private Button btnCreateProcess;
        private Button btnRunWithCapture;
        private ComboBox cmbFileType;
        private RadioButton rbFile;
        private RadioButton rbUrl;
        private RichTextBox txtOutput;
        private Label lblStatus;
        private GroupBox grpMethod;
        private GroupBox grpType;
        private CheckBox chkCaptureOutput;
        private ComboBox cmbEncoding;
        private Button btnClearOutput;

        public Form1()
        {
            // Регистрируем кодировки для .NET Core/.NET 5+
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            InitializeComponent();
            SetupControls();
        }

        private void SetupControls()
        {
            this.Text = "File Launcher - С перехватом вывода консольных программ";
            this.Size = new Size(950, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

            // Группа выбора типа
            grpType = new GroupBox()
            {
                Text = "Тип запускаемого объекта",
                Location = new Point(20, 20),
                Size = new Size(890, 60)
            };

            rbFile = new RadioButton()
            {
                Text = "Файл (EXE/TXT/Image)",
                Location = new Point(20, 25),
                Size = new Size(180, 25),
                Checked = true
            };

            rbUrl = new RadioButton()
            {
                Text = "Веб-сайт (URL)",
                Location = new Point(220, 25),
                Size = new Size(150, 25)
            };

            rbFile.CheckedChanged += RbFile_CheckedChanged;
            rbUrl.CheckedChanged += RbUrl_CheckedChanged;

            grpType.Controls.Add(rbFile);
            grpType.Controls.Add(rbUrl);
            this.Controls.Add(grpType);

            // Поле для пути
            Label lblPath = new Label()
            {
                Text = "Путь/URL:",
                Location = new Point(20, 100),
                Size = new Size(80, 25)
            };
            this.Controls.Add(lblPath);

            txtFilePath = new TextBox()
            {
                Location = new Point(100, 98),
                Size = new Size(580, 23),
                ReadOnly = false
            };
            this.Controls.Add(txtFilePath);

            btnBrowse = new Button()
            {
                Text = "Обзор...",
                Location = new Point(690, 96),
                Size = new Size(70, 30),
                BackColor = Color.LightGray
            };
            btnBrowse.Click += BtnBrowse_Click;
            this.Controls.Add(btnBrowse);

            // Аргументы командной строки
            Label lblArgs = new Label()
            {
                Text = "Аргументы:",
                Location = new Point(20, 140),
                Size = new Size(80, 25)
            };
            this.Controls.Add(lblArgs);

            txtArguments = new TextBox()
            {
                Location = new Point(100, 138),
                Size = new Size(580, 23),
                ReadOnly = false,
                Text = ""
            };
            this.Controls.Add(txtArguments);

            // Выбор кодировки
            Label lblEncoding = new Label()
            {
                Text = "Кодировка:",
                Location = new Point(20, 180),
                Size = new Size(80, 25)
            };
            this.Controls.Add(lblEncoding);

            cmbEncoding = new ComboBox()
            {
                Location = new Point(100, 178),
                Size = new Size(180, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbEncoding.Items.AddRange(new object[] {
                "Авто (UTF-8)",
                "Windows-1251 (CP1251)",
                "DOS (CP866)",
                "UTF-8 (BOM)"
            });
            cmbEncoding.SelectedIndex = 0;
            this.Controls.Add(cmbEncoding);

            // Фильтр файлов
            Label lblFilter = new Label()
            {
                Text = "Фильтр:",
                Location = new Point(300, 180),
                Size = new Size(80, 25)
            };
            this.Controls.Add(lblFilter);

            cmbFileType = new ComboBox()
            {
                Location = new Point(380, 178),
                Size = new Size(200, 25),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbFileType.Items.AddRange(new object[] {
                "Все файлы (*.*)",
                "Исполняемые файлы (*.exe)",
                "Текстовые файлы (*.txt)",
                "Изображения (*.jpg;*.png;*.bmp)",
                "Консольные утилиты (*.exe)"
            });
            cmbFileType.SelectedIndex = 0;
            this.Controls.Add(cmbFileType);

            // Чекбокс перехвата вывода
            chkCaptureOutput = new CheckBox()
            {
                Text = "Перехватывать вывод консоли",
                Location = new Point(600, 178),
                Size = new Size(200, 25),
                Checked = true
            };
            this.Controls.Add(chkCaptureOutput);

            // Группа методов запуска
            grpMethod = new GroupBox()
            {
                Text = "Метод запуска",
                Location = new Point(20, 220),
                Size = new Size(890, 80)
            };

            btnShellExecute = new Button()
            {
                Text = "ShellExecute\n(без перехвата)",
                Location = new Point(20, 22),
                Size = new Size(180, 45),
                BackColor = Color.LightBlue,
                FlatStyle = FlatStyle.Flat
            };
            btnShellExecute.Click += BtnShellExecute_Click;

            btnCreateProcess = new Button()
            {
                Text = "CreateProcess\n(без перехвата)",
                Location = new Point(220, 22),
                Size = new Size(180, 45),
                BackColor = Color.LightGreen,
                FlatStyle = FlatStyle.Flat
            };
            btnCreateProcess.Click += BtnCreateProcess_Click;

            btnRunWithCapture = new Button()
            {
                Text = "ЗАПУСТИТЬ С ПЕРЕХВАТОМ\n(Process.Start + Redirect)",
                Location = new Point(420, 22),
                Size = new Size(250, 45),
                BackColor = Color.Orange,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9, FontStyle.Bold)
            };
            btnRunWithCapture.Click += BtnRunWithCapture_Click;

            btnClearOutput = new Button()
            {
                Text = "Очистить вывод",
                Location = new Point(690, 22),
                Size = new Size(120, 45),
                BackColor = Color.LightGray,
                FlatStyle = FlatStyle.Flat
            };
            btnClearOutput.Click += (s, e) => txtOutput.Clear();

            grpMethod.Controls.Add(btnShellExecute);
            grpMethod.Controls.Add(btnCreateProcess);
            grpMethod.Controls.Add(btnRunWithCapture);
            grpMethod.Controls.Add(btnClearOutput);
            this.Controls.Add(grpMethod);

            // Окно вывода
            Label lblOutput = new Label()
            {
                Text = "ВЫВОД КОНСОЛИ (stdout/stderr):",
                Location = new Point(20, 320),
                Size = new Size(250, 25),
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            this.Controls.Add(lblOutput);

            txtOutput = new RichTextBox()
            {
                Location = new Point(20, 350),
                Size = new Size(890, 300),
                BackColor = Color.Black,
                ForeColor = Color.LightGreen,
                Font = new Font("Consolas", 10),
                ReadOnly = true,
                WordWrap = true
            };
            this.Controls.Add(txtOutput);

            // Статус
            lblStatus = new Label()
            {
                Text = "Готов к работе. Выберите консольную утилиту для перехвата вывода.",
                Location = new Point(20, 670),
                Size = new Size(890, 40),
                BackColor = Color.LightYellow,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Segoe UI", 9, FontStyle.Italic)
            };
            this.Controls.Add(lblStatus);
        }

        private Encoding GetSelectedEncoding()
        {
            switch (cmbEncoding.SelectedIndex)
            {
                case 1: return Encoding.GetEncoding(1251); // Windows-1251
                case 2: return Encoding.GetEncoding(866);  // DOS (CP866)
                case 3: return new UTF8Encoding(true);     // UTF-8 с BOM
                default: return Encoding.UTF8;              // UTF-8
            }
        }

        private void RbFile_CheckedChanged(object sender, EventArgs e)
        {
            btnBrowse.Enabled = rbFile.Checked;
            cmbFileType.Enabled = rbFile.Checked;
            txtArguments.Enabled = rbFile.Checked;
            if (rbFile.Checked)
            {
                txtFilePath.Text = "";
                UpdateStatus("Выберите файл для запуска");
            }
        }

        private void RbUrl_CheckedChanged(object sender, EventArgs e)
        {
            if (rbUrl.Checked)
            {
                txtFilePath.Text = "https://";
                btnBrowse.Enabled = false;
                cmbFileType.Enabled = false;
                txtArguments.Enabled = false;
                txtArguments.Text = "";
                UpdateStatus("Введите URL веб-сайта");
            }
        }

        private string GetFileFilter()
        {
            switch (cmbFileType.SelectedIndex)
            {
                case 1: return "Исполняемые файлы (*.exe)|*.exe";
                case 2: return "Текстовые файлы (*.txt)|*.txt";
                case 3: return "Изображения (*.jpg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp";
                case 4: return "Консольные утилиты (*.exe)|*.exe";
                default: return "Все файлы (*.*)|*.*";
            }
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = GetFileFilter();
                openFileDialog.Title = "Выберите файл для запуска";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;
                    UpdateStatus($"Выбран файл: {Path.GetFileName(openFileDialog.FileName)}");

                    string fileName = Path.GetFileName(openFileDialog.FileName).ToLower();
                    if (fileName == "ping.exe")
                        txtArguments.Text = "ya.ru -n 4";
                    else if (fileName == "ipconfig.exe")
                        txtArguments.Text = "/all";
                }
            }
        }

        private void BtnShellExecute_Click(object sender, EventArgs e)
        {
            string target = txtFilePath.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                MessageBox.Show("Пожалуйста, выберите файл или введите URL", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                int result;
                if (rbUrl.Checked)
                {
                    result = ShellExecute(this.Handle, "open", target, "", "", SW_SHOWNORMAL);
                }
                else
                {
                    string directory = string.IsNullOrEmpty(Path.GetDirectoryName(target))
                        ? Environment.CurrentDirectory
                        : Path.GetDirectoryName(target);
                    result = ShellExecute(this.Handle, "open", target, txtArguments.Text, directory, SW_SHOWNORMAL);
                }

                if (result > 32)
                {
                    UpdateStatus($"✓ Запущено через ShellExecute: {target}");
                    AddToOutput("[ShellExecute] Программа запущена (вывод не перехватывается)\n");
                }
                else
                {
                    UpdateStatus($"✗ Ошибка ShellExecute. Код: {result}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"✗ Ошибка: {ex.Message}");
            }
        }

        private void BtnCreateProcess_Click(object sender, EventArgs e)
        {
            string target = txtFilePath.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                MessageBox.Show("Пожалуйста, выберите файл", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (rbUrl.Checked)
            {
                MessageBox.Show("CreateProcess не поддерживает URL", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!File.Exists(target))
            {
                MessageBox.Show($"Файл не существует:\n{target}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                STARTUPINFO si = new STARTUPINFO();
                si.cb = Marshal.SizeOf(si);
                si.dwFlags = 0x00000001;
                si.wShowWindow = 1;

                PROCESS_INFORMATION pi = new PROCESS_INFORMATION();

                string commandLine = target.Contains(" ") ? $"\"{target}\"" : target;
                if (!string.IsNullOrEmpty(txtArguments.Text))
                    commandLine += " " + txtArguments.Text;

                bool success = CreateProcess(
                    null,
                    commandLine,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    false,
                    0,
                    IntPtr.Zero,
                    Path.GetDirectoryName(target),
                    ref si,
                    out pi
                );

                if (success)
                {
                    UpdateStatus($"✓ Запущено через CreateProcess. PID: {pi.dwProcessId}");
                    AddToOutput($"[CreateProcess] Процесс запущен (PID: {pi.dwProcessId})\n");
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    UpdateStatus($"✗ Ошибка CreateProcess. Код: {error}");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"✗ Ошибка: {ex.Message}");
            }
        }

        private async void BtnRunWithCapture_Click(object sender, EventArgs e)
        {
            string target = txtFilePath.Text.Trim();
            if (string.IsNullOrEmpty(target))
            {
                MessageBox.Show("Пожалуйста, выберите программу", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (rbUrl.Checked)
            {
                MessageBox.Show("URL нельзя запустить с перехватом вывода", "Информация",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (!File.Exists(target))
            {
                MessageBox.Show($"Файл не существует:\n{target}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Очищаем вывод
            txtOutput.Clear();
            AddToOutput($"═══════════════════════════════════════════════════════════\n", Color.Cyan);
            AddToOutput($"Запуск: {Path.GetFileName(target)}\n", Color.Yellow);
            AddToOutput($"Путь: {target}\n", Color.Gray);
            if (!string.IsNullOrEmpty(txtArguments.Text))
                AddToOutput($"Аргументы: {txtArguments.Text}\n", Color.Gray);
            AddToOutput($"═══════════════════════════════════════════════════════════\n\n", Color.Cyan);

            // Запускаем процесс с перехватом вывода
            await Task.Run(() => RunProcessAndCaptureOutput(target));
        }

        private void RunProcessAndCaptureOutput(string target)
        {
            try
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = target;
                startInfo.Arguments = txtArguments.Text;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.CreateNoWindow = chkCaptureOutput.Checked;
                startInfo.StandardOutputEncoding = GetSelectedEncoding();
                startInfo.StandardErrorEncoding = GetSelectedEncoding();

                using (Process process = new Process())
                {
                    process.StartInfo = startInfo;

                    StringBuilder outputBuilder = new StringBuilder();
                    StringBuilder errorBuilder = new StringBuilder();

                    using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                    using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                    {
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                outputWaitHandle.Set();
                            }
                            else
                            {
                                outputBuilder.AppendLine(e.Data);
                                AddToOutput(e.Data + "\n");
                            }
                        };

                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (e.Data == null)
                            {
                                errorWaitHandle.Set();
                            }
                            else
                            {
                                errorBuilder.AppendLine(e.Data);
                                AddToOutput(e.Data + "\n", Color.LightCoral);
                            }
                        };

                        process.Start();

                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        if (process.WaitForExit(30000))
                        {
                            outputWaitHandle.WaitOne(5000);
                            errorWaitHandle.WaitOne(5000);

                            AddToOutput($"\n═══════════════════════════════════════════════════════════\n", Color.Cyan);
                            AddToOutput($"Процесс завершен. Exit code: {process.ExitCode}\n", Color.Yellow);

                            if (process.ExitCode == 0)
                                AddToOutput("✓ УСПЕШНОЕ ЗАВЕРШЕНИЕ (ExitCode = 0)\n", Color.LightGreen);
                            else
                                AddToOutput($"✗ ОШИБКА (ExitCode = {process.ExitCode})\n", Color.Orange);

                            UpdateStatus($"✓ Программа завершена. Код выхода: {process.ExitCode}");
                        }
                        else
                        {
                            process.Kill();
                            AddToOutput($"\n═══════════════════════════════════════════════════════════\n", Color.Orange);
                            AddToOutput($"⚠ ПРЕВЫШЕН ТАЙМАУТ (30 сек). Процесс принудительно завершен.\n", Color.Orange);
                            UpdateStatus("⚠ Таймаут! Процесс принудительно завершен");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AddToOutput($"ОШИБКА: {ex.Message}\n", Color.Red);
                UpdateStatus($"✗ Ошибка при запуске: {ex.Message}");
            }
        }

        private void AddToOutput(string text, Color? color = null)
        {
            if (txtOutput.InvokeRequired)
            {
                txtOutput.Invoke(new Action(() => AddToOutput(text, color)));
                return;
            }

            if (color.HasValue)
            {
                txtOutput.SelectionStart = txtOutput.TextLength;
                txtOutput.SelectionLength = 0;
                txtOutput.SelectionColor = color.Value;
                txtOutput.AppendText(text);
                txtOutput.SelectionColor = txtOutput.ForeColor;
            }
            else
            {
                txtOutput.AppendText(text);
            }

            txtOutput.ScrollToCaret();
            Application.DoEvents();
        }

        private void UpdateStatus(string message)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action(() => UpdateStatus(message)));
                return;
            }
            lblStatus.Text = $"[{DateTime.Now:HH:mm:ss}] {message}";
        }

        private void InitializeComponent() { }
    }
}
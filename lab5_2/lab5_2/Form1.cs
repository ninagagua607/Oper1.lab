using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace lab5_2
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        private static extern int SetThreadPriority(IntPtr hThread, int nPriority);

        [DllImport("kernel32.dll")]
        private static extern IntPtr CreateThread(
            IntPtr lpThreadAttributes,
            uint dwStackSize,
            ThreadProc lpStartAddress,
            IntPtr lpParameter,
            uint dwCreationFlags,
            out uint lpThreadId
        );

        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private delegate uint ThreadProc(IntPtr lpParameter);

        private const int THREAD_PRIORITY_LOWEST = -2;
        private const int THREAD_PRIORITY_BELOW_NORMAL = -1;
        private const int THREAD_PRIORITY_NORMAL = 0;
        private const int THREAD_PRIORITY_ABOVE_NORMAL = 1;
        private const int THREAD_PRIORITY_HIGHEST = 2;

        private Button btnStartAll;
        private Button btnPauseAll;
        private Button btnStopAll;
        private Button btnClone;
        private NumericUpDown numPriority;
        private Label lblThreadCount;
        private Label lblPriorityInfo;
        private Panel panelThreads;

        private List<WorkerThread> workers = new List<WorkerThread>();
        private SynchronizationContext uiContext;

        public Form1()
        {
            InitializeComponent();
            uiContext = SynchronizationContext.Current;

            CreateNewThread();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Управление потоками";
            this.Size = new Size(800, 600);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormClosing += Form1_FormClosing;

            panelThreads = new Panel
            {
                Location = new Point(10, 10),
                Size = new Size(760, 450),
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle
            };

            btnStartAll = new Button
            {
                Location = new Point(10, 470),
                Size = new Size(100, 35),
                Text = "Пуск всех",
            };
            btnStartAll.Click += BtnStartAll_Click;

            btnPauseAll = new Button
            {
                Location = new Point(120, 470),
                Size = new Size(100, 35),
                Text = "Пауза всех",
            };
            btnPauseAll.Click += BtnPauseAll_Click;

            btnStopAll = new Button
            {
                Location = new Point(230, 470),
                Size = new Size(100, 35),
                Text = "Стоп всех",
            };
            btnStopAll.Click += BtnStopAll_Click;

            btnClone = new Button
            {
                Location = new Point(340, 470),
                Size = new Size(120, 35),
                Text = "Клонировать поток",
            };
            btnClone.Click += BtnClone_Click;

            Label lblPriority = new Label
            {
                Location = new Point(480, 475),
                Size = new Size(60, 25),
                Text = "Приоритет:"
            };

            numPriority = new NumericUpDown
            {
                Location = new Point(545, 475),
                Size = new Size(60, 25),
                Minimum = -2,
                Maximum = 2,
                Value = 0,
                Increment = 1
            };
            numPriority.ValueChanged += NumPriority_ValueChanged;

            lblPriorityInfo = new Label
            {
                Location = new Point(615, 478),
                Size = new Size(150, 20),
                Text = "Normal",
                Font = new Font("Arial", 9, FontStyle.Bold)
            };

            lblThreadCount = new Label
            {
                Location = new Point(10, 515),
                Size = new Size(200, 25),
                Text = "Активных потоков: 1",
                Font = new Font("Arial", 10, FontStyle.Bold)
            };

            this.Controls.Add(panelThreads);
            this.Controls.Add(btnStartAll);
            this.Controls.Add(btnPauseAll);
            this.Controls.Add(btnStopAll);
            this.Controls.Add(btnClone);
            this.Controls.Add(lblPriority);
            this.Controls.Add(numPriority);
            this.Controls.Add(lblPriorityInfo);
            this.Controls.Add(lblThreadCount);

            this.ResumeLayout(false);
        }

        private class WorkerThread
        {
            public IntPtr NativeHandle;
            public uint ThreadId;
            public volatile bool IsRunning = true;
            public volatile bool IsPaused = false;
            public int Counter = 0;
            public SynchronizationContext UiContext;

            public GroupBox GroupBox;
            public Label CounterLabel;
            public Label PriorityLabel;
            public Button PauseButton;
            public Button StopButton;

            private ThreadProc threadDelegate;
            private ManualResetEvent pauseEvent;
            private ManualResetEvent stopEvent;

            public WorkerThread(SynchronizationContext uiContext)
            {
                UiContext = uiContext;
                pauseEvent = new ManualResetEvent(true);
                stopEvent = new ManualResetEvent(false);

                threadDelegate = new ThreadProc(ThreadWork);

                NativeHandle = CreateThread(
                    IntPtr.Zero,
                    0,
                    threadDelegate,
                    IntPtr.Zero,
                    0,
                    out ThreadId
                );
            }

            private uint ThreadWork(IntPtr lpParameter)
            {
                NativeHandle = GetCurrentThread();

                while (true)
                {
                    if (stopEvent.WaitOne(0))
                    {
                        break;
                    }

                    pauseEvent.WaitOne();

                    if (stopEvent.WaitOne(0))
                    {
                        break;
                    }

                    Counter++;

                    UiContext.Post(_ =>
                    {
                        if (CounterLabel != null)
                        {
                            CounterLabel.Text = $"Счетчик: {Counter}";
                        }
                    }, null);

                    Thread.Sleep(100);
                }

                return 0;
            }

            public void Start()
            {
                pauseEvent.Set();
            }

            public void Pause()
            {
                IsPaused = true;
                pauseEvent.Reset();
                UiContext.Post(_ =>
                {
                    if (PauseButton != null)
                        PauseButton.Text = "Возобновить";
                }, null);
            }

            public void Resume()
            {
                IsPaused = false;
                pauseEvent.Set();
                UiContext.Post(_ =>
                {
                    if (PauseButton != null)
                        PauseButton.Text = "Пауза";
                }, null);
            }

            public void Stop()
            {
                IsRunning = false;
                stopEvent.Set();
                pauseEvent.Set();
                WaitForSingleObject(NativeHandle, 2000);
                CloseHandle(NativeHandle);

                pauseEvent.Dispose();
                stopEvent.Dispose();
            }

            public void SetPriority(int priority)
            {
                int winPriority;
                string priorityName;

                switch (priority)
                {
                    case -2:
                        winPriority = THREAD_PRIORITY_LOWEST;
                        priorityName = "Низкий приоритет";
                        break;
                    case -1:
                        winPriority = THREAD_PRIORITY_BELOW_NORMAL;
                        priorityName = "Не самый важный приоритет";
                        break;
                    case 0:
                        winPriority = THREAD_PRIORITY_NORMAL;
                        priorityName = "Обычный приоритет";
                        break;
                    case 1:
                        winPriority = THREAD_PRIORITY_ABOVE_NORMAL;
                        priorityName = "Приоритет выше нормы";
                        break;
                    case 2:
                        winPriority = THREAD_PRIORITY_HIGHEST;
                        priorityName = "Высокий приоритет";
                        break;
                    default:
                        winPriority = THREAD_PRIORITY_NORMAL;
                        priorityName = "Обычный приоритет";
                        break;
                }

                SetThreadPriority(NativeHandle, winPriority);

                UiContext.Post(_ =>
                {
                    if (PriorityLabel != null)
                        PriorityLabel.Text = $"Приоритет: {priorityName}";
                }, null);
            }
        }

        private void CreateNewThread()
        {
            int threadNumber = workers.Count + 1;

            var groupBox = new GroupBox
            {
                Text = $"Поток #{threadNumber}",
                Location = new Point(5, 5 + (workers.Count) * 130),
                Size = new Size(730, 100)
            };

            var lblCounter = new Label
            {
                Location = new Point(10, 30),
                Size = new Size(200, 25),
                Text = "Счетчик: 0",
                Font = new Font("Arial", 10, FontStyle.Bold),
            };

            var lblPriority = new Label
            {
                Location = new Point(10, 60),
                Size = new Size(200, 25),
                Text = "Приоритет: Обычный приоритет",
                Font = new Font("Arial", 9)
            };

            var btnPause = new Button
            {
                Location = new Point(550, 30),
                Size = new Size(85, 30),
                Text = "Пауза",
            };

            var btnStop = new Button
            {
                Location = new Point(640, 30),
                Size = new Size(85, 30),
                Text = "Стоп",
            };

            groupBox.Controls.Add(lblCounter);
            groupBox.Controls.Add(lblPriority);
            groupBox.Controls.Add(btnPause);
            groupBox.Controls.Add(btnStop);

            panelThreads.Controls.Add(groupBox);

            var worker = new WorkerThread(uiContext)
            {
                GroupBox = groupBox,
                CounterLabel = lblCounter,
                PriorityLabel = lblPriority,
                PauseButton = btnPause,
                StopButton = btnStop
            };

            btnPause.Tag = worker;
            btnStop.Tag = worker;

            btnPause.Click += BtnThreadPause_Click;
            btnStop.Click += BtnThreadStop_Click;

            workers.Add(worker);

            worker.SetPriority((int)numPriority.Value);

            UpdateThreadCount();

            panelThreads.AutoScrollPosition = new Point(0, panelThreads.VerticalScroll.Maximum);
        }

        private void BtnThreadPause_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var worker = btn.Tag as WorkerThread;

            if (worker != null)
            {
                if (worker.IsPaused)
                {
                    worker.Resume();
                }
                else
                {
                    worker.Pause();
                }
            }
        }

        private void BtnThreadStop_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            var worker = btn.Tag as WorkerThread;

            if (worker != null)
            {
                worker.Stop();
                workers.Remove(worker);
                panelThreads.Controls.Remove(worker.GroupBox);
                worker.GroupBox.Dispose();

                int index = 1;
                int yPos = 5;
                foreach (var w in workers)
                {
                    w.GroupBox.Location = new Point(5, yPos);
                    w.GroupBox.Text = $"Поток #{index}";
                    yPos += 130;
                    index++;
                }

                UpdateThreadCount();
            }
        }

        private void BtnStartAll_Click(object sender, EventArgs e)
        {
            foreach (var worker in workers)
            {
                if (worker.IsPaused)
                {
                    worker.Resume();
                }
            }
        }

        private void BtnPauseAll_Click(object sender, EventArgs e)
        {
            foreach (var worker in workers)
            {
                if (!worker.IsPaused)
                {
                    worker.Pause();
                }
            }
        }

        private void BtnStopAll_Click(object sender, EventArgs e)
        {
            foreach (var worker in workers.ToArray())
            {
                worker.Stop();
                workers.Remove(worker);
                panelThreads.Controls.Remove(worker.GroupBox);
                worker.GroupBox.Dispose();
            }

            UpdateThreadCount();
        }

        private void BtnClone_Click(object sender, EventArgs e)
        {
            if (workers.Count < 5)
            {
                CreateNewThread();
            }
            else
            {
                MessageBox.Show("Максимальное количество потоков", "Предупреждение",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void NumPriority_ValueChanged(object sender, EventArgs e)
        {
            int priority = (int)numPriority.Value;

            string priorityText = priority switch
            {
                -2 => "Низкий приоритет",
                -1 => "Не самый важный приоритет",
                0 => "Обычный приоритет",
                1 => "Приоритет выше нормы",
                2 => "Высокий приоритет",
                _ => "Обычный приоритет"
            };
            lblPriorityInfo.Text = priorityText;

            foreach (var worker in workers)
            {
                worker.SetPriority(priority);
            }
        }

        private void UpdateThreadCount()
        {
            lblThreadCount.Text = $"Активных потоков: {workers.Count}";

        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            foreach (var worker in workers)
            {
                worker.Stop();
            }
        }
    }
}
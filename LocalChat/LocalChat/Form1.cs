using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace LocalChat
{
    public partial class Form1 : Form
    {
        // Компоненты для чата
        private TextBox txtHistory;
        private TextBox txtMessage;
        private Button btnSend;
        private TextBox txtIP;
        private Button btnStartServer;
        private Button btnConnectAsClient;
        private Label lblStatus;
        private ListBox lstClients;
        private Button btnDisconnectClient;
        private Button btnRefreshClients;

        // Компоненты для управления клиентами
        private GroupBox grpControl;
        private TextBox txtNewWindowTitle;
        private Button btnChangeWindowTitle;
        private Label lblSelectedClient;

        // Сетевые компоненты для сервера
        private TcpListener server;
        private Thread serverThread;
        private List<ClientInfo> clients = new List<ClientInfo>();
        private object clientsLock = new object();

        // Сетевые компоненты для клиента
        private TcpClient client;
        private NetworkStream clientStream;
        private Thread clientReceiveThread;

        private bool isRunning = true;
        private bool isServerMode = false;

        // Префикс для команд
        private const string CMD_PREFIX = "CMD://";

        private class ClientInfo
        {
            public TcpClient Client { get; set; }
            public NetworkStream Stream { get; set; }
            public string ClientId { get; set; }
            public string IPAddress { get; set; }
            public Thread ReceiveThread { get; set; }
        }

        public Form1()
        {
            this.Text = "Локальный чат с управлением клиентами";
            this.Size = new System.Drawing.Size(900, 650);
            this.StartPosition = FormStartPosition.CenterScreen;

            SplitContainer mainSplit = new SplitContainer();
            mainSplit.Dock = DockStyle.Fill;
            mainSplit.Orientation = Orientation.Vertical;
            mainSplit.SplitterDistance = 550;

            Panel chatPanel = CreateChatPanel();
            mainSplit.Panel1.Controls.Add(chatPanel);

            Panel rightPanel = CreateRightPanel();
            mainSplit.Panel2.Controls.Add(rightPanel);

            Panel topPanel = CreateTopPanel();
            topPanel.Dock = DockStyle.Top;
            topPanel.Height = 80;

            this.Controls.Add(mainSplit);
            this.Controls.Add(topPanel);

            txtMessage.KeyPress += (s, e) => {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    e.Handled = true;
                    BtnSend_Click(s, e);
                }
            };

            this.FormClosing += Form1_FormClosing;
        }

        private Panel CreateChatPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;

            txtHistory = new TextBox();
            txtHistory.Multiline = true;
            txtHistory.ReadOnly = true;
            txtHistory.Dock = DockStyle.Fill;
            txtHistory.ScrollBars = ScrollBars.Vertical;
            txtHistory.Font = new System.Drawing.Font("Consolas", 10);
            txtHistory.BackColor = System.Drawing.Color.Black;
            txtHistory.ForeColor = System.Drawing.Color.LightGreen;

            Panel inputPanel = new Panel();
            inputPanel.Dock = DockStyle.Bottom;
            inputPanel.Height = 80;

            txtMessage = new TextBox();
            txtMessage.Dock = DockStyle.Fill;
            txtMessage.Font = new System.Drawing.Font("Consolas", 10);
            txtMessage.Multiline = true;

            btnSend = new Button();
            btnSend.Text = "Отправить";
            btnSend.Dock = DockStyle.Right;
            btnSend.Width = 100;
            btnSend.Click += BtnSend_Click;

            inputPanel.Controls.Add(txtMessage);
            inputPanel.Controls.Add(btnSend);
            panel.Controls.Add(txtHistory);
            panel.Controls.Add(inputPanel);

            return panel;
        }

        private Panel CreateRightPanel()
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            panel.Padding = new Padding(5);

            Label lblClientsTitle = new Label();
            lblClientsTitle.Text = "ПОДКЛЮЧЕННЫЕ КЛИЕНТЫ:";
            lblClientsTitle.Dock = DockStyle.Top;
            lblClientsTitle.Height = 30;
            lblClientsTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10, System.Drawing.FontStyle.Bold);
            lblClientsTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            lstClients = new ListBox();
            lstClients.Dock = DockStyle.Top;
            lstClients.Height = 200;
            lstClients.Font = new System.Drawing.Font("Consolas", 10);
            lstClients.SelectedIndexChanged += LstClients_SelectedIndexChanged;

            grpControl = new GroupBox();
            grpControl.Dock = DockStyle.Fill;
            grpControl.Text = "УПРАВЛЕНИЕ КЛИЕНТОМ";
            grpControl.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Bold);

            int y = 30;
            int spacing = 40;

            Label lblSelected = new Label();
            lblSelected.Text = "Выбранный клиент:";
            lblSelected.Location = new System.Drawing.Point(10, y);
            lblSelected.Size = new System.Drawing.Size(120, 25);

            lblSelectedClient = new Label();
            lblSelectedClient.Text = "не выбран";
            lblSelectedClient.Location = new System.Drawing.Point(130, y);
            lblSelectedClient.Size = new System.Drawing.Size(130, 25);
            lblSelectedClient.ForeColor = System.Drawing.Color.Red;
            lblSelectedClient.Font = new System.Drawing.Font("Microsoft Sans Serif", 8, System.Drawing.FontStyle.Bold);

            y += spacing;

            Label separator1 = new Label();
            separator1.Text = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
            separator1.Location = new System.Drawing.Point(10, y);
            separator1.Size = new System.Drawing.Size(260, 20);
            separator1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;

            y += spacing;

            Label lblTitle = new Label();
            lblTitle.Text = "СМЕНА ЗАГОЛОВКА ОКНА:";
            lblTitle.Location = new System.Drawing.Point(10, y);
            lblTitle.Size = new System.Drawing.Size(250, 25);
            lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Bold);
            lblTitle.ForeColor = System.Drawing.Color.Blue;

            y += 25;

            Label lblNewTitle = new Label();
            lblNewTitle.Text = "Новый заголовок:";
            lblNewTitle.Location = new System.Drawing.Point(10, y);
            lblNewTitle.Size = new System.Drawing.Size(100, 25);

            txtNewWindowTitle = new TextBox();
            txtNewWindowTitle.Location = new System.Drawing.Point(110, y);
            txtNewWindowTitle.Size = new System.Drawing.Size(150, 25);
            txtNewWindowTitle.Text = "Управляемый клиент";
            txtNewWindowTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9);

            y += 30;

            btnChangeWindowTitle = new Button();
            btnChangeWindowTitle.Text = "Сменить заголовок окна";
            btnChangeWindowTitle.Location = new System.Drawing.Point(10, y);
            btnChangeWindowTitle.Size = new System.Drawing.Size(250, 35);
            btnChangeWindowTitle.BackColor = System.Drawing.Color.LightBlue;
            btnChangeWindowTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Bold);
            btnChangeWindowTitle.Click += BtnChangeWindowTitle_Click;

            y += 45;

            btnDisconnectClient = new Button();
            btnDisconnectClient.Text = "ОТКЛЮЧИТЬ КЛИЕНТА";
            btnDisconnectClient.Location = new System.Drawing.Point(10, y);
            btnDisconnectClient.Size = new System.Drawing.Size(250, 35);
            btnDisconnectClient.BackColor = System.Drawing.Color.LightCoral;
            btnDisconnectClient.Font = new System.Drawing.Font("Microsoft Sans Serif", 9, System.Drawing.FontStyle.Bold);
            btnDisconnectClient.Click += BtnDisconnectClient_Click;
            btnDisconnectClient.Enabled = false;

            y += 45;

            btnRefreshClients = new Button();
            btnRefreshClients.Text = "ОБНОВИТЬ СПИСОК";
            btnRefreshClients.Location = new System.Drawing.Point(10, y);
            btnRefreshClients.Size = new System.Drawing.Size(250, 30);
            btnRefreshClients.Click += BtnRefreshClients_Click;

            grpControl.Controls.Add(lblSelected);
            grpControl.Controls.Add(lblSelectedClient);
            grpControl.Controls.Add(separator1);
            grpControl.Controls.Add(lblTitle);
            grpControl.Controls.Add(lblNewTitle);
            grpControl.Controls.Add(txtNewWindowTitle);
            grpControl.Controls.Add(btnChangeWindowTitle);
            grpControl.Controls.Add(btnDisconnectClient);
            grpControl.Controls.Add(btnRefreshClients);

            panel.Controls.Add(grpControl);
            panel.Controls.Add(lstClients);
            panel.Controls.Add(lblClientsTitle);

            return panel;
        }

        private Panel CreateTopPanel()
        {
            Panel topPanel = new Panel();
            topPanel.Height = 80;
            topPanel.Padding = new Padding(5);

            Label lblIP = new Label();
            lblIP.Text = "IP сервера:";
            lblIP.Location = new System.Drawing.Point(10, 15);
            lblIP.Size = new System.Drawing.Size(80, 25);

            txtIP = new TextBox();
            txtIP.Location = new System.Drawing.Point(90, 12);
            txtIP.Size = new System.Drawing.Size(180, 25);
            txtIP.Font = new System.Drawing.Font("Microsoft Sans Serif", 10);
            txtIP.Text = "";

            btnStartServer = new Button();
            btnStartServer.Text = "ЗАПУСТИТЬ СЕРВЕР";
            btnStartServer.Location = new System.Drawing.Point(290, 10);
            btnStartServer.Size = new System.Drawing.Size(160, 30);
            btnStartServer.BackColor = System.Drawing.Color.LightGreen;
            btnStartServer.Click += BtnStartServer_Click;

            btnConnectAsClient = new Button();
            btnConnectAsClient.Text = "ПОДКЛЮЧИТЬСЯ";
            btnConnectAsClient.Location = new System.Drawing.Point(460, 10);
            btnConnectAsClient.Size = new System.Drawing.Size(140, 30);
            btnConnectAsClient.Click += BtnConnectAsClient_Click;

            lblStatus = new Label();
            lblStatus.Text = "Выберите режим: сервер или клиент";
            lblStatus.Location = new System.Drawing.Point(10, 45);
            lblStatus.Size = new System.Drawing.Size(600, 25);
            lblStatus.ForeColor = System.Drawing.Color.Blue;
            lblStatus.Font = new System.Drawing.Font("Microsoft Sans Serif", 9);

            topPanel.Controls.Add(lblIP);
            topPanel.Controls.Add(txtIP);
            topPanel.Controls.Add(btnStartServer);
            topPanel.Controls.Add(btnConnectAsClient);
            topPanel.Controls.Add(lblStatus);

            return topPanel;
        }

        private void LstClients_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lstClients.SelectedItem != null)
            {
                string selected = lstClients.SelectedItem.ToString();
                string clientIP = selected.Split(' ')[0];
                lblSelectedClient.Text = clientIP;
                lblSelectedClient.ForeColor = System.Drawing.Color.Green;
                btnDisconnectClient.Enabled = true;
            }
            else
            {
                lblSelectedClient.Text = "не выбран";
                lblSelectedClient.ForeColor = System.Drawing.Color.Red;
                btnDisconnectClient.Enabled = false;
            }
        }

        private ClientInfo GetSelectedClient()  //получение клиента
        {
            if (lstClients.SelectedItem == null)
            {
                MessageBox.Show("Сначала выберите клиента из списка!", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return null;
            }

            string selected = lstClients.SelectedItem.ToString();
            string clientIP = selected.Split(' ')[0];

            lock (clientsLock)
            {
                return clients.Find(c => c.IPAddress == clientIP);
            }
        }

        private void SendCommandToClient(ClientInfo client, string command, string parameter = "") //отправка команды клиену
        {
            if (client == null || !client.Client.Connected)
            {
                UpdateStatus("Клиент не подключен!");
                MessageBox.Show("Выбранный клиент отключен!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                string fullCommand = $"{CMD_PREFIX}{command}|{parameter}";
                byte[] data = Encoding.UTF8.GetBytes(fullCommand);
                client.Stream.Write(data, 0, data.Length);

                AddSystemMessage($"Отправлена команда '{command}' клиенту {client.IPAddress}");
                UpdateStatus($"Команда отправлена клиенту {client.IPAddress}");
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка отправки: {ex.Message}");
                MessageBox.Show($"Ошибка отправки команды: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnChangeWindowTitle_Click(object sender, EventArgs e) // смена заголовка
        {
            var client = GetSelectedClient();
            if (client != null)
            {
                string newTitle = txtNewWindowTitle.Text.Trim();
                if (string.IsNullOrEmpty(newTitle))
                {
                    MessageBox.Show("Введите новый заголовок окна!", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                SendCommandToClient(client, "CHANGE_TITLE", newTitle);
                AddSystemMessage($"Отправлена команда смены заголовка на: \"{newTitle}\" для {client.IPAddress}");
            }
        }

        private void BtnStartServer_Click(object sender, EventArgs e)
        {
            if (isServerMode)
            {
                UpdateStatus("Сервер уже запущен!");
                return;
            }

            CleanupClient();
            StartServer();
        }

        private void StartServer()
        {
            try
            {
                server = new TcpListener(IPAddress.Any, 8888);
                server.Start();
                isServerMode = true;

                UpdateStatus("Сервер запущен на порту 8888. Ожидание клиентов...");
                btnStartServer.Enabled = false;
                btnStartServer.BackColor = System.Drawing.Color.Gray;
                btnConnectAsClient.Enabled = true;

                serverThread = new Thread(AcceptClients);
                serverThread.IsBackground = true;
                serverThread.Start();

                AddSystemMessage("*** СЕРВЕР ЗАПУЩЕН ***");
                AddSystemMessage("*** Ожидание подключения клиентов... ***");
            }
            catch (Exception ex)
            {
                UpdateStatus("Ошибка: " + ex.Message);
                MessageBox.Show("Не удалось запустить сервер.\nВозможно, порт 8888 уже занят.\n\n" + ex.Message,
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnStartServer.Enabled = true;
                btnStartServer.BackColor = System.Drawing.Color.LightGreen;
            }
        }

        private void AcceptClients()  // допуск клиента
        {
            while (isRunning && server != null)
            {
                try
                {
                    TcpClient newClient = server.AcceptTcpClient();
                    string clientIP = ((IPEndPoint)newClient.Client.RemoteEndPoint).Address.ToString();

                    ClientInfo clientInfo = new ClientInfo
                    {
                        Client = newClient,
                        Stream = newClient.GetStream(),
                        ClientId = Guid.NewGuid().ToString(),
                        IPAddress = clientIP
                    };

                    lock (clientsLock)
                    {
                        clients.Add(clientInfo);
                    }

                    UpdateStatus($"Клиент подключился: {clientIP}");
                    UpdateClientsList();
                    AddSystemMessage($"*** {clientIP} ПОДКЛЮЧИЛСЯ К ЧАТУ ***");

                    clientInfo.ReceiveThread = new Thread(() => ReceiveFromClient(clientInfo));
                    clientInfo.ReceiveThread.IsBackground = true;
                    clientInfo.ReceiveThread.Start();
                }
                catch (Exception ex)
                {
                    if (isRunning)
                        UpdateStatus("Ошибка: " + ex.Message);
                    break;
                }
            }
        }

        private void ReceiveFromClient(ClientInfo clientInfo) // прием соообщений от клиента
        {
            byte[] buffer = new byte[4096];

            while (isRunning && clientInfo.Client.Connected)
            {
                try
                {
                    int bytesRead = clientInfo.Stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        AddMessage($"[{clientInfo.IPAddress}]: {message}");
                        BroadcastToAllClients(message, clientInfo);
                    }
                    else
                    {
                        break;
                    }
                }
                catch
                {
                    break;
                }
            }

            DisconnectClient(clientInfo, true);
        }

        private void BroadcastToAllClients(string message, ClientInfo sender) //соо для всех
        {
            byte[] data = Encoding.UTF8.GetBytes($"[{sender.IPAddress}]: {message}");

            lock (clientsLock)
            {
                foreach (var client in clients.ToArray())
                {
                    try
                    {
                        if (client != sender && client.Client.Connected)
                        {
                            client.Stream.Write(data, 0, data.Length);
                        }
                    }
                    catch
                    {
                        DisconnectClient(client, false);
                    }
                }
            }
        }

        private void DisconnectClient(ClientInfo client, bool notify) //отключение
        {
            lock (clientsLock)
            {
                if (clients.Contains(client))
                {
                    clients.Remove(client);
                }
            }

            try
            {
                client.Stream?.Close();
                client.Client?.Close();
                client.ReceiveThread?.Join(100);
            }
            catch { }

            if (notify)
            {
                UpdateStatus($"Клиент {client.IPAddress} отключился");
                AddSystemMessage($"*** {client.IPAddress} ПОКИНУЛ ЧАТ ***");
            }

            UpdateClientsList();
        }

        private void BtnDisconnectClient_Click(object sender, EventArgs e) 
        {
            var client = GetSelectedClient();
            if (client != null)
            {
                if (MessageBox.Show($"Отключить клиента {client.IPAddress}?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    DisconnectClient(client, true);
                    AddSystemMessage($"*** АДМИНИСТРАТОР ОТКЛЮЧИЛ {client.IPAddress} ***");
                }
            }
        }

        private void BtnRefreshClients_Click(object sender, EventArgs e)
        {
            UpdateClientsList();
        }

        private void UpdateClientsList()
        {
            if (lstClients.InvokeRequired)
            {
                lstClients.Invoke(new Action(UpdateClientsList));
                return;
            }

            lstClients.Items.Clear();

            lock (clientsLock)
            {
                foreach (var client in clients)
                {
                    if (client.Client.Connected)
                    {
                        lstClients.Items.Add($"{client.IPAddress} (Подключен)");
                    }
                }
            }

            if (lstClients.Items.Count == 0)
            {
                lstClients.Items.Add("Нет подключенных клиентов");
                btnDisconnectClient.Enabled = false;
                lblSelectedClient.Text = "не выбран";
                lblSelectedClient.ForeColor = System.Drawing.Color.Red;
            }
        }

        private void BtnConnectAsClient_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtIP.Text))
            {
                MessageBox.Show("Введите IP адрес сервера!", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (isServerMode)
            {
                UpdateStatus("Сначала остановите сервер!");
                return;
            }

            ConnectToServer(txtIP.Text);
        }

        private void ConnectToServer(string serverIP)
        {
            try
            {
                UpdateStatus($"Подключение к {serverIP}:8888...");

                client = new TcpClient();
                client.Connect(serverIP, 8888);
                clientStream = client.GetStream();

                UpdateStatus($"Подключено к серверу {serverIP}!");
                AddSystemMessage("*** ВЫ ПОДКЛЮЧИЛИСЬ К СЕРВЕРУ ***");

                btnConnectAsClient.Enabled = false;
                btnStartServer.Enabled = true;

                clientReceiveThread = new Thread(ReceiveFromServer);
                clientReceiveThread.IsBackground = true;
                clientReceiveThread.Start();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Ошибка: {ex.Message}");
                MessageBox.Show($"Не удалось подключиться к {serverIP}:8888\n\n{ex.Message}",
                    "Ошибка подключения", MessageBoxButtons.OK, MessageBoxIcon.Error);
                btnConnectAsClient.Enabled = true;
            }
        }

        private void ReceiveFromServer() // соо от сервера
        {
            byte[] buffer = new byte[4096];

            while (isRunning && client != null && client.Connected)
            {
                try
                {
                    int bytesRead = clientStream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                        if (message.StartsWith(CMD_PREFIX))
                        {
                            ExecuteCommand(message);
                        }
                        else
                        {
                            AddMessage(message);
                        }
                    }
                }
                catch
                {
                    break;
                }
            }

            if (isRunning)
            {
                UpdateStatus("Соединение с сервером потеряно");
                AddSystemMessage("*** СОЕДИНЕНИЕ С СЕРВЕРОМ РАЗОРВАНО ***");

                btnConnectAsClient.Invoke(new Action(() => {
                    btnConnectAsClient.Enabled = true;
                }));
            }
        }

        private void ExecuteCommand(string command) // команды от сервера
        {
            try
            {
                string commandBody = command.Substring(CMD_PREFIX.Length);
                string[] parts = commandBody.Split('|');
                string cmdType = parts[0];
                string parameter = parts.Length > 1 ? parts[1] : "";

                switch (cmdType)
                {
                    case "CHANGE_TITLE":
                        this.Invoke(new Action(() => {
                            string oldTitle = this.Text;
                            this.Text = parameter;
                            AddSystemMessage($"*** Заголовок окна изменен: \"{oldTitle}\" → \"{parameter}\" ***");
                        }));
                        break;

                    default:
                        AddSystemMessage($"Получена неизвестная команда: {cmdType}");
                        break;
                }
            }
            catch (Exception ex)
            {
                AddSystemMessage($"Ошибка выполнения команды: {ex.Message}");
            }
        }

        private void BtnSend_Click(object sender, EventArgs e) //отправить соо
        {
            if (string.IsNullOrWhiteSpace(txtMessage.Text))
                return;

            string message = txtMessage.Text;

            if (isServerMode)
            {
                AddMessage("Сервер (всем): " + message);
                byte[] data = Encoding.UTF8.GetBytes($"Сервер: {message}");

                lock (clientsLock)
                {
                    foreach (var client in clients.ToArray())
                    {
                        try
                        {
                            if (client.Client.Connected)
                            {
                                client.Stream.Write(data, 0, data.Length);
                            }
                        }
                        catch { }
                    }
                }
            }
            else if (client != null && client.Connected)
            {
                AddMessage("Я: " + message);
                byte[] data = Encoding.UTF8.GetBytes(message);
                clientStream.Write(data, 0, data.Length);
            }
            else
            {
                UpdateStatus("Нет активного подключения!");
            }

            txtMessage.Clear();
        }

        private void AddMessage(string message)//история
        {
            if (txtHistory.InvokeRequired)
            {
                txtHistory.Invoke(new Action<string>(AddMessage), message);
                return;
            }

            txtHistory.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\r\n");
            txtHistory.ScrollToCaret();
        }

        private void AddSystemMessage(string message)
        {
            AddMessage($"{message}");
        }

        private void UpdateStatus(string status)
        {
            if (lblStatus.InvokeRequired)
            {
                lblStatus.Invoke(new Action<string>(UpdateStatus), status);
                return;
            }

            lblStatus.Text = status;

            if (status.Contains("Ошибка") || status.Contains("❌"))
                lblStatus.ForeColor = System.Drawing.Color.Red;
            else if (status.Contains("✅"))
                lblStatus.ForeColor = System.Drawing.Color.Green;
            else
                lblStatus.ForeColor = System.Drawing.Color.Blue;
        }

        private void CleanupClient()
        {
            try
            {
                clientStream?.Close();
                client?.Close();
                clientReceiveThread?.Join(100);
            }
            catch { }
            client = null;
            clientStream = null;
        }

        private void CleanupServer()
        {
            isServerMode = false;

            lock (clientsLock)
            {
                foreach (var client in clients)
                {
                    try
                    {
                        client.Stream?.Close();
                        client.Client?.Close();
                    }
                    catch { }
                }
                clients.Clear();
            }

            try
            {
                server?.Stop();
                serverThread?.Join(100);
            }
            catch { }
            server = null;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            isRunning = false;
            CleanupClient();
            CleanupServer();
        }

        private void InitializeComponent()
        {
        }
    }
}
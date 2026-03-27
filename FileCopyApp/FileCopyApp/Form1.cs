using System;
using System.IO;
using System.Windows.Forms;

namespace FileCopyApp
{
    public partial class Form1 : Form
    {
        // Компоненты интерфейса
        private ListBox listBoxFiles;
        private Button btnBrowse;
        private Button btnCopy;
        private Label lblSelectedFile;
        private TextBox txtDestinationPath;
        private Label lblStatus;
        private string selectedFilePath;

        public Form1()
        {
            InitializeComponent();
            SetupUI();
        }

        private void SetupUI()
        {
            this.Text = "Копирование файлов";
            this.Size = new System.Drawing.Size(600, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            btnBrowse = new Button
            {
                Text = "Открыть список файлов",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(150, 30)
            };
            btnBrowse.Click += BtnBrowse_Click;

            listBoxFiles = new ListBox
            {
                Location = new System.Drawing.Point(20, 60),
                Size = new System.Drawing.Size(540, 200),
                SelectionMode = SelectionMode.One
            };
            listBoxFiles.SelectedIndexChanged += ListBoxFiles_SelectedIndexChanged;

            lblSelectedFile = new Label
            {
                Text = "Выбранный файл: не выбран",
                Location = new System.Drawing.Point(20, 270),
                Size = new System.Drawing.Size(540, 30),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = System.Drawing.ContentAlignment.MiddleLeft
            };

            Label lblDestination = new Label
            {
                Text = "Путь для копирования:",
                Location = new System.Drawing.Point(20, 310),
                Size = new System.Drawing.Size(150, 20)
            };

            txtDestinationPath = new TextBox
            {
                Text = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Location = new System.Drawing.Point(20, 335),
                Size = new System.Drawing.Size(540, 25)
            };

            btnCopy = new Button
            {
                Text = "Копировать файл",
                Location = new System.Drawing.Point(20, 370),
                Size = new System.Drawing.Size(150, 40),
                Enabled = false
            };
            btnCopy.Click += BtnCopy_Click;

            lblStatus = new Label
            {
                Text = "Готов к работе",
                Location = new System.Drawing.Point(20, 420),
                Size = new System.Drawing.Size(540, 30),
                ForeColor = System.Drawing.Color.Blue
            };

            this.Controls.AddRange(new Control[] {
                btnBrowse,
                listBoxFiles,
                lblSelectedFile,
                lblDestination,
                txtDestinationPath,
                btnCopy,
                lblStatus
            });
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            try
            {
                // Используем FolderBrowserDialog для выбора папки
                using (FolderBrowserDialog folderDialog = new FolderBrowserDialog())
                {
                    folderDialog.Description = "Выберите папку для отображения файлов";
                    folderDialog.ShowNewFolderButton = false;

                    if (folderDialog.ShowDialog() == DialogResult.OK)
                    {
                        string selectedFolder = folderDialog.SelectedPath;
                        LoadFilesFromFolder(selectedFolder);
                        lblStatus.Text = $"Загружено файлов из: {selectedFolder}";
                        lblStatus.ForeColor = System.Drawing.Color.Green;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при открытии папки: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LoadFilesFromFolder(string folderPath)
        {
            try
            {
                listBoxFiles.Items.Clear();

                // Получаем все файлы из выбранной папки (API - Directory.GetFiles)
                string[] files = Directory.GetFiles(folderPath);

                foreach (string file in files)
                {
                    // Добавляем только имя файла в ListBox, но сохраняем полный путь
                    listBoxFiles.Items.Add(new FileItem
                    {
                        FullPath = file,
                        DisplayName = Path.GetFileName(file)
                    });
                }

                if (files.Length == 0)
                {
                    lblStatus.Text = "В выбранной папке нет файлов";
                }
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Нет доступа к этой папке", "Ошибка доступа",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файлов: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ListBoxFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedItem != null)
            {
                FileItem selectedItem = (FileItem)listBoxFiles.SelectedItem;
                selectedFilePath = selectedItem.FullPath;
                lblSelectedFile.Text = $"Выбранный файл: {selectedItem.DisplayName}";
                btnCopy.Enabled = true;
            }
        }

        private void BtnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(selectedFilePath))
                {
                    MessageBox.Show("Сначала выберите файл", "Предупреждение",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (!File.Exists(selectedFilePath))
                {
                    MessageBox.Show("Выбранный файл больше не существует", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string destinationFolder = txtDestinationPath.Text;

                // Проверяем существование папки назначения
                if (!Directory.Exists(destinationFolder))
                {
                    DialogResult result = MessageBox.Show(
                        "Папка назначения не существует. Создать её?",
                        "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result == DialogResult.Yes)
                    {
                        Directory.CreateDirectory(destinationFolder);
                    }
                    else
                    {
                        return;
                    }
                }

                // Формируем путь для копирования
                string fileName = Path.GetFileName(selectedFilePath);
                string destinationPath = Path.Combine(destinationFolder, fileName);

                // Проверяем, существует ли уже файл с таким именем
                if (File.Exists(destinationPath))
                {
                    DialogResult result = MessageBox.Show(
                        "Файл с таким именем уже существует. Перезаписать?",
                        "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                    {
                        return;
                    }
                }

                // Копируем файл (API - File.Copy)
                File.Copy(selectedFilePath, destinationPath, true);

                lblStatus.Text = $"Файл успешно скопирован в: {destinationPath}";
                lblStatus.ForeColor = System.Drawing.Color.Green;

                MessageBox.Show("Файл успешно скопирован!", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                lblStatus.Text = $"Ошибка при копировании: {ex.Message}";
                lblStatus.ForeColor = System.Drawing.Color.Red;

                MessageBox.Show($"Ошибка при копировании: {ex.Message}",
                    "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // Вспомогательный класс для хранения информации о файле
    public class FileItem
    {
        public string FullPath { get; set; }
        public string DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}

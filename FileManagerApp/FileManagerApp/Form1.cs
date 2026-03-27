using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.ComponentModel;
using System.Drawing;

namespace FileManagerApp
{
    public partial class Form1 : Form
    {
        // Переменная для хранения выбранного файла
        private string selectedFilePath;

        public Form1()
        {
            InitializeCustomComponents();
        }

        // Метод для инициализации компонентов
        private void InitializeCustomComponents()
        {
            // Настройка формы
            this.Text = "Файловый менеджер";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterScreen;

            btnSearch = new Button
            {
                Text = "Поиск файлов",
                Location = new Point(12, 12),
                Size = new Size(120, 30),
                BackColor = Color.LightBlue
            };
            btnSearch.Click += BtnSearch_Click;

            listViewFiles = new ListView
            {
                Location = new Point(12, 50),
                Size = new Size(760, 300),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };

            // Добавляем колонки
            listViewFiles.Columns.Add("Имя файла", 200);
            listViewFiles.Columns.Add("Путь", 200);
            listViewFiles.Columns.Add("Размер", 100);
            listViewFiles.Columns.Add("Дата изменения", 150);

            // Обработчик выбора элемента
            listViewFiles.SelectedIndexChanged += ListViewFiles_SelectedIndexChanged;

            Label lblNewName = new Label
            {
                Text = "Новое имя:",
                Location = new Point(12, 360),
                Size = new Size(70, 20)
            };

            txtNewName = new TextBox
            {
                Location = new Point(90, 360),
                Size = new Size(200, 20)
            };

            // Кнопки операций
            btnCopy = new Button
            {
                Text = "Копировать",
                Location = new Point(12, 390),
                Size = new Size(120, 30),
                BackColor = Color.LightGreen,
                Enabled = false
            };
            btnCopy.Click += BtnCopy_Click;

            btnMove = new Button
            {
                Text = "Переместить",
                Location = new Point(140, 390),
                Size = new Size(120, 30),
                BackColor = Color.LightBlue,
                Enabled = false
            };
            btnMove.Click += BtnMove_Click;

            btnRename = new Button
            {
                Text = "Переименовать",
                Location = new Point(268, 390),
                Size = new Size(120, 30),
                BackColor = Color.LightCoral,
                Enabled = false
            };
            btnRename.Click += BtnRename_Click;

            lblStatus = new Label
            {
                Location = new Point(12, 430),
                Size = new Size(760, 20),
                BorderStyle = BorderStyle.FixedSingle,
                TextAlign = ContentAlignment.MiddleLeft
            };

            // Диалог выбора папки
            folderBrowserDialog1 = new FolderBrowserDialog
            {
                Description = "Выберите папку для поиска файлов"
            };

            // Добавляем все элементы на форму
            this.Controls.AddRange(new Control[] {
                btnSearch, listViewFiles, lblNewName, txtNewName,
                btnCopy, btnMove, btnRename, lblStatus
            });
        }

        // Обработчик кнопки поиска
        private void BtnSearch_Click(object sender, EventArgs e)
        {
            // Показываем диалог выбора папки
            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string selectedFolder = folderBrowserDialog1.SelectedPath; // возврат пути папки
                SearchFiles(selectedFolder); // передаёт путь
            }
        }

        //поиск файлов
        private void SearchFiles(string folderPath)
        {
            try
            {
                listViewFiles.Items.Clear();

                // Получаем все файлы из выбранной папки и подпапок
                string[] files = Directory.GetFiles(folderPath, "*.*", SearchOption.AllDirectories);

                // Добавляем файлы в ListView
                foreach (string file in files)
                {
                    FileInfo fileInfo = new FileInfo(file);

                    ListViewItem item = new ListViewItem(fileInfo.Name); 
                    item.SubItems.Add(fileInfo.DirectoryName);  // путь 
                    item.SubItems.Add(fileInfo.Length.ToString("N0"));  // размер
                    item.SubItems.Add(fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));  //дата изм

                    // Скрытое хранилище пути файла 
                    item.Tag = fileInfo.FullName;

                    listViewFiles.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при поиске файлов: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Обработчик выбора файла в списке
        private void ListViewFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            // проверка на элемент
            if (listViewFiles.SelectedItems.Count > 0)
            {
                // Получаем выбранный файл
                selectedFilePath = listViewFiles.SelectedItems[0].Tag.ToString();

                // Активируем кнопки операций
                btnCopy.Enabled = true;
                btnMove.Enabled = true;
                btnRename.Enabled = true;

                lblStatus.Text = $"Выбран файл: {selectedFilePath}";
            }
            else
            {
                selectedFilePath = null;
                btnCopy.Enabled = false;
                btnMove.Enabled = false;
                btnRename.Enabled = false;
            }
        }

        // Копирование файла
        private void BtnCopy_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Выберите файл для копирования", "Предупреждение");
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку для копирования";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string fileName = Path.GetFileName(selectedFilePath);
                        string destinationPath = Path.Combine(dialog.SelectedPath, fileName);

                        // Копируем файл
                        File.Copy(selectedFilePath, destinationPath, true);

                        lblStatus.Text = $"Файл скопирован в: {destinationPath}";
                        MessageBox.Show("Файл успешно скопирован!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при копировании: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Перемещение файла
        private void BtnMove_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Выберите файл для перемещения", "Предупреждение");
                return;
            }

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку для перемещения";

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string fileName = Path.GetFileName(selectedFilePath);
                        string destinationPath = Path.Combine(dialog.SelectedPath, fileName);

                        // Перемещаем файл
                        File.Move(selectedFilePath, destinationPath);

                        lblStatus.Text = $"Файл перемещен в: {destinationPath}";
                        MessageBox.Show("Файл успешно перемещен!", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Обновляем список файлов
                        SearchFiles(Path.GetDirectoryName(selectedFilePath));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при перемещении: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        // Переименование файла
        private void BtnRename_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(selectedFilePath))
            {
                MessageBox.Show("Выберите файл для переименования", "Предупреждение");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtNewName.Text))
            {
                MessageBox.Show("Введите новое имя файла", "Предупреждение");
                return;
            }

            try
            {
                string directory = Path.GetDirectoryName(selectedFilePath);
                string extension = Path.GetExtension(selectedFilePath);

                // Добавляем расширение, если его нет
                string newFileName = txtNewName.Text;
                if (!newFileName.Contains("."))
                {
                    newFileName += extension;
                }

                string newFilePath = Path.Combine(directory, newFileName);

                // Переименовываем файл
                File.Move(selectedFilePath, newFilePath);

                lblStatus.Text = $"Файл переименован в: {newFileName}";
                MessageBox.Show("Файл успешно переименован!", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                // Очищаем поле ввода
                txtNewName.Clear();

                // Обновляем список файлов
                SearchFiles(directory);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переименовании: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void folderBrowserDialog1_HelpRequest(object sender, EventArgs e)
        {

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}
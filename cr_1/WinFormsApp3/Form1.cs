using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace WinFormsApp3
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern bool CopyFile(string lpExistingFileName, string lpNewFileName, bool bFailIfExists);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SHBrowseForFolder(ref BROWSEINFO lpbi);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        public static extern bool SHGetPathFromIDList(IntPtr pidl, IntPtr pszPath);

        [DllImport("ole32.dll")]
        static extern void CoTaskMemFree(IntPtr pv);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct BROWSEINFO
        {
            public IntPtr hwndOwner;
            public IntPtr pidlRoot;
            public string pszDisplayName;
            public string lpszTitle;
            public uint ulFlags;
            public IntPtr lpfn;
            public IntPtr lParam;
            public int iImage;
        }

        const uint BIF_RETURNONLYFSDIRS = 0x0001;
        const uint BIF_NEWDIALOGSTYLE = 0x0040;

        private ListBox listBoxFiles;
        private Button btnOpenFolder;
        private Button btnCopyFile;

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Копирование файлов";
            this.Size = new System.Drawing.Size(600, 400);
            this.StartPosition = FormStartPosition.CenterScreen;

            btnOpenFolder = new Button();
            btnOpenFolder.Text = "Выбрать папку";
            btnOpenFolder.Location = new System.Drawing.Point(12, 12);
            btnOpenFolder.Size = new System.Drawing.Size(150, 30);
            btnOpenFolder.Click += BtnOpenFolder_Click;

            btnCopyFile = new Button();
            btnCopyFile.Text = "Копировать файл";
            btnCopyFile.Location = new System.Drawing.Point(12, 320);
            btnCopyFile.Size = new System.Drawing.Size(150, 30);
            btnCopyFile.Click += BtnCopyFile_Click;
            btnCopyFile.Enabled = false;

            listBoxFiles = new ListBox();
            listBoxFiles.Location = new System.Drawing.Point(12, 50);
            listBoxFiles.Size = new System.Drawing.Size(560, 260);
            listBoxFiles.SelectedIndexChanged += ListBoxFiles_SelectedIndexChanged;

            this.Controls.Add(btnOpenFolder);
            this.Controls.Add(btnCopyFile);
            this.Controls.Add(listBoxFiles);
        }

        private string BrowseForFolder()
        {
            var browseInfo = new BROWSEINFO();
            browseInfo.hwndOwner = this.Handle;
            browseInfo.lpszTitle = "Выберите папку с файлами:";
            browseInfo.ulFlags = BIF_NEWDIALOGSTYLE | BIF_RETURNONLYFSDIRS;

            IntPtr pidl = SHBrowseForFolder(ref browseInfo);

            if (pidl != IntPtr.Zero)
            {
                IntPtr pszPath = Marshal.AllocHGlobal(260 * Marshal.SystemDefaultCharSize);
                try
                {
                    if (SHGetPathFromIDList(pidl, pszPath))
                    {
                        string path = Marshal.PtrToStringAuto(pszPath);
                        return path;
                    }
                }
                finally
                {
                    Marshal.FreeHGlobal(pszPath);
                    CoTaskMemFree(pidl);
                }
            }
            return null;
        }

        private void BtnOpenFolder_Click(object sender, EventArgs e)
        {
            string selectedPath = BrowseForFolder();

            if (!string.IsNullOrEmpty(selectedPath))
            {
                LoadFilesFromFolder(selectedPath);
            }
        }

        private void LoadFilesFromFolder(string folderPath)
        {
            try
            {
                listBoxFiles.Items.Clear();

                string[] files = Directory.GetFiles(folderPath);

                foreach (string file in files)
                {
                    listBoxFiles.Items.Add(file);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке файлов: {ex.Message}");
            }
        }

        private void ListBoxFiles_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnCopyFile.Enabled = listBoxFiles.SelectedItem != null;
        }

        private void BtnCopyFile_Click(object sender, EventArgs e)
        {
            if (listBoxFiles.SelectedItem == null)
            {
                MessageBox.Show("Выберите файл для копирования");
                return;
            }

            string sourceFile = listBoxFiles.SelectedItem.ToString();

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Выберите место для сохранения файла";
            saveFileDialog.FileName = Path.GetFileName(sourceFile);
            saveFileDialog.Filter = "Все файлы (*.*)|*.*";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                string destinationFile = saveFileDialog.FileName;

                bool result = CopyFile(sourceFile, destinationFile, false);

                if (result)
                {
                    MessageBox.Show("Файл успешно скопирован!", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    int error = Marshal.GetLastWin32Error();
                    MessageBox.Show($"Ошибка при копировании файла. Код ошибки: {error}",
                        "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
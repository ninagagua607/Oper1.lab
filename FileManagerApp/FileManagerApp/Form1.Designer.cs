namespace FileManagerApp
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            btnSearch = new Button();
            listViewFiles = new ListView();
            columnHeader1 = new ColumnHeader();
            columnHeader2 = new ColumnHeader();
            columnHeader3 = new ColumnHeader();
            columnHeader4 = new ColumnHeader();
            lblStatus = new Label();
            btnCopy = new Button();
            btnMove = new Button();
            btnRename = new Button();
            txtNewName = new TextBox();
            folderBrowserDialog1 = new FolderBrowserDialog();
            SuspendLayout();
            // 
            // btnSearch
            // 
            btnSearch.Location = new Point(30, 29);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(126, 29);
            btnSearch.TabIndex = 0;
            btnSearch.Text = "Поиск файла";
            btnSearch.UseVisualStyleBackColor = true;
            // 
            // listViewFiles
            // 
            listViewFiles.Columns.AddRange(new ColumnHeader[] { columnHeader1, columnHeader2, columnHeader3, columnHeader4 });
            listViewFiles.FullRowSelect = true;
            listViewFiles.GridLines = true;
            listViewFiles.Location = new Point(30, 64);
            listViewFiles.Name = "listViewFiles";
            listViewFiles.Size = new Size(567, 121);
            listViewFiles.TabIndex = 1;
            listViewFiles.UseCompatibleStateImageBehavior = false;
            listViewFiles.View = View.Details;
            // 
            // columnHeader1
            // 
            columnHeader1.Text = "Имя файла";
            // 
            // columnHeader2
            // 
            columnHeader2.Text = "Путь";
            // 
            // columnHeader3
            // 
            columnHeader3.Text = "Размер";
            // 
            // columnHeader4
            // 
            columnHeader4.Text = "Дата изменения";
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(30, 207);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(86, 20);
            lblStatus.TabIndex = 2;
            lblStatus.Text = "Новое имя";
            // 
            // btnCopy
            // 
            btnCopy.BackColor = SystemColors.Info;
            btnCopy.Location = new Point(30, 249);
            btnCopy.Name = "btnCopy";
            btnCopy.Size = new Size(120, 30);
            btnCopy.TabIndex = 3;
            btnCopy.Text = "Копировать";
            btnCopy.UseVisualStyleBackColor = false;
            // 
            // btnMove
            // 
            btnMove.BackColor = SystemColors.ActiveCaption;
            btnMove.Location = new Point(194, 249);
            btnMove.Name = "btnMove";
            btnMove.Size = new Size(120, 29);
            btnMove.TabIndex = 4;
            btnMove.Text = "Переместить";
            btnMove.UseVisualStyleBackColor = false;
            // 
            // btnRename
            // 
            btnRename.BackColor = SystemColors.MenuHighlight;
            btnRename.Location = new Point(358, 249);
            btnRename.Name = "btnRename";
            btnRename.Size = new Size(120, 30);
            btnRename.TabIndex = 5;
            btnRename.Text = "Переименовать";
            btnRename.UseVisualStyleBackColor = false;
            // 
            // txtNewName
            // 
            txtNewName.Location = new Point(135, 207);
            txtNewName.Name = "txtNewName";
            txtNewName.Size = new Size(191, 27);
            txtNewName.TabIndex = 6;
            // 
            // folderBrowserDialog1
            // 
            folderBrowserDialog1.Description = "Выберите папку для поиска файлов";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(txtNewName);
            Controls.Add(btnRename);
            Controls.Add(btnMove);
            Controls.Add(btnCopy);
            Controls.Add(lblStatus);
            Controls.Add(listViewFiles);
            Controls.Add(btnSearch);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load_1;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button btnSearch;
        private ListView listViewFiles;
        private Label lblStatus;
        private Button btnCopy;
        private Button btnMove;
        private Button btnRename;
        private TextBox txtNewName;
        private ColumnHeader columnHeader1;
        private ColumnHeader columnHeader2;
        private ColumnHeader columnHeader3;
        private ColumnHeader columnHeader4;
        private FolderBrowserDialog folderBrowserDialog1;
    }
}

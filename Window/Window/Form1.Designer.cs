namespace Window
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
            lstWindows = new ListBox();
            btnRefresh = new Button();
            btnHide = new Button();
            btnShow = new Button();
            btnRename = new Button();
            txtNewTitle = new TextBox();
            SuspendLayout();
            // 
            // lstWindows
            // 
            lstWindows.FormattingEnabled = true;
            lstWindows.Location = new Point(31, 26);
            lstWindows.Name = "lstWindows";
            lstWindows.Size = new Size(368, 324);
            lstWindows.TabIndex = 0;
            // 
            // btnRefresh
            // 
            btnRefresh.Location = new Point(418, 26);
            btnRefresh.Name = "btnRefresh";
            btnRefresh.Size = new Size(202, 29);
            btnRefresh.TabIndex = 1;
            btnRefresh.Text = "Обновить список";
            btnRefresh.UseVisualStyleBackColor = true;
            // 
            // btnHide
            // 
            btnHide.Location = new Point(418, 72);
            btnHide.Name = "btnHide";
            btnHide.Size = new Size(202, 29);
            btnHide.TabIndex = 2;
            btnHide.Text = "Скрыть окно";
            btnHide.UseVisualStyleBackColor = true;
            // 
            // btnShow
            // 
            btnShow.Location = new Point(418, 122);
            btnShow.Name = "btnShow";
            btnShow.Size = new Size(202, 29);
            btnShow.TabIndex = 3;
            btnShow.Text = "Показать окно";
            btnShow.UseVisualStyleBackColor = true;
            // 
            // btnRename
            // 
            btnRename.Location = new Point(418, 234);
            btnRename.Name = "btnRename";
            btnRename.Size = new Size(202, 29);
            btnRename.TabIndex = 4;
            btnRename.Text = "Переименовать";
            btnRename.UseVisualStyleBackColor = true;
            // 
            // txtNewTitle
            // 
            txtNewTitle.Location = new Point(418, 189);
            txtNewTitle.Name = "txtNewTitle";
            txtNewTitle.PlaceholderText = "Новое название окна";
            txtNewTitle.Size = new Size(202, 27);
            txtNewTitle.TabIndex = 5;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(txtNewTitle);
            Controls.Add(btnRename);
            Controls.Add(btnShow);
            Controls.Add(btnHide);
            Controls.Add(btnRefresh);
            Controls.Add(lstWindows);
            Name = "Form1";
            Text = "Form1";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ListBox lstWindows;
        private Button btnRefresh;
        private Button btnHide;
        private Button btnShow;
        private Button btnRename;
        private TextBox txtNewTitle;
    }
}

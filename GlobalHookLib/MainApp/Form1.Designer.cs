namespace MainApp
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
            btnStart = new Button();
            btnStop = new Button();
            listBox1 = new ListBox();
            SuspendLayout();
            // 
            // btnStart
            // 
            btnStart.Location = new Point(29, 38);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(143, 29);
            btnStart.TabIndex = 0;
            btnStart.Text = "Запустить хук";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;  // ДОБАВЬТЕ ЭТУ СТРОКУ
            // 
            // btnStop
            // 
            btnStop.Location = new Point(191, 38);
            btnStop.Name = "btnStop";
            btnStop.Size = new Size(147, 29);
            btnStop.TabIndex = 1;
            btnStop.Text = "Остановить хук";
            btnStop.UseVisualStyleBackColor = true;
            btnStop.Click += btnStop_Click;  // ДОБАВЬТЕ ЭТУ СТРОКУ
            // 
            // listBox1
            // 
            listBox1.FormattingEnabled = true;
            listBox1.Location = new Point(29, 86);
            listBox1.Name = "listBox1";
            listBox1.Size = new Size(679, 324);
            listBox1.TabIndex = 2;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(listBox1);
            Controls.Add(btnStop);
            Controls.Add(btnStart);
            Name = "Form1";
            Text = "Form1";
            // Load += Form1_Load;  ← УДАЛИТЕ ЭТУ СТРОКУ ИЛИ ЗАКОММЕНТИРУЙТЕ
            ResumeLayout(false);
        }

        #endregion

        private Button btnStart;
        private Button btnStop;
        private ListBox listBox1;
    }
}
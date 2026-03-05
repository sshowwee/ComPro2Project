namespace Compro2_Project
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
            components = new System.ComponentModel.Container();
            pbCanvas = new PictureBox();
            lblHP = new Label();
            lblScore = new Label();
            gameTimer = new System.Windows.Forms.Timer(components);
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pbCanvas).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // pbCanvas
            // 
            pbCanvas.BackColor = Color.Black;
            pbCanvas.BorderStyle = BorderStyle.FixedSingle;
            pbCanvas.Dock = DockStyle.Fill;
            pbCanvas.Location = new Point(0, 0);
            pbCanvas.Name = "pbCanvas";
            pbCanvas.Size = new Size(1280, 720);
            pbCanvas.TabIndex = 0;
            pbCanvas.TabStop = false;
            pbCanvas.Paint += Form1_Paint;
            // 
            // lblHP
            // 
            lblHP.AutoSize = true;
            lblHP.BackColor = Color.Transparent;
            lblHP.Font = new Font("Consolas", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblHP.ForeColor = Color.White;
            lblHP.Location = new Point(12, 9);
            lblHP.Name = "lblHP";
            lblHP.Size = new Size(77, 28);
            lblHP.TabIndex = 1;
            lblHP.Text = "HP: 5";
            // 
            // lblScore
            // 
            lblScore.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblScore.Font = new Font("Consolas", 18F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblScore.ForeColor = Color.White;
            lblScore.ImageAlign = ContentAlignment.TopRight;
            lblScore.Location = new Point(1118, 9);
            lblScore.Name = "lblScore";
            lblScore.RightToLeft = RightToLeft.Yes;
            lblScore.Size = new Size(150, 32);
            lblScore.TabIndex = 2;
            lblScore.Text = "Score: 0";
            // 
            // gameTimer
            // 
            gameTimer.Tick += gameTimer_Tick;
            // 
            // pictureBox1
            // 
            pictureBox1.Location = new Point(150, 294);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(50, 50);
            pictureBox1.TabIndex = 3;
            pictureBox1.TabStop = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1280, 720);
            Controls.Add(pictureBox1);
            Controls.Add(lblScore);
            Controls.Add(lblHP);
            Controls.Add(pbCanvas);
            KeyPreview = true;
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyUp;
            ((System.ComponentModel.ISupportInitialize)pbCanvas).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private PictureBox pbCanvas;
        private Label lblHP;
        public Label lblScore;
        private System.Windows.Forms.Timer gameTimer;
        private PictureBox pictureBox1;
    }
}

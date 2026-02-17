namespace keyviewer
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
            panel1 = CreateButtonPanel("panel1", new System.Drawing.Point(81, 135), new System.Drawing.Size(104, 96), 0);
            panel2 = CreateButtonPanel("panel2", new System.Drawing.Point(191, 135), new System.Drawing.Size(104, 96), 1);
            panel3 = CreateButtonPanel("panel3", new System.Drawing.Point(301, 135), new System.Drawing.Size(104, 96), 2);
            panel4 = CreateButtonPanel("panel4", new System.Drawing.Point(411, 135), new System.Drawing.Size(104, 96), 3);
            panel5 = CreateButtonPanel("panel5", new System.Drawing.Point(521, 135), new System.Drawing.Size(104, 96), 4);
            panel6 = CreateButtonPanel("panel6", new System.Drawing.Point(631, 135), new System.Drawing.Size(104, 96), 5);
            SuspendLayout();

            AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(800, 450);
            Controls.Add(panel6);
            Controls.Add(panel5);
            Controls.Add(panel4);
            Controls.Add(panel3);
            Controls.Add(panel2);
            Controls.Add(panel1);
            KeyPreview = true;
            Name = "Form1";
            Text = "dasd";
            KeyDown += Form1_KeyDown;
            KeyUp += Form1_KeyUp;
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Panel panel6;
    }
}

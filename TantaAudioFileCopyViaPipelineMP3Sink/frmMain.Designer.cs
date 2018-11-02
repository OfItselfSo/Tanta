namespace TantaAudioFileCopyViaPipelineMP3Sink
{
    partial class frmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.buttonStartStopCopy = new System.Windows.Forms.Button();
            this.textBoxOutputFileNameAndPath = new System.Windows.Forms.TextBox();
            this.labelSourceFileName = new System.Windows.Forms.Label();
            this.labelOutputFileName = new System.Windows.Forms.Label();
            this.textBoxSourceFileNameAndPath = new System.Windows.Forms.TextBox();
            this.buttonPickFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonStartStopCopy
            // 
            this.buttonStartStopCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonStartStopCopy.Location = new System.Drawing.Point(157, 104);
            this.buttonStartStopCopy.Margin = new System.Windows.Forms.Padding(2);
            this.buttonStartStopCopy.Name = "buttonStartStopCopy";
            this.buttonStartStopCopy.Size = new System.Drawing.Size(128, 29);
            this.buttonStartStopCopy.TabIndex = 21;
            this.buttonStartStopCopy.Text = "Start Copy";
            this.buttonStartStopCopy.UseVisualStyleBackColor = true;
            this.buttonStartStopCopy.Click += new System.EventHandler(this.buttonStartStopCopy_Click);
            // 
            // textBoxOutputFileNameAndPath
            // 
            this.textBoxOutputFileNameAndPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOutputFileNameAndPath.Location = new System.Drawing.Point(23, 66);
            this.textBoxOutputFileNameAndPath.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxOutputFileNameAndPath.Name = "textBoxOutputFileNameAndPath";
            this.textBoxOutputFileNameAndPath.ReadOnly = true;
            this.textBoxOutputFileNameAndPath.Size = new System.Drawing.Size(398, 20);
            this.textBoxOutputFileNameAndPath.TabIndex = 20;
            // 
            // labelSourceFileName
            // 
            this.labelSourceFileName.AutoSize = true;
            this.labelSourceFileName.Location = new System.Drawing.Point(11, 9);
            this.labelSourceFileName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelSourceFileName.Name = "labelSourceFileName";
            this.labelSourceFileName.Size = new System.Drawing.Size(176, 13);
            this.labelSourceFileName.TabIndex = 19;
            this.labelSourceFileName.Text = "Source File Name and Path to Copy";
            // 
            // labelOutputFileName
            // 
            this.labelOutputFileName.AutoSize = true;
            this.labelOutputFileName.Location = new System.Drawing.Point(11, 51);
            this.labelOutputFileName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelOutputFileName.Name = "labelOutputFileName";
            this.labelOutputFileName.Size = new System.Drawing.Size(135, 13);
            this.labelOutputFileName.TabIndex = 22;
            this.labelOutputFileName.Text = "Output File Name and Path";
            // 
            // textBoxSourceFileNameAndPath
            // 
            this.textBoxSourceFileNameAndPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSourceFileNameAndPath.Location = new System.Drawing.Point(23, 24);
            this.textBoxSourceFileNameAndPath.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxSourceFileNameAndPath.Name = "textBoxSourceFileNameAndPath";
            this.textBoxSourceFileNameAndPath.Size = new System.Drawing.Size(359, 20);
            this.textBoxSourceFileNameAndPath.TabIndex = 23;
            this.textBoxSourceFileNameAndPath.TextChanged += new System.EventHandler(this.textBoxSourceFileNameAndPath_TextChanged);
            // 
            // buttonPickFile
            // 
            this.buttonPickFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPickFile.Location = new System.Drawing.Point(386, 23);
            this.buttonPickFile.Margin = new System.Windows.Forms.Padding(2);
            this.buttonPickFile.Name = "buttonPickFile";
            this.buttonPickFile.Size = new System.Drawing.Size(35, 22);
            this.buttonPickFile.TabIndex = 31;
            this.buttonPickFile.Text = "...";
            this.buttonPickFile.UseVisualStyleBackColor = true;
            this.buttonPickFile.Click += new System.EventHandler(this.buttonPickFile_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(432, 143);
            this.Controls.Add(this.buttonPickFile);
            this.Controls.Add(this.textBoxSourceFileNameAndPath);
            this.Controls.Add(this.labelOutputFileName);
            this.Controls.Add(this.buttonStartStopCopy);
            this.Controls.Add(this.textBoxOutputFileNameAndPath);
            this.Controls.Add(this.labelSourceFileName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(0, 0);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(448, 182);
            this.Name = "frmMain";
            this.Text = "Tanta: Copy Audio File Via Pipeline to an MP3 Sink";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonStartStopCopy;
        private System.Windows.Forms.TextBox textBoxOutputFileNameAndPath;
        private System.Windows.Forms.Label labelSourceFileName;
        private System.Windows.Forms.Label labelOutputFileName;
        private System.Windows.Forms.TextBox textBoxSourceFileNameAndPath;
        private System.Windows.Forms.Button buttonPickFile;
    }
}


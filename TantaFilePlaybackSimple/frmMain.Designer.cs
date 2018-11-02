namespace TantaFilePlaybackSimple
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
            this.buttonStartStopPlay = new System.Windows.Forms.Button();
            this.labelSourceFileName = new System.Windows.Forms.Label();
            this.textBoxSourceFileNameAndPath = new System.Windows.Forms.TextBox();
            this.ctlTantaEVRStreamDisplay1 = new TantaCommon.ctlTantaEVRStreamDisplay();
            this.buttonPickFile = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonStartStopPlay
            // 
            this.buttonStartStopPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonStartStopPlay.Location = new System.Drawing.Point(247, 426);
            this.buttonStartStopPlay.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonStartStopPlay.Name = "buttonStartStopPlay";
            this.buttonStartStopPlay.Size = new System.Drawing.Size(171, 36);
            this.buttonStartStopPlay.TabIndex = 21;
            this.buttonStartStopPlay.Text = "Start Play";
            this.buttonStartStopPlay.UseVisualStyleBackColor = true;
            this.buttonStartStopPlay.Click += new System.EventHandler(this.buttonStartStopPlay_Click);
            // 
            // labelSourceFileName
            // 
            this.labelSourceFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelSourceFileName.AutoSize = true;
            this.labelSourceFileName.Location = new System.Drawing.Point(15, 373);
            this.labelSourceFileName.Name = "labelSourceFileName";
            this.labelSourceFileName.Size = new System.Drawing.Size(228, 17);
            this.labelSourceFileName.TabIndex = 19;
            this.labelSourceFileName.Text = "Source File Name and Path to Play";
            // 
            // textBoxSourceFileNameAndPath
            // 
            this.textBoxSourceFileNameAndPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSourceFileNameAndPath.Location = new System.Drawing.Point(31, 391);
            this.textBoxSourceFileNameAndPath.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBoxSourceFileNameAndPath.Name = "textBoxSourceFileNameAndPath";
            this.textBoxSourceFileNameAndPath.Size = new System.Drawing.Size(597, 22);
            this.textBoxSourceFileNameAndPath.TabIndex = 23;
            // 
            // ctlTantaEVRStreamDisplay1
            // 
            this.ctlTantaEVRStreamDisplay1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ctlTantaEVRStreamDisplay1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ctlTantaEVRStreamDisplay1.Location = new System.Drawing.Point(8, 15);
            this.ctlTantaEVRStreamDisplay1.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.ctlTantaEVRStreamDisplay1.Name = "ctlTantaEVRStreamDisplay1";
            this.ctlTantaEVRStreamDisplay1.Size = new System.Drawing.Size(670, 349);
            this.ctlTantaEVRStreamDisplay1.TabIndex = 25;
            // 
            // buttonPickFile
            // 
            this.buttonPickFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPickFile.Location = new System.Drawing.Point(631, 390);
            this.buttonPickFile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonPickFile.Name = "buttonPickFile";
            this.buttonPickFile.Size = new System.Drawing.Size(47, 24);
            this.buttonPickFile.TabIndex = 31;
            this.buttonPickFile.Text = "...";
            this.buttonPickFile.UseVisualStyleBackColor = true;
            this.buttonPickFile.Click += new System.EventHandler(this.buttonPickFile_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(693, 468);
            this.Controls.Add(this.buttonPickFile);
            this.Controls.Add(this.ctlTantaEVRStreamDisplay1);
            this.Controls.Add(this.textBoxSourceFileNameAndPath);
            this.Controls.Add(this.buttonStartStopPlay);
            this.Controls.Add(this.labelSourceFileName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(0, 0);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MinimumSize = new System.Drawing.Size(661, 481);
            this.Name = "frmMain";
            this.Text = "Tanta: Simple File Playback";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonStartStopPlay;
        private System.Windows.Forms.Label labelSourceFileName;
        private System.Windows.Forms.TextBox textBoxSourceFileNameAndPath;
        private TantaCommon.ctlTantaEVRStreamDisplay ctlTantaEVRStreamDisplay1;
        private System.Windows.Forms.Button buttonPickFile;
    }
}


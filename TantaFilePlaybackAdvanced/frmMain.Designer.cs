namespace TantaFilePlaybackAdvanced
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
            this.textBoxVideoFileNameAndPath = new System.Windows.Forms.TextBox();
            this.labelVideoFilePathAndName = new System.Windows.Forms.Label();
            this.buttonPickFile = new System.Windows.Forms.Button();
            this.ctlTantaEVRFilePlayer1 = new TantaCommon.ctlTantaEVRFilePlayer();
            this.SuspendLayout();
            // 
            // textBoxVideoFileNameAndPath
            // 
            this.textBoxVideoFileNameAndPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxVideoFileNameAndPath.Location = new System.Drawing.Point(32, 424);
            this.textBoxVideoFileNameAndPath.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.textBoxVideoFileNameAndPath.Name = "textBoxVideoFileNameAndPath";
            this.textBoxVideoFileNameAndPath.Size = new System.Drawing.Size(442, 22);
            this.textBoxVideoFileNameAndPath.TabIndex = 22;
            this.textBoxVideoFileNameAndPath.TextChanged += new System.EventHandler(this.textBoxVideoFileNameAndPath_TextChanged);
            // 
            // labelVideoFilePathAndName
            // 
            this.labelVideoFilePathAndName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelVideoFilePathAndName.AutoSize = true;
            this.labelVideoFilePathAndName.Location = new System.Drawing.Point(13, 404);
            this.labelVideoFilePathAndName.Name = "labelVideoFilePathAndName";
            this.labelVideoFilePathAndName.Size = new System.Drawing.Size(172, 17);
            this.labelVideoFilePathAndName.TabIndex = 21;
            this.labelVideoFilePathAndName.Text = "Video File Path and Name";
            // 
            // buttonPickFile
            // 
            this.buttonPickFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonPickFile.Location = new System.Drawing.Point(479, 423);
            this.buttonPickFile.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.buttonPickFile.Name = "buttonPickFile";
            this.buttonPickFile.Size = new System.Drawing.Size(80, 25);
            this.buttonPickFile.TabIndex = 23;
            this.buttonPickFile.Text = "Choose...";
            this.buttonPickFile.UseVisualStyleBackColor = true;
            this.buttonPickFile.Click += new System.EventHandler(this.buttonPickFile_Click);
            // 
            // ctlTantaEVRFilePlayer1
            // 
            this.ctlTantaEVRFilePlayer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ctlTantaEVRFilePlayer1.Location = new System.Drawing.Point(12, 4);
            this.ctlTantaEVRFilePlayer1.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.ctlTantaEVRFilePlayer1.Name = "ctlTantaEVRFilePlayer1";
            this.ctlTantaEVRFilePlayer1.Size = new System.Drawing.Size(640, 381);
            this.ctlTantaEVRFilePlayer1.TabIndex = 25;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(666, 474);
            this.Controls.Add(this.ctlTantaEVRFilePlayer1);
            this.Controls.Add(this.buttonPickFile);
            this.Controls.Add(this.textBoxVideoFileNameAndPath);
            this.Controls.Add(this.labelVideoFilePathAndName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(0, 0);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MinimumSize = new System.Drawing.Size(650, 500);
            this.Name = "frmMain";
            this.Text = "Tanta: Play a Video File";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxVideoFileNameAndPath;
        private System.Windows.Forms.Label labelVideoFilePathAndName;
        private System.Windows.Forms.Button buttonPickFile;
        private TantaCommon.ctlTantaEVRFilePlayer ctlTantaEVRFilePlayer1;
    }
}


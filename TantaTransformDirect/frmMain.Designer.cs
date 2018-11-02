namespace TantaTransformDirect
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
            this.groupBoxTransforms = new System.Windows.Forms.GroupBox();
            this.radioButtonMFTWriteText = new System.Windows.Forms.RadioButton();
            this.radioButtonMFTGrayscaleAsync = new System.Windows.Forms.RadioButton();
            this.radioButtonMFTGrayscaleSync = new System.Windows.Forms.RadioButton();
            this.radioButtonMFTFrameCounter = new System.Windows.Forms.RadioButton();
            this.radioButtonMFTNone = new System.Windows.Forms.RadioButton();
            this.buttonPickFile = new System.Windows.Forms.Button();
            this.buttonGetFrameCount = new System.Windows.Forms.Button();
            this.labelTransformContactDemo = new System.Windows.Forms.Label();
            this.groupBoxTransforms.SuspendLayout();
            this.SuspendLayout();
            // 
            // buttonStartStopPlay
            // 
            this.buttonStartStopPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonStartStopPlay.Location = new System.Drawing.Point(239, 355);
            this.buttonStartStopPlay.Margin = new System.Windows.Forms.Padding(2);
            this.buttonStartStopPlay.Name = "buttonStartStopPlay";
            this.buttonStartStopPlay.Size = new System.Drawing.Size(128, 47);
            this.buttonStartStopPlay.TabIndex = 21;
            this.buttonStartStopPlay.Text = "Start Play";
            this.buttonStartStopPlay.UseVisualStyleBackColor = true;
            this.buttonStartStopPlay.Click += new System.EventHandler(this.buttonStartStopPlay_Click);
            // 
            // labelSourceFileName
            // 
            this.labelSourceFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelSourceFileName.AutoSize = true;
            this.labelSourceFileName.Location = new System.Drawing.Point(11, 301);
            this.labelSourceFileName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelSourceFileName.Name = "labelSourceFileName";
            this.labelSourceFileName.Size = new System.Drawing.Size(172, 13);
            this.labelSourceFileName.TabIndex = 19;
            this.labelSourceFileName.Text = "Source File Name and Path to Play";
            // 
            // textBoxSourceFileNameAndPath
            // 
            this.textBoxSourceFileNameAndPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSourceFileNameAndPath.Location = new System.Drawing.Point(23, 316);
            this.textBoxSourceFileNameAndPath.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxSourceFileNameAndPath.Name = "textBoxSourceFileNameAndPath";
            this.textBoxSourceFileNameAndPath.Size = new System.Drawing.Size(369, 20);
            this.textBoxSourceFileNameAndPath.TabIndex = 23;
            // 
            // ctlTantaEVRStreamDisplay1
            // 
            this.ctlTantaEVRStreamDisplay1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ctlTantaEVRStreamDisplay1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ctlTantaEVRStreamDisplay1.Location = new System.Drawing.Point(6, 12);
            this.ctlTantaEVRStreamDisplay1.MinimumSize = new System.Drawing.Size(503, 284);
            this.ctlTantaEVRStreamDisplay1.Name = "ctlTantaEVRStreamDisplay1";
            this.ctlTantaEVRStreamDisplay1.Size = new System.Drawing.Size(503, 284);
            this.ctlTantaEVRStreamDisplay1.TabIndex = 25;
            // 
            // groupBoxTransforms
            // 
            this.groupBoxTransforms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxTransforms.Controls.Add(this.radioButtonMFTWriteText);
            this.groupBoxTransforms.Controls.Add(this.radioButtonMFTGrayscaleAsync);
            this.groupBoxTransforms.Controls.Add(this.radioButtonMFTGrayscaleSync);
            this.groupBoxTransforms.Controls.Add(this.radioButtonMFTFrameCounter);
            this.groupBoxTransforms.Controls.Add(this.radioButtonMFTNone);
            this.groupBoxTransforms.Location = new System.Drawing.Point(397, 310);
            this.groupBoxTransforms.Name = "groupBoxTransforms";
            this.groupBoxTransforms.Size = new System.Drawing.Size(111, 96);
            this.groupBoxTransforms.TabIndex = 28;
            this.groupBoxTransforms.TabStop = false;
            this.groupBoxTransforms.Text = "Transform";
            // 
            // radioButtonMFTWriteText
            // 
            this.radioButtonMFTWriteText.AutoSize = true;
            this.radioButtonMFTWriteText.Location = new System.Drawing.Point(6, 74);
            this.radioButtonMFTWriteText.Name = "radioButtonMFTWriteText";
            this.radioButtonMFTWriteText.Size = new System.Drawing.Size(74, 17);
            this.radioButtonMFTWriteText.TabIndex = 31;
            this.radioButtonMFTWriteText.Text = "Write Text";
            this.radioButtonMFTWriteText.UseVisualStyleBackColor = true;
            // 
            // radioButtonMFTGrayscaleAsync
            // 
            this.radioButtonMFTGrayscaleAsync.AutoSize = true;
            this.radioButtonMFTGrayscaleAsync.Location = new System.Drawing.Point(6, 59);
            this.radioButtonMFTGrayscaleAsync.Name = "radioButtonMFTGrayscaleAsync";
            this.radioButtonMFTGrayscaleAsync.Size = new System.Drawing.Size(104, 17);
            this.radioButtonMFTGrayscaleAsync.TabIndex = 30;
            this.radioButtonMFTGrayscaleAsync.Text = "Grayscale Async";
            this.radioButtonMFTGrayscaleAsync.UseVisualStyleBackColor = true;
            // 
            // radioButtonMFTGrayscaleSync
            // 
            this.radioButtonMFTGrayscaleSync.AutoSize = true;
            this.radioButtonMFTGrayscaleSync.Location = new System.Drawing.Point(6, 44);
            this.radioButtonMFTGrayscaleSync.Name = "radioButtonMFTGrayscaleSync";
            this.radioButtonMFTGrayscaleSync.Size = new System.Drawing.Size(99, 17);
            this.radioButtonMFTGrayscaleSync.TabIndex = 29;
            this.radioButtonMFTGrayscaleSync.Text = "Grayscale Sync";
            this.radioButtonMFTGrayscaleSync.UseVisualStyleBackColor = true;
            // 
            // radioButtonMFTFrameCounter
            // 
            this.radioButtonMFTFrameCounter.AutoSize = true;
            this.radioButtonMFTFrameCounter.Location = new System.Drawing.Point(6, 29);
            this.radioButtonMFTFrameCounter.Name = "radioButtonMFTFrameCounter";
            this.radioButtonMFTFrameCounter.Size = new System.Drawing.Size(94, 17);
            this.radioButtonMFTFrameCounter.TabIndex = 28;
            this.radioButtonMFTFrameCounter.Text = "Frame Counter";
            this.radioButtonMFTFrameCounter.UseVisualStyleBackColor = true;
            // 
            // radioButtonMFTNone
            // 
            this.radioButtonMFTNone.AutoSize = true;
            this.radioButtonMFTNone.Checked = true;
            this.radioButtonMFTNone.Location = new System.Drawing.Point(6, 14);
            this.radioButtonMFTNone.Name = "radioButtonMFTNone";
            this.radioButtonMFTNone.Size = new System.Drawing.Size(51, 17);
            this.radioButtonMFTNone.TabIndex = 27;
            this.radioButtonMFTNone.TabStop = true;
            this.radioButtonMFTNone.Text = "None";
            this.radioButtonMFTNone.UseVisualStyleBackColor = true;
            // 
            // buttonPickFile
            // 
            this.buttonPickFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonPickFile.Location = new System.Drawing.Point(23, 342);
            this.buttonPickFile.Margin = new System.Windows.Forms.Padding(2);
            this.buttonPickFile.Name = "buttonPickFile";
            this.buttonPickFile.Size = new System.Drawing.Size(99, 20);
            this.buttonPickFile.TabIndex = 29;
            this.buttonPickFile.Text = "Choose File...";
            this.buttonPickFile.UseVisualStyleBackColor = true;
            this.buttonPickFile.Click += new System.EventHandler(this.buttonPickFile_Click);
            // 
            // buttonGetFrameCount
            // 
            this.buttonGetFrameCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGetFrameCount.Enabled = false;
            this.buttonGetFrameCount.Location = new System.Drawing.Point(23, 388);
            this.buttonGetFrameCount.Name = "buttonGetFrameCount";
            this.buttonGetFrameCount.Size = new System.Drawing.Size(102, 23);
            this.buttonGetFrameCount.TabIndex = 31;
            this.buttonGetFrameCount.Text = "Get Frame Count";
            this.buttonGetFrameCount.UseVisualStyleBackColor = true;
            this.buttonGetFrameCount.Click += new System.EventHandler(this.buttonGetFrameCount_Click);
            // 
            // labelTransformContactDemo
            // 
            this.labelTransformContactDemo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelTransformContactDemo.AutoSize = true;
            this.labelTransformContactDemo.Enabled = false;
            this.labelTransformContactDemo.Location = new System.Drawing.Point(11, 372);
            this.labelTransformContactDemo.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelTransformContactDemo.Name = "labelTransformContactDemo";
            this.labelTransformContactDemo.Size = new System.Drawing.Size(125, 13);
            this.labelTransformContactDemo.TabIndex = 30;
            this.labelTransformContactDemo.Text = "Transform Contact Demo";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(520, 422);
            this.Controls.Add(this.buttonGetFrameCount);
            this.Controls.Add(this.labelTransformContactDemo);
            this.Controls.Add(this.buttonPickFile);
            this.Controls.Add(this.groupBoxTransforms);
            this.Controls.Add(this.ctlTantaEVRStreamDisplay1);
            this.Controls.Add(this.textBoxSourceFileNameAndPath);
            this.Controls.Add(this.buttonStartStopPlay);
            this.Controls.Add(this.labelSourceFileName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(0, 0);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "frmMain";
            this.Text = "Tanta: Direct Transform Creation Demonstrator";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.groupBoxTransforms.ResumeLayout(false);
            this.groupBoxTransforms.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonStartStopPlay;
        private System.Windows.Forms.Label labelSourceFileName;
        private System.Windows.Forms.TextBox textBoxSourceFileNameAndPath;
        private TantaCommon.ctlTantaEVRStreamDisplay ctlTantaEVRStreamDisplay1;
        private System.Windows.Forms.GroupBox groupBoxTransforms;
        private System.Windows.Forms.RadioButton radioButtonMFTWriteText;
        private System.Windows.Forms.RadioButton radioButtonMFTGrayscaleAsync;
        private System.Windows.Forms.RadioButton radioButtonMFTGrayscaleSync;
        private System.Windows.Forms.RadioButton radioButtonMFTFrameCounter;
        private System.Windows.Forms.RadioButton radioButtonMFTNone;
        private System.Windows.Forms.Button buttonPickFile;
        private System.Windows.Forms.Button buttonGetFrameCount;
        private System.Windows.Forms.Label labelTransformContactDemo;
    }
}


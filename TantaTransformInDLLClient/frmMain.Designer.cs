namespace TantaTransformInDLLClient
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
            this.groupBoxTransforms = new System.Windows.Forms.GroupBox();
            this.radioButtonMFTRotator = new System.Windows.Forms.RadioButton();
            this.radioButtonMFTNone = new System.Windows.Forms.RadioButton();
            this.groupBoxRotateMode = new System.Windows.Forms.GroupBox();
            this.radioButtonRotateNoneFlipNone = new System.Windows.Forms.RadioButton();
            this.radioButtonRotateNoneFlipX = new System.Windows.Forms.RadioButton();
            this.radioButtonRotate180FlipX = new System.Windows.Forms.RadioButton();
            this.radioButtonRotate180FlipNone = new System.Windows.Forms.RadioButton();
            this.buttonResetFrameCount = new System.Windows.Forms.Button();
            this.buttonGetFCViaProperty = new System.Windows.Forms.Button();
            this.buttonGetFCViaFunction = new System.Windows.Forms.Button();
            this.labelMFTCommsDemonstrators = new System.Windows.Forms.Label();
            this.groupBoxTransforms.SuspendLayout();
            this.groupBoxRotateMode.SuspendLayout();
            this.SuspendLayout();
            // 
            // textBoxVideoFileNameAndPath
            // 
            this.textBoxVideoFileNameAndPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxVideoFileNameAndPath.Location = new System.Drawing.Point(20, 349);
            this.textBoxVideoFileNameAndPath.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxVideoFileNameAndPath.Name = "textBoxVideoFileNameAndPath";
            this.textBoxVideoFileNameAndPath.Size = new System.Drawing.Size(346, 20);
            this.textBoxVideoFileNameAndPath.TabIndex = 22;
            this.textBoxVideoFileNameAndPath.TextChanged += new System.EventHandler(this.textBoxVideoFileNameAndPath_TextChanged);
            // 
            // labelVideoFilePathAndName
            // 
            this.labelVideoFilePathAndName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelVideoFilePathAndName.AutoSize = true;
            this.labelVideoFilePathAndName.Location = new System.Drawing.Point(6, 332);
            this.labelVideoFilePathAndName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelVideoFilePathAndName.Name = "labelVideoFilePathAndName";
            this.labelVideoFilePathAndName.Size = new System.Drawing.Size(130, 13);
            this.labelVideoFilePathAndName.TabIndex = 21;
            this.labelVideoFilePathAndName.Text = "Video File Path and Name";
            // 
            // buttonPickFile
            // 
            this.buttonPickFile.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonPickFile.Location = new System.Drawing.Point(113, 372);
            this.buttonPickFile.Margin = new System.Windows.Forms.Padding(2);
            this.buttonPickFile.Name = "buttonPickFile";
            this.buttonPickFile.Size = new System.Drawing.Size(99, 20);
            this.buttonPickFile.TabIndex = 23;
            this.buttonPickFile.Text = "Choose File...";
            this.buttonPickFile.UseVisualStyleBackColor = true;
            this.buttonPickFile.Click += new System.EventHandler(this.buttonPickFile_Click);
            // 
            // ctlTantaEVRFilePlayer1
            // 
            this.ctlTantaEVRFilePlayer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ctlTantaEVRFilePlayer1.Location = new System.Drawing.Point(9, 3);
            this.ctlTantaEVRFilePlayer1.Margin = new System.Windows.Forms.Padding(2);
            this.ctlTantaEVRFilePlayer1.Name = "ctlTantaEVRFilePlayer1";
            this.ctlTantaEVRFilePlayer1.Size = new System.Drawing.Size(493, 318);
            this.ctlTantaEVRFilePlayer1.TabIndex = 25;
            // 
            // groupBoxTransforms
            // 
            this.groupBoxTransforms.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxTransforms.Controls.Add(this.radioButtonMFTRotator);
            this.groupBoxTransforms.Controls.Add(this.radioButtonMFTNone);
            this.groupBoxTransforms.Location = new System.Drawing.Point(371, 326);
            this.groupBoxTransforms.Name = "groupBoxTransforms";
            this.groupBoxTransforms.Size = new System.Drawing.Size(127, 35);
            this.groupBoxTransforms.TabIndex = 27;
            this.groupBoxTransforms.TabStop = false;
            this.groupBoxTransforms.Text = "Transform to Use";
            // 
            // radioButtonMFTRotator
            // 
            this.radioButtonMFTRotator.AutoSize = true;
            this.radioButtonMFTRotator.Location = new System.Drawing.Point(63, 14);
            this.radioButtonMFTRotator.Name = "radioButtonMFTRotator";
            this.radioButtonMFTRotator.Size = new System.Drawing.Size(60, 17);
            this.radioButtonMFTRotator.TabIndex = 28;
            this.radioButtonMFTRotator.Text = "Rotator";
            this.radioButtonMFTRotator.UseVisualStyleBackColor = true;
            this.radioButtonMFTRotator.CheckedChanged += new System.EventHandler(this.radioButtonMFTRotator_CheckedChanged);
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
            this.radioButtonMFTNone.CheckedChanged += new System.EventHandler(this.radioButtonMFTNone_CheckedChanged);
            // 
            // groupBoxRotateMode
            // 
            this.groupBoxRotateMode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxRotateMode.Controls.Add(this.radioButtonRotateNoneFlipNone);
            this.groupBoxRotateMode.Controls.Add(this.radioButtonRotateNoneFlipX);
            this.groupBoxRotateMode.Controls.Add(this.radioButtonRotate180FlipX);
            this.groupBoxRotateMode.Controls.Add(this.radioButtonRotate180FlipNone);
            this.groupBoxRotateMode.Location = new System.Drawing.Point(371, 361);
            this.groupBoxRotateMode.Name = "groupBoxRotateMode";
            this.groupBoxRotateMode.Size = new System.Drawing.Size(131, 73);
            this.groupBoxRotateMode.TabIndex = 31;
            this.groupBoxRotateMode.TabStop = false;
            this.groupBoxRotateMode.Text = "Rotate Mode";
            // 
            // radioButtonRotateNoneFlipNone
            // 
            this.radioButtonRotateNoneFlipNone.AutoSize = true;
            this.radioButtonRotateNoneFlipNone.Checked = true;
            this.radioButtonRotateNoneFlipNone.Location = new System.Drawing.Point(7, 12);
            this.radioButtonRotateNoneFlipNone.Name = "radioButtonRotateNoneFlipNone";
            this.radioButtonRotateNoneFlipNone.Size = new System.Drawing.Size(125, 17);
            this.radioButtonRotateNoneFlipNone.TabIndex = 38;
            this.radioButtonRotateNoneFlipNone.TabStop = true;
            this.radioButtonRotateNoneFlipNone.Text = "RotateNoneFlipNone";
            this.radioButtonRotateNoneFlipNone.UseVisualStyleBackColor = true;
            this.radioButtonRotateNoneFlipNone.CheckedChanged += new System.EventHandler(this.radioButtonRotateNoneFlipNone_CheckedChanged);
            // 
            // radioButtonRotateNoneFlipX
            // 
            this.radioButtonRotateNoneFlipX.AutoSize = true;
            this.radioButtonRotateNoneFlipX.Location = new System.Drawing.Point(7, 57);
            this.radioButtonRotateNoneFlipX.Name = "radioButtonRotateNoneFlipX";
            this.radioButtonRotateNoneFlipX.Size = new System.Drawing.Size(106, 17);
            this.radioButtonRotateNoneFlipX.TabIndex = 37;
            this.radioButtonRotateNoneFlipX.Text = "RotateNoneFlipX";
            this.radioButtonRotateNoneFlipX.UseVisualStyleBackColor = true;
            this.radioButtonRotateNoneFlipX.CheckedChanged += new System.EventHandler(this.radioButtonRotateNoneFlipX_CheckedChanged);
            // 
            // radioButtonRotate180FlipX
            // 
            this.radioButtonRotate180FlipX.AutoSize = true;
            this.radioButtonRotate180FlipX.Location = new System.Drawing.Point(7, 42);
            this.radioButtonRotate180FlipX.Name = "radioButtonRotate180FlipX";
            this.radioButtonRotate180FlipX.Size = new System.Drawing.Size(98, 17);
            this.radioButtonRotate180FlipX.TabIndex = 36;
            this.radioButtonRotate180FlipX.Text = "Rotate180FlipX";
            this.radioButtonRotate180FlipX.UseVisualStyleBackColor = true;
            this.radioButtonRotate180FlipX.CheckedChanged += new System.EventHandler(this.radioButtonRotate180FlipX_CheckedChanged);
            // 
            // radioButtonRotate180FlipNone
            // 
            this.radioButtonRotate180FlipNone.AutoSize = true;
            this.radioButtonRotate180FlipNone.Location = new System.Drawing.Point(7, 27);
            this.radioButtonRotate180FlipNone.Name = "radioButtonRotate180FlipNone";
            this.radioButtonRotate180FlipNone.Size = new System.Drawing.Size(117, 17);
            this.radioButtonRotate180FlipNone.TabIndex = 35;
            this.radioButtonRotate180FlipNone.Text = "Rotate180FlipNone";
            this.radioButtonRotate180FlipNone.UseVisualStyleBackColor = true;
            this.radioButtonRotate180FlipNone.CheckedChanged += new System.EventHandler(this.radioButtonRotate180FlipNone_CheckedChanged);
            // 
            // buttonResetFrameCount
            // 
            this.buttonResetFrameCount.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonResetFrameCount.Location = new System.Drawing.Point(12, 412);
            this.buttonResetFrameCount.Name = "buttonResetFrameCount";
            this.buttonResetFrameCount.Size = new System.Drawing.Size(60, 23);
            this.buttonResetFrameCount.TabIndex = 32;
            this.buttonResetFrameCount.Text = "ResetFC";
            this.buttonResetFrameCount.UseVisualStyleBackColor = true;
            this.buttonResetFrameCount.Click += new System.EventHandler(this.buttonResetFrameCount_Click);
            // 
            // buttonGetFCViaProperty
            // 
            this.buttonGetFCViaProperty.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGetFCViaProperty.Location = new System.Drawing.Point(78, 412);
            this.buttonGetFCViaProperty.Name = "buttonGetFCViaProperty";
            this.buttonGetFCViaProperty.Size = new System.Drawing.Size(60, 23);
            this.buttonGetFCViaProperty.TabIndex = 33;
            this.buttonGetFCViaProperty.Text = "GetFC(p)";
            this.buttonGetFCViaProperty.UseVisualStyleBackColor = true;
            this.buttonGetFCViaProperty.Click += new System.EventHandler(this.buttonGetFCViaProperty_Click);
            // 
            // buttonGetFCViaFunction
            // 
            this.buttonGetFCViaFunction.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.buttonGetFCViaFunction.Location = new System.Drawing.Point(144, 412);
            this.buttonGetFCViaFunction.Name = "buttonGetFCViaFunction";
            this.buttonGetFCViaFunction.Size = new System.Drawing.Size(60, 23);
            this.buttonGetFCViaFunction.TabIndex = 34;
            this.buttonGetFCViaFunction.Text = "GetFC(f)";
            this.buttonGetFCViaFunction.UseVisualStyleBackColor = true;
            this.buttonGetFCViaFunction.Click += new System.EventHandler(this.buttonGetFCViaFunction_Click);
            // 
            // labelMFTCommsDemonstrators
            // 
            this.labelMFTCommsDemonstrators.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelMFTCommsDemonstrators.AutoSize = true;
            this.labelMFTCommsDemonstrators.Location = new System.Drawing.Point(6, 396);
            this.labelMFTCommsDemonstrators.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelMFTCommsDemonstrators.Name = "labelMFTCommsDemonstrators";
            this.labelMFTCommsDemonstrators.Size = new System.Drawing.Size(236, 13);
            this.labelMFTCommsDemonstrators.TabIndex = 35;
            this.labelMFTCommsDemonstrators.Text = "Client/Transform Communications Demonstrators";
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(512, 437);
            this.Controls.Add(this.labelMFTCommsDemonstrators);
            this.Controls.Add(this.buttonGetFCViaFunction);
            this.Controls.Add(this.buttonGetFCViaProperty);
            this.Controls.Add(this.buttonResetFrameCount);
            this.Controls.Add(this.groupBoxRotateMode);
            this.Controls.Add(this.groupBoxTransforms);
            this.Controls.Add(this.ctlTantaEVRFilePlayer1);
            this.Controls.Add(this.buttonPickFile);
            this.Controls.Add(this.textBoxVideoFileNameAndPath);
            this.Controls.Add(this.labelVideoFilePathAndName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(0, 0);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(528, 432);
            this.Name = "frmMain";
            this.Text = "Tanta: Load a Transform from the Registry";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.groupBoxTransforms.ResumeLayout(false);
            this.groupBoxTransforms.PerformLayout();
            this.groupBoxRotateMode.ResumeLayout(false);
            this.groupBoxRotateMode.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxVideoFileNameAndPath;
        private System.Windows.Forms.Label labelVideoFilePathAndName;
        private System.Windows.Forms.Button buttonPickFile;
        private TantaCommon.ctlTantaEVRFilePlayer ctlTantaEVRFilePlayer1;
        private System.Windows.Forms.GroupBox groupBoxTransforms;
        private System.Windows.Forms.RadioButton radioButtonMFTRotator;
        private System.Windows.Forms.RadioButton radioButtonMFTNone;
        private System.Windows.Forms.GroupBox groupBoxRotateMode;
        private System.Windows.Forms.RadioButton radioButtonRotateNoneFlipX;
        private System.Windows.Forms.RadioButton radioButtonRotate180FlipX;
        private System.Windows.Forms.RadioButton radioButtonRotate180FlipNone;
        private System.Windows.Forms.RadioButton radioButtonRotateNoneFlipNone;
        private System.Windows.Forms.Button buttonResetFrameCount;
        private System.Windows.Forms.Button buttonGetFCViaProperty;
        private System.Windows.Forms.Button buttonGetFCViaFunction;
        private System.Windows.Forms.Label labelMFTCommsDemonstrators;
    }
}


namespace TantaCaptureToScreenAndFile
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
            this.textBoxOutputFileNameAndPath = new System.Windows.Forms.TextBox();
            this.labelVideoCaptureDeviceName = new System.Windows.Forms.Label();
            this.labelOutputFileName = new System.Windows.Forms.Label();
            this.textBoxPickedVideoDeviceURL = new System.Windows.Forms.TextBox();
            this.ctlTantaEVRStreamDisplay1 = new TantaCommon.ctlTantaEVRStreamDisplay();
            this.ctlTantaVideoPicker1 = new TantaCommon.ctlTantaVideoPicker();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonRecordingOnOff = new System.Windows.Forms.Button();
            this.checkBoxTimeBaseRebase = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // buttonStartStopPlay
            // 
            this.buttonStartStopPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonStartStopPlay.Location = new System.Drawing.Point(456, 314);
            this.buttonStartStopPlay.Margin = new System.Windows.Forms.Padding(2);
            this.buttonStartStopPlay.Name = "buttonStartStopPlay";
            this.buttonStartStopPlay.Size = new System.Drawing.Size(128, 29);
            this.buttonStartStopPlay.TabIndex = 21;
            this.buttonStartStopPlay.Text = "Start Capture";
            this.buttonStartStopPlay.UseVisualStyleBackColor = true;
            this.buttonStartStopPlay.Click += new System.EventHandler(this.buttonStartStopPlay_Click);
            // 
            // textBoxOutputFileNameAndPath
            // 
            this.textBoxOutputFileNameAndPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOutputFileNameAndPath.Location = new System.Drawing.Point(23, 361);
            this.textBoxOutputFileNameAndPath.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxOutputFileNameAndPath.Name = "textBoxOutputFileNameAndPath";
            this.textBoxOutputFileNameAndPath.Size = new System.Drawing.Size(392, 20);
            this.textBoxOutputFileNameAndPath.TabIndex = 20;
            // 
            // labelVideoCaptureDeviceName
            // 
            this.labelVideoCaptureDeviceName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelVideoCaptureDeviceName.AutoSize = true;
            this.labelVideoCaptureDeviceName.Location = new System.Drawing.Point(11, 302);
            this.labelVideoCaptureDeviceName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelVideoCaptureDeviceName.Name = "labelVideoCaptureDeviceName";
            this.labelVideoCaptureDeviceName.Size = new System.Drawing.Size(107, 13);
            this.labelVideoCaptureDeviceName.TabIndex = 19;
            this.labelVideoCaptureDeviceName.Text = "Picked Video Device";
            // 
            // labelOutputFileName
            // 
            this.labelOutputFileName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.labelOutputFileName.AutoSize = true;
            this.labelOutputFileName.Location = new System.Drawing.Point(11, 346);
            this.labelOutputFileName.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.labelOutputFileName.Name = "labelOutputFileName";
            this.labelOutputFileName.Size = new System.Drawing.Size(135, 13);
            this.labelOutputFileName.TabIndex = 22;
            this.labelOutputFileName.Text = "Output File Name and Path";
            // 
            // textBoxPickedVideoDeviceURL
            // 
            this.textBoxPickedVideoDeviceURL.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxPickedVideoDeviceURL.Location = new System.Drawing.Point(23, 317);
            this.textBoxPickedVideoDeviceURL.Margin = new System.Windows.Forms.Padding(2);
            this.textBoxPickedVideoDeviceURL.Name = "textBoxPickedVideoDeviceURL";
            this.textBoxPickedVideoDeviceURL.Size = new System.Drawing.Size(392, 20);
            this.textBoxPickedVideoDeviceURL.TabIndex = 23;
            this.textBoxPickedVideoDeviceURL.TextChanged += new System.EventHandler(this.textBoxPickedVideoDeviceURL_TextChanged);
            // 
            // ctlTantaEVRStreamDisplay1
            // 
            this.ctlTantaEVRStreamDisplay1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ctlTantaEVRStreamDisplay1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.ctlTantaEVRStreamDisplay1.Location = new System.Drawing.Point(430, 12);
            this.ctlTantaEVRStreamDisplay1.Name = "ctlTantaEVRStreamDisplay1";
            this.ctlTantaEVRStreamDisplay1.Size = new System.Drawing.Size(503, 284);
            this.ctlTantaEVRStreamDisplay1.TabIndex = 25;
            // 
            // ctlTantaVideoPicker1
            // 
            this.ctlTantaVideoPicker1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
            this.ctlTantaVideoPicker1.Location = new System.Drawing.Point(23, 46);
            this.ctlTantaVideoPicker1.Name = "ctlTantaVideoPicker1";
            this.ctlTantaVideoPicker1.Size = new System.Drawing.Size(392, 253);
            this.ctlTantaVideoPicker1.TabIndex = 26;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(20, 21);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(119, 13);
            this.label1.TabIndex = 27;
            this.label1.Text = "Choose a Video Source";
            // 
            // buttonRecordingOnOff
            // 
            this.buttonRecordingOnOff.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonRecordingOnOff.Location = new System.Drawing.Point(456, 355);
            this.buttonRecordingOnOff.Margin = new System.Windows.Forms.Padding(2);
            this.buttonRecordingOnOff.Name = "buttonRecordingOnOff";
            this.buttonRecordingOnOff.Size = new System.Drawing.Size(128, 29);
            this.buttonRecordingOnOff.TabIndex = 28;
            this.buttonRecordingOnOff.Text = "Recording is OFF";
            this.buttonRecordingOnOff.UseVisualStyleBackColor = true;
            this.buttonRecordingOnOff.Click += new System.EventHandler(this.buttonRecordingOnOff_Click);
            // 
            // checkBoxTimeBaseRebase
            // 
            this.checkBoxTimeBaseRebase.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxTimeBaseRebase.AutoSize = true;
            this.checkBoxTimeBaseRebase.Location = new System.Drawing.Point(598, 361);
            this.checkBoxTimeBaseRebase.Name = "checkBoxTimeBaseRebase";
            this.checkBoxTimeBaseRebase.Size = new System.Drawing.Size(150, 17);
            this.checkBoxTimeBaseRebase.TabIndex = 29;
            this.checkBoxTimeBaseRebase.Text = "Rebase timestamps from 0";
            this.checkBoxTimeBaseRebase.UseVisualStyleBackColor = true;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(943, 397);
            this.Controls.Add(this.checkBoxTimeBaseRebase);
            this.Controls.Add(this.buttonRecordingOnOff);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ctlTantaVideoPicker1);
            this.Controls.Add(this.ctlTantaEVRStreamDisplay1);
            this.Controls.Add(this.textBoxPickedVideoDeviceURL);
            this.Controls.Add(this.labelOutputFileName);
            this.Controls.Add(this.buttonStartStopPlay);
            this.Controls.Add(this.textBoxOutputFileNameAndPath);
            this.Controls.Add(this.labelVideoCaptureDeviceName);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(0, 0);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "frmMain";
            this.Text = "Tanta: Capture to Screen and File";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonStartStopPlay;
        private System.Windows.Forms.TextBox textBoxOutputFileNameAndPath;
        private System.Windows.Forms.Label labelVideoCaptureDeviceName;
        private System.Windows.Forms.Label labelOutputFileName;
        private System.Windows.Forms.TextBox textBoxPickedVideoDeviceURL;
        private TantaCommon.ctlTantaEVRStreamDisplay ctlTantaEVRStreamDisplay1;
        private TantaCommon.ctlTantaVideoPicker ctlTantaVideoPicker1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonRecordingOnOff;
        private System.Windows.Forms.CheckBox checkBoxTimeBaseRebase;
    }
}


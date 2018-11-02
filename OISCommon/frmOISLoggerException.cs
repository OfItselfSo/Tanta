using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;

/// +------------------------------------------------------------------------------------------------------------------------------+
/// ¦                                                   TERMS OF USE: MIT License                                                  ¦
/// +------------------------------------------------------------------------------------------------------------------------------¦
/// ¦Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation    ¦
/// ¦files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy,    ¦
/// ¦modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software¦
/// ¦is furnished to do so, subject to the following conditions:                                                                   ¦
/// ¦                                                                                                                              ¦
/// ¦The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.¦
/// ¦                                                                                                                              ¦
/// ¦THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE          ¦
/// ¦WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR         ¦
/// ¦COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,   ¦
/// ¦ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                         ¦
/// +------------------------------------------------------------------------------------------------------------------------------+

/// #########
/// Note: the three letter "OIS" prefix used here is an acronym for "OfItselfSo.com" this softwares home website.
/// #########

namespace OISCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
    /// <summary>
    /// Form to be launched only when an unhandleable exception occurs in the 
    /// logger class. Not to be inherited from anything OSI. Can only ever
    /// call things from the windows API
    /// </summary>
    /// <remarks>Strictly Windows API calls in here.</remarks>
    /// <history>
    ///    03 Nov 09  Cynic - Started
    /// </history>
    public class frmOISLoggerException : System.Windows.Forms.Form
    {
        private System.ComponentModel.Container components = null;
        private System.Windows.Forms.TextBox logFileDirectory;
        private System.Windows.Forms.TextBox logFileName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button buttonBrowseForDir;
        private System.Windows.Forms.Label errorMessage;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button buttonTestLogFile;
        private System.Windows.Forms.Button buttonUseNewLogFile;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
        private System.Windows.Forms.Label labelTitle;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public frmOISLoggerException(Exception e)
        {
            InitializeComponent();
            errorMessage.Text=e.Message;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Get or set the log file name
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public string LogFileName
        {
            get { return this.logFileName.Text; }
            set { this.logFileName.Text = value; }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Get or set the log file directory location
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public string LogFileDirectory
        {
            get 
            { 
                // Verify a '\' exists on the end of the location
                if (this.logFileDirectory.Text.EndsWith("\\") == false)
                {
                    return this.logFileDirectory.Text + "\\";
                }
                else return this.logFileDirectory.Text; 
            }
            set
            {
                this.logFileDirectory.Text = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Handle a click on the browse for directory button
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        private void buttonBrowseForDir_Click(object sender, System.EventArgs e)
        {
            // Show the FolderBrowserDialog.
            if (folderBrowserDialog1 != null)
            {
                folderBrowserDialog1.SelectedPath = this.LogFileDirectory;
                folderBrowserDialog1.Description = "Choose a new log file directory";
                DialogResult result = folderBrowserDialog1.ShowDialog();
                if (result == DialogResult.OK)
                {
                    this.LogFileDirectory = folderBrowserDialog1.SelectedPath;
                }
            }
        }


        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose( bool disposing )
        {
            if( disposing )
            {
                if(components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose( disposing );
        }

        #region Windows Form Designer generated code
        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.logFileDirectory = new System.Windows.Forms.TextBox();
            this.logFileName = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.buttonBrowseForDir = new System.Windows.Forms.Button();
            this.labelTitle = new System.Windows.Forms.Label();
            this.errorMessage = new System.Windows.Forms.Label();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.buttonTestLogFile = new System.Windows.Forms.Button();
            this.buttonUseNewLogFile = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.groupBox1.SuspendLayout();
            this.SuspendLayout();
            // 
            // logFileDirectory
            // 
            this.logFileDirectory.Location = new System.Drawing.Point(95, 168);
            this.logFileDirectory.Name = "logFileDirectory";
            this.logFileDirectory.ReadOnly = true;
            this.logFileDirectory.Size = new System.Drawing.Size(296, 20);
            this.logFileDirectory.TabIndex = 1;
            this.logFileDirectory.Text = "<no dir>";
            // 
            // logFileName
            // 
            this.logFileName.Location = new System.Drawing.Point(95, 224);
            this.logFileName.Name = "logFileName";
            this.logFileName.Size = new System.Drawing.Size(288, 20);
            this.logFileName.TabIndex = 2;
            this.logFileName.Text = "<no file>";
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(71, 144);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(100, 16);
            this.label1.TabIndex = 3;
            this.label1.Text = "Directory";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(71, 200);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(100, 16);
            this.label2.TabIndex = 4;
            this.label2.Text = "Log File Name";
            this.label2.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // buttonBrowseForDir
            // 
            this.buttonBrowseForDir.Location = new System.Drawing.Point(407, 168);
            this.buttonBrowseForDir.Name = "buttonBrowseForDir";
            this.buttonBrowseForDir.TabIndex = 5;
            this.buttonBrowseForDir.Text = "&Browse...";
            this.buttonBrowseForDir.Click += new System.EventHandler(this.buttonBrowseForDir_Click);
            // 
            // labelTitle
            // 
            this.labelTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((System.Byte)(0)));
            this.labelTitle.Location = new System.Drawing.Point(32, 0);
            this.labelTitle.Name = "labelTitle";
            this.labelTitle.Size = new System.Drawing.Size(488, 48);
            this.labelTitle.TabIndex = 6;
            this.labelTitle.Text = "An Error Occurred When Writing to the Log File";
            this.labelTitle.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // errorMessage
            // 
            this.errorMessage.Location = new System.Drawing.Point(72, 64);
            this.errorMessage.Name = "errorMessage";
            this.errorMessage.Size = new System.Drawing.Size(408, 48);
            this.errorMessage.TabIndex = 7;
            this.errorMessage.Text = "Error Message";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.buttonTestLogFile);
            this.groupBox1.Location = new System.Drawing.Point(64, 120);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(432, 152);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Edit Log File Location";
            // 
            // buttonTestLogFile
            // 
            this.buttonTestLogFile.Location = new System.Drawing.Point(344, 104);
            this.buttonTestLogFile.Name = "buttonTestLogFile";
            this.buttonTestLogFile.TabIndex = 0;
            this.buttonTestLogFile.Text = "&Test...";
            // 
            // buttonUseNewLogFile
            // 
            this.buttonUseNewLogFile.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonUseNewLogFile.Location = new System.Drawing.Point(124, 296);
            this.buttonUseNewLogFile.Name = "buttonUseNewLogFile";
            this.buttonUseNewLogFile.Size = new System.Drawing.Size(120, 40);
            this.buttonUseNewLogFile.TabIndex = 9;
            this.buttonUseNewLogFile.Text = "&Use New Log File";
            // 
            // button1
            // 
            this.button1.DialogResult = System.Windows.Forms.DialogResult.Ignore;
            this.button1.Location = new System.Drawing.Point(308, 296);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(120, 40);
            this.button1.TabIndex = 10;
            this.button1.Text = "&Do not use Logging";
            // 
            // frmOISLoggerException
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.ClientSize = new System.Drawing.Size(552, 349);
            //BUGBUG this.MinimumXYSize = new System.Drawing.Size(552, 349);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.buttonUseNewLogFile);
            this.Controls.Add(this.errorMessage);
            this.Controls.Add(this.labelTitle);
            this.Controls.Add(this.buttonBrowseForDir);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.logFileName);
            this.Controls.Add(this.logFileDirectory);
            this.Controls.Add(this.groupBox1);
            this.Name = "frmOISLoggerException";
            this.Text = "frmOISLoggerException";
            this.groupBox1.ResumeLayout(false);
            this.ResumeLayout(false);

        }
        #endregion

    }
}

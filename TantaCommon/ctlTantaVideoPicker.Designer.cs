namespace TantaCommon
{
    partial class ctlTantaVideoPicker
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.comboBoxCaptureDevices = new System.Windows.Forms.ComboBox();
            this.listViewSupportedFormats = new System.Windows.Forms.ListView();
            this.SuspendLayout();
            // 
            // comboBoxCaptureDevices
            // 
            this.comboBoxCaptureDevices.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxCaptureDevices.FormattingEnabled = true;
            this.comboBoxCaptureDevices.Location = new System.Drawing.Point(0, 0);
            this.comboBoxCaptureDevices.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.comboBoxCaptureDevices.Name = "comboBoxCaptureDevices";
            this.comboBoxCaptureDevices.Size = new System.Drawing.Size(391, 24);
            this.comboBoxCaptureDevices.TabIndex = 10;
            this.comboBoxCaptureDevices.SelectedIndexChanged += new System.EventHandler(this.comboBoxCaptureDevices_SelectedIndexChanged);
            // 
            // listViewSupportedFormats
            // 
            this.listViewSupportedFormats.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewSupportedFormats.FullRowSelect = true;
            this.listViewSupportedFormats.GridLines = true;
            this.listViewSupportedFormats.Location = new System.Drawing.Point(0, 31);
            this.listViewSupportedFormats.MultiSelect = false;
            this.listViewSupportedFormats.Name = "listViewSupportedFormats";
            this.listViewSupportedFormats.Size = new System.Drawing.Size(391, 103);
            this.listViewSupportedFormats.TabIndex = 11;
            this.listViewSupportedFormats.UseCompatibleStateImageBehavior = false;
            this.listViewSupportedFormats.View = System.Windows.Forms.View.Details;
            this.listViewSupportedFormats.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewSupportedFormats_ColumnClick);
            this.listViewSupportedFormats.SelectedIndexChanged += new System.EventHandler(this.listViewSupportedFormats_SelectedIndexChanged);
            this.listViewSupportedFormats.DoubleClick += new System.EventHandler(this.listViewSupportedFormats_DoubleClick);
            // 
            // ctlTantaVideoPicker
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.listViewSupportedFormats);
            this.Controls.Add(this.comboBoxCaptureDevices);
            this.Name = "ctlTantaVideoPicker";
            this.Size = new System.Drawing.Size(392, 137);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxCaptureDevices;
        private System.Windows.Forms.ListView listViewSupportedFormats;
    }
}

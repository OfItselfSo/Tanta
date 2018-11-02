namespace TantaTransformPicker
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
            this.ctlTantaTransformPicker1 = new TantaCommon.ctlTantaTransformPicker();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonSelectedFormat = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // ctlTantaTransformPicker1
            // 
            this.ctlTantaTransformPicker1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.ctlTantaTransformPicker1.Location = new System.Drawing.Point(12, 26);
            this.ctlTantaTransformPicker1.Name = "ctlTantaTransformPicker1";
            this.ctlTantaTransformPicker1.Size = new System.Drawing.Size(1090, 407);
            this.ctlTantaTransformPicker1.TabIndex = 1;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(197, 17);
            this.label1.TabIndex = 8;
            this.label1.Text = "MF Transforms on the System";
            // 
            // buttonSelectedFormat
            // 
            this.buttonSelectedFormat.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonSelectedFormat.Location = new System.Drawing.Point(12, 439);
            this.buttonSelectedFormat.Name = "buttonSelectedFormat";
            this.buttonSelectedFormat.Size = new System.Drawing.Size(1090, 26);
            this.buttonSelectedFormat.TabIndex = 13;
            this.buttonSelectedFormat.Text = "<No Transform Selected>";
            this.buttonSelectedFormat.UseVisualStyleBackColor = true;
            this.buttonSelectedFormat.Click += new System.EventHandler(this.buttonSelectedFormat_Click);
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1114, 469);
            this.Controls.Add(this.buttonSelectedFormat);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.ctlTantaTransformPicker1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Location = new System.Drawing.Point(0, 0);
            this.MinimumSize = new System.Drawing.Size(565, 318);
            this.Name = "frmMain";
            this.Text = "Tanta: View WMF Transforms and Display Associated Information";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private TantaCommon.ctlTantaTransformPicker ctlTantaTransformPicker1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonSelectedFormat;
    }
}


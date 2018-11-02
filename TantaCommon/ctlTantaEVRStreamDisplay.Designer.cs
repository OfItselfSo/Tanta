namespace TantaCommon
{
    partial class ctlTantaEVRStreamDisplay
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
            this.panelDisplayPanel = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // panelDisplayPanel
            // 
            this.panelDisplayPanel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panelDisplayPanel.Location = new System.Drawing.Point(0, 0);
            this.panelDisplayPanel.Name = "panelDisplayPanel";
            this.panelDisplayPanel.Size = new System.Drawing.Size(480, 339);
            this.panelDisplayPanel.TabIndex = 0;
            // 
            // ctlTantaEVRStreamDisplay
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.panelDisplayPanel);
            this.Name = "ctlTantaEVRStreamDisplay";
            this.Size = new System.Drawing.Size(480, 339);
            this.SizeChanged += new System.EventHandler(this.ctlTantaEVRStreamDisplay_SizeChanged);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelDisplayPanel;
    }
}

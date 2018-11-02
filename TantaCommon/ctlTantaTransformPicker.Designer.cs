namespace TantaCommon
{
    partial class ctlTantaTransformPicker
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
            this.comboBoxTransformCategories = new System.Windows.Forms.ComboBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.listViewAvailableTransforms = new System.Windows.Forms.ListView();
            this.richTextBoxtTransformDetails = new System.Windows.Forms.RichTextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // comboBoxTransformCategories
            // 
            this.comboBoxTransformCategories.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxTransformCategories.FormattingEnabled = true;
            this.comboBoxTransformCategories.Location = new System.Drawing.Point(0, 0);
            this.comboBoxTransformCategories.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.comboBoxTransformCategories.Name = "comboBoxTransformCategories";
            this.comboBoxTransformCategories.Size = new System.Drawing.Size(391, 21);
            this.comboBoxTransformCategories.TabIndex = 10;
            this.comboBoxTransformCategories.SelectedIndexChanged += new System.EventHandler(this.comboBoxTransformCategories_SelectedIndexChanged);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(0, 29);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.listViewAvailableTransforms);
            this.splitContainer1.Panel1MinSize = 100;
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.richTextBoxtTransformDetails);
            this.splitContainer1.Panel2MinSize = 100;
            this.splitContainer1.Size = new System.Drawing.Size(391, 100);
            this.splitContainer1.SplitterDistance = 244;
            this.splitContainer1.TabIndex = 12;
            // 
            // listViewAvailableTransforms
            // 
            this.listViewAvailableTransforms.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listViewAvailableTransforms.FullRowSelect = true;
            this.listViewAvailableTransforms.GridLines = true;
            this.listViewAvailableTransforms.Location = new System.Drawing.Point(0, 0);
            this.listViewAvailableTransforms.MultiSelect = false;
            this.listViewAvailableTransforms.Name = "listViewAvailableTransforms";
            this.listViewAvailableTransforms.Size = new System.Drawing.Size(241, 99);
            this.listViewAvailableTransforms.TabIndex = 12;
            this.listViewAvailableTransforms.UseCompatibleStateImageBehavior = false;
            this.listViewAvailableTransforms.View = System.Windows.Forms.View.Details;
            this.listViewAvailableTransforms.ColumnClick += new System.Windows.Forms.ColumnClickEventHandler(this.listViewAvailableTransforms_ColumnClick);
            this.listViewAvailableTransforms.SelectedIndexChanged += new System.EventHandler(this.listViewAvailableTransforms_SelectedIndexChanged);
            this.listViewAvailableTransforms.DoubleClick += new System.EventHandler(this.listViewAvailableTransforms_DoubleClick);
            // 
            // richTextBoxtTransformDetails
            // 
            this.richTextBoxtTransformDetails.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxtTransformDetails.Location = new System.Drawing.Point(0, 0);
            this.richTextBoxtTransformDetails.Name = "richTextBoxtTransformDetails";
            this.richTextBoxtTransformDetails.ReadOnly = true;
            this.richTextBoxtTransformDetails.Size = new System.Drawing.Size(142, 99);
            this.richTextBoxtTransformDetails.TabIndex = 0;
            this.richTextBoxtTransformDetails.Text = "";
            this.richTextBoxtTransformDetails.WordWrap = false;
            // 
            // ctlTantaTransformPicker
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.comboBoxTransformCategories);
            this.Name = "ctlTantaTransformPicker";
            this.Size = new System.Drawing.Size(392, 137);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox comboBoxTransformCategories;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ListView listViewAvailableTransforms;
        private System.Windows.Forms.RichTextBox richTextBoxtTransformDetails;
    }
}

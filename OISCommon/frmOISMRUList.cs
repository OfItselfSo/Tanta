using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
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
/// 
namespace OISCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
    /// <summary>
    /// Provides Most Recently Used File (MRU) List functionality
    /// </summary>
    /// <history>
    ///    08 Aug 10  Cynic - Started
    /// </history>
    public partial class frmOISMRUList : frmOISBase
    {
        // this is the most we keep
        public const int DEFAULT_MAXMRU_VALUES = 25;
        private int maxMRUValues = DEFAULT_MAXMRU_VALUES;

        public const string DEFAULT_NOFILES_NOTICE = "No files to display...";

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        public frmOISMRUList()
        {
            InitializeComponent();

            // set  the default "no files" notice
            this.listBoxFileList.Items.AddRange(new object[] {DEFAULT_NOFILES_NOTICE});

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Gets/Sets the Max number of MRU values we accept. Will never accept
        /// a negative number.
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        public int MaxMRUValues
        {
            get
            {
                if (maxMRUValues < 0) maxMRUValues = DEFAULT_MAXMRU_VALUES;
                return maxMRUValues;
            }
            set
            {
                maxMRUValues = value;
                if (maxMRUValues < 0) maxMRUValues = DEFAULT_MAXMRU_VALUES;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// We are only permitted to keep maxMRUValues here. So we trim it as 
        /// necessary
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        private void TrimFileListToMRUMaxValues()
        {
            // first some checks
            if (this.listBoxFileList.Items.Count == 0) return;
            int numToTrim = this.listBoxFileList.Items.Count-MaxMRUValues;
            // do we have to do anything?
            if (numToTrim <= 0) return;
            // yes we do, get the list 
            List<string> tmpList = FileList;
            // drop them off the bottom
            for (int index = FileList.Count - 1; index > FileList.Count - numToTrim; index--)
            {
                tmpList.RemoveAt(index);
            }
            // put it back
            listBoxFileList.Items.Clear();
            listBoxFileList.Items.AddRange(tmpList.ToArray());
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Gets/Sets the filelist as a generic list of strings
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        public List<string> FileList
        {
            set
            {
                if (value == null) return;
                listBoxFileList.Items.Clear();
                listBoxFileList.Items.AddRange(value.ToArray());
                TrimFileListToMRUMaxValues();
            }
            get
            {
                List<string> tmpList = new List<string>();
                foreach(object strObj in listBoxFileList.Items)
                {
                    if((strObj is string) == false) continue;
                    tmpList.Add((strObj as string));
                }
                return tmpList;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Adds a filename to the top of the list
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        public void AddFileNameToTop(string fileName)
        {
            int existingIndex;
            // sanity check
            if ((fileName == null) || (fileName.Length == 0)) return;
            // get the list 
            List<string> tmpList = FileList;

            // note the lambda here, way cool way to represent an anonymous method with an 
            // inline delegate. Yummy!
            existingIndex = tmpList.FindIndex(s => s == fileName);
            // did we find one?
            if (existingIndex >= 0)
            {
                 // yes we did, remove it then re-insert it
                tmpList.RemoveAt(existingIndex);
            }
            // be sure to remove the DEFAULT_NOFILES_NOTICE also
            existingIndex = tmpList.FindIndex(s => s == DEFAULT_NOFILES_NOTICE);
            // did we find one?
            if (existingIndex >= 0)
            {
                // yes we did, remove it then re-insert it
                tmpList.RemoveAt(existingIndex);
            }
            // insert filename right at the top now
            tmpList.Insert(0,fileName);
            // put it back now
            FileList = tmpList;
            // make sure this is in sync
            SyncSelectedFileName();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Removes a file from the list
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        public void RemoveFileName(string fileName)
        {
            // sanity check
            if ((fileName == null) || (fileName.Length == 0)) return;
            // get the list 
            List<string> tmpList = FileList;
            // note the lambda here, way cool way to represent an anonymous method with an 
            // inline delegate. Yummy!
            int existingIndex = tmpList.FindIndex(s => s == fileName);
            // did we find one?
            if (existingIndex <= 0)
            {
                // no we did not - no need to do anything
                return;
            }
            // yes we did, remove it 
            tmpList.RemoveAt(existingIndex);
            // do we have no file left?
            if (tmpList.Count == 0)
            {
                // yes, we removed the last file, Put this in here
                tmpList.Add(DEFAULT_NOFILES_NOTICE);
            }
            // put the list back now
            FileList = tmpList;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Handles a click on the OK button.
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        private void buttonOk_Click(object sender, EventArgs e)
        {
            if ((SelectedFileName == null) || (SelectedFileName.Length == 0))
            {
                // just assume a cancel
                DialogResult = DialogResult.Cancel;
            }
            else DialogResult = DialogResult.OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Handles a click on the Cancel button.
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// handles changes to the selected file
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        private void listBoxFileList_SelectedIndexChanged(object sender, EventArgs e)
        {
            SyncSelectedFileName();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Synchronized the selected filename
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        private void SyncSelectedFileName()
        {
            if (listBoxFileList.SelectedItem == null)
            {
                textBoxChosenFile.Text = "";
                this.buttonOk.Enabled = false;
            }
            else
            {
                this.buttonOk.Enabled = true;
                this.textBoxChosenFile.Text = listBoxFileList.SelectedItem.ToString();
            }
            // never let the DEFAULT_NOFILES_NOTICE go in this box
            if (textBoxChosenFile.Text == DEFAULT_NOFILES_NOTICE)
            {
                textBoxChosenFile.Text = "";
                this.buttonOk.Enabled = false;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Returns the selected file name. Will never return null
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        public string SelectedFileName
        {
            get
            {
                if (textBoxChosenFile.Text == null) return "";
                return textBoxChosenFile.Text;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// handles the form shown event
        /// </summary>
        /// <history>
        ///    08 Aug 10  Cynic - Started
        /// </history>
        private void frmOISMRUList_Shown(object sender, EventArgs e)
        {
            SyncSelectedFileName();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// handles a double click on a filename
        /// </summary>
        /// <history>
        ///    31 Aug 10  Cynic - Started
        /// </history>
        void listBoxFileList_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            // did we click on an item
            int index = listBoxFileList.IndexFromPoint(e.X, e.Y);
            if(index < 0) return;

            SyncSelectedFileName();
            // do we have selected filename, check this?
            if(buttonOk.Enabled==true)
            {
                // yes we do, simulate an ok click
                buttonOk_Click(this, new EventArgs());
            }
        }
    }
}

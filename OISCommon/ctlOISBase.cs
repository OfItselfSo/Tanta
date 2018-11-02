using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
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

namespace OISCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
    /// <summary>
    /// Parent of all controls in the OIS apps. Basically provides logging and
    /// other options in a transparent way which still allows the ctl to inherit
    /// from UserControl.
    /// </summary>
    /// <history>
    ///    07 Nov 09  Cynic - Started
    /// </history>
    public partial class ctlOISBase : UserControl
    {
        // the logger instance
        protected OISLogger g_Logger;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    07 Nov 09  Cynic - Started
        /// </history>
        public ctlOISBase()
        {
            // Acquire the Singleton g_Logger instance - this must be done first
            g_Logger = OISLogger.OISLoggerInstance;

            InitializeComponent();
        }

        // ########################################################################
        // ##### Log and Diagnostic Code
        // ########################################################################

        #region Log and Diagnostic Code

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A wrapper to launch a modal Yes/No Message box, with logging
        /// </summary>
        /// <param name="boxText">The text to display in the box</param>
        /// <history>
        ///    31 Aug 10  Cynic - Started
        /// </history>
        public DialogResult OISMessageBox_YesNo(string boxText)
        {
            if (boxText == null) boxText = "";
            DialogResult dlgRes = MessageBox.Show(this, boxText, g_Logger.DefaultDialogBoxTitle, MessageBoxButtons.YesNo);
            LogMessage("dlgres=" + dlgRes.ToString());
            return dlgRes;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A wrapper to launch a modal Yes/No Message box, with logging
        /// </summary>
        /// <param name="boxText">The text to display in the box</param>
        /// <param name="boxTitle">The box title</param>
        /// <history>
        ///    31 Aug 10  Cynic - Started
        /// </history>
        public DialogResult OISMessageBox_YesNo(string boxText, string boxTitle)
        {
            if (boxTitle == null) boxTitle = "";
            if (boxText == null) boxText = "";
            LogMessage(boxTitle + " " + boxText);
            DialogResult dlgRes = MessageBox.Show(this, boxText, boxTitle, MessageBoxButtons.YesNo);
            LogMessage("dlgres=" + dlgRes.ToString());
            return dlgRes;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A wrapper to launch a modal Yes/No/Cancel Message box, with logging
        /// </summary>
        /// <param name="boxText">The text to display in the box</param>
        /// <history>
        ///    31 Aug 10  Cynic - Started
        /// </history>
        public DialogResult OISMessageBox_YesNoCancel(string boxText)
        {
            if (boxText == null) boxText = "";
            DialogResult dlgRes = MessageBox.Show(this, boxText, g_Logger.DefaultDialogBoxTitle, MessageBoxButtons.YesNoCancel);
            LogMessage("dlgres=" + dlgRes.ToString());
            return dlgRes;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A wrapper to launch a modal Yes/No/Cancel Message box, with logging
        /// </summary>
        /// <param name="boxText">The text to display in the box</param>
        /// <param name="boxTitle">The box title</param>
        /// <history>
        ///    31 Aug 10  Cynic - Started
        /// </history>
        public DialogResult OISMessageBox_YesNoCancel(string boxText, string boxTitle)
        {
            if (boxTitle == null) boxTitle = "";
            if (boxText == null) boxText = "";
            LogMessage(boxTitle + " " + boxText);
            DialogResult dlgRes = MessageBox.Show(this, boxText, boxTitle, MessageBoxButtons.YesNoCancel);
            LogMessage("dlgres=" + dlgRes.ToString());
            return dlgRes;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A wrapper to launch a modal Message box, with logging
        /// </summary>
        /// <param name="boxText">The text to display in the box</param>
        /// <param name="boxTitle">The box title</param>
        /// <history>
        ///    07 Nov 09  Cynic - Started
        /// </history>
        public void OISMessageBox(string boxText, string boxTitle)
        {
            if (boxTitle == null) boxTitle = "";
            if (boxText == null) boxText = "";
            LogMessage(boxTitle + " " + boxText);
            MessageBox.Show(this, boxText, boxTitle, MessageBoxButtons.OK);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A wrapper to launch a modal Message box, with logging
        /// </summary>
        /// <param name="boxText">The text to display in the box</param>
        /// <history>
        ///    07 Nov 09  Cynic - Started
        /// </history>
        public void OISMessageBox(string boxText)
        {
            if (boxText == null) boxText = "";
            LogMessage(boxText);
            MessageBox.Show(this, boxText);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Simple wrapper for the most common record message call
        /// </summary>
        /// <param name="msgText">Text to Write to the Log</param>
        /// <history>
        ///    07 Nov 09  Cynic - Started
        /// </history>
        public void LogMessage(string msgText)
        {
            // write it out to the log - but prepend the object type name
            if (g_Logger != null) g_Logger.RecordMessage(this.GetType().ToString() + ": " + msgText);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Simple wrapper for the most common debug mode record message call
        /// </summary>
        /// <param name="msgText">Text to Write to the Log</param>
        /// <history>
        ///    07 Nov 09  Cynic - Started
        /// </history>
#if DEBUG
        public void DebugMessage(string msgText)
        {
            // write it out to the log in debug mode - but prepend the object type name
            if (g_Logger != null) g_Logger.RecordMessage(this.GetType().ToString() + ": " + msgText);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Does nothing but place a marker in the text that will not compile out
        /// of debug mode. This should be used to mark stuff that absolutely must be
        /// done before release
        /// </summary>
        /// <param name="msgText">The thing To Do</param>
        /// <history>
        ///    07 Nov 09  Cynic - Started
        /// </history>
        public void DebugTODO(string msgText)
        {
        }
#endif

        #endregion

    }
}

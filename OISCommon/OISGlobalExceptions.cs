using System;
using System.Text;
using System.Windows.Forms;
using System.Threading;

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
    /// OISGlobalExceptions: A class of last resort to globally handle any exceptions
    /// not handled elsewhere. Basically this just means putting the information out to
    /// the logs.  This member function based on code found at:
    /// http://samples.gotdotnet.com/quickstart/howto/doc/WinForms/WinFormsAppErrorHandler.aspx
    /// </summary>
    /// <history>
    ///    04 Nov 09  Cynic - Started
    /// </history>
    public class OISGlobalExceptions : OISObjBase
    {
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        public OISGlobalExceptions()
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle the exception event
        /// </summary>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        public void OnThreadException(object sender, ThreadExceptionEventArgs t) 
        {
            DialogResult result = DialogResult.Cancel;
            try 
            {
                result = this.ShowThreadExceptionDialog(t.Exception);
            }
            catch 
            {
                try 
                {
                    MessageBox.Show("Fatal Error", "Fatal Error", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
                }
                finally 
                {
                    Application.Exit();
                }
            }
            // handle the result here.
            if (result == DialogResult.Abort) 
            {
                Application.Exit();
            }
            else
            {
                this.ShowExceptionContinueDialog();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// The dialog that is displayed when this class catches an exception
        /// </summary>
        /// <param name="e">The exception that caused this function to be called</param>
        /// <returns>DialogResult one of Abort, Retry, Ignore</returns>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        private DialogResult ShowThreadExceptionDialog(Exception e) 
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("An error occurred. The log file will contain the following helpful information:\n\n");
            if (e.Message != null) sb.Append(e.Message);
            if (e.StackTrace != null) sb.Append(e.StackTrace);
            LogMessage(sb.ToString());
            return MessageBox.Show(sb.ToString(), "Application Error", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Stop);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// The dialog that is displayed when the use chooses to continue.
        /// </summary>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        private DialogResult ShowExceptionContinueDialog() 
        { 
            string errorMsg = "This software will attempt to continue.\n\nHowever, it might be wise at this time to save your work under a new name and possibly also stop and restart this application.";
            LogMessage(errorMsg);
            return MessageBox.Show(errorMsg, "Application Restart", MessageBoxButtons.OK, MessageBoxIcon.Stop);
        }
    }
}

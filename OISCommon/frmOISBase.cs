using System;
using System.Windows.Forms;
using System.Threading;
using System.Collections;
using System.Text;
using System.Drawing;
using Microsoft.Win32;
using System.IO;
using System.ComponentModel;

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
    /// Parent of all forms in the OIS apps. Basically provides logging and
    /// other options in a transparent way which still allows the ctl to inherit
    /// from System.Windows.Forms.Form.
    /// </summary>
    /// <history>
    ///    03 Nov 09  Cynic - Started
    /// </history>
    public class frmOISBase : System.Windows.Forms.Form
    {
        // the logger instance
        protected OISLogger g_Logger;

        // this code is used to launch things after startup
        //   sample usage:
        // StartActionWithDelay=new StartActionWithDelayDelegate(NameOfFunctionToCall);
        // BeginActionWithDelay(500);
        public delegate void StartActionWithDelayDelegate();
        public StartActionWithDelayDelegate StartActionWithDelay = null;
        private ThreadStart delayWorker;
        private Thread delayThread = null;
        private int delayTime = 0;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public frmOISBase()
        {
            // create a handle now. This prevents a nasty InvokeRequired bug
            // google (InvokeRequired bug handle) for more info
            this.CreateHandle();

            // Acquire the Singleton g_Logger instance 
            g_Logger = OISLogger.OISLoggerInstance;

            // set the icon for the form
            if(g_Logger!=null)
            {
                if(g_Logger.AppIcon!=null) this.Icon=g_Logger.AppIcon;
            }
        }
                 
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Turns the cursor into an hourglass or back off
        /// </summary>
        /// <param name="Show">if true we enable it if false we put back the default cursor</param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        protected void WaitCursor(bool enabled) 
        { 
            if (enabled == true) 
            { 
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.WaitCursor; 
            } 
            else 
            { 
                System.Windows.Forms.Cursor.Current = System.Windows.Forms.Cursors.Default; 
            } 
            return; 
        } 

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Accepts a string and figures out what area is required to display it.
        /// It then takes looks at the form factor of an existing label and increases
        /// the form dimensions so that the label becomes that size. It takes care to 
        /// preserve the height to width ratio of the original form
        /// </summary>
        /// <remarks>The text box must be anchored so the increase in form size will cause an increase
        /// in width</remarks>
        /// <param name="strIn">Incoming string to measure</param>
        /// <param name="labelIn">label which should be increased</param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        protected void AdjustFormToTextDimensions(string strIn, System.Windows.Forms.Label labelIn)
        {
            int textArea;
            int originalArea;
            int newHeight;
            int newWidth;
            int heightDifference;
            int widthDifference;

            if(labelIn==null) return;
            if(strIn==null) return;
            if(strIn.Length==0) return;

            System.Drawing.Graphics gr=this.CreateGraphics();
            SizeF sF=gr.MeasureString(strIn,labelIn.Font);

            // now we have the height and width calculate the total area
            // in square pixels 
            textArea=(int)sF.Height*(int)sF.Width;
            if(textArea<=0) return;
            // now calculate the original area of the text box
            originalArea=labelIn.Height*labelIn.Width;
            // if the new area is smaller than the old we do not need to do anything
            // we never shink the box
            if(originalArea>=textArea) return;
            // get the new height and width - which is in the same ratio as the 
            // original (this works - you do the math!!!)
            newHeight=(int)Math.Sqrt((textArea*labelIn.Height)/labelIn.Width);
            newWidth=(int)Math.Sqrt((textArea*labelIn.Width)/labelIn.Height);
            // now get the difference between the new height and the old height
            heightDifference=newHeight-labelIn.Height;
            widthDifference=newWidth-labelIn.Width;
            // some sanity checks
            if(heightDifference<=0) return;
            if(widthDifference<=0) return;

            // increase the form size (not the text box size) by the differences. The
            // text box should be anchored so the increase in form size will cause an increase
            // in width
            this.Height+=heightDifference;
            this.Width+=widthDifference;
            // some sanity checks
            if(this.Height>=640) this.Height=640;
            if(this.Width>=480) this.Width=480;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Launches the help file. The helpFileName should not have a path.
        /// This helpFileName is assumed to be an html file
        /// </summary>
        /// <param name="helpDirName">directory to use if null the global dir is used. if not rooted the application exe path is assumed</param>
        /// <param name="helpFileName">the name of the help file</param>
        /// <history>
        ///   13 Sep 10  Cynic - Originally written
        /// </history>
        public void LaunchHelpFile(string helpDirName, string helpFileName)
        {
            string dirName;

            // sanity check
            if (helpFileName == null) return;
            // set the dirname
            if (helpDirName == null) return;
            else dirName = helpDirName;

            try
            {
                // launch the file
                System.Diagnostics.Process p = new System.Diagnostics.Process();
                System.Diagnostics.ProcessStartInfo pi = new System.Diagnostics.ProcessStartInfo();
                // if the path doees not have a root use the startup directory

                if (Path.IsPathRooted(dirName) != true)
                {
                    pi.FileName = Path.Combine(System.Windows.Forms.Application.StartupPath, dirName);
                    pi.FileName = Path.Combine(pi.FileName, helpFileName);
                }
                else
                {
                    pi.FileName = Path.Combine(dirName, helpFileName);
                }
                // we expect the quick start guide to be immediately below the exe directory
                p.StartInfo = pi;
                p.Start();
            }
            catch (Exception)
            {
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Encapsulates everything required to get a folder by user interaction.
        /// </summary>
        /// <param name="boxTitle">Box Title</param>
        /// <param name="outDir">The output directory is returned in here</param>
        /// <param name="showNewFolder">if true show new folder button</param>
        /// <param name="startDir">the starting directory</param>
        /// <returns>true the user pressed ok, false the user cancelled</returns>
        /// <history>
        ///    03 Jan 11  Cynic - Started
        /// </history>
        public static bool GetDirectoryByPrompt(string startDir, string boxTitle, bool showNewFolder, out string outDir)
        {
            outDir = "";
            FolderBrowserDialog frmFolderBrowser = new FolderBrowserDialog();
            frmFolderBrowser.ShowNewFolderButton = true;
            frmFolderBrowser.SelectedPath = startDir;
            DialogResult dlgRes = frmFolderBrowser.ShowDialog();
            if (dlgRes != DialogResult.OK) return false;
            outDir = frmFolderBrowser.SelectedPath;
            return true;
        }

        // ########################################################################
        // ##### Code to support the action with delay functionality
        // ########################################################################
        #region ActionWithDelay

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A function which can be called to begin an action after an out of band
        /// delay. The action will be initiated on the forms window thread. The
        /// StartActionWithDelay delegate must be instantiated
        /// </summary>
        /// <param name="delayInMsIn">the delay time in miliseconds</param>
        /// <history>
        ///   06 Jun 11  Cynic - Originally written
        /// </history>
        protected void BeginActionWithDelay(int delayInMsIn)
        {
            if (StartActionWithDelay == null) return;
            delayTime = delayInMsIn;
            // start up the worker thread
            delayWorker = new ThreadStart(DoTheDelay);
            delayThread = new Thread(delayWorker);
            delayThread.CurrentCulture = g_Logger.GetDefaultCulture();
            delayThread.Start();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Actually performs the delay. Should be called in a different thread.
        /// Will call Invoke to initiate the StartActionWithDelay delegate on the 
        /// forms main window thread.
        /// </summary>
        /// <history>
        ///   06 Jun 11  Cynic - Originally written
        /// </history>
        private void DoTheDelay()
        {
            if (StartActionWithDelay == null) return;
            if (IsDisposed) return;

            Thread.Sleep(delayTime);
            // we have to invoke here - might be called from other threads
            if (InvokeRequired)
            {
                // we are now sure we are on the proper thread!
                this.Invoke(StartActionWithDelay);
                return;
            }
            else { StartActionWithDelay(); }
        }
        #endregion

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
        ///    08 Aug 10  Cynic - Started
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
        ///    08 Aug 10  Cynic - Started
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
        ///    10 Aug 10  Cynic - Started
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
        ///    10 Aug 10  Cynic - Started
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
        ///    03 Nov 09  Cynic - Started
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
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public void OISMessageBox(string boxText)
        {
            if (boxText == null) boxText = "";
            LogMessage(boxText);
            MessageBox.Show(this, boxText, g_Logger.DefaultDialogBoxTitle);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Simple wrapper for the most common record message call
        /// </summary>
        /// <param name="msgText">Text to Write to the Log</param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public void LogMessage(string msgText)
        {
            // write it out to the log - but prepend the object type name
            if(g_Logger!=null) g_Logger.RecordMessage(this.GetType().ToString()+ ": "+msgText);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Simple wrapper for the most common debug mode record message call
        /// </summary>
        /// <param name="msgText">Text to Write to the Log</param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
#if DEBUG
        public void DebugMessage(string msgText)
        {
            // write it out to the log in debug mode - but prepend the object type name
            if(g_Logger!=null) g_Logger.RecordMessage(this.GetType().ToString()+ ": "+msgText);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Does nothing but place a marker in the text that will not compile out
        /// of debug mode. This should be used to mark stuff that absolutely must be
        /// done before release
        /// </summary>
        /// <param name="msgText">The thing To Do</param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public void DebugTODO(string msgText)
        {
        }
#endif
#endregion
    }
}

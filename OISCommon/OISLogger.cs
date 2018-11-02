using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices; 
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

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
    /// This is a singleton class which contains logging and other misc items
    /// useful to an application. If you don't know what a Singleton class is 
    /// you should look it up. Otherwise the way this class works will not really
    /// make sense.
    /// 
    /// Note that calls to the RecordMessage() logwriters are thread safe
    /// due to the Writer lock acquisition.
    /// 
    /// The logfile stream is opened, flushed and closed with each call. This
    /// means RecordMessage() calls are not suitable for the fast recording of data.  
    /// They are too slow for that. However, if things crash the log will have
    /// everything that was written to it. Basically it is useful for recording
    /// way points in the code path and a back trail of user actions and errors. 
    /// 
    /// </summary>
    /// <history>
    ///    03 Nov 09  Cynic - Started
    /// </history>
    public sealed class OISLogger 
    {
        // constants
        private const string DEFAULTDIALOGBOXTITLE = "OIS Application Notice";
        private const string STANDARDLOGFILEEXT = ".log";
        private const string DEFAULTLOGFILENAME = "OISBaseApp";
        private const string DEFAULTLOGFILE_SUBDIR = "Log Files";
        const int timeOut = 100;   // logfile lock time out in milliseconds 100=.1sec

        private bool ignoreLogging = false;   
        private string defaultDialogBoxTitle = DEFAULTDIALOGBOXTITLE;
        private string logFileDirectory = null;
        private string logFileName = DEFAULTLOGFILENAME + STANDARDLOGFILEEXT;

        // this is the applications main form - only set it when the logger is created
        private frmOISBase applicationMainForm=null;

        private object randomNumberSeedLock=new object();
        private int randomNumberSeed=(int)DateTime.Now.Ticks; 

        // the main registry key of the application. We keep this in here because
        // there are some generic classes which need to record settings etc.
        private string applicationPrimaryRegistryKey=null;

        int uniqueObjectCounter=0;
        private Icon appIcon=null;

        // Declaring the ReaderWriterLock at the class level
        // makes it visible to all threads.
        static ReaderWriterLock rwl = new ReaderWriterLock();

        // instantiate the singleton OISLogger here
        public static readonly OISLogger OISLoggerInstance = new OISLogger();

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Constructor - note this is private. Required for a singleton class
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        private OISLogger()
        {
        }

        // ########################################################################
        // ##### Log Message Writers
        // ########################################################################

        #region Log Message Writers

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Log an exception.
        /// </summary>
        /// <param name="Message">Exception to LogBase. </param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public void RecordMessage(Exception Message)
        {
            this.RecordMessage(Message.Message);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Log a message. This is thread safe due to the Writer lock acquisition.
        /// We are opening, flushing and closing the stream with each call. This
        /// means this code is not suitable for the fast recording of data. It 
        /// is too slow for that. However, if things crash the log will have
        /// everything that was written to it. Basically it is useful for recording
        /// way points and a back trail of code paths and errors.
        /// </summary>
        /// <param name="msgIn">Message text. </param>
        /// <param name="wantTimeStamp">if true we output a timestamp</param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public void RecordMessage(string msgIn, bool wantTimeStamp)
        {
            // if we are ignoring logging then leave now
            if (ignoreLogging == true) return;
            if (msgIn == null) return;

            FileStream fileStream = null;
            StreamWriter writer = null;
            StringBuilder message = new StringBuilder();
            try
            {
                rwl.AcquireWriterLock(timeOut);
                try
                {
                    fileStream = new FileStream(this.LogFileDirectory +
                                                this.LogFileName,
                                                FileMode.OpenOrCreate,
                                                FileAccess.Write);
                    writer = new StreamWriter(fileStream);

                    // Set the file pointer to the end of the file
                    writer.BaseStream.Seek(0, SeekOrigin.End);

                    // Create the message
                    if(wantTimeStamp==true)
                    {
                        message.Append(System.DateTime.Now.ToString());
                        message.Append(" ");
                    }
                    message.Append(msgIn);

                    // Force the write to the underlying file
                    writer.WriteLine(message.ToString());
                    writer.Flush();
                }
                catch (Exception e)
                {
                    // will launch a dialog box if things are not right
                    HandleLoggerException(e);
                }
                finally
                {
                    // ensure the writer is closed
                    if (writer != null) writer.Close();
                    // Ensure that the lock is released.
                    if (rwl != null) rwl.ReleaseWriterLock();
                }
            }
            catch (ApplicationException)
            {
                // The writer lock request timed out. Not much we can do - ignore it
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Log a message.
        /// </summary>
        /// <param name="msgIne">Message text. </param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public void RecordMessage(string msgIn)
        {
            RecordMessage(msgIn,true);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Post details of an exception to the log. Also places the trace information
        /// out to the log
        /// </summary>
        /// <param name="e">The exception to log the details of</param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public void RecordException(Exception e)
        {
            RecordMessage("Exception Message: "+e.Message+"\nException Source: "+e.Source);
            RecordMessage(e.StackTrace);
        }
        #endregion

        // ########################################################################
        // ##### Logging Code
        // ########################################################################

        #region Logging Code

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Gets/Sets the ignore logging property
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public bool IgnoreLogging
        {
            get { return this.ignoreLogging; }
            set { this.ignoreLogging = value; }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Gets/Sets the log file name
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public string LogFileName
        {
            get
            {
                if (this.logFileName == null) logFileName = DEFAULTLOGFILENAME + STANDARDLOGFILEEXT;
                else if (this.logFileName.Length == 0) logFileName = DEFAULTLOGFILENAME + STANDARDLOGFILEEXT;
                return logFileName;
            }
            set
            {
                // we require it to have the standard extension - if it does not then
                // tack it on
                if (value == null) logFileName = logFileName = DEFAULTLOGFILENAME + STANDARDLOGFILEEXT;
                else if (value.Trim().Length == 0) logFileName = DEFAULTLOGFILENAME + STANDARDLOGFILEEXT;
                else if (value.Trim().Length <= STANDARDLOGFILEEXT.Length) logFileName = value.Trim() + STANDARDLOGFILEEXT;
                else
                {
                    if (value.Trim().EndsWith(STANDARDLOGFILEEXT) == false)
                    {
                        this.logFileName = value.Trim() + STANDARDLOGFILEEXT;
                    }
                    else this.logFileName = value.Trim();
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Get or set the log file directory location
        /// </summary>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        public string LogFileDirectory
        {
            get
            {
                // return "" if it is null. This should put the log in the default directory
                if (this.logFileDirectory == null) logFileDirectory = "";
                return this.logFileDirectory;
            }
            set
            {
                if (value == null) logFileDirectory = "";
                else if (value.Trim().Length == 0) logFileDirectory = "";
                else
                {
                    // Verify a '\' exists on the end of the location
                    if (value.Trim().EndsWith("\\") == false)
                    {
                        logFileDirectory = value.Trim() + "\\";
                    }
                    else
                    {
                        logFileDirectory = value.Trim();
                    }
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Initializes the logging 
        /// </summary>
        /// <returns>true logging inited, false it did not</returns>
        /// <param name="logDirName">The directory name for the log file. If null or "" we 
        /// use the current exe directory</param>
        /// <param name="logfileBaseName">The base name of the log file</param>
        /// <param name="wantDistinctName">if true we give it a distinct name</param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public bool InitLogging(string logDirName, string logfileBaseName, bool wantDistinctName)
        {
            string workingFileName = null;
            string workingDirName = null;

            // now build a filename for it
            if (wantDistinctName == false)
            {
                workingFileName = logfileBaseName;
            }
            else
            {
                workingFileName = logfileBaseName + "_" + System.DateTime.Now.ToString("yyyyMMddHHmmssfffffff");
            }
            // get the best directory we can think of
            workingDirName = GetBestLogfileDir(logDirName);
            if (workingDirName == null) return false;

            // set these now, they have checks built in
            LogFileDirectory = workingDirName;
            LogFileName = workingFileName;

            // this sets everything up
            this.Reset(LogFileDirectory, LogFileName);
            return true;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Figures out the best log directory name. Will create the directory if 
        /// it does not exist.
        /// </summary>
        /// <param name="logDir">the log directory to use, if null or "" we use the 
        /// application exe directory</param>
        /// <returns>log directory to use</returns>
        /// <history>
        ///    06 Nov 09  Cynic - Started
        /// </history>
        private string GetBestLogfileDir(string logDir)
        {
            string workingLogDir = logDir;

            // is this sensible?
            if (workingLogDir == null)
            {
                // could not be found so generate one
                workingLogDir = Path.Combine(Environment.CurrentDirectory, DEFAULTLOGFILE_SUBDIR);
            }

            // is it dodgy?
            if (Path.IsPathRooted(workingLogDir) == false)
            {
                // could not be found so generate one
                workingLogDir = Path.Combine(Environment.CurrentDirectory, DEFAULTLOGFILE_SUBDIR);
            }

            // does it exist?
            if (Directory.Exists(workingLogDir) == false)
            {
                Directory.CreateDirectory(workingLogDir);
            }
            // test again
            if (Directory.Exists(workingLogDir) == false)
            {
                return null;
            }
            // return it
            return workingLogDir;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Dumps a standard logfile header out to the log
        /// </summary>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        public void EmitStandardLogfileheader(string softwareName)
        {
            RecordMessage("###########################################################");
            if (softwareName != null)
            {
                RecordMessage("##### " + softwareName + " Software");
            }
            else
            {
                RecordMessage("###########################################################");
            }
            RecordMessage("###########################################################");
            RecordMessage("");
            RecordMessage("Operation System: " + Environment.OSVersion.ToString());
            RecordMessage(".NET Version: " + Environment.Version.ToString());
            RecordMessage("Current Directory: " + Environment.CurrentDirectory.ToString());
            RecordMessage("Machine Name: " + Environment.MachineName.ToString());
            RecordMessage("User Name: " + Environment.UserName.ToString());
            RecordMessage("");
            RecordMessage("Logging Begins");

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Reset the log file - create it as an empty file. Will overwrite if it
        ///     already exists. Call this before any writes to the log
        /// </summary>
        /// <param name="logFileDirectoryIn">Path to log file.</param>
        /// <param name="logFileNameIn">Name of log file. </param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public void Reset(string logFileDirectoryIn, string logFileNameIn)
        {
            FileStream fileStream = null;

            // use the properties to set these - they perform checks
            LogFileDirectory = logFileDirectoryIn;
            LogFileName = logFileNameIn;
            try
            {
                rwl.AcquireWriterLock(timeOut);
                try
                {
                    fileStream = new FileStream(this.LogFileDirectory +
                                                this.LogFileName,
                                                FileMode.Create,
                                                FileAccess.Write);
                    fileStream.Close();
                }
                catch (Exception e)
                {
                    // will launch a dialog box if things are not right
                    HandleLoggerException(e);
                }
                finally
                {
                    // ensure the filestream is closed
                    if (fileStream != null) fileStream.Close();
                    // Ensure that the lock is released.
                    if (rwl != null) rwl.ReleaseWriterLock();
                }
            }
            catch (ApplicationException)
            {
                // The writer lock request timed out. Not much we can do - ignore it
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// This is called by the logger when an error occurs. It tells the user
        /// what error happened and offers to fix it or to run without logging.
        /// </summary>
        /// <param name="errText">The error text to post</param>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public void HandleLoggerException(Exception e)
        {
            // you CANNOT call the standard logging functions here. The 
            // Standard logging functions are what caused the problem in the
            // first place. Stick soley with the Windows API !!!!
            frmOISLoggerException dlgObj = new frmOISLoggerException(e);
            if (dlgObj == null)
            {
                IgnoreLogging = true;
                return;
            }
            dlgObj.Text = DefaultDialogBoxTitle + " Log File Error";
            dlgObj.LogFileDirectory = LogFileDirectory;
            dlgObj.LogFileName = LogFileName;
            DialogResult result = dlgObj.ShowDialog();
            // the box is set to return DialogResult.OK for "use new logfile path and name"
            // and DialogResult.Ignore for "Turn Logging Off"
            if (result == DialogResult.OK)
            {
                // reset these
                LogFileDirectory = dlgObj.LogFileDirectory;
                LogFileName = dlgObj.LogFileName;
            }
            else
            {
                // turn logging off
                IgnoreLogging = true;
            }
        }

        #endregion

        // ########################################################################
        // ##### Misc. Properties and Functions
        // ########################################################################

        #region Misc. Properties and Functions

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the "this" value of the applications main form. This should 
        /// only be set once - immediately after the first instance of the singleton
        /// logger object is created.
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public frmOISBase ApplicationMainForm
        {
            get
            {
                return applicationMainForm;
            }
            set
            {
                applicationMainForm = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the default culture we use for most things
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public System.Globalization.CultureInfo GetDefaultCulture()
        {
            System.Globalization.CultureInfo culture = new System.Globalization.CultureInfo("");
            culture.NumberFormat.NumberDecimalSeparator = ".";
            culture.NumberFormat.NumberGroupSeparator = "";
            return culture;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the random number seed for the application
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public int RandomNumberSeed
        {
            get
            {
                // we do this so that nobody ever gets the same seed
                lock (randomNumberSeedLock)
                {
                    return ++randomNumberSeed;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Set the applications primary registry key.
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public string ApplicationPrimaryRegistryKey
        {
            get
            {
                return applicationPrimaryRegistryKey;
            }
            set
            {
                applicationPrimaryRegistryKey = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the default icon for this application
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public Icon AppIcon
        {
            get
            {
                return appIcon;
            }
            set
            {
                appIcon = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Gets/Sets the default dialog box title
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public string DefaultDialogBoxTitle
        {
            get
            {
                if (defaultDialogBoxTitle == null)
                {
                    defaultDialogBoxTitle = DEFAULTDIALOGBOXTITLE;
                }
                return this.defaultDialogBoxTitle;
            }
            set
            {
                if (value == null) defaultDialogBoxTitle = DEFAULTDIALOGBOXTITLE;
                else defaultDialogBoxTitle = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Make a Beep sound - just essentially the Win32 MessageBeep()
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        [DllImport("user32")]
        public static extern int MessageBeep(int wType);

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Play a system .wav sound file.
        /// Usage: OISLogger.PlaySound("beep.wav", 0, OISLogger.SND_logFileName | OISLogger.SND_ASYNC); 
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public const int SND_logFileName = 0x00020000;
        public const int SND_ASYNC = 0x0001;
        [DllImport("winmm.dll")]
        public static extern bool PlaySound(string pszSound, int hmod, int fdwSound);

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Returns a temporary object name. We put it here in a singleton class
        /// so that we can crank up a counter for every one - this ensuring it
        /// is unique
        /// </summary>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public string GetTempObjectName(string leadingStr)
        {
            lock (this)
            {
                uniqueObjectCounter++;
                return leadingStr + System.DateTime.Now.ToString("yyyyMMddHHmmssfffffff") + uniqueObjectCounter.ToString();
            }
        }

        #endregion

    }
}

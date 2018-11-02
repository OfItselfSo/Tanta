using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MediaFoundation;
using MediaFoundation.ReadWrite;
using MediaFoundation.Misc;
using MediaFoundation.Transform;
using MediaFoundation.Alt;
using OISCommon;
using TantaCommon;

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

/// Notes
/// Some parts of this code may be derived from the samples which ships with the MF.Net dll. These are 
/// in turn derived from the original Microsoft samples. These have been placed in the public domain 
/// without copyright.
/// 
/// The MF.Net library itself is licensed under the GNU Library or Lesser General Public License version 2.0 (LGPLv2)
/// 

/// SUPER IMPORTANT NOTE: You MUST use the [MTAThread] to decorate your entry point method. If you use the default [STAThread] 
/// you may get errors. See the Program.cs file for details.

/// The function of this code is to read a video file from disk and play 
/// it on the screen. It is a rewrite of the WMF Basic Playback sample.
/// If you wish to see a much simpler version of this functionality
/// see the TantaFilePlaybackSimple sample application

namespace TantaFilePlaybackAdvanced
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// The main form for the application
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Originally Written
    /// </history>
    public partial class frmMain : frmOISBase
    {
        private const string DEFAULTLOGDIR = @"C:\Dump\Project Logs";
        private const string APPLICATION_NAME = "TantaFilePlaybackAdvanced";
        private const string APPLICATION_VERSION = "01.00";

        //     public const string INITIALFILE = @"C:\Dump\SampleVideo_720x480_1mb.mp4"; // 5 Sec
        public const string INITIALFILE = @"C:\Dump\SampleVideo_1280x720_5mb.mp4"; // 30 Sec
  
        // the number of msec delay between heartbeats
        private const int HEARTBEAT_DELAYTIME = 100;

        // our heart beat - it performs all the necessary periodic
        // update actions. 
        private Thread heartBeatThread = null;
        private bool stopHeartBeat = false;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public frmMain()
        {
            bool retBOOL = false;
            HResult hr = 0;

            if (DesignMode == false)
            {
                // set the current directory equal to the exe directory. We do this because
                // people can start from a link and if the start-in directory is not right
                // it can put the log file in strange places
                Directory.SetCurrentDirectory(Application.StartupPath);

                // set up the Singleton g_Logger instance. Simply using it in a test
                // creates it.
                if (g_Logger == null)
                {
                    // did not work, nothing will start say so now in a generic way
                    MessageBox.Show("Logger Class Failed to Initialize. Nothing will work well.");
                    return;
                }
                // record this in the logger for everybodys use
                g_Logger.ApplicationMainForm = this;
                g_Logger.DefaultDialogBoxTitle = APPLICATION_NAME;
                try
                {
                    // set the icon for this form and for all subsequent forms
                    g_Logger.AppIcon = new Icon(GetType(), "App.ico");
                    this.Icon = new Icon(GetType(), "App.ico");
                }
                catch (Exception)
                {
                }

                // Register the global error handler as soon as we can in Main
                // to make sure that we catch as many exceptions as possible
                // this is a last resort. All execeptions should really be trapped
                // and handled by the code.
                OISGlobalExceptions ex1 = new OISGlobalExceptions();
                Application.ThreadException += new ThreadExceptionEventHandler(ex1.OnThreadException);

                // set the culture so our numbers convert consistently
                System.Threading.Thread.CurrentThread.CurrentCulture = g_Logger.GetDefaultCulture();
            }

            InitializeComponent();

            if (DesignMode == false)
            {
                // set up our logging
                retBOOL = g_Logger.InitLogging(DEFAULTLOGDIR, APPLICATION_NAME, false);
                if (retBOOL == false)
                {
                    // did not work, nothing will start say so now in a generic way
                    MessageBox.Show("The log file failed to create. No log file will be recorded.");
                }
                // pump out the header
                g_Logger.EmitStandardLogfileheader(APPLICATION_NAME);
                LogMessage("");
                LogMessage("Version: " + APPLICATION_VERSION);
                LogMessage("");

                // we always have to initialize MF. The 0x00020070 here is the WMF version 
                // number used by the MF.Net samples. Not entirely sure if it is appropriate
                hr = MFExtern.MFStartup(0x00020070, MFStartup.Full);
                if (hr != 0)
                {
                    LogMessage("Constructor: call to MFExtern.MFStartup returned " + hr.ToString());
                }

                // some initial configuration
                this.textBoxVideoFileNameAndPath.Text = INITIALFILE;
                ctlTantaEVRFilePlayer1.VideoFileAndPathToPlay = INITIALFILE;

                // set our heartbeat going
                LaunchHeartBeat();
            }

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The form loaded handler
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void frmMain_Load(object sender, EventArgs e)
        {
            LogMessage("frmMain_Load");
            // let the EVR tell us about some events
            ctlTantaEVRFilePlayer1.TantaEVRFilePlayerStateChangedEvent = PlayerStateChangedEventHandler;

            ctlTantaEVRFilePlayer1.InitMediaPlayer();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The form closing handler
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogMessage("frmMain_FormClosing");
            try
            {
                // stop the heartbeat
                KillHeartbeat();

                // do everything to close all media devices
                CloseAllMediaDevices();

                // shutdown our EVR player
                ctlTantaEVRFilePlayer1.ShutDownFilePlayer();

                // Shut down MF
                MFExtern.MFShutdown();
            }
            catch
            {
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A centralized place to close down all media devices 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void CloseAllMediaDevices()
        {
            // close down our EVR player
            ctlTantaEVRFilePlayer1.CloseAllMediaDevices();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles a press on the buttonPickFile button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonPickFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            // we only permit this action if we are not playing
            if (ctlTantaEVRFilePlayer1.PlayerState != TantaEVRPlayerStateEnum.Ready)
            {
                OISMessageBox("A video is currently playing");
                return;
            }

            openFileDialog1.Filter = "Windows Media|*.wmv;*.wma;*.asf;*.mp4|Wave|*.wav|MP3|*.mp3|All files|*.*";

            // File dialog windows must be on STA threads.  ByteStream handlers are happier if
            // they are opened on MTA.  So, the application stays MTA and we call OpenFileDialog
            // on its own thread.
            TantaOpenFileDialogInvoker invokerObj = new TantaOpenFileDialogInvoker(openFileDialog1);
            // Show the File Open dialog.
            if (invokerObj.Invoke() == DialogResult.OK)
            {
                // pick the file
                textBoxVideoFileNameAndPath.Text = openFileDialog1.FileName;
                ctlTantaEVRFilePlayer1.VideoFileAndPathToPlay = openFileDialog1.FileName;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles typed file name changes
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void textBoxVideoFileNameAndPath_TextChanged(object sender, EventArgs e)
        {
            ctlTantaEVRFilePlayer1.VideoFileAndPathToPlay = textBoxVideoFileNameAndPath.Text;
        }
  
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Syncs the onscreen state to the video file and playing state
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void ProcessScreenUpdates()
        {
            // we need an invoke in here because this can be called
            // from other threads and we do NOT want to mess with 
            // the controls from anything but the form thread

            if (this.InvokeRequired == true)
            {
                Action d = new Action(ProcessScreenUpdates);
                this.Invoke(d, new object[] { });
                return;
            }

            // tell the EVR control to update it's screen. It does not
            // do this automatically
            ctlTantaEVRFilePlayer1.UpdateScreen();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle player state changed events
        /// </summary>
        /// <param name="sender">the sender object</param>
        /// <param name="playerState">the new player state</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void PlayerStateChangedEventHandler(object sender, TantaEVRPlayerStateEnum playerState)
        {
            SyncScreenStateToPlayerState(playerState);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sync the screen state to the player state
        /// </summary>
        /// <param name="playerState">the player state</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SyncScreenStateToPlayerState(TantaEVRPlayerStateEnum playerState)
        {
            if (playerState == TantaEVRPlayerStateEnum.Ready)
            {
                textBoxVideoFileNameAndPath.Enabled = true;
                buttonPickFile.Enabled = true;
                labelVideoFilePathAndName.Enabled = true;
            }
            else
            {
                textBoxVideoFileNameAndPath.Enabled = false;
                buttonPickFile.Enabled = false;
                labelVideoFilePathAndName.Enabled = false;
            }
        }

        // ########################################################################
        // ##### HeartbeatCode - code to handle the heartbeat thread
        // ########################################################################

        #region HeartbeatCode

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The heartbeat is a continuously cycling thread which we use to 
        /// handle the periodic automatic updating of various screen controls
        /// </summary>
        /// <history>
        ///  01 Nov 18  Cynic - Originally written
        /// </history>
        private void LaunchHeartBeat()
        {
            // never start two of them
            if (heartBeatThread != null) return;
            ThreadStart heartBeatJob = new ThreadStart(HeartBeat);
            heartBeatThread = new Thread(heartBeatJob);
            heartBeatThread.Start();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Kills off the heartbeat. Only called when the app is shutting down
        /// </summary>
        /// <history>
        ///   01 Nov 18  Cynic - Originally written
        /// </history>
        public void KillHeartbeat()
        {
            // kill off the heartbeat - it will trap this exception
            stopHeartBeat = true;
            if (heartBeatThread != null)
            {
                heartBeatThread.Abort();
                heartBeatThread = null;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The heart beat handles repetitive tasks which need to happen periodically
        /// If you call anything which is going to interact with a form or 
        /// control you must set up a Invoke delegate mechanism otherwise you will
        /// "non thread safe" issues. 
        /// </summary>
        /// <history>
        ///   01 Nov 18  Cynic - Originally written
        /// </history>
        private void HeartBeat()
        {
            try
            {
                // this is where the heart beat does its stuff we do not
                // stop till we get a Thread.Abort
                while (true)
                {
                    // this will wake up every so often to perform its
                    // required processing
                    try
                    {
                        Thread.Sleep(HEARTBEAT_DELAYTIME);

                        // should we exit
                        if (stopHeartBeat == true) break;

                        // this contains everything we need to do
                        ProcessScreenUpdates();

                    }
                    catch (ThreadAbortException)
                    {
                        // this just means the heartbeat stopped
                        break;
                    }
                    catch (Exception)
                    {
                    }
                } // bottom of while(true)
            }
            catch (ThreadAbortException)
            {
            }
            finally
            {
                heartBeatThread = null;
            }
        }

        #endregion


    }
}

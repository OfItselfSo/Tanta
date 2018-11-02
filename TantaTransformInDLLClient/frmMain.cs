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
using System.Reflection;

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

/// The function of this app is to demonstrate how to find a Transform in the registry and to place it in the 
/// video pipleline. This code expects to use the MFTTantaVideoRotator_Sync transform provided by the TantaTransformInDLL 
/// sample solution. This application also demonstrates a method of communication which can be used with a
/// a dynamically loaded Transform in order to exchange information with it while it is running.
/// 
/// The MFTTantaVideoRotator_Sync transform provided by the TantaTransformInDLL is designed to be able to rotate
/// the video on display through various orientations.
/// 
/// This code picks up a file off a disk and displays the video. The ctlTantaEVRFilePlayer is used to display the video.  
/// The TantaFilePlayback sample contains a much more fully implemented version of this process in the TantaFilePlayer 
/// sample. You should probably familiarize yourself with that code before working with this one. 
/// 
/// The user can choose to use the Rotator transform from via a radio buttons. The video on screen will display the effects
/// of that transform. The transform must be chosen before the video is played but once the video is playing the 
/// various rotation options can be used to instruct the Rotator transform to adjust its output.
/// 
/// The setting of the rotation option is an example of client/transform communication and is accomplished by
/// setting an Attribute on the Transform. If the transform is written in .NET it is also possible to use C# 
/// and Reflection for client/transform communication. There are three buttons on the bottom of the form
/// which demonstrate this. All they really do is get the current frame count in the transform - but use a get/set 
/// call to a property and a call to a function with a ref parameter to demonstrate this.
/// 
/// This client application knows the GUID of the MFTTantaVideoRotator_Sync transform which is how it
/// knows which one to load via COM. It does not do discovery - for an example of that process see the
/// TantaTransformPicker sample application.
/// 
/// There is also a sample appplicaton named TantaTransforms which demonstrates the usage of transforms in which
/// the the transform code is compiled directly into the application. That sample has multiple transforms which 
/// demonstrate both Sync and Async modes. Also because it is directly compiled, you can set breakpoints and step through
/// the parts that interest you. If you are interested in learning about Transforms that sample is the place to start.
/// Use this one as a reference to see how to load a Transform from the Registry and communicate with it.


namespace TantaTransformInDLLClient
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
        private const string APPLICATION_NAME = "TantaTransformInDLLClient";
        private const string APPLICATION_VERSION = "01.00";

        //     public const string INITIALFILE = @"C:\Dump\SampleVideo_720x480_1mb.mp4"; // 5 Sec
        public const string INITIALFILE = @"C:\Dump\SampleVideo_1280x720_5mb.mp4"; // 30 Sec

        // the number of msec delay between heartbeats
        private const int HEARTBEAT_DELAYTIME = 100;

        // our heart beat - it performs all the necessary periodic
        // update actions. 
        private Thread heartBeatThread = null;
        private bool stopHeartBeat = false;

        // this Guid is the GUID of the rotator transform. In this example
        // we know this. The transform is properly registered by the 
        // TantaTransformInDLL C# solution so we could enumerate the registry
        // for it - but then we would have to know the name or some other 
        // characteristic. We will just keep it simple here. See the 
        // TantaTransformPicker sample code for an example of how to dig 
        // a transform out of the registry
        private Guid clsidRotatorTransform = new Guid("F1E67619-FB5B-470B-9306-EBF40D54985E");

        // this Guid is the key we use to set the FlipMode on the attributes
        // of the transform we inserted into the PipeLine. The FlipMode is 
        // retrieved by the Transform so it also needs to know this Guid. 
        // Other than that, there is nothing special about this value. 
        private Guid clsidFlipMode = new Guid("EF5FB03A-23B5-4250-9AA6-0E70907F8B4B");

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
            // set it up
            ctlTantaEVRFilePlayer1.InitMediaPlayer();

            SyncScreenStateToPlayerState(TantaEVRPlayerStateEnum.Ready);
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

        // 

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
        /// Handle a checked changed on the transform radio button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void radioButtonMFTNone_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonMFTNone.Checked == false) return;
            // give it the transform
            SetTransformOnEVRControl(Guid.Empty);
            SyncRotateGroupToTransformState();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle a checked changed on the transform radio button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void radioButtonMFTRotator_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonMFTRotator.Checked == false) return;
            // give it the transform Guid
            SetTransformOnEVRControl(clsidRotatorTransform);
            SyncRotateGroupToTransformState();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle a checked changed on the flip mode radio button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void radioButtonRotateNoneFlipNone_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonRotateNoneFlipNone.Checked == false) return;
            // set the flip mode
            SyncFlipModeOnTransformToCurrentState();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle a checked changed on the flip mode radio button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void radioButtonRotate180FlipNone_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonRotate180FlipNone.Checked == false) return;
            // set the flip mode
            SyncFlipModeOnTransformToCurrentState();
        }
    
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle a checked changed on the flip mode radio button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void radioButtonRotate180FlipX_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonRotate180FlipX.Checked == false) return;
            // set the flip mode
            SyncFlipModeOnTransformToCurrentState();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle a checked changed on the transform radio button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void radioButtonRotateNoneFlipX_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonRotateNoneFlipX.Checked == false) return;
            // set the flip mode
            SyncFlipModeOnTransformToCurrentState();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Set the transform on the EVR control. Can only be done when the 
        /// video is not playing.
        /// </summary>
        /// <param name="transformGuid">the GUID of the transform object to use, 
        /// can be Guid.Empty for no transform</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SetTransformOnEVRControl(Guid transformGuid)
        {

            // we only permit this action if we are not playing
            if (ctlTantaEVRFilePlayer1.PlayerState != TantaEVRPlayerStateEnum.Ready)
            {
                OISMessageBox("A video is currently playing");
                return;
            }

            // give it to the EVR player
            ctlTantaEVRFilePlayer1.VideoTransformGuid = transformGuid;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Set the flip type on our rotator transform in the EVR control
        /// </summary>
        /// <param name="flipType">the fliptype to use</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SetFlipModeOnTransform(RotateFlipType flipType)
        {
            // get the attribute container from the transform in the EVR player
            IMFAttributes attributeContainer = ctlTantaEVRFilePlayer1.GetTransformAttributes();
            if (attributeContainer == null) return;

            // set the fliptype as an int32. Attributes cannot contain enums
            HResult hr = attributeContainer.SetUINT32(clsidFlipMode, (int)flipType);

            // release it
            System.Runtime.InteropServices.Marshal.ReleaseComObject(attributeContainer);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Syncs the flip mode on the rotator transform in the EVR control to the 
        /// current state of the screen controls
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SyncFlipModeOnTransformToCurrentState()
        {
            if (radioButtonRotateNoneFlipNone.Checked == true) SetFlipModeOnTransform(RotateFlipType.RotateNoneFlipNone);
            else if (radioButtonRotate180FlipNone.Checked == true) SetFlipModeOnTransform(RotateFlipType.Rotate180FlipNone);
            else if (radioButtonRotate180FlipX.Checked == true) SetFlipModeOnTransform(RotateFlipType.Rotate180FlipX);
            else if (radioButtonRotateNoneFlipX.Checked == true) SetFlipModeOnTransform(RotateFlipType.RotateNoneFlipX);
            else SetFlipModeOnTransform(RotateFlipType.RotateNoneFlipNone);
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
            // are we about to open and set up ?
            if(playerState == TantaEVRPlayerStateEnum.OpenPending)
            {
                // we do nothing here. The transform to use is already
                // set in the checked changed event of the radio buttons
            }
            else if (playerState == TantaEVRPlayerStateEnum.StartPending)
            {
                // we are about to start, we need to tell the transform
                // what rotate mode to use. We cannot do this before now
                // because the transform has to be found and instantiated
                // before we can set the FlipMode on it
                SyncFlipModeOnTransformToCurrentState();
            }

                // always do this
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
            if(playerState == TantaEVRPlayerStateEnum.Ready) 
            {
                groupBoxTransforms.Enabled = true;
                textBoxVideoFileNameAndPath.Enabled = true;
                buttonPickFile.Enabled = true;
                labelVideoFilePathAndName.Enabled = true;
            }
            else
            {
                groupBoxTransforms.Enabled = false;
                textBoxVideoFileNameAndPath.Enabled = false;
                buttonPickFile.Enabled = false;
                labelVideoFilePathAndName.Enabled = false;
             }
            SyncRotateGroupToTransformState();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sync the rotate mode radio buttons to the screen options
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SyncRotateGroupToTransformState()
        {
            // 
            if (radioButtonMFTRotator.Checked == true)
            {
                groupBoxRotateMode.Enabled = true;
                // also do the comms demonstrator buttons
                buttonResetFrameCount.Enabled = true;
                buttonGetFCViaProperty.Enabled = true;
                buttonGetFCViaFunction.Enabled = true;
                labelMFTCommsDemonstrators.Enabled = true;
            }
            else
            {
                groupBoxRotateMode.Enabled = false;
                // also do the comms demonstrator buttons
                buttonResetFrameCount.Enabled = false;
                buttonGetFCViaProperty.Enabled = false;
                buttonGetFCViaFunction.Enabled = false;
                labelMFTCommsDemonstrators.Enabled = false;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Demonstrates the client/transform communications. Resets the
        /// frame count in the rotator transform by calling the set accessor
        /// of a property
        /// 
        /// This function uses late binding and expects the rotator transform
        /// to be instantiated. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonResetFrameCount_Click(object sender, EventArgs e)
        {
            LogMessage("buttonResetFrameCount_Click called");

            // get the transform
            IMFTransform transformObject = ctlTantaEVRFilePlayer1.GetTransform();
            if (transformObject == null)
            {
                LogMessage("buttonResetFrameCount: transformObject == null");
                OISMessageBox("No transform object. Is the video running?");
                return;
            }

            // get the real type of the transform. This assumes it is a .NET
            // based transform - otherwise it will probably just be a generic
            // _ComObject and the code below will fail.
            Type transformObjectType = transformObject.GetType();

            try
            {
                // set up to invoke the FrameCountAsPropertyDemonstrator. Note that
                // we have to know the name of the propery we are calling and the
                // type it takes. 
                object[] parameter = new object[1];
                parameter[0] = (int)1;
                transformObjectType.InvokeMember("FrameCountAsPropertyDemonstrator", BindingFlags.SetProperty, null, transformObject, parameter);
                LogMessage("buttonResetFrameCount: The frame count has been reset");
                OISMessageBox("The frame count has been reset");
            }
            catch (Exception ex)
            {
                OISMessageBox("An error occured please see the logfile");
                LogMessage(ex.Message);
                LogMessage(ex.StackTrace);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Demonstrates the client/transform communications. Displays the
        /// frame count in the rotator transform by calling the get accessor
        /// of a property
        /// 
        /// This function uses late binding and expects the rotator transform
        /// to be instantiated. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonGetFCViaProperty_Click(object sender, EventArgs e)
        {
            LogMessage("buttonGetFCViaProperty_Click called");

            // get the transform
            IMFTransform transformObject = ctlTantaEVRFilePlayer1.GetTransform();
            if (transformObject == null)
            {
                LogMessage("buttonGetFCViaProperty: transformObject == null");
                OISMessageBox("No transform object. Is the video running?");
                return;
            }

            // get the real type of the transform. This assumes it is a .NET
            // based transform - otherwise it will probably just be a generic
            // _ComObject and the code below will fail.
            Type transformObjectType = transformObject.GetType();

            // set up to invoke the FrameCountAsPropertyDemonstrator. Note that
            // we have to know the name of the propery we are calling and the
            // type it takes. 
            try
            {
                object frameCount = transformObjectType.InvokeMember("FrameCountAsPropertyDemonstrator", BindingFlags.GetProperty, null, transformObject, null);
                if ((frameCount is int) == true)
                {
                    LogMessage("The frame count is " + frameCount.ToString());
                    OISMessageBox("FrameCount=" + frameCount.ToString());
                }
            }
            catch (Exception ex)
            {
                OISMessageBox("An error occured please see the logfile");
                LogMessage(ex.Message);
                LogMessage(ex.StackTrace);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Demonstrates the client/transform communications. Displays the
        /// frame count in the rotator transform by calling the a function.
        /// The function requires two parameters a leading string and a ref string
        /// which is the output. A boolean is returned to indicate success. The
        /// frame count is appended to the user supplied leading string.
        /// 
        /// This function uses late binding and expects the rotator transform
        /// to be instantiated. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonGetFCViaFunction_Click(object sender, EventArgs e)
        {

            LogMessage("buttonGetFCViaFunction_Click called");

            // get the transform
            IMFTransform transformObject = ctlTantaEVRFilePlayer1.GetTransform();
            if (transformObject == null)
            {
                LogMessage("buttonGetFCViaFunction: transformObject == null");
                OISMessageBox("No transform object. Is the video running?");
                return;
            }

            // get the real type of the transform. This assumes it is a .NET
            // based transform - otherwise it will probably just be a generic
            // _ComObject and the code below will fail.
            Type transformObjectType = transformObject.GetType();

            // set up our parameters. both are strings, the second is ref string
            object[] parameters = new object[2];
            string outText = "Unknown FrameCount";
            parameters[0] = "I just checked, the frame count is ";
            parameters[1] = outText;

            // set up our parameter modifiers. This is how we tell the InvokeMember
            // call that one of our parameters is a ref
            ParameterModifier paramMods = new ParameterModifier(2);
            paramMods[1] = true;
            ParameterModifier[] paramModifierArray = { paramMods };

            try
            {
                // set up to invoke the FrameCountAsFunctionDemonstrator. Note that
                // we have to know the name of the function we are calling, the return
                // type and its parameter types
                object retVal = transformObjectType.InvokeMember("FrameCountAsFunctionDemonstrator", BindingFlags.InvokeMethod, null, transformObject, parameters, paramModifierArray, null, null);
                if ((retVal is bool) == false)
                {
                    LogMessage("buttonGetFCViaFunction_Click: call to FrameCountAsFunctionDemonstrator failed.");
                    OISMessageBox("call to FrameCountAsFunctionDemonstrator failed.");
                    return;
                }
            }
            catch(Exception ex)
            {
                OISMessageBox("An error occured please see the logfile");
                LogMessage(ex.Message);
                LogMessage(ex.StackTrace);
            }

            if (parameters[1] == null)
            {
                LogMessage("buttonGetFCViaFunction_Click: Null value returned for ref parameter.");
                OISMessageBox("Null value returned for ref parameter.");
                return;
            }
            if ((parameters[1] is string) == false)
            {
                LogMessage("buttonGetFCViaFunction_Click: Reference value is not a string");
                OISMessageBox("Reference value is not a string.");
                return;
            }

            LogMessage("buttonGetFCViaFunction_Click: " + (parameters[1] as string));
            OISMessageBox((parameters[1] as string));
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

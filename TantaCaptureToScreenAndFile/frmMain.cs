using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MediaFoundation;
using MediaFoundation.ReadWrite;
using MediaFoundation.Misc;
using MediaFoundation.Transform;
using MediaFoundation.Alt;
using MediaFoundation.EVR;
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

/// The function of this app is to allow the user to choose a video device and to both display the image data from that
/// video device on the screen and (optionally) write that data to a file for recording purposes. The app uses a 
/// standard pipeline topology in which the video device acts as the source and the EVR acts as the sink.
/// 
/// However, a Transform has been inserted into the pipeline. In normal operation all this transform does is 
/// present its input data to the output. If a sink writer has been configured on the Transform then the 
/// MFT will also give the sample data to the sink writer which writes it out to disk as an MP4 file - thus 
/// recording it. 
/// 
/// In effect, the Transform demonstrates how to present pipeline based data to other non-pipleline based
/// objects.

namespace TantaCaptureToScreenAndFile
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// The main form for the application
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public partial class frmMain : frmOISBase
    {
        private const string DEFAULTLOGDIR = @"C:\Dump\Project Logs";
        private const string APPLICATION_NAME = "TantaCaptureToScreenAndFile";
        private const string APPLICATION_VERSION = "01.00";

        private const string START_CAPTURE = "Start Capture";
        private const string STOP_CAPTURE = "Stop Capture";
        private const string RECORDING_IS_ON = "Recording is ON";
        private const string RECORDING_IS_OFF = "Recording is OFF";

        private const string DEFAULT_SOURCE_DEVICE = @"<No Video Device Selected>";

        private const string DEFAULT_OUTPUT_FILE = @"C:\Dump\TantaCaptureToScreenAndFile_Capture.mp4";

        // the call back handler for the mediaSession
        private TantaAsyncCallbackHandler mediaSessionAsyncCallbackHandler = null;

        // A session provides playback controls for the media content. The Media Session and the protected media path (PMP) session objects 
        // expose this interface. This interface is the primary interface that applications use to control the Media Foundation pipeline.
        // In this app we want the copy to proceed as fast as possible so we do not implement any of the usual session control items.
        protected IMFMediaSession mediaSession;

        // Media sources are objects that generate media data. For example, the data might come from a video file, a network stream, 
        // or a hardware device, such as a camera. Each media source contains one or more streams, and each stream delivers 
        // data of one type, such as audio or video.
        protected IMFMediaSource mediaSource;

        // The Enhanced Video Renderer(EVR) implements this interface and it controls how the EVR presenter displays video.
        // The EVR also a sink but we do not really use it as one - that functionality is largely internal to the pipeline.
        // we only get access to this object once the topology has been resolved. We still have to release it though!
        protected IMFVideoDisplayControl evrVideoDisplay;

        // we are using a custom transform to intercept the information as it moves through the
        // pipeline. If recording is enabled, it takes a copy of the media samples and then presents 
        // this data to a SinkWriter to be saved. This is an IMFTransform
        protected MFTTantaSampleGrabber_Sync sampleGrabberTransform = null;

        // this is the current type of the video stream. We need this to set up the sink writer
        // properly. This must be released at the end
        protected IMFMediaType currentVideoMediaType = null;

        // our thread safe screen update delegate
        public delegate void ThreadSafeScreenUpdate_Delegate(object obj, bool captureIsActive, string displayText);

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
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

                // a bit of setup
                buttonStartStopPlay.Text = START_CAPTURE;
                textBoxPickedVideoDeviceURL.Text = DEFAULT_SOURCE_DEVICE;
                buttonRecordingOnOff.Text = RECORDING_IS_OFF;
                SyncScreenControlsToCaptureState(false, null);
                textBoxOutputFileNameAndPath.Text = DEFAULT_OUTPUT_FILE;

                // we always have to initialize MF. The 0x00020070 here is the WMF version 
                // number used by the MF.Net samples. Not entirely sure if it is appropriate
                hr = MFExtern.MFStartup(0x00020070, MFStartup.Full);
                if (hr != 0)
                {
                    LogMessage("Constructor: call to MFExtern.MFStartup returned " + hr.ToString());
                }

                // set up our Video Picker Control
                ctlTantaVideoPicker1.VideoDevicePickedEvent += new ctlTantaVideoPicker.VideoDevicePickedEventHandler(VideoDevicePickedHandler);
                ctlTantaVideoPicker1.VideoFormatPickedEvent += new ctlTantaVideoPicker.VideoFormatPickedEventHandler(VideoFormatPickedHandler);

            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The form loaded handler
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void frmMain_Load(object sender, EventArgs e)
        {
            LogMessage("frmMain_Load");

            try
            {
                // enumerate all video devices and display their formats
                ctlTantaVideoPicker1.DisplayVideoCaptureDevices();

                ctlTantaEVRStreamDisplay1.InitMediaPlayer();
            }
            catch (Exception ex)
            {
                // something went wrong
                OISMessageBox("An error occurred\n\n" + ex.Message + "\n\nPlease see the logs");
                LogMessage("frmMain_Load " + ex.Message);
                LogMessage("frmMain_Load " + ex.StackTrace);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The form closing handler
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogMessage("frmMain_FormClosing");
            try
            {
                // do everything to close all media devices
                CloseAllMediaDevices();

                // Shut down MF
                MFExtern.MFShutdown();
            }
            catch
            {
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A centralized place to close down all media devices.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void CloseAllMediaDevices()
        {
            HResult hr;
            LogMessage("CloseAllMediaDevices");

            // if we are recording we had better stop now
            StopRecording();

            // close and release our call back handler
            if (mediaSessionAsyncCallbackHandler != null)
            {
                // stop any messaging or events in the call back handler
                mediaSessionAsyncCallbackHandler.ShutDown();
                mediaSessionAsyncCallbackHandler = null;
            }

            // close the session (this is NOT the same as shutting it down)
            if (mediaSession != null)
            {
                hr = mediaSession.Close();
                if (hr != HResult.S_OK)
                {
                    // just log it
                    LogMessage("CloseAllMediaDevices call to mediaSession.Close failed. Err=" + hr.ToString());
                }
            }

            // Shut down the media source
            if (mediaSource != null)
            {
                hr = mediaSource.Shutdown();
                if (hr != HResult.S_OK)
                {
                    // just log it
                    LogMessage("CloseAllMediaDevices call to mediaSource.Shutdown failed. Err=" + hr.ToString());
                }
                Marshal.ReleaseComObject(mediaSource);
                mediaSource = null;
            }

            // Shut down the media session (note we only closed it before).
            if (mediaSession != null)
            {
                hr = mediaSession.Shutdown();
                if (hr != HResult.S_OK)
                {
                    // just log it
                    LogMessage("CloseAllMediaDevices call to mediaSession.Shutdown failed. Err=" + hr.ToString());
                }
                Marshal.ReleaseComObject(mediaSession);
                mediaSession = null;
            }

            // close down the display
            ctlTantaEVRStreamDisplay1.ShutDownFilePlayer();

            // close the evrvideodisplay
            if (evrVideoDisplay != null)
            {
                Marshal.ReleaseComObject(evrVideoDisplay);
                evrVideoDisplay = null;
            }

            if (currentVideoMediaType != null)
            {
                Marshal.ReleaseComObject(currentVideoMediaType);
                currentVideoMediaType = null;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the source filename. Will never return null, will return ""
        /// There is no set accessor, This is obtained off the screen.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string VideoCaptureDeviceName
        {
            get
            {
                if (textBoxPickedVideoDeviceURL.Text == null) textBoxPickedVideoDeviceURL.Text = "";
                return textBoxPickedVideoDeviceURL.Text;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the video device
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public TantaMFVideoFormatContainer VideoFormatContainer
        {
            get
            {
                if ((textBoxPickedVideoDeviceURL.Tag is TantaMFVideoFormatContainer) == false)
                {
                    textBoxPickedVideoDeviceURL.Tag = null;
                    return null;
                }
                return (textBoxPickedVideoDeviceURL.Tag as TantaMFVideoFormatContainer);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the output filename. Will never return null, will return ""
        /// There is no set accessor, This is obtained off the screen.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string OutputFileName
        {
            get
            {
                if (textBoxOutputFileNameAndPath.Text == null) textBoxOutputFileNameAndPath.Text = "";
                return textBoxOutputFileNameAndPath.Text;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Starts/Stops the capture
        /// 
        /// Because this code is intended for demo purposes, and in the interests of
        /// reducing complexity, it is extremely linear, step-by-step and kicked off
        /// directly from a button press in the main form. Doubtless there is much 
        /// refactoring that could be done.
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void buttonStartStopPlay_Click(object sender, EventArgs e)
        {
            // this code toggles both the start and stop capture. Since the
            // STOP code is much simpler we test for it first. We use the 
            // text on the button to detect if we are capturing or not. 
            if (buttonStartStopPlay.Text == STOP_CAPTURE)
            {
                // do everything to close all media devices
                // the MF itself is still active.
                CloseAllMediaDevices();

                // re-enable our screen controls
                SyncScreenControlsToCaptureState(false, null);
                return;
            }

            // ####
            // #### below here we assume we are starting the capture
            // ####

            try
            {

                // check our source filename is correct and usable
                if ((VideoCaptureDeviceName == null) || (VideoCaptureDeviceName.Length == 0))
                {
                    OISMessageBox("No Source Filename and path. Cannot continue.");
                    return;
                }
                if(VideoFormatContainer == null)
                {
                    OISMessageBox("The video device and format is unknown.\n\nHave you selected a video device and format?");
                    return;
                }

                // check our output filename is correct and usable
                if ((OutputFileName == null) || (OutputFileName.Length == 0))
                {
                    OISMessageBox("No Output Filename and path. Cannot continue.");
                    return;
                } 
                if (Path.IsPathRooted(OutputFileName) == false)
                {
                    OISMessageBox("No Output Filename and path is not rooted. A full directory and path is required. Cannot continue.");
                    return;
                }

                // check the directory of the path exists
                string dirName = Path.GetDirectoryName(OutputFileName);
                if (Directory.Exists(dirName) == false)
                {
                    OISMessageBox("The output directory does not exist. A full directory and path is required. Cannot continue.");
                    return;
                }
 
                // set up a session, topology and open the media source and sink etc
                PrepareSessionAndTopology(VideoFormatContainer);

                // disable our screen controls
                SyncScreenControlsToCaptureState(true, null);

            }
            finally
            {

            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sync the state on the screen controls to the current capture state
        /// </summary>
        /// <param name="captureIsActive">if true we set the controls to enabled</param>
        /// <param name="displayText">Text to display in a message box. If null we ignore</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void SyncScreenControlsToCaptureState(bool captureIsActive, string displayText)
        {

            if (captureIsActive == false)
            {
                textBoxOutputFileNameAndPath.Enabled = true;
                labelVideoCaptureDeviceName.Enabled = true;
                textBoxOutputFileNameAndPath.Enabled = true;
                labelOutputFileName.Enabled = true;
                ctlTantaVideoPicker1.Enabled = true;
                buttonStartStopPlay.Text = START_CAPTURE;
                buttonRecordingOnOff.Enabled = false;
                buttonRecordingOnOff.Text = RECORDING_IS_OFF;
                checkBoxTimeBaseRebase.Enabled = false;
            }
            else
            {
                // set this
                textBoxPickedVideoDeviceURL.Enabled = false;
                labelVideoCaptureDeviceName.Enabled = false;
                textBoxOutputFileNameAndPath.Enabled = false;
                labelOutputFileName.Enabled = false;
                ctlTantaVideoPicker1.Enabled = false;
                buttonStartStopPlay.Text = STOP_CAPTURE;
                buttonRecordingOnOff.Enabled = true;
                buttonRecordingOnOff.Text = RECORDING_IS_OFF;
                checkBoxTimeBaseRebase.Enabled = true;
            }

            if ((displayText != null) && (displayText.Length != 0))
            {
                OISMessageBox(displayText);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the enabled state on the screen controls in a thread safe way
        /// </summary>
        /// <param name="captureIsActive">if true we set the controls to enabled</param>
        /// <param name="displayText">Text to display in a message box. If null we ignore</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public void ThreadSafeScreenUpdate(object caller, bool captureIsActive, string displayText)
        {

            // log it - the logger is thread safe!
            LogMessage("ThreadSafeScreenUpdate, wantEnable=" + captureIsActive.ToString());

            // Ok, you probably already know this but I'll note it here because this is so important
            // You do NOT want to update any form controls from a thread that is not the forms main
            // thread. Very odd, intermittent and hard to debug problems will result. Even if your 
            // handler does not actually update any form controls do not do it! Sooner or later you 
            // or someone else will make changes that calls something that eventually updates a
            // form or control and then you will have introduced a really hard to find bug.

            // So, we always use the InvokeRequired...Invoke sequence to get us back on the form thread
            if (InvokeRequired == true)
            {
                // call ourselves again but this time be on the form thread.
                Invoke(new ThreadSafeScreenUpdate_Delegate(ThreadSafeScreenUpdate), new object[] { caller, captureIsActive, displayText });
                return;
            }

            // if we get here we are assured we are on the form thread.
            SyncScreenControlsToCaptureState(captureIsActive, displayText);

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens and prepares the media session and topology and opens the media source
        /// and media sink.
        /// 
        /// Once the session and topology are setup, a MESessionTopologySet event
        /// will be triggered in the callback handler. After that the events there
        /// trigger other events and everything rolls along automatically.
        /// </summary>
        /// <param name="videoCaptureDevice">the video capture device name</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void PrepareSessionAndTopology(TantaMFVideoFormatContainer videoFormatContainer)
        {
            HResult hr;
            IMFSourceResolver pSourceResolver = null;
            IMFTopology topologyObj = null;
            IMFPresentationDescriptor sourcePresentationDescriptor = null;
            int sourceStreamCount = 0;
            bool streamIsSelected = false;
            IMFStreamDescriptor videoStreamDescriptor = null;
            IMFTopologyNode sourceVideoNode = null;
            IMFTopologyNode outputSinkNodeVideo = null;
            IMFTopologyNode transformNode = null;

            LogMessage("PrepareSessionAndTopology ");

            // we sanity check the video source device 
            if (videoFormatContainer == null)
            {
                throw new Exception("PrepareSessionAndTopology: videoFormatContainer is invalid. Cannot continue.");
            }
            if (videoFormatContainer.VideoDevice == null)
            {
                throw new Exception("PrepareSessionAndTopology: VideoDevice is invalid. Cannot continue.");
            }
            if ((videoFormatContainer.VideoDevice.SymbolicName == null) || (videoFormatContainer.VideoDevice.SymbolicName.Length == 0))
            {
                throw new Exception("PrepareSessionAndTopology: VideoDevice.SymbolicName is invalid. Cannot continue.");
            }

            try
            {
                // reset everything
                CloseAllMediaDevices();

                // Create the media session.
                hr = MFExtern.MFCreateMediaSession(null, out mediaSession);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFExtern.MFCreateMediaSession failed. Err=" + hr.ToString());
                }
                if (mediaSession == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFExtern.MFCreateMediaSession failed. mediaSession == null");
                }

                // set up our media session call back handler.
                mediaSessionAsyncCallbackHandler = new TantaAsyncCallbackHandler();
                mediaSessionAsyncCallbackHandler.Initialize();
                mediaSessionAsyncCallbackHandler.MediaSession = mediaSession;
                mediaSessionAsyncCallbackHandler.MediaSessionAsyncCallBackError = HandleMediaSessionAsyncCallBackErrors;
                mediaSessionAsyncCallbackHandler.MediaSessionAsyncCallBackEvent = HandleMediaSessionAsyncCallBackEvent;

                // Register the callback handler with the session and tell it that events can
                // start. This does not actually trigger an event it just lets the media session 
                // know that it can now send them if it wishes to do so.
                hr = mediaSession.BeginGetEvent(mediaSessionAsyncCallbackHandler, null);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to mediaSession.BeginGetEvent failed. Err=" + hr.ToString());
                }

                // Create a new topology.  A topology describes a collection of media sources, sinks, and transforms that are 
                // connected in a certain order. These objects are represented within the topology by topology nodes, 
                // which expose the IMFTopologyNode interface. A topology describes the path of multimedia data through these nodes.
                hr = MFExtern.MFCreateTopology(out topologyObj);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFExtern.MFCreateTopology failed. Err=" + hr.ToString());
                }
                if (topologyObj == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFExtern.MFCreateTopology failed. topologyObj == null");
                }

                // ####
                // #### we now create the media source, this is video device (camera)
                // ####

                // use the device symbolic name to create the media source for the video device. Media sources are objects that generate media data. 
                // For example, the data might come from a video file, a network stream, or a hardware device, such as a camera. Each 
                // media source contains one or more streams, and each stream delivers data of one type, such as audio or video.                
                mediaSource = TantaWMFUtils.GetMediaSourceFromTantaDevice(videoFormatContainer.VideoDevice);
                if (mediaSource == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to mediaSource == null");
                }

                // A presentation is a set of related media streams that share a common presentation time.  We now get a copy of the media 
                // source's presentation descriptor. Applications can use the presentation descriptor to select streams 
                // and to get information about the source content.
                hr = mediaSource.CreatePresentationDescriptor(out sourcePresentationDescriptor);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to mediaSource.CreatePresentationDescriptor failed. Err=" + hr.ToString());
                }
                if (sourcePresentationDescriptor == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to mediaSource.CreatePresentationDescriptor failed. sourcePresentationDescriptor == null");
                }

                // Now we get the number of stream descriptors in the presentation. A presentation descriptor contains a list of one or more 
                // stream descriptors. These describe the streams in the presentation. Streams can be either selected or deselected. Only the 
                // selected streams produce data. Deselected streams are not active and do not produce any data. 
                hr = sourcePresentationDescriptor.GetStreamDescriptorCount(out sourceStreamCount);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorCount failed. Err=" + hr.ToString());
                }
                if (sourceStreamCount == 0)
                {
                    throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorCount failed. sourceStreamCount == 0");
                }

                // Look at each stream, there can be more than one stream here
                // Usually only one is enabled. This app uses the first "selected"  
                // stream we come to which has the appropriate media type

                // look for the video stream
                for (int i = 0; i < sourceStreamCount; i++)
                {
                    // we require the major type to be video
                    Guid guidMajorType = TantaWMFUtils.GetMajorMediaTypeFromPresentationDescriptor(sourcePresentationDescriptor, i);
                    if (guidMajorType != MFMediaType.Video) continue;

                    // we also require the stream to be enabled
                    hr = sourcePresentationDescriptor.GetStreamDescriptorByIndex(i, out streamIsSelected, out videoStreamDescriptor);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(v) failed. Err=" + hr.ToString());
                    }
                    if (videoStreamDescriptor == null)
                    {
                        throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(v) failed. videoStreamDescriptor == null");
                    }
                    // if the stream is selected, leave now we will release the videoStream descriptor later
                    if (streamIsSelected == true) break;

                    // release the one we are not using
                    if (videoStreamDescriptor != null)
                    {
                        Marshal.ReleaseComObject(videoStreamDescriptor);
                        videoStreamDescriptor = null;
                    }
                }

                // by the time we get here we should have a video StreamDescriptor if
                // we do not, then we cannot proceed. 
                if (videoStreamDescriptor == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. videoStreamDescriptor == null");
                }

                // sets the current media type on a stream descriptor by matching
                // its mediaTypes to the video format container contents. We know we will
                // get a match because our video format picker enumerated all the formats
                // for us and thus we chose one we already know exists.
                hr = TantaWMFUtils.SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer(videoStreamDescriptor, videoFormatContainer);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer failed. Err=" + hr.ToString());
                }

                // ####
                // #### when we create the sink writer to record the video data we will need the types from the stream to do 
                // #### this which is why we get this now.
                // ####

                currentVideoMediaType = TantaWMFUtils.GetCurrentMediaTypeFromStreamDescriptor(videoStreamDescriptor);
                if (currentVideoMediaType == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to currentVideoMediaType == null");
                }

                // ####
                // #### Create the custom sample grabber transform which will send a copy of the data
                // #### to the SinkWriter for recording purposes
                // ####
                sampleGrabberTransform = new MFTTantaSampleGrabber_Sync();

                // ####
                // #### we now make up a topology branch for the video stream
                // ####

                // Create a source Video node for this stream.
                sourceVideoNode = TantaWMFUtils.CreateSourceNodeForStream(mediaSource, sourcePresentationDescriptor, videoStreamDescriptor);
                if (sourceVideoNode == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to CreateSourceNodeForStream(v) failed. sourceAudioNode == null");
                }

                // Create the Video sink node. 
                outputSinkNodeVideo = TantaWMFUtils.CreateEVRRendererOutputNodeForStream(this.ctlTantaEVRStreamDisplay1.DisplayPanelHandle);
                if (outputSinkNodeVideo == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFCreateTopologyNode(v) failed.  outputSinkNodeVideo == null");
                }

                // Create the transform node.
                hr = MFExtern.MFCreateTopologyNode(MFTopologyType.TransformNode, out transformNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFExtern.MFCreateTopologyNode failed. Err=" + hr.ToString());
                }

                // set the transform object (it is an IMFTransform) as an object on the transform node. Since it is already
                // instantiated the topology does not need a GUID or activator to create it
                hr = transformNode.SetObject(sampleGrabberTransform);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to pTransformNode.SetObject failed. Err=" + hr.ToString());
                }

                // Add the nodes to the topology. First the source node
                hr = topologyObj.AddNode(sourceVideoNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to topologyObj.AddNode(sourceAudioNode) failed. Err=" + hr.ToString());
                }

                // add the output Node
                hr = topologyObj.AddNode(outputSinkNodeVideo);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to topologyObj.AddNode(outputSinkNodeVideo) failed. Err=" + hr.ToString());
                }

                // add the transform Node
                hr = topologyObj.AddNode(transformNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to topologyObj.AddNode(transformNode) failed. Err=" + hr.ToString());
                }

                // Connect the output stream from the source node to the input of the transform and the output from the transform to the 
                // input stream of the output node. The parameters are:
                //    dwOutputIndex  -  Zero-based index of the output stream on this node.
                //    *pDownstreamNode  -  Pointer to the IMFTopologyNode interface of the node to connect to.
                //    dwInputIndexOnDownstreamNode  -  Zero-based index of the input stream on the other node.

                // Note even though the streamID from the source may be non zero it the output index of this node
                // is still 0 since that is the only stream we have configured on it. 
                hr = sourceVideoNode.ConnectOutput(0, transformNode, 0);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to  sourceVideoNode.ConnectOutput failed. Err=" + hr.ToString());
                }
                hr = transformNode.ConnectOutput(0, outputSinkNodeVideo, 0);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to  transformNode.ConnectOutput failed. Err=" + hr.ToString());
                }

                // Set the topology on the media session.
                // If SetTopology succeeds, the media session will queue an
                // MESessionTopologySet event. We can use that to discover the
                // EVR display object
                hr = mediaSession.SetTopology(0, topologyObj);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("PrepareSessionAndTopology mediaSession.SetTopology failed, retVal=" + hr.ToString());
                }

                // Release the topology
                if (topologyObj != null)
                {
                    Marshal.ReleaseComObject(topologyObj);
                }

            }
            catch (Exception ex)
            {
                LogMessage("PrepareSessionAndTopology Error: " + ex.Message);
                OISMessageBox(ex.Message);
            }
            finally
            {
                // Clean up
                if (pSourceResolver != null)
                {
                    Marshal.ReleaseComObject(pSourceResolver);
                }
                if (sourcePresentationDescriptor != null)
                {
                    Marshal.ReleaseComObject(sourcePresentationDescriptor);
                }
                if (videoStreamDescriptor != null)
                {
                    Marshal.ReleaseComObject(videoStreamDescriptor);
                }
                if (sourceVideoNode != null)
                {
                    Marshal.ReleaseComObject(sourceVideoNode);
                }
                if (outputSinkNodeVideo != null)
                {
                    Marshal.ReleaseComObject(outputSinkNodeVideo);
                }
                if (transformNode != null)
                {
                    Marshal.ReleaseComObject(transformNode);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles events reported by the media session TantaAsyncCallbackHandler 
        /// </summary>
        /// <param name="sender">the object sending the event</param>
        /// <param name="mediaEvent">the event generated by the media session. Do NOT release this here.</param>
        /// <param name="mediaEventType">the eventType, this is just an enum</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void HandleMediaSessionAsyncCallBackEvent(object sender, IMFMediaEvent pEvent, MediaEventType mediaEventType)
        {
            LogMessage("Media Event Type " + mediaEventType.ToString());

            switch (mediaEventType)
            {
                case MediaEventType.MESessionTopologyStatus:
                    // Raised by the Media Session when the status of a topology changes. 
                    // Get the topology changed status code. This is an enum in the event
                    int i;
                    HResult hr = pEvent.GetUINT32(MFAttributesClsid.MF_EVENT_TOPOLOGY_STATUS, out i);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("HandleMediaSessionAsyncCallBackEvent call to pEvent to get the status code failed. Err=" + hr.ToString());
                    }
                    // the one we are most interested in is i == MFTopoStatus.Ready
                    // which we get then the Topology is built and ready to run
                    HandleTopologyStatusChanged(pEvent, mediaEventType, (MFTopoStatus)i);
                    break;

                case MediaEventType.MESessionStarted:
                    // Raised when the IMFMediaSession::Start method completes asynchronously. 
                    //       PlayerState = TantaEVRPlayerStateEnum.Started;
                    break;

                case MediaEventType.MESessionPaused:
                    // Raised when the IMFMediaSession::Pause method completes asynchronously. 
                    //        PlayerState = TantaEVRPlayerStateEnum.Paused;
                    break;

                case MediaEventType.MESessionStopped:
                    // Raised when the IMFMediaSession::Stop method completes asynchronously.
                    break;

                case MediaEventType.MESessionClosed:
                    // Raised when the IMFMediaSession::Close method completes asynchronously. 
                    break;

                case MediaEventType.MESessionCapabilitiesChanged:
                    // Raised by the Media Session when the session capabilities change.
                    // You can use IMFMediaEvent::GetValue to figure out what they are
                    break;

                case MediaEventType.MESessionTopologySet:
                    // Raised after the IMFMediaSession::SetTopology method completes asynchronously. 
                    // The Media Session raises this event after it resolves the topology into a full topology and queues the topology for playback. 
                    break;

                case MediaEventType.MESessionNotifyPresentationTime:
                    // Raised by the Media Session when a new presentation starts. 
                    // This event indicates when the presentation will start and the offset between the presentation time and the source time.      
                    break;

                case MediaEventType.MEEndOfPresentation:
                    // Raised by a media source when a presentation ends. This event signals that all streams 
                    // in the presentation are complete. The Media Session forwards this event to the application.

                    // we cannot sucessfully .Finalize_ on the SinkWriter
                    // if we call CloseAllMediaDevices directly from this thread
                    // so we use an asynchronous method
                    Task taskA = Task.Run(() => CloseAllMediaDevices());
                    // we have to be on the form thread to update the screen
                    ThreadSafeScreenUpdate(this, false, null);
                    break;

                case MediaEventType.MESessionRateChanged:
                    // Raised by the Media Session when the playback rate changes. This event is sent after the 
                    // IMFRateControl::SetRate method completes asynchronously. 
                    break;

                default:
                    LogMessage("Unhandled Media Event Type " + mediaEventType.ToString());
                    break;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles topology status changes reported by the media session TantaAsyncCallbackHandler 
        /// </summary>
        /// <param name="sender">the object sending the event</param>
        /// <param name="mediaEvent">the event generated by the media session. Do NOT release this here.</param>
        /// <param name="mediaEventType">the eventType</param>
        /// <param name="topoStatus">the topology status flag</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void HandleTopologyStatusChanged(IMFMediaEvent mediaEvent, MediaEventType mediaEventType, MFTopoStatus topoStatus)
        {
            LogMessage("HandleTopologyStatusChanged event type: " + mediaEventType.ToString() + ", topoStatus=" + topoStatus.ToString());

            if (topoStatus == MFTopoStatus.Ready)
            {
                MediaSessionTopologyNowReady(mediaEvent);
            }
            else
            {
                // we are not interested in any other status changes
                return;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the topology status changes to ready. This status change
        /// is generally signaled by the media session when it is fully configured.
        /// </summary>
        /// <param name="sender">the object sending the event</param>
        /// <param name="mediaEvent">the event generated by the media session. Do NOT release this here.</param>
        /// <param name="mediaEventType">the eventType</param>
        /// <param name="topoStatus">the topology status flag</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void MediaSessionTopologyNowReady(IMFMediaEvent mediaEvent)
        {
            HResult hr;
            object evrVideoService;

            LogMessage("MediaSessionTopologyNowReady");

            // we need to obtain a reference to the EVR Video Display Control.
            // We used an Activator to configure this in the Topology and so
            // there is no reference to it at this point. However the media session
            // knows about it and so we can get it from that.

            // Ask for the IMFVideoDisplayControl interface. This interface is implemented by the EVR and is
            // exposed by the media session as a service.

            // Some interfaces in Media Foundation must be obtained by calling IMFGetService::GetService instead 
            // of by calling QueryInterface. The GetService method works like QueryInterface, but 
            // the big difference is that if an object is returning itself as a different interface 
            // you can use QueryInterface. If, as in this case where the media session is NOT the
            // evrVideoDisplay object, an object is returning another object you obtain that object
            // as a service.            

            // Note: This call is expected to fail if the source does not have video.

            try
            {
                // we need to get the active IMFVideoDisplayControl. The EVR presenter implements this interface
                // and it controls how the Enhanced Video Renderer (EVR) displays video.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MR_VIDEO_RENDER_SERVICE,
                    typeof(IMFVideoDisplayControl).GUID,
                    out evrVideoService
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("MediaSessionTopologyNowReady call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (evrVideoService == null)
                {
                    throw new Exception("MediaSessionTopologyNowReady call to MFExtern.MFGetService failed. evrVideoService == null");
                }

                // set the video display now for later use
                evrVideoDisplay = evrVideoService as IMFVideoDisplayControl;
                // also give this to the display control
                ctlTantaEVRStreamDisplay1.EVRVideoDisplay = evrVideoDisplay;
            }
            catch (Exception ex)
            {
                evrVideoDisplay = null;
                ctlTantaEVRStreamDisplay1.EVRVideoDisplay = evrVideoDisplay;
                LogMessage("Error: " + ex.Message);
            }

            try
            {
                StartFileCapture();
            }
            catch (Exception ex)
            {
                LogMessage("MediaSessionTopologyNowReady errored ex=" + ex.Message);
                OISMessageBox(ex.Message);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Starts the capture of the media data and gets it moving through
        /// the pipeline from source to sink.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void StartFileCapture()
        {
            LogMessage("StartFileCapture called");

            if (mediaSession == null)
            {
                LogMessage("StartFileCapture Failed.  mediaSession == null");
                return;
            }

            if (evrVideoDisplay != null)
            {
                // the aspect ratio can be changed by uncommenting either of these lines
                // evrVideoDisplay.SetAspectRatioMode(MFVideoAspectRatioMode.None);
                // evrVideoDisplay.SetAspectRatioMode(MFVideoAspectRatioMode.PreservePicture);
            }

            // this is what starts the data moving through the pipeline
            HResult hr = mediaSession.Start(Guid.Empty, new PropVariant());
            if (hr != HResult.S_OK)
            {
                throw new Exception("StartFileCapture call to mediaSession.Start failed. Err=" + hr.ToString());
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles errors reported by the media session TantaAsyncCallbackHandler 
        /// </summary>
        /// <param name="sender">the object sending the event</param>
        /// <param name="errMsg">the error message</param>
        /// <param name="ex">the exception. Can be null</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void HandleMediaSessionAsyncCallBackErrors(object sender, string errMsg, Exception ex)
        {
            if (errMsg == null) errMsg = "unknown error";
            LogMessage("HandleMediaSessionAsyncCallBackErrors Error" + errMsg);
            if (ex != null)
            {
                LogMessage("HandleMediaSessionAsyncCallBackErrors Exception trace = " + ex.StackTrace);
            }
            OISMessageBox("The media session reported an error\n\nPlease see the logfile.");
            // do everything to close all media devices
            CloseAllMediaDevices();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle changes on the input filename so we can set our output filename.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void textBoxPickedVideoDeviceURL_TextChanged(object sender, EventArgs e)
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Handle a picked video device 
        /// </summary>
        /// <param name="videoDevice">the video device</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void VideoDevicePickedHandler(object sender, TantaMFDevice videoDevice)
        {
           // we do nothing here. The user has to also pick a format from that device
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Handle a picked video device and format
        /// </summary>
        /// <param name="videoFormatCont">the video format container. Also contains the device</param>
        private void VideoFormatPickedHandler(object sender, TantaMFVideoFormatContainer videoFormatCont)
        {
            string mfDeviceName = "<unknown device>";
            string formatSummary = "<unknown format>";

            // set these now
            if (videoFormatCont != null)
            {
                formatSummary = videoFormatCont.DisplayString() + ", " + videoFormatCont.FrameRateAsString + " fps";
                if (videoFormatCont.VideoDevice != null) mfDeviceName = videoFormatCont.VideoDevice.FriendlyName;
                // set the button text appropriately
                textBoxPickedVideoDeviceURL.Text = mfDeviceName + " " + formatSummary;
                // save the container here - this is the last one that came in
                textBoxPickedVideoDeviceURL.Tag = videoFormatCont;
            }
            else
            {
                // set the button text appropriately
                textBoxPickedVideoDeviceURL.Text = "Use: " + mfDeviceName + " " + formatSummary;
                // save the container here - this is the last one that came in
                textBoxPickedVideoDeviceURL.Tag = null;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Detects if we are currently recording
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private bool IsRecording
        {
            get
            {
                if (sampleGrabberTransform == null) return false;
                return sampleGrabberTransform.IsRecording;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Toggles the recording on and off
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void buttonRecordingOnOff_Click(object sender, EventArgs e)
        {
            int retInt;

            if (sampleGrabberTransform == null)
            {
                // no transform, recording is always off
                buttonRecordingOnOff.Text = RECORDING_IS_OFF;
                return;
            }

            if (IsRecording == false)
            {
                // recording is currently off, turn it on
                buttonRecordingOnOff.Text = RECORDING_IS_ON;
                // start recording
                retInt = StartRecording();
                if (retInt != 0)
                {
                    // we errored
                    StopRecording();
                    buttonRecordingOnOff.Text = RECORDING_IS_OFF;
                    OISMessageBox("Error " + retInt.ToString() + " occurred. Cannot continue. Please see the logs");
                    return;
                }
            }
            else
            {
                // recording is currently on, turn it off
                buttonRecordingOnOff.Text = RECORDING_IS_OFF;
                // just do this
                StopRecording();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Starts the recording process. Does not update the screen to say it is
        ///  doing this.
        /// </summary>
        /// <returns>z success, nz fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private int StartRecording()
        {
            LogMessage("StartRecording called");

            // we have to have this.
            if (sampleGrabberTransform == null) return 100;

            // check our output filename is correct and usable
            if ((OutputFileName == null) || (OutputFileName.Length == 0))
            {
                OISMessageBox("No Output Filename and path. Cannot continue.");
                return 200;
            }
            // check the path is rooted
            if (Path.IsPathRooted(OutputFileName) == false)
            {
                OISMessageBox("No Output Filename and path is not rooted. A full directory and path is required. Cannot continue.");
                return 300;
            }

            if(currentVideoMediaType == null)
            {
                OISMessageBox("No current video type. Something went wrong. Cannot continue.");
                return 400;
            }

            checkBoxTimeBaseRebase.Enabled = false;

            // ask the sampleGrabberTransform to start recording
            sampleGrabberTransform.StartRecording(OutputFileName, currentVideoMediaType, checkBoxTimeBaseRebase.Checked);
            return 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Starts the recording process. Does not update the screen to say it is
        ///  doing this.
        /// </summary>
        /// <returns>z success, nz fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void StopRecording()
        {
            LogMessage("StopRecording called");

            // we have to have this.
            if (sampleGrabberTransform == null) return;

            // ask the sampleGrabberTransform to start recording
            sampleGrabberTransform.StopRecording();

            if (buttonStartStopPlay.Text == STOP_CAPTURE)
            {
                checkBoxTimeBaseRebase.Enabled = true;
            }
            else
            {
                checkBoxTimeBaseRebase.Enabled = false;
            }
        }
    }
}

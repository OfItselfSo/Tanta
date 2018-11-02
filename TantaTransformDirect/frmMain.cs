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

/// The function of this app is to directly compile Transforms and add one of them to the video branch of the topology. This
/// application does not place its transforms in the registry or use transforms that are there. It is entirely devoted
/// to demonstrating the basics of transforms without the overhead of using COM to dig it out of the system. 
/// 
/// This code picks up a file off a disk and displays the video. The ctlTantaEVRStreamDisplay is used to display the video.  
/// The TantaFilePlayback sample contains a much more fully implemented version of this process in the TantaFilePlayer 
/// sample. You should probably familiarize yourself with that code before working with this one. 
/// 
/// The user can choose a specific transform from a list of radio buttons. The video on screen will display the effects
/// of the transform. The transform must be chosen before the video is played. 
/// 
/// Note because the transform code is compiled directly into the application, you can set breakpoints and step through
/// the parts that interest you. Also, because everything inherits from the OISCommon classes, you can also put 
/// LogMessage, DebugMessage, and ReleaseMessage calls in various places to emit information to the log.
/// 
/// The transforms are:
/// 
///   FrameCounter, MFTTantaFrameCounter_Sync:  A synchronous transform to count the video frames as they pass through it. 
///                 This is a very basic transform and it does not do anything with the count. It simply serves
///                 to demonstrate a very basic transform. Processes in-place.
///                 
///   Grayscale,    MFTTantaGrayscale_Sync   A synchronous transform to change the video data to grayscale. This transform
///                 demonstrates copying the input data to the output data and a method for supporting multiple video 
///                 formats.
///                 
///   Grayscale,    MFTTantaGrayscale_Async  An Asnchronous transform to change the video data to grayscale. This transform
///                 is identical to MFTTantaGrayscale_Sync except that it is asynchronous and uses in-place processing.
///                 
///   WriteText,    MFTTantaWriteText_Sync   A synchronous transform to overwrite text on the video display. This transform
///                 uses in-place processing and supports only one input type. It demonstrates how to use both overlay and
///                 transparent fonts. 
///                  
namespace TantaTransformDirect
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// The main form for the application. 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public partial class frmMain : frmOISBase
    {
        private const string DEFAULTLOGDIR = @"C:\Dump\Project Logs";
        private const string APPLICATION_NAME = "TantaTransformDirect";
        private const string APPLICATION_VERSION = "01.00";

        private const string START_PLAY = "Start Play";
        private const string STOP_PLAY = "Stop Play";

        private const string DEFAULT_SOURCE_FILE = @"C:\Dump\SampleVideo_1280x720_5mb.mp4";
        private const string DEFAULT_COPY_SUFFIX = "_TantaCopy";

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
        // this is also a sink but we do not formally use it as one - that functionality is largely internal to the pipeline.
        // we only get access to this object once the topology has been resolved. We still have to release it though!
        protected IMFVideoDisplayControl evrVideoDisplay;

        // our thread safe screen update delegate
        public delegate void ThreadSafeScreenUpdate_Delegate(object obj, bool playingIsActive, string displayText);

        // if we are using a transform (as a binary) this will be non-null
        protected IMFTransform videoTransform = null;

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
                buttonStartStopPlay.Text = START_PLAY;
                textBoxSourceFileNameAndPath.Text = DEFAULT_SOURCE_FILE;

                // we always have to initialize MF. The 0x00020070 here is the WMF version 
                // number used by the MF.Net samples. Not entirely sure if it is appropriate
                hr = MFExtern.MFStartup(0x00020070, MFStartup.Full);
                if (hr != 0)
                {
                    LogMessage("Constructor: call to MFExtern.MFStartup returned " + hr.ToString());
                }
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

            ctlTantaEVRStreamDisplay1.InitMediaPlayer();
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

            // remove this
            VideoTransform = null;

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
         }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the source filename. Will never return null, will return ""
        /// There is no set accessor, This is obtained off the screen.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string SourceFileName
        {
            get
            {
                if (textBoxSourceFileNameAndPath.Text == null) textBoxSourceFileNameAndPath.Text = "";
                return textBoxSourceFileNameAndPath.Text;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Starts/Stops the play
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
            // this code toggles both the start and stop the play. Since the
            // STOP code is much simpler we test for it first. We use the 
            // text on the button to detect if we are capturing or not. 
            if (buttonStartStopPlay.Text == STOP_PLAY)
            {
                // do everything to close all media devices
                // the MF itself is still active.
                CloseAllMediaDevices();

                // re-enable our screen controls
                SyncScreenControlsToPlayState(false, null);
                return;
            }

            // ####
            // #### below here we assume we are starting the capture
            // ####

            try
            {

                // check our source filename is correct and usable
                if ((SourceFileName == null) || (SourceFileName.Length == 0))
                {
                    MessageBox.Show("No Source Filename and path. Cannot continue.");
                    return;
                }
                 // check the path is rooted
                if (Path.IsPathRooted(SourceFileName) == false)
                {
                    MessageBox.Show("No Source Filename and path is not rooted. A full directory and path is required. Cannot continue.");
                    return;
                }
 
                // set up a session, topology and open the media source and sink etc
                PrepareSessionAndTopology(SourceFileName);

                // disable our screen controls
                SyncScreenControlsToPlayState(true, null);

            }
            finally
            {

            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sync the state on the screen controls to the current copying state
        /// </summary>
        /// <param name="playingIsActive">if true we set the controls to enabled</param>
        /// <param name="displayText">Text to display in a message box. If null we ignore</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void SyncScreenControlsToPlayState(bool playingIsActive, string displayText)
        {

            if(playingIsActive==false)
            {
                textBoxSourceFileNameAndPath.Enabled = true;
                labelSourceFileName.Enabled = true;
                buttonStartStopPlay.Text = START_PLAY;
                groupBoxTransforms.Enabled = true;
                buttonPickFile.Enabled = true;
                labelTransformContactDemo.Enabled = false;
                buttonGetFrameCount.Enabled = false;
            }
            else
            {
                // set this
                textBoxSourceFileNameAndPath.Enabled = false;
                labelSourceFileName.Enabled = false;
                buttonStartStopPlay.Text = STOP_PLAY;
                groupBoxTransforms.Enabled = false;
                buttonPickFile.Enabled = false;
                labelTransformContactDemo.Enabled = true;
                buttonGetFrameCount.Enabled = true;
            }

            if ((displayText!=null) && (displayText.Length!=0))
            {
                OISMessageBox(displayText);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the enabled state on the screen controls in a thread safe way
        /// </summary>
        /// <param name="playingIsActive">if true we set the controls to enabled</param>
        /// <param name="displayText">Text to display in a message box. If null we ignore</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public void ThreadSafeScreenUpdate(object caller, bool playingIsActive, string displayText)
        {

            // log it - the logger is thread safe!
            LogMessage("ThreadSafeScreenUpdate, wantEnable=" + playingIsActive.ToString());
 
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
                Invoke(new ThreadSafeScreenUpdate_Delegate(ThreadSafeScreenUpdate), new object[] { caller, playingIsActive, displayText });
                return;
            }

            // if we get here we are assured we are on the form thread.
            SyncScreenControlsToPlayState(playingIsActive, displayText);
            
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the current video transform object. Can be null
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IMFTransform VideoTransform
        {
            get
            {
                return videoTransform;
            }
            set
            {
                videoTransform = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the transform we are currently using according to the current screen display
        /// state. This does not necessarily implement it in the topology. Just creates 
        /// it in the 
        /// </summary>
        /// <returns> the a new transform object according to the display settings or null for none</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private IMFTransform CreateTransformObjectAccordingToDisplay()
        {
            if (radioButtonMFTGrayscaleAsync.Checked == true) return new MFTTantaGrayscale_Async();
            if (radioButtonMFTWriteText.Checked == true) return new MFTTantaWriteText_Sync(); ;
            if (radioButtonMFTGrayscaleSync.Checked == true)  return new MFTTantaGrayscale_Sync();
            if (radioButtonMFTFrameCounter.Checked == true) return new MFTTantaFrameCounter_Sync();
            return null;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens prepares the media session and topology and opens the media source
        /// and the video and audio renderers. It will also inject the transform
        /// in the video stream if appropriate.
        /// 
        /// Once the session and topology are setup, a MESessionTopologySet event
        /// will be triggered in the callback handler. After that, the events there
        /// trigger other events and everything rolls along automatically.
        /// </summary>
        /// <param name="sourceFileName">the source file name</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void PrepareSessionAndTopology(string sourceFileName)
        {
            HResult hr;
            IMFSourceResolver pSourceResolver = null;
            IMFTopology pTopology = null;
            IMFPresentationDescriptor sourcePresentationDescriptor = null;
            int sourceStreamCount = 0;
            bool streamIsSelected = false;
            IMFStreamDescriptor audioStreamDescriptor = null;
            IMFStreamDescriptor videoStreamDescriptor = null;
            IMFTopologyNode sourceAudioNode = null;
            IMFTopologyNode sourceVideoNode = null;
            IMFTopologyNode outputSinkNodeAudio = null;
            IMFTopologyNode outputSinkNodeVideo = null;
            IMFTopologyNode videoTransformNode = null;

            LogMessage("PrepareSessionAndTopology ");

            // we sanity check the filename - the existence of the path and if the file already exists
            // should have been checked before this call
            if ((sourceFileName == null) || (sourceFileName.Length == 0))
            {
                throw new Exception("PrepareSessionAndTopology: source file name is invalid. Cannot continue.");
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
                hr = MFExtern.MFCreateTopology(out pTopology);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFExtern.MFCreateTopology failed. Err=" + hr.ToString());
                }
                if (pTopology == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFExtern.MFCreateTopology failed. pTopology == null");
                }

                // ####
                // #### we now create the media source, this is an file with audio and video (mp4)
                // ####

                // use the file name to create the media source for the media device. Media sources are objects that generate media data. 
                // For example, the data might come from a video file, a network stream, or a hardware device, such as a camera. Each 
                // media source contains one or more streams, and each stream delivers data of one type, such as audio or video.                
                mediaSource = TantaWMFUtils.GetMediaSourceFromFile(sourceFileName);
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

                // We look at each stream here
                // there will usually be more than one and can be multiple ones
                //  of each type (video, audio etc). Usually only one is enabled. 

                // Because this is app is for the purposes of demonstration we are going to 
                // do each stream type separately rather - one loop for each. Normally 
                // you would just look at the type inside the loop and then make decisions
                // about what to do with it. We are going to "unroll" this process to make 
                // it obvious. We use the first "selected" stream of the appropriate type 
                // we come to

                // first look for the video stream
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

                // next look for the audio stream
                for (int i = 0; i < sourceStreamCount; i++)
                {
                    // we require the major type to be audio
                    Guid guidMajorType = TantaWMFUtils.GetMajorMediaTypeFromPresentationDescriptor(sourcePresentationDescriptor, i);
                    if (guidMajorType != MFMediaType.Audio) continue;

                    // we also require the stream to be enabled
                    hr = sourcePresentationDescriptor.GetStreamDescriptorByIndex(i, out streamIsSelected, out audioStreamDescriptor);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(a) failed. Err=" + hr.ToString());
                    }
                    if (audioStreamDescriptor == null)
                    {
                        throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(a) failed. audioStreamDescriptor == null");
                    }
                    // if the stream is selected, leave now we will release the audioStream descriptor later
                    if (streamIsSelected == true) break;

                    // release the one we are not using
                    if (audioStreamDescriptor != null)
                    {
                        Marshal.ReleaseComObject(audioStreamDescriptor);
                        audioStreamDescriptor = null;
                    }
                }

                // by the time we get here we should have a audio and video StreamDescriptors if
                // we do not, then we cannot proceed. Of course the MP4 file could have only
                // audio or video. However, I don't want to code around those special cases here
                // so we assume the file has both audio and video streams.
                if (videoStreamDescriptor == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. videoStreamDescriptor == null");
                }
                if (audioStreamDescriptor == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. audioStreamDescriptor == null");
                }


                // ####
                // #### we now make up a topology branch for the video stream
                // ####

                // Create a source Video node for this stream.
                sourceVideoNode = TantaWMFUtils.CreateSourceNodeForStream(mediaSource, sourcePresentationDescriptor, videoStreamDescriptor);
                if (sourceVideoNode == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to CreateSourceNodeForStream(v) failed. sourceAudioNode == null");
                }
                // Create a source Audio node for this stream.
                sourceAudioNode = TantaWMFUtils.CreateSourceNodeForStream(mediaSource, sourcePresentationDescriptor, audioStreamDescriptor);
                if (sourceAudioNode == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to CreateSourceNodeForStream(a) failed. sourceAudioNode == null");
                }

                // ####
                // #### we now create the media sink nodes, in this case we are using the Enhance Video Renderer (EVR)
                // #### and the Streaming Audio Renderer (SAR). Both of these discover the media type from the stream
                // #### themselves so, in this case, we do not need to get the current video and audio types from the 
                // #### stream.
                // ####
                // #### Also note that the CreateRendererOutputNodeForStream call places an Activator in the node (not
                // #### the sink itself). The topology, when resolved, will sort this out. However this does mean that 
                // #### we do not, at this point, have a reference to either renderer. We dig the EVR object out of
                // #### the Media Session when its callback indicates the topology is now ready. See the
                // #### MediaSessionTopologyNowReady call for details. The display control needs a reference to the 
                // #### EVR in order to handle things like screen re-sizing etc.
                // ####

                // Create the Video sink node. 
                outputSinkNodeVideo = TantaWMFUtils.CreateEVRRendererOutputNodeForStream(this.ctlTantaEVRStreamDisplay1.DisplayPanelHandle);
                if (outputSinkNodeVideo == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFCreateTopologyNode(v) failed.  outputSinkNodeVideo == null");
                }
                // Create the Audio sink node. 
                outputSinkNodeAudio = TantaWMFUtils.CreateSARRendererOutputNodeForStream();
                if (outputSinkNodeAudio == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFCreateTopologyNode(a) failed.  outputSinkNodeAudio == null");
                }

                // Add the nodes to the topology. First the source nodes
                hr = pTopology.AddNode(sourceVideoNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to pTopology.AddNode(sourceAudioNode) failed. Err=" + hr.ToString());
                }
                hr = pTopology.AddNode(sourceAudioNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to pTopology.AddNode(sourceAudioNode) failed. Err=" + hr.ToString());
                }

                // add the output Nodes
                hr = pTopology.AddNode(outputSinkNodeVideo);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to pTopology.AddNode(outputSinkNodeVideo) failed. Err=" + hr.ToString());
                }
                hr = pTopology.AddNode(outputSinkNodeAudio);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to pTopology.AddNode(outputSinkNodeAudio) failed. Err=" + hr.ToString());
                }

                // set the current transform according to the display
                VideoTransform = CreateTransformObjectAccordingToDisplay();
                // do we have one?
                if(VideoTransform!=null)
                {
                    // yes, we do. Create a video Tranform node for it
                    hr = MFExtern.MFCreateTopologyNode(MFTopologyType.TransformNode, out videoTransformNode);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("PrepareSessionAndTopology call to MFExtern.MFCreateTopologyNode failed. Err=" + hr.ToString());
                    }
                    if (videoTransformNode == null)
                    {
                        throw new Exception("PrepareSessionAndTopology call to MFCreateTopologyNode(t) failed.  videoTransformNode == null");
                    }

                    // set the transform object (it is an IMFTransform) as an object on the transform node. Since it is already there
                    // the topology does not need a GUID or activator to create it
                    hr = videoTransformNode.SetObject(VideoTransform);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("PrepareSessionAndTopology call to pTransformNode.SetObject failed. Err=" + hr.ToString());
                    }
                }

                // Connect the output stream from the source nodes to the input stream of the output nodes. The parameters are:
                //    dwOutputIndex  -  Zero-based index of the output stream on this node.
                //    *pDownstreamNode  -  Pointer to the IMFTopologyNode interface of the node to connect to.
                //    dwInputIndexOnDownstreamNode  -  Zero-based index of the input stream on the other node.

                // Note even though the streamID from the source may be non zero it the output index of these nodes
                // is still 0 since that is the only stream we have configured on it. 

                // do we have a transform? 
                if (VideoTransform != null)
                {
                    // yes we do, inject the transform node into the topology
                    // first source node to transform node
                    hr = sourceVideoNode.ConnectOutput(0, videoTransformNode, 0);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("PrepareSessionAndTopology call to  sourceVideoNode.ConnectOutput failed. Err=" + hr.ToString());
                    }
                    // now transform node to sink node
                    hr = videoTransformNode.ConnectOutput(0, outputSinkNodeVideo, 0);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("PrepareSessionAndTopology call to  videoTransformNode.ConnectOutput failed. Err=" + hr.ToString());
                    }
                }
                else
                {
                    // no we do not, just connect the source to the sink directly
                    hr = sourceVideoNode.ConnectOutput(0, outputSinkNodeVideo, 0);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("PrepareSessionAndTopology call to  sourceVideoNode.ConnectOutput failed. Err=" + hr.ToString());
                    }
                }

                // we have no audio transforms in this demo
                hr = sourceAudioNode.ConnectOutput(0, outputSinkNodeAudio, 0);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to  sourceAudioNode.ConnectOutput failed. Err=" + hr.ToString());
                }

                // Set the topology on the media session.
                // If SetTopology succeeds, the media session will queue an
                // MESessionTopologySet event the MediaSessionTopologyNowReady
                // function will eventually be called asynchronously
                hr = mediaSession.SetTopology(0, pTopology);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("PrepareSessionAndTopology mediaSession.SetTopology failed, retVal=" + hr.ToString());
                }

                // Release the topology
                if (pTopology != null)
                {
                    Marshal.ReleaseComObject(pTopology);
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
                if (audioStreamDescriptor != null)
                {
                    Marshal.ReleaseComObject(audioStreamDescriptor);
                }
                if (videoStreamDescriptor != null)
                {
                    Marshal.ReleaseComObject(videoStreamDescriptor);
                }
                if (sourceAudioNode != null)
                {
                    Marshal.ReleaseComObject(sourceAudioNode);
                }
                if (sourceVideoNode != null)
                {
                    Marshal.ReleaseComObject(sourceVideoNode);
                }
                if (videoTransformNode != null)
                {
                    Marshal.ReleaseComObject(videoTransformNode);
                }
                if (outputSinkNodeVideo != null)
                {
                    Marshal.ReleaseComObject(outputSinkNodeVideo);
                }
                if (outputSinkNodeAudio != null)
                {
                    Marshal.ReleaseComObject(outputSinkNodeAudio);
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

                    // we cannot sucessfully close a lot of things
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
                StartFilePlay();
            }
            catch (Exception ex)
            {
                LogMessage("MediaSessionTopologyNowReady errored ex="+ex.Message);
                OISMessageBox(ex.Message);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Starts the play of the media data
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void StartFilePlay()
        {
            LogMessage("StartFilePlay called");

            if (mediaSession == null)
            {
                LogMessage("StartFilePlay Failed.  mediaSession == null");
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
                throw new Exception("StartFilePlay call to mediaSession.Start failed. Err=" + hr.ToString());
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
        /// Handles a press on the buttonPickFile button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonPickFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();

            openFileDialog1.Filter = "Windows Media|*.wmv;*.wma;*.asf;*.mp4|Wave|*.wav|MP3|*.mp3|All files|*.*";

            // File dialog windows must be on STA threads.  ByteStream handlers are happier if
            // they are opened on MTA.  So, the application stays MTA and we call OpenFileDialog
            // on its own thread.
            TantaOpenFileDialogInvoker invokerObj = new TantaOpenFileDialogInvoker(openFileDialog1);
            // Show the File Open dialog.
            if (invokerObj.Invoke() == DialogResult.OK)
            {
                // pick the file
                textBoxSourceFileNameAndPath.Text = openFileDialog1.FileName;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Displays the frame count from the Transform (if it is in use)
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonGetFrameCount_Click(object sender, EventArgs e)
        {
            // we have to have one
            if (VideoTransform == null)
            {
                OISMessageBox("No transform found.");
                return;
            }
            // it has to be the frame counter transform
            if ((VideoTransform is MFTTantaFrameCounter_Sync) == false)
            {
                OISMessageBox("The Transform in use is not the Frame Counter.");
                return;
            }
            // just display a dialog box
            OISMessageBox("The current frame count is " + (VideoTransform as MFTTantaFrameCounter_Sync).FrameCount.ToString());
        }

    }
}

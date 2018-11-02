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

/// The function of this app is to copy the first audio and video streams it finds in a mp4 media file to another file. The primary
/// purpose is to demonstrate the use of a media source, topology, pipeline with two branches and two sinks in the simplest possible way. 
/// This code assumes the mp4 file has both audio and video streams and will not work on mp4 files that do not. To implement support
/// for audio only or video only would not be hard but doing so would complicate the code and we are trying to keep things simple here.
/// 
namespace TantaVideoFileCopyViaPipelineMP4Sink
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
        private const string APPLICATION_NAME = "TantaVideoFileCopyViaPipelineMP4Sink";
        private const string APPLICATION_VERSION = "01.00";

        private const string START_COPY = "Start Copy";
        private const string STOP_COPY = "Stop Copy";

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

        // Media sinks are objects that renders or records media data. This might be a video display or file on disk 
        // each of which can have multiple streams such as audio or video.
        protected IMFMediaSink mediaSink;

        // our thread safe screen update delegate
        public delegate void ThreadSafeScreenUpdate_Delegate(object obj, bool copyingIsActive, string displayText);

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
                buttonStartStopCopy.Text = START_COPY;
                textBoxSourceFileNameAndPath.Text = DEFAULT_SOURCE_FILE;
                SetOutputFileName();

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

            // close the media sink
            if (mediaSink != null)
            {
                Marshal.ReleaseComObject(mediaSink);
                mediaSink = null;
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
        /// Starts/Stops the copy
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
        private void buttonStartStopCopy_Click(object sender, EventArgs e)
        {
            // this code toggles both the start and stop the copy. Since the
            // STOP code is much simpler we test for it first. We use the 
            // text on the button to detect if we are capturing or not. 
            if (buttonStartStopCopy.Text == STOP_COPY)
            {
                // do everything to close all media devices
                // the MF itself is still active.
                CloseAllMediaDevices();

                // re-enable our screen controls
                SyncScreenControlsToCopyState(false, null);
                return;
            }

            // ####
            // #### below here we assume we are starting the copy
            // ####

            try
            {

                // check our source filename is correct and usable
                if ((SourceFileName == null) || (SourceFileName.Length == 0))
                {
                    MessageBox.Show("No Source Filename and path. Cannot continue.");
                    return;
                }
                // check our output filename is correct and usable
                if ((OutputFileName == null) || (OutputFileName.Length == 0))
                {
                    MessageBox.Show("No Output Filename and path. Cannot continue.");
                    return;
                }
                // check the path is rooted
                if (Path.IsPathRooted(SourceFileName) == false)
                {
                    MessageBox.Show("No Source Filename and path is not rooted. A full directory and path is required. Cannot continue.");
                    return;
                }
                if (Path.IsPathRooted(OutputFileName) == false)
                {
                    MessageBox.Show("No Output Filename and path is not rooted. A full directory and path is required. Cannot continue.");
                    return;
                }

                // set up a session, topology and open the media source and sink etc
                PrepareSessionAndTopology(SourceFileName, OutputFileName);

                // disable our screen controls
                SyncScreenControlsToCopyState(true, null);

            }
            finally
            {

            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sync the state on the screen controls to the current copying state
        /// </summary>
        /// <param name="copyingIsActive">if true we set the controls to enabled</param>
        /// <param name="displayText">Text to display in a message box. If null we ignore</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void SyncScreenControlsToCopyState(bool copyingIsActive, string displayText)
        {

            if(copyingIsActive==false)
            {
                textBoxSourceFileNameAndPath.Enabled = true;
                labelSourceFileName.Enabled = true;
                textBoxOutputFileNameAndPath.Enabled = true;
                labelOutputFileName.Enabled = true;
                buttonStartStopCopy.Text = START_COPY;
            }
            else
            {
                // set this
                textBoxSourceFileNameAndPath.Enabled = false;
                labelSourceFileName.Enabled = false;
                textBoxOutputFileNameAndPath.Enabled = false;
                labelOutputFileName.Enabled = false;
                buttonStartStopCopy.Text = STOP_COPY;
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
        /// <param name="copyingIsActive">if true we set the controls to enabled</param>
        /// <param name="displayText">Text to display in a message box. If null we ignore</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public void ThreadSafeScreenUpdate(object caller, bool copyingIsActive, string displayText)
        {

            // log it - the logger is thread safe!
            LogMessage("ThreadSafeScreenUpdate, wantEnable=" + copyingIsActive.ToString());
 
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
                Invoke(new ThreadSafeScreenUpdate_Delegate(ThreadSafeScreenUpdate), new object[] { caller, copyingIsActive, displayText });
                return;
            }

            // if we get here we are assured we are on the form thread.
            SyncScreenControlsToCopyState(copyingIsActive, displayText);
            
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens prepares the media session and topology and opens the media source
        /// and media sink.
        /// 
        /// Once the session and topology are setup, a MESessionTopologySet event
        /// will be triggered in the callback handler. After that the events there
        /// trigger other events and everything rolls along automatically.
        /// </summary>
        /// <param name="sourceFileName">the source file name</param>
        /// <param name="outputFileName">the name of the output file</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void PrepareSessionAndTopology(string sourceFileName, string outputFileName)
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
            IMFMediaType currentAudioMediaType = null;
            IMFMediaType currentVideoMediaType = null;

            LogMessage("PrepareSessionAndTopology ");

            // we sanity check the filenames - the existence of the path and if the file already exists
            // should have been checked before this call
            if ((sourceFileName == null) || (sourceFileName.Length == 0))
            {
                throw new Exception("PrepareSessionAndTopology: source file name is invalid. Cannot continue.");
            }

            if ((outputFileName == null) || (outputFileName.Length==0))
            {
                throw new Exception("PrepareSessionAndTopology: output file name is invalid. Cannot continue.");
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
                // we assume the file has both audio and video streams.
                if (videoStreamDescriptor == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. videoStreamDescriptor == null");
                }
                if (audioStreamDescriptor==null)
                {
                    throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. audioStreamDescriptor == null");
                }

                // ####
                // #### we now create the media sink, we need the types from the stream to do 
                // #### this which is why we wait until now to set it up
                // ####

                currentVideoMediaType = TantaWMFUtils.GetCurrentMediaTypeFromStreamDescriptor(videoStreamDescriptor);
                if (currentVideoMediaType == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to currentVideoMediaType == null");
                }
                currentAudioMediaType = TantaWMFUtils.GetCurrentMediaTypeFromStreamDescriptor(audioStreamDescriptor);
                if (currentAudioMediaType == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to currentAudioMediaType == null");
                }

                mediaSink = OpenMediaFileSink(outputFileName, currentVideoMediaType, currentAudioMediaType);
                if (mediaSink == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to mediaSink == null");
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

                // Create the Video and Audio sink nodes. Note the use of the media type here. The 
                // MP4 File Sink creates two StreamSinks when it is created (video and audio) and we
                // use the major media type in order to figure out which one is which. It is the 
                // StreamSink that gets added to the node, not the sink itself.
                outputSinkNodeVideo = TantaWMFUtils.CreateSinkNodeForStream(mediaSink, MFMediaType.Video);
                if (outputSinkNodeVideo == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFCreateTopologyNode(v) failed.  outputSinkNodeVideo == null");
                }
                outputSinkNodeAudio = TantaWMFUtils.CreateSinkNodeForStream(mediaSink, MFMediaType.Audio);
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

                // Connect the output stream from the source nodes to the input stream of the output nodes. The parameters are:
                //    dwOutputIndex  -  Zero-based index of the output stream on this node.
                //    *pDownstreamNode  -  Pointer to the IMFTopologyNode interface of the node to connect to.
                //    dwInputIndexOnDownstreamNode  -  Zero-based index of the input stream on the other node.

                // Note even though the streamID from the source may be non zero it the output index of this node
                // is still 0 since that is the only stream we have configured on it. 
                hr = sourceVideoNode.ConnectOutput(0, outputSinkNodeVideo, 0);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to  sourceVideoNode.ConnectOutput failed. Err=" + hr.ToString());
                }
                hr = sourceAudioNode.ConnectOutput(0, outputSinkNodeAudio, 0);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to  sourceAudioNode.ConnectOutput failed. Err=" + hr.ToString());
                }  

                // Set the topology on the media session.
                // If SetTopology succeeds, the media session will queue an
                // MESessionTopologySet event.
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
                if (outputSinkNodeVideo != null)
                {
                    Marshal.ReleaseComObject(outputSinkNodeVideo);
                }
                if (outputSinkNodeAudio != null)
                {
                    Marshal.ReleaseComObject(outputSinkNodeAudio);
                }
                if (currentAudioMediaType != null)
                {
                    Marshal.ReleaseComObject(currentAudioMediaType);
                }
                if (currentVideoMediaType != null)
                {
                    Marshal.ReleaseComObject(currentVideoMediaType);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens the Media File Sink, both the video and audio types cannot be null
        /// at the same time.
        /// 
        /// The caller must release the returned sink.
        /// </summary>
        /// <param name="outputFileName">the filename we write out to</param>
        /// <param name="videoMediaType">the video media type - can be null</param>
        /// <param name="audioMediaType">the audio media type - can be null</param>
        /// <returns>an IMFMediaSink object or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private IMFMediaSink OpenMediaFileSink(string outputFileName, IMFMediaType videoMediaType, IMFMediaType audioMediaType)
        {
            HResult hr;
            IMFMediaSink workingSink = null;
            IMFByteStream outbyteStream = null;

            if ((outputFileName == null) || (outputFileName.Length == 0))
            {
                // we failed
                throw new Exception("OpenMediaFileSink: Invalid filename specified");
            }
            // either the video or audio type can be null but not both
            if ((videoMediaType == null) && (audioMediaType == null))
            {
                // we failed
                throw new Exception("OpenMediaFileSink: Both video and audio types are null");
            }

            try
            {
                // Create the media sink. We use the filename to create a byte stream and
                // then create the sink from that. The types configure the output

                // first we need a bytestream
                hr = MFExtern.MFCreateFile(MFFileAccessMode.ReadWrite, MFFileOpenMode.DeleteIfExist, MFFileFlags.None, outputFileName, out outbyteStream);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("OpenMediaFileSink: Failed on call to MFExtern.MFCreateFile, retVal=" + hr.ToString());
                }
                if (outbyteStream == null)
                {
                    // we failed
                    throw new Exception("OpenMediaFileSink: Failed to create Sink bytestream, Nothing will work.");
                }
                hr = MFExtern.MFCreateMPEG4MediaSink(outbyteStream, videoMediaType, audioMediaType, out workingSink);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("OpenMediaFileSink: Failed on call to MFCreateMPEG4MediaSink, retVal=" + hr.ToString());
                }
                if (workingSink == null)
                {
                    // we failed
                    throw new Exception("OpenMediaFileSink: Failed to create media sink, Nothing will work.");
                }
            }
            catch (Exception ex)
            {
                // note this clean up is in the Catch block not the finally block. 
                // if there are no errors we return it to the caller. The caller
                // is expected to clean up after itself
                if (workingSink != null)
                {
                    // clean up. Nothing else has this yet
                    Marshal.ReleaseComObject(workingSink);
                    workingSink = null;
                }
                workingSink = null;
                throw ex;
            }

            return workingSink;
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
                    ThreadSafeScreenUpdate(this, false, "Done");
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
            LogMessage("MediaSessionTopologyNowReady");

            try
            {
                StartFileCopy();
            }
            catch (Exception ex)
            {
                LogMessage("MediaSessionTopologyNowReady errored ex="+ex.Message);
                OISMessageBox(ex.Message);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Starts the copy of the media data
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void StartFileCopy()
        {
            LogMessage("StartFileCopy called");

            if (mediaSession == null)
            {
                LogMessage("StartFileCopy Failed.  mediaSession == null");
                return;
            }

            // this is what starts the data moving through the pipeline
            HResult hr = mediaSession.Start(Guid.Empty, new PropVariant());
            if (hr != HResult.S_OK)
            {
                throw new Exception("StartFileCopy call to mediaSession.Start failed. Err=" + hr.ToString());
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
        private void textBoxSourceFileNameAndPath_TextChanged(object sender, EventArgs e)
        {
            SetOutputFileName();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the output file name. In this app it is always the source file name with 
        /// the word _TantaCopy appended on it
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SetOutputFileName()
        {
            string dir = Path.GetDirectoryName(SourceFileName);
            string name = Path.GetFileNameWithoutExtension(SourceFileName);
            string ext = Path.GetExtension(SourceFileName);
            textBoxOutputFileNameAndPath.Text = Path.Combine(dir, String.Concat(name, DEFAULT_COPY_SUFFIX, ext));
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

            openFileDialog1.Filter = "MP4 Files (mp4)|*.mp4;|All files|*.*";

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
    }
}

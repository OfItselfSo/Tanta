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

/// The function of this app is to copy the first audio stream it finds in a mp3 media file to another file. The primary
/// purpose is to demonstrate the use of a media source, topology, pipeline and sink in the simplest possible way. 
/// Since this application demonstrates the hybrid architecture, a SinkWriter will be used to perform the
/// actual write of the data. However, SinkWriters cannot directly interface to a pipeline, so a SampleGrabber 
/// sink is used as the Media Sink and its callback handler feeds the audio data into the SinkWriter.
/// 
/// There is only one stream (the audio stream) in this application. The input
/// file does not have to be an audio only file such as an mp3. You can provide
/// a mixed media file such as an mp4 and strip off the video stream using this
/// code. Alternately it is a trivial process to change this code to strip off 
/// audio and copy only the video stream.
/// 
/// This code is for demonstration purposes and has been specifically 
/// written to illustrate the various configuration sequeuences as
/// a linear step-by-step process. This means that a lot of the functionality
/// that could reasonably be factored out into a single call is duplicated.
/// 

namespace TantaAudioFileCopyViaPipelineAndWriter
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
        private const string APPLICATION_NAME = "TantaAudioFileCopyViaPipelineAndWriter";
        private const string APPLICATION_VERSION = "01.00";

        private const string START_COPY = "Start Copy";
        private const string STOP_COPY = "Stop Copy";

        // sample sound file courtesy of https://archive.org/details/testmp3testfile
        private const string DEFAULT_SOURCE_FILE = @"C:\Dump\SampleAudio_0.4mb.mp3";
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

        // this is the call back handler of the Sample Grabber sink. In this
        // particular type of sink a copy of the the data in the pipeline presented 
        // here
        protected TantaSampleGrabberSinkCallback sampleGrabberSinkCallback = null;

        // a class which is our SourceWriter object. We use this to write out the 
        // data since it can, to a limited extent, take care of a lot of the formatting
        // and structure issues for us.
        private IMFSinkWriter workingSinkWriter = null;

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

            // Shutdown the sample grabber callback
            if (sampleGrabberSinkCallback != null)
            {
                sampleGrabberSinkCallback.SinkWriter = null;
                sampleGrabberSinkCallback.SampleGrabberAsyncCallBackError = null;
                sampleGrabberSinkCallback = null;
            }

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

            // End any active captures
            if (workingSinkWriter != null)
            {
                // close and release
                workingSinkWriter.Finalize_();
                //   Marshal.ReleaseComObject(workingSinkWriter);
                workingSinkWriter = null;
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
            // this code toggles both the start and stop of the copy. Since the
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

            // Windows 10, by default, provides an adequate set of codecs which the Sink Writer can
            // find to write out the MP3 file. This is not true on Windows 7.

            // at this time is has not been possible to figure out how to load a set of codecs
            // that will work. So a warning will be issued. If you get this working feel
            // free to send though the update. See the MFTRegisterLocalByCLSID call in the 
            // TantaCaptureToFileViaReaderWriter app for local MFT registration details
            OperatingSystem os = Environment.OSVersion;
            int versionID = ((os.Version.Major*10)+ os.Version.Minor);
            if (versionID < 62)
            {
                OISMessageBox("You appear to be on a version of Windows less than 10. Earlier versions of Windows do not provide a default set of codecs to support this option.\n\nThis operation may not work.");
            }

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
            IMFStreamDescriptor audioStreamDescriptor = null;
            bool streamIsSelected = false;
            IMFTopologyNode sourceAudioNode = null;
            IMFTopologyNode outputSampleGrabberNode = null;
            IMFMediaType currentAudioMediaType = null;
            IMFActivate sampleGrabberSinkActivate=null;
            int audioStreamIndex = -1;

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
                // #### we now create the media source, this is an audio file
                // ####

                // use the file name to create the media source for the audio device. Media sources are objects that generate media data. 
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

                // Look at each stream, there can be more than one stream here
                // Usually only one is enabled. This app uses the first "selected"  
                // stream we come to which has the appropriate media type
                for (int i = 0; i < sourceStreamCount; i++)
                {
                    // we require the major type to be audio
                    Guid guidMajorType = TantaWMFUtils.GetMajorMediaTypeFromPresentationDescriptor(sourcePresentationDescriptor, i);
                    if (guidMajorType != MFMediaType.Audio) continue;

                    // we also require the stream to be enabled
                    hr = sourcePresentationDescriptor.GetStreamDescriptorByIndex(i, out streamIsSelected, out audioStreamDescriptor);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. Err=" + hr.ToString());
                    }
                    if (audioStreamDescriptor == null)
                    {
                        throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. audioStreamDescriptor == null");
                    }
                    // if the stream is selected, leave now we will release the audioStream descriptor later
                    if (streamIsSelected == true)
                    {
                        audioStreamIndex = i;  // record this
                        break;
                    }

                    // release the one we are not using
                    if (audioStreamDescriptor != null)
                    {
                        Marshal.ReleaseComObject(audioStreamDescriptor);
                        audioStreamDescriptor = null;
                    }
                    audioStreamIndex = -1;
                }

                // by the time we get here we should have a audioStreamDescriptor if
                // we do not, then we cannot proceed
                if (audioStreamDescriptor==null)
                {
                    throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. audioStreamDescriptor == null");
                }
                if(audioStreamIndex < 0)
                {
                    throw new Exception("PrepareSessionAndTopology call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. audioStreamIndex < 0");
                }

                // ####
                // #### we now create the media sink, we need the type from the stream to do 
                // #### this which is why we wait until now to set it up
                // ####

                currentAudioMediaType = TantaWMFUtils.GetCurrentMediaTypeFromStreamDescriptor(audioStreamDescriptor);
                if (currentAudioMediaType == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to currentAudioMediaType == null");
                }

                // Create the sample grabber sink callback object.
                sampleGrabberSinkCallback = new TantaSampleGrabberSinkCallback();

                // create the activator for the sample grabber sink
                hr = MFExtern.MFCreateSampleGrabberSinkActivate(currentAudioMediaType, sampleGrabberSinkCallback, out sampleGrabberSinkActivate);
                if (sampleGrabberSinkActivate == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to sampleGrabberSinkActivate == null");
                }
                // To run as fast as possible, set this attribute (requires Windows 7):
                hr = sampleGrabberSinkActivate.SetUINT32(MFAttributesClsid.MF_SAMPLEGRABBERSINK_IGNORE_CLOCK, 1);

                // open up the Sink Writer
                workingSinkWriter = OpenSinkWriter(outputFileName);
                if (workingSinkWriter == null)
                {
                    MessageBox.Show("PrepareSessionAndTopology OpenSinkWriter did not return a media sink. Cannot continue.");
                    return;
                }

                // now set the the sink in the callback handler. It needs to know this
                // in order to operate
                sampleGrabberSinkCallback.SinkWriter = workingSinkWriter;
                sampleGrabberSinkCallback.InitForFirstSample();
                sampleGrabberSinkCallback.SampleGrabberAsyncCallBackError = HandleSampleGrabberAsyncCallBackErrors;

                // ####
                // #### we now make up a topology branch for the audio stream
                // ####

                // Create a source node for this stream.
                sourceAudioNode = TantaWMFUtils.CreateSourceNodeForStream(mediaSource, sourcePresentationDescriptor, audioStreamDescriptor);
                if (sourceAudioNode == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to CreateSourceNodeForStream failed. pSourceNode == null");
                }

                // Create the empty structure of the sink node.
                hr = MFExtern.MFCreateTopologyNode(MFTopologyType.OutputNode, out outputSampleGrabberNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFCreateTopologyNode failed. Err=" + hr.ToString());
                }
                if (outputSampleGrabberNode == null)
                {
                    throw new Exception("PrepareSessionAndTopology call to MFCreateTopologyNode failed. outSinkNode == null");
                }

                // set the activator
                hr = outputSampleGrabberNode.SetObject(sampleGrabberSinkActivate);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to outputSampleGrabberNode.SetObject failed. Err=" + hr.ToString());
                }

                // set the output stream id - always 0 here
                hr = outputSampleGrabberNode.SetUINT32(MFAttributesClsid.MF_TOPONODE_STREAMID, 0);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to outputSampleGrabberNode MF_TOPONODE_STREAMID failed. Err=" + hr.ToString());
                }

                hr = outputSampleGrabberNode.SetUINT32(MFAttributesClsid.MF_TOPONODE_NOSHUTDOWN_ON_REMOVE, 0);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to outputSampleGrabberNode MF_TOPONODE_NOSHUTDOWN_ON_REMOVE failed. Err=" + hr.ToString());
                }

                // Add the nodes to the topology. First the source
                hr = pTopology.AddNode(sourceAudioNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to pTopology.AddNode(sourceAudioNode) failed. Err=" + hr.ToString());
                }

                // then add the output
                hr = pTopology.AddNode(outputSampleGrabberNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to pTopology.AddNode(outputSampleGrabberNode) failed. Err=" + hr.ToString());
                }

                // Connect the output stream from the source node to the input stream of the output node. The parameters are:
                //    dwOutputIndex  -  Zero-based index of the output stream on this node.
                //    *pDownstreamNode  -  Pointer to the IMFTopologyNode interface of the node to connect to.
                //    dwInputIndexOnDownstreamNode  -  Zero-based index of the input stream on the other node.
                hr = sourceAudioNode.ConnectOutput(0, outputSampleGrabberNode, 0);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("PrepareSessionAndTopology call to  pSourceNode.ConnectOutput failed. Err=" + hr.ToString());
                }

                // now configure the SinkWriter

                // add a stream to the sink writer. The currentAudioMediaType specifies the 
                // format of the samples that will be written to the file. Note that it does 
                // not necessarily need to match the input format.  In this case it does 
                // match because we found the audio media type earlier
                int sink_stream = 0;
                hr = workingSinkWriter.AddStream(currentAudioMediaType, out sink_stream);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("PrepareSessionAndTopology Failed adding the output stream, retVal=" + hr.ToString());
                }

                // Set the input format for a stream on the sink writer. Note the use of the stream index here
                // The input format does not have to match the target format that is written to the media sink
                // If the formats do not match, this call attempts to load an transform 
                // that can convert from the input format to the target format. If it cannot find one, (and this is not
                // a sure thing), it will throw an exception.
                hr = workingSinkWriter.SetInputMediaType(sink_stream, currentAudioMediaType, null);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("PrepareSessionAndTopology Failed on calling SetInputMediaType on the writer, retVal=" + hr.ToString());
                }

                // make sure to set this now that we know the stream
                sampleGrabberSinkCallback.SinkWriterMediaStreamId = sink_stream;

                // now we initialize the sink writer for writing. We call this method after configuring the 
                // input streams but before we send any data to the sink writer. The underlying media sink must 
                // have at least one input stream and we know it does because we set it up earlier
                hr = workingSinkWriter.BeginWriting();
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("PrepareSessionAndTopology Failed on calling BeginWriting on the writer, retVal=" + hr.ToString());
                }

                // Set the topology on the media session.
                // If SetTopology succeeds, the media session will queue an
                // MESessionTopologySet event.
                hr = mediaSession.SetTopology(0, pTopology);
                MFError.ThrowExceptionForHR(hr);

                // Release the topology
                if (pTopology != null)
                {
                    Marshal.ReleaseComObject(pTopology);
                }

            }
            catch (Exception ex)
            {
                LogMessage("Error: " + ex.Message);
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
                if (sourceAudioNode != null)
                {
                    Marshal.ReleaseComObject(sourceAudioNode);
                }
                if (outputSampleGrabberNode != null)
                {
                    Marshal.ReleaseComObject(outputSampleGrabberNode);
                }
                if (currentAudioMediaType != null)
                {
                    Marshal.ReleaseComObject(currentAudioMediaType);
                }
                if (currentAudioMediaType != null)
                {
                    Marshal.ReleaseComObject(currentAudioMediaType);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens the Sink Writer object
        /// </summary>
        /// <param name="outputFileName">the filename we write out to</param>
        /// <returns>an IMFSinkWriter object or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private IMFSinkWriter OpenSinkWriter(string outputFileName)
        {
            HResult hr;
            IMFSinkWriter workingWriter = null;

            if ((outputFileName == null) || (outputFileName.Length == 0))
            {
                // we failed
                throw new Exception("OpenSinkWriter: Invalid filename specified");
            }

            try
            {
                // Create the sink writer. This takes the URL of an output file or a pointer to a byte stream and
                // creates the media sink internally. You could also use the more round-about 
                // MFCreateSinkWriterFromMediaSink takes a pointer to a media sink that has already been created by
                // the application. If you are using one of the built-in media sinks, the MFCreateSinkWriterFromURL 
                // function is preferable, because the caller does not need to configure the media sink. 
                hr = MFExtern.MFCreateSinkWriterFromURL(outputFileName, null, null, out workingWriter);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("OpenSinkWriter: Failed on call to MFCreateSinkWriterFromURL, retVal=" + hr.ToString());
                }
                if (workingWriter == null)
                {
                    // we failed
                    throw new Exception("OpenSinkWriter: Failed to create Sink Writer, Nothing will work.");
                }
            }
            catch (Exception ex)
            {
                // note this clean up is in the Catch block not the finally block. 
                // if there are no errors we return it to the caller. The caller
                // is expected to clean up after itself
                if (workingWriter != null)
                {
                    // clean up. Nothing else has this yet
                    Marshal.ReleaseComObject(workingWriter);
                    workingWriter = null;
                }
                workingWriter = null;
                throw ex;
            }

            return workingWriter;
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
        /// Handles error reports from the AsyncCallBackHandler. Note you CANNOT
        /// assume this is called from within the form thread.
        /// </summary>
        /// <param name="caller">the call back handler obj</param>
        /// <param name="errMsg">the error message</param>
        /// <param name="ex">the exception (if there is one) that generated the error</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public void HandleSampleGrabberAsyncCallBackErrors(object caller, string errMsg, Exception ex)
        {

            // log it - the logger is thread safe!
            if (errMsg == null) errMsg = "unknown error";
            LogMessage("HandleSampleGrabberAsyncCallBackErrors, errMsg=" + errMsg);
            if (ex != null)
            {
                LogMessage("HandleSampleGrabberAsyncCallBackErrors, ex=" + ex.Message);
                LogMessage("HandleSampleGrabberAsyncCallBackErrors, ex=" + ex.StackTrace);
            }

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
                Invoke(new TantaSampleGrabberSinkCallback.SampleGrabberAsyncCallBackError_Delegate(HandleSampleGrabberAsyncCallBackErrors), new object[] { this, errMsg, ex });
                return;
            }

            // if we get here we are assured we are on the form thread.

            // do everything to close all media devices
            CloseAllMediaDevices();
            buttonStartStopCopy.Text = START_COPY;
            // re-enable our screen controls
            SyncScreenControlsToCopyState(false, null);

            // tell the user
            if (ex != null) OISMessageBox("There was an error processing the audio stream.\n\n" + ex.Message + "\n\nPlease see the logfile");
            else if (errMsg != null)
            {
                OISMessageBox("There was an error processing the audio stream.\n\n" + errMsg + "\n\nPlease see the logfile");
            }
            else
            {
                OISMessageBox("There was an unknown error processing the audio stream.\n\nPlease see the logfile");
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

            openFileDialog1.Filter = "MP3 Files (mp3)|*.mp3;|All files|*.*";

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

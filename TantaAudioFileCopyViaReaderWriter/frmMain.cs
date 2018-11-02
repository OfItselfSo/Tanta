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

/// The function of this app is to copy the first audio stream it finds in an mp3 media file to another file. 
/// The primary purpose is to demonstrate the use of a synchronous source reader and sink writer in the simplest possible way.
/// The native media types of the audio stream is are used as output and this renders the output file identical 
/// to the input file. This could be changed by adjusting the output media type on the Sink Writer
/// 

namespace TantaAudioFileCopyViaReaderWriter
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
        private const string APPLICATION_NAME = "TantaAudioFileCopyViaReaderWriter";
        private const string APPLICATION_VERSION = "01.00";

        private const string START_COPY = "Start Copy";
        private const string STOP_COPY = "Stop Copy";

        // sample sound file courtesy of https://archive.org/details/testmp3testfile
        private const string DEFAULT_SOURCE_FILE = @"C:\Dump\SampleAudio_0.4mb.mp3";

        private const string DEFAULT_COPY_SUFFIX = "_TantaCopy";

        // indicates if we permit the SourceReader and SinkWriter to load hardware based transforms
        private bool DEFAULT_ALLOW_HARDWARE_TRANSFORMS = false;

        // This is the sink writer that creates the copy of the output file
        protected IMFSinkWriter sinkWriter;

        // This is the source reader the reads the contents of the input file
        protected IMFSourceReader sourceReader;

        // this is used to configure the input and output
        private IMFMediaType sourceReaderNativeAudioMediaType = null;

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
            //HResult hr;
            LogMessage("CloseAllMediaDevices");

            // Shut down the source reader
            if (sourceReader != null)
            {
                Marshal.ReleaseComObject(sourceReader);
                sourceReader = null;
            }

            // close the sink writer
            if (sinkWriter != null)
            {
                // note we could Finalize_() this here but there
                // is no need. That is done when the stream ends
                Marshal.ReleaseComObject(sinkWriter);
                sinkWriter = null;
            }

            if (sourceReaderNativeAudioMediaType != null)
            {
                Marshal.ReleaseComObject(sourceReaderNativeAudioMediaType);
                sourceReaderNativeAudioMediaType = null;
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
        /// Starts/Stops the file copy
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

            // Windows 10, by default, provides an adequate set of codecs which the Sink Writer can
            // find to write out the MP3 file. This is not true on Windows 7.

            // at this time is has not been possible to figure out how to load a set of codecs
            // that will work. So a warning will be issued. If you get this working feel
            // free to send though the update. See the MFTRegisterLocalByCLSID call in the 
            // TantaCaptureToFileViaReaderWriter app for local MFT registration details
            OperatingSystem os = Environment.OSVersion;
            int versionID = ((os.Version.Major * 10) + os.Version.Minor);
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

                // disable our screen controls
                SyncScreenControlsToCopyState(true, null);

                // Set up a source reader and sink writer and copy the file
                CopyFile(SourceFileName, OutputFileName);

                // enable our screen controls
                SyncScreenControlsToCopyState(false, "Done");

            }
            catch (Exception ex)
            {
                LogMessage("Exception: " + ex.Message);
                LogMessage("Stack Trace: " + ex.StackTrace);
                OISMessageBox("An error occured: " + ex.Message + ", Please see the logfile");
                // enable our screen controls
                SyncScreenControlsToCopyState(false, null);
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
        /// Does everything to copy a file. Opens the Source Reader and Sink Writer
        /// configures the streams and then, because a Synchronous version
        /// of the Source Reader is used, we sit in a loop and perform the copy.
        /// 
        /// Any errors here simply throw an exception and must be trapped elsewhere
        /// 
        /// Note that because this code is intended for demo purposes, it has been
        /// kept very simple and linear. Most of the things that could have been 
        /// refactored in a common procedure are simply 
        /// written out in order to make it obvious what is going on.
        /// </summary>
        /// <param name="sourceFileName">the source file name</param>
        /// <param name="outputFileName">the name of the output file</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void CopyFile(string sourceFileName, string outputFileName)
        {
            HResult hr;

            int sinkWriterOutputAudioStreamId = -1;
            int audioSamplesProcessed = 0;
            bool audioStreamIsAtEOS = false;
            int sourceReaderAudioStreamId=-1;

            // not keen on endless loops. This is the maximum number
            // of streams we will check in the source reader.
            const int MAX_SOURCEREADER_STREAMS = 100;

            // create the SourceReader
            sourceReader = TantaWMFUtils.CreateSourceReaderSyncFromFile(sourceFileName, DEFAULT_ALLOW_HARDWARE_TRANSFORMS);
            if (sourceReader == null)
            {
                // we failed
                throw new Exception("CopyFile: Failed to create SourceReader, Nothing will work.");
            }
            // create the SinkWriter
            sinkWriter = TantaWMFUtils.CreateSinkWriterFromFile(outputFileName, DEFAULT_ALLOW_HARDWARE_TRANSFORMS);
            if (sinkWriter == null)
            {
                // we failed
                throw new Exception("CopyFile: Failed to create Sink Writer, Nothing will work.");
            }

            // find the first audio stream and identify the default Media Type
            // it is using. We could look into the stream and enumerate all of the 
            // types on offer and choose one from the list - but for a copy operation 
            // the default will be quite suitable.

            sourceReaderNativeAudioMediaType = null;
            for (int streamIndex =0; streamIndex < MAX_SOURCEREADER_STREAMS; streamIndex++)
            {
                IMFMediaType workingType = null;
                Guid guidMajorType = Guid.Empty;

                // the the major media type - we are looking for audio
                hr = sourceReader.GetNativeMediaType(streamIndex, 0, out workingType);
                if (hr == HResult.MF_E_NO_MORE_TYPES) break;
                if (hr == HResult.MF_E_INVALIDSTREAMNUMBER) break;
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CopyFile: failed on call to GetNativeMediaType, retVal=" + hr.ToString());
                }
                if (workingType == null)
                {
                    // we failed
                    throw new Exception("CopyFile: failed on call to GetNativeMediaType, workingType == null");
                }

                // what major type does this stream have?
                hr = workingType.GetMajorType(out guidMajorType);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CopyFile:  call to workingType.GetMajorType failed. Err=" + hr.ToString());
                }
                if (guidMajorType == null)
                {
                    throw new Exception("CopyFile:  call to workingType.GetMajorType failed. guidMajorType == null");
                }

                // test for audio (there can be others)
                if ((guidMajorType == MFMediaType.Audio))
                {
                    // this stream represents a audio type
                    sourceReaderNativeAudioMediaType = workingType;
                    sourceReaderAudioStreamId = streamIndex;
                    // the sourceReaderNativeAudioMediaType will be released elsewhere
                    break;
                }

                // if we get here release the type - we do not use it
                if (workingType != null)
                {
                    Marshal.ReleaseComObject(workingType);
                    workingType = null;
                }          
            }

            // at this point we expect we can have a native video or a native audio media type
            // or both, but not neither. if we don't we cannot carry on            
            if (sourceReaderNativeAudioMediaType == null)
            {
                // we failed
                throw new Exception("CopyFile: failed on call to GetNativeMediaType, sourceReaderNativeAudioMediaType == null");
            }

            // set the media type on the reader - this is the media type the source reader will output
            // this does not have to match the media type in the file. If it does not the Source Reader
            // will attempt to load a transform to perform the conversion. In this case we know it 
            // matches because the type we are using IS the same media type we got from the stram
            hr = sourceReader.SetCurrentMediaType(sourceReaderAudioStreamId, null, sourceReaderNativeAudioMediaType);
            if (hr != HResult.S_OK)
            {
                // we failed
                throw new Exception("CopyFile: failed on call to SetCurrentMediaType(a), retVal=" + hr.ToString());
            }

            // add a stream to the sink writer. The mediaType specifies the format of the samples that will be written 
            // to the file. Note that it does not necessarily need to match the format of the samples
            // we provide to the sink writer. In this case, because we are copying a file, the media type
            // we write to disk IS the media type the source reader reads from the disk.
            hr = sinkWriter.AddStream(sourceReaderNativeAudioMediaType, out sinkWriterOutputAudioStreamId);
            if (hr != HResult.S_OK)
            {
                // we failed
                throw new Exception("CopyFile: Failed adding the output stream(a), retVal=" + hr.ToString());
            }

            // Set the input format for a stream on the sink writer. Note the use of the stream index here
            // The input format does not have to match the output format that is written to the media sink
            // If the formats do not match, this call attempts to load an transform that can convert from 
            // the input format to the target format. If it cannot find one, and this is not a sure thing, 
            // it will throw an exception.
            hr = sinkWriter.SetInputMediaType(sinkWriterOutputAudioStreamId, sourceReaderNativeAudioMediaType, null);
            if (hr != HResult.S_OK)
            {
                // we failed
                throw new Exception("CopyFile: Failed on calling SetInputMediaType(a) on the writer, retVal=" + hr.ToString());
            }

            // begin writing on the sink writer
            hr = sinkWriter.BeginWriting();
            if (hr != HResult.S_OK)
            {
                // we failed
                throw new Exception("CopyFile: failed on call to BeginWriting, retVal=" + hr.ToString());
            }

            // we sit in a loop here and get the sample from the source reader and write it out
            // to the sink writer. An EOS (end of sample) value in the flags will signal the end.
            // Note the application will appear to be locked up while we are in here. We are ok
            // with this because it is quick and we want to keep things simple
            while (true)
            {
                int actualStreamIndex;
                MF_SOURCE_READER_FLAG actualStreamFlags;
                long timeStamp = 0;
                IMFSample workingMediaSample = null;

                // Request the next sample from the media source. Note that this could be
                // any type of media sample (video, audio, subtitles etc). We do not know
                // until we look at the stream ID. We saved the stream ID earlier when
                // we obtained the media types and so we can branch based on that. 

                // In reality since we only set up one stream (audio) this will always be
                // the audio stream - but there is no need to assume this and the
                // TantaVideoFileCopyViaReaderWriter demonstrates an example with two 
                // streams (audio and video)
                hr = sourceReader.ReadSample(
                    TantaWMFUtils.MF_SOURCE_READER_ANY_STREAM,
                    0,
                    out actualStreamIndex,
                    out actualStreamFlags,
                    out timeStamp,
                    out workingMediaSample
                    );
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CopyFile: Failed on calling the ReadSample on the reader, retVal=" + hr.ToString());
                }

                // the sample may be null if either end of stream or a stream tick is returned
                if (workingMediaSample == null)
                {
                    // just ignore, the flags will have the information we need.
                }
                else
                {
                    // the sample is not null
                    if (actualStreamIndex == sourceReaderAudioStreamId)
                    {
                        // audio data
                        // ensure discontinuity is set for the first sample in each stream
                        if (audioSamplesProcessed == 0)
                        {
                            // audio data
                            hr = workingMediaSample.SetUINT32(MFAttributesClsid.MFSampleExtension_Discontinuity, 1);
                            if (hr != HResult.S_OK)
                            {
                                // we failed
                                throw new Exception("CopyFile: Failed on calling SetUINT32 on the sample, retVal=" + hr.ToString());
                            }
                            // remember this - we only do it once
                            audioSamplesProcessed++;
                        }
                        hr = sinkWriter.WriteSample(sinkWriterOutputAudioStreamId, workingMediaSample);
                        if (hr != HResult.S_OK)
                        {
                            // we failed
                            throw new Exception("CopyFile: Failed on calling the WriteSample on the writer, retVal=" + hr.ToString());
                        }
                    }
 
                    // release the sample
                    if (workingMediaSample != null)
                    {
                        Marshal.ReleaseComObject(workingMediaSample);
                        workingMediaSample = null;
                    }
                }

                // do we have a stream tick event?
                if ((actualStreamFlags & MF_SOURCE_READER_FLAG.StreamTick)!=0)
                {
                    if (actualStreamIndex == sourceReaderAudioStreamId)
                    {
                        // audio stream
                        hr = sinkWriter.SendStreamTick(sinkWriterOutputAudioStreamId, timeStamp);
                    }
                    else
                    {
                    }
                }

                // is this stream at an END of Segment
                if ((actualStreamFlags & MF_SOURCE_READER_FLAG.EndOfStream) !=0)
                {
                    // We have an EOS - but is it on the audio channel?
                    if (actualStreamIndex == sourceReaderAudioStreamId)
                    {
                        // audio stream
                        // have we seen this before?
                        if (audioStreamIsAtEOS == false)
                        {
                            hr = sinkWriter.NotifyEndOfSegment(sinkWriterOutputAudioStreamId);
                            if (hr != HResult.S_OK)
                            {
                                // we failed
                                throw new Exception("CopyFile: Failed on calling the NotifyEndOfSegment on audio stream, retVal=" + hr.ToString());
                            }
                            audioStreamIsAtEOS = true;
                        }
                        // audio stream
                    }
                    else
                    {
                    }

                    // our exit condition depends on which streams are in use
                    if (sourceReaderNativeAudioMediaType != null)
                    {
                        // only audio is active, if the audio stream is EOS we can leave
                        if (audioStreamIsAtEOS == true) break;
                    }
                }
            } // bottom of endless for loop

            hr = sinkWriter.Finalize_();
            if (hr != HResult.S_OK)
            {
                // we failed
                throw new Exception("Failed on call tosinkWriter.Finalize(), retVal=" + hr.ToString());
            }

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

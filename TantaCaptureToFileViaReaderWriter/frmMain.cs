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

/// The function of this app is to capture images from a video camera and write those images to a file. It is a tidied up 
/// re-write of the MF.Net WindowsCaptureToFile-2010 sample code. 

namespace TantaCaptureToFileViaReaderWriter
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
        private const string APPLICATION_NAME = "TantaCaptureToFileViaReaderWriter";
        private const string APPLICATION_VERSION = "01.00";

        private const string START_CAPTURE = "Start Capture";
        private const string STOP_CAPTURE = "Stop Capture";

        private const string DEFAULT_CAPTURE_FILE = @"C:\Dump\TantaCaptureToFileViaReaderWriter.mp4";

        // 240 * 1000 is the value used in the sample. Not sure how it is derived
        // the one below gives way better quality video
        private const int TARGET_BIT_RATE = 2000 * 1000;
        private static Guid MEDIA_TYPETO_WRITE = MFMediaType.H264; // MP4, this could also be MFMediaType.WMV3 or others

        // a class which is our SourceReader object.
        private IMFSourceReaderAsync workingSourceReader = null;

        // a class which is our SourceWriter object
        private IMFSinkWriter workingSinkWriter = null;

        // a class which acts as our Async Mode stream data pump
        private TantaSourceReaderCallbackHandler workingSourceReaderCallBackHandler = null;

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
                buttonStartStopCapture.Text = START_CAPTURE;
                textBoxCaptureFileNameAndPath.Text = DEFAULT_CAPTURE_FILE;

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
        /// A centralized place to close down all media devices 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void CloseAllMediaDevices()
        {
            // close the async call back handler
            CloseAsyncCallBackHandler(workingSourceReaderCallBackHandler);
            workingSourceReaderCallBackHandler = null;

            // close the media sink
            CloseSinkWriter(workingSinkWriter);
            workingSinkWriter = null;            

            // Close the media source
            CloseSourceReader(workingSourceReader);
            workingSourceReader = null;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Starts/Stops the capture. 
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void buttonStartStopCapture_Click(object sender, EventArgs e)
        {
            // this code toggles both the start and stop the capture. Since the
            // STOP code is much simpler we test for it first. We use the 
            // text on the button to detect if we are capturing or not. 
            if (buttonStartStopCapture.Text == STOP_CAPTURE)
            {
                buttonStartStopCapture.Text = START_CAPTURE;
                // do everything to close all media devices
                // the MF itself is still active.
                CloseAllMediaDevices();

                // re-enable our screen controls
                SetEnableStateOnScreenControls(true);
                return;
            }
            else
            {

                // start the capture
                CaptureToFile();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Starts capture of the data to a file. 
        /// 
        /// Because this code is intended for demo purposes and in the interests of
        /// reducing complexity it is extremely linear and step-by-step. Doubtless 
        /// there is much refactoring that could be done.
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void CaptureToFile()
        {
            HResult hr;
            IMFMediaType videoType = null;
            IMFMediaType encoderType = null;
            TantaMFDevice currentDevice = null;

            try
            {
 
                // get the current video device. 
                currentDevice = ctlTantaVideoPicker1.CurrentDevice;
                if (currentDevice == null)
                {
                    MessageBox.Show("No current video device. Are there any video devices on this system?");
                    return;
                }

                // check our output filename is correct and usable
                if ((textBoxCaptureFileNameAndPath == null) || (textBoxCaptureFileNameAndPath.Text.Length == 0))
                {
                    MessageBox.Show("No Capture Filename and path. Cannot continue.");
                    return;
                }
                string pwszFileName = textBoxCaptureFileNameAndPath.Text;
                // check the path is rooted
                if (Path.IsPathRooted(pwszFileName) == false)
                {
                    MessageBox.Show("No Capture Filename and path is not rooted. A full directory and path is required. Cannot continue.");
                    return;
                }

                // create a new call back handler. This, once we get it all wired up, will act
                // as a pump to move the data from the source to the sink
                workingSourceReaderCallBackHandler = new TantaSourceReaderCallbackHandler();

                // the following code will create a SourceReader which is tied to a camera on the system,
                // a SinkWriter which is tied to a file output and will hook up the two. Because we are using
                // a SourceReader and SourceWriter we do not have the usual Topology or Pipeline. The SourceReader
                // and SourceWriter are connected directly (input to output) in the code below and transfer their
                // data via the callback handler. The callback handler also requests the next sample from
                // the SourceReader when it has written the data to the sink. Note however it is possible
                // that the SourceReader can automatically bring in a Transform for format conversion. This
                // is done internally and you never deal with it - other than perhaps making it available
                // to the process if it is not globally available.

                // create the source reader
                workingSourceReader = TantaWMFUtils.CreateSourceReaderAsyncFromDevice(currentDevice, workingSourceReaderCallBackHandler);
                if (workingSourceReader == null)
                {
                    MessageBox.Show("CreateSourceReaderAsyncFromDevice did not return a media source. Cannot continue.");
                    return;
                }

                // open up the sink Writer
                workingSinkWriter = OpenSinkWriter(pwszFileName);
                if (workingSinkWriter == null)
                {
                    MessageBox.Show("OpenSinkWriter workingSinkWriter == null. Cannot continue.");
                    return;
                }

                // now set the source and the sink in the callback handler. It needs to know these
                // in order to operate
                workingSourceReaderCallBackHandler.SourceReader = workingSourceReader;
                workingSourceReaderCallBackHandler.SinkWriter = workingSinkWriter;
                workingSourceReaderCallBackHandler.InitForFirstSample();
                workingSourceReaderCallBackHandler.SourceReaderAsyncCallBackError = HandleSourceReaderAsyncCallBackErrors;

                // now we configure the video source. It will probably offer a lot of different types
                // this example offers two modes: one mode where you choose the format and mode and 
                // effectively just say "Use this one". The other uses the general case where we
                // present a list of reasonable types we can accept and then let it auto
                // configure itself from one of those. Of course if it autoconfigures itself we 
                // don't know which one it has chosen. This is why, you will later see the video 
                // source being interrogated after the configuration so we know which one we hit.

                if (radioButtonUseSpecified.Checked == true)
                {
                    // we saved the video format container here - this is just the last one that came in
                    if ((radioButtonUseSpecified.Tag == null) || ((radioButtonUseSpecified.Tag is TantaMFVideoFormatContainer)==false))
                    {
                        MessageBox.Show("No source video device and format selected. Cannot continue.");
                        return;
                    }
                    // get the container
                    TantaMFVideoFormatContainer videoFormatCont = (radioButtonUseSpecified.Tag as TantaMFVideoFormatContainer);
                    // configure the Source Reader to use this format
                    hr = TantaWMFUtils.ConfigureSourceReaderWithVideoFormat(workingSourceReader, videoFormatCont);
                    if (hr != HResult.S_OK)
                    {
                        // we failed
                        MessageBox.Show("Failed on call to ConfigureSourceAsyncReaderWithVideoFormat (a), retVal=" + hr.ToString());
                        return;
                    }
                }
                else
                {
                    // prepare a list of subtypes we are prepared to accept from the video source
                    // device. These will be tested in order - the first match will be used.
                    List<Guid> subTypes = new List<Guid>();
                    subTypes.Add(MFMediaType.NV12);
                    subTypes.Add(MFMediaType.YUY2);
                    subTypes.Add(MFMediaType.UYVY);
                    subTypes.Add(MFMediaType.RGB32);
                    subTypes.Add(MFMediaType.RGB24);
                    subTypes.Add(MFMediaType.IYUV);

                    // make sure the default Media Type is one of the above video formats
                    hr = TantaWMFUtils.ConfigureSourceReaderWithVideoFormat(workingSourceReader, subTypes, false);
                    if (hr != HResult.S_OK)
                    {
                        // we failed
                        MessageBox.Show("Failed on call to ConfigureSourceAsyncReaderWithVideoFormat (b), retVal=" + hr.ToString());
                        return;
                    }
                }

                // if we get here we know the source reader now has a configured format but we might not
                // know which one it is. So we ask it. It will return a video type
                // we will use this later on to configure our sink writer. Note, we have to properly dispose 
                // of the videoType object after we use it.
                hr = workingSourceReader.GetCurrentMediaType(TantaWMFUtils.MF_SOURCE_READER_FIRST_VIDEO_STREAM, out videoType);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed on call to GetCurrentMediaType, retVal=" + hr.ToString());
                }

                // now we configure the encoder. This sets up the sink writer so that it knows what format
                // the output data should be written in. The format we give the writer does not
                // need to be the same as the format it outputs to disk - however to make life easier for ourselves
                // we will copy a lot of the settings from the videoType retrieved above

                // create a new empty media type for us to populate
                hr = MFExtern.MFCreateMediaType(out encoderType);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed on call to MFCreateMediaType, retVal=" + hr.ToString());
                }

                // The major type defines the overall category of the media data. Major types include video, audio, script & etc.
                hr = encoderType.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, MFMediaType.Video);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed setting the MF_MT_MAJOR_TYPE, retVal=" + hr.ToString());
                }

                // The subtype GUID defines a specific media format type within a major type. For example, within video, 
                // the subtypes include MFMediaType.H264 (MP4), MFMediaType.WMV3 (WMV), MJPEG & etc. Within audio, the 
                // subtypes include PCM audio, Windows Media Audio 9, & etc.
                hr = encoderType.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, MEDIA_TYPETO_WRITE);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed setting the MF_MT_SUBTYPE, retVal=" + hr.ToString());
                }

                // this is the approximate data rate of the video stream, in bits per second, for a video media type
                // in the MF.Net sample code this is 240000 but I found 2000000 to be much better. I am not sure,
                // at this time, how this value is derived or what the tradeoffs are.
                hr = encoderType.SetUINT32(MFAttributesClsid.MF_MT_AVG_BITRATE, TARGET_BIT_RATE);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed setting the MF_MT_AVG_BITRATE, retVal=" + hr.ToString());
                }

                // populate our new encoding type with the frame size of the videoType selected earlier
                hr = TantaWMFUtils.CopyAttributeData(videoType, encoderType, MFAttributesClsid.MF_MT_FRAME_SIZE);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed copying the MF_MT_FRAME_SIZE, retVal=" + hr.ToString());
                }

                // populate our new encoding type with the frame rate of the video type selected earlier
                hr = TantaWMFUtils.CopyAttributeData(videoType, encoderType, MFAttributesClsid.MF_MT_FRAME_RATE);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed copying the MF_MT_FRAME_RATE, retVal=" + hr.ToString());
                }

                // populate our new encoding type with the pixel aspect ratio of the video type selected earlier
                hr = TantaWMFUtils.CopyAttributeData(videoType, encoderType, MFAttributesClsid.MF_MT_PIXEL_ASPECT_RATIO);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed copying the MF_MT_PIXEL_ASPECT_RATIO, retVal=" + hr.ToString());
                }

                // populate our new encoding type with the interlace mode of the video type selected earlier
                hr = TantaWMFUtils.CopyAttributeData(videoType, encoderType, MFAttributesClsid.MF_MT_INTERLACE_MODE);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed copying the MF_MT_INTERLACE_MODE, retVal=" + hr.ToString());
                }

                // add a stream to the sink writer. The encoderType specifies the format of the samples that will be written 
                // to the file. Note that it does not necessarily need to match the input format. To set the input format
                // use SetInputMediaType. 
                int sink_stream = 0;
                hr = workingSinkWriter.AddStream(encoderType, out sink_stream);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed adding the output stream, retVal=" + hr.ToString());
                }

                // Windows 10, by default, provides an adequate set of codecs which the Sink Writer can
                // find to write out the MP4 file. This is not true on Windows 7.

                // If we are not on Windows 10 we register (locally) a codec
                // the Sink Writer can find and use. The ColorConvertDMO is supplied by
                // microsoft it is just not available to enumerate on Win7 etc. 
                // Making it available locally does not require administrator privs
                // but only this process can see it and it disappears when the process 
                // closes
                OperatingSystem os = Environment.OSVersion;
                int versionID = ((os.Version.Major * 10) + os.Version.Minor);
                if (versionID < 62)
                {
                    Guid ColorConvertDMOGUID = new Guid("98230571-0087-4204-b020-3282538e57d3");

                    // Register the color converter DSP for this process, in the video 
                    // processor category. This will enable the sink writer to enumerate
                    // the color converter when the sink writer attempts to match the
                    // media types. 
                    hr = MFExtern.MFTRegisterLocalByCLSID(
                        ColorConvertDMOGUID,
                        MFTransformCategory.MFT_CATEGORY_VIDEO_PROCESSOR,
                        "",
                        MFT_EnumFlag.SyncMFT,
                        0,
                        null,
                        0,
                        null
                        );
                }

                // Set the input format for a stream on the sink writer. Note the use of the stream index here
                // The input format does not have to match the target format that is written to the media sink
                // If the formats do not match, this call attempts to load an transform 
                // that can convert from the input format to the target format. If it cannot find one, and this is not
                // a sure thing, it will throw an exception.
                hr = workingSinkWriter.SetInputMediaType(sink_stream, videoType, null);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed on calling SetInputMediaType on the writer, retVal=" + hr.ToString());
                }

                // now we initialize the sink writer for writing. We call this method after configuring the 
                // input streams but before we send any data to the sink writer. The underlying media sink must 
                // have at least one input stream and we know it does because we set it up earlier
                hr = workingSinkWriter.BeginWriting();
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed on calling BeginWriting on the writer, retVal=" + hr.ToString());
                }

                // Request the first video frame from the media source. The TantaSourceReaderCallbackHandler
                // set up earlier will be invoked and it will continue requesting and processing video
                // frames after that.
                hr = workingSourceReader.ReadSample(
                    TantaWMFUtils.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                    0,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero,
                    IntPtr.Zero
                    );
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed on calling the first ReadSample on the reader, retVal=" + hr.ToString());
                }

                // we are ready to start, flag this
                buttonStartStopCapture.Text = STOP_CAPTURE;
                // disable our screen controls
                SetEnableStateOnScreenControls(false);

            }
            finally
            {
                // setting this to null will cause it to be cleaned up
                currentDevice = null;

                // close and release
                if (videoType != null)
                {
                    Marshal.ReleaseComObject(videoType);
                    videoType = null;
                }

                if (encoderType != null)
                {
                    Marshal.ReleaseComObject(encoderType);
                    encoderType = null;
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
        /// Close the Media Source
        /// </summary>
        /// <param name=" readerObject">the media source object</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void CloseSourceReader(IMFSourceReaderAsync readerObject)
        {
            // End any active captures
            if (readerObject == null)
            {
                LogMessage("CloseSourceReader readerObject == null");
                return;
            }

            // close and release
            Marshal.ReleaseComObject(readerObject);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Close the Media sink
        /// </summary>
        /// <param name="writerObject">the media source object</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void CloseSinkWriter(IMFSinkWriter writerObject)
        {
            // End any active captures
            if (writerObject == null)
            {
                LogMessage("CloseSinkWriter rwriterObject == null");
                return;
            }

            // close and release
            HResult hr = writerObject.Finalize_();
            Marshal.ReleaseComObject(writerObject);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Closes the AsyncCallBackHandler
        /// </summary>
        /// <param name="callbackHandler">the call back handler</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void CloseAsyncCallBackHandler(TantaSourceReaderCallbackHandler callbackHandler)
        {
            // End any active captures
            if (callbackHandler == null)
            {
                LogMessage("CloseAsyncCallBackHandler callbackHandler == null");
                return;
            }

            // close and release
            callbackHandler.SourceReader = null;
            callbackHandler.SinkWriter = null;
            callbackHandler.SourceReaderAsyncCallBackError = null;
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
        public void HandleSourceReaderAsyncCallBackErrors(object caller, string errMsg, Exception ex)
        {

            // log it - the logger is thread safe!
            if (errMsg == null) errMsg = "unknown error";
            LogMessage("HandleSourceReaderAsyncCallBackErrors, errMsg=" + errMsg);
            if (ex != null)
            {
                LogMessage("HandleSourceReaderAsyncCallBackErrors, ex=" + ex.Message);
                LogMessage("HandleSourceReaderAsyncCallBackErrors, ex=" + ex.StackTrace);
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
                Invoke(new TantaSourceReaderCallbackHandler.SourceReaderAsyncCallBackError_Delegate(HandleSourceReaderAsyncCallBackErrors), new object[] { this, errMsg, ex}); 
                return;
            }

            // if we get here we are assured we are on the form thread.

            // do everything to close all media devices
            CloseAllMediaDevices();
            buttonStartStopCapture.Text = START_CAPTURE;
            // re-enable our screen controls
            SetEnableStateOnScreenControls(true);

            // tell the user
            if (ex != null) OISMessageBox("There was an error processing the video stream.\n\n" + ex.Message + "\n\nPlease see the logfile");
            else if(errMsg!=null)
            {
                OISMessageBox("There was an error processing the video stream.\n\n" + errMsg + "\n\nPlease see the logfile");
            }
            else
            {
                OISMessageBox("There was an unknown error processing the video stream.\n\nPlease see the logfile");
            }


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
            string mfDeviceName = "<unknown device>";

            // set these now
            if (videoDevice != null)
            {
                mfDeviceName = videoDevice.FriendlyName;
                // set the button text appropriately
                radioButtonVideoFormatAutoSelect.Text = "Auto Select in Device: " + mfDeviceName;
                // save the container here - this is the last one that came in
                radioButtonVideoFormatAutoSelect.Tag = videoDevice;
            }
            else
            {
                // set the button text appropriately
                radioButtonVideoFormatAutoSelect.Text = "Auto Select in Device: <unknown device>";
                // save the container here - this is the last one that came in
                radioButtonVideoFormatAutoSelect.Tag = null;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Handle a picked video device and format
        /// </summary>
        /// <param name="videoFormatCont">the video format container. Also contains the device</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void VideoFormatPickedHandler(object sender, TantaMFVideoFormatContainer videoFormatCont)
        {
            string mfDeviceName = "<unknown device>";
            string formatSummary = "<unknown format>";

            // set these now
            if (videoFormatCont != null)
            {
                formatSummary = videoFormatCont.DisplayString();
                if (videoFormatCont.VideoDevice != null) mfDeviceName = videoFormatCont.VideoDevice.FriendlyName;
                // set the button text appropriately
                radioButtonUseSpecified.Text = mfDeviceName + " " + formatSummary;
                // save the container here - this is the last one that came in
                radioButtonUseSpecified.Tag = videoFormatCont;
            }
            else
            {
                // set the button text appropriately
                radioButtonUseSpecified.Text = "Use: " + mfDeviceName + " " + formatSummary;
                // save the container here - this is the last one that came in
                radioButtonUseSpecified.Tag = null;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the enabled state on the screen controls
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void SetEnableStateOnScreenControls(bool wantEnabled)
        {
            // re-enable our screen controls
            radioButtonVideoFormatAutoSelect.Enabled = wantEnabled;
            radioButtonUseSpecified.Enabled = wantEnabled;
            ctlTantaVideoPicker1.Enabled = wantEnabled;
            textBoxCaptureFileNameAndPath.Enabled = wantEnabled;
            label1.Enabled = wantEnabled;
            label2.Enabled = wantEnabled;
            label4.Enabled = wantEnabled;
        }

    }
}

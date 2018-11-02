using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

using MediaFoundation;
using MediaFoundation.Misc;
using MediaFoundation.ReadWrite;
using MediaFoundation.Transform;
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

/// Large parts of this code are derived from the samples which ship with the MF.Net dll. These are 
/// in turn derived from the original Microsoft samples. These have been placed in the public domain 
/// without copyright. The original copyright statement is below

/// *****************************************************************************
/// Original Copyright Statement - Released to public domain
/// While the underlying library is covered by LGPL or BSD, this sample is released
/// as public domain.  It is distributed in the hope that it will be useful, but
/// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
/// or FITNESS FOR A PARTICULAR PURPOSE.
/// ******************************************************************************

/// This file implements a Synchronous Media Foundation Transform (MFT)
/// which simply passes the input samples to the output. As such it
/// accepts all formats. 
/// 
/// However, the transform also implements "sample grabber" type 
/// functionality - much like the Microsoft supplied SampleGrabber sink.
/// If the sink writer object is present, the transform will 
/// also give the incoming sample out to the sink writer so that it can
/// be written to disk.
/// 
/// There are two modes. In the simplest mode the input sample is simply
/// given to the sink writer. If the user wants to implement a rebasing
/// of the time on the sample (so that the first sample starts at time
/// 0 and every subsequent sample is adjusted off of that) then the 
/// input sample is cloned, the timestamp reset and that cloned sample
/// is given to the sink writer. Note that the rebasing of the timestamp
/// does not seem to be necessary - it probably once was because all of
/// the sample code seems to take particular care to implement this.
/// 
/// In the interests of simplicity, this particular MFT is not designed
/// to be "independent" of the rest of the application. In other words,
/// it will not be placed in an independent assembly (DLL) it will not 
/// be COM visible or registered with MFTRegister so that other applications
/// can use it. This MFT is expected to be instantiated with a standard
/// C# new operator and simply given to the topology as a binary

namespace TantaCaptureToScreenAndFile
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// An MFT which acts as a sample grabber. The input type and the output 
    /// type are the same for this MFT. We are doing "inplace" processing on
    /// the frames and simply giving the input data back to the output and 
    /// doing nothing to it. This means we can support any type as long as 
    /// we can enforce that our input type is the same as the output type. 
    /// 
    /// This class uses the TantaMFTBase_Sync class which handles 90%
    /// of the work involved in implementing an MFT. We just have to 
    /// set up a few overrides in order to get things operational.  
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>
    public sealed class MFTTantaSampleGrabber_Sync : TantaMFTBase_Sync
    {

        // if you look at other sample TantaMFT's you will see this gets
        // populated with the types we support. Since we support all types
        // this can be empty and we handle it in the overrides below
        private readonly Guid[] m_MediaSubtypes = new Guid[] { Guid.Empty };

        // used for the timebase rebasing
        private bool isFirstSample = true;
        private long firstSampleBaseTime;
        bool wantTimebaseRebase = false;

        // this is the sinkwriter we will write to if it is present.
        private IMFSinkWriter workingSinkWriter = null;
        private object sinkWriterLockObject = new object();
        private int sinkWriterVideoStreamId = -1;

        // used to setup the output media type
        private const int TARGET_BIT_RATE = 2000 * 1000;
        private static Guid MEDIA_TYPETO_WRITE = MFMediaType.H264; // MP4, this could also be MFMediaType.WMV3 or others

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public MFTTantaSampleGrabber_Sync() : base()
        {
            // DebugMessage("MFTTantaSampleGrabber_Sync Constructor");

            isFirstSample = true;
            wantTimebaseRebase = false;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Detects if we are currently recording
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public bool IsRecording
        {
            get
            {
                // we assume we have one of these
                if (workingSinkWriter == null) return false;
                return true;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Starts the process of recording. creates the sink writer. We do not
        ///  check to see if the filename is viable or already exists. This is 
        ///  assumed to have been done before this call.
        /// </summary>
        /// <param name="outputFileName">the output file name</param>
        /// <param name="incomingVideoMediaType">the incoming media type</param>
        /// <param name="wantTimebaseRebaseIn">if true we rebase all incoming sample
        /// times to zero from the point we started recording and send a copy of the 
        /// sample to the sink writer instead of the input sample</param>
        /// <returns>z success, nz fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public int StartRecording(string outputFileName, IMFMediaType incomingVideoMediaType, bool wantTimebaseRebaseIn)
        {
            HResult hr;
            IMFMediaType encoderType = null;

            LogMessage("MFTTantaSampleGrabber_Sync, StartRecording called");

            // first stop any recordings now
            StopRecording();

            // check the output file name for sanity
            if((outputFileName == null)|| (outputFileName.Length==0))
            {
                LogMessage("StartRecording (outputFileName==null)|| (outputFileName.Length==0)");
                return 100;
            }

            // check the media type for sanity
            if (incomingVideoMediaType == null) 
            {
                LogMessage("StartRecording videoMediaType == null");
                return 150;
            }

            lock (sinkWriterLockObject)
            {
                // create the sink writer            
                workingSinkWriter = OpenSinkWriter(outputFileName, true);
                if (workingSinkWriter == null)
                {
                    LogMessage("StartRecording failed to create sink writer");
                    return 200;
                }

                // now configure the SinkWriter. This sets up the sink writer so that it knows what format
                // the output data should be written in. The format we give the writer does not
                // need to be the same as the format receives as input - however to make life easier for ourselves
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

                // this is the approximate data rate of the video stream, in bits per second, for a 
                // video media type. The choice here is somewhat arbitrary but seems to work well.
                hr = encoderType.SetUINT32(MFAttributesClsid.MF_MT_AVG_BITRATE, TARGET_BIT_RATE);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed setting the MF_MT_AVG_BITRATE, retVal=" + hr.ToString());
                }

                // populate our new encoding type with the frame size of the videoType selected earlier
                hr = TantaWMFUtils.CopyAttributeData(incomingVideoMediaType, encoderType, MFAttributesClsid.MF_MT_FRAME_SIZE);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed copying the MF_MT_FRAME_SIZE, retVal=" + hr.ToString());
                }

                // populate our new encoding type with the frame rate of the video type selected earlier
                hr = TantaWMFUtils.CopyAttributeData(incomingVideoMediaType, encoderType, MFAttributesClsid.MF_MT_FRAME_RATE);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed copying the MF_MT_FRAME_RATE, retVal=" + hr.ToString());
                }

                // populate our new encoding type with the pixel aspect ratio of the video type selected earlier
                hr = TantaWMFUtils.CopyAttributeData(incomingVideoMediaType, encoderType, MFAttributesClsid.MF_MT_PIXEL_ASPECT_RATIO);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed copying the MF_MT_PIXEL_ASPECT_RATIO, retVal=" + hr.ToString());
                }

                // populate our new encoding type with the interlace mode of the video type selected earlier
                hr = TantaWMFUtils.CopyAttributeData(incomingVideoMediaType, encoderType, MFAttributesClsid.MF_MT_INTERLACE_MODE);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("Failed copying the MF_MT_INTERLACE_MODE, retVal=" + hr.ToString());
                }

                // add a stream to the sink writer for the output Media type. The 
                // incomingVideoMediaType specifies the format of the samples that will
                // be written to the file. Note that it does not necessarily need to 
                // match the input format. 
                hr = workingSinkWriter.AddStream(encoderType, out sinkWriterVideoStreamId);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("StartRecording Failed adding the output stream(v), retVal=" + hr.ToString());
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
                    Guid ColorConverterDMOGUID = new Guid("98230571-0087-4204-b020-3282538e57d3");

                    // Register the color converter DSP for this process, in the video 
                    // processor category. This will enable the sink writer to enumerate
                    // the color converter when the sink writer attempts to match the
                    // media types. 
                    hr = MFExtern.MFTRegisterLocalByCLSID(
                        ColorConverterDMOGUID,
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
                // that can convert from the input format to the target format. If it cannot find one, (and this is not
                // a sure thing), it will throw an exception.
                hr = workingSinkWriter.SetInputMediaType(sinkWriterVideoStreamId, incomingVideoMediaType, null);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("StartRecording Failed on calling SetInputMediaType(v) on the writer, retVal=" + hr.ToString());
                }

                // set this flag now
                wantTimebaseRebase = wantTimebaseRebaseIn;

                // now we initialize the sink writer for writing. We call this method after configuring the 
                // input streams but before we send any data to the sink writer. The underlying media sink must 
                // have at least one input stream and we know it does because we set it up above
                hr = workingSinkWriter.BeginWriting();
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("StartRecording Failed on calling BeginWriting on the writer, retVal=" + hr.ToString());
                }
            }

            return 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  stops the process of recording. finalizes and then releases the sink writer
        /// </summary>
        /// <returns>z success, nz fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public int StopRecording()
        {
            LogMessage("StopRecording called");

            // End any active captures
            if (workingSinkWriter != null)
            {
                lock (sinkWriterLockObject)
                {
                    // close and release
                    workingSinkWriter.Finalize_();
                    Marshal.ReleaseComObject(workingSinkWriter);
                    workingSinkWriter = null;
                }
            }

            // reset these
            sinkWriterVideoStreamId = -1;
            isFirstSample = true;
            firstSampleBaseTime = 0;
            wantTimebaseRebase = false;

            return 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens the Sink Writer object
        /// </summary>
        /// <param name="outputFileName">the filename we write out to</param>
        /// <param name="wantDisableThrottling">if true we set a flag that disabled throttling</param>
        /// <returns>an IMFSinkWriter object or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private IMFSinkWriter OpenSinkWriter(string outputFileName, bool wantDisableThrottling)
        {
            HResult hr;
            IMFSinkWriter workingWriter = null;
            IMFAttributes attributeContainer = null;

            if ((outputFileName == null) || (outputFileName.Length == 0))
            {
                // we failed
                throw new Exception("OpenSinkWriter: Invalid filename specified");
            }

            try
            {
                if (wantDisableThrottling == true)
                {
                    // Initialize an attribute store. We will use this to 
                    // specify the enumeration parameters.
                    hr = MFExtern.MFCreateAttributes(out attributeContainer, 1);
                    if (hr != HResult.S_OK)
                    {
                        // we failed
                        throw new Exception("OpenSinkWriter: failed on call to MFCreateAttributes, retVal=" + hr.ToString());
                    }
                    if (attributeContainer == null)
                    {
                        // we failed
                        throw new Exception("OpenSinkWriter: attributeContainer == MFAttributesClsid.null");
                    }
                    // setup the attribute container
                    hr = attributeContainer.SetUINT32(MFAttributesClsid.MF_SINK_WRITER_DISABLE_THROTTLING, 1);
                    if (hr != HResult.S_OK)
                    {
                        // we failed
                        throw new Exception("OpenSinkWriter: failed setting up the attributes, retVal=" + hr.ToString());
                    }
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
            }
            finally
            {
                if (attributeContainer != null)
                {
                    // clean up. 
                    Marshal.ReleaseComObject(attributeContainer);
                    attributeContainer = null;
                }
            }

            return workingWriter;
        }

        // ########################################################################
        // ##### TantaMFTBase_Sync Overrides, all child classes must implement these
        // ########################################################################

        #region Overrides

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Returns a value indicating if the proposed input type is acceptable to 
        /// this MFT.
        /// 
        /// An override of the abstract version in TantaMFTBase_Sync. 
        /// </summary>
        /// <param name="pmt">The type to check.  Should never be null.</param>
        /// <returns>S_Ok if the type is valid or MF_E_INVALIDTYPE.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        override protected HResult OnCheckInputType(IMFMediaType pmt)
        {
            HResult hr;

            // We accept any input type as long as the output type
            // has not been set yet
            if (OutputType == null)
            {
                hr = HResult.S_OK;
            }
            else
            {
                // Otherwise, proposed input must be identical to the output.
                hr = TantaWMFUtils.IsMediaTypeIdentical(pmt, OutputType);
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Return settings to describe input stream. This should get the buffer 
        /// requirements and other information for an input stream. 
        /// (see IMFTransform::GetInputStreamInfo).
        /// 
        /// An override of the abstract version in TantaMFTBase_Sync. 
        /// </summary>
        /// <param name="pStreamInfo">The struct where the parameters get set.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        override protected void OnGetInputStreamInfo(ref MFTInputStreamInfo pStreamInfo)
        {
            // the code below tells the caller what we do
            pStreamInfo.cbSize = 0;
            // MFT_INPUT_STREAM_PROCESSES_IN_PLACE - The MFT can perform in-place processing.
            //     In this mode, the MFT directly modifies the input buffer. When the client calls 
            //     ProcessOutput, the same sample that was delivered to this stream is returned in 
            //     the output stream that has a matching stream identifier. This flag implies that 
            //     the MFT holds onto the input buffer, so this flag cannot be combined with the 
            //     MFT_INPUT_STREAM_DOES_NOT_ADDREF flag. If this flag is present, the MFT must 
            //     set the MFT_OUTPUT_STREAM_PROVIDES_SAMPLES or MFT_OUTPUT_STREAM_CAN_PROVIDE_SAMPLES 
            //     flag for the output stream that corresponds to this input stream. 
            pStreamInfo.dwFlags = MFTInputStreamInfoFlags.ProcessesInPlace;

            // Note: There are many other flags we could set here but because we are doing
            //       nothing to the sample (other than counting it) we do not need them. 
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Return settings to describe output stream. This should get the buffer 
        /// requirements and other information for an output stream. 
        /// (see IMFTransform::GetOutputStreamInfo).
        /// 
        /// An override of the abstract version in TantaMFTBase_Sync. 
        /// </summary>
        /// <param name="pStreamInfo">The struct where the parameters get set.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        override protected void OnGetOutputStreamInfo(ref MFTOutputStreamInfo pStreamInfo)
        {
            // the code below tells the caller what we do
            pStreamInfo.cbSize = 0;
            // MFT_OUTPUT_STREAM_PROVIDES_SAMPLES - The MFT provides the output samples 
            //    for this stream, either by allocating them internally or by operating 
            //    directly on the input samples. The MFT cannot use output samples provided 
            //    by the client for this stream. If this flag is not set, the MFT must 
            //    set cbSize to a nonzero value in the MFT_OUTPUT_STREAM_INFO structure, 
            //    so that the client can allocate the correct buffer size. For more information,
            //    see IMFTransform::GetOutputStreamInfo. This flag cannot be combined with 
            //    the MFT_OUTPUT_STREAM_CAN_PROVIDE_SAMPLES flag.
            pStreamInfo.dwFlags = MFTOutputStreamInfoFlags.ProvidesSamples;

            // Note: There are many other flags we could set here but because we are doing
            //       nothing to the sample (other than counting it) we do not need them. 

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This is the routine that performs the transform. Unless the sinkWriter object
        /// is set all we do is pass the sample on. If the sink writer object is set
        /// we give the sample to it for writing. There are two modes - one where we just
        /// give the sinkwriter the input sample and the other where we clone the input
        /// sample and rebase the timestamps.
        /// 
        /// An override of the abstract version in TantaMFTBase_Sync. 
        /// </summary>
        /// <param name="pOutputSamples">The structure to populate with output values.</param>
        /// <returns>S_Ok unless error.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally written
        /// </history>
        protected override HResult OnProcessOutput(ref MFTOutputDataBuffer outputSampleDataStruct)
        {
            HResult hr = HResult.S_OK;
            IMFMediaBuffer inputMediaBuffer = null;
            IMFSample sinkWriterSample = null;
            IMFAttributes sampleAttributes = null;
            long sampleDuration = 0;
            int sampleSize = 0;
            long sampleTimeStamp = 0;
            int sampleFlags = 0;

            // in this MFT we are processing in place, the input sample is the output sample, the media buffer of the
            // input sample is the media buffer of the output sample. Thats for the pipeline. If a sink writer exists
            // we also write the sample data out to the sink writer. This provides the effect of displaying on the 
            // screen and simultaneously recording.

            // There are two ways the sink writer can be given the media sample data. It can just be given the 
            // input sample directly or a copy of the sample can be made and that copy given to the sink writer.

            // There is also an additional complication - the sample has a timestamp and video cameras tend
            // to just use the current date and time as a timestamp. There are several reports that MP4 files 
            // need to have their first frame starting at zero and then every subsequent frame adjusted to that
            // new base time. Certainly the Microsoft supplied example code (and see the 
            // TantaCaptureToFileViaReaderWriter also) take great care to do this. This requirement does not
            // seem to exist - my tests indicate it is not necessary to start from 0 in the mp4 file. Maybe the
            // Sink Writer has been improved and now does this automatically. For demonstration purposes 
            // the timebase-rebase functionality has been included and choosing that mode copies the sample
            // and resets the time. If the user does not rebase the time we simply send the input sample
            // to the Sink Writer as-is.

            try
            {

                // Set status flags.
                outputSampleDataStruct.dwStatus = MFTOutputDataBufferFlags.None;

                // The output sample is the input sample. We get a new IUnknown for the Input
                // sample since we are going to release it below. The client will release this 
                // new IUnknown
                outputSampleDataStruct.pSample = Marshal.GetIUnknownForObject(InputSample);

                // are we recording?
                if (workingSinkWriter != null)
                {
                    // we do everything in a lock
                    lock (sinkWriterLockObject)
                    {
                        // are we in timebase rebase mode?
                        if (wantTimebaseRebase == false )
                        {
                            // we are not. Just give the input sample to the Sink Writer which will
                            // write it out.
                            hr = workingSinkWriter.WriteSample(sinkWriterVideoStreamId, InputSample);
                            if (hr != HResult.S_OK)
                            {
                                throw new Exception("OnProcessOutput call to WriteSample(a) failed. Err=" + hr.ToString());
                            }
                        }
                        else
                        {
                            // the timebase rebase option has been chosen. We need to create a copy of the input sample
                            // so we can adjust the time on it.

                            // Get the data buffer from the input sample. If the sample contains more than one buffer, 
                            // this method copies the data from the original buffers into a new buffer, and replaces 
                            // the original buffer list with the new buffer. The new buffer is returned in the inputMediaBuffer parameter.
                            // If the sample contains a single buffer, this method returns a pointer to the original buffer. 
                            // In typical use, most samples do not contain multiple buffers.
                            hr = InputSample.ConvertToContiguousBuffer(out inputMediaBuffer);
                            if (hr != HResult.S_OK)
                            {
                                throw new Exception("OnProcessOutput call to InputSample.ConvertToContiguousBuffer failed. Err=" + hr.ToString());
                            }

                            // get some other things from the input sample
                            hr = InputSample.GetSampleDuration(out sampleDuration);
                            if (hr != HResult.S_OK)
                            {
                                throw new Exception("OnProcessOutput call to InputSample.GetSampleDuration failed. Err=" + hr.ToString());
                            }
                            hr = InputSample.GetTotalLength(out sampleSize);
                            if (hr != HResult.S_OK)
                            {
                                throw new Exception("OnProcessOutput call to InputSample.GetTotalLength failed. Err=" + hr.ToString());
                            }
                            hr = InputSample.GetSampleTime(out sampleTimeStamp);
                            if (hr != HResult.S_OK)
                            {
                                throw new Exception("OnProcessOutput call to InputSample.GetSampleTime failed. Err=" + hr.ToString());
                            }

                            // get the attributes from the input sample
                            if (InputSample is IMFAttributes) sampleAttributes = (InputSample as IMFAttributes);
                            else sampleAttributes = null;

                            // we have all the information we need to create a new output sample
                            sinkWriterSample = TantaWMFUtils.CreateMediaSampleFromBuffer(sampleFlags, sampleTimeStamp, sampleDuration, inputMediaBuffer, sampleSize, sampleAttributes);
                            if (sinkWriterSample == null)
                            {
                                throw new Exception("OnProcessOutput, Error on call to CreateMediaSampleFromBuffer sinkWriterSample == null");
                            }

                            // we have a sample, if so is it the first non null one?
                            if (isFirstSample)
                            {
                                // yes it is set up our timestamp
                                firstSampleBaseTime = sampleTimeStamp;
                                isFirstSample = false;
                            }

                            // rebase the time stamp
                            sampleTimeStamp -= firstSampleBaseTime;

                            hr = sinkWriterSample.SetSampleTime(sampleTimeStamp);
                            if (hr != HResult.S_OK)
                            {
                                throw new Exception("OnProcessOutput call to InputSample.SetSampleTime failed. Err=" + hr.ToString());
                            }

                            // write the sample out
                            hr = workingSinkWriter.WriteSample(sinkWriterVideoStreamId, sinkWriterSample);
                            if (hr != HResult.S_OK)
                            {
                                throw new Exception("OnProcessOutput call to WriteSample(b) failed. Err=" + hr.ToString());
                            }
                        }
                    }
                }
            }
            finally
            {
                // clean up
                if (inputMediaBuffer != null)
                {
                    Marshal.ReleaseComObject(inputMediaBuffer);
                    inputMediaBuffer = null;
                }

                if (sinkWriterSample != null)
                {
                    Marshal.ReleaseComObject(sinkWriterSample);
                    sinkWriterSample = null;
                }

                // Release the current input sample so we can get another one.
                // the act of setting it to null releases it because the property
                // is coded that way
                InputSample = null;
            }

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The MFT defines a list of available media types for each input stream
        /// and orders them by preference. This method enumerates the available
        /// media types for an input stream. 
        /// 
        /// Many clients will just "try it on" with their preferred media type
        /// and if/when that gets rejected will start enumerating the types the
        /// transform prefers in order to see if they have one in common
        ///
        /// An override of the virtual version in TantaMFTBase_Sync. 
        /// </summary>
        /// <param name="dwTypeIndex">The (zero-based) index of the type.</param>
        /// <param name="pInputType">The input type supported by the MFT.</param>
        /// <returns>S_Ok unless error.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected override HResult OnEnumInputTypes(int dwTypeIndex, out IMFMediaType pInputType)
        {
            // MF.Net Sample comments...
            // I'd like to skip implementing this, but while some clients 
            // don't require it (PlaybackFX), some do (MEPlayer/IMFMediaEngine).  
            // Although frame counting should be able to run against any type, 
            // we must at a minimum provide a major type.

            return TantaWMFUtils.CreatePartialMediaType(dwTypeIndex, MFMediaType.Video, m_MediaSubtypes, out pInputType);
        }

        #endregion

        

    }

}

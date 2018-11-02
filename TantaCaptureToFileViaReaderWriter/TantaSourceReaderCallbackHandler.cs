using System;
using System.Runtime.InteropServices;
using System.Security;

using MediaFoundation;
using MediaFoundation.ReadWrite;
using MediaFoundation.Misc;
using MediaFoundation.Transform;
using MediaFoundation.Alt;
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
/// This code is derived from the sample which ships with the MF.Net dll. 
/// These have been placed in the public domain 
/// without copyright.
/// 

namespace TantaCaptureToFileViaReaderWriter
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// A class to handle async SourceReader callbacks. This class is populated
    /// appropriately and then fed into the SourceReader. Once the first
    /// ReadSample is called on the SourceReader the code in this class is 
    /// called and it continues requesting and processing new video frames
    /// indefinitely
    /// 
    /// Note: The callback interface must be thread-safe, because OnReadSample 
    /// and the other callback methods are called from worker threads. 
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public class TantaSourceReaderCallbackHandler: COMBase, IMFSourceReaderCallback
    {

        // our SourceReader object.
        private IMFSourceReaderAsync sourceReader = null;

        // our SinkWriter object
        private IMFSinkWriter sinkWriter = null;

        // our error reporting delegate
        public delegate void SourceReaderAsyncCallBackError_Delegate(object obj, string errMsg, Exception ex);
        public SourceReaderAsyncCallBackError_Delegate SourceReaderAsyncCallBackError = null;

       // private bool isFirstSample;
       // private long firstSampleBaseTime;
 
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// gets/sets the sourceReader. Will get/set null
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public IMFSourceReaderAsync SourceReader
        {
            get
            {
                return sourceReader;
            }
            set
            {
                sourceReader = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// gets/sets the sinkWriter. Will get/set null
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public IMFSinkWriter SinkWriter
        {
            get
            {
                return sinkWriter;
            }
            set
            {
                sinkWriter = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Inits this class for the first sample
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public void InitForFirstSample()
        {
            //isFirstSample = true;
            //firstSampleBaseTime = 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This gets called when a Called IMFSourceReader.ReadSample method completes
        /// (assuming the SourceReader has been given this class during setup with
        /// an attribute of MFAttributesClsid.MF_SOURCE_READER_ASYNC_CALLBACK).
        /// 
        /// The first ReadSample triggers it after that it continues by itself
        /// </summary>
        /// <param name="hrStatus">The status code. If an error occurred while processing the next sample, this parameter contains the error code.</param>
        /// <param name="streamIndex">The zero-based index of the stream that delivered the sample.</param>
        /// <param name="streamFlags">A bitwise OR of zero or more flags from the MF_SOURCE_READER_FLAG enumeration.</param>
        /// <param name="sampleTimeStamp">The time stamp of the sample, or the time of the stream event indicated in streamFlags. The time is given in 100-nanosecond units. </param>
        /// <param name="mediaSample">A pointer to the IMFSample interface of a media sample. This parameter might be NULL.</param>
        /// <returns>Returns an HRESULT value. Reputedly, the source reader ignores the return value.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public HResult OnReadSample(HResult hrStatus, int streamIndex, MF_SOURCE_READER_FLAG streamFlags, long sampleTimeStamp, IMFSample mediaSample)
        {
            HResult hr = HResult.S_OK;
            try
            {
                lock (this)
                {
                    // are we capturing? if not leave
                    if (IsCapturing() == false)
                    {
                        return HResult.S_OK;
                    }

                    // have we got an error?
                    if (Failed(hrStatus))
                    {
                        string errMsg = "OnReadSample, Error on call =" + hrStatus.ToString();
                        SourceReaderAsyncCallBackError(this, errMsg, null);
                        return hrStatus;
                    }

                    // have we got a sample? It seems this can be null on the first sample
                    // in after the ReadSample that triggered this. So we just ignore it
                    // and request the next to get things rolling
                    if (mediaSample != null)
                    {

/*  This rebases the timestamps coming off the webcam so that
 *  the first one starts at zero. Not needed in later versions
 *  of WMF apparently. It seems the Media Sink now does this automatically
 *  
 *  If the data is not being written correctly, try uncommenting this.
 *  
                        // we have a sample, if so is it the first non null one?
                        if (isFirstSample)
                        {
                            // yes it is set up our timestamp
                            firstSampleBaseTime = sampleTimeStamp;
                            isFirstSample = false;
                        }

                        // Samples have a time stamp and a duration. The time stamp indicates when the data in the sample 
                        // should be rendered, relative to the presentation clock. The duration is the length of time 
                        // for which the data should be rendered.
                        //
                        // We now set the presentation time of the sample. This is the presentation time
                        // in 100-nanosecond units. We rebase this using the first sample
                        // processed as zero.

                        // rebase the time stamp
                        sampleTimeStamp -= firstSampleBaseTime;
                        hr = mediaSample.SetSampleTime(sampleTimeStamp);
                        if (Failed(hr))
                        {
                            string errMsg = "OnReadSample, Error on SetSampleTime =" + hr.ToString();
                            SourceReaderAsyncCallBackError(this, errMsg, null);
                            return hr;
                        } */

                        // write the sample out
                        hr = sinkWriter.WriteSample(0, mediaSample);
                        if (Failed(hr))
                        {
                            string errMsg = "OnReadSample, Error on WriteSample =" + hr.ToString();
                            SourceReaderAsyncCallBackError(this, errMsg, null);
                            return hr;
                        }
                    }
 
                    // Read another sample.
                    hr = (sourceReader as IMFSourceReaderAsync).ReadSample(
                        TantaWMFUtils.MF_SOURCE_READER_FIRST_VIDEO_STREAM,
                        0,
                        IntPtr.Zero,   // actual
                        IntPtr.Zero,   // flags
                        IntPtr.Zero,   // timestamp
                        IntPtr.Zero    // sample
                        );                
                    if (Failed(hr))
                    {
                        string errMsg = "OnReadSample, Error on ReadSample =" + hr.ToString();
                        SourceReaderAsyncCallBackError(this, errMsg, null);
                        return hr;
                    }
                }
            }
            catch (Exception ex)
            {
                if (SourceReaderAsyncCallBackError != null)
                {
                    SourceReaderAsyncCallBackError(this, ex.Message, ex);
                }
            }
            finally
            {
                SafeRelease(mediaSample);
            }

            return hr;
        }

        public HResult OnEvent(int a, IMFMediaEvent b)
        {
            return HResult.S_OK;
        }

        public HResult OnFlush(int a)
        {
            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Detects if a capture operation is in progress
        /// </summary>
        /// <returns>an HResult</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public bool IsCapturing()
        {
            lock (this)
            {
                // this is the way the MF.Net sample code does it. It is a fairly crude
                // sort of marker - but I suppose it is sufficient.
                if (sinkWriter != null) return true;
                else return false;
            }
        }
    }
}

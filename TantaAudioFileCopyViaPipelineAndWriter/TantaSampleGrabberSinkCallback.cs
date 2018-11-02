using System;
using System.Runtime.InteropServices;
using System.Security;

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

namespace TantaAudioFileCopyViaPipelineAndWriter
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// A class to handle async SampleGrabber callbacks. The SampleGrabber
    /// is a Media Sink which makes the data in the media streams available
    /// to objects outside the Media Pipeline. When this callback is provided
    /// to the SampleGrabber Sink the data in each IMFMediaBuffer will be
    /// presented in the OnProcessSample call. 
    /// 
    /// In this example we wish to send the data to a SinkWriter so a SinkWriter
    /// object is provided at configuration time.
    /// 
    /// Information about errors is transmitted back via the SampleGrabberAsyncCallBackError
    /// event.
    ///  
    /// Note: The callback interface must be thread-safe because OnProcessSample
    /// and the other callback methods are called from worker threads. 
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public class TantaSampleGrabberSinkCallback : COMBase, IMFSampleGrabberSinkCallback2
    {
        // our SinkWriter object
        private IMFSinkWriter sinkWriter = null;
        private int sinkWriterMediaStreamId = -1;

        // our error reporting delegate
        public delegate void SampleGrabberAsyncCallBackError_Delegate(object obj, string errMsg, Exception ex);
        public SampleGrabberAsyncCallBackError_Delegate SampleGrabberAsyncCallBackError = null;

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
        /// gets/sets the stream to write to on the sinkWriter
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public int SinkWriterMediaStreamId
        {
            get
            {
                return sinkWriterMediaStreamId;
            }
            set
            {
                sinkWriterMediaStreamId = value;
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
        }

        #region IMFSampleGrabberSink2 methods

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the presentation clock is set.
        /// </summary>
        /// <param name="pPresentationClock">the presentation clock</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public HResult OnSetPresentationClock(IMFPresentationClock pPresentationClock)
        {
            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the sample grabber sink processes a sample. We can use this
        /// to do what we want with the sample
        /// </summary>
        /// <param name="guidMajorMediaType">the media type</param>
        /// <param name="sampleFlags">the sample flags</param>
        /// <param name="sampleSize">the sample size</param>
        /// <param name="sampleDuration">the sample duration</param>
        /// <param name="sampleTimeStamp">the sample time</param>
        /// <param name="sampleBuffer">the sample buffer</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public HResult OnProcessSample(Guid guidMajorMediaType, int sampleFlags, long sampleTimeStamp, long sampleDuration, IntPtr sampleBuffer, int sampleSize)
        {
            // just call the ex version
            return OnProcessSample(guidMajorMediaType, sampleFlags, sampleTimeStamp, sampleDuration, sampleBuffer, sampleSize);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the sample grabber sink processes a sample. We can use this
        /// to do what we want with the media data
        /// </summary>
        /// <param name="guidMajorMediaType">the media type</param>
        /// <param name="sampleFlags">the sample flags</param>
        /// <param name="sampleSize">the sample size</param>
        /// <param name="sampleDuration">the sample duration</param>
        /// <param name="sampleTimeStamp">the sample time</param>
        /// <param name="sampleBuffer">the sample buffer</param>
        /// <param name="sampleAttributes">the attributes for the sample</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public HResult OnProcessSampleEx(Guid guidMajorMediaType, int sampleFlags, long sampleTimeStamp, long sampleDuration, IntPtr sampleBuffer, int sampleSize, IMFAttributes sampleAttributes)
        {
            IMFSample outputSample = null;
            HResult hr;

            try
            {
                if(sinkWriter==null)
                {
                    string errMsg = "OnProcessSample, Error sinkWriter==null";
                    SampleGrabberAsyncCallBackError(this, errMsg, null);
                    return HResult.E_FAIL;
                }

                // we have all the information we need to create a new output sample
                outputSample = TantaWMFUtils.CreateMediaSampleFromIntPtr(sampleFlags, sampleTimeStamp, sampleDuration, sampleBuffer, sampleSize, null);
                if (outputSample == null)
                {
                    string errMsg = "OnProcessSample, Error on call to CreateMediaSampleFromBuffer outputSample == null";
                    SampleGrabberAsyncCallBackError(this, errMsg, null);
                    return HResult.E_FAIL;
                }
 
                // write the sample out
                hr = sinkWriter.WriteSample(sinkWriterMediaStreamId, outputSample);
                if (Failed(hr))
                {
                    string errMsg = "OnProcessSample, Error on WriteSample =" + hr.ToString();
                    SampleGrabberAsyncCallBackError(this, errMsg, null);
                    return hr;
                }
            }
            finally
            {
                if(outputSample!=null)
                {
                    Marshal.ReleaseComObject(outputSample);
                    outputSample = null;
                }
            }

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the sample grabber sink is shutting down.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public HResult OnShutdown()
        {
            return HResult.S_OK;
        }
        #endregion

        #region IMFClockStateSink methods

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the presentation clock starts.
        ///
        /// hnsSystemTime: System time when the clock started.
        /// llClockStartOffset: Starting presentatation time.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public HResult OnClockStart(long hnsSystemTime, long llClockStartOffset)
        {
            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the presentation clock stops. After this method is 
        /// called, we stop accepting new data.
        ///
        /// hnsSystemTime: System time when the clock started.
        /// llClockStartOffset: Starting presentatation time.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public HResult OnClockStop(long hnsSystemTime)
        {
            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the presentation clock paused.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public HResult OnClockPause(long hnsSystemTime)
        {
            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the presentation clock restarts.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public HResult OnClockRestart(long hnsSystemTime)
        {
            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the presentation clock's rate changes. For a rateless
        /// sink, the clock rate is not important.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public HResult OnClockSetRate(long hnsSystemTime, float flRate)
        {
            return HResult.S_OK;
        }

        #endregion

        #region Externs

        [DllImport("Kernel32.dll"), System.Security.SuppressUnmanagedCodeSecurity]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        [DllImport("kernel32.dll"), System.Security.SuppressUnmanagedCodeSecurity]
        private static extern void FillMemory(IntPtr destination, int len, byte val);

        #endregion


    }

}
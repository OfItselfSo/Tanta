using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

using MediaFoundation;
using MediaFoundation.Misc;
using MediaFoundation.Transform;

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

/// Most of this code is derived from the AsyncMFTBase sample which ships with the MF.Net dll. These have 
/// been placed in the public domain without copyright. The original copyright statement is below. The
/// changes made to incorporate it into the Tanta Library have largely been trivial or enhanced comments.

/// *****************************************************************************
/// Original Copyright Statement - Released to public domain
/// While the underlying library is covered by LGPL or BSD, this sample is released
/// as public domain.  It is distributed in the hope that it will be useful, but
/// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
/// or FITNESS FOR A PARTICULAR PURPOSE.
/// ******************************************************************************

namespace TantaCommon
{

    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// This class acts as a base for Asynchronous Transforms with one input and one output
    /// stream. 
    /// 
    /// This class does not exist in the C++ samples and was added by the implementors
    /// of MF.Net to the MF.Net samples in order to make it easy to implement Transforms.
    /// 
    /// This code automates pretty much everything that can be abstracted out leaving only a 
    /// few implementation specific routines to be implemented in the child class 
    /// (the actual transform) which is derived from this one
    /// 
    /// This code has largely been copied directly from the MFTBase class in the MFT sample
    /// code with a few (mostly minor) enhancements.
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>
    abstract public class TantaMFTBase_Async : TantaCOMObjBase, IMFTransform, IMFMediaEventGenerator, IMFShutdown, ICustomQueryInterface
    {
        /// Synchronization object used by public entry points
        /// to prevent multiple methods from being invoked at
        /// once.  Some work (for example parameter validation)
        /// is done before invoking this lock.
        private object m_TransformLockObject;

        /// Input type set by SetInputType. Can be null if not set yet or 
        /// if reset by SetInputType(null).
        private IMFMediaType m_InputMediaType;

        /// Output type set by SetOutputType.Can be null if not set yet or if reset by
        /// SetOutputType(null).
        private IMFMediaType m_OutputMediaType;

        /// Attributes returned to IMFTransform::GetAttributes. The MF_TRANSFORM_ASYNC_UNLOCK is stored here.
        private readonly IMFAttributes m_TransformAttributeCollection;

        /// Queue of output samples to be returned to IMFTransform::ProcessOutput.
        /// Derived classes should populate this by using OutputSample().
        private readonly ConcurrentQueue<IMFSample> m_OutputSampleQueue;

        /// MF Helper class to support IMFMediaEventGenerator.
        private readonly IMFMediaEventQueue m_TransformEventQueue;

        /// Used to control how many samples are active at one time. Defaults 
        /// to m_ThreadCount * 2.  See SendNeedEvent for details.
        private int m_MaxPermittedThreads;

        /// The number of processing threads. Set by derived 
        /// class' constructor.  Must be at least 1.</remarks>
        private readonly int m_ThreadCount;

        /// Used by dynamic format change to perform synchronization.
        private int m_ThreadsBlocked;

        /// Used by dynamic format change to perform synchronization.
        private SemaphoreSlim m_FormatEventSemaphore;

        /// A count of how many NeedInput messages have been sent that haven't
        /// yet been satisfied.  Also used by SendNeedEvent.
        private int m_UnsatisfiedNeedInputMessageCount;

        /// Used to shut down the processing threads.
        private int m_ShutdownThreads;

        /// Have we been shutdown?
        private bool m_Shutdown;

        /// Has a stream been started that has not yet been ended? Streams 
        /// are not guaranteed to receive end stream  notices.
        private bool m_StreamIsActive;

        /// Has the MF_TRANSFORM_ASYNC_UNLOCK been set?
        private bool m_Unlocked;

        /// Should the next sample be marked as Discontinuity?
        private bool m_bDiscontinuity;

        /// Has Command Flush been received? Cleared by next call to StartStream.
        private volatile bool m_Flushing;

        /// Queue of events to be processed by threads. Derived classes shouldn't touch this.
        private readonly TantaMFTAsyncBlockingQueue m_InputSampleQueue;

        /// Used to order access to m_OutputSampleQueue.
        private readonly ManualResetEventSlim[] m_ThreadSemaphoreArray;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected TantaMFTBase_Async() : this(1)
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected TantaMFTBase_Async(int iThreads) : this(iThreads, iThreads * 2)
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected TantaMFTBase_Async(int iThreads, int iThreshold)
        {
            HResult hr;

            DebugMessage("TantaMFTBase_Async Constructor");

            if (iThreads < 1) throw new Exception("TantaMFTBase_Async, Invalid thread count specified");

            // init our state variables
            m_ThreadCount = iThreads;
            m_MaxPermittedThreads = iThreshold;

            m_UnsatisfiedNeedInputMessageCount = -1;
            m_ShutdownThreads = 0;
            m_ThreadsBlocked = 0;

            m_StreamIsActive = false;
            m_Shutdown = false;
            m_bDiscontinuity = false;
            m_Unlocked = false;
            m_Flushing = false;

            m_InputMediaType = null;
            m_OutputMediaType = null;

            m_InputSampleQueue = new TantaMFTAsyncBlockingQueue();
            m_TransformLockObject = new object();
            m_ThreadSemaphoreArray = new ManualResetEventSlim[iThreads];
            m_OutputSampleQueue = new ConcurrentQueue<IMFSample>();
            m_FormatEventSemaphore = new SemaphoreSlim(0, m_ThreadCount);

            // Build the IMFAttributes container to use for IMFTransform::GetAttributes.
            hr = MFExtern.MFCreateAttributes(out m_TransformAttributeCollection, 3);
            if (hr != HResult.S_OK)
            {
                throw new Exception("TantaMFTBase_Async call to MFExtern.MFCreateAttributes failed. Err=" + hr.ToString());
            }

            // THis specifies that we are an asynchronous transform. For asynchronous MFTs, this 
            // attribute must be set to a nonzero value. For synchronous MFTs, this attribute 
            // is optional, but must be set to 0 if present.
            hr = m_TransformAttributeCollection.SetUINT32(MFAttributesClsid.MF_TRANSFORM_ASYNC, 1);
            if (hr != HResult.S_OK)
            {
                throw new Exception("TantaMFTBase_Async call to MFAttributesClsid.MF_TRANSFORM_ASYNC failed. Err=" + hr.ToString());
            }

            // Asynchronous MFTs are not compatible with earlier versions of Microsoft Media Foundation. To prevent
            // existing applications from accidentally using an asynchronous MFT, this attribute must be set to a 
            // nonzero value before an asynchronous MFT can be used. The Media Foundation pipeline automatically 
            // sets the attribute, so that most applications do not need to use this attribute. We set it to zero here
            // so that if an application outside of the Media Foundation pipeline tries to use this transform it 
            // must be able to set this value itself
            hr = m_TransformAttributeCollection.SetUINT32(MFAttributesClsid.MF_TRANSFORM_ASYNC_UNLOCK, 0);
            if (hr != HResult.S_OK)
            {
                throw new Exception("TantaMFTBase_Async call to MFAttributesClsid.MF_TRANSFORM_ASYNC_UNLOCKs failed. Err=" + hr.ToString());
            }

            // Indicate that we support dynamic format changes. This base class automatically handles this 
            // but at a considerable cost in increased complexity
            hr = m_TransformAttributeCollection.SetUINT32(MFAttributesClsid.MFT_SUPPORT_DYNAMIC_FORMAT_CHANGE, 1);
            if (hr != HResult.S_OK)
            {
                throw new Exception("TantaMFTBase_Async call to MFAttributesClsid.MFT_SUPPORT_DYNAMIC_FORMAT_CHANGE failed. Err=" + hr.ToString());
            }

            // Used for IMFMediaEventGenerator support.  All 
            // METransformNeedInput, METransformHaveOutput, etc go through here.
            hr = MFExtern.MFCreateEventQueue(out m_TransformEventQueue);
            if (hr != HResult.S_OK)
            {
                throw new Exception("TantaMFTBase_Async call to MFExtern.MFCreateEventQueue failed. Err=" + hr.ToString());
            }

            // Start the processing threads.
            for (int x = 0; x < iThreads; x++)
            {
                // The first event is 'set', the rest are not.
                m_ThreadSemaphoreArray[x] = new ManualResetEventSlim(x == 0, 1000);

                Thread ProcessThread = new Thread(new ThreadStart(ProcessingThread));

                // Background threads can be terminated by .Net without waiting for
                // them to shutdown.
                ProcessThread.IsBackground = true;
#if DEBUG
                ProcessThread.Name = "Async MFT Processing Thread #" + ProcessThread.ManagedThreadId.ToString() + " for " + this.GetType().Name;
#endif
                ProcessThread.Start();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Destructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        ~TantaMFTBase_Async()
        {
            MyShutdown();
        }


        // ########################################################################
        // ##### Overrides, child classes must implement these
        // ########################################################################
        #region Abstracts

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Report whether a proposed input type is accepted by the MFT. For 
        /// async MFTs, you should NOT check to see if a 
        /// proposed input type is compatible with the currently-set output 
        /// type, just whether the specified type is a valid input type.</remarks>
        /// <returns>S_Ok if the type is valid or MF_E_INVALIDTYPE.
        /// </summary>
        /// <param name="pmt">The type to check.  Should never be null (which are always valid).</param>
        abstract protected HResult OnCheckInputType(IMFMediaType pmt);

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Return settings to describe input stream
        /// (see IMFTransform::GetInputStreamInfo).
        /// </summary>
        /// <param name="pStreamInfo">The struct where the parameters get set.</param>
        abstract protected void OnGetInputStreamInfo(ref MFTInputStreamInfo pStreamInfo);

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Return settings to describe output stream
        /// (see IMFTransform::GetOutputStreamInfo).
        /// </summary>
        /// <param name="pStreamInfo">The struct where the parameters get set.</param>
        abstract protected void OnGetOutputStreamInfo(ref MFTOutputStreamInfo pStreamInfo);

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The routine called by processing threads that actually performs the transform.
        ///
        /// Takes the input sample and process it.
        /// Depending on what you set in On*StreamInfo, you can either perform
        /// in-place processing by modifying the input sample, or create a new
        /// IMFSample and fully populate it from the input (including attributes).
        /// When output sample(s) are complete, pass them to OutputSample().  A
        /// single input can generate zero or more outputs.  Use the same value
        /// of InputMessageNumber for all calls to OutputSample.
        /// If the output sample is not the input sample, call SafeRelease
        /// on the input sample.
        /// </summary>
        /// <param name="sample">The input sample to process into output.</param>
        /// <param name="Discontinuity">Whether or not the sample should be marked as MFSampleExtension_Discontinuity.</param>
        /// <param name="InputMessageNumber">The value to pass to OutputSample.  This is NOT the frame number.</param>
        abstract protected void OnProcessSample(IMFSample pInputSample, bool Discontinuity, int InputMessageNumber);

        #endregion

        #region Virtuals

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Report whether a proposed output type is accepted by the MFT.
        /// 
        /// The default behavior is to assume that the input type 
        /// must be set before the output type, and that the proposed output 
        /// type must exactly equal the value returned from the virtual
        /// CreateOutputFromInput.  Override as  necessary.
        /// </summary>
        /// <param name="pmt">The type to check.  Should never be null (which are always valid).</param>
        /// <returns>S_Ok if the type is valid or MF_E_INVALIDTYPE.</returns>
        virtual protected HResult OnCheckOutputType(IMFMediaType pmt)
        {
            HResult hr = HResult.S_OK;

            // If the input type is set, see if they match.
            if (m_InputMediaType != null)
            {
                IMFMediaType pCheck = CreateOutputFromInput();

                try
                {
                    hr = TantaWMFUtils.IsMediaTypeIdentical(pmt, pCheck);
                }
                finally
                {
                    SafeRelease(pCheck);
                }
            }
            else
            {
                // Input type is not set.
                hr = HResult.MF_E_TRANSFORM_TYPE_NOT_SET;
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Override to get notified when the Input Type is actually being set.
        /// 
        /// The new type is in InputType, and can be null.  Input types 
        /// can be changed while the stream is running.  See
        /// the comments at the top of TantaMFTBase_Async.cs.
        /// </summary>
        virtual protected void OnSetInputType()
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Override to get notified when the Output Type is actually being set.
        /// 
        /// The new type is in OutputType, and can be null
        /// </summary>
        virtual protected void OnSetOutputType()
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Override to allow the client to retrieve the MFT's list of supported input types.
        /// 
        /// This method is virtual since it is (sort of) optional.
        /// For example, if a client *knows* what types the MFT supports, it can
        /// just set it.  Not all clients support MFTs that won't enum types.
        /// </summary>
        /// <param name="dwTypeIndex">The (zero-based) index of the type.</param>
        /// <param name="pInputType">The input type supported by the MFT.</param>
        /// <returns>S_Ok unless error.</returns>
        virtual protected HResult OnEnumInputTypes(int dwTypeIndex, out IMFMediaType pInputType)
        {
            pInputType = null;
            return HResult.E_NOTIMPL;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Override to allow the client to retrieve the MFT's list of supported Output Types.
        /// 
        /// By default, assume the input type must be set first, and 
        /// that the output type is the single entry returned from the virtual 
        /// CreateOutputFromInput.  Override as needed.
        /// 
        /// </summary>
        /// <param name="dwTypeIndex">The (zero-based) index of the type.</param>
        /// <param name="pOutputType">The output type supported by the MFT.</param>
        /// <returns>S_Ok or MFError.MF_E_NO_MORE_TYPES.</returns>
        virtual protected HResult OnEnumOutputTypes(int dwTypeIndex, out IMFMediaType pOutputType)
        {
            HResult hr = HResult.S_OK;

            // If the input type is specified, the output type must be the same.
            if (m_InputMediaType != null)
            {
                // If the input type is specified, there can be only one output type.
                if (dwTypeIndex == 0)
                {
                    pOutputType = CreateOutputFromInput();
                }
                else
                {
                    pOutputType = null;
                    hr = HResult.MF_E_NO_MORE_TYPES;
                }
            }
            else
            {
                pOutputType = null;
                hr = HResult.MF_E_TRANSFORM_TYPE_NOT_SET;
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create a single output type from an input type.
        /// 
        /// In many cases, there is only one possible output type, and it is 
        /// either identical to, or a direct consequence of the input type.  
        /// Provided with that single output type, OnCheckOutputType and 
        /// OnEnumOutputTypes can be written generically, so they don't have 
        /// to be implemented by the derived class.  At worst, this one method 
        /// may need to be overridden.
        /// 
        /// </summary>
        /// <returns>By default, a clone of the input type.  Can be overridden.</returns>
        virtual protected IMFMediaType CreateOutputFromInput()
        {
            return TantaWMFUtils.CloneMediaType(m_InputMediaType);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called to allow any trailing output samples to be sent (think: Echo).
        /// 
        /// Call OutputSample() with any trailing samples. Use the same
        /// value of InputMessageNumber for all calls to OutputSample.
        /// 
        /// </summary>
        /// <param name="InputMessageNumber">Parameter to pass to OutputSample().</param>
        virtual protected void OnDrain(int InputMessageNumber)
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called in response to MFTMessageType.NotifyStartOfStream.
        /// 
        /// This is called after the message, but before any samples
        /// arrive.  All input and output types should be set before this
        /// point.  Note that there is no guarantee that OnEndStream
        /// will be called before a second call to OnStartStream.
        /// 
        /// </summary>
        virtual protected void OnStartStream()
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when streaming is ending. 
        /// 
        /// A good place to clean up resources at the end of a stream.
        /// Before any more samples are sent, OnStartStream will get
        /// called again.  Note that this routine may never get called,
        /// or may get called more than once.  This should discard any
        /// pending drain information.
        /// 
        /// </summary>
        virtual protected void OnEndStream()
        {
        }

        #endregion

        // ########################################################################
        // ##### IMFTransform methods
        // ########################################################################

        #region IMFTransform methods

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the minimum and maximum number of input and output streams for 
        /// this transform
        /// </summary>
        /// <param name="pdwInputMaximum">Receives the maximum number of input streams. If there is no maximum, receives the value MFT_STREAMS_UNLIMITED</param>
        /// <param name="pdwInputMinimum">Receives the minimum number of input streams</param>
        /// <param name="pdwOutputMaximum">Receives the maximum number of output streams. If there is no maximum, receives the value MFT_STREAMS_UNLIMITED. </param>
        /// <param name="pdwOutputMinimum">Receives the minimum number of output streams</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetStreamLimits(
            MFInt pdwInputMinimum,
            MFInt pdwInputMaximum,
            MFInt pdwOutputMinimum,
            MFInt pdwOutputMaximum
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("GetStreamLimits");

                CheckUnlocked();

                // This template requires a fixed number of input and output
                // streams (1 for each).

                lock (m_TransformLockObject)
                {
                    // Fixed stream limits.
                    if (pdwInputMinimum != null)
                    {
                        pdwInputMinimum.Assign(1);
                    }
                    if (pdwInputMaximum != null)
                    {
                        pdwInputMaximum.Assign(1);
                    }
                    if (pdwOutputMinimum != null)
                    {
                        pdwOutputMinimum.Assign(1);
                    }
                    if (pdwOutputMaximum != null)
                    {
                        pdwOutputMaximum.Assign(1);
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the current number of input and output streams on this transform.  
        /// The number of streams includes unselected streams — that is, 
        /// streams with no media type or a NULL media type.
        ///
        /// </summary>
        /// <param name="pcInputStreams">Receives the number of input streams</param>
        /// <param name="pcOutputStreams">Receives the number of output streams</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetStreamCount(
            MFInt pcInputStreams,
            MFInt pcOutputStreams
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                // DebugMessage("GetStreamCount"); Not interesting

                // Do not check CheckUnlocked (per spec)

                lock (m_TransformLockObject)
                {
                    // This template requires a fixed number of input and output
                    // streams (1 for each).
                    if (pcInputStreams != null)
                    {
                        pcInputStreams.Assign(1);
                    }

                    if (pcOutputStreams != null)
                    {
                        pcOutputStreams.Assign(1);
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return hr; // CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the stream identifiers for the input and output streams on this 
        /// transform
        ///
        /// </summary>
        /// <param name="dwInputIDArraySize">Number of elements in the pdwInputIDs array</param>
        /// <param name="dwOutputIDArraySize">Number of elements in the pdwOutputIDs array</param>
        /// <param name="pdwInputIDs">Pointer to an array allocated by the caller. The method fills 
        /// the array with the input stream identifiers. The array size must be at least equal to the number of input streams. 
        /// To get the number of input streams, call IMFTransform::GetStreamCount</param>
        /// <param name="pdwOutputIDs">Pointer to an array allocated by the caller. The method fills
        /// the array with the output stream identifiers. The array size must be at least equal to 
        /// the number of output streams. To get the number of output streams, call GetStreamCount. </param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetStreamIDs(
            int dwInputIDArraySize,
            int[] pdwInputIDs,
            int dwOutputIDArraySize,
            int[] pdwOutputIDs
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                // DebugMessage("GetStreamIDs"); not interesting

                // While the spec says this should be checked, IMFMediaEngine
                // apparently doesn't follow the spec.
                //CheckUnlocked();

                lock (m_TransformLockObject)
                {
                    // Since our stream counts are fixed, we don't need
                    // to implement this method.  As a result, our input
                    // and output streams are always #0.
                    hr = HResult.E_NOTIMPL;
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return hr; // CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the buffer requirements and other information for an input stream on 
        /// this transform
        ///
        /// </summary>
        /// <param name="dwInputStreamID">Input stream identifier. To get the list of stream 
        /// identifiers, call IMFTransform::GetStreamIDs</param>
        /// <param name="pStreamInfo">Pointer to an MFT_INPUT_STREAM_INFO structure. 
        /// The method fills the structure with information about the input stream. </param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetInputStreamInfo(
            int dwInputStreamID,
            out MFTInputStreamInfo pStreamInfo
        )
        {
            // Overwrites everything with zeros.
            pStreamInfo = new MFTInputStreamInfo();

            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("GetInputStreamInfo");

                CheckUnlocked();
                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    // The implementation can override the defaults,
                    // and should set cbSize
                    OnGetInputStreamInfo(ref pStreamInfo);
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the buffer requirements and other information for an output stream on
        /// this transform.
        ///
        /// </summary>
        /// <param name="dwOutputStreamID">Output stream identifier. </param>
        /// <param name="pStreamInfo">Pointer to an MFT_OUTPUT_STREAM_INFO structure. 
        /// The method fills the structure with information about the output stream. </param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetOutputStreamInfo(
            int dwOutputStreamID,
            out MFTOutputStreamInfo pStreamInfo
        )
        {
            // Overwrites everything with zeros.
            pStreamInfo = new MFTOutputStreamInfo();

            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("GetOutputStreamInfo");

                CheckUnlocked();
                CheckValidStream(dwOutputStreamID);

                lock (m_TransformLockObject)
                {
                    // The implementation can override the defaults,
                    // and should set cbSize.
                    OnGetOutputStreamInfo(ref pStreamInfo);
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the global attribute store for this Transform 
        ///
        /// </summary>
        /// <param name="pAttributes">Receives a pointer to the IMFAttributes interface. 
        /// The caller must release the interface.</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetAttributes(out IMFAttributes pAttributes)
        {
            pAttributes = null;

            HResult hr = HResult.S_OK;

            try
            {
                // DebugMessage("GetAttributes"); not interesting

                // Do not check CheckUnlocked (per spec)

                lock (m_TransformLockObject)
                {
                    // Using GetUniqueRCW means the caller can do
                    // ReleaseComObject without trashing our copy.  We *don't*
                    // want to return a clone because we *do* want them to be
                    // able to change our attributes.
                    pAttributes = TantaWMFUtils.GetUniqueRCW(m_TransformAttributeCollection) as IMFAttributes;
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return hr; // CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the attribute store for an input stream on this Transform. 
        ///
        /// </summary>
        /// <param name="dwInputStreamID">Input stream identifier.</param>
        /// <param name="pAttributes">Receives a pointer to the IMFAttributes interface. 
        /// The caller must release the interface.</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetInputStreamAttributes(
            int dwInputStreamID,
            out IMFAttributes pAttributes
        )
        {
            pAttributes = null;

            HResult hr = HResult.S_OK;

            try
            {
                // DebugMessage("GetInputStreamAttributes"); not interesting

                CheckUnlocked();
                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    hr = HResult.E_NOTIMPL;
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return hr; // CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the attribute store for an output stream on this Transform. 
        ///
        /// </summary>
        /// <param name="dwOutputStreamID">Output stream identifier.</param>
        /// <param name="pAttributes">Receives a pointer to the IMFAttributes interface. T
        /// he caller must release the interface.</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetOutputStreamAttributes(
            int dwOutputStreamID,
            out IMFAttributes pAttributes
        )
        {
            pAttributes = null;

            HResult hr = HResult.S_OK;

            try
            {
                // DebugMessage("GetOutputStreamAttributes"); not interesting

                CheckUnlocked();
                CheckValidStream(dwOutputStreamID);

                lock (m_TransformLockObject)
                {
                    hr = HResult.E_NOTIMPL;
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return hr; // CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Removes an input stream from this Transform .  
        ///
        /// </summary>
        /// <param name="dwInputStreamID">Input stream identifier.</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult DeleteInputStream(
            int dwStreamID
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("DeleteInputStream");

                CheckUnlocked();
                CheckValidStream(dwStreamID);

                lock (m_TransformLockObject)
                {
                    // Removing streams not supported.
                    hr = HResult.E_NOTIMPL;
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Adds one or more new input streams to this Transform .  
        ///
        /// </summary>
        /// <param name="adwStreamIDs">Array of stream identifiers. The new stream 
        /// identifiers must not match any existing input streams</param>
        /// <param name="cStreams">Number of streams to add</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult AddInputStreams(
            int cStreams,
            int[] adwStreamIDs
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("AddInputStreams");

                CheckUnlocked();

                lock (m_TransformLockObject)
                {
                    // Adding streams not supported.
                    hr = HResult.E_NOTIMPL;
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets an available media type for an input stream on this Transform.
        ///
        /// </summary>
        /// <param name="dwInputStreamID">Input stream identifier. To get the list 
        /// of stream identifiers, call IMFTransform::GetStreamIDs.</param>
        /// <param name="dwTypeIndex">Index of the media type to retrieve. Media 
        /// types are indexed from zero and returned in approximate order of preference.</param>
        /// <param name="ppType">Receives a pointer to the IMFMediaType interface</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetInputAvailableType(
            int dwInputStreamID,
            int dwTypeIndex, // 0-based
            out IMFMediaType ppType
        )
        {
            ppType = null;

            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage(string.Format("GetInputAvailableType (stream = {0}, type index = {1})", dwInputStreamID, dwTypeIndex));

                // While the spec says this should be checked, IMFMediaEngine
                // apparently doesn't follow the spec.
                //CheckUnlocked();

                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    // Get the input media type from the derived class.
                    // No need to pass dwInputStreamID, since it must
                    // always be zero.
                    hr = OnEnumInputTypes(dwTypeIndex, out ppType);
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets an available media type for an output stream on this Transform.
        ///
        /// </summary>
        /// <param name="dwOutputStreamID">Output stream identifier. To get the 
        /// list of stream identifiers, call IMFTransform::GetStreamIDs.</param>
        /// <param name="dwTypeOutdex">Outdex of the media type to retrieve. 
        /// Media types are outdexed from zero and returned out approximate order of preference.</param>
        /// <param name="ppType">Receives a pooutter to the IMFMediaType interface</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetOutputAvailableType(
            int dwOutputStreamID,
            int dwTypeIndex, // 0-based
            out IMFMediaType ppType
        )
        {
            ppType = null;

            HResult hr = HResult.S_OK;

            try
            {
                // DebugMessage((string.Format("GetOutputAvailableType (stream = {0}, type index = {1})", dwOutputStreamID, dwTypeIndex));

                CheckUnlocked();
                CheckValidStream(dwOutputStreamID);

                lock (m_TransformLockObject)
                {
                    // Get the output media type from the derived class.
                    // No need to pass dwOutputStreamID, since it must
                    // always be zero.
                    hr = OnEnumOutputTypes(dwTypeIndex, out ppType);
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets, tests, or clears the media type for an input stream on this Transform.
        ///
        /// </summary>
        /// <param name="dwFlags">Zero or more flags from the _MFT_SET_TYPE_FLAGS enumeration.</param>
        /// <param name="dwInputStreamID">Input stream identifier. To get the list of 
        /// stream identifiers, call IMFTransform::GetStreamIDs.</param>
        /// <param name="pType">Pointer to the IMFMediaType interface, or NULL. </param>
        /// <returns>S_OK or other for fail</returns>
        public HResult SetInputType(
            int dwInputStreamID,
            IMFMediaType pType,
            MFTSetTypeFlags dwFlags
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("SetInputType");

                CheckUnlocked();
                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    // Asynchronous MFTs must allow formats to change while
                    // they are running.  However, it must still be a format
                    // that the MFT supports (ie OnCheckInputType).

                    // Allow input type to be cleared.
                    if (pType != null)
                    {
                        // Validate non-null types.
                        hr = OnCheckInputType(pType);
                    }
                    else
                    {
                        if (m_StreamIsActive)
                        {
                            // Can't set input type to null while actively streaming.
                            hr = HResult.MF_E_INVALIDMEDIATYPE;
                        }
                    }
                    if (Succeeded(hr))
                    {
                        // Does the caller want to set the type?  Or just test it?
                        bool bReallySet = ((dwFlags & MFTSetTypeFlags.TestOnly) == 0);

                        if (bReallySet)
                        {
                            // Make a copy of the IMFMediaType and queue
                            // it to the processing thread.  The type will
                            // get changed after any pending inputs have
                            // been processed.
                            IMFMediaType pTmp;

                            pTmp = TantaWMFUtils.CloneMediaType(pType);

                            // If we are streaming, we need to delay 
                            // changing the input type until all current 
                            // samples are processed.
                            if (!m_StreamIsActive)
                            {
                                MySetInput(pTmp);
                            }
                            else
                            {
                                EnqueueThreadMessage(pTmp);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }
            //finally
            {
                // While we *should* do this, we can't.  If the caller is c#, we
                // would destroy their copy.  Instead we have to leave this for
                // the GC.
                // SafeRelease(pType);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets, tests, or clears the media type for an output stream on this Transform.
        ///
        /// </summary>
        /// <param name="dwFlags">Zero or more flags from the _MFT_SET_TYPE_FLAGS enumeration.</param>
        /// <param name="dwOutputStreamID">Output stream identifier. To get the list of stream identifiers, call IMFTransform::GetStreamIDs.</param>
        /// <param name="pType">Pooutter to the IMFMediaType interface, or NULL. </param>
        /// <returns>S_OK or other for fail</returns>
        public HResult SetOutputType(
            int dwOutputStreamID,
            IMFMediaType pType,
            MFTSetTypeFlags dwFlags
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("SetOutputType");

                // By spec, we are supposed to check this for non-decoders.
                // But knowing what is and isn't a decoder isn't practical.
                // Since MS already breaks the spec for GetStreamIDs and
                // GetAvailableInputType, I'm going to break it here and
                // not check.  Correctly functioning clients don't care,
                // and incorrect ones will still get an error eventually.
                //CheckUnlocked();
                CheckValidStream(dwOutputStreamID);

                lock (m_TransformLockObject)
                {
                    if (pType != null)
                    {
                        // Validate the type.
                        hr = OnCheckOutputType(pType);
                    }

                    if (Succeeded(hr))
                    {
                        // Does the caller want us to set the type, or just test it?
                        bool bReallySet = ((dwFlags & MFTSetTypeFlags.TestOnly) == 0);

                        // Set the type, unless the caller was just testing.
                        if (bReallySet)
                        {
                            // Make our own copy of the type.
                            OutputType = TantaWMFUtils.CloneMediaType(pType);

                            // Notify the derived class
                            OnSetOutputType();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }
            //finally
            {
                // While we *should* do this, we can't.  If the caller is c#, we
                // would destroy their copy.  Instead we have to leave this for
                // the GC.
                // SafeRelease(pType);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the current media type for an input stream on this Transform. 
        ///
        /// </summary>
        /// <param name="dwInputStreamID">Input stream identifier. To get the 
        /// list of stream identifiers, call IMFTransform::GetStreamIDs. </param>
        /// <param name="ppType">Receives a pointer to the IMFMediaType 
        /// interface. The caller must release the interface.</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetInputCurrentType(
            int dwInputStreamID,
            out IMFMediaType ppType
        )
        {
            ppType = null;

            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("GetInputCurrentType");

                CheckUnlocked();
                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    if (m_InputMediaType != null)
                    {
                        ppType = TantaWMFUtils.CloneMediaType(m_InputMediaType);
                    }
                    else
                    {
                        // Type is not set
                        hr = HResult.MF_E_TRANSFORM_TYPE_NOT_SET;
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the current media type for an output stream on this Transform. 
        ///
        /// </summary>
        /// <param name="dwOutputStreamID">Output stream identifier. To get the 
        /// list of stream identifiers, call IMFTransform::GetStreamIDs. </param>
        /// <param name="ppType">Receives a pooutter to the IMFMediaType 
        /// interface. The caller must release the interface.</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetOutputCurrentType(
            int dwOutputStreamID,
            out IMFMediaType ppType
        )
        {
            ppType = null;

            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("GetOutputCurrentType");

                // By spec, we are supposed to check this for non-decoders.
                // But knowing what is and isn't a decoder isn't practical.
                // Since MS already breaks the spec for GetStreamIDs and
                // GetAvailableInputType, I'm going to break it here and
                // not check.  Correctly functioning clients don't care,
                // and incorrect ones will still get an error eventually.
                //CheckUnlocked();
                CheckValidStream(dwOutputStreamID);

                lock (m_TransformLockObject)
                {
                    if (m_OutputMediaType != null)
                    {
                        ppType = TantaWMFUtils.CloneMediaType(m_OutputMediaType);
                    }
                    else
                    {
                        // No output type set
                        hr = HResult.MF_E_TRANSFORM_TYPE_NOT_SET;
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Queries whether an input stream on this Transform can accept more data. 
        ///
        /// </summary>
        /// <param name="dwInputStreamID">Input stream identifier. To get the list 
        /// of stream identifiers, call IMFTransform::GetStreamIDs. </param>
        /// <param name="pdwFlags">Receives a member of the _MFT_INPUT_STATUS_FLAGS 
        /// enumeration, or zero. If the value is MFT_INPUT_STATUS_ACCEPT_DATA, 
        /// the stream specified in dwInputStreamID can accept more input data.</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetInputStatus(
            int dwInputStreamID,
            out MFTInputStatusFlags pdwFlags
        )
        {
            pdwFlags = MFTInputStatusFlags.None;

            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("GetInputStatus");

                CheckUnlocked();
                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    if (AllowInput())
                    {
                        pdwFlags = MFTInputStatusFlags.AcceptData;
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Queries whether the Transform (MFT) is ready to produce output data. 
        ///
        /// </summary>
        /// <param name="pdwFlags">Receives a member of the _MFT_OUTPUT_STATUS_FLAGS 
        /// enumeration, or zero. If the value is MFT_OUTPUT_STATUS_SAMPLE_READY, 
        /// the MFT can produce an output sample.</param>
        /// <returns>S_OK or other for fail</returns>
        public HResult GetOutputStatus(
            out MFTOutputStatusFlags pdwFlags
        )
        {
            pdwFlags = MFTOutputStatusFlags.None;

            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("GetOutputStatus");

                CheckUnlocked();

                lock (m_TransformLockObject)
                {
                    if (HasPendingOutput())
                    {
                        pdwFlags = MFTOutputStatusFlags.SampleReady;
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the range of time stamps the client needs for output.  
        ///
        /// </summary>
        /// <param name="hnsLowerBound">Specifies the earliest time stamp. 
        /// The Media Foundation transform (MFT) will accept input until it can produce an 
        /// output sample that begins at this time; or until it can produce a sample that ends at 
        /// this time or later. If there is no lower bound, use the value MFT_OUTPUT_BOUND_LOWER_UNBOUNDED. </param>
        /// <param name="hnsUpperBound">Specifies the latest time stamp. The MFT will not produce 
        /// an output sample with time stamps later than this time. If there is no upper bound, 
        /// use the value MFT_OUTPUT_BOUND_UPPER_UNBOUNDED. </param>
        /// <returns>S_OK or other for fail</returns>
        public HResult SetOutputBounds(
            long hnsLowerBound,
            long hnsUpperBound
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("SetOutputBounds");

                CheckUnlocked();

                lock (m_TransformLockObject)
                {
                    // Output bounds not supported
                    hr = HResult.E_NOTIMPL;
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sends an event to an input stream on this Transform.  
        ///
        /// </summary>
        /// <param name="dwInputStreamID">Input stream identifier. To get the 
        /// list of stream identifiers, call IMFTransform::GetStreamIDs. </param>
        /// <param name="pEvent">Pointer to the IMFMediaEvent interface of an 
        /// event object. </param>
        /// <returns>S_OK or other for fail</returns>
        public HResult ProcessEvent(
            int dwInputStreamID,
            IMFMediaEvent pEvent
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("ProcessEvent");

                CheckUnlocked();

                lock (m_TransformLockObject)
                {
                    // Events not supported.
                    hr = HResult.E_NOTIMPL;
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }
            //finally
            {
                // While we *should* do this, we can't.  If the caller is c#, we
                // would destroy their copy.  Instead we have to leave this for
                // the GC.
                // SafeRelease(pEvent);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sends a message to the Transform. 
        ///
        /// </summary>
        /// <param name="eMessage">The message to send, specified as a 
        /// member of the MFT_MESSAGE_TYPE enumeration.</param>
        /// <param name="ulParam">Message parameter. The meaning of this 
        /// parameter depends on the message type. </param>
        /// <returns>S_OK or other for fail</returns>
        public HResult ProcessMessage(
            MFTMessageType eMessage,
            IntPtr ulParam
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("ProcessMessage " + eMessage.ToString());

                CheckUnlocked();

                lock (m_TransformLockObject)
                {
                    switch (eMessage)
                    {
                        case MFTMessageType.NotifyStartOfStream:
                            // Mandatory message for async MFTs.  Note
                            // that the corresponding EndOfStream is
                            // optional.

                            // Start new stream.
                            MyStartStream();

                            break;

                        case MFTMessageType.CommandFlush:
                            // Tell the processing thread to skip samples
                            // instead of processing them.  Flush stays
                            // in effect until next NotifyStartOfStream.
                            EnqueueThreadMessage(TantaMFTAsyncMessageTypeEnum.Flush);

                            // Flushes should happen very quickly, but we can't
                            // risk returning until it is done.
                            while (Thread.VolatileRead(ref m_UnsatisfiedNeedInputMessageCount) != -1)
                                Thread.Sleep(5);

                            // No more processing until next NotifyStartOfStream
                            MyEndStream();

                            break;

                        case MFTMessageType.CommandDrain:
                            // Stop sending NeedInputs.  Drain stays
                            // in effect until next NotifyStartOfStream.
                            m_UnsatisfiedNeedInputMessageCount = -1; // Stop sending NeedInput until new stream

                            // The processing thread will signal the drain complete.
                            EnqueueThreadMessage(TantaMFTAsyncMessageTypeEnum.Drain);

                            // No more processing until next NotifyStartOfStream

                            // Called by processing thread when the drain is complete.
                            //MyEndStream();

                            break;

                        case MFTMessageType.CommandMarker:
                            // Tell the processing thread to send the message when
                            // all inputs received before this message have been 
                            // turned into outputs.  Note that unlike drain, 
                            // processing does NOT stop at that point.  We just 
                            // post the message and keep going.

                            EnqueueThreadMessage(ulParam);
                            break;

                        case MFTMessageType.NotifyEndOfStream:
                            // Optional message.
                            m_UnsatisfiedNeedInputMessageCount = -1;

                            // For non-Async MFTs, the client could send more
                            // ProcessInput calls after this message.  Not an
                            // option for Async, since we would have to send
                            // some NeedInput messages.
                            MyEndStream();
                            break;

                        case MFTMessageType.NotifyBeginStreaming:
                            // Optional message.
                            break;

                        case MFTMessageType.NotifyEndStreaming:
                            // Optional message.
                            break;

                        case MFTMessageType.SetD3DManager:
                            // The pipeline should never send this message unless the MFT
                            // has the MF_SA_D3D_AWARE attribute set to TRUE. However, if we
                            // do get this message, it's invalid and we don't implement it.
                            hr = HResult.E_NOTIMPL;
                            break;

                        default:
#if DEBUG
                            Debug.Fail("Unknown message type: " + eMessage.ToString());
#endif
                            // The spec doesn't say to do this, but I do it anyway.
                            hr = HResult.S_FALSE;
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Delivers data to an input stream on this Transform. Since we usually do 
        /// the actual work elsewhere we just cache the input buffer. 
        ///
        /// </summary>
        /// <param name="dwFlags">Reserved. Must be zero. </param>
        /// <param name="dwInputStreamID">Input stream identifier. To get the 
        /// list of stream identifiers, call IMFTransform::GetStreamIDs.</param>
        /// <param name="pSample">Pointer to the IMFSample interface of the input 
        /// sample. The sample must contain at least one media buffer that contains 
        /// valid input data. </param>
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public HResult ProcessInput(
            int dwInputStreamID,
            IMFSample pSample,
            int dwFlags
        )
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("ProcessInput");

                CheckUnlocked();
                CheckValidStream(dwInputStreamID);

                if (pSample != null)
                {
                    if (dwFlags != 0)
                    {
                        // Invalid flags
                        hr = HResult.E_INVALIDARG;
                    }
                }
                else
                {
                    // No input sample provided
                    hr = HResult.E_POINTER;
                }

                if (Succeeded(hr))
                {
                    lock (m_TransformLockObject)
                    {
                        hr = AllTypesSet();
                        if (Succeeded(hr))
                        {
                            // Unless this call is in response to a
                            // METransformNeedInput message, reject it.
                            if (AllowInput())
                            {
                                m_UnsatisfiedNeedInputMessageCount--;
                                EnqueueThreadMessage(pSample);
                            }
                            else
                            {
                                hr = HResult.MF_E_NOTACCEPTING;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Generates output from the current input data.
        /// 
        ///
        /// </summary>
        /// <param name="dwFlags">Bitwise OR of zero or more flags from the _MFT_PROCESS_OUTPUT_FLAGS enumeration. </param>
        /// <param name="cOutputBufferCount">Number of elements in the pOutputSamples array. The value must be at least 1. </param>
        /// <param name="pdwStatus">Receives a bitwise OR of zero or more flags from the _MFT_PROCESS_OUTPUT_STATUS enumeration.</param>
        /// <param name="outputSamplesArray">An array of MFT_OUTPUT_DATA_BUFFER structures, allocated by the caller. The MFT uses 
        /// this array to return output data to the caller. One for each stream, however This base class is only
        /// designed for a single stream in and a single stream out. Thus we only
        /// process the first entry in the outputSamplesArray array</param>
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public HResult ProcessOutput(
            MFTProcessOutputFlags dwFlags,
            int cOutputBufferCount,
            MFTOutputDataBuffer[] pOutputSamples, // one per stream
            out ProcessOutputStatus pdwStatus
        )
        {
            pdwStatus = ProcessOutputStatus.None;

            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("ProcessOutput");

                CheckUnlocked();

                // Check input parameters.

                if (dwFlags != MFTProcessOutputFlags.None || cOutputBufferCount != 1)
                {
                    hr = HResult.E_INVALIDARG;
                }

                if (Succeeded(hr) && pOutputSamples == null)
                {
                    hr = HResult.E_POINTER;
                }

                // pOutputSamples[0].pSample may be null or not depending
                // on how the derived set MFTOutputStreamInfoFlags.  Do the
                // check in MyProcessOutput.

                if (Succeeded(hr))
                {
                    lock (m_TransformLockObject)
                    {
                        hr = AllTypesSet();
                        if (Succeeded(hr))
                        {
                            // Unless this call is in response to a
                            // METransformHaveOutput message, reject it.
                            if (HasPendingOutput())
                            {
                                hr = MyProcessOutput(ref pOutputSamples[0]);
                            }
                            else
                            {
                                hr = HResult.E_UNEXPECTED;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        #endregion

        // ########################################################################
        // ##### IMFMediaEventGenerator methods
        // ########################################################################

        #region IMFMediaEventGenerator methods

        // These methods are just wrappers around m_TransformEventQueue.
        // When we want to send an event back to the client, we just call
        // m_TransformEventQueue.QueueEvent, and let m_TransformEventQueue handle the plumbing.


        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Begins an asynchronous request for the next event in the queue.
        ///
        /// </summary>
        /// <param name="o">Pointer to the IUnknown interface of a state object, 
        /// defined by the caller. This parameter can be NULL. You can use this 
        /// object to hold state information. The object is returned to the caller 
        /// when the callback is invoked.</param>
        /// <param name="pCallback">Pointer to the IMFAsyncCallback interface of a callback object. 
        /// The client must implement this interface.</param>
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public HResult BeginGetEvent(IMFAsyncCallback pCallback, object o)
        {
            HResult hr = HResult.S_OK;

            try
            {
                hr = m_TransformEventQueue.BeginGetEvent(pCallback, null);
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }
            //finally
            {
                // While we *should* do this, we can't.  If the caller is c#, we
                // would destroy their copy.  Instead we have to leave this for
                // the GC.
                //SafeRelease(pCallback);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Completes an asynchronous request for the next event in the queue.
        ///
        /// </summary>
        /// <param name="ppEvent">Pointer to the IMFAsyncResult interface. 
        /// Pass in the same pointer that your callback object received 
        /// in the Invoke method.</param>
        /// <param name="pResult">Receives a pointer to the IMFMediaEvent interface.
        /// The caller must release the interface.</param>
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public HResult EndGetEvent(IMFAsyncResult pResult, out IMFMediaEvent ppEvent)
        {
            ppEvent = null;
            HResult hr = HResult.S_OK;

            try
            {
                hr = m_TransformEventQueue.EndGetEvent(pResult, out ppEvent);
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }
            //finally
            {
                // While we *should* do this, we can't.  If the caller is c#, we
                // would destroy their copy.  Instead we have to leave this for
                // the GC.
                //SafeRelease(pResult);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Retrieves the next event in the queue. This method is synchronous.
        ///
        /// </summary>
        /// <param name="dwFlags">flags that specify if the function blocks or not</param>
        /// <param name="ppEvent">Receives a pointer to the IMFMediaEvent interface. 
        /// The caller must release the interface.</param>
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public HResult GetEvent(MFEventFlag dwFlags, out IMFMediaEvent ppEvent)
        {
            ppEvent = null;

            HResult hr = HResult.S_OK;

            try
            {
                hr = m_TransformEventQueue.GetEvent(dwFlags, out ppEvent);
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Puts a new event in the object's queue.
        ///
        /// </summary>
        /// <param name="guidExtendedType">The extended type. If the event does 
        /// not have an extended type it will be the value GUID_NULL. </param>
        /// <param name="hrStatus">A success or failure code indicating the status of the event.</param>
        /// <param name="met">Specifies the event type.</param>
        /// <param name="pvValue">ointer to a PROPVARIANT that contains the event value.</param>
        /// 
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public HResult QueueEvent(MediaEventType met, Guid guidExtendedType, HResult hrStatus, ConstPropVariant pvValue)
        {
            IMFMediaEvent pEvent;
            MFError throwonhr;
            HResult hr = HResult.S_OK;

            try
            {
                throwonhr = MFExtern.MFCreateMediaEvent(met, Guid.Empty, HResult.S_OK, null, out pEvent);

                MyQueueEvent(pEvent);
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        #endregion

        // ########################################################################
        // ##### IMFShutdown methods
        // ########################################################################

        #region IMFShutdown methods

        // the IMFShutdown interface is exposed by some Media Foundation objects 
        // that must be explicitly shut down. 

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Queries the status of an earlier call to the IMFShutdown::Shutdown method. 
        ///
        /// </summary>
        /// <param name="pStatus">Receives a member of the MFSHUTDOWN_STATUS enumeration. </param>
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public HResult GetShutdownStatus(out MFShutdownStatus pStatus)
        {
            HResult hr = HResult.S_OK;
            pStatus = 0; // Have to set it to something

            try
            {
                DebugMessage("GetShutdownStatus");

                lock (m_TransformLockObject)
                {
                    if (m_Shutdown)
                    {
                        // By spec, the shutdown must have completed during
                        // the call to Shutdown().
                        pStatus = MFShutdownStatus.Completed;
                    }
                    else
                    {
                        hr = HResult.MF_E_INVALIDREQUEST;
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Shuts down a Media Foundation object and releases all resources associated 
        /// with the object. 
        ///
        /// </summary>
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public HResult Shutdown()
        {
            HResult hr = HResult.S_OK;

            try
            {
                DebugMessage("Shutdown");

                MyShutdown();
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return CheckReturn(hr);
        }

        #endregion

        // ########################################################################
        // ##### ICustomQueryInterface methods
        // ########################################################################

        #region ICustomQueryInterface methods

        // The  ICustomQueryInterface enables developers to provide a custom, 
        // managed implementation of the IUnknown::QueryInterface(REFIID riid, 
        // void **ppvObject) method.

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Returns an interface according to a specified interface ID. 
        ///
        /// </summary>
        /// <param name="iid">The GUID of the requested interface.</param>
        /// <param name="ppv">A reference to the requested interface, when this method returns.</param>
        /// <returns>One of the enumeration values that indicates whether a 
        /// custom implementation of IUnknown::QueryInterface was used.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public CustomQueryInterfaceResult GetInterface(ref Guid iid, out IntPtr ppv)
        {
            ppv = IntPtr.Zero;

#if DEBUG
            if (iid != typeof(IMFTransform).GUID &&
                iid != typeof(IMFMediaEventGenerator).GUID &&
                iid != typeof(IMFShutdown).GUID &&
                iid != typeof(IPersist).GUID)
            {
                string sGuidString = iid.ToString("B");
                string sKeyName = string.Format("HKEY_CLASSES_ROOT\\Interface\\{0}", sGuidString);
                string sName = (string)Microsoft.Win32.Registry.GetValue(sKeyName, null, sGuidString);

                // DebugMessage(string.Format("Unhandled interface requested: {0}", sName));
            }
#endif

            return CustomQueryInterfaceResult.NotHandled;
        }

        #endregion

        // ########################################################################
        // ##### Private methods (Only expected to be used by template)
        // ########################################################################

        #region Private methods

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Retrieve a sample from m_OutputSampleQueue and process it into MFTOutputDataBuffer.
        /// </summary>
        /// <param name="pOutputSamples">The struct into which the output sample is written.</param>
        /// <returns>S_Ok or error.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private HResult MyProcessOutput(ref MFTOutputDataBuffer pOutputSamples)
        {
            HResult hr = HResult.S_OK;

            if (pOutputSamples.pSample == IntPtr.Zero)
            {
                IMFSample sample = GetOutputSample();

                if (sample != null)
                {
                    pOutputSamples.dwStatus = MFTOutputDataBufferFlags.None;
                    pOutputSamples.pSample = Marshal.GetIUnknownForObject(sample);

                    // There is some risk in using Marshal.ReleaseComObject here,
                    // since some other .Net component could have a pointer to this
                    // same object, and this would yank the RCW out from under
                    // them.  But we go thru a LOT of samples.
                    SafeRelease(sample);
                }
                else
                {
                    pOutputSamples.dwStatus = MFTOutputDataBufferFlags.FormatChange;

                    // A null entry in this queue is a request to change the 
                    // output type.

                    // There are two times our output type can change.
                    //
                    // 1) As part of a Dynamic format change initiated by 
                    // the client.
                    // 2) At the request of the derived class by sending 
                    // null to OutputSample.
                    //
                    // We need to clear the output type in both cases.  This
                    // ensures that new ProcessInputs won't succeed and 
                    // OnProcessSample won't get called until the new output 
                    // type is set.

                    OutputType = null;

                    // Note: OnSetOutputType can reset the output type.
                    OnSetOutputType();

                    hr = HResult.MF_E_TRANSFORM_STREAM_CHANGE;
                }
            }
            else
            {
                hr = HResult.E_INVALIDARG;
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Allow for reset between NotifyStartOfStream calls.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void MyStartStream()
        {
            MyEndStream();

            // In theory, we might have a Flush pending here that this
            // StartOfStream is going to reset.  However, there cannot 
            // be any samples pending in the message queue, since we
            // cleared the message queue as part of flush, and 
            // m_UnsatisfiedNeedInputMessageCount got set to -1, preventing any more
            // samples from having been accepted.  So it is safe to do 
            // this with no further checking:

            m_Flushing = false;

            m_StreamIsActive = true;
            m_UnsatisfiedNeedInputMessageCount = 0;

            OnStartStream();

            // Ask for some inputs.
            SendNeedEvent();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when a stream ends.  Probably.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void MyEndStream()
        {
            if (m_StreamIsActive)
            {
                m_StreamIsActive = false;

                OnEndStream();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Are inputs allowed at the current time?
        /// </summary>
        /// <returns></returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private bool AllowInput()
        {
            return m_UnsatisfiedNeedInputMessageCount > 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Check for valid stream number.
        /// </summary>
        /// <param name="dwStreamID"></param>
        /// <returns>S_Ok unless error.</returns>
        /// <remarks>Easy to do since the only valid value is zero.</remarks>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private static void CheckValidStream(int dwStreamID)
        {
            if (dwStreamID != 0)
            {
                throw new MFException(HResult.MF_E_INVALIDSTREAMNUMBER);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Ensure both input and output media types are set.
        /// </summary>
        /// <returns>S_Ok or MFError.MF_E_TRANSFORM_TYPE_NOT_SET.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private HResult AllTypesSet()
        {
            if (m_InputMediaType == null || m_OutputMediaType == null)
                return HResult.MF_E_TRANSFORM_TYPE_NOT_SET;

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Print out a debug line when hr doesn't equal S_Ok. This code shows 
        /// the calling routine and the error text.
        /// All the public interface methods use this to wrap their returns,
        /// which is helpful during debugging.
        /// </summary>
        /// <param name="hr">Value to check</param>
        /// <returns>The input value.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private static HResult CheckReturn(HResult hr)
        {
#if DEBUG
            if (hr != HResult.S_OK)
            {
                StackTrace st = new StackTrace();
                StackFrame sf = st.GetFrame(1);

                string sName = sf.GetMethod().Name;
                string sError = MFError.GetErrorText(hr);
                if (sError != null)
                    sError = sError.Trim();
                // DebugMessage(string.Format("{1} returning 0x{0:x} ({2})", hr, sName, sError));
            }
#endif
            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Do we have data for ProcessOutput?
        /// </summary>
        /// <returns></returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private bool HasPendingOutput()
        {
            return !m_OutputSampleQueue.IsEmpty;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Does the work of actually changing the input media type. Called 
        /// both directly from SetInputType and async
        /// from the processing threads (during dynamic format change).
        /// </summary>
        /// <param name="pType">Input media type (can be null).</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void MySetInput(IMFMediaType pType)
        {
            // At this point, any pending input samples with the old type 
            // have been processed.

            // pType is a clone of the IMFMediaType passed to SetInputType or null.

            IMFMediaType oldType = TantaWMFUtils.CloneMediaType(m_OutputMediaType);

            // Set the new type
            InputType = pType;

            // Inform the derived
            OnSetInputType();

            if (pType != null)
            {
                // Are we going to have to have ProcessOutput do a format 
                // change message?
                //
                // There are 2 cases where we might:
                //
                // 1) We have started streaming, and someone changed the 
                //    input type to something that is incompatible with the 
                //    output type.
                // 2) We haven't started streaming yet, but someone
                //    a) Set the input type.
                //    b) Set the output type.
                //    c) Set the input type again to something that was 
                //       incompatible with (b).
                //
                // It may seem a little redundant to trigger a format change 
                // message to ProcessOutput for #2 since streaming hasn't 
                // even started yet.  But we need some way to inform the 
                // client that the output type they set (b) is no longer 
                // valid.  And we can't simply reject the change to the input 
                // type, since that would break the whole concept of dynamic 
                // format changes.  #2 is an unlikely scenario, so I'm not 
                // going to worry too much about the performance impact of
                // this silly case.  NB: It can be avoided by calling 
                // SetOutputType(NULL) before doing (c).

                // If the old type was null, neither condition applies, no 
                // notification is required (the most common case for 
                // SetInputType).
                if (oldType != null)
                {
                    if (
                        // OnSetInputType changed the output type. Since the 
                        // oldtype wasn't null, we need to inform the client 
                        // that there's a new output type.
                        (Failed(TantaWMFUtils.IsMediaTypeIdentical(oldType, m_OutputMediaType))) ||

                        // The new input type is not compatible with current 
                        // output type.
                        (Failed(OnCheckOutputType(m_OutputMediaType)))
                       )
                    {
                        // After we have sent all the current entries in
                        // the output queue, re-negotiate the output type.
                        OutputSample(null, int.MinValue);
                    }
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called from various places to shutdown the MFT. There is no coming 
        /// back from this.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void MyShutdown()
        {
            if (!m_Shutdown)
            {
                lock (m_TransformLockObject)
                {
                    m_Shutdown = true;
                    m_Unlocked = false;

                    Thread.VolatileWrite(ref m_ShutdownThreads, m_ThreadCount);
                    for (int x = 0; x < m_ThreadCount; x++)
                    {
                        EnqueueThreadMessage(TantaMFTAsyncMessageTypeEnum.Shutdown);
                    }

                    MyEndStream();

                    // All attempts by the client to read events will
                    // start returning MF_E_SHUTDOWN.
                    m_TransformEventQueue.Shutdown();
                    //SafeRelease(m_TransformEventQueue);

                    // Flush the outputs
                    if (m_OutputSampleQueue != null)
                    {
                        IMFSample pSamp;
                        while (!m_OutputSampleQueue.IsEmpty)
                        {
                            if (m_OutputSampleQueue.TryDequeue(out pSamp))
                                SafeRelease(pSamp);
                        }
                    }

                    // We own all these objects, so SafeRelease should
                    // be, well, safe.

                    InputType = null;
                    OutputType = null;

                    SafeRelease(m_TransformAttributeCollection);
                    SafeRelease(m_TransformEventQueue);


                    GC.SuppressFinalize(this);
                }
                m_TransformLockObject = null;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Dequeue sample from m_OutputSampleQueue
        /// </summary>
        /// <returns>The sample.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private IMFSample GetOutputSample()
        {
            IMFSample sample;

            // There is no contention for dequeuing, but
            // a simultaneous enqueue could cause a failure here.
            while (!m_OutputSampleQueue.TryDequeue(out sample))
                ;

            // See if we need more input.
            SendNeedEvent();

            return sample;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Checks to see if the MFT has been unlocked. Also handles shutdown.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void CheckUnlocked()
        {
            if (!m_Shutdown)
            {
                // Shortcut making the call to GetUINT32.
                if (!m_Unlocked)
                {
                    int iVal;

                    HResult hr = m_TransformAttributeCollection.GetUINT32(MFAttributesClsid.MF_TRANSFORM_ASYNC_UNLOCK, out iVal);
                    if (Succeeded(hr) && iVal != 0)
                    {
                        DebugMessage("Unlocked!");
                        m_Unlocked = true;
                    }
                    else
                    {
                        throw new MFException(HResult.MF_E_TRANSFORM_ASYNC_LOCKED);
                    }
                }
            }
            else
            {
                throw new MFException(HResult.MF_E_SHUTDOWN);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Send a message to the client telling it output is ready.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void SendSampleReady()
        {
            MFError throwonhr;

            throwonhr = QueueEvent(MediaEventType.METransformHaveOutput, Guid.Empty, HResult.S_OK, null);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Send a message associated with a stream to the client. Used by 
        /// METransformNeedInput & METransformDrainComplete
        /// to populate MF_EVENT_MFT_INPUT_STREAM_ID and queue the message.
        /// </summary>
        /// <param name="met"></param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void SendStreamEvent(MediaEventType met)
        {
            MFError throwonhr;
            IMFMediaEvent pEvent;

            throwonhr = MFExtern.MFCreateMediaEvent(met, Guid.Empty, HResult.S_OK, null, out pEvent);
            throwonhr = pEvent.SetUINT32(MFAttributesClsid.MF_EVENT_MFT_INPUT_STREAM_ID, 0);

            MyQueueEvent(pEvent);

            //SafeRelease(pEvent);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Send message to client asking for more input.
        /// 
        /// m_UnsatisfiedNeedInputMessageCount = -1 indicates the stream is shutting down.
        /// This calc is tougher than you might think.  When I first wrote it, I assumed
        /// that the goal was "always keep the input threads busy."  But I ended up
        /// with hundreds of samples in the output queue.  So the new plan is, we
        /// add these things together.  If there are less than Threshold, ask for more input.
        /// - In the input queue
        /// - In the output queue
        /// - Requested from the client
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void SendNeedEvent()
        {
            if (m_UnsatisfiedNeedInputMessageCount >= 0)
            {
                int j = m_MaxPermittedThreads - m_UnsatisfiedNeedInputMessageCount - m_OutputSampleQueue.Count - m_InputSampleQueue.Count;

                while (j-- > 0)
                {
                    SendStreamEvent(MediaEventType.METransformNeedInput);

                    // Keep track of how many requests we have sent.
                    m_UnsatisfiedNeedInputMessageCount++;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Let a client know that a marker has been reached.
        /// </summary>
        /// <param name="ulParam">The parameter they sent along with the marker request.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void SendMarker(IntPtr ulParam)
        {
            IMFMediaEvent pEvent;
            MFError throwonhr;

            throwonhr = MFExtern.MFCreateMediaEvent(MediaEventType.METransformMarker, Guid.Empty, HResult.S_OK, null, out pEvent);

            // Send back the parameter they provided.
            throwonhr = pEvent.SetUINT64(MFAttributesClsid.MF_EVENT_MFT_CONTEXT, ulParam.ToInt64());

            MyQueueEvent(pEvent);
            //SafeRelease(pEvent);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Send a message back to the client.
        /// </summary>
        /// <param name="pEvent"></param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void MyQueueEvent(IMFMediaEvent pEvent)
        {
            MFError throwonhr;

            throwonhr = m_TransformEventQueue.QueueEvent(pEvent);
        }

        #endregion

        // ########################################################################
        // ##### ThreadStuff
        // ########################################################################

        #region ThreadStuff

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Queue an event to the Processing Thread. This enqueues a message of
        /// type TantaMFTAsyncMessageTypeEnum.Marker
        /// </summary>
        /// <param name="p">The intptr to place as the marker.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void EnqueueThreadMessage(IntPtr p)
        {
            TantaMFTAsyncMessageHolder mh = new TantaMFTAsyncMessageHolder(TantaMFTAsyncMessageTypeEnum.Marker);
            mh.ptr = p;
            m_InputSampleQueue.Enqueue(mh);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Queue an event to the Processing Thread. This enqueues a message of
        /// type TantaMFTAsyncMessageTypeEnum.Sample
        /// </summary>
        /// <param name="sample">The IMFSample to place on the queue.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void EnqueueThreadMessage(IMFSample sample)
        {
            TantaMFTAsyncMessageHolder mh = new TantaMFTAsyncMessageHolder(TantaMFTAsyncMessageTypeEnum.Sample);
            mh.sample = sample;
            mh.bDiscontinuity = m_bDiscontinuity;
            m_bDiscontinuity = false;

            m_InputSampleQueue.Enqueue(mh);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Queue an event to the Processing Thread. This enqueues a message of
        /// type TantaMFTAsyncMessageTypeEnum.Format
        /// </summary>
        /// <param name="dt">The IMFMediaType to place on the queue.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void EnqueueThreadMessage(IMFMediaType dt)
        {
            // All processing threads need to block
            for (int x = 0; x < m_ThreadCount; x++)
            {
                TantaMFTAsyncMessageHolder mh = new TantaMFTAsyncMessageHolder(TantaMFTAsyncMessageTypeEnum.Format);
                mh.dt = dt;
                m_InputSampleQueue.Enqueue(mh);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Queue an event to the Processing Thread. This enqueues a message of
        /// whichever type is input.
        /// </summary>
        /// <param name="mt">The TantaMFTAsyncMessageTypeEnum to place on the queue.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void EnqueueThreadMessage(TantaMFTAsyncMessageTypeEnum mt)
        {
            if (mt == TantaMFTAsyncMessageTypeEnum.Flush || mt == TantaMFTAsyncMessageTypeEnum.Shutdown)
            {
                m_Flushing = true;
            }
            m_InputSampleQueue.Enqueue(new TantaMFTAsyncMessageHolder(mt));
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Internal function that handles the processing of the incoming samples
        /// into the outgoing ones. There is a lot of admin associated with this
        /// it is recommended that you read the comments below.
        /// 
        /// This is a thread worker function! There can be multiple copies running
        /// at the same time.
        /// 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void ProcessingThread()
        {
            // NOTE: these are the original MF.Net comments!!!

            // Understanding this routine is key to understanding how this entire
            // template works.  Reading it should only be necessary if a) there is
            // a bug. b) You need to add new features. c) You are just that curious
            // about how this all works.
            //
            // I write this text both for future maintainers, and because writing
            // this focuses my thinking.  I make bold, assertive statements here
            // about how things work, then check the code to make sure they are
            // true.
            //
            // In general, everything that the caller of the MFT (aka the client)
            // does is treated as an event.  All the interesting IMFTransform &
            // IMFShutdown calls are added as events to the m_InputSampleQueue queue (see
            // TantaMFTAsyncMessageTypeEnum & TantaMFTAsyncMessageHolder).  So while m_InputSampleQueue contains samples,
            // it also contains other event types, so they can be processed
            // (mostly) in order (as described here).
            //
            // That said, there is also support for multiple threads (specified as
            // the argument to the base class constructor).  So 'in order' has a
            // special meaning here.  Let's walk thru some cases.  These assume
            // m_ThreadCount is 4 and Threshold is 8.
            //
            // S1) 5 input samples get sent to ProcessInput.
            //
            // The ProcessingThread threads were created and started in the
            // constructor, and have been blocked in m_InputSampleQueue.Dequeue waiting
            // for work.  Now that there is something to do, all 4 threads wake up,
            // get a sample, and send it to OnProcessSample.  OnProcessSample is an
            // abstract method, so I can have no idea what it does, but I picture 2
            // general cases:
            //
            // 1) Each sample is completely independent of every other (like
            // Grayscale).
            // 2) There is some interdependence between samples (like MPEG
            // samples).
            //
            // This template treats these cases identically.  If some type of
            // inter-frame coordination is required, it is up to the derived class
            // to provide it.
            //
            // HOWEVER.
            //
            // There is some synchronization required for both cases.
            // OnProcessSample is expected to call OutputSample to output any
            // samples it creates.  But when OutputSample adds the samples
            // to the m_OutputSampleQueue queue, they must be in the correct order.  IOW
            // all the output samples generated from the first frame, followed by
            // all the samples from the second frame, etc.
            //
            // So while multiple samples can be CALCULATED at the same time, sample
            // processing isn't *completely* asynchronous, since outputs from the
            // second frame cannot be queued until the all the outputs from first
            // frame have been queued.
            //
            // How does a thread indicate that it has finished sending samples?
            // Since a thread may generate 0, 1 or more outputs from a single input
            // sample, calling OutputSample is not a practical way to signal this.
            // Instead, it simply returns from OnProcessSample.  At that point,
            // presumably no more samples can be generated from that input.
            //
            // The implication of this is that until a thread returns from
            // OnProcessSample, no further output can be generated by any thread.
            // So don't try to use one of the ProcessingThreads as anything other
            // than a sample processor.
            //
            // To return to S1, as each sample is processed, the thread returns
            // from OnProcessSample which releases the next thread to enter
            // OutputSample.
            //
            // A reasonable question here is: What if something goes wrong? If a
            // sample is corrupt, an allocation fails, some unexpected failure of
            // some kind, what do you do then?  Mostly you just (try to) ignore it.
            // Skip the sample and move on as best you can.  That's not just the
            // philosophy of this template, that's what MS suggests.  It's ugly,
            // but what is the alternative?
            //
            // There is a try/catch around OnProcessSample, but for performance
            // reasons, you should avoid depending on it for normal/expected
            // errors.
            //
            // S2) 2 samples followed by a drain.
            //
            // By spec, it is expected that no more input samples will be requested
            // after a drain request until the next NotifyStartOfStream (which
            // presumably comes after the drain completes).
            //
            // So the 2 input samples are processed as per normal, but the third
            // thread simply blocks on the same event that synchronizes access to
            // OutputSample.  When its turn arrives, it knows that all outputs
            // from previous inputs have been queued, so it just sends the
            // notification back to the client.  Then it calls MyEndStream since
            // the stream is now complete.
            //
            // S3) 2 samples followed by a Marker.
            //
            // Just like S2, except that (per spec) we *don't* stop sending
            // requests for input samples.
            //
            // S4) 7 samples followed by a flush.
            //
            // While in theory flush could be handled pretty much like drain,
            // ideally we'd like to skip uselessly calling OnProcessSample as much
            // as possible.
            //
            // So, when the flush message is sent, m_Flushing gets set AS PART of
            // queuing the message.  We don't want to wait until the flush message
            // is read out of the input queue, we want to start flushing as soon as
            // we can.
            //
            // So, the 4 threads that are processing samples when the
            // flush gets sent will all complete their processing as per normal.
            // When they are done and loop back to get their next sample, they find
            // the flushing flag is set, so they skip the processing and loop
            // around for more, quickly emptying the input queue.
            //
            // Eventually the flush message is retrieved from the queue.  That
            // thread blocks on the same event that synchronizes access to
            // OutputSample.  When it unblocks, it flushes the output queue.  And
            // (this is important) it sets m_UnsatisfiedNeedInputMessageCount to -1.  The
            // CommandFlush message that arrived in ProcessMessage HASN'T
            // returned yet.  It is blocking, waiting for us to complete.  If we
            // don't do this blocking, the client  could conceivably call
            // NotifyStartOfStream to start streaming again while we are still
            // flushing, resulting in a mess.
            //
            // I should also point out that if you are doing inter-frame
            // coordination (such as the MPEG example mentioned in S1), you need to
            // be aware that 'waiting for the other part to arrive' may be a
            // problem if the queue is being flushed.  You can check IsFlushing to
            // handle this case.
            //
            // S5) 4 input samples followed by a format change followed by 4
            // input samples.
            //
            // This one is a pain.  By spec, asynchronous MFTs must support
            // 'Dynamic Format Changes' (see ' Handling Stream Changes' in MSDN).
            // So, in the middle of processing frames using formatA, you can receive
            // a change notice and start receiving frames using formatB.
            //
            // This format change could be something as simple as changing the
            // frame size, or something more drastic like changing the subtype.
            //
            // So, my requirements here are:
            //
            // a) m_InputMediaType must not change until all pending input samples with
            // the old format have been processed.
            // b) m_OutputMediaType must not change until all pending output samples
            // from the old format have been processed thru both OnProcessSample &
            // MyProcessOutput.  This is not a requirement for the template code,
            // but OnProcessSample is a virtual function.  Who knows what it may 
            // need.
            // c) Allow new inputs to be queued pending the changes.
            //
            // So here's what happens:
            //
            // a) When the new format is proposed (via SetInputType), it is
            // validated with OnCheckInputType.  No change happens unless this
            // approves it.  OnCheckInputType must NOT compare the proposed new
            // with the currently set output type.  This breaks the whole concept 
            // of dynamic format changes.
            // b) When the change is approved by OnCheckInputType, m_ThreadCount 
            // Format messages are queued to m_InputSampleQueue.  They all contain the 
            // new media type.
            // c) Any remaining input samples are processed until the format
            // messages start getting hit.  Processing threads will block until the
            // final format message is processed.
            // d) m_InputMediaType gets changed and OnSetInputType gets called.
            // e) OnCheckOutputType is then called to see if the current output
            // type is still valid with the new input type.
            // f) *If* the output type must be changed, threads remain blocked
            // until the new output type is negotiated.
            // g) The processing threads are released.
            //
            // At this point samples can start getting processed again.
            //
            // Note that by spec, the client cannot change the output type
            // while the stream is active.  The output format type change must be
            // initiated by the MFT.
            //
            // S6) 7 samples followed by Shutdown.
            //
            // As with Flush, Shutdown also sets m_Flushing when the event gets
            // queued.  So while we don't cancel threads in the middle of
            // OnProcessSample, we do skip processing as fast as we can.
            //
            // When a shutdown is issued, threads could be in any number of places:
            //
            // a) Blocked in m_InputSampleQueue.Dequeue waiting for work
            // b) Processing in OnProcessSample
            // c) Blocked in OutputSample waiting for their turn
            // d) Waiting for other threads in Drain, Flush, Marker
            // e) Spinning in Format
            //
            // The question is, how to get all these folks to exit?  It might be
            // tempting to do nothing.  Since ProcessingThreads are all
            // IsBackground = true, .net will kill them cleanly at app shutdown.
            // But we may not be DOING an app shutdown.  MFTs can be loaded and
            // unloaded at will during an app run.  So, we really do need to clear
            // out all these threads.
            //
            // Since they might be blocked in m_InputSampleQueue.Dequeue (a), we send
            // m_ThreadCount Shutdown messages to wake them up.  However, we don't
            // actually have to wait to process the shutdown messages.  If there
            // are 7 samples queued followed by m_ThreadCount Shutdown messages, a
            // thread could retrieve a sample, see that we are m_Flushing, hit the
            // bottom of the loop and exit without ever having seen a Shutdown
            // event message.  Since we keep doing the 'Release the next guy' stuff
            // at the end of the loop, everybody who is blocked gets released
            // (b, c, d).  By checking for m_ShutdownThreads during the spins in
            // Format, we prevent this from blocking shutdown as well (e).
            //
            // Processing threads in OnProcessSample that are waiting for external
            // events ('waiting for the other part to arrive' or waiting for a new
            // output type to get set, etc) also need to watch for shutdown.  I
            // don't expect this to be a common situation.
            //
            // S7) 2 samples, drain, flush, StartOfStream, 2 samples
            //
            // While processing a flush, no messages can get added to the queue
            // until the flush is complete.  Further, since flush sets 
            // m_UnsatisfiedNeedInputMessageCount to -1, no samples been accepted since the 
            // flush.
            //
            // When a new NotifyStartOfStream message is received from the client,
            // we need to reset m_Flushing and m_UnsatisfiedNeedInputMessageCount, and send some
            // NeedInput messages.  But that doesn't require any action by the 
            // processing threads.  Once ProcessMessage has reset everything,
            // new samples start arriving and away we go.

            do
            {
                // get the next message
                TantaMFTAsyncMessageHolder messageHolderObj = (TantaMFTAsyncMessageHolder)m_InputSampleQueue.Dequeue();

                switch (messageHolderObj.mt)
                {
                    // have we got a sample
                    case TantaMFTAsyncMessageTypeEnum.Sample:
                    {
                        if (m_Flushing == false)
                        {
                            try
                            {
                                // call the user written sample processing code
                                OnProcessSample(messageHolderObj.sample, messageHolderObj.bDiscontinuity, messageHolderObj.InputNumber);
                            }
                            catch
                            {
                                // Ignore any errors and just keep going
                            }
                        }
                        else
                        {
                            // release the sample
                            SafeRelease(messageHolderObj.sample);
                        }
                        break;
                    }                    
                    // do we have a drain request?
                    case TantaMFTAsyncMessageTypeEnum.Drain:
                    {
                        // Wait for my turn (using the same events as
                        // OutputSample).
                        WaitForMyTurn(messageHolderObj.InputNumber);

                        try
                        {
                            // Process any 'final' samples (ie remaining samples
                            // from an audio echo).
                            OnDrain(messageHolderObj.InputNumber);
                        }
                        catch
                        {
                            // Ignore any errors and just keep going
                        }
                        SendStreamEvent(MediaEventType.METransformDrainComplete);
                        MyEndStream();
                        m_bDiscontinuity = true;
                        break;
                    }
                    // do we have a flush request
                    case TantaMFTAsyncMessageTypeEnum.Flush:
                    {
                        // Flush stays in effect until next StartOfStream.

                        // Also, since the thread that sent the Flush message
                        // is blocked waiting for us to respond (holding the
                        // m_TransformLockObject lock) there are no
                        // messages in the queue following the flush.

                        // Wait for the other threads to finish.  Note that they
                        // might be simply reading/discarding samples from the
                        // queue (which is presumably very quick), or they might
                        // be in OnProcessSample, and could take a while.
                        WaitForMyTurn(messageHolderObj.InputNumber);

                        // Now that all the input have finished, clear the output
                        // queue.
                        IMFSample samp;
                        while (!m_OutputSampleQueue.IsEmpty)
                            if (m_OutputSampleQueue.TryDequeue(out samp))
                                SafeRelease(samp);

                        // Note that ProcessMessage is blocked waiting for this value
                        // to change.  This also signals that no more inputs should
                        // be requested until the next NotifyStartOfStream.
                        Thread.VolatileWrite(ref m_UnsatisfiedNeedInputMessageCount, -1);

                        break;
                    }
                    // do we have a marker?
                    case TantaMFTAsyncMessageTypeEnum.Marker:
                    {
                        // When all the outputs for the previous inputs have been sent,
                        // it's time to send the marker.
                        WaitForMyTurn(messageHolderObj.InputNumber);

                        try
                        {
                            SendMarker(messageHolderObj.ptr);
                        }
                        catch
                        {
                            // Ignore any errors and just keep going
                        }

                        break;
                    }
                    // do we have a format change?
                    case TantaMFTAsyncMessageTypeEnum.Format:
                    {
                        // When the format changes, m_Thread Format messages
                        // get queued.  We need to wait for them all.
                        int me = Interlocked.Increment(ref m_ThreadsBlocked);

                        // Note that we never blocked requesting more input.  
                        // There could be samples using the new format in the 
                        // m_InputSampleQueue queue, which is why we are keeping 
                        // things blocked until the format change is complete.

                        if (me == m_ThreadCount)
                        {
                            // If there are multiple processing threads, all 
                            // but this one are sleeping at the Wait below.

                            // At this point:
                            // - A format change was set via SetInputType
                            // - All samples queued before this (ie using the
                            // old type) have been processed thru OnProcessSample.

                            // Wait for all the samples to go thru 
                            // MyProcessOutput.  While it might be safe/performant
                            // to proceed after all the OnProcessSamples are 
                            // complete, Let's be safe.  
                            // It's possible that instead of processing our 
                            // output, someone could flush it, so check for 
                            // that.
                            while (m_OutputSampleQueue.Count > 0 && !m_Flushing)
                                Thread.Sleep(10);

                            try
                            {
                                // Time to change the type.  After this, 
                                // m_InputMediaType has changed, and 
                                // m_OutputMediaType either isn't going to change, 
                                // or is null, awaiting the client's call to
                                // SetOutputType.

                                // Note that if we are flushing, we still 
                                // need to do the change.  It's a waste of 
                                // time if we are shutting down, but there
                                // you go.
                                MySetInput(messageHolderObj.dt);

                                // When m_OutputMediaType isn't null, we have our 
                                // (possibly new) output type, so we can 
                                // proceed with samples processing.  If we are
                                // flushing, no one is reading our outputs, so
                                // there's not point waiting.
                                while (m_OutputMediaType == null &&
                                    !m_Flushing)
                                    Thread.Sleep(10);

                                // Release the other threads (and ourself).
                                m_FormatEventSemaphore.Release(m_ThreadCount);
                            }
                            catch
                            {
                                // Ignore any errors and just keep going
                                // even though we are probably toast.
                            }
                        }

                        // Wait until the both the input type has changed and
                        // (possibly) the output type has been updated.

                        m_FormatEventSemaphore.Wait();

                        // Make sure no one tries to loop around and grab 
                        // someone else's semaphore.

                        if (Interlocked.Decrement(ref m_ThreadsBlocked) != 0)
                        {
                            while (Thread.VolatileRead(ref m_ThreadsBlocked) != 0)
                                Thread.Sleep(0);
                        }

                        break;
                    }
                    // do we have a shutdown notice
                    case TantaMFTAsyncMessageTypeEnum.Shutdown:
                    {
                        // Nothing to do here.  We will exit at the bottom of
                        // the loop.
                        break;
                    }
                    default:
                    {
                        Debug.Fail("Unrecognized type");
                        break;
                    }
                }

                if (m_ThreadCount > 1)
                {
                    // Note that since this is a MANUAL event, we can wait
                    // on it multiple times. Once it's set, it stays set
                    // until it gets Reset.
                    WaitForMyTurn(messageHolderObj.InputNumber);

                    // Release the next guy
                    int MyIndex = messageHolderObj.InputNumber % m_ThreadCount;
                    int k = MyIndex < m_ThreadCount - 1 ? MyIndex + 1 : 0;
                    m_ThreadSemaphoreArray[k].Set();

                    // And now we are resetting it.
                    m_ThreadSemaphoreArray[MyIndex].Reset();
                }

            } while (Thread.VolatileRead(ref m_ShutdownThreads) == 0);

            // Last guy out: free the thread stuff
            if (Interlocked.Decrement(ref m_ShutdownThreads) == 0)
            {
                m_FormatEventSemaphore.Dispose();
                m_InputSampleQueue.Shutdown(); // Flush queue and free semaphore

                for (int x = 0; x < m_ThreadCount; x++)
                {
                    m_ThreadSemaphoreArray[x].Dispose();
                }
            }
        }

        #endregion

        // ########################################################################
        // ##### Utility methods (possibly useful to derived classes).
        // ########################################################################

        #region Utility methods

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// If Discontinuity, set the appropriate attribute on the sample.
        /// </summary>
        /// <param name="Discontinuity">Time to set discontinuity?</param>
        /// <param name="pSample">The sample that will be sent to OutputSample.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected static void HandleDiscontinuity(bool Discontinuity, IMFSample pSample)
        {
            if (Discontinuity)
            {
                MFError throwonhr = pSample.SetUINT32(MFAttributesClsid.MFSampleExtension_Discontinuity, 1);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Blocks waiting on a semaphore
        /// </summary>
        /// <param name="InputMessageNumber">the message number to wait on</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        void WaitForMyTurn(int InputMessageNumber)
        {
            int j = InputMessageNumber % m_ThreadCount;
            m_ThreadSemaphoreArray[j].Wait();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called by OnProcessSample to output processed samples.
        /// 
        /// If pSample is null, it triggers a change to the output
        /// media type.  Normally this is done as part of a dynamic format
        /// change when the client changes our input type.  But in theory, an
        /// MFT can change its output type at will while running.
        /// 
        /// </summary>
        /// <param name="pSample">The processed sample (can be the input
        /// sample if using MFTInputStreamInfoFlags.ProcessesInPlace.</param>
        /// <param name="InputMessageNumber">The (exact) value passed to
        /// OnProcessSample or OnDrain.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected void OutputSample(IMFSample pSample, int InputMessageNumber)
        {
            // It's possible for multiple outputs to be generated from
            // a single input.
            // And to ensure ordering, we can't allow the outputs from
            // input #2 to get queued before all the input from #1 has
            // been queued.

            // This will block threads until it's time to accept their
            // output.  Since m_ThreadSemaphoreArray are manual events, we can wait on
            // them multiple times if multiple samples are being sent.

            WaitForMyTurn(InputMessageNumber);

            if (pSample == null)
            {
                DebugMessage("Preparing for dynamic change of output");
            }
            m_OutputSampleQueue.Enqueue(pSample);

            // Send message to client that another sample is ready.
            SendSampleReady();
        }

        // Accessors.  Mostly derived classes shouldn't need to access our 
        // members, but if they do, here you go.

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the Input type. Note that setting a value here releases
        /// the previous value.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected IMFMediaType InputType
        {
            get { return m_InputMediaType; }
            set { SafeRelease(m_InputMediaType); m_InputMediaType = value; }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the Output type. Note that setting a value here releases
        /// the previous value.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected IMFMediaType OutputType
        {
            get { return m_OutputMediaType; }
            set { SafeRelease(m_OutputMediaType); m_OutputMediaType = value; }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the attribute collection
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected IMFAttributes Attributes
        {
            get { return m_TransformAttributeCollection; }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the thread threshold
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected int Threshold
        {
            get { return m_MaxPermittedThreads; }
            set { m_MaxPermittedThreads = value; }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the is shutdown flag
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected bool IsShutdown
        {
            get { return m_Shutdown; }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the is stream active flag
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected bool IsStreamActive
        {
            get { return m_StreamIsActive; }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the is flushing flag
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected bool IsFlushing
        {
            get { return m_Flushing; }
        }

        #endregion

    }

}

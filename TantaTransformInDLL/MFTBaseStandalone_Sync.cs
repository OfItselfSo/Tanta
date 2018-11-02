using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
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

/// Notes
/// Most of this code is derived from the samples which ship with the MF.Net dll. These are 
/// in turn derived from the original Microsoft samples. These have been placed in the public domain 
/// without copyright.
/// 
/// The MF.Net library itself is licensed under the GNU Library or Lesser General Public License version 2.0 (LGPLv2)
/// 

/// The ultimate output of this solution is a DLL which can act as a Transform. This class acts as a base class for that
/// transform. Because we want to minimize the number of dlls involved in the transform we do not want to include the TantaCommon
/// libaray. Therefore this code is a local copy of the TantaMFTBase_Sync class in the TantaCommon library with some additional
/// code copied out of the static TantaWMFUtils class to get it to compile. Other than that it is the same.

namespace MFTTantaVideoRotator_Sync
{

    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// This class acts as a base for Synchronous Transforms with one input and 
    /// one output stream. 
    /// 
    /// This class is a local copy of the TantaMFTBase_Sync class in the 
    /// TantaCommon library with some additional code copied out of the static 
    /// TantaWMFUtils class to get it to compile.
    /// 
    /// This code automates pretty much everything that can be abstracted out leaving only a 
    /// few implementation specific routines to be implemented in the child class 
    /// (the actual transform) which is derived from this one
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>
    abstract public class TantaMFTBaseStandalone_Sync : COMBase, IMFTransform
    {

        /// Synchronization object used by public entry points
        /// to prevent multiple methods from being invoked at
        /// once.  Some work (for example parameter validation)
        /// is done before invoking this lock.
        private object m_TransformLockObject;

        /// Input type set by SetInputType.
        /// Can be null if not set yet or if reset by SetInputType(null).
        private IMFMediaType m_pInputType;

        /// Output type set by SetOutputType.
        /// Can be null if not set yet or if reset by SetOutputType(null).
        private IMFMediaType m_pOutputType;

        /// The most recent sample received by ProcessInput, or null if no sample is pending.
        private IMFSample m_pSample;

        /// this attribute collection is not strictly required in
        /// sync mode transforms as no part of the configuration
        /// and I/O type negotiation process uses these. However, 
        /// the client can access the attributes in this object at
        /// runtime via the TopologyNode of the transform so we 
        /// can use these attributes to send and receive certain
        /// types of configuration information.
        private readonly IMFAttributes m_TransformAttributeCollection; 

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected TantaMFTBaseStandalone_Sync()
        {
            //DebugMessage("TantaMFTBaseStandalone_Sync MFTImplementation");

            m_TransformLockObject = new object();

            m_pInputType = null;
            m_pOutputType = null;

            m_pSample = null;

            // Build the IMFAttributes we use. We give it three by default.
            MFExtern.MFCreateAttributes(out m_TransformAttributeCollection, 3);

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Destructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        ~TantaMFTBaseStandalone_Sync()
        {
            //DebugMessage("TantaMFTBaseStandalone_Sync ~MFTImplementation");

            // release any COM objects we have
            if (m_TransformLockObject != null)
            {
                SafeRelease(m_pInputType);
                SafeRelease(m_pOutputType);
                SafeRelease(m_pSample);
                SafeRelease(m_TransformAttributeCollection);

                m_TransformLockObject = null;
            }
        }

        // ########################################################################
        // ##### Overrides, child classes must implement these
        // ########################################################################

        #region Overrides

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Report whether a proposed input type is accepted by the MFT.
        /// </summary>
        /// <param name="pmt">The type to check.  Should never be null.</param>
        /// <returns>S_Ok if the type is valid or MF_E_INVALIDTYPE.</returns>
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
        /// The routine that usually performs the transform. 
        /// 
        /// The input sample is in InputSample.  Process it into the pOutputSamples struct.
        /// Depending on what you set in On*StreamInfo, you can either perform
        /// in-place processing by modifying the input sample (which still must
        /// set inout the struct), or create a new IMFSample and FULLY populate
        /// it from the input.  If the input sample has been fully processed, 
        /// set InputSample to null.
        /// </summary>
        /// <param name="pOutputSamples">The structure to populate with output values.</param>
        /// <returns>S_Ok unless error.</returns>
        abstract protected HResult OnProcessOutput(ref MFTOutputDataBuffer pOutputSamples);

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Report whether a proposed output type is accepted by the MFT. The 
        /// default behavior is to assume that the input type 
        /// must be set before the output type, and that the proposed output 
        /// type must exactly equal the value returned from the virtual
        /// CreateOutputFromInput.  Override as necessary
        /// </summary>
        /// <param name="pmt">The type to check.  Should never be null (which are always valid).</param>
        /// <returns>S_Ok if the type is valid or MF_E_INVALIDTYPE.</returns>
        virtual protected HResult OnCheckOutputType(IMFMediaType pmt)
        {
            HResult hr = HResult.S_OK;

            // If the input type is set, see if they match.
            if (m_pInputType != null)
            {
                IMFMediaType pCheck = CreateOutputFromInput();

                try
                {
                    hr = IsMediaTypeIdentical(pmt, pCheck);
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
        /// </summary>
        /// <remarks>The new type is in InputType, and can be null.</remarks>
        virtual protected void OnSetInputType()
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Override to get notified when the Output Type is actually being set.
        /// </summary>
        /// <remarks>The new type is in OutputType, and can be null.</remarks>
        virtual protected void OnSetOutputType()
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Override to allow the client to retrieve the MFT's list of supported 
        /// Input Types. 
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
        /// </summary>
        /// <param name="dwTypeIndex">The (zero-based) index of the type.</param>
        /// <param name="pOutputType">The output type supported by the MFT.</param>
        /// <returns>S_Ok or MFError.MF_E_NO_MORE_TYPES.</returns>
        virtual protected HResult OnEnumOutputTypes(int dwTypeIndex, out IMFMediaType pOutputType)
        {
            HResult hr = HResult.S_OK;

            // If the input type is specified, the output type must be the same.
            if (m_pInputType != null)
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
        /// A new input sample has been received.
        /// </summary>
        /// <returns>S_Ok unless error.</returns>
        /// <remarks>The sample is in InputSample.  Typically nothing is done
        /// here.  The processing is done in OnProcessOutput, when we have
        /// the output buffer.</remarks>
        virtual protected HResult OnProcessInput()
        {
            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called at end of stream (and start of new stream).
        /// </summary>
        virtual protected void OnReset()
        {
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
        /// </summary>
        /// <returns>By default, a clone of the input type.  Can be overridden.</returns>
        virtual protected IMFMediaType CreateOutputFromInput()
        {
            return CloneMediaType(m_pInputType);
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
        public HResult GetStreamLimits(MFInt pdwInputMinimum, MFInt pdwInputMaximum, MFInt pdwOutputMinimum, MFInt pdwOutputMaximum)
        {
            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("GetStreamLimits GetStreamLimits");

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
        public HResult GetStreamCount(MFInt pcInputStreams, MFInt pcOutputStreams)
        {
            HResult hr = HResult.S_OK;

            try
            {
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

            return hr;
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
        public HResult GetStreamIDs(int dwInputIDArraySize, int[] pdwInputIDs, int dwOutputIDArraySize, int[] pdwOutputIDs)
        {
            HResult hr = HResult.S_OK;

            try
            {
                lock (m_TransformLockObject)
                {
                    // MF.Net sample notes
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
        public HResult GetInputStreamInfo(int dwInputStreamID, out MFTInputStreamInfo pStreamInfo)
        {
            // Overwrites everything with zeros.
            pStreamInfo = new MFTInputStreamInfo();

            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("GetInputStreamInfo");

                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    // The implementation can override the defaults,
                    // and must set cbSize
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
        public HResult GetOutputStreamInfo(int dwOutputStreamID, out MFTOutputStreamInfo pStreamInfo)
        {
            // Overwrites everything with zeros.
            pStreamInfo = new MFTOutputStreamInfo();

            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("GetOutputStreamInfo");

                CheckValidStream(dwOutputStreamID);

                lock (m_TransformLockObject)
                {
                    // The implementation can override the defaults,
                    // and must set cbSize.
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
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
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
                    pAttributes = GetUniqueRCW(m_TransformAttributeCollection) as IMFAttributes;
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }

            return hr; 
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
        public HResult GetInputStreamAttributes(int dwInputStreamID, out IMFAttributes pAttributes)
        {
            pAttributes = null;

            HResult hr = HResult.S_OK;

            try
            {
                // Trace("GetInputStreamAttributes"); Not interesting

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
        public HResult GetOutputStreamAttributes(int dwOutputStreamID, out IMFAttributes pAttributes)
        {
            pAttributes = null;

            HResult hr = HResult.S_OK;

            try
            {
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
        public HResult DeleteInputStream(int dwStreamID)
        {
            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("DeleteInputStream");

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
        public HResult AddInputStreams(int cStreams, int[] adwStreamIDs)
        {
            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("AddInputStreams");

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
        public HResult GetInputAvailableType(int dwInputStreamID, int dwTypeIndex, out IMFMediaType ppType)
        {
            ppType = null;
            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage(string.Format("GetInputAvailableType (stream = {0}, type index = {1})", dwInputStreamID, dwTypeIndex));

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
        public HResult GetOutputAvailableType(int dwOutputStreamID, int dwTypeIndex, out IMFMediaType ppType)
        {
            ppType = null;

            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage(string.Format("GetOutputAvailableType (stream = {0}, type index = {1})", dwOutputStreamID, dwTypeIndex));

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
        public HResult SetInputType(int dwInputStreamID, IMFMediaType pType, MFTSetTypeFlags dwFlags)
        {
            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("SetInputType");

                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    // If we have an input sample, the client cannot change the type now.
                    if (!HasPendingOutput())
                    {
                        // Allow input type to be cleared.
                        if (pType != null)
                        {
                            // Validate non-null types.
                            hr = OnCheckInputType(pType);
                        }

                        if (Succeeded(hr))
                        {
                            // Does the caller want to set the type?  Or just test it?
                            bool bReallySet = ((dwFlags & MFTSetTypeFlags.TestOnly) == 0);

                            if (bReallySet)
                            {
                                // Make a copy of the IMFMediaType.
                                InputType = CloneMediaType(pType);

                                OnSetInputType();
                            }
                        }
                    }
                    else
                    {
                        // Can't change type while samples are pending
                        hr = HResult.MF_E_TRANSFORM_CANNOT_CHANGE_MEDIATYPE_WHILE_PROCESSING;
                    }
                }
            }
            catch (Exception e)
            {
                hr = (HResult)Marshal.GetHRForException(e);
            }
            //finally
            {
                // MF.Net sample notes
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
        public HResult SetOutputType(int dwOutputStreamID, IMFMediaType pType, MFTSetTypeFlags dwFlags)
        {
            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("SetOutputType");

                CheckValidStream(dwOutputStreamID);

                lock (m_TransformLockObject)
                {
                    // If we have an input sample, the client cannot change the type now.
                    if (!HasPendingOutput())
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
                                OutputType = CloneMediaType(pType);

                                OnSetOutputType();
                            }
                        }
                    }
                    else
                    {
                        // Cannot change type while samples are pending
                        hr = HResult.MF_E_TRANSFORM_CANNOT_CHANGE_MEDIATYPE_WHILE_PROCESSING;
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
        public HResult GetInputCurrentType(int dwInputStreamID, out IMFMediaType ppType)
        {
            ppType = null;

            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("GetInputCurrentType");

                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    if (m_pInputType != null)
                    {
                        ppType = CloneMediaType(m_pInputType);
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
        public HResult GetOutputCurrentType(int dwOutputStreamID, out IMFMediaType ppType)
        {
            ppType = null;

            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("GetOutputCurrentType");

                CheckValidStream(dwOutputStreamID);

                lock (m_TransformLockObject)
                {
                    if (m_pOutputType != null)
                    {
                        ppType = CloneMediaType(m_pOutputType);
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
        public HResult GetInputStatus(int dwInputStreamID, out MFTInputStatusFlags pdwFlags)
        {
            pdwFlags = MFTInputStatusFlags.None;

            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("GetInputStatus");

                CheckValidStream(dwInputStreamID);

                lock (m_TransformLockObject)
                {
                    if (CanAcceptInput())
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
        public HResult GetOutputStatus(out MFTOutputStatusFlags pdwFlags)
        {
            pdwFlags = MFTOutputStatusFlags.None;

            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("GetOutputStatus");

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
        public HResult SetOutputBounds(long hnsLowerBound, long hnsUpperBound)
        {
            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("SetOutputBounds");

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
        public HResult ProcessEvent(int dwInputStreamID, IMFMediaEvent pEvent)
        {
            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("ProcessEvent");

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
                // MF.Net sample notes
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
        public HResult ProcessMessage(MFTMessageType eMessage, IntPtr ulParam)
        {
            HResult hr = HResult.S_OK;

            try
            {
                //DebugMessage("ProcessMessage " + eMessage.ToString());

                lock (m_TransformLockObject)
                {
                    switch (eMessage)
                    {
                        case MFTMessageType.NotifyStartOfStream:
                            // Optional message for non-async MFTs.
                            Reset();
                            break;

                        case MFTMessageType.CommandFlush:
                            Reset();
                            break;

                        case MFTMessageType.CommandDrain:
                            // Drain: Tells the MFT not to accept any more input until
                            // all of the pending output has been processed. That is our
                            // default behavior already, so there is nothing to do.
                            break;

                        case MFTMessageType.CommandMarker:
                            hr = HResult.E_NOTIMPL;
                            break;

                        case MFTMessageType.NotifyEndOfStream:
                            break;

                        case MFTMessageType.NotifyBeginStreaming:
                            break;

                        case MFTMessageType.NotifyEndStreaming:
                            break;

                        case MFTMessageType.SetD3DManager:
                            // The pipeline should never send this message unless the MFT
                            // has the MF_SA_D3D_AWARE attribute set to TRUE. However, if we
                            // do get this message, it's invalid and we don't implement it.
                            hr = HResult.E_NOTIMPL;
                            break;

                        default:
                            // DebugMessage("Unknown message type: " + eMessage.ToString());
                            // MF.Net sample comment
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
        /// the actual work in ProcessOutput we just cache the input buffer. However
        /// we do call our virtual override so the child class can do some 
        /// pre-processing if it wants to.
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
        public HResult ProcessInput(int dwInputStreamID, IMFSample pSample, int dwFlags)
        {
            HResult hr;

            // some sanity checks
            hr = CheckValidStreamID(dwInputStreamID);
            if (hr != HResult.S_OK)
            {
                throw new Exception("ProcessInput call CheckValidStreamID on ID " + dwInputStreamID.ToString() + " failed. Err=" + hr.ToString());
            }
            // more checks
            if (pSample == null) return HResult.E_POINTER;
            // the docs state this must be zero
            if (dwFlags != 0) return HResult.E_INVALIDARG;


            lock (m_TransformLockObject)
            {
                hr = CheckIfInputAndOutputTypesAreSet();
                if (hr != HResult.S_OK)
                {
                    throw new Exception("ProcessInput call to CheckIfInputAndOutputTypesAreSet failed. Err=" + hr.ToString());
                }

                if (CanAcceptInput() == false) return HResult.MF_E_NOTACCEPTING;

                // Cache the sample. We usually do the actual
                // work in ProcessOutput, since that's when we
                // have the output buffer.
                m_pSample = pSample;

                // Call the virtual function.
                hr = OnProcessInput();
            }

            return CheckReturn(hr);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Generates output from the current input data. We expect InputSample
        /// to have been set. This is the source data.
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
        public HResult ProcessOutput(MFTProcessOutputFlags dwFlags, int cOutputBufferCount, MFTOutputDataBuffer[] outputSamplesArray, out ProcessOutputStatus pdwStatus)
        {
            pdwStatus = ProcessOutputStatus.None;

            HResult hr = HResult.S_OK;

            // Check input parameters.
            if (cOutputBufferCount != 1) hr = HResult.E_INVALIDARG;
            if (dwFlags != MFTProcessOutputFlags.None) hr = HResult.E_INVALIDARG;
            if (outputSamplesArray == null) hr = HResult.E_POINTER;

            // In theory, we should check pOutputSamples[0].pSample,
            // but it may be null or not depending on how the derived
            // set MFTOutputStreamInfoFlags, so we leave the checking
            // for OnProcessOutput.

            lock (m_TransformLockObject)
            {
                hr = CheckIfInputAndOutputTypesAreSet();
                if (hr != HResult.S_OK)
                {
                    throw new Exception("ProcessOutput call to CheckIfInputAndOutputTypesAreSet failed. Err=" + hr.ToString());
                }

                // If we don't have an input sample, we need some input before
                // we can generate any output.
                if (HasPendingOutput() == false) return HResult.MF_E_TRANSFORM_NEED_MORE_INPUT;

                // This base class is only designed for a single stream in and a 
                // single stream out. Thus we only process the Input Sample into 
                // the first entry in the pOutputSamples array. 
                hr = OnProcessOutput(ref outputSamplesArray[0]);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("ProcessOutput call to OnProcessOutput failed. Err=" + hr.ToString());
                }
            }

            return HResult.S_OK;
        }

        #endregion

        // ########################################################################
        // ##### Private methods (Only expected to be used by template)
        // ########################################################################

        #region Private methods 

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Allow for reset between NotifyStartOfStream calls.
        /// </summary>
        private void Reset()
        {
            InputSample = null;

            // Call the virtual function
            OnReset();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Are inputs allowed at the current time?
        /// </summary>
        /// <returns>true we can allow input, false we cannot</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private bool CanAcceptInput()
        {
            // If we already have an input sample, we don't accept
            // another one until the client calls ProcessOutput or Flush.
            if (m_pSample == null) return true;
            return false;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Do we have data for ProcessOutput?
        /// </summary>
        /// <returns>true we have data, false we do not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private bool HasPendingOutput()
        {
            if (m_pSample != null) return true;
            return false;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Check for valid stream number.
        /// </summary>
        /// <param name="dwStreamID">Stream to check.</param>
        /// <remarks>Easy to do since the only valid value is zero.</remarks>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private static void CheckValidStream(int dwStreamID)
        {
            // DebugTODO("remove this in the future");
            if (dwStreamID != 0)
            {
                throw new MFException(HResult.MF_E_INVALIDSTREAMNUMBER);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Check for valid stream number. Easy to do since the only valid value is zero.
        /// </summary>
        /// <param name="dwStreamID">Stream to check.</param>
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private static HResult CheckValidStreamID(int dwStreamID)
        {
            if (dwStreamID == 0) return HResult.S_OK;
            return HResult.MF_E_INVALIDSTREAMNUMBER;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Ensure both input and output media types are set.
        /// </summary>
        /// <returns>S_OK or other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private HResult CheckIfInputAndOutputTypesAreSet()
        {
            if (m_pInputType == null || m_pOutputType == null)
                return HResult.MF_E_TRANSFORM_TYPE_NOT_SET;

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Print out a debug line when hr doesn't equal S_Ok.
        /// </summary>
        /// <param name="hr">Value to check</param>
        /// <returns>The input value.</returns>
        /// <remarks>This code shows the calling routine and the error text.
        /// All the public interface methods use this to wrap their returns.
        /// </remarks>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private HResult CheckReturn(HResult hr)
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
                //DebugMessage(string.Format("{1} returning 0x{0:x} ({2})", hr, sName, sError));
            }
#endif
            return hr;
        }

        #endregion

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
            get { return m_pInputType; }
            set { SafeRelease(m_pInputType); m_pInputType = value; }
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
            get { return m_pOutputType; }
            set { SafeRelease(m_pOutputType); m_pOutputType = value; }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the InputSample. Note that setting a value here releases
        /// the previous value.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected IMFSample InputSample
        {
            get { return m_pSample; }
            set { SafeRelease(m_pSample); m_pSample = value; }
        }


        // ########################################################################
        // ##### Routines which would normally be called out of TantaWMFUtils except 
        // ##### that we do not include TantaCommon here since we want a single 
        // ##### DLL as output
        // ########################################################################

        #region TantaWMFUtils Routines

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary> 
        /// Gets a unique RuntimeCallableWrapper (RCW) so a call to ReleaseComObject 
        /// on the returned object won't wipe the original object.
        /// </summary>
        /// <param name="o">Object to wrap.  Can be null.</param>
        /// <returns>Wrapped object.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static object GetUniqueRCW(object o)
        {
            object oret;

            if (o != null)
            {
                IntPtr p = Marshal.GetIUnknownForObject(o);
                try
                {
                    oret = Marshal.GetUniqueObjectForIUnknown(p);
                }
                finally
                {
                    Marshal.Release(p);
                }
            }
            else
            {
                oret = null;
            }

            return oret;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Check to see if two IMFMediaTypes are identical. Copied straight out 
        /// of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="a">First media type.</param>
        /// <param name="b">Second media type.</param>
        /// <returns>S_Ok if identical, else MF_E_INVALIDTYPE.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static HResult IsMediaTypeIdentical(IMFMediaType a, IMFMediaType b)
        {
            // Otherwise, proposed input must be identical to output.
            MFMediaEqual flags;
            HResult hr = a.IsEqual(b, out flags);

            // IsEqual can return S_FALSE. Treat this as failure.
            if (hr != HResult.S_OK)
            {
                hr = HResult.MF_E_INVALIDTYPE;
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Return a duplicate of a media type. Null input gives null output. 
        /// Copied straight out of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="inType">The IMFMediaType to clone or null.</param>
        /// <returns>Duplicate IMFMediaType or null.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static IMFMediaType CloneMediaType(IMFMediaType inType)
        {
            IMFMediaType outType = null;
            HResult hr;

            if (inType != null)
            {

                hr = MFExtern.MFCreateMediaType(out outType);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CloneMediaType call to MFCreateMediaType failed. Err=" + hr.ToString());
                }
                hr = inType.CopyAllItems(outType);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CloneMediaType call to CopyAllItems failed. Err=" + hr.ToString());
                }
            }

            return outType;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Locks an IMFMediaBuffer and returns an IntPtr to the raw data. You must call 
        /// UnLockIMFBuffer. Note that LockIMF2DBufferAndGetRawData is preferred
        /// because it is faster but not all Media Buffers are IMF2DBuffers so you have
        /// to check. Make sure to call the appropriate version of unlock.
        /// 
        /// Note: the IMFMediaBuffer lock does not return the stride - you have to obtain
        ///       that elsewhere.
        ///
        /// </summary>
        /// <param name="mediaBuffer">The IMFMediaBuffer object</param>
        /// <param name="rawDataPtr">the pointer to the raw data</param>
        /// <param name="maxLen">Receives the maximum amount of data that can be written to the buffer.</param>
        /// <param name="currentLen">Receives the length of the valid data in the buffer, in bytes.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static void LockIMFMediaBufferAndGetRawData(IMFMediaBuffer mediaBuffer, out IntPtr rawDataPtr, out int maxLen, out int currentLen)
        {
            // init
            rawDataPtr = IntPtr.Zero;
            maxLen = 0;
            currentLen = 0;

            if (mediaBuffer == null) throw new Exception("LockMediaBufferAndGetRawData mediaBuffer == null");

            // must call an UnLockIMFBuffer
            HResult hr = mediaBuffer.Lock(out rawDataPtr, out maxLen, out currentLen);
            if (hr != HResult.S_OK)
            {
                throw new Exception("LockMediaBufferAndGetRawData call to mediaBuffer.Lock failed. Err=" + hr.ToString());
            }
            return;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Locks an IMF2DBuffer and returns an IntPtr to the raw data. You must call 
        /// UnLockIMF2DBuffer. Note that LockIMF2DBufferAndGetRawData is preferred
        /// to LockIMFBufferAndGetRawData because it is faster but not all Media Buffers 
        /// are IMF2DBuffers so you have to check. Make sure to call the appropriate 
        /// version of unlock.
        /// 
        ///
        /// </summary>
        /// <param name="mediaBuffer">The IMFMediaBuffer object</param>
        /// <param name="rawDataPtr">the pointer to the raw data</param>
        /// <param name="bufferStride">Receives the stride of the buffer.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static void LockIMF2DBufferAndGetRawData(IMF2DBuffer media2DBuffer, out IntPtr rawDataPtr, out int bufferStride)
        {
            // init
            rawDataPtr = IntPtr.Zero;
            bufferStride = 0;

            if (media2DBuffer == null) throw new Exception("LockIMF2DBufferAndGetRawData media2DBuffer == null");

            // must call an UnLockIMF2DBuffer
            HResult hr = media2DBuffer.Lock2D(out rawDataPtr, out bufferStride);
            if (hr != HResult.S_OK)
            {
                throw new Exception("LockIMF2DBufferAndGetRawData call to media2DBuffer.Lock2D failed. Err=" + hr.ToString());
            }
            return;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// UnLocks a previoiusly locked IMFMediaBuffer. There is also an 
        /// UnLockIMF2DBuffer make sure you call the one you locked it with
        ///
        /// </summary>
        /// <param name="mediaBuffer">The IMFMediaBuffer object</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static void UnLockIMFMediaBuffer(IMFMediaBuffer mediaBuffer)
        {
            HResult hr = mediaBuffer.Unlock();
            if (hr != HResult.S_OK)
            {
                throw new Exception("UnLockIMFMediaBuffer call to mediaBuffer.Unlock failed. Err=" + hr.ToString());
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// UnLocks a previoiusly locked IMF2DBuffer. There is also an 
        /// UnLockIMFMediaBuffer make sure you call the one you locked it with
        ///
        /// </summary>
        /// <param name="media2DBuffer">The IMF2DBuffer object</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static void UnLockIMF2DBuffer(IMF2DBuffer media2DBuffer)
        {
            HResult hr = media2DBuffer.Unlock2D();
            if (hr != HResult.S_OK)
            {
                throw new Exception("UnLockIMFMediaBuffer call to media2DBuffer.Unlock2D failed. Err=" + hr.ToString());
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create a partial media type from a Major and Sub type. Copied straight 
        /// out of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="gMajorType">Major type.</param>
        /// <param name="gSubtype">Sub type.</param>
        /// <returns>Newly created media type.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static IMFMediaType CreatePartialMediaType(Guid gMajorType, Guid gSubtype)
        {
            IMFMediaType ppmt;
            MFError throwonhr;

            throwonhr = MFExtern.MFCreateMediaType(out ppmt);
            throwonhr = ppmt.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, gMajorType);
            throwonhr = ppmt.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, gSubtype);

            return ppmt;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create an array of partial media types using an array of subtypes. Copied straight 
        /// out of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="dwTypeIndex">Index into the array.</param>
        /// <param name="gMajorType">Major type.</param>
        /// <param name="gSubTypes">Array of subtypes.</param>
        /// <param name="ppmt">Newly created media type.</param>
        /// <returns>S_Ok if valid index, else MF_E_NO_MORE_TYPES.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static HResult CreatePartialMediaType(int dwTypeIndex, Guid gMajorType, Guid[] gSubTypes, out IMFMediaType ppmt)
        {
            HResult hr;

            if (dwTypeIndex < gSubTypes.Length)
            {
                ppmt = CreatePartialMediaType(gMajorType, gSubTypes[dwTypeIndex]);
                hr = HResult.S_OK;
            }
            else
            {
                ppmt = null;
                hr = HResult.MF_E_NO_MORE_TYPES;
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Check a media type for a matching Major type and a list of Subtypes.
        ///  Copied straight out of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="pmt">IMFMediaType to check</param>
        /// <param name="gMajorType">MajorType to check for.</param>
        /// <param name="gSubtypes">Array of subTypes to check for.</param>
        /// <returns>S_Ok if match, else MF_E_INVALIDTYPE.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static HResult CheckMediaType(IMFMediaType pmt, Guid gMajorType, Guid[] gSubTypes)
        {
            Guid major_type;

            // Major type must be video.
            HResult hr = HResult.S_OK;
            MFError throwonhr;

            throwonhr = pmt.GetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, out major_type);

            if (major_type == gMajorType)
            {
                Guid subtype;

                // Get the subtype GUID.
                throwonhr = pmt.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out subtype);

                // Look for the subtype in our list of accepted types.
                hr = HResult.MF_E_INVALIDTYPE;
                for (int i = 0; i < gSubTypes.Length; i++)
                {
                    if (subtype == gSubTypes[i])
                    {
                        hr = HResult.S_OK;
                        break;
                    }
                }
            }
            else
            {
                hr = HResult.MF_E_INVALIDTYPE;
            }

            return hr;
        }
        #endregion
    }
}

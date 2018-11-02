using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

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
/// Some parts of this code are derived from the samples which ships with the MF.Net dll. These are 
/// in turn derived from the original Microsoft samples. These have been placed in the public domain 
/// without copyright.
/// 
/// The MF.Net library itself is licensed under the GNU Library or Lesser General Public License version 2.0 (LGPLv2)
/// 

/// This code demonstrates how a transform can be compiled as a DLL and configured
/// in the registry and accessed via a separate client. It also demonstrates a communication
/// method between the client and the dll which enables the client to modify the behaviour
/// of the transform while it is running.

/// SUPER IMPORTANT NOTE: This code produces a DLL which implements a transform.  This Visual Studio solution will automatically 
/// install that DLL in the registry and also make it available for enumeration by other WMF objects. In order to do this
/// it must have administrator priviledges. 

/// START THIS VISUAL STUDIO SOLUTION AS ADMINISTRATOR BEFORE COMPILATION IF YOU WISH TO HAVE IT AUTOMATICALLY CONFIGURE
/// THE RESULTING TRANSFORM DLL IN THE REGISTRY THUS MAKING IT AVAILABLE FOR USE BY OTHER WMF APPLICATIONS.
/// Alternately, you can ignore the error message about not being able to write to the registry and just use the commands
/// listed in the ManualRegister.txt file. You will still need to be Administrator to run these.

/// This code requires the use of a client software to operate. The TantaTransformInDLLClient code is designed for this purpose.
/// The TantaTransformInDLLClient code does not need to be compiled as administrator.
/// 

namespace MFTTantaVideoRotator_Sync
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// An MFT to demonstrate how to rotate the video frames and (probably more
    /// importantly) to demonstrate how an MFT can be automatically placed in the 
    /// registry upon compilation. It also demonstrates a method of communication
    /// between the client and this transform. The client can dynamically change
    /// the rotate mode while the video is playing.
    /// 
    /// The TantaTransformInDLLClient code is the client to use for this DLL.
    /// 
    /// START VISUAL STUDIO AS ADMINISTRATOR IN ORDER TO AUTOMATICALLY REGISTER
    /// THIS DLL IN THE REGISTRY
    /// 
    /// This MFT can handle 1 media type (ARGB). You will also note that it
    /// hard codes the support for this type unlike some of the other TotoMFT's 
    /// which support a number of different types.
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>

    /// These class decorations are important. This is the GUID under which the MFT
    /// will be registered. If you copy this code you should change this.
    /// You should also probably change the class name. This will appear in the 
    /// registry as well. Use the TantaTransformPicker sample application to view
    /// the MFT's in the registry.
    [ComVisible(true),
    Guid("F1E67619-FB5B-470B-9306-EBF40D54985E"),
    ClassInterface(ClassInterfaceType.None)]
    public sealed class MFTTantaVideoRotator_Sync : TantaMFTBaseStandalone_Sync
    {
        // Format information
        private int m_imageWidthInPixels;
        private int m_imageHeightInPixels;
        private int m_cbImageSize;              // Image size, in bytes.
        private int m_lStrideIfContiguous;

        // this list of the guids of the media subtypes we support. The input format must be the same
        // as the output format 
        private readonly Guid[] m_MediaSubtypes = new Guid[] { MFMediaType.RGB32 };

        // we do not support interlacing. If the Media Type proposed by the client says
        // it "might be interlaced" we set this flag. If interlaced frames are passe in, we will reject them 
        private bool m_MightBeInterlaced;

        // this Guid is the key we use to retrive the FlipMode from the attributes
        // of the Topology Node of transform. The FlipMode is placed there by the 
        // client. The client also needs to know this Guid. Other than that, there 
        // is nothing special about this value. 
        private Guid clsidFlipMode = new Guid("EF5FB03A-23B5-4250-9AA6-0E70907F8B4B");

        // this value is only used to demponstrate communications between the 
        // client and this DLL
        private int m_FrameCount = 0;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public MFTTantaVideoRotator_Sync() : base()
        {
              // DebugMessage("MFTTantaVideoRotator_Sync Constructor");
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Destructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        ~MFTTantaVideoRotator_Sync()
        {
            // DebugMessage("MFTTantaVideoRotator_Sync Destructor");

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

            // We assume the input type will get checked first
            if (OutputType == null)
            {
                // we do not have an output type, check that the proposed
                // input type is acceptable
                hr = OnCheckMediaType(pmt);
            }
            else
            {
                // we have an output type
                hr = IsMediaTypeIdentical(pmt, OutputType);
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
            // return the image size
            pStreamInfo.cbSize = m_cbImageSize;
            // MFT_INPUT_STREAM_WHOLE_SAMPLES - Each media sample(IMFSample interface) of 
            //    input data from the MFT contains complete, unbroken units of data. 
            // MFT_INPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER -  Each input sample contains 
            //    exactly one unit of data 
            // MFT_INPUT_STREAM_FIXED_SAMPLE_SIZE - All input samples are the same size.
            // MFT_INPUT_STREAM_PROCESSES_IN_PLACE - The MFT can perform in-place processing.
            //     In this mode, the MFT directly modifies the input buffer. When the client calls 
            //     ProcessOutput, the same sample that was delivered to this stream is returned in 
            //     the output stream that has a matching stream identifier. This flag implies that 
            //     the MFT holds onto the input buffer, so this flag cannot be combined with the 
            //     MFT_INPUT_STREAM_DOES_NOT_ADDREF flag. If this flag is present, the MFT must 
            //     set the MFT_OUTPUT_STREAM_PROVIDES_SAMPLES or MFT_OUTPUT_STREAM_CAN_PROVIDE_SAMPLES 
            //     flag for the output stream that corresponds to this input stream. 
            pStreamInfo.dwFlags = MFTInputStreamInfoFlags.WholeSamples |
                                  MFTInputStreamInfoFlags.FixedSampleSize |
                                  MFTInputStreamInfoFlags.SingleSamplePerBuffer |
                                  MFTInputStreamInfoFlags.ProcessesInPlace;
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
            // return the image size
            pStreamInfo.cbSize = m_cbImageSize;

            // MFT_OUTPUT_STREAM_WHOLE_SAMPLES - Each media sample(IMFSample interface) of 
            //    output data from the MFT contains complete, unbroken units of data. 
            // MFT_OUTPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER -  Each output sample contains 
            //    exactly one unit of data 
            // MFT_OUTPUT_STREAM_FIXED_SAMPLE_SIZE - All output samples are the same size.
            // MFT_OUTPUT_STREAM_PROVIDES_SAMPLES - The MFT provides the output samples 
            //    for this stream, either by allocating them internally or by operating 
            //    directly on the input samples. The MFT cannot use output samples provided 
            //    by the client for this stream. If this flag is not set, the MFT must 
            //    set cbSize to a nonzero value in the MFT_OUTPUT_STREAM_INFO structure, 
            //    so that the client can allocate the correct buffer size. For more information,
            //    see IMFTransform::GetOutputStreamInfo. This flag cannot be combined with 
            //    the MFT_OUTPUT_STREAM_CAN_PROVIDE_SAMPLES flag.
            pStreamInfo.dwFlags = MFTOutputStreamInfoFlags.WholeSamples |
                                  MFTOutputStreamInfoFlags.SingleSamplePerBuffer |
                                  MFTOutputStreamInfoFlags.FixedSampleSize |
                                  MFTOutputStreamInfoFlags.ProvidesSamples;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This is the routine that performs the transform. Assumes InputSample is set.
        ///
        /// An override of the abstract version in TantaMFTBase_Sync. 
        /// </summary>
        /// <param name="outputSampleDataStruct">The structure to populate with output data.</param>
        /// <returns>S_Ok unless error.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected override HResult OnProcessOutput(ref MFTOutputDataBuffer outputSampleDataStruct)
        {
            HResult hr = HResult.S_OK;
            IMFMediaBuffer outputMediaBuffer = null;

            // we are processing in place, the input sample is the output sample, the media buffer of the
            // input sample is the media buffer of the output sample.

            try
            {
                // Get the data buffer from the input sample. If the sample contains more than one buffer, 
                // this method copies the data from the original buffers into a new buffer, and replaces 
                // the original buffer list with the new buffer. The new buffer is returned in the inputMediaBuffer parameter.
                // If the sample contains a single buffer, this method returns a pointer to the original buffer. 
                // In typical use, most samples do not contain multiple buffers.
                hr = InputSample.ConvertToContiguousBuffer(out outputMediaBuffer);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OnProcessOutput call to InputSample.ConvertToContiguousBuffer failed. Err=" + hr.ToString());
                }

                // now that we have an output buffer, do the work to implement the appropriate rotate mode.
                // Writing into outputMediaBuffer will write to the approprate location in the outputSample
                FlipImageInBuffer(outputMediaBuffer);

                // increment this for the client/transform communications demonstrator code
                m_FrameCount++;

                // Set status flags.
                outputSampleDataStruct.dwStatus = MFTOutputDataBufferFlags.None;
                // The output sample is the input sample. We get a new IUnknown for the Input
                // sample since we are going to release it below. The client will release this 
                // new IUnknown
                outputSampleDataStruct.pSample = Marshal.GetIUnknownForObject(InputSample);

            }
            finally
            {
                // clean up
                SafeRelease(outputMediaBuffer);

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
            // our input type and output type must be the same
            return CreatePartialMediaType(dwTypeIndex, MFMediaType.Video, m_MediaSubtypes, out pInputType);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The MFT defines a list of available media types for each output stream
        /// and orders them by preference. This method enumerates the available
        /// media types for an output stream. 
        /// 
        /// The Tanta Transform Base is designed so that the output and input
        /// types are the same so we just use the same code as the input version
        ///
        /// An override of the virtual version in MFTBaseStandalone_Sync. 
        /// </summary>
        /// <param name="dwTypeIndex">The (zero-based) index of the type.</param>
        /// <param name="pOutputType">The output type supported by the MFT.</param>
        /// <returns>S_Ok unless error.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected override HResult OnEnumOutputTypes(int dwTypeIndex, out IMFMediaType pOutputType)
        {
            // our input type and output type must be the same, so first we try the base class
            // version. This will check for this.
            HResult hr = base.OnEnumOutputTypes(dwTypeIndex, out pOutputType);
            if ((hr == HResult.S_OK) && (pOutputType != null)) return hr;
            
            // input type not set, return what we would do for the input classes
            return CreatePartialMediaType(dwTypeIndex, MFMediaType.Video, m_MediaSubtypes, out pOutputType);
        }

        #endregion

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Called when the input type gets set. We record the basic image 
        ///  size and format information here.
        ///  
        ///  Expects the InputType variable to have been set. This will have been
        ///  done in the base class immediately before this routine gets called
        ///
        ///  An override of the virtual stub in TantaMFTBase_Sync. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        override protected void OnSetInputType()
        {
            HResult hr;

            // init some things
            m_imageWidthInPixels = 0;
            m_imageHeightInPixels = 0;
            m_cbImageSize = 0;
            m_lStrideIfContiguous = 0;

            // get this now, the type can be null to clear
            IMFMediaType pmt = InputType;
            if (pmt == null)
            {
                // Since the input must be set before the output, nulling the 
                // input must also clear the output.  Note that nulling the 
                // input is only valid if we are not actively streaming.
                OutputType = null;
                return;
            }

            // get the image width and height in pixels. These will become 
            // very important later when the time comes to size and center the 
            // text we will write on the screen.

            // Note that changing the size of the image on the screen, by resizing
            // the EVR control does not change this image size. The EVR will 
            // remove various rows and columns as necssary for display purposes
            hr = MFExtern.MFGetAttributeSize(pmt, MFAttributesClsid.MF_MT_FRAME_SIZE, out m_imageWidthInPixels, out m_imageHeightInPixels);
            if (hr != HResult.S_OK)
            {
                throw new Exception("OnSetInputType call to get the image size failed failed. Err=" + hr.ToString());
            }

            // get the image size
            hr = MFExtern.MFGetAttributeSize(pmt, MFAttributesClsid.MF_MT_FRAME_SIZE, out m_imageWidthInPixels, out m_imageHeightInPixels);
            if (hr != HResult.S_OK)
            {
                throw new Exception("OnSetInputType call to get MFGetAttributeSize failed failed. Err=" + hr.ToString());
            }

            // get the image stride. The stride is the number of bytes from one row of pixels in memory 
            // to the next row of pixels in memory. If padding bytes are present, the stride is wider 
            // than the width of the image.
            hr = pmt.GetUINT32(MFAttributesClsid.MF_MT_DEFAULT_STRIDE, out m_lStrideIfContiguous);
            if (hr != HResult.S_OK)
            {
                throw new Exception("OnSetInputType call to MF_MT_DEFAULT_STRIDE failed failed. Err=" + hr.ToString());
            }

            // Calculate the image size (including padding)
            m_cbImageSize = m_imageHeightInPixels * m_lStrideIfContiguous;

            OnSetOutputType();

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Called when the output type should be set. Since our output type must be the 
        ///  same as the input type we just create the output type as a copy of 
        ///  the input type here
        ///  
        ///  Expects the InputType variable to have been set.
        ///
        ///  An override of the virtual stub in TantaMFTBase_Sync. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected override void OnSetOutputType()
        {
            // If the output type is null or is being reset to null (by 
            // dynamic format change), pre-populate it.
            if (InputType != null && OutputType == null)
            {
                OutputType = CreateOutputFromInput();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Processes the input. Most of the transformation happens in OnProcessOutput
        /// so all we need to do here is check to see if the sample is interlaced and, if
        /// it is, discard it.
        /// 
        /// Expects InputSample to be set.
        /// </summary>
        /// <returns>S_Ok or E_FAIL.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported over
        /// </history>
        protected override HResult OnProcessInput()
        {
            HResult hr = HResult.S_OK;

            // While we accept types that *might* be interlaced, if we actually receive
            // an interlaced sample, reject it.
            if (m_MightBeInterlaced == true)
            {
                int ix;

                // Returns a bool: true = interlaced, false = progressive
                hr = InputSample.GetUINT32(MFAttributesClsid.MFSampleExtension_Interlaced, out ix);
                if (hr != HResult.S_OK || ix != 0)
                {
                    hr = HResult.E_FAIL;
                }
            }

            return hr;
        }

        #region Helpers

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Validates a media type for this transform. Since both input and output types must be
        /// the same, they both call this routine.
        /// </summary>
        /// <param name="pmt">The media type to validate.</param>
        /// <returns>S_Ok or MF_E_INVALIDTYPE.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported over
        /// </history>
        private HResult OnCheckMediaType(IMFMediaType pmt)
        {
            int interlace;
            HResult hr = HResult.S_OK;

            // see if the media type is one of our list of acceptable subtypes
            hr = CheckMediaType(pmt, MFMediaType.Video, m_MediaSubtypes);
            if (hr != HResult.S_OK)
            {
                throw new Exception("OnCheckMediaType call to TantaWMFUtils.CheckMediaType failed. Err=" + hr.ToString());
            }

            // Video must be progressive frames. Set this now
            m_MightBeInterlaced = false;

            // get the interlace mode
            hr = pmt.GetUINT32(MFAttributesClsid.MF_MT_INTERLACE_MODE, out interlace);
            if (hr != HResult.S_OK)
            {
                throw new Exception("OnCheckMediaType call to getting the interlace mode failed. Err=" + hr.ToString());
            }
            // set it now
            MFVideoInterlaceMode im = (MFVideoInterlaceMode)interlace;

            // Mostly we only accept Progressive.
            if (im == MFVideoInterlaceMode.Progressive) return HResult.S_OK;
            // If the type MIGHT be interlaced, we'll accept it.
            if (im == MFVideoInterlaceMode.MixedInterlaceOrProgressive)
            {
                // But we will check to see if any samples actually
                // are interlaced, and reject them.
                m_MightBeInterlaced = true;

                return HResult.S_OK;
            }
            // not a valid option
            return HResult.MF_E_INVALIDTYPE;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Setup the buffer and prepare to flip the image 
        /// </summary>
        /// <param name="outputMediaBuffer">Output buffer</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void FlipImageInBuffer(IMFMediaBuffer outputMediaBuffer)
        {
            IntPtr destRawDataPtr = IntPtr.Zero;			// Destination buffer.
            int destStride = 0;	                            // Destination stride.
            bool destIs2D = false;

            try
            {
                // Lock the output buffer. Use the IMF2DBuffer interface  
                // (if available) as it is faster
                if ((outputMediaBuffer is IMF2DBuffer) == false)
                {
                    // not an IMF2DBuffer - get the raw data from the IMFMediaBuffer 
                    int maxLen = 0;
                    int currentLen = 0;
                    LockIMFMediaBufferAndGetRawData(outputMediaBuffer, out destRawDataPtr, out maxLen, out currentLen);
                    // the stride is always this. The Lock function does not return it
                    destStride = m_lStrideIfContiguous;
                }
                else
                {
                    // we are an IMF2DBuffer, we get the stride here as well
                    LockIMF2DBufferAndGetRawData((outputMediaBuffer as IMF2DBuffer), out destRawDataPtr, out destStride);
                    destIs2D = true;
                }

                // We could eventually offer the ability to write on other formats depending on the 
                // current media type. We have this hardcoded to ARGB for now
                FlipImageOfTypeARGB(destRawDataPtr,
                                destStride,
                                m_imageWidthInPixels,
                                m_imageHeightInPixels);

                // Set the data size on the output buffer. It probably is already there
                // since the output buffer is the input buffer
                HResult hr = outputMediaBuffer.SetCurrentLength(m_cbImageSize);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("FlipImageInBuffer call to outputMediaBuffer.SetCurrentLength failed. Err=" + hr.ToString());
                }
            }
            finally
            {
                // we MUST unlock
                if (destIs2D == false) UnLockIMFMediaBuffer(outputMediaBuffer);
                else UnLockIMF2DBuffer((outputMediaBuffer as IMF2DBuffer));
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Flip an ARGB formatted image. The FlipMode is present in the attributes
        /// of the topology node controlling this MFT and it is set at runtime by 
        /// the client that adds this MFT to the pipleline. The FlipMode can be 
        /// changed dynamically while the video is running
        /// </summary>
        /// <param name="pDest">Pointer to the destination buffer.</param>
        /// <param name="lDestStride">Stride of the destination buffer, in bytes.</param>
        /// <param name="dwWidthInPixels">Frame width in pixels.</param>
        /// <param name="dwHeightInPixels">Frame height, in pixels.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void FlipImageOfTypeARGB(
            IntPtr pDest,
            int lDestStride,
            int dwWidthInPixels,
            int dwHeightInPixels
            )
        {

            // Although the actual data is down in unmanaged memory
            // we do not need to use "unsafe" access to get at it. 
            // The new BitMap() call does this for us. This is probably
            // only useful in this sort of rare circumstance. Normally
            // you have to copy it about. 

            // Get a Bitmap object as a wrapper around the video data.
            using (Bitmap workingBitmap = new Bitmap(m_imageWidthInPixels, m_imageHeightInPixels, m_lStrideIfContiguous, PixelFormat.Format32bppRgb, pDest))
            {
                RotateFlipType flipMode = FlipMode;

                // we now have the bitmap data from the frame copied 
                // into in a BitMap object. Now we do the rotate
                // which is easy with bitmaps. 
                workingBitmap.RotateFlip(flipMode);

                // At this point we have flipped the image according
                // to the desired rotation. Now we have to copy it
                // back into the destination unmanaged memory pointed
                // at by pDest. 

                // we need this rectangle object
                Rectangle rect = new Rectangle(0, 0, workingBitmap.Width, workingBitmap.Height);
                const int BYTES_PER_PIXEL = 4;

                // create a BitmapData object to receive the flipped Bitmap
                // data. Note how tmpBitMapData.Scan0 = pDest here!!!
                BitmapData tmpBitMapData = new BitmapData();
                tmpBitMapData.Width = rect.Width;
                tmpBitMapData.Height = rect.Height;
                tmpBitMapData.Stride = BYTES_PER_PIXEL * tmpBitMapData.Width;
                tmpBitMapData.PixelFormat = PixelFormat.Format32bppRgb;
                tmpBitMapData.Scan0 = pDest; // <<<<< NOTE this!!!
                tmpBitMapData.Reserved = 0;

                // lock the bits, this effectively copies them to 
                // the bmd object since we specify the ImageLockMode.UserInputBuffer
                // flag. This is what we want since tmpBitMapData.Scan0 = pDest - effectively
                // we are putting the flipped image back in unmanaged memory
                workingBitmap.LockBits(rect, ImageLockMode.ReadOnly | ImageLockMode.UserInputBuffer, PixelFormat.Format32bppRgb, tmpBitMapData);
                // actually the above call just put the changes in an internal buffer. This commits the copy
                workingBitmap.UnlockBits(tmpBitMapData); 
            }

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Get the FlipMode from this object. It located as an Attribute there. The
        /// client knows the topology node when it adds this MFT to the pipeline
        /// and so it can get access to the attributes of this class even though
        /// it was dynamically created via COM and adjust this attribute to control things.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private RotateFlipType FlipMode
        {
            get
            {
                IMFAttributes attributeContainer = null;

                // get the attribute container
                HResult hr = this.GetAttributes(out attributeContainer);
                if (hr != HResult.S_OK) return RotateFlipType.RotateNoneFlipNone;
                if (attributeContainer == null) return RotateFlipType.RotateNoneFlipNone;

                // we expect this to be a RotateFlipType enum. However, attributes
                // cannot have enums (just things like strings or ints or doubles
                // so the enum will have been casted to an int32 by the client.
                int enumAsInt = MFExtern.MFGetAttributeUINT32(attributeContainer, clsidFlipMode, (int)RotateFlipType.RotateNoneFlipNone);                
                // return the int as an enum
                return (RotateFlipType)enumAsInt;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Get/Set the current frame count. Note how this is ComVisible. This
        /// property can be used by a .NET client via Reflection and Late Binding
        /// to interact with the transform.
        /// 
        /// Note how this property is ComVisible This is not necessary to make it
        /// accessible to a .NET client via Reflection and Late Binding
        /// to interact with the transform but will make it visible to non-.NET
        /// clients via standard COM calls.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [ComVisible(true)]
        public int FrameCountAsPropertyDemonstrator
        {
            get
            {
/* diagnostic code
                string path = @"c:\Dump\RotatorDLLTest.txt";
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine("FrameCountAsPropertyDemonstrator get value=" + m_FrameCount.ToString());
                }
*/
                return m_FrameCount;
            }
            set
            {
                // set the new value
                m_FrameCount = value;

/* diagnostic code
                string path = @"c:\Dump\RotatorDLLTest.txt";
                using (StreamWriter sw = File.AppendText(path))
                {
                    sw.WriteLine("FrameCountAsPropertyDemonstrator set value=" + value.ToString());
                } */
            }

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Get the current frame count and prepend a user supplied string. The
        /// output is a string in a ref variable.
        /// 
        /// Note how this function is ComVisible This is not necessary to make it
        /// accessible to a .NET client via Reflection and Late Binding
        /// to interact with the transform but will make it visible to non-.NET
        /// clients via standard COM calls.
        /// </summary>
        /// <param name="frameCountLeadingText">the leading text to prepend. Cannot be null</param>
        /// <param name="outString">the string with the framecount is returned here</param>
        /// <returns>true the operation was successful, false it was not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [ComVisible(true)]
        public bool FrameCountAsFunctionDemonstrator(string frameCountLeadingText, ref string outString)
        {
            // we say the leading text cannot be null
            if (frameCountLeadingText == null)
            {
                outString = "";
                return false;
            }
            // set up the string
            outString = frameCountLeadingText + m_FrameCount.ToString();
            return true;
        }

        #endregion

        // ########################################################################
        // ##### COM Registration methods
        // ########################################################################

        #region COM Registration methods


        /// There are two registrations that need to happen in order to make an MFT
        /// available in the registry and discoverable by other WMF software. 
        /// 
        /// The first registration is to register this DLL as a COM object. This is 
        /// achieved by ticking the "Register for COM Interop" option on the Build
        /// tab of the properties of this project. This is why you need to start
        /// this solution as administrator - otherwise you will not have permission
        /// to update the registry. If you untick this box you will not need to be 
        /// an administrator and you will still get a dll but it will not be registered
        /// for COM. You can do this manually by using Regasm.exe with the /tlb 
        /// and /codebase options

        /// The second registration is to make the DLL discoverable by other WMF objects
        /// using the MFTEnumEx function. You would then, for example, be able to see
        /// the MFT in the TantaTransformPicker sample application. Doing this requires 
        /// call to the static MFExtern.MFTRegister function. This, of course, is a C# 
        /// function. 
        /// 
        /// It turns out that during the process of making a COM object available via
        /// the "Register for COM Interop" process, COM will make a call into the DLL 
        /// and execute two functions: one flagged with the [ComUnregisterFunctionAttribute] and
        /// the other flagged with the [ComRegisterFunctionAttribute] attribute (in that order). 
        /// This allows the DLL to perform specific actions needed to configure it.
        /// We use this to call MFExtern.MFTUnregister to remove the previous registration
        /// and then MFExtern.MFTRegister to register ourselves again. This also happens
        /// if you use the manual Regasm.exe method of COM registration.
        /// 
        /// The standard names for these functions are: DllRegisterServer and
        /// DllUnregisterServer - but they could be anything.

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Set up the function which will be automatically called by COM when this
        /// DLL is registered for COM interop. We use this call to register the MFT
        /// and make it available for discovery by other MFT applications.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        [ComRegisterFunctionAttribute]
        static private void DllRegisterServer(Type t)
        {

            HResult hr = MFExtern.MFTRegister(
                t.GUID,
                MFTransformCategory.MFT_CATEGORY_VIDEO_EFFECT,
                t.Name,
                MFT_EnumFlag.SyncMFT,
                0,
                null,
                0,
                null,
                null
                );
            MFError.ThrowExceptionForHR(hr);

/* this is just some diagnostic code so we can see the registration
 * and unregistration happening. There is no way to step through 
 * this function in a debugger
            string path = @"c:\Dump\RotatorDLL.txt";
            using (StreamWriter sw = File.AppendText(path))
            {
                sw.WriteLine("DllRegisterServer\r\n");
                sw.WriteLine(t.GUID + "\r\n");
                sw.WriteLine(t.Name + "\r\n");
            } */
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Set up the function which will be automatically called by COM when this
        /// DLL is registered for COM interop. We use this call to deregister the MFT
        /// and make it unavailable for discovery by other MFT applications.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        [ComUnregisterFunctionAttribute]
        static private void DllUnregisterServer(Type t)
        {

            HResult hr = MFExtern.MFTUnregister(t.GUID);

            // In Windows 7, MFTUnregister reports an error even if it succeeds:
            // https://social.msdn.microsoft.com/forums/windowsdesktop/en-us/7d3dc70f-8eae-4ad0-ad90-6c596cf78c80
            //MFError.ThrowExceptionForHR(hr);

/* this is just some diagnostic code so we can see the registration
* and unregistration happening. There is no way to step through 
* this function in a debugger
          string path = @"c:\Dump\RotatorDLL.txt";
           using (StreamWriter sw = File.AppendText(path))
           {
               sw.WriteLine("DllUnregisterServer");
           } */
       }

        #endregion
    }
}

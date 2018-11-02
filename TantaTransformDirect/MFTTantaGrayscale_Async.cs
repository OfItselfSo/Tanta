using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.Generic;

using MediaFoundation;
using MediaFoundation.Misc;
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

/// Almost all of this code are derived from a sample which ship with the MF.Net dll. These are 
/// in turn derived from the original Microsoft samples. These have been placed in the public domain 
/// without copyright. The original copyright statement is below

/// *****************************************************************************
/// Original Copyright Statement - Released to public domain
/// While the underlying library is covered by LGPL or BSD, this sample is released
/// as public domain.  It is distributed in the hope that it will be useful, but
/// WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
/// or FITNESS FOR A PARTICULAR PURPOSE.
/// ******************************************************************************

/// This file implements an Asynchronous Media Foundation Transform (MFT)
/// which converts video frames to grayscale as they pass through the transform.
/// 
/// It only supports three input types and the input type and output type
/// must be identical. 
/// 
/// This class uses the TantaMFTBase_Async base class and much of the 
/// standard processing is handled there. This base class is an only
/// slightly modified version of the AsyncMFTBase class which ships with the 
/// MF.Net samples. The AsyncMFTBase class (and hence TantaMFTBase_Async) is
/// designed to factor out all of the common code required to build an 
/// asynchronous MFT in C# MF.Net. 
/// 
/// In the interests of simplicity, this particular MFT is not designed
/// to be "independent" of the rest of the application. In other words,
/// it will not be placed in an independent assembly (DLL) it will not 
/// be COM visible or registered with MFTRegister so that other applications
/// can use it. This MFT is expected to be instantiated with a standard
/// C# new operator and simply given to the topology as a binary
/// 

namespace TantaTransformDirect
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// An MFT to turn video from color to grayscale. This code is almost all the   
    /// GrayscaleAsync code in the MF.Net sample code. It uses the TantaMFTBase_Async
    /// base class to do the majority of the work.  All the 'grayscale-specific'
    /// code is in this file, so you can use the template to easily create your
    /// own MFT.
    /// 
    /// This MFT can handle 3 different media types:
    /// 
    ///    YUY2
    ///    UYVY
    ///    NV12
    ///    
    /// A few things to know about the Grayscale MFT:
    /// 
    /// The output media type must be exactly equal to the input type and the
    /// input type must be set before the output type. 
    /// 
    /// This transform uses in-place processing, to see an example of copying
    /// the data from the source sample to the target sample see the 
    /// Grayscale_Sync Transform
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>
    public sealed class MFTTantaGrayscale_Async : TantaMFTBase_Async
    {
        // Format information
        private FourCC m_videoFOURCC; // type of samples being processed
        private int m_imageWidthInPixels;
        private int m_imageHeightInPixels;
        private int m_cbImageSize;              // Image size, in bytes.
        private int m_lStrideIfContiguous;

        // this function will be set when the negotiated input type is set
        // the actual mechanics of the conversion are, necessarily, highly
        // dependent on the input type. The output type is always the same 
        // as the input type
        private delegate void TransformImageDelegate(IntPtr pSrc, int lSrcStride, int dwWidthInPixels, int dwHeightInPixels);
        private TransformImageDelegate TransformImageFunction;

        // FOURCC is short for "four character code" - an identifier for a digital media format.
        // The MF.Net library contains a class named FourCC which provides comparison and manipulation
        // routines for the FOURCC code. One of the most useful of these is the generation of a 
        // standard GUID for the Media Subtype from the FOURCC code using a known standard algorythm
        private readonly static FourCC FOURCC_YUY2 = new FourCC('Y', 'U', 'Y', '2');
        private readonly static FourCC FOURCC_UYVY = new FourCC('U', 'Y', 'V', 'Y');
        private readonly static FourCC FOURCC_NV12 = new FourCC('N', 'V', '1', '2');

        // this list of the guids of the media subtypes we support. The input format must be the same
        // as the output format
        private readonly Guid[] m_MediaSubtypes;

        // we do not support interlacing. If the Media Type proposed by the client says
        // it "might be interlaced" we set this flag. If interlaced frames are passe in, we will reject them 
        private bool m_MightBeInterlaced;


        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor, The '1' indicates there should only be 1 processing thread.
        ///    While you *can* set this higher, Grayscaling doesn't really 
        ///    benefit from it.  See the TypeConverter for an MFT that does.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public MFTTantaGrayscale_Async() : base(1)
        {
            DebugMessage("MFTTantaGrayscale_Async Constructor");

            TransformImageFunction = null;

            // build our list of guids of the Media Subtypes
            // we are prepared to accept
            m_MediaSubtypes = new Guid[] { FOURCC_NV12.ToMediaSubtype(), FOURCC_YUY2.ToMediaSubtype(), FOURCC_UYVY.ToMediaSubtype() };
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Destructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        ~MFTTantaGrayscale_Async()
        {
            DebugMessage("MFTTantaGrayscale_Async Destructor");
        }

        #region Overrides

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Returns a value indicating if the proposed input type is acceptable to 
        /// this MFT.
        /// 
        /// An override of the abstract version in TantaMFTBase_Async. 
        /// </summary>
        /// <param name="pmt">The type to check.  Should never be null.</param>
        /// <returns>S_Ok if the type is valid or MF_E_INVALIDTYPE.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        override protected HResult OnCheckInputType(IMFMediaType pmt)
        {
            // We only check to see if the type is valid as an input type.  We
            // do NOT check if it is consistent with the current output type.
            // This is required in order to support dynamic format change (a 
            // requirement for Async MFTs).  Any incompatibility will be 
            // caught and handled if/when the type actually gets set (see 
            // MySetInput).

            HResult hr = HResult.S_OK;

            hr = OnCheckMediaType(pmt);

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Return settings to describe input stream. This should get the buffer 
        /// requirements and other information for an input stream. 
        /// (see IMFTransform::GetInputStreamInfo).
        /// 
        /// An override of the abstract version in TantaMFTBase_Async. 
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
            // MFT_INPUT_STREAM_FIXED_SAMPLE_SIZE - All input samples are the same size.
            // MFT_INPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER -  Each input sample contains 
            //    exactly one unit of data 
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
        /// An override of the abstract version in TantaMFTBase_Async. 
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
            //    so that the client can allocate the correct buffer size.For more information,
            //    see IMFTransform::GetOutputStreamInfo. This flag cannot be combined with 
            //    the MFT_OUTPUT_STREAM_CAN_PROVIDE_SAMPLES flag.
            pStreamInfo.dwFlags = MFTOutputStreamInfoFlags.WholeSamples |
                        MFTOutputStreamInfoFlags.SingleSamplePerBuffer |
                        MFTOutputStreamInfoFlags.FixedSampleSize |
                        MFTOutputStreamInfoFlags.ProvidesSamples;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This gets called out of the ProcessingThread() function in the 
        /// TantaMFTBase_Async base class. 
        /// 
        /// This function may be called from any one of the worker threads processing 
        /// inside of the base class and there may be multiple threads running at 
        /// the same time. The base class takes care to put the outputs samples from
        /// these calls back on the output queue in the proper order. This means that
        /// if your processing here is independent of any other sample then there is 
        /// nothing you need to worry about - things just happen in parallel.  If the
        /// processing of any one sample needs to be sychronised with other samples
        /// then you will have to arrange for this yourself. 
        /// 
        /// There are lengthy comments on the base class the ProcessingThread() function.
        /// It is recommended that you read them.
        /// 
        /// An override of the abstract version in TantaMFTBase_Async. 
        /// </summary>
        /// <param name="pStreamInfo">The struct where the parameters get set.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        override protected void OnProcessSample(IMFSample pInputSample, bool Discontinuity, int InputMessageNumber)
        {
            MFError throwonhr;

            IMFMediaBuffer pInput;

            // While we accept types that *might* be interlaced, if we actually receive
            // an interlaced sample, reject it.
            if (m_MightBeInterlaced)
            {
                int ix;

                // Returns a bool: true = interlaced, false = progressive
                throwonhr = pInputSample.GetUINT32(MFAttributesClsid.MFSampleExtension_Interlaced, out ix);

                if (ix != 0)
                {
                    SafeRelease(pInputSample);
                    return;
                }
            }

            // Set the Discontinuity flag on the sample that's going to OutputSample.
            HandleDiscontinuity(Discontinuity, pInputSample);

            // Get the data buffer from the input sample.  If the sample has
            // multiple buffers, you might be able to get (slightly) better
            // performance processing each buffer in turn rather than forcing
            // a new, full-sized buffer to get created.
            throwonhr = pInputSample.ConvertToContiguousBuffer(out pInput);

            try
            {
                // Process it.
                DoWork(pInput);

                // Send the modified input sample to the output sample queue.
                OutputSample(pInputSample, InputMessageNumber);
            }
            finally
            {
                // If (somewhere) there is .Net code that is holding on to an instance of
                // the same buffer as pInput, this will yank the RCW out from underneath 
                // it, probably causing it to crash.  But if we don't release it, our memory 
                // usage explodes.
                SafeRelease(pInput);
            }
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
        /// An override of the virtual version in TantaMFTBase_Async. 
        /// </summary>
        /// <param name="dwTypeIndex">The (zero-based) index of the type.</param>
        /// <param name="pInputType">The input type supported by the MFT.</param>
        /// <returns>S_Ok unless error.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        override protected HResult OnEnumInputTypes(int dwTypeIndex, out IMFMediaType pInputType)
        {
            return TantaWMFUtils.CreatePartialMediaType(dwTypeIndex, MFMediaType.Video, m_MediaSubtypes, out pInputType);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Called when the input type gets set. We record the basic image 
        ///  size and format information here.
        ///  
        ///  Expects the InputType variable to have been set. This will have been
        ///  done in the base class immediately before this routine gets called
        ///
        ///  An override of the virtual stub in TantaMFTBase_Async. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        override protected void OnSetInputType()
        {
            MFError throwonhr;

            m_imageWidthInPixels = 0;
            m_imageHeightInPixels = 0;
            m_videoFOURCC = new FourCC(0);
            m_cbImageSize = 0;
            m_lStrideIfContiguous = 0;

            TransformImageFunction = null;

            IMFMediaType pmt = InputType;

            // type can be null to clear
            if (pmt != null)
            {
                Guid subtype;

                throwonhr = pmt.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out subtype);

                throwonhr = MFExtern.MFGetAttributeSize(pmt, MFAttributesClsid.MF_MT_FRAME_SIZE, out m_imageWidthInPixels, out m_imageHeightInPixels);

                m_videoFOURCC = new FourCC(subtype);

                if (m_videoFOURCC == FOURCC_YUY2)
                {
                    TransformImageFunction = Update_YUY2;
                    m_lStrideIfContiguous = m_imageWidthInPixels * 2;
                }
                else if (m_videoFOURCC == FOURCC_UYVY)
                {
                    TransformImageFunction = Update_UYVY;
                    m_lStrideIfContiguous = m_imageWidthInPixels * 2;
                }
                else if (m_videoFOURCC == FOURCC_NV12)
                {
                    TransformImageFunction = Update_NV12;
                    m_lStrideIfContiguous = m_imageWidthInPixels;
                }
                else
                {
                    throw new COMException("Unrecognized type", (int)HResult.E_UNEXPECTED);
                }

                // Calculate the image size (not including padding)
                int lImageSize;
                if (Succeeded(pmt.GetUINT32(MFAttributesClsid.MF_MT_SAMPLE_SIZE, out lImageSize)))
                    m_cbImageSize = lImageSize;
                else
                    m_cbImageSize = GetImageSize(m_videoFOURCC, m_imageWidthInPixels, m_imageHeightInPixels);

                int lStrideIfContiguous;
                if (Succeeded(pmt.GetUINT32(MFAttributesClsid.MF_MT_DEFAULT_STRIDE, out lStrideIfContiguous)))
                    m_lStrideIfContiguous = lStrideIfContiguous;

                // If the output type isn't set yet, we can pre-populate it, 
                // since output must always exactly equal input.  This can 
                // save a (tiny) bit of time in negotiating types.

                OnSetOutputType();
            }
            else
            {
                // Since the input must be set before the output, nulling the 
                // input must also clear the output.  Note that nulling the 
                // input is only valid if we are not actively streaming.

                OutputType = null;
            }
        }


        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Called when the output type should be set. Since our output type must be the 
        ///  same as the input type we just create the output type as a copy of 
        ///  the input type here
        ///  
        ///  Expects the InputType variable to have been set.
        ///
        ///  An override of the virtual stub in TantaMFTBase_Async. 
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

        #endregion

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
            HResult hr;

            // Check the Major and Subtype
            hr = TantaWMFUtils.CheckMediaType(pmt, MFMediaType.Video, m_MediaSubtypes);
            if (Succeeded(hr))
            {
                int interlace;

                // Video must be progressive frames.
                m_MightBeInterlaced = false;

                MFError throwonhr = pmt.GetUINT32(MFAttributesClsid.MF_MT_INTERLACE_MODE, out interlace);

                MFVideoInterlaceMode im = (MFVideoInterlaceMode)interlace;

                // Mostly we only accept Progressive.
                if (im != MFVideoInterlaceMode.Progressive)
                {
                    // If the type MIGHT be interlaced, we'll accept it.
                    if (im != MFVideoInterlaceMode.MixedInterlaceOrProgressive)
                    {
                        hr = HResult.MF_E_INVALIDTYPE;
                    }
                    else
                    {
                        // But we will check to see if any samples actually
                        // are interlaced, and reject them.
                        m_MightBeInterlaced = true;
                    }
                }
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Calculates the buffer size needed, based on the video format.
        /// </summary>
        /// <param name="fcc">Video type</param>
        /// <param name="width">Frame width</param>
        /// <param name="height">Frame height</param>
        /// <returns>Size in bytes</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported over
        /// </history>
        private int GetImageSize(FourCC fcc, int width, int height)
        {
            int pcbImage;

            if ((fcc == FOURCC_YUY2) || (fcc == FOURCC_UYVY))
            {
                // check overflow
                if ((width > int.MaxValue / 2) ||
                    (width * 2 > int.MaxValue / height))
                {
                    throw new COMException("Bad size", (int)HResult.E_INVALIDARG);
                }

                // 16 bpp
                pcbImage = width * height * 2;
            }
            else if (fcc == FOURCC_NV12)
            {
                // check overflow
                if ((height / 2 > int.MaxValue - height) ||
                    ((height + height / 2) > int.MaxValue / width))
                {
                    throw new COMException("Bad size", (int)HResult.E_INVALIDARG);
                }

                // 12 bpp
                pcbImage = width * (height + (height / 2));
            }
            else
            {
                throw new COMException("Unrecognized type", (int)HResult.E_FAIL);
            }

            return pcbImage;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Get the buffers and sizes to be modified, then pass them
        ///  to the appropriate Update_* routine.
        /// </summary>
        /// <param name="inputMediaBuffer">the mediaBuffer</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported over
        /// </history>
        private void DoWork(IMFMediaBuffer inputMediaBuffer)
        {
            IntPtr srcRawDataPtr = IntPtr.Zero;			    // Source buffer.
            int srcStride;		                            // Source stride.
            bool srcIs2D = false;

            try
            {
                // Lock the input buffer. Use the IMF2DBuffer interface  
                // (if available) as it is faster
                if ((inputMediaBuffer is IMF2DBuffer) == false)
                {
                    // not an IMF2DBuffer - get the raw data from the IMFMediaBuffer 
                    int maxLen = 0;
                    int currentLen = 0;
                    TantaWMFUtils.LockIMFMediaBufferAndGetRawData(inputMediaBuffer, out srcRawDataPtr, out maxLen, out currentLen);
                    // the stride is always this. The Lock function does not return it
                    srcStride = m_lStrideIfContiguous;
                }
                else
                {
                    // we are an IMF2DBuffer, we get the stride here as well
                    TantaWMFUtils.LockIMF2DBufferAndGetRawData((inputMediaBuffer as IMF2DBuffer), out srcRawDataPtr, out srcStride);
                    srcIs2D = true;
                }
                // Invoke the image transform function.
                if (TransformImageFunction != null)
                {
                    TransformImageFunction(srcRawDataPtr, srcStride,
                        m_imageWidthInPixels, m_imageHeightInPixels);
                }
                else
                {
                    throw new COMException("Transform type not set", (int)HResult.E_UNEXPECTED);
                }

                // Set the data size on the output buffer.
                MFError throwonhr = inputMediaBuffer.SetCurrentLength(m_cbImageSize);
            }
            finally
            {
                if (srcIs2D == false) TantaWMFUtils.UnLockIMFMediaBuffer(inputMediaBuffer);
                else TantaWMFUtils.UnLockIMF2DBuffer((inputMediaBuffer as IMF2DBuffer));
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts an image in UYVY format to grayscale.
        /// </summary>
        /// <param name="pSrc">Pointer to the source buffer.</param>
        /// <param name="lSrcStride">Stride of the source buffer, in bytes.</param>
        /// <param name="dwWidthInPixels">Frame width in pixels.</param>
        /// <param name="dwHeightInPixels">Frame height, in pixels.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported over
        /// </history>
        unsafe private void Update_UYVY(
            IntPtr pSrc,
            int lSrcStride,
            int dwWidthInPixels,
            int dwHeightInPixels
            )
        {
            ushort* pSrc_Pixel = (ushort*)pSrc;
            int lMySrcStride = (lSrcStride / 2);  // lSrcStride is in bytes and we need words

            for (int y = 0; y < dwHeightInPixels; y++)
            {
                for (int x = 0; x < dwWidthInPixels; x++)
                {
                    // Byte order is U0 Y0 V0 Y1
                    // Each WORD is a byte pair (U/V, Y)
                    // Windows is little-endian so the order appears reversed.

                    pSrc_Pixel[x] = (ushort)((pSrc_Pixel[x] & 0xFF00) | 0x0080);
                }

                pSrc_Pixel += lMySrcStride;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts an image in YUY2 format to grayscale.
        /// </summary>
        /// <param name="pSrc">Pointer to the source buffer.</param>
        /// <param name="lSrcStride">Stride of the source buffer, in bytes.</param>
        /// <param name="dwWidthInPixels">Frame width in pixels.</param>
        /// <param name="dwHeightInPixels">Frame height, in pixels.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported over
        /// </history>
        unsafe private void Update_YUY2(
            IntPtr pSrc,
            int lSrcStride,
            int dwWidthInPixels,
            int dwHeightInPixels
            )
        {
            ushort* pSrc_Pixel = (ushort*)pSrc;
            int lMySrcStride = (lSrcStride / 2);  // lSrcStride is in bytes and we need words

            for (int y = 0; y < dwHeightInPixels; y++)
            {
                for (int x = 0; x < dwWidthInPixels; x++)
                {
                    // Byte order is Y0 U0 Y1 V0
                    // Each WORD is a byte pair (Y, U/V)
                    // Windows is little-endian so the order appears reversed.

                    pSrc_Pixel[x] = (ushort)((pSrc_Pixel[x] & 0x00FF) | 0x8000);
                }

                pSrc_Pixel += lMySrcStride;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts an image in NV12 format to grayscale.
        /// </summary>
        /// <param name="pSrc">Pointer to the source buffer.</param>
        /// <param name="lSrcStride">Stride of the source buffer, in bytes.</param>
        /// <param name="dwWidthInPixels">Frame width in pixels.</param>
        /// <param name="dwHeightInPixels">Frame height, in pixels.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported over
        /// </history>
        private void Update_NV12(
            IntPtr pSrc,
            int lSrcStride,
            int dwWidthInPixels,
            int dwHeightInPixels
            )
        {
            // NV12 is planar: Y plane, followed by packed U-V plane.

            if (lSrcStride != dwWidthInPixels)
            {
                // Y plane
                for (int y = 0; y < dwHeightInPixels; y++)
                {
                    pSrc += lSrcStride;
                }
                // U-V plane
                for (int y = 0; y < dwHeightInPixels / 2; y++)
                {
                    FillMemory(pSrc, dwWidthInPixels, 0x80);
                    pSrc += lSrcStride;
                }
            }
            else
            {
                int iSize = dwHeightInPixels * lSrcStride;

                FillMemory(pSrc + iSize, (dwHeightInPixels / 2) * lSrcStride, 0x80);
            }
        }

        #endregion

        #region Externs

        [DllImport("kernel32.dll"), System.Security.SuppressUnmanagedCodeSecurity]
        private static extern void FillMemory(IntPtr destination, int len, byte val);

        #endregion
    }

}

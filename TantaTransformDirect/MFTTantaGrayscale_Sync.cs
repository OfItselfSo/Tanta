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
/// which converts video frames to grayscale as they pass through the transform.
/// 
/// It only supports three input types and the input type and output type
/// must be identical. The transform creates new output buffers and copies
/// the data from the input buffer to the output buffer. If you wish to 
/// see an example of in-place processing in which the input sample is
/// passed back as the output sample see the MFTTantaWriteText_Sync example
/// 
/// This class uses the TantaMFTBase_Sync base class and much of the 
/// standard processing is handled there. This base class is an only
/// slightly modified version of the MFTBase class which ships with the 
/// MF.Net samples. The MFTBase class (and hence TantaMFTBase_Sync) is
/// designed to factor out all of the common code required to build an 
/// Synchronous MFT in C# MF.Net. 
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
    /// Grayscale-2015 in the MF.Net sample code. It uses the TantaMFTBase_Sync
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
    /// This buffer data is in unmanaged memory. Since we are copying data from
    /// the source to the target we have to use unsafe calls for performance reasons.
    /// This code must be compiled with the /UNSAFE flag
    /// 
    /// It would be MUCH more efficient to do in-place processing.  However, not
    /// all clients support it (or even check for it). For maximum portability,
    /// this MFT always uses separate output samples. To see in-place processing,
    /// look at MFTTantaWriteText_Sync. 
    /// 
    /// Because there are multiple input and output types this code uses
    /// the FourCC codes to keep track of what types are currently in use.
    /// This technique is quite interesting but not mandatory and the other
    /// sample TantaMFT's mostly do not use it.
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>
    public sealed class MFTTantaGrayscale_Sync : TantaMFTBase_Sync
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
        private delegate void TransformImageDelegate(IntPtr pDest, int lDestStride, IntPtr pSrc, int lSrcStride, int dwWidthInPixels, int dwHeightInPixels);
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
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public MFTTantaGrayscale_Sync() : base()
        {
            // DebugMessage("MFTTantaGrayscale_Sync Constructor");

            TransformImageFunction = null;

            // build our list of guids of the Media Subtypes
            // we are prepared to accept
            m_MediaSubtypes = new Guid[] { FOURCC_NV12.ToMediaSubtype(), FOURCC_YUY2.ToMediaSubtype(), FOURCC_UYVY.ToMediaSubtype() };
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
            // return the image size
            pStreamInfo.cbSize = m_cbImageSize;
            // MFT_INPUT_STREAM_WHOLE_SAMPLES - Each media sample(IMFSample interface) of 
            //    input data from the MFT contains complete, unbroken units of data. 
            // MFT_INPUT_STREAM_SINGLE_SAMPLE_PER_BUFFER -  Each input sample contains 
            //    exactly one unit of data 
            // MFT_INPUT_STREAM_FIXED_SAMPLE_SIZE - All input samples are the same size.
            pStreamInfo.dwFlags = MFTInputStreamInfoFlags.WholeSamples |
                                  MFTInputStreamInfoFlags.FixedSampleSize |
                                  MFTInputStreamInfoFlags.SingleSamplePerBuffer;

            // NOTE that we do NOT add the MFTInputStreamInfoFlags.ProcessesInPlace flag
            // since we do not do inplace processing here
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
            pStreamInfo.dwFlags = MFTOutputStreamInfoFlags.WholeSamples |
                                  MFTOutputStreamInfoFlags.SingleSamplePerBuffer |
                                  MFTOutputStreamInfoFlags.FixedSampleSize;

            // NOTE that we do NOT add the MFTOutputStreamInfoFlags.ProvidesSamples flag
            // since we do not do inplace processing here or create our own samples
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
            long hnsDuration;
            long hnsTime;
            HResult hr = HResult.S_OK;
            IMFMediaBuffer inputMediaBuffer = null;
            IMFMediaBuffer outputMediaBuffer = null;
            IMFSample outputSample = null;

            // Since we don't specify MFTOutputStreamInfoFlags.ProvidesSamples, this can't be null.
            // we expect the caller to have allocated the memory for this
            if (outputSampleDataStruct.pSample == IntPtr.Zero) return HResult.E_INVALIDARG;

            try
            {
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

                // Turn pointer into an interface. The GetUniqueObjectForIUnknown method ensures that we
                // receive a unique Runtime Callable Wrapper, because it does not match an IUnknown pointer 
                // to an existing object. Use this method when you have to create a unique RCW that is not 
                // impacted by other code that calls the ReleaseComObject method.
                outputSample = Marshal.GetUniqueObjectForIUnknown(outputSampleDataStruct.pSample) as IMFSample;
                if (outputSample ==  null)
                {
                    throw new Exception("OnProcessOutput call to GetUniqueObjectForIUnknown failed. outputSample ==  null");
                }

                // Now get the output buffer. A media sample contains zero or more buffers. Each buffer manages a block of 
                // memory, and is represented by the IMFMediaBuffer interface. A sample can have multiple buffers. 
                // The buffers are kept in an ordered list and accessed by index value. This call gets us a single
                // pointer to a single contigous buffer which is much more useful.
                hr = outputSample.ConvertToContiguousBuffer(out outputMediaBuffer);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OnProcessOutput call to InputSample.ConvertToContiguousBuffer failed. Err=" + hr.ToString());
                }

                // now that we have an input and output buffer, do the work to convert them to grayscale.
                // Writing into outputMediaBuffer will write to the approprate location in the outputSample
                // since we took care to Marshal it that way
                ConvertMediaBufferToGrayscale(inputMediaBuffer, outputMediaBuffer);

                // Set status flags.
                outputSampleDataStruct.dwStatus = MFTOutputDataBufferFlags.None;

                // Copy the duration from the input sample, if present. The 
                // Media Session needs these in order to keep everything sync'ed
                hr = InputSample.GetSampleDuration(out hnsDuration);
                if (hr == HResult.S_OK)
                {
                    hr = outputSample.SetSampleDuration(hnsDuration);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OnProcessOutput call to OutSample.SetSampleDuration failed. Err=" + hr.ToString());
                    }
                }

                // Copy the time stamp from the input sample, if present.
                hr = InputSample.GetSampleTime(out hnsTime);
                if (hr == HResult.S_OK)
                {
                    hr = outputSample.SetSampleTime(hnsTime);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OnProcessOutput call to OutSample.SetSampleTime failed. Err=" + hr.ToString());
                    }
                }
            }
            finally
            {
                // clean up
                SafeRelease(inputMediaBuffer);
                SafeRelease(outputMediaBuffer);
                SafeRelease(outputSample);

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
            return TantaWMFUtils.CreatePartialMediaType(dwTypeIndex, MFMediaType.Video, m_MediaSubtypes, out pInputType);
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
            int lImageSize;
            int lStrideIfContiguous;
            Guid subtype;
            HResult hr;

            // init some things
            m_imageWidthInPixels = 0;
            m_imageHeightInPixels = 0;
            m_videoFOURCC = new FourCC(0);
            m_cbImageSize = 0;
            m_lStrideIfContiguous = 0;
            TransformImageFunction = null;

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

            // get the subtype GUID
            hr = pmt.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out subtype);
            if (hr != HResult.S_OK)
            {
                throw new Exception("OnSetInputType call to get the subtype guid failed. Err=" + hr.ToString());
            }

            // get the image width and height in pixels. These will become 
            // very important later when the time comes to convert to grey scale
            // Note that changing the size of the image on the screen, by resizing
            // the EVR control does not change this image size. The EVR will 
            // remove various rows and columns as necssary for display purposes
            hr = MFExtern.MFGetAttributeSize(pmt, MFAttributesClsid.MF_MT_FRAME_SIZE, out m_imageWidthInPixels, out m_imageHeightInPixels);
            if (hr != HResult.S_OK)
            {
                throw new Exception("OnSetInputType call to get the image size failed failed. Err=" + hr.ToString());
            }

            // create a working FourCC subtype from the Guid. This only works because we can always
            // figure out the FOURCC code from the subtype Guid - it is the first four hex digits in 
            // reverse order
            m_videoFOURCC = new FourCC(subtype);

            // switch based on the supported formats and set our transform function
            // and other format specific based info
            if (m_videoFOURCC == FOURCC_YUY2)
            {
                TransformImageFunction = TransformImageOfTypeYUY2;
                m_lStrideIfContiguous = m_imageWidthInPixels * 2;
            }
            else if (m_videoFOURCC == FOURCC_UYVY)
            {
                TransformImageFunction = TransformImageOfTypeUYVY;
                m_lStrideIfContiguous = m_imageWidthInPixels * 2;
            }
            else if (m_videoFOURCC == FOURCC_NV12)
            {
                TransformImageFunction = TransformImageOfTypeNV12;
                m_lStrideIfContiguous = m_imageWidthInPixels;
            }
            else
            {
                throw new Exception("OnSetInputType Unrecognized type. Type =" + m_videoFOURCC.ToString());
            }

            // Calculate the image size (not including padding)
            hr = pmt.GetUINT32(MFAttributesClsid.MF_MT_SAMPLE_SIZE, out lImageSize);
            if (hr == HResult.S_OK)
            {
                // we have an attribute with the image size use that
                m_cbImageSize = lImageSize;
            }
            else
            {
                // we calculate the image size from the format
                m_cbImageSize = GetImageSize(m_videoFOURCC, m_imageWidthInPixels, m_imageHeightInPixels);
            }

            // get the default stride. The stride is the number of bytes from one row of pixels in memory 
            // to the next row of pixels in memory. If padding bytes are present, the stride is wider 
            // than the width of the image. We will need this if the frames come in IMFMediaBuffers, 
            // if the frames come in IMF2DBuffers we can get the stride directly from the buffer
            hr = pmt.GetUINT32(MFAttributesClsid.MF_MT_DEFAULT_STRIDE, out lStrideIfContiguous);
            if (hr != HResult.S_OK)
            {
                throw new Exception("OnSetInputType call to MFAttributesClsid.MF_MT_DEFAULT_STRIDE failed. Err=" + hr.ToString());
            }

            // we have an attribute with the default stride
            m_lStrideIfContiguous = lStrideIfContiguous;

            // If the output type isn't set yet, we can pre-populate it, 
            // since output must always exactly equal input.  This can 
            // save a (tiny) bit of time in negotiating types.

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
        /// so all we need to do here is check to see if the sample interlaced and, if
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
            hr = TantaWMFUtils.CheckMediaType(pmt, MFMediaType.Video, m_MediaSubtypes);
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
                if ((width > int.MaxValue / 2) || (width * 2 > int.MaxValue / height))
                {
                    throw new COMException("Bad size", (int)HResult.E_INVALIDARG);
                }

                // 16 bpp
                pcbImage = width * height * 2;
            }
            else if (fcc == FOURCC_NV12)
            {
                // check overflow
                if ((height / 2 > int.MaxValue - height) || ((height + height / 2) > int.MaxValue / width))
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
        /// Given the input and output media buffers, do the transform.
        /// </summary>
        /// <param name="inputMediaBuffer">Input buffer</param>
        /// <param name="outputMediaBuffer">Output buffer</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void ConvertMediaBufferToGrayscale(IMFMediaBuffer inputMediaBuffer, IMFMediaBuffer outputMediaBuffer)
        {
            IntPtr destRawDataPtr = IntPtr.Zero;			// Destination buffer.
            int destStride=0;	                            // Destination stride.
            bool destIs2D = false;

            IntPtr srcRawDataPtr = IntPtr.Zero;			    // Source buffer.
            int srcStride;		                            // Source stride.
            bool srcIs2D = false;

            if (TransformImageFunction == null) throw new COMException("Transform type not set", (int)HResult.E_UNEXPECTED);

            try
            {
                // Lock the output buffer. Use the IMF2DBuffer interface  
                // (if available) as it is faster
                if ((outputMediaBuffer is IMF2DBuffer) == false)
                {
                    // not an IMF2DBuffer - get the raw data from the IMFMediaBuffer 
                    int maxLen =0;
                    int currentLen = 0;
                    TantaWMFUtils.LockIMFMediaBufferAndGetRawData(outputMediaBuffer,  out destRawDataPtr, out maxLen, out currentLen);
                    // the stride is always this. The Lock function does not return it
                    destStride = m_lStrideIfContiguous;
                }
                else
                {
                    // we are an IMF2DBuffer, we get the stride here as well
                    TantaWMFUtils.LockIMF2DBufferAndGetRawData((outputMediaBuffer as IMF2DBuffer), out destRawDataPtr, out destStride);
                    destIs2D = true;
                }

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
                TransformImageFunction( destRawDataPtr, 
                                destStride,
                                srcRawDataPtr, 
                                srcStride,
                                m_imageWidthInPixels, 
                                m_imageHeightInPixels);

                // Set the data size on the output buffer.
                HResult hr = outputMediaBuffer.SetCurrentLength(m_cbImageSize);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("ConvertMediaBufferToGrayscale call to outputMediaBuffer.SetCurrentLength failed. Err=" + hr.ToString());
                }
            }
            finally
            {
                // we MUST unlock
                if(destIs2D == false) TantaWMFUtils.UnLockIMFMediaBuffer(outputMediaBuffer);
                else TantaWMFUtils.UnLockIMF2DBuffer((outputMediaBuffer as IMF2DBuffer));
                if (srcIs2D == false) TantaWMFUtils.UnLockIMFMediaBuffer(inputMediaBuffer);
                else TantaWMFUtils.UnLockIMF2DBuffer((inputMediaBuffer as IMF2DBuffer));
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Copy a UYVY formatted image to an output buffer while converting to grayscale.
        /// </summary>
        /// <param name="pDest">Pointer to the destination buffer.</param>
        /// <param name="lDestStride">Stride of the destination buffer, in bytes.</param>
        /// <param name="pSrc">Pointer to the source buffer.</param>
        /// <param name="lSrcStride">Stride of the source buffer, in bytes.</param>
        /// <param name="dwWidthInPixels">Frame width in pixels.</param>
        /// <param name="dwHeightInPixels">Frame height, in pixels.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        unsafe private void TransformImageOfTypeUYVY(
            IntPtr pDest,
            int lDestStride,
            IntPtr pSrc,
            int lSrcStride,
            int dwWidthInPixels,
            int dwHeightInPixels
            )
        {
            // This routine uses unsafe pointers for performance reasons.
            // If you don't know what unsafe pointers are then you
            // should look it up. Spoiler alert: they are not as 
            // "unsafe" as the word would imply - unsafe is just a 
            // specific c# technical term. 

            // Remember the actual data is down in unmanaged memory
            // there does not seem to be an efficient "safe" way to copy
            // unmanaged memory to unmanaged memory without spooling it
            // through a temporary managed store - and this is slow.

            // if you really needed optimal performance here you could 
            // adjust the transform code (and base class) to produce "in-place" 
            // processing and modify the input buffer rather than 
            // having the transform create its own output buffers
            // and copy the data there

            ushort* pSrc_Pixel = (ushort*)pSrc;
            ushort* pDest_Pixel = (ushort*)pDest;
            int lMySrcStride = (lSrcStride / 2);  // lSrcStride is in bytes and we need words
            int lMyDestStride = (lDestStride / 2); // lSrcStride is in bytes and we need words

            for (int y = 0; y < dwHeightInPixels; y++)
            {
                for (int x = 0; x < dwWidthInPixels; x++)
                {
                    // Byte order is U0 Y0 V0 Y1
                    // Each WORD is a byte pair (U/V, Y)
                    // Windows is little-endian so the order appears reversed.

                    pDest_Pixel[x] = (ushort)((pSrc_Pixel[x] & 0xFF00) | 0x0080);
                }

                pSrc_Pixel += lMySrcStride;
                pDest_Pixel += lMyDestStride;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Copy a YUY2 formatted image to an output buffer while converting to grayscale.
        /// </summary>
        /// <param name="pDest">Pointer to the destination buffer.</param>
        /// <param name="lDestStride">Stride of the destination buffer, in bytes.</param>
        /// <param name="pSrc">Pointer to the source buffer.</param>
        /// <param name="lSrcStride">Stride of the source buffer, in bytes.</param>
        /// <param name="dwWidthInPixels">Frame width in pixels.</param>
        /// <param name="dwHeightInPixels">Frame height, in pixels.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        unsafe private void TransformImageOfTypeYUY2(
            IntPtr pDest,
            int lDestStride,
            IntPtr pSrc,
            int lSrcStride,
            int dwWidthInPixels,
            int dwHeightInPixels
            )
        {
            // This routine uses unsafe pointers for performance reasons.
            // If you don't know what unsafe pointers are then you
            // should look it up. Spoiler alert: they are not as 
            // "unsafe" as the word would imply - unsafe is just a 
            // specific c# technical term. 

            // Remember the actual data is down in unmanaged memory
            // there does not seem to be an efficient "safe" way to copy
            // unmanaged memory to unmanaged memory without spooling it
            // through a temporary managed store - and this is slow.

            // if you really needed optimal performance here you could 
            // adjust the transform code (and base class) to produce "in-place" 
            // processing and modify the input buffer rather than 
            // having the transform create its own output buffers
            // and copy the data there

            ushort* pSrc_Pixel = (ushort*)pSrc;
            ushort* pDest_Pixel = (ushort*)pDest;
            int lMySrcStride = (lSrcStride / 2);  // lSrcStride is in bytes and we need words
            int lMyDestStride = (lDestStride / 2); // lSrcStride is in bytes and we need words

            for (int y = 0; y < dwHeightInPixels; y++)
            {
                for (int x = 0; x < dwWidthInPixels; x++)
                {
                    // Byte order is Y0 U0 Y1 V0 
                    // Each WORD is a byte pair (Y, U/V)
                    // Windows is little-endian so the order appears reversed.

                    pDest_Pixel[x] = (ushort)((pSrc_Pixel[x] & 0x00FF) | 0x8000);
                }

                pSrc_Pixel += lMySrcStride;
                pDest_Pixel += lMyDestStride;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Copy a NV12 formatted image to an output buffer while converting to grayscale.
        /// </summary>
        /// <param name="pDest">Pointer to the destination buffer.</param>
        /// <param name="lDestStride">Stride of the destination buffer, in bytes.</param>
        /// <param name="pSrc">Pointer to the source buffer.</param>
        /// <param name="lSrcStride">Stride of the source buffer, in bytes.</param>
        /// <param name="dwWidthInPixels">Frame width in pixels.</param>
        /// <param name="dwHeightInPixels">Frame height, in pixels.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        private void TransformImageOfTypeNV12(
            IntPtr pDest,
            int lDestStride,
            IntPtr pSrc,
            int lSrcStride,
            int dwWidthInPixels,
            int dwHeightInPixels
            )
        {
            // in this code we do not need to indulge in "unsafe" pointer
            // manipulations because NV12 is planar ( Y plane, followed
            // by packed U-V plane), we can just copy the planes we need
            // and fill the rest with dummy data

            // Y plane
            for (int y = 0; y < dwHeightInPixels; y++)
            {
                CopyMemory(pDest, pSrc, dwWidthInPixels);
                pDest = new IntPtr(pDest.ToInt64() + lDestStride);
                pSrc = new IntPtr(pSrc.ToInt64() + lSrcStride);
            }

            // U-V plane
            for (int y = 0; y < dwHeightInPixels / 2; y++)
            {
                FillMemory(pDest, dwWidthInPixels, 0x80);
                pDest = new IntPtr(pDest.ToInt64() + lDestStride);
            }
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

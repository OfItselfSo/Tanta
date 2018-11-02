using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

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
/// which simply counts the frames as they pass through the transform.
/// It is about the simplest possible transform it is possible to make.
/// The base class for this code is TantaMFTBase_Sync. This is an only
/// slightly modified version of the MFTBase class which ships with the 
/// MF.Net samples. The MFTBase class (and hence TantaMFTBase_Sync) is
/// designed to factor much of the common code required to build an 
/// Synchronous MFT in C# MF.Net. You should not need to modify the 
/// base class code in order to create a new MFT. Everything you need 
/// to change  will be in the derived class. 
/// 
/// In the interests of simplicity, this particular MFT is not designed
/// to be "independent" of the rest of the application. In other words,
/// it will not be placed in an independent assembly (DLL) it will not 
/// be COM visible or registered with MFTRegister so that other applications
/// can use it. This MFT is expected to be instantiated with a standard
/// C# new operator and simply given to the topology as a binary

namespace TantaTransformDirect
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// An MFT which counts frames. The input type and the output type are 
    /// the same for this MFT. We are doing "inplace" processing on the frames
    /// and simply giving the input data back to the output and doing nothing
    /// to it. This means we can support any type as long as we can enforce
    /// that our input type is the same as the output type. 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>
    public sealed class MFTTantaFrameCounter_Sync : TantaMFTBase_Sync
    {
        // this is the frame count, since we have one frame per sample
        private int m_FrameCount;
        // if you look at other sample TantaMFT's you will see this gets
        // populated with the types we support. Since we support all types
        // this can be empty and we handle it in the overrides below
        private readonly Guid[] m_MediaSubtypes = new Guid[] { Guid.Empty };

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public MFTTantaFrameCounter_Sync() : base()
        {
           // DebugMessage("MFTTantaFrameCounter Constructor");

            m_FrameCount = 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the frame count. Note this is public so the client can see it!
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally written
        /// </history>
        public int FrameCount
        {
            get
            {
                return m_FrameCount;
            }
            set
            {
                m_FrameCount = value;
            }
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
        /// This is the routine that performs the transform. All we do here is count
        /// the sample and pass it on. 
        ///
        /// An override of the abstract version in TantaMFTBase_Sync. 
        /// </summary>
        /// <param name="pOutputSamples">The structure to populate with output values.</param>
        /// <returns>S_Ok unless error.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        protected override HResult OnProcessOutput(ref MFTOutputDataBuffer pOutputSamples)
        {
            HResult hr = HResult.S_OK;

            if (pOutputSamples.pSample == IntPtr.Zero)
            {
 
                // this is the line that does the work. Simple isn't it?
                m_FrameCount++;

                // Actually there is more going on here than is obvious. We have an
                // Input sample supplied earlier, that sample will contain a MediaBuffer which
                // in turn will contain the data. If we were doing anything with that data we could
                // modify it now and pass it back. However, as you will note when you look at the 
                // other sample transforms, the data is in unmanaged memory space and so
                // will need to be brought up into an object C# can work on. This can be done
                // via a variety of methods but "unsafe" copies are generally used for speed 
                // reasons.

                // We are doing in-place processing, the output sample is the input sample.
                pOutputSamples.pSample = Marshal.GetIUnknownForObject(InputSample);

                // the output data does not have to be (optinally modified) and handed right 
                // back as the output data. We could create a new output sample and copy the 
                // data across to it. The TantaMFTGrayscale does this. However the
                // OnGetInputStreamInfo and OnGetOutputStreamInfo overrides would have to
                // be modified so the client knows that we are creating our own samples.

                // Release the current input sample so we can get another one.
                // the act of setting it to null releases it because the property
                // is coded that way
                InputSample = null;
            }
            else
            {
                hr = HResult.E_INVALIDARG;
            }

            return hr;
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

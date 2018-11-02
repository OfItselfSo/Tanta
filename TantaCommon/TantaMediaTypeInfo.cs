using System;
using System.Text;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MediaFoundation;
using MediaFoundation.ReadWrite;
using MediaFoundation.Misc;
using MediaFoundation.Transform;
using MediaFoundation.Alt;
using OISCommon;

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
/// Some parts of this code may be derived directly from the Microsoft examples and are 
/// presumed to be in the public domain. 

/// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
/// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
/// <summary>
/// A class to enumerate media devices and return information based on them
/// </summary>
/// <history>
///    01 Nov 18  Cynic - Started
/// </history>
namespace TantaCommon 
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// Constructor
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public class TantaMediaTypeInfo : OISObjBase
    {

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all supported video formats from a video source device
        /// as a nice displayable bit of text. outSb will never be null but can be
        /// empty. There will be one line per mediaType
        /// 
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="sourceReader">the source reader object</param>
        /// <param name="maxFormatsToTestFor">the max number of formats we test for</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult GetSupportedVideoFormatsFromSourceReaderAsText(IMFSourceReader sourceReader, int maxFormatsToTestFor, out StringBuilder outSb)
        {
            IMFMediaType mediaTypeObj=null;
            HResult hr;

            // we always return something here
            outSb = new StringBuilder();

            // sanity check
            if (sourceReader == null) return HResult.E_FAIL;

            try
            {
                for (int typeIndex = 0; typeIndex < maxFormatsToTestFor; typeIndex++)
                {
                    // test this
                    hr = sourceReader.GetNativeMediaType(TantaWMFUtils.MF_SOURCE_READER_FIRST_VIDEO_STREAM, typeIndex, out mediaTypeObj);
                    if (hr == HResult.MF_E_NO_MORE_TYPES)
                    {
                        // we are all done. The outSb container has been populated
                        return HResult.S_OK;
                    }
                    else if (hr != HResult.S_OK)
                    {
                        // we failed
                        throw new Exception("GetSupportedVideoFormatsFromSourceReaderAsText failed on call to GetNativeMediaType, retVal=" + hr.ToString());
                    }

                    // get the formats for this type
                    StringBuilder tmpSb;
                    hr = GetSupportedFormatsFromMediaTypeAsText(mediaTypeObj, out tmpSb);
                    if (hr != HResult.S_OK)
                    {
                        // we failed
                        throw new Exception("GetSupportedVideoFormatsFromSourceReaderAsText failed on call to GetSupportedFormatsFromMediaTypeAsText, retVal=" + hr.ToString());
                    }
                    // add it here
                    outSb.Append(typeIndex.ToString() + " ");
                    outSb.Append(tmpSb);
                    outSb.Append("\r\n");
                    outSb.Append("\r\n");

                }
            }
            finally
            {
                // always release our mediaType if we have one
                if (mediaTypeObj != null)
                {
                    Marshal.ReleaseComObject(mediaTypeObj);
                    mediaTypeObj = null;
                }
            }

            // all done
            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all supported video formats from a video source device
        /// as a list of TantaMFVideoFormatContainer's
        /// 
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="currentDevice">the video device that created the source reader</param>
        /// <param name="sourceReader">the source reader object</param>
        /// <param name="maxFormatsToTestFor">the max number of formats we test for</param>
        /// <param name="formatList">the list of video formats supported by the SourceReader</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult GetSupportedVideoFormatsFromSourceReaderInFormatContainers(TantaMFDevice currentDevice, IMFSourceReaderAsync sourceReader, int maxFormatsToTestFor, out List<TantaMFVideoFormatContainer> formatList)
        {
            IMFMediaType mediaTypeObj = null;
            HResult hr;

            // init this, we never return null here
            formatList = new List<TantaMFVideoFormatContainer>();

            // sanity check
            if (currentDevice == null) return HResult.E_FAIL;
            if (sourceReader == null) return HResult.E_FAIL;

            try
            {
                for (int typeIndex = 0; typeIndex < maxFormatsToTestFor; typeIndex++)
                {
                    // test this
                    hr = sourceReader.GetNativeMediaType(TantaWMFUtils.MF_SOURCE_READER_FIRST_VIDEO_STREAM, typeIndex, out mediaTypeObj);
                    if (hr == HResult.MF_E_NO_MORE_TYPES)
                    {
                        // we are all done. The outSb container has been populated
                        return HResult.S_OK;
                    }
                    else if (hr != HResult.S_OK)
                    {
                        // we failed
                        throw new Exception("GetSupportedVideoFormatsFromSourceReaderInFormatContainers failed on call to GetNativeMediaType, retVal=" + hr.ToString());
                    }

                    // get a format container from the media type
                    TantaMFVideoFormatContainer tmpContainer = GetVideoFormatContainerFromMediaTypeObject(mediaTypeObj, currentDevice);
                    if (tmpContainer == null)
                    {
                        // we failed
                        throw new Exception("GetSupportedVideoFormatsFromSourceReaderInFormatContainers failed on call to GetVideoFormatContainerFromMediaTypeObject");
                    }
                    // now add it
                    formatList.Add(tmpContainer);
                }
            }
            finally
            {
                // always release our mediaType if we have one
                if (mediaTypeObj != null)
                {
                    Marshal.ReleaseComObject(mediaTypeObj);
                    mediaTypeObj = null;
                }
            }

            // all done
            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a TantaMFVideoFormatContainer from an IMFMediaType
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="mediaTypeObj">the media type object</param>
        /// <param name="currentDevice">the video device that created the source reader</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static TantaMFVideoFormatContainer GetVideoFormatContainerFromMediaTypeObject(IMFMediaType mediaTypeObj, TantaMFDevice currentDevice)
        {
            HResult hr;
            Guid majorType;
            Guid subType;
            int attributeCount;
            int frameSizeWidth;
            int frameSizeHeight;
            int frameRate;
            int frameRateDenominator;
            int frameRateMin;
            int frameRateMinDenominator;
            int frameRateMax;
            int frameRateMaxDenominator;

            if (mediaTypeObj == null)
            {
                // we failed
                throw new Exception("GetVideoFormatContainerFromMediaTypeObject failedmediaTypeObj == null");
            }

            if (currentDevice == null)
            {
                // we failed
                throw new Exception("GetVideoFormatContainerFromMediaTypeObject currentDevice == null");
            }

            // get the formats for this type
            hr = GetSupportedFormatsFromMediaType(mediaTypeObj, out majorType, out subType, out attributeCount, out frameSizeWidth, out frameSizeHeight, out frameRate, out frameRateDenominator, out frameRateMin, out frameRateMinDenominator, out frameRateMax, out frameRateMaxDenominator);
            if (hr != HResult.S_OK)
            {
                // we failed
                throw new Exception("GetSupportedVideoFormatsFromSourceReaderInFormatContainers failed on call to GetSupportedFormatsFromMediaType, retVal=" + hr.ToString());
            }

            // enumerate all of the possible Attributes so we can see which ones are present that we did not report on
            StringBuilder allAttrs = new StringBuilder();
            hr = EnumerateAllAttributeNamesInMediaTypeAsText(mediaTypeObj, attributeCount, out allAttrs);
            if (hr != HResult.S_OK)
            {
                // we failed
                throw new Exception("GetSupportedVideoFormatsFromSourceReaderInFormatContainers failed on call to EnumerateAllAttributeNamesInMediaTypeAsText, retVal=" + hr.ToString());
            }

            // create the container here
            TantaMFVideoFormatContainer tmpContainer = new TantaMFVideoFormatContainer();
            tmpContainer.MajorType = majorType;
            tmpContainer.SubType = subType;
            tmpContainer.AttributeCount = attributeCount;
            tmpContainer.FrameSizeWidth = frameSizeWidth;
            tmpContainer.FrameSizeHeight = frameSizeHeight;
            tmpContainer.FrameRate = frameRate;
            tmpContainer.FrameRateDenominator = frameRateDenominator;
            tmpContainer.FrameRateMin = frameRateMin;
            tmpContainer.FrameRateMinDenominator = frameRateMinDenominator;
            tmpContainer.FrameRateMax = frameRateMax;
            tmpContainer.FrameRateMaxDenominator = frameRateMaxDenominator;
            tmpContainer.AllAttributes = allAttrs.ToString();
            // we also record the video device here - it is useful to have
            tmpContainer.VideoDevice = currentDevice;

            return tmpContainer;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all supported video formats from a media type
        /// as a nice displayable bit of text. outSb will never be null can be
        /// empty.
        /// 
        /// Adapted from
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ee663602(v=vs.85).aspx
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="mediaTypeObj">the media type object</param>
        /// <param name="outSb">The output string</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult GetSupportedFormatsFromMediaType(IMFMediaType mediaTypeObj, out Guid majorType, out Guid subType, out int attributeCount, out int frameSizeWidth, out int frameSizeHeight, out int frameRate, out int frameRateDenominator, out int frameRateMin, out int frameRateMinDenominator, out int frameRateMax, out int frameRateMaxDenominator)
        {
            // init these
            majorType = Guid.Empty;
            subType = Guid.Empty;
            attributeCount = 0;
            frameSizeWidth = 0;
            frameSizeHeight = 0;
            frameRate = 0;
            frameRateDenominator = 0;
            frameRateMin = 0;
            frameRateMinDenominator = 0;
            frameRateMax = 0;
            frameRateMaxDenominator = 0;

            // sanity check
            if (mediaTypeObj == null) return HResult.E_FAIL;

            // Retrieves the number of attributes that are set on this object.
            HResult hr = mediaTypeObj.GetCount(out attributeCount);
            if (hr != HResult.S_OK)
            {
                // if we failed here, bail out
                return HResult.E_FAIL;
            }
            // put in this line now
            //   outSb.Append("attributeCount=" + attributeCount.ToString()+", ");

            // MF_MT_MAJOR_TYPE
            // Major type GUID, we return this as human readable text
            hr = mediaTypeObj.GetMajorType(out majorType);
            if (hr != HResult.S_OK)
            {
                // if we failed here, bail out
                return HResult.E_FAIL;
            }

            // MF_MT_SUBTYPE
            // Subtype GUID which describes the basic media type, we return this as human readable text
            hr = mediaTypeObj.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out subType);
            if (hr != HResult.S_OK)
            {
                // if we failed here, bail out
                return HResult.E_FAIL;
            }

            // MF_MT_FRAME_SIZE
            // the Width and height of a video frame, in pixels
            hr = MFExtern.MFGetAttributeSize(mediaTypeObj, MFAttributesClsid.MF_MT_FRAME_SIZE, out frameSizeWidth, out frameSizeHeight);
            if (hr != HResult.S_OK)
            {
                // if we failed here, bail out
                return HResult.E_FAIL;
            }

            // MF_MT_FRAME_RATE
            // The frame rate is expressed as a ratio.The upper 32 bits of the attribute value contain the numerator and the lower 32 bits contain the denominator. 
            // For example, if the frame rate is 30 frames per second(fps), the ratio is 30 / 1.If the frame rate is 29.97 fps, the ratio is 30,000 / 1001.
            // we report this back to the user as a decimal
            hr = MFExtern.MFGetAttributeRatio(mediaTypeObj, MFAttributesClsid.MF_MT_FRAME_RATE, out frameRate, out frameRateDenominator);
            if (hr != HResult.S_OK)
            {
                // if we failed here, bail out
                return HResult.E_FAIL;
            }

            // MF_MT_FRAME_RATE_RANGE_MIN
            // The frame rate is expressed as a ratio.The upper 32 bits of the attribute value contain the numerator and the lower 32 bits contain the denominator. 
            // For example, if the frame rate is 30 frames per second(fps), the ratio is 30 / 1.If the frame rate is 29.97 fps, the ratio is 30,000 / 1001.
            // we report this back to the user as a decimal
            hr = MFExtern.MFGetAttributeRatio(mediaTypeObj, MFAttributesClsid.MF_MT_FRAME_RATE_RANGE_MIN, out frameRateMin, out frameRateMinDenominator);
            if (hr != HResult.S_OK)
            {
                // if we failed here, bail out
                return HResult.E_FAIL;
            }

            // MF_MT_FRAME_RATE_RANGE_MAX
            // The frame rate is expressed as a ratio.The upper 32 bits of the attribute value contain the numerator and the lower 32 bits contain the denominator. 
            // For example, if the frame rate is 30 frames per second(fps), the ratio is 30 / 1.If the frame rate is 29.97 fps, the ratio is 30,000 / 1001.
            // we report this back to the user as a decimal
            hr = MFExtern.MFGetAttributeRatio(mediaTypeObj, MFAttributesClsid.MF_MT_FRAME_RATE_RANGE_MAX, out frameRateMax, out frameRateMaxDenominator);
            if (hr != HResult.S_OK)
            {
                // if we failed here, bail out
                return HResult.E_FAIL;
            }

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all supported video formats from a media type
        /// as a nice displayable bit of text. outSb will never be null can be
        /// empty.
        /// 
        /// Adapted from
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ee663602(v=vs.85).aspx
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="mediaTypeObj">the media type object</param>
        /// <param name="outSb">The output string</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult GetSupportedFormatsFromMediaTypeAsText(IMFMediaType mediaTypeObj, out StringBuilder outSb)
        {
            Guid majorType;
            Guid subType;
            int attributeCount;
            int frameSizeWidth;
            int frameSizeHeight;
            int frameRate;
            int frameRateDenominator;
            int frameRateMin;
            int frameRateMinDenominator;
            int frameRateMax;
            int frameRateMaxDenominator;

            // we always return something here
            outSb = new StringBuilder();

            // sanity check
            if (mediaTypeObj == null) return HResult.E_FAIL;

            // Retrieves the number of attributes that are set on this object.
            HResult hr = mediaTypeObj.GetCount(out attributeCount);
            if (hr != HResult.S_OK)
            {
                // if we failed here, bail out
                outSb.Append("failed getting attributeCount, retVal=" + hr.ToString());
                outSb.Append("\r\n");
                return HResult.E_FAIL;
            }
            // put in this line now
         //   outSb.Append("attributeCount=" + attributeCount.ToString()+", ");

            // MF_MT_MAJOR_TYPE
            // Major type GUID, we return this as human readable text
            hr = mediaTypeObj.GetMajorType(out majorType);
            if (hr == HResult.S_OK)
            {
                // only report success
                outSb.Append("MF_MT_MAJOR_TYPE=" + TantaWMFUtils.ConvertGuidToName(majorType) + ", ");
            }

            // MF_MT_SUBTYPE
            // Subtype GUID which describes the basic media type, we return this as human readable text
            hr = mediaTypeObj.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out subType);
            if (hr == HResult.S_OK)
            {
                // only report success
                outSb.Append("MF_MT_SUBTYPE=" + TantaWMFUtils.ConvertGuidToName(subType) + ", ");
            }
 
            // MF_MT_FRAME_SIZE
            // the Width and height of a video frame, in pixels
            hr = MFExtern.MFGetAttributeSize(mediaTypeObj, MFAttributesClsid.MF_MT_FRAME_SIZE, out frameSizeWidth, out frameSizeHeight);
            if (hr == HResult.S_OK)
            {
                // only report success
                outSb.Append("MF_MT_FRAME_SIZE (W,H)=(" + frameSizeWidth.ToString() + "," + frameSizeHeight.ToString() + "), ");
            }

            // MF_MT_FRAME_RATE
            // The frame rate is expressed as a ratio.The upper 32 bits of the attribute value contain the numerator and the lower 32 bits contain the denominator. 
            // For example, if the frame rate is 30 frames per second(fps), the ratio is 30 / 1.If the frame rate is 29.97 fps, the ratio is 30,000 / 1001.
            // we report this back to the user as a decimal
            hr = MFExtern.MFGetAttributeRatio(mediaTypeObj, MFAttributesClsid.MF_MT_FRAME_RATE, out frameRate, out frameRateDenominator);
            if (hr == HResult.S_OK)
            {
                // only report success
                if (frameRateDenominator < 0)
                {
                    outSb.Append("MF_MT_FRAME_RATE (frames/s)=(undefined),");
                }
                else
                {
                    outSb.Append("MF_MT_FRAME_RATE=" + ((decimal)frameRate / (decimal)frameRateDenominator).ToString() + "f/s, ");
                }
            }

            // MF_MT_FRAME_RATE_RANGE_MIN
            // The frame rate is expressed as a ratio.The upper 32 bits of the attribute value contain the numerator and the lower 32 bits contain the denominator. 
            // For example, if the frame rate is 30 frames per second(fps), the ratio is 30 / 1.If the frame rate is 29.97 fps, the ratio is 30,000 / 1001.
            // we report this back to the user as a decimal
            hr = MFExtern.MFGetAttributeRatio(mediaTypeObj, MFAttributesClsid.MF_MT_FRAME_RATE_RANGE_MIN, out frameRateMin, out frameRateMinDenominator);
            if (hr == HResult.S_OK)
            {
                // only report success
                if (frameRateMinDenominator < 0)
                {
                    outSb.Append("MF_MT_FRAME_RATE_RANGE_MIN (frames/s)=(undefined),");
                }
                else
                {
                    outSb.Append("MF_MT_FRAME_RATE_RANGE_MIN=" + ((decimal)frameRateMin / (decimal)frameRateMinDenominator).ToString() + "f/s, ");
                }
            }

            // MF_MT_FRAME_RATE_RANGE_MAX
            // The frame rate is expressed as a ratio.The upper 32 bits of the attribute value contain the numerator and the lower 32 bits contain the denominator. 
            // For example, if the frame rate is 30 frames per second(fps), the ratio is 30 / 1.If the frame rate is 29.97 fps, the ratio is 30,000 / 1001.
            // we report this back to the user as a decimal
            hr = MFExtern.MFGetAttributeRatio(mediaTypeObj, MFAttributesClsid.MF_MT_FRAME_RATE_RANGE_MAX, out frameRateMax, out frameRateMaxDenominator);
            if (hr == HResult.S_OK)
            {
                // only report success
                if (frameRateMaxDenominator < 0)
                {
                    outSb.Append("MF_MT_FRAME_RATE_RANGE_MAX (frames/s)=(undefined),");
                }
                else
                {
                    outSb.Append("MF_MT_FRAME_RATE_RANGE_MAX=" + ((decimal)frameRateMax / (decimal)frameRateMaxDenominator).ToString() + "f/s, ");
                }
            }

            // enumerate all of the possible Attributes so we can see which ones are present that we did not report on
            StringBuilder allAttrs = new StringBuilder();
            hr = EnumerateAllAttributeNamesInMediaTypeAsText(mediaTypeObj, attributeCount, out allAttrs);
            if (hr == HResult.S_OK)
            {
                outSb.Append("\r\n");
                outSb.Append("         AllAttrs=" + allAttrs.ToString());

            }

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the major media type of a IMFMediaType as a text string
        /// 
        /// Adapted from
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ee663602(v=vs.85).aspx
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="mediaTypeObj">the media type object</param>
        /// <param name="outSb">The output string</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult GetMediaMajorTypeAsText(IMFMediaType mediaTypeObj, out StringBuilder outSb)
        {
            Guid majorType;
            HResult hr;

            // we always return something here
            outSb = new StringBuilder();

            // sanity check
            if (mediaTypeObj == null) return HResult.E_FAIL;

            // MF_MT_MAJOR_TYPE
            // Major type GUID, we return this as human readable text
            hr = mediaTypeObj.GetMajorType(out majorType);
            if (hr == HResult.S_OK)
            {
                // only report success
                outSb.Append("MF_MT_MAJOR_TYPE=" + TantaWMFUtils.ConvertGuidToName(majorType));
            }

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the major media type of a IMFMediaType as a text string
        /// 
        /// Adapted from
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ee663602(v=vs.85).aspx
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="mediaTypeObj">the media type object</param>
        /// <param name="outSb">The output string</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult GetMediaSubTypeAsText(IMFMediaType mediaTypeObj, out StringBuilder outSb)
        {
            Guid subType;
            HResult hr;

            // we always return something here
            outSb = new StringBuilder();

            // sanity check
            if (mediaTypeObj == null) return HResult.E_FAIL;

            // MF_MT_SUBTYPE
            // Subtype GUID which describes the basic media type, we return this as human readable text
            hr = mediaTypeObj.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out subType);
            if (hr == HResult.S_OK)
            {
                // only report success
                outSb.Append("MF_MT_SUBTYPE=" + TantaWMFUtils.ConvertGuidToName(subType));
            }

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all attributes contained in a media type and displays
        /// them as a human readable name. More or less just for practice
        /// 
        /// Adapted from
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ee663602(v=vs.85).aspx
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="mediaTypeObj">the media type object</param>
        /// <param name="maxAttributes">the maximum number of attributes</param>
        /// <param name="outSb">The output string</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult EnumerateAllAttributeNamesInMediaTypeAsText(IMFMediaType mediaTypeObj, int maxAttributes, out StringBuilder outSb)
        {
            return EnumerateAllAttributeNamesInMediaTypeAsText(mediaTypeObj, false, false, maxAttributes, out outSb);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all attributes contained in a media type and displays
        /// them as a human readable name. More or less just for practice
        /// 
        /// Adapted from
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ee663602(v=vs.85).aspx
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="mediaTypeObj">the media type object</param>
        /// <param name="maxAttributes">the maximum number of attributes</param>
        /// <param name="outSb">The output string</param>
        /// <param name="ignoreMajorType">if true we ignore the major type attribute</param>
        /// <param name="ignoreSubType">if true we ignore the sub type attribute</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult EnumerateAllAttributeNamesInMediaTypeAsText(IMFMediaType mediaTypeObj, bool ignoreMajorType, bool ignoreSubType, int maxAttributes, out StringBuilder outSb)
        {

            // we always return something here
            outSb = new StringBuilder();

            // sanity check
            if (mediaTypeObj == null) return HResult.E_FAIL;
            if ((mediaTypeObj is IMFAttributes) == false) return HResult.E_FAIL;

            // set up to ignore
            List<string> attributesToIgnore = new List<string>();
            if (ignoreMajorType == true) attributesToIgnore.Add("MF_MT_MAJOR_TYPE");
            if (ignoreSubType == true) attributesToIgnore.Add("MF_MT_SUBTYPE");

            // just call the generic TantaWMFUtils Attribute Enumerator
            return TantaWMFUtils.EnumerateAllAttributeNamesAsText((mediaTypeObj as IMFAttributes), attributesToIgnore, maxAttributes, out outSb);
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace TantaCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// A class to contain Video Format Information 
    ///
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public class TantaMFVideoFormatContainer : OISObjBase, IComparable
    {
        private TantaMFDevice videoDevice = null;
        private Guid majorType = Guid.Empty;
        private Guid subType = Guid.Empty;
        private int attributeCount = 0;
        private int frameSizeWidth = 0;
        private int frameSizeHeight = 0;
        private int frameRate = 0;
        private int frameRateDenominator = 0;
        private int frameRateMin = 0;
        private int frameRateMinDenominator = 0;
        private int frameRateMax = 0;
        private int frameRateMaxDenominator = 0;
        private string allAttributes = "";

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public TantaMFVideoFormatContainer()
        {
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Override the ToString()
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public override string ToString()
        {
            // just provide the subtype - we already know it is of type video
            return SubTypeAsString;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A display summary string
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string DisplayString()
        {
            return SubTypeAsString + " " + FrameSizeAsString;
        }


        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the SubType as a string
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string SubTypeAsString
        {
            get
            {
                return TantaWMFUtils.ConvertGuidToName(SubType);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the FrameSize as a string
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string FrameSizeAsString
        {
            get
            {
                return "(" + frameSizeWidth.ToString() + "," + frameSizeHeight.ToString() + ")";
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the FrameRate as a string
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string FrameRateAsString
        {
            get
            {
                if (frameRateDenominator <= 0)
                {
                    return "(undefined)";
                }
                else
                {
                    return Math.Round(((decimal)frameRate / (decimal)frameRateDenominator)).ToString();
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the FrameRateMin as a string
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string FrameRateMinAsString
        {
            get
            {
                if (frameRateMinDenominator <= 0)
                {
                    return "(undefined)";
                }
                else
                {
                    return Math.Round(((decimal)frameRateMin / (decimal)frameRateMinDenominator)).ToString();
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the FrameRateMax as a string
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string FrameRateMaxAsString
        {
            get
            {
                if (frameRateMaxDenominator <= 0)
                {
                    return "(undefined)";
                }
                else
                {
                    return Math.Round(((decimal)frameRateMax / (decimal)frameRateMaxDenominator)).ToString();
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/sets the allAttributes as a string. Never gets/sets null.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string AllAttributes
        {
            get
            {
                if (allAttributes == null) allAttributes = "";
                return allAttributes;
            }
            set
            {
                allAttributes = value;
                if (allAttributes == null) allAttributes = "";
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/sets the video device which owns these formats. Will get/set null.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public TantaMFDevice VideoDevice
        {
            get
            {
                return videoDevice;
            }
            set
            {
                videoDevice = value;
            }
        }
        
        // get and set accessors
        public Guid MajorType { get { return  majorType; } set { majorType = value; } }
        public Guid SubType { get { return  subType; } set { subType = value; } }
        public int AttributeCount { get { return  attributeCount; } set { attributeCount = value; } }
        public int FrameSizeWidth { get { return  frameSizeWidth; } set { frameSizeWidth = value; } }
        public int FrameSizeHeight { get { return  frameSizeHeight; } set { frameSizeHeight = value; } }
        public int FrameRate { get { return  frameRate; } set { frameRate = value; } }
        public int FrameRateDenominator { get { return  frameRateDenominator; } set { frameRateDenominator = value; } }
        public int FrameRateMin { get { return  frameRateMin; } set { frameRateMin = value; } }
        public int FrameRateMinDenominator { get { return  frameRateMinDenominator; } set { frameRateMinDenominator = value; } }
        public int FrameRateMax { get { return  frameRateMax; } set { frameRateMax = value; } }
        public int FrameRateMaxDenominator { get { return  frameRateMaxDenominator; } set { frameRateMaxDenominator = value; } }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// IComparable implementation
        /// </summary>
        /// <param name="obj">Our container to compare to</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            if ((obj is TantaMFVideoFormatContainer) == false) return 2;

            if (MajorType != (obj as TantaMFVideoFormatContainer).MajorType) return 10;
            if (SubType != (obj as TantaMFVideoFormatContainer).SubType) return 11;
            if (AttributeCount != (obj as TantaMFVideoFormatContainer).AttributeCount) return 12;
            if (FrameSizeWidth != (obj as TantaMFVideoFormatContainer).FrameSizeWidth) return 13;
            if (FrameSizeHeight != (obj as TantaMFVideoFormatContainer).FrameSizeHeight) return 14;
            if (FrameRate != (obj as TantaMFVideoFormatContainer).FrameRate) return 15;
            if (FrameRateDenominator != (obj as TantaMFVideoFormatContainer).FrameRateDenominator) return 16;
            if (FrameRateMin != (obj as TantaMFVideoFormatContainer).FrameRateMin) return 17;
            if (FrameRateMinDenominator != (obj as TantaMFVideoFormatContainer).FrameRateMinDenominator) return 18;
            if (FrameRateMax != (obj as TantaMFVideoFormatContainer).FrameRateMax) return 19;
            if (FrameRateMaxDenominator != (obj as TantaMFVideoFormatContainer).FrameRateMaxDenominator) return 20;
            if (AllAttributes != (obj as TantaMFVideoFormatContainer).AllAttributes) return 21;
 
            // now compare the video devices
            if ((this.VideoDevice == null) && ((obj as TantaMFVideoFormatContainer).VideoDevice == null)) return 0;
            if ((this.VideoDevice == null) && ((obj as TantaMFVideoFormatContainer).VideoDevice != null)) return 30;
            if ((this.VideoDevice != null) && ((obj as TantaMFVideoFormatContainer).VideoDevice == null)) return 40;
            // compare the two devices
            return this.VideoDevice.CompareTo((obj as TantaMFVideoFormatContainer).VideoDevice);
        }

    }
}

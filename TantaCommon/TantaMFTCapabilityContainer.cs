using System;
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

namespace TantaCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// A class to correlate the capabilities of a Transform. A lot of these
    /// are not derivable from the transform itself - they have to be obtained
    /// elsewhere. This class makes it possible to transport this information around
    /// the system
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public class TantaMFTCapabilityContainer : OISObjBase, IComparable
    {
        private string mftCategoryFriendlyName = "";
        private Guid mftCategoryGuidValue = Guid.Empty;
        private string transformFriendlyName = "";
        private Guid transformGuidValue = Guid.Empty;
        MFT_EnumFlag enumFlags = MFT_EnumFlag.None;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="transformFriendlyNameIn">the transformFriendlyName</param>
        /// <param name="transformGuidValueIn">the guid value </param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public TantaMFTCapabilityContainer(string transformFriendlyNameIn, Guid transformGuidValueIn)
        {
            TransformFriendlyName = transformFriendlyNameIn;
            TransformGuidValue = transformGuidValueIn;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="transformFriendlyNameIn">the transformFriendlyName</param>
        /// <param name="transformGuidValueIn">the guid value </param>
        /// <param name="mftCategoryFriendlyNameIn">the mftCategoryFriendlyName</param>
        /// <param name="mftCategoryGuidValueIn">the guid value </param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public TantaMFTCapabilityContainer(string transformFriendlyNameIn, Guid transformGuidValueIn, string mftCategoryFriendlyNameIn, Guid mftCategoryGuidValueIn)
        {
            TransformFriendlyName = transformFriendlyNameIn;
            TransformGuidValue = transformGuidValueIn;
            MFTCategoryFriendlyName = mftCategoryFriendlyNameIn;
            MFTCategoryGuidValue = mftCategoryGuidValueIn;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="transformFriendlyNameIn">the transformFriendlyName</param>
        /// <param name="transformGuidValueIn">the guid value </param>
        /// <param name="mftCategoryGuidNamePairIn">the category guid name pair</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public TantaMFTCapabilityContainer(string transformFriendlyNameIn, Guid transformGuidValueIn, TantaGuidNamePair mftCategoryGuidNamePairIn)
        {
            TransformFriendlyName = transformFriendlyNameIn;
            TransformGuidValue = transformGuidValueIn;
            if (mftCategoryGuidNamePairIn != null)
            {
                MFTCategoryFriendlyName = mftCategoryGuidNamePairIn.FriendlyName;
                MFTCategoryGuidValue = mftCategoryGuidNamePairIn.GuidValue;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the friendly name. Never gets/sets null. Will return empty.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string TransformFriendlyName
        {
            get
            {
                // we never return null
                if (transformFriendlyName == null) transformFriendlyName = "";
                return transformFriendlyName;
            }
            set
            {
                transformFriendlyName = value;
                if (transformFriendlyName == null) transformFriendlyName = "";
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the guid value. Will return Guid.Empty.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public Guid TransformGuidValue
        {
            get
            {
                return transformGuidValue;
            }
            set
            {
                transformGuidValue = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the guid value. Will return Guid.Empty.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string TransformGuidValueAsString
        {
            get
            {
                return transformGuidValue.ToString();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the friendly name. Never gets/sets null. Will return empty.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string MFTCategoryFriendlyName
        {
            get
            {
                // we never return null
                if (mftCategoryFriendlyName == null) mftCategoryFriendlyName = "";
                return mftCategoryFriendlyName;
            }
            set
            {
                mftCategoryFriendlyName = value;
                if (mftCategoryFriendlyName == null) mftCategoryFriendlyName = "";
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the guid value. Will return Guid.Empty.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public Guid MFTCategoryGuidValue
        {
            get
            {
                return mftCategoryGuidValue;
            }
            set
            {
                mftCategoryGuidValue = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the guid value. Will return Guid.Empty.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public string MFTCategoryGuidValueAsString
        {
            get
            {
                return mftCategoryGuidValue.ToString();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The ToString() override.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public override string ToString()
        {
            return TransformFriendlyName;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// IComparable implementation
        /// </summary>
        /// <param name="obj">Our object to compare to</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;
            if ((obj is TantaMFTCapabilityContainer) == false) return 2;

            if (TransformGuidValue != (obj as TantaMFTCapabilityContainer).TransformGuidValue) return 20;
            if (TransformFriendlyName != (obj as TantaMFTCapabilityContainer).TransformFriendlyName) return 30;
            if (MFTCategoryGuidValue != (obj as TantaMFTCapabilityContainer).MFTCategoryGuidValue) return 20;
            if (MFTCategoryFriendlyName != (obj as TantaMFTCapabilityContainer).MFTCategoryFriendlyName) return 30;

            return 0;
        }

        // get/set out Enum Flags
        public MFT_EnumFlag EnumFlags { get { return enumFlags; } set { enumFlags = value; } }

        // get out flag values as a boolean
        public string IsSyncMFT { get { if ((EnumFlags & MFT_EnumFlag.SyncMFT) != 0) return "x"; else return ""; } }
        public string IsAsyncMFT { get { if ((EnumFlags & MFT_EnumFlag.AsyncMFT) != 0) return "x"; else return ""; } }
        public string IsHardware { get { if ((EnumFlags & MFT_EnumFlag.Hardware) != 0) return "x"; else return ""; } }
        public string IsFieldOfUse { get { if ((EnumFlags & MFT_EnumFlag.FieldOfUse) != 0) return "x"; else return ""; } }
        public string IsLocalMFT { get { if ((EnumFlags & MFT_EnumFlag.LocalMFT) != 0) return "x"; else return ""; } }
        public string IsTranscodeOnly { get { if ((EnumFlags & MFT_EnumFlag.TranscodeOnly) != 0) return "x"; else return ""; } }

    }
}

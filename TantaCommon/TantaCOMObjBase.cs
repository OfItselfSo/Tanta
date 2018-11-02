using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OISCommon;
using MediaFoundation;
using System.Runtime.InteropServices;

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
/// Some of this code is derived from the samples which ship with the MF.Net dll. 
/// These have been placed in the public domain without copyright.

namespace TantaCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// A class to act as a base class for COM objects in the Tanta Library.
    /// Replaces COMBase in the MF.Net samples and inherits from OISBase
    /// so we can use its logging mechanisms.
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>    
    public class TantaCOMObjBase : OISObjBase
    {
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A standard test for the success of an HResult. Ported 
        /// straight out of the MF.Net sample code COMBase class
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>    
        public static bool Succeeded(HResult hr)
        {
            return hr >= 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A standard test for the failure of an HResult. Ported 
        /// straight out of the MF.Net sample code COMBase class
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>    
        public static bool Failed(HResult hr)
        {
            return hr < 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A standard safe release of an object. Ported 
        /// straight out of the MF.Net sample code COMBase class
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>    
        public static void SafeRelease(object o)
        {
            if (o != null)
            {
                if (Marshal.IsComObject(o))
                {
                    int i = Marshal.ReleaseComObject(o);
                }
                else
                {
                    IDisposable iDis = o as IDisposable;
                    if (iDis != null)
                    {
                        iDis.Dispose();
                    }
                    else
                    {
                        throw new Exception("SafeRelease: iDis != null");
                    }
                }
            }
        }
    }
}

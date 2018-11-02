using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using MediaFoundation;
using MediaFoundation.Misc;
using MediaFoundation.Transform;
using OISCommon;

// +------------------------------------------------------------------------------------------------------------------------------+
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

/// This code has been derived from the AsyncMFTBase sample which ships with the MF.Net dll. These have 
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
    /// This class acts as a wrapper around TantaMFTAsyncMessageTypeEnum + event parameters. Used
    /// in Async Transforms
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>
    public class TantaMFTAsyncMessageHolder : OISObjBase
    {
        public IMFSample sample;
        public IntPtr ptr;
        public IMFMediaType dt;
        public TantaMFTAsyncMessageTypeEnum mt;
        public bool bDiscontinuity;
        public int InputNumber;

        public TantaMFTAsyncMessageHolder(TantaMFTAsyncMessageTypeEnum pmt)
        {
            mt = pmt;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Show something usable in debug window.
        /// </summary>
        /// <returns></returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public override string ToString()
        {
            return string.Format("{0}: {1}", InputNumber, mt);
        }

    }
}

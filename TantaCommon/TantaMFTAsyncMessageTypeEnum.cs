using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TantaCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// The types of message in a TantaMFTAsyncMessageHolder. Used by the 
    /// m_inSamples Queue to describe what type of event just got Dequeued.
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Ported In
    /// </history>
    public enum TantaMFTAsyncMessageTypeEnum
    {
        Sample,
        Drain,
        Flush,
        Marker,
        Format,
        Shutdown
    }
}

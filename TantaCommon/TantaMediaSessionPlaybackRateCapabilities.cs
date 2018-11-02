using System;
using System.Collections.Generic;
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
    /// A helper class to determine and present the playback rate capabilities. 
    /// 
    /// NOTE: Reverse Playback rates are positive here (you have to multiply them
    /// by -1 to use them). We just values less than zero to indicate undefined
    /// values.
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public class TantaMediaSessionPlaybackRateCapabilities : OISObjBase
    {
        public const float UNKNOWN_PLAYBACK_RATE = -1.0f;

        // determines if we have been able to successfully request
        // the capabilities from the session
        private bool capabilityRequestSuccessful = false;

        // nonThinned speeds
        private bool reverseSpeedIsSupportedNonThinned = false;
        private float fastestForwardSpeedNonThinned = UNKNOWN_PLAYBACK_RATE;
        private float slowestForwardSpeedNonThinned = UNKNOWN_PLAYBACK_RATE;
        private float fastestReverseSpeedNonThinned = UNKNOWN_PLAYBACK_RATE;
        private float slowestReverseSpeedNonThinned = UNKNOWN_PLAYBACK_RATE;

        // Thinned speeds
        private bool reverseSpeedIsSupportedThinned = false;
        private float fastestForwardSpeedThinned = UNKNOWN_PLAYBACK_RATE;
        private float slowestForwardSpeedThinned = UNKNOWN_PLAYBACK_RATE;
        private float fastestReverseSpeedThinned = UNKNOWN_PLAYBACK_RATE;
        private float slowestReverseSpeedThinned = UNKNOWN_PLAYBACK_RATE;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public TantaMediaSessionPlaybackRateCapabilities()
        {

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="mediaSession">the media session. If not null, we will  
        /// populate this object from the session</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public TantaMediaSessionPlaybackRateCapabilities(IMFMediaSession mediaSession)
        {
            // get the playback capabilities
            AcquirePlayBackRates(mediaSession);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the playback rate capabilitys for the session
        /// </summary>
        /// <param name="mediaSession">the media session. If not null, we will  
        /// populate this object from the session</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>        
        public bool AcquirePlayBackRates(IMFMediaSession mediaSession)
        {
            bool outBool;
            bool wantThinned;
            float supportedRate;

            Reset();

            if (mediaSession == null) return false;

            try
            {
                // first we acquire the thinned rates
                wantThinned = true;

                outBool = TantaWMFUtils.GetFastestRate(mediaSession, MFRateDirection.Forward, wantThinned, out supportedRate);
                if (outBool == true) fastestForwardSpeedThinned = Math.Abs(supportedRate);
                outBool = TantaWMFUtils.GetSlowestRate(mediaSession, MFRateDirection.Forward, wantThinned, out supportedRate);
                if (outBool == true) slowestForwardSpeedThinned = Math.Abs(supportedRate);

                // now test for the reverse being possible with this thinning mode
                outBool = TantaWMFUtils.IsRewindSupported(mediaSession, wantThinned);
                if (outBool == true)
                {
                    reverseSpeedIsSupportedThinned = true;
                    outBool = TantaWMFUtils.GetFastestRate(mediaSession, MFRateDirection.Reverse, wantThinned, out supportedRate);
                    if (outBool == true) fastestReverseSpeedThinned = Math.Abs(supportedRate);
                    outBool = TantaWMFUtils.GetSlowestRate(mediaSession, MFRateDirection.Reverse, wantThinned, out supportedRate);
                    if (outBool == true) slowestReverseSpeedThinned = Math.Abs(supportedRate);
                }

                // next we acquire the thinned rates
                wantThinned = false;

                outBool = TantaWMFUtils.GetFastestRate(mediaSession, MFRateDirection.Forward, wantThinned, out supportedRate);
                if (outBool == true) fastestForwardSpeedNonThinned = Math.Abs(supportedRate);
                outBool = TantaWMFUtils.GetSlowestRate(mediaSession, MFRateDirection.Forward, wantThinned, out supportedRate);
                if (outBool == true) slowestForwardSpeedNonThinned = Math.Abs(supportedRate);

                // now test for the reverse being possible with this thinning mode
                outBool = TantaWMFUtils.IsRewindSupported(mediaSession, wantThinned);
                if (outBool == true)
                {
                    reverseSpeedIsSupportedNonThinned = true;
                    outBool = TantaWMFUtils.GetFastestRate(mediaSession, MFRateDirection.Reverse, wantThinned, out supportedRate);
                    if (outBool == true) fastestReverseSpeedNonThinned = Math.Abs(supportedRate);
                    outBool = TantaWMFUtils.GetSlowestRate(mediaSession, MFRateDirection.Reverse, wantThinned, out supportedRate);
                    if (outBool == true) slowestReverseSpeedNonThinned = Math.Abs(supportedRate);
                }

                capabilityRequestSuccessful = true;
            }
            catch
            {
                capabilityRequestSuccessful = false;
            }
            return CapabilityRequestSuccessful;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Resets the object to the defaults
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public void Reset()
        {
            capabilityRequestSuccessful = false;

            // nonThinned speeds
            reverseSpeedIsSupportedNonThinned = false;
            fastestForwardSpeedNonThinned = UNKNOWN_PLAYBACK_RATE;
            slowestForwardSpeedNonThinned = UNKNOWN_PLAYBACK_RATE;
            fastestReverseSpeedNonThinned = UNKNOWN_PLAYBACK_RATE;
            slowestReverseSpeedNonThinned = UNKNOWN_PLAYBACK_RATE;

            // Thinned speeds
            reverseSpeedIsSupportedThinned = false;
            fastestForwardSpeedThinned = UNKNOWN_PLAYBACK_RATE;
            slowestForwardSpeedThinned = UNKNOWN_PLAYBACK_RATE;
            fastestReverseSpeedThinned = UNKNOWN_PLAYBACK_RATE;
            slowestReverseSpeedThinned = UNKNOWN_PLAYBACK_RATE;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the fastestForwardSpeedThinned. There is no set accessor. This
        /// value is derived from the media session
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public float FastestForwardSpeedThinned
        {
            get
            {
                return fastestForwardSpeedThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the slowestForwardSpeedThinned. There is no set accessor. This
        /// value is derived from the media session
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public float SlowestForwardSpeedThinned
        {
            get
            {
                return slowestForwardSpeedThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the fastestReverseSpeedThinned. There is no set accessor. This
        /// value is derived from the media session. 
        /// Note: this value will be positive unless unsupported. Multiply by -1 to use.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public float FastestReverseSpeedThinned
        {
            get
            {
                return fastestReverseSpeedThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the slowestReverseSpeedThinned. There is no set accessor. This
        /// value is derived from the media session
        /// Note: this value will be positive unless unsupported. Multiply by -1 to use.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public float SlowestReverseSpeedThinned
        {
            get
            {
                return slowestReverseSpeedThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the fastestForwardSpeedNonThinned. There is no set accessor. This
        /// value is derived from the media session
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public float FastestForwardSpeedNonThinned
        {
            get
            {
                return fastestForwardSpeedNonThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the slowestForwardSpeedNonThinned. There is no set accessor. This
        /// value is derived from the media session
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public float SlowestForwardSpeedNonThinned
        {
            get
            {
                return slowestForwardSpeedNonThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the fastestReverseSpeedNonThinned. There is no set accessor. This
        /// value is derived from the media session
        /// Note: this value will be positive unless unsupported. Multiply by -1 to use.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public float FastestReverseSpeedNonThinned
        {
            get
            {
                return fastestReverseSpeedNonThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the slowestReverseSpeedNonThinned. There is no set accessor. This
        /// value is derived from the media session
        /// Note: this value will be positive unless unsupported. Multiply by -1 to use.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public float SlowestReverseSpeedNonThinned
        {
            get
            {
                return slowestReverseSpeedNonThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the reverseSpeedIsSupportedNonThinned. There is no set accessor. This
        /// value is derived from the media session
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public bool ReverseSpeedIsSupportedNonThinned
        {
            get
            {
                return reverseSpeedIsSupportedNonThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the reverseSpeedIsSupportedThinned. There is no set accessor. This
        /// value is derived from the media session
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public bool ReverseSpeedIsSupportedThinned
        {
            get
            {
                return reverseSpeedIsSupportedThinned;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the CapabilityRequestSuccessful. This value will only be true
        /// if there are no errors collecting the playback rates
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public bool CapabilityRequestSuccessful
        {
            get
            {
                return capabilityRequestSuccessful;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Dumps the playback rates to the logfile
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public void DumpPlaybackRatesToLog()
        {
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: capabilityRequestSuccessful=" + capabilityRequestSuccessful.ToString());

            // nonThinned speeds
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: ReverseSpeedIsSupportedNonThinned=" + ReverseSpeedIsSupportedNonThinned.ToString());
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: FastestForwardSpeedNonThinned=" + FastestForwardSpeedNonThinned.ToString());
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: SlowestForwardSpeedNonThinned=" + SlowestForwardSpeedNonThinned.ToString());
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: FastestReverseSpeedNonThinned=" + FastestReverseSpeedNonThinned.ToString());
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: SlowestReverseSpeedNonThinned=" + SlowestReverseSpeedNonThinned.ToString());

            // Thinned speeds
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: ReverseSpeedIsSupportedThinned=" + reverseSpeedIsSupportedThinned.ToString());
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: FastestForwardSpeedThinned=" + FastestForwardSpeedThinned.ToString());
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: SlowestForwardSpeedThinned=" + SlowestForwardSpeedThinned.ToString());
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: FastestReverseSpeedThinned=" + FastestReverseSpeedThinned.ToString());
            LogMessage("TantaMediaSessionPlaybackRateCapabilities: SlowestReverseSpeedThinned=" + SlowestReverseSpeedThinned.ToString());
        }

    }
}

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.ExceptionServices;
using System.Security;
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

/// Notes
/// Some parts of this code may be derived from the samples which ships with the MF.Net dll. These are 
/// in turn derived from the original Microsoft samples. These have been placed in the public domain 
/// without copyright.
/// //

namespace TantaCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// General WMF utilities and defines for the Tanta App
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public class TantaWMFUtils
    {

        // not sure why these are not defined in the MF.Net library somewhere. Nonetheless We define 
        // them here. The MF.Net samples also do this.
        public const int MF_SOURCE_READER_FIRST_VIDEO_STREAM = unchecked((int)0xfffffffc);
        public const int MF_SOURCE_READER_FIRST_AUDIO_STREAM = unchecked((int)0xfffffffd);
        public const int MF_SOURCE_READER_ANY_STREAM = unchecked((int)0xfffffffe);

        // we use this to normalize the video duration (whatever it might be) to a
        // range between 0 and 1000
        public const int MAX_DURATION_RANGE = 1000;
        public const int DEFAULT_LARGE_INCREMENT_FOR_DURATIONRANGE = 50;
        public const int DEFAULT_SMALL_INCREMENT_FOR_DURATIONRANGE = 10;

        public const int MAX_TYPES_TESTED_PER_TRANSFORM = 100;
        private const uint CLSCTX_INPROC_SERVER = 1;
        private const uint CLSCTX_LOCAL_SERVER = 4;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a video position to a range value between 0 and TantaWMFUtils.MAX_DURATION_RANGE
        /// </summary>
        /// <param name="videoPosition">the video position</param>
        /// <param name="videoDuration">the video duration</param>
        /// <returns>the presentation time, or -1 for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static int ConvertVideoPostionToRangeValue(UInt64 videoDuration, UInt64 videoPosition)
        {
            if (videoDuration == 0) return 0;
            return (int)(((UInt64)TantaWMFUtils.MAX_DURATION_RANGE * videoPosition) / videoDuration);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a range value to a seek position 
        /// </summary>
        /// <param name="rangeValue">a value between 0 and TantaWMFUtils.MAX_DURATION_RANGE representing the 
        /// position to seek to</param>
        /// <param name="videoDuration">the video duration</param>
        /// <returns>the presentation time, or -1 for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static UInt64 ConvertRangeValueToVideoPosition(UInt64 videoDuration, int rangeValue)
        {
            return (videoDuration * (UInt64)rangeValue) / TantaWMFUtils.MAX_DURATION_RANGE;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Increments the audio volume on a sesson up or down. Volume is expressed 
        /// as an attenuation level, where 0.0 indicates silence and 1.0 indicates 
        /// full volume (no attenuation). The actual full volume is controlled
        /// by the PC - perhaps even a knob on the speakers.
        /// </summary>
        /// <param name="mediaSession">the media session</param>
        /// <param name="attenuationIncrement">the volume relative to 1 where 1 is full PC volume</param>
        /// <returns>true the operation was a succes, false it was not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool IncrementAudioVolumeOnSession(IMFMediaSession mediaSession, float attenuationIncrement)
        {
            HResult hr;
            IMFSimpleAudioVolume simpleAudioService = null;
            object rcServiceObj = null;

            // sanity check
            if (mediaSession == null) return false;

            try
            {
                // We get the audio volume service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MR_POLICY_VOLUME_SERVICE,
                    typeof(IMFSimpleAudioVolume).GUID,
                    out rcServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("IncrementAudioVolumeOnSession call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rcServiceObj == null)
                {
                    throw new Exception("IncrementAudioVolumeOnSession call to MFExtern.MFGetService failed. rcServiceObj == null");
                }
                // set the rate control service now for later use
                simpleAudioService = (rcServiceObj as IMFSimpleAudioVolume);

                // now get the current attenuation level on the audio service
                float attenuationLevel = 1.0f;
                hr = simpleAudioService.GetMasterVolume(out attenuationLevel);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("IncrementAudioVolumeOnSession call to simpleAudioService.GetMasterVolume failed. Err=" + hr.ToString());
                }

                // now perform the increment
                attenuationLevel += attenuationIncrement;
                // anything below zero or above one will throw an error. Volume is expressed 
                // as an attenuation level, where 0.0 indicates silence and 1.0 indicates 
                // full volume (no attenuation). The actual full volume level is controlled
                // by the PC - perhaps even a knob on the speakers.
                if (attenuationLevel < 0f) attenuationLevel = 0f;
                if (attenuationLevel > 1.0f) attenuationLevel = 1.0f;

                // now set the attenuation level on the audio service
                hr = simpleAudioService.SetMasterVolume(attenuationLevel);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("IncrementAudioVolumeOnSession call to simpleAudioService.SetMasterVolume failed. Err=" + hr.ToString());
                }
                return true;
            }
            finally
            {
                // release the audio service interface
                if (simpleAudioService != null)
                {
                    Marshal.ReleaseComObject(simpleAudioService);
                    simpleAudioService = null;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the audio volume on a sesson. Volume is expressed as an 
        /// attenuation level, where 0.0 indicates silence and 1.0 indicates 
        /// full volume (no attenuation). The actual full volume is controlled
        /// by the PC - perhaps even a knob on the speakers.
        /// </summary>
        /// <param name="mediaSession">the media session</param>
        /// <param name="attenuationLevel">the volume relative to 1 where 1 is full PC volume</param>
        /// <returns>true the operation was a succes, false it was not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool SetAudioVolumeOnSession(IMFMediaSession mediaSession, float attenuationLevel)
        {
            HResult hr;
            IMFSimpleAudioVolume simpleAudioService = null;
            object rcServiceObj = null;

            // sanity check
            if (mediaSession == null) return false;

            try
            {
                // We get the audio volume service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MR_POLICY_VOLUME_SERVICE,
                    typeof(IMFSimpleAudioVolume).GUID,
                    out rcServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("SetAudioVolumeOnSession call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rcServiceObj == null)
                {
                    throw new Exception("SetAudioVolumeOnSession call to MFExtern.MFGetService failed. rcServiceObj == null");
                }
                // set the rate control service now for later use
                simpleAudioService = (rcServiceObj as IMFSimpleAudioVolume);

                // now set the attenuation level on the audio service
                hr = simpleAudioService.SetMasterVolume(attenuationLevel);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("SetAudioVolumeOnSession call to simpleAudioService.SetMasterVolume failed. Err=" + hr.ToString());
                }
                return true;
            }
            finally
            {
                // release the audio service interface
                if (simpleAudioService != null)
                {
                    Marshal.ReleaseComObject(simpleAudioService);
                    simpleAudioService = null;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Toggles the mute state on a session.
        /// </summary>
        /// <param name="mediaSession">the media session</param>
        /// <returns>true the operation was a succes, false it was not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool ToggleAudioMuteStateOnSession(IMFMediaSession mediaSession)
        {
            HResult hr;
            IMFSimpleAudioVolume simpleAudioService = null;
            object rcServiceObj = null;

            // sanity check
            if (mediaSession == null) return false;

            try
            {
                // We get the audio volume service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MR_POLICY_VOLUME_SERVICE,
                    typeof(IMFSimpleAudioVolume).GUID,
                    out rcServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("ToggleAudioMuteStateOnSession call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rcServiceObj == null)
                {
                    throw new Exception("ToggleAudioMuteStateOnSession call to MFExtern.MFGetService failed. rcServiceObj == null");
                }
                // set the rate control service now for later use
                simpleAudioService = (rcServiceObj as IMFSimpleAudioVolume);

                // now get the mute state on the audio service
                bool muteState = false;
                hr = simpleAudioService.GetMute(out muteState);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("ToggleAudioMuteStateOnSession call to audioVolumeService.GetMute failed. Err=" + hr.ToString());
                }

                // toggle the state
                if (muteState == true) muteState = false;
                else muteState = true;

                // now set the mute state on the audio service
                hr = simpleAudioService.SetMute(muteState);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("ToggleAudioMuteStateOnSession call to audioVolumeService.SetMute failed. Err=" + hr.ToString());
                }
                return true;
            }
            finally
            {
                // release the audio service interface
                if (simpleAudioService != null)
                {
                    Marshal.ReleaseComObject(simpleAudioService);
                    simpleAudioService = null;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Mutes or unmutes a session
        /// </summary>
        /// <param name="wantMuted">if true we want muted, false we do not</param>
        /// <param name="mediaSession">the media session</param>
        /// <returns>true the operation was a succes, false it was not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool SetAudioMuteStateOnSession(IMFMediaSession mediaSession, bool wantMuted)
        {
            HResult hr;
            IMFSimpleAudioVolume simpleAudioService = null;
            object rcServiceObj = null;

            // sanity check
            if (mediaSession == null) return false;

            try
            {
                // we need to determine the current rate. We get the rate control service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MR_POLICY_VOLUME_SERVICE,
                    typeof(IMFSimpleAudioVolume).GUID,
                    out rcServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("SetAudioMuteStateOnSession call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rcServiceObj == null)
                {
                    throw new Exception("SetAudioMuteStateOnSession call to MFExtern.MFGetService failed. rcServiceObj == null");
                }
                // set the rate control service now for later use
                simpleAudioService = (rcServiceObj as IMFSimpleAudioVolume);

                // now set the mute state on the audio service
                hr = simpleAudioService.SetMute(wantMuted);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("SetAudioMuteStateOnSession call to audioVolumeService.SetMute failed. Err=" + hr.ToString());
                }
                return true;
            }
            finally
            {
                // release the audio service interface
                if (simpleAudioService != null)
                {
                    Marshal.ReleaseComObject(simpleAudioService);
                    simpleAudioService = null;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Changes the current playback rate on the session higher or lower by an
        /// increment. 
        /// 
        /// Playback rates are relative to value of 1. A value of 1 is normal speed.
        /// A playback rate of 2 is 2x speed, a rate of .5 is half speed. A rate of 
        /// -1 is reverse and so on. 
        /// 
        /// This function increments the playback rate by the specified value 
        /// either up or down. It will not set a rate that will fail because the 
        /// speed is invalid. 
        /// 
        /// </summary>
        /// <param name="rateIncrement">the rate increment. must be in a range from -1 and 1 </param>
        /// <param name="mediaSession">the media session</param>
        /// <param name="newRate">we output the new rate in here</param>
        /// <returns>true the operation was a succes, false it was not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool IncrementPlaybackRateOnSession(IMFMediaSession mediaSession, float rateIncrement, out float newRate)
        {
            HResult hr;
            IMFRateControl rateControlService = null;
            object rcServiceObj = null;
            bool wantThinned = false;
            bool isThinned = false;
            float currentRate = 0;

            // sanity check
            newRate = 0;
            if (mediaSession == null) return false;

            try
            {

                // we need to determine the current rate. We get the rate control service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MF_RATE_CONTROL_SERVICE,
                    typeof(IMFRateControl).GUID,
                    out rcServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("IncrementPlaybackRateOnSession call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rcServiceObj == null)
                {
                    throw new Exception("IncrementPlaybackRateOnSession call to MFExtern.MFGetService failed. rcServiceObj == null");
                }
                // set the rate control service now for later use
                rateControlService = (rcServiceObj as IMFRateControl);

                // now get the current rate from the rate control interface
                hr = rateControlService.GetRate(ref isThinned, out currentRate);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("IncrementPlaybackRateOnSession call to rateControlService.GetRate failed. Err=" + hr.ToString());
                }

                // now modify the rate
                newRate = currentRate + rateIncrement;
                // in general thinning works best if we are in slow speed
                if (newRate < 1) wantThinned = false;
                else wantThinned = false;

                // see if the payback rate is supported
                bool retBool = IsPlaybackRateSupported(mediaSession, newRate, wantThinned);
                if (retBool == false) return false;

                // ok we can do it 
                // now set the current rate on the rate control interface
                hr = rateControlService.SetRate(wantThinned, newRate);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("IncrementPlaybackRateOnSession call to rateControlService.SetRate failed. Err=" + hr.ToString());
                }

                return true;
            }
            finally
            {
                // release the rate control interface
                if (rateControlService != null)
                {
                    Marshal.ReleaseComObject(rateControlService);
                    rateControlService = null;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the current playback rate from a session
        /// </summary>
        /// <param name="currentRate">the current playback rate is returned here</param>
        /// <param name="isThinned">a flag indicating if the stream is thinned is return here</param>
        /// <param name="mediaSession">the media session</param>
        /// <returns>true the operation was a succes, false it was not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool GetCurrentPlaybackRateFromSession(IMFMediaSession mediaSession, out bool isThinned, out float currentRate)
        {
            HResult hr;
            IMFRateControl rateControlService = null;
            object rcServiceObj = null;

            // init these
            isThinned = false;
            currentRate = 1;

            // sanity check
            if (mediaSession == null) return false;

            try
            {

                // we need to determine the current rate. We get the rate control service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MF_RATE_CONTROL_SERVICE,
                    typeof(IMFRateControl).GUID,
                    out rcServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetCurrentPlaybackRateFromSession call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rcServiceObj == null)
                {
                    throw new Exception("GetCurrentPlaybackRateFromSession call to MFExtern.MFGetService failed. rcServiceObj == null");
                }
                // set the rate control service now for later use
                rateControlService = (rcServiceObj as IMFRateControl);

                // now get the current rate from the rate control interface
                hr = rateControlService.GetRate(ref isThinned, out currentRate);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetCurrentPlaybackRateFromSession call to rateControlService.GetRate failed. Err=" + hr.ToString());
                }
                return true;
            }
            finally
            {
                // release the rate control interface
                if (rateControlService != null)
                {
                    Marshal.ReleaseComObject(rateControlService);
                    rateControlService = null;
                }

            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the current playback rate from a session
        /// </summary>
        /// <param name="newRate">the new playback rate</param>
        /// <param name="wantThinned">a flag indicating if the stream can be thinned</param>
        /// <param name="mediaSession">the media session</param>
        /// <returns>true the operation was a succes, false it was not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool SetCurrentPlaybackRateOnSession(IMFMediaSession mediaSession, bool wantThinned, float newRate)
        {
            HResult hr;
            IMFRateControl rateControlService = null;
            object rcServiceObj = null;

            // sanity check
            if (mediaSession == null) return false;

            try
            {

                // We get the rate control service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MF_RATE_CONTROL_SERVICE,
                    typeof(IMFRateControl).GUID,
                    out rcServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("SetCurrentPlaybackRateOnSession call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rcServiceObj == null)
                {
                    throw new Exception("SetCurrentPlaybackRateOnSession call to MFExtern.MFGetService failed. rcServiceObj == null");
                }
                // set the rate control service now for later use
                rateControlService = (rcServiceObj as IMFRateControl);

                // now set the current rate on the rate control interface
                hr = rateControlService.SetRate(wantThinned, newRate);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("SetCurrentPlaybackRateOnSession call to rateControlService.SetRate failed. Err=" + hr.ToString());
                }
                return true;
            }
            finally
            {
                // release the rate control interface
                if (rateControlService != null)
                {
                    Marshal.ReleaseComObject(rateControlService);
                    rateControlService = null;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Detects if the proposed rate is supported. 
        /// 
        /// Note there is a bug in the MF.Net Library as of version 3.1 and earlier. 
        /// The call below to IsRateSupported should return an acceptable value in the 
        /// actualRate which should be defineable as an OUT parameter. Due to a bug 
        /// in the MF.Net library the actualRate parameter cannot be defined as OUT.
        /// In reality is does not seem to matter, because even though the docs say
        /// the nearest useable rate should be returned here, all that seems to come
        /// back is the rate you put in. So it is useless.
        /// 
        /// Thus, all you will get out of this function is a true or false indicating 
        /// if the rate is supported
        /// 
        /// </summary>
        /// <param name="rateRequested">the requested playback rate</param>
        /// <param name="wantThinned">a flag indicating if the stream should be thinned</param>
        /// <param name="mediaSession">the media session</param>
        /// <returns>true the rate is supported, false it is not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool IsPlaybackRateSupported(IMFMediaSession mediaSession, float rateRequested, bool wantThinned)
        {
            HResult hr;
            IMFRateSupport rateSupportService = null;
            object rsServiceObj = null;

            // sanity check
            float actualRate = 0f;
            if (mediaSession == null) return false;

            try
            {

                // we need to determine if the proposed rate is possible. We get the rate support service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MF_RATE_CONTROL_SERVICE,
                    typeof(IMFRateSupport).GUID,
                    out rsServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("IsPlaybackRateSupported call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rsServiceObj == null)
                {
                    throw new Exception("IsPlaybackRateSupported call to MFExtern.MFGetService failed. rsServiceObj == null");
                }
                // set the rate control service now for later use
                rateSupportService = (rsServiceObj as IMFRateSupport);

                // Note there is a bug in the MF.Net Library as of version 3.1 and earlier. 
                // The call below to IsRateSupported should return an acceptable value in the 
                // actualRate which should be defineable as an OUT parameter. Due to a bug 
                // in the MF.Net library the actualRate parameter cannot be defined as OUT.
                // In reality is does not seem to matter, because even though the docs say
                // the nearest useable rate should be returned here, all that seems to come
                // back is the rate you put in. So it is useless.

                // now see if the rate is supported      
                hr = rateSupportService.IsRateSupported(wantThinned, rateRequested, actualRate);
                if (hr == HResult.MF_E_UNSUPPORTED_RATE)
                {
                    // see above note for why we do not return the actualRate here
                    return false;
                }
                else if (hr != HResult.S_OK)
                {
                    throw new Exception("IsPlaybackRateSupported call to rateSupportService.IsRateSupported failed. Err=" + hr.ToString());
                }
                // rate is supported
                return true;
            }
            finally
            {
                // release the rate control interface
                if (rateSupportService != null)
                {
                    Marshal.ReleaseComObject(rateSupportService);
                    rateSupportService = null;
                }

            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Detects if rewind is supported on the media session. 
        /// 
        /// All you will get out of this function is a true or false indicating 
        /// if rewind is supported with the specified thinning state.
        /// 
        /// </summary>
        /// <param name="mediaSession">the media session</param>
        /// <param name="wantThinned">a flag indicating if the stream should be thinned</param>
        /// <returns>true the rewind is supported with the current thinning state, false it is not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool IsRewindSupported(IMFMediaSession mediaSession, bool wantThinned)
        {
            HResult hr;
            IMFRateSupport rateSupportService = null;
            object rsServiceObj = null;

            // sanity check
            if (mediaSession == null) return false;

            try
            {
                // we need to determine if the proposed rate is possible. We get the rate support service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MF_RATE_CONTROL_SERVICE,
                    typeof(IMFRateSupport).GUID,
                    out rsServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("IsRewindSupported call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rsServiceObj == null)
                {
                    throw new Exception("IsRewindSupported call to MFExtern.MFGetService failed. rsServiceObj == null");
                }
                // set the rate control service now for later use
                rateSupportService = (rsServiceObj as IMFRateSupport);

                // now see if the rewind is supported      
                float supportedRate = 0;
                hr = rateSupportService.GetSlowestRate(MFRateDirection.Reverse, wantThinned, out supportedRate);
                if (hr == HResult.MF_E_REVERSE_UNSUPPORTED)
                {
                    // just let the user know the rate is not supported
                    return false;
                }
                else if (hr == HResult.MF_E_THINNING_UNSUPPORTED)
                {
                    // just let the user know the rate is not supported
                    return false;
                }
                else if (hr != HResult.S_OK)
                {
                    throw new Exception("IsRewindSupported call to rateSupportService.GetSlowestRate failed. Err=" + hr.ToString());
                }
                // rate is supported
                return true;
            }
            finally
            {
                // release the rate control interface
                if (rateSupportService != null)
                {
                    Marshal.ReleaseComObject(rateSupportService);
                    rateSupportService = null;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the slowest rate supported on the media session for a particular
        /// direction and thinning state. 
        /// 
        /// </summary>
        /// <param name="mediaSession">the media session</param>
        /// <param name="wantThinned">a flag indicating if the stream should be thinned</param>
        /// <param name="supportedRate">the slowest supported rate is returned here</param>
        /// <param name="rateDirection">the rate direction</param>
        /// <returns>true the slowest supported rate is returned, false it is not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool GetSlowestRate(IMFMediaSession mediaSession, MFRateDirection rateDirection, bool wantThinned, out float supportedRate)
        {
            HResult hr;
            IMFRateSupport rateSupportService = null;
            object rsServiceObj = null;

            supportedRate = 0;

            // sanity check
            if (mediaSession == null) return false;

            try
            {
                // We get the rate support service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MF_RATE_CONTROL_SERVICE,
                    typeof(IMFRateSupport).GUID,
                    out rsServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetSlowestRate call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rsServiceObj == null)
                {
                    throw new Exception("GetSlowestRate call to MFExtern.MFGetService failed. rsServiceObj == null");
                }
                // set the rate control service now for later use
                rateSupportService = (rsServiceObj as IMFRateSupport);

                // now get the slowest rate 
                hr = rateSupportService.GetSlowestRate(rateDirection, wantThinned, out supportedRate);
                if (hr == HResult.MF_E_REVERSE_UNSUPPORTED)
                {
                    // just let the user know the rate is not supported
                    return false;
                }
                else if (hr == HResult.MF_E_THINNING_UNSUPPORTED)
                {
                    // just let the user know the rate is not supported
                    return false;
                }
                else if (hr != HResult.S_OK)
                {
                    throw new Exception("GetSlowestRate call to rateSupportService.GetSlowestRate failed. Err=" + hr.ToString());
                }
                // rate is supported
                return true;
            }
            finally
            {
                // release the rate control interface
                if (rateSupportService != null)
                {
                    Marshal.ReleaseComObject(rateSupportService);
                    rateSupportService = null;
                }

            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the fastest rate supported on the media session for a particular
        /// direction and thinning state. 
        /// 
        /// </summary>
        /// <param name="mediaSession">the media session</param>
        /// <param name="wantThinned">a flag indicating if the stream should be thinned</param>
        /// <param name="supportedRate">the slowest supported rate is returned here</param>
        /// <param name="rateDirection">the rate direction</param>
        /// <returns>true the slowest supported rate is returned, false it is not</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static bool GetFastestRate(IMFMediaSession mediaSession, MFRateDirection rateDirection, bool wantThinned, out float supportedRate)
        {
            HResult hr;
            IMFRateSupport rateSupportService = null;
            object rsServiceObj = null;

            supportedRate = 0;

            // sanity check
            if (mediaSession == null) return false;

            try
            {
                // We get the rate support service from the Media Session.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MF_RATE_CONTROL_SERVICE,
                    typeof(IMFRateSupport).GUID,
                    out rsServiceObj
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetFastestRate call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (rsServiceObj == null)
                {
                    throw new Exception("GetFastestRate call to MFExtern.MFGetService failed. rsServiceObj == null");
                }
                // set the rate control service now for later use
                rateSupportService = (rsServiceObj as IMFRateSupport);

                // now get the slowest rate 
                hr = rateSupportService.GetFastestRate(rateDirection, wantThinned, out supportedRate);
                if (hr == HResult.MF_E_REVERSE_UNSUPPORTED)
                {
                    // just let the user know the rate is not supported
                    return false;
                }
                else if (hr == HResult.MF_E_THINNING_UNSUPPORTED)
                {
                    // just let the user know the rate is not supported
                    return false;
                }
                else if (hr != HResult.S_OK)
                {
                    throw new Exception("GetFastestRate call to rateSupportService.GetFastestRate failed. Err=" + hr.ToString());
                }
                // rate is supported
                return true;
            }
            finally
            {
                // release the rate control interface
                if (rateSupportService != null)
                {
                    Marshal.ReleaseComObject(rateSupportService);
                    rateSupportService = null;
                }

            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the current presentation time from a media session
        /// </summary>
        /// <param name="mediaSession">the media session</param>
        /// <returns>the current state of the presentation clock in 100ns units.
        /// </returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static UInt64 GetPresentationTimeFromSession(IMFMediaSession mediaSession)
        {
            HResult hr;
            IMFClock clockObject = null;
            Int64 presentationClock = 0;

            if (mediaSession == null)
            {
                throw new Exception("No mediaSession provided");
            }

            try
            {
                // get our presentation clock, this needs to be released.
                hr = mediaSession.GetClock(out clockObject);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetPresentationTimeFromSession call to MediaSession.GetClock failed. Err=" + hr.ToString());
                }
                if (clockObject == null)
                {
                    throw new Exception("GetPresentationTimeFromSession call to MediaSession.GetClock failed. clockObject == null");
                }
                // we have the clock object, but we actually need an IMFPresentationClock
                // the clock object returned above is both so we cast it.
                if ((clockObject is IMFPresentationClock) == true)
                {
                    // the cast is valid, so get the time from it, this comes back as an Int64
                    // even though it will never be negative
                    (clockObject as IMFPresentationClock).GetTime(out presentationClock);
                }
            }
            finally
            {
                // release the clock object we just obtained
                if (clockObject != null)
                {
                    Marshal.ReleaseComObject(clockObject);
                }
            }
            // return what we got
            return (UInt64)presentationClock;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the duration of a media stream from a source reader. This is only
        /// useful if the source reader is operating on a file rather than on 
        /// a device such as a camera
        /// </summary>
        /// <param name="ssourceReader">the source reader</param>
        /// <returns>the duration of the presentation in 100ns units. Will be zero if not found.
        /// </returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static UInt64 GetDurationFromSourceReader(IMFSourceReader sourceReader)
        {
            if (sourceReader == null)
            {
                throw new Exception("No source reader provided");
            }

            // now we get the duration from the source reader. This will be returned in a PROPVARIANT. 
            // NOTE that propvariants in C# are not quite the same as a propvariant in C++. In C# they
            // are a class with a variable for each supported type as opposed to a single memory area
            // which can be interpreted as any one of a number of types. This is important below because
            // the duration comes back as a ULONG. In C# you tried to access it as a simple LONG you would
            // get an error. The data is simply not there as a LONG!
            PropVariant pvObj = new PropVariant();
            HResult hr = sourceReader.GetPresentationAttribute((int)MF_SOURCE_READER.MediaSource, MFAttributesClsid.MF_PD_DURATION, pvObj);
            if (hr != HResult.S_OK)
            {
                // we failed
                throw new Exception("GetDurationFromSourceReader: Failed on call to sourceReader.GetPresentationAttribute, retVal=" + hr.ToString());
            }
            // get the duration from the ULong slot
            UInt64 duration = pvObj.GetULong();

            return duration;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the duration of a presentation from a presentation descriptor
        /// </summary>
        /// <param name="sourcePresentationDescriptor">the source presentation descriptor</param>
        /// <returns>the duration of the presentation in 100ns units. Will be zero if not found.
        /// </returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static UInt64 GetDurationFromPresentationDescriptor(IMFPresentationDescriptor sourcePresentationDescriptor)
        {
            Int64 presentationDuration = 0;

            if (sourcePresentationDescriptor == null)
            {
                throw new Exception("No presentation descriptor provided");
            }

            // Ask the presentation descriptor for the duration. This should never be negative even 
            // though it is stored as an Int64
            sourcePresentationDescriptor.GetUINT64(MFAttributesClsid.MF_PD_DURATION, out presentationDuration);

            return (UInt64)presentationDuration;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the major media type from a presentationDescriptor descriptor and 
        /// and stream index. This will be something like MFMediaType.Audio or MFMediaType.Video
        /// </summary>
        /// <param name="presentationDescriptor">the source presentation descriptor</param>
        /// <param name="streamIndex">the index of the stream in the presentation we are interested in</param>
        /// <returns>the major media type of the stream</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static Guid GetMajorMediaTypeFromPresentationDescriptor(IMFPresentationDescriptor presentationDescriptor, int streamIndex)
        {
            HResult hr;
            Guid guidMajorType = Guid.Empty;
            bool streamIsSelected = false;
            IMFStreamDescriptor streamDescriptor = null;

            if (presentationDescriptor == null)
            {
                throw new Exception("GetMajorMediaTypeFromPresentationDescriptor: No source stream descriptor provided");
            }

            try
            {
                // get the stream descriptor
                hr = presentationDescriptor.GetStreamDescriptorByIndex(streamIndex, out streamIsSelected, out streamDescriptor);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetMajorMediaTypeFromPresentationDescriptor call to GetStreamDescriptorByIndex failed. Err=" + hr.ToString());
                }
                if (streamDescriptor == null)
                {
                    throw new Exception("GetMajorMediaTypeFromPresentationDescriptor call tosourcePresentationDescriptor.GetStreamDescriptorByIndex failed. streamDescriptor == null");
                }

                // return this
                return GetMajorMediaTypeFromStreamDescriptor(streamDescriptor);
            }
            finally
            {
                // Clean up.
                if (streamDescriptor != null)
                {
                    Marshal.ReleaseComObject(streamDescriptor);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the major media type from a stream descriptor. This will be something
        /// like MFMediaType.Audio or MFMediaType.Video
        /// </summary>
        /// <param name="streamDescriptor">the source stream descriptor</param>
        /// <returns>the major media type of the stream</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static Guid GetMajorMediaTypeFromStreamDescriptor(IMFStreamDescriptor streamDescriptor)
        {
            HResult hr;
            IMFMediaTypeHandler pHandler = null;
            Guid guidMajorType = Guid.Empty;

            if (streamDescriptor == null)
            {
                throw new Exception("GetMajorMediaTypeFromStreamDescriptor: No source stream descriptor provided");
            }

            // Getting the media type from a stream has to be done by first fetching a IMFMediaTypeHandler 
            // from the stream descriptor and then asking that about the media type. The type handler also has
            // to be cleaned up afterwards. This is a pretty commonly required, multi-step, operation so it
            // has been factored off here as a useful bit of building block code.

            try
            {
                // Get the media type handler for the stream. IMFMediaTypeHandler interface is a standard way of getting or 
                // setting the media types on an object
                hr = streamDescriptor.GetMediaTypeHandler(out pHandler);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetMajorMediaTypeFromStreamDescriptor call to streamDescriptor.GetMediaTypeHandler failed. Err=" + hr.ToString());
                }
                if (pHandler == null)
                {
                    throw new Exception("GetMajorMediaTypeFromStreamDescriptor call to streamDescriptor.GetMediaTypeHandler failed. pHandler == null");
                }

                // Now we have the handler, get the major media type.
                hr = pHandler.GetMajorType(out guidMajorType);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetMajorMediaTypeFromStreamDescriptor call to pHandler.GetMajorType failed. Err=" + hr.ToString());
                }
                if (guidMajorType == null)
                {
                    throw new Exception("GetMajorMediaTypeFromStreamDescriptor call to pHandler.GetMajorType failed. guidMajorType == null");
                }

                // return this
                return guidMajorType;
            }
            finally
            {
                // Clean up.
                if (pHandler != null)
                {
                    Marshal.ReleaseComObject(pHandler);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the current media type from a stream descriptor. This object must
        /// be released by the caller.
        /// </summary>
        /// <param name="streamDescriptor">the source stream descriptor</param>
        /// <returns>the major media type of the stream</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static IMFMediaType GetCurrentMediaTypeFromStreamDescriptor(IMFStreamDescriptor streamDescriptor)
        {
            HResult hr;
            IMFMediaTypeHandler pHandler = null;
            IMFMediaType outMediaType = null;
            Guid guidMajorType = Guid.Empty;

            if (streamDescriptor == null)
            {
                throw new Exception("GetCurrentMediaTypeFromStreamDescriptor: No source stream descriptor provided");
            }

            // Getting the media type from a stream has to be done by first fetching a IMFMediaTypeHandler 
            // from the stream descriptor and then asking that for the media type. The type handler also has
            // to be cleaned up afterwards. This is a pretty commonly required, multi-step, operation so it
            // has been factored off here as a useful bit of building block code.

            try
            {
                // Get the media type handler for the stream. IMFMediaTypeHandler interface is a standard way of getting or 
                // setting the media types on an object
                hr = streamDescriptor.GetMediaTypeHandler(out pHandler);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetCurrentMediaTypeFromStreamDescriptor call to streamDescriptor.GetMediaTypeHandler failed. Err=" + hr.ToString());
                }
                if (pHandler == null)
                {
                    throw new Exception("GetCurrentMediaTypeFromStreamDescriptor call to streamDescriptor.GetMediaTypeHandler failed. pHandler == null");
                }

                // Now we have the handler, get the full media type.
                hr = pHandler.GetCurrentMediaType(out outMediaType);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetCurrentMediaTypeFromStreamDescriptor call to pHandlerGetCurrentMediaType failed. Err=" + hr.ToString());
                }
                if (guidMajorType == null)
                {
                    throw new Exception("GetCurrentMediaTypeFromStreamDescriptor call to pHandler.GetCurrentMediaType failed. outMediaType == null");
                }

                // return this
                return outMediaType;
            }
            finally
            {
                // Clean up.
                if (pHandler != null)
                {
                    Marshal.ReleaseComObject(pHandler);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the current media type on a stream descriptor by matching 
        /// its mediaTypes to the video format container contents.
        /// 
        /// </summary>
        /// <param name="videoStreamDescriptor">the stream descriptor</param>
        /// <param name="videoFormatContainer">the format container</param>
        /// <returns>S_OK for success, other for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static HResult SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer(IMFStreamDescriptor videoStreamDescriptor, TantaMFVideoFormatContainer videoFormatContainer)
        {
            HResult hr;
            IMFMediaTypeHandler workingMediaTypeHandler = null;
            IMFMediaType workingMediaType = null;
            Guid majorType = Guid.Empty;
            Guid subType = Guid.Empty;
            int attributeCount=0;
            int frameSizeWidth = 0;
            int frameSizeHeight = 0;
            int frameRate = 0;
            int frameRateDenominator = 0;
            int frameRateMin = 0;
            int frameRateMinDenominator = 0;
            int frameRateMax = 0;
            int frameRateMaxDenominator = 0;

            if (videoStreamDescriptor == null)
            {
                throw new Exception("SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer No stream descriptor provided");
            }
            if (videoFormatContainer == null)
            {
                throw new Exception("SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer No videoFormatContainer provided");
            }

            // get the media type handler from the stream descriptor
            hr = videoStreamDescriptor.GetMediaTypeHandler(out workingMediaTypeHandler);
            if (hr != HResult.S_OK)
            {
                throw new Exception("SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer call to GetMediaTypeHandler failed. Err=" + hr.ToString());
            }
            if (workingMediaTypeHandler == null)
            {
                throw new Exception("SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer call to GetMediaTypeHandler failed. workingMediaTypeHandler == null");
            }

            // we assume we will never have more media sub types than this
            const int MAXSUBTYPES = 250;

            try
            {

                // look at each possible subType on this stream
                for (int subTypeIndex = 0; subTypeIndex < MAXSUBTYPES; subTypeIndex++)
                {
                    // get the Media Type
                    hr = workingMediaTypeHandler.GetMediaTypeByIndex(subTypeIndex, out workingMediaType);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer call to GetMediaTypeByIndex failed. Err=" + hr.ToString());
                    }
                    if (workingMediaType == null)
                    {
                        throw new Exception("SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer call to GetMediaTypeByIndex failed. workingMediaType == null");
                    }

                    // is this the type we want? we get some information from it to compare against
                    hr = TantaMediaTypeInfo.GetSupportedFormatsFromMediaType(workingMediaType, out majorType, out subType, out attributeCount, out frameSizeWidth, out frameSizeHeight, out frameRate, out frameRateDenominator, out frameRateMin, out frameRateMinDenominator, out frameRateMax, out frameRateMaxDenominator);
                    if (hr != HResult.S_OK)
                    {
                        // we are not interested in this media type, release it and get the next
                        if (workingMediaType != null)
                        {
                            Marshal.ReleaseComObject(workingMediaType);
                            workingMediaType = null;
                        }
                        continue;
                     }
                    
                    // compare the major type, sub type, frame width and frame height of this media
                    /// type to our format container. Reject if any do not match. There are other things
                    /// we could compare on here - but this is enough
                    if ((videoFormatContainer.MajorType != majorType) ||
                        (videoFormatContainer.SubType != subType) ||
                        (videoFormatContainer.FrameSizeWidth != frameSizeWidth) ||
                        (videoFormatContainer.FrameSizeHeight != frameSizeHeight))
                    {
                        // we are not interested in this media type, release it and get the next
                        if (workingMediaType != null)
                        {
                            Marshal.ReleaseComObject(workingMediaType);
                            workingMediaType = null;
                        }
                        continue;
                    }

                    // ok the media type matches, we set this as the current
                    // media type on the stream by making a call on the type handler.
                    hr = workingMediaTypeHandler.SetCurrentMediaType(workingMediaType);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("SetCurrentMediaTypeOnIMFStreamDescriptorByFormatContainer call to SetCurrentMediaType failed. Err=" + hr.ToString());
                    }

                    // release this, we are done with it
                    if (workingMediaType != null)
                    {
                        Marshal.ReleaseComObject(workingMediaType);
                        workingMediaType = null;
                    }

                    // we are done
                    return HResult.S_OK;
                }
            }
            finally
            {
                if (workingMediaTypeHandler != null)
                {
                    Marshal.ReleaseComObject(workingMediaTypeHandler);
                    workingMediaTypeHandler = null;
                }
                if (workingMediaType != null)
                {
                    Marshal.ReleaseComObject(workingMediaType);
                    workingMediaType = null;
                }
            }


            // if we get here we did not match
            return HResult.MF_E_NOT_FOUND;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the major media type from a stream descriptor. 
        /// NOTE the Media Type returned here must be released.
        /// 
        /// </summary>
        /// <param name="streamDescriptor">the source stream descriptor</param>
        /// <param name="mediaTypeId">the id of the media type in the stream descriptor</param>
        /// <returns>the media type of the stream</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static IMFMediaType GetMediaTypeFromStreamDescriptorById(IMFStreamDescriptor streamDescriptor, int mediaTypeId)
        {
            HResult hr;
            IMFMediaTypeHandler typeHandler = null;
            IMFMediaType outMediaType = null;

            if (streamDescriptor == null)
            {
                throw new Exception("GetMediaTypeFromStreamDescriptorById: No source stream descriptor provided");
            }

            // Getting the media type from a stream has to be done by first fetching a IMFMediaTypeHandler 
            // from the stream descriptor and then asking that about the media type. The type handler also has
            // to be cleaned up afterwards. This is a pretty commonly required, multi-step, operation so it
            // has been factored off here as a useful bit of building block code.

            try
            {
                // Get the media type handler for the stream. IMFMediaTypeHandler interface is a standard way of getting or 
                // setting the media types on an object
                hr = streamDescriptor.GetMediaTypeHandler(out typeHandler);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetMediaTypeFromStreamDescriptorById call to streamDescriptor.GetMediaTypeHandler failed. Err=" + hr.ToString());
                }
                if (typeHandler == null)
                {
                    throw new Exception("GetMediaTypeFromStreamDescriptorById call to streamDescriptor.GetMediaTypeHandler failed. typeHandler == null");
                }

                // Now we have the handler, get the media type.
                hr = typeHandler.GetMediaTypeByIndex(mediaTypeId, out outMediaType); 
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetMediaTypeFromStreamDescriptorById call to typeHandler.GetMediaTypeByIndex failed. Err=" + hr.ToString());
                }
                if (outMediaType == null)
                {
                    throw new Exception("GetMediaTypeFromStreamDescriptorById call to typeHandler.GetMediaTypeByIndex failed. outMediaType == null");
                }

                // return this
                return outMediaType;
            }
            finally
            {
                // Clean up.
                if (typeHandler != null)
                {
                    Marshal.ReleaseComObject(typeHandler);
                }
            }
        }

        // in order to create a transform from a guid we have to call 
        // the COM CoCreateInstance. The code below makes this possible
        [DllImport("ole32.dll", EntryPoint = "CoCreateInstance", CallingConvention = CallingConvention.StdCall)]
        static extern UInt32 CoCreateInstance([In, MarshalAs(UnmanagedType.LPStruct)] Guid rclsid,
        IntPtr pUnkOuter, UInt32 dwClsContext, [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object rReturnedComObject);

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a transform object from a Guid
        /// 
        /// </summary>
        /// <param name="transformGuid">the guid of the transform</param>
        /// <param name="wantLocalServer">if true use CLSCTX_LOCAL_SERVER otherwise CLSCTX_INPROC_SERVER</param>
        /// <returns>the transform object - this must be released</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>        
        public static IMFTransform GetTransformFromGuid(Guid transformGuid, bool wantLocalServer)
        {

            object retInstance = null;
            IMFTransform transformObj = null;

            try
            {
                // set up for INPROC or LOCAL server 
                uint serverType = CLSCTX_INPROC_SERVER;
                if (wantLocalServer == true) serverType = CLSCTX_LOCAL_SERVER;

                // call COM and create and instance from the Guid
                UInt32 hResult = CoCreateInstance(transformGuid,
                                                IntPtr.Zero,
                                                serverType,
                                                typeof(IMFTransform).GUID,
                                                out retInstance);
                if (hResult != 0) return null;
                if (retInstance == null) return null;
                transformObj = (IMFTransform)retInstance;
            }
            catch
            {
            }
            finally
            {

            }

            return transformObj;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of output Media Types object from a Transform represented
        /// by a guid.
        /// 
        /// NOTE: the media types returned here must be released
        /// 
        /// </summary>
        /// <param name="transformGuid">the guid of the transform</param>
        /// <param name="wantLocalServer">if true use CLSCTX_LOCAL_SERVER otherwise CLSCTX_INPROC_SERVER</param>
        /// <returns>a list of Media Types - these must be released</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>        
   //     [HandleProcessCorruptedStateExceptions, SecurityCritical]
        public static List<IMFMediaType> GetOutputMediaTypesFromTransformByGuid(Guid transformGuid, bool wantLocalServer)
        {
            IMFTransform transformObj = null;
            IMFMediaType mediaType = null;
            HResult hr;
            List<IMFMediaType> outList = new List<IMFMediaType>();

            try
            {
                // get the transform object
                transformObj = GetTransformFromGuid(transformGuid, wantLocalServer);
                if (transformObj == null) return outList;

                // get all of the media types this transform can handle
                // I do not like endless loops. So we cap this with
                // a hardcoded limit

                // note: it appears some transforms report having no types
                //  possibly this is proprietary and the code using it 
                //  will know the types anyways.
                for (int typeCounter = 0; typeCounter < MAX_TYPES_TESTED_PER_TRANSFORM; typeCounter++)
                {
                    try
                    {
                        // get the available input type for the current typeCounter 
                        hr = transformObj.GetOutputAvailableType(0, typeCounter, out mediaType);
                        // not found, we are done
                        if (hr != HResult.S_OK) break;
                        if (mediaType == null) break;

                        // add it now 
                        outList.Add(mediaType);
                    }
                    catch
                    {
                    } // bottom of inner try...catch
                } // bottom of for (int typeCounter = 0; ...
            }
            catch
            {
                // do nothing
            }
            finally
            {
                //  make sure this is released
                if (transformObj != null)
                {
                    // close and release
                    if (Marshal.IsComObject(transformObj) == true) Marshal.ReleaseComObject(transformObj);
                    transformObj = null;
                }
            }

            return outList;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of input Media Types object from a Transform represented
        /// by a guid.
        /// 
        /// NOTE: the media types returned here must be released
        /// 
        /// </summary>
        /// <param name="transformGuid">the guid of the transform</param>
        /// <param name="wantLocalServer">if true use CLSCTX_LOCAL_SERVER otherwise CLSCTX_INPROC_SERVER</param>
        /// <returns>a list of Media Types - these must be released</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>        
     //   [HandleProcessCorruptedStateExceptions, SecurityCritical]
        public static List<IMFMediaType> GetInputMediaTypesFromTransformByGuid(Guid transformGuid, bool wantLocalServer)
        {
            IMFTransform transformObj = null;
            IMFMediaType mediaType = null;
            HResult hr;
            List<IMFMediaType> outList = new List<IMFMediaType>();

            try
            {
                // get the transform object
                transformObj = GetTransformFromGuid(transformGuid, wantLocalServer);
                if (transformObj == null) return outList;

                // get all of the media types this transform can handle
                // I do not like endless loops. So we cap this with
                // a hardcoded limit
                for (int typeCounter = 0; typeCounter < MAX_TYPES_TESTED_PER_TRANSFORM; typeCounter++)
                {
                    try
                    {
                        // get the available input type for the current typeCounter 
                        hr = transformObj.GetInputAvailableType(0, typeCounter, out mediaType);
                        // not found, we are done
                        if (hr != HResult.S_OK) break;
                        if (mediaType == null) break;

                        // add it now 
                        outList.Add(mediaType);
                    }
                    catch
                    {
                    } // bottom of try...catch
                } // bottom of for (int typeCounter = 0; ...
            }
            catch
            {
                // do nothing
            }
            finally
            {
                //  make sure this is released
                if (transformObj != null)
                {
                    // close and release
                    if (Marshal.IsComObject(transformObj) ==true) Marshal.ReleaseComObject(transformObj);
                    transformObj = null;
                }
            }

            return outList;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create a topology node for a sink stream. This assumes the sink stream
        /// is stream 0. Only useful for sinks which have one stream.
        /// </summary>
        /// <param name="pSink">the media sink</param>
        /// <returns>the sink stream node</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static IMFTopologyNode CreateSinkNodeForStream(IMFMediaSink pSink)
        {
            // many Sinks only have one streamSink - this takes care of that case
            return CreateSinkNodeForStream(pSink, 0);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create a topology node for a sink stream. The sink node must contain
        /// pointers to the media sink and the stream it is using. This code is just 
        /// an ecapsulated way of doing that. 
        /// </summary>
        /// <param name="pSink">the media sink</param>
        /// <param name="streamIndex">the stream index</param>
        /// <returns>the sink stream node</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static IMFTopologyNode CreateSinkNodeForStream(IMFMediaSink pSink, int streamIndex)
        {

            HResult hr;
            IMFTopologyNode outSinkNode = null;
            IMFStreamSink pStream = null;


            if (pSink == null)
            {
                throw new Exception("CreateSinkNodeForStream No media sink object provided");
            }

            try
            {
                // A sink node represents one stream from a media sink. The sink node must 
                // to the media sink and the stream it is using.

                // Create the empty structure of the sink-stream node.
                hr = MFExtern.MFCreateTopologyNode(MFTopologyType.OutputNode, out outSinkNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSinkNodeForStream call to MFCreateTopologyNode failed. Err=" + hr.ToString());
                }
                if (outSinkNode == null)
                {
                    throw new Exception("CreateSinkNodeForStream call to MFCreateTopologyNode failed. outSinkNode == null");
                }

                // get the StreamSink
                hr = pSink.GetStreamSinkByIndex(streamIndex, out pStream);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSinkNodeForStream call to GetStreamSinkByIndex failed. Err=" + hr.ToString());
                }
                if (pStream == null)
                {
                    throw new Exception("CreateSinkNodeForStream call to GetStreamSinkByIndex failed. pStream == null");
                }

                // Set the object pointer to the media stream sink
                hr = outSinkNode.SetObject(pStream);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSinkNodeForStream call to pNode.SetObject on sink node failed. Err=" + hr.ToString());
                }

                // Return the IMFTopologyNode pointer to the caller.
                return outSinkNode;
            }
            catch
            {
                // If we failed, release the pnode
                if (outSinkNode != null)
                {
                    Marshal.ReleaseComObject(outSinkNode);
                }
                outSinkNode = null;
                throw;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create a topology node for a sink stream. Some sinks (the MP4Sink) 
        /// automatically create the streams when they are created as opposed to 
        /// the streams being manually added by AddStreamSink(). Since we did not
        /// add those streamsinks we cannot know which index they are. This code
        /// creates a Sink node based on the major media type - this implicitly
        /// assumes that there is only one of each major media type sink node.
        /// We just use the first one we come to.
        /// </summary>
        /// <param name="mediaSink">the media sink</param>
        /// <param name="majorMediaType">the major media type already on the sinkStream</param>
        /// <returns>the sink stream node</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static IMFTopologyNode CreateSinkNodeForStream(IMFMediaSink mediaSink, Guid majorMediaType)
        {

            HResult hr;
            IMFTopologyNode outSinkNode = null;
            IMFStreamSink streamSink = null;

            if (mediaSink == null)
            {
                throw new Exception("CreateSinkNodeForStream No media sink object provided");
            }

            try
            {
                // A sink node represents one stream from a media sink. The sink node must contain pointer to the stream descriptor. This code is just an ecapsulated way of doing that. 

                // Create the empty structure of the sink-stream node.
                hr = MFExtern.MFCreateTopologyNode(MFTopologyType.OutputNode, out outSinkNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSinkNodeForStream call to MFCreateTopologyNode failed. Err=" + hr.ToString());
                }
                if (outSinkNode == null)
                {
                    throw new Exception("CreateSinkNodeForStream call to MFCreateTopologyNode failed. outSinkNode == null");
                }

                // get the StreamSink
                streamSink = GetStreamSinkByMajorMediaType(mediaSink, majorMediaType);
                if (streamSink == null)
                {
                    throw new Exception("CreateSinkNodeForStream call to GetStreamSinkByMajorMediaType failed. streamSink == null");
                }

                // Set the object pointer to the media stream sink
                hr = outSinkNode.SetObject(streamSink);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSinkNodeForStream call to pNode.SetObject on sink object failed. Err=" + hr.ToString());
                }

                // Return the IMFTopologyNode pointer to the caller.
                return outSinkNode;
            }
            catch
            {
                // If we failed, release the pnode
                if (outSinkNode != null)
                {
                    Marshal.ReleaseComObject(outSinkNode);
                }
                outSinkNode = null;
                throw;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the object underlying a Topology Node. Note that if the 
        /// node was created with an activator this object will only be
        /// available after the Topology has been resolved.
        /// 
        /// NOTE: the caller must check that the object is of the 
        /// correct type and cast it appropriately.
        /// 
        /// </summary>
        /// <returns>object backing the Topology Node or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static object GetObjectFromTopologyNode(IMFTopologyNode topoNode)
        {
            HResult hr;
            object nodeObject;

            if (topoNode == null) return null;

            // get the object from the node
            hr = topoNode.GetObject(out nodeObject);
            if (hr != HResult.S_OK) return null;
            return nodeObject;          
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the StreamSink on Media Sink by the Major Media Type
        /// it supports. We just return the first match we find. 
        /// 
        /// The caller MUST release this StreamSink
        /// </summary>
        /// <param name="mediaSink">the media sink</param>
        /// <param name="majorMediaType">the major media type</param>
        /// <returns>the IMFStreamSink associated with the media type or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static IMFStreamSink GetStreamSinkByMajorMediaType(IMFMediaSink mediaSink, Guid majorMediaType)
        {
            HResult hr;
            IMFStreamSink outStreamSink = null;
            IMFStreamSink workingStreamSink = null;
            IMFMediaTypeHandler workingMediaTypeHandler = null;
            IMFMediaType workingMediaType = null;
            Guid workingMajorType = Guid.Empty;

            if (mediaSink == null)
            {
                throw new Exception("GetStreamSinkByMajorMediaType No media sink object provided");
            }

            // we assume we will never have more streams sinks than this
            const int MAXSTREAMS = 10;

            // look at each possible stream sink on the media sink
            for (int streamIndex=0; streamIndex < MAXSTREAMS; streamIndex++)
            {
                try
                {
                    // get the StreamSink
                    hr = mediaSink.GetStreamSinkByIndex(streamIndex, out workingStreamSink);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("GetStreamSinkByMajorMediaType call to GetStreamSinkByIndex failed. Err=" + hr.ToString());
                    }
                    if (workingStreamSink == null)
                    {
                        throw new Exception("GetStreamSinkByMajorMediaType call to GetStreamSinkByIndex failed. workingStreamSink == null");
                    }
                    // get the media type handler from the steam sink
                    hr = workingStreamSink.GetMediaTypeHandler(out workingMediaTypeHandler);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("GetStreamSinkByMajorMediaType call to GetMediaTypeHandler failed. Err=" + hr.ToString());
                    }
                    if (workingMediaTypeHandler == null)
                    {
                        throw new Exception("GetStreamSinkByMajorMediaType call to GetMediaTypeHandler failed. workingMediaTypeHandler == null");
                    }
                    // get the current media type
                    workingMediaTypeHandler.GetCurrentMediaType(out workingMediaType);
                    if (hr != HResult.S_OK) continue;
                    if (workingMediaType == null) continue;

                    // get the major type
                    hr = workingMediaType.GetMajorType(out workingMajorType);
                    if (hr != HResult.S_OK) continue;
                    if (workingMajorType == Guid.Empty) continue;

                    if (workingMajorType == majorMediaType)
                    {
                        // make sure we do not release the workingStreamSink
                        // which matches. The caller must do that
                        outStreamSink = workingStreamSink;
                        workingStreamSink = null;
                        break;
                    }
                }
                finally
                {
                    if (workingStreamSink != null)
                    {
                        Marshal.ReleaseComObject(workingStreamSink);
                        workingStreamSink = null;
                    }
                    if (workingMediaTypeHandler != null)
                    {
                        Marshal.ReleaseComObject(workingMediaTypeHandler);
                        workingMediaTypeHandler = null;
                    }
                    if (workingMediaType != null)
                    {
                        Marshal.ReleaseComObject(workingMediaType);
                        workingMediaType = null;
                    }
                }
            }

            // by the time we get here the outStreamSink has either
            // been set or it has not.
            return outStreamSink;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create a topology node for a source stream. The source node must contain
        /// pointers to the media source, the presentation descriptor, and the 
        /// stream descriptor. This code is just an ecapsulated way of doing
        ///  that. It looks way more complicated than it is.
        /// </summary>
        /// <param name="pSource">the media source</param>
        /// <param name="sourcePresentationDescriptor">the source presentation descriptor</param>
        /// <param name="streamDescriptor">the source stream descriptor</param>
        /// <returns>the source stream node</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static IMFTopologyNode CreateSourceNodeForStream(IMFMediaSource pSource, IMFPresentationDescriptor sourcePresentationDescriptor, IMFStreamDescriptor streamDescriptor)
        {

            HResult hr;
            IMFTopologyNode outSourceNode = null;

            if (pSource == null)
            {
                throw new Exception("No media source object provided");
            }
            if (sourcePresentationDescriptor == null)
            {
                throw new Exception("No source presentation descriptor provided");
            }
            if (streamDescriptor == null)
            {
                throw new Exception("No source stream descriptor provided");
            }

            try
            {
                // A source node represents one stream from a media source. The source node must contain pointers to the media source, 
                // the presentation descriptor, and the stream descriptor. This code is just an ecapsulated way of doing that. 

                // Create the empty structure of the source-stream node.
                hr = MFExtern.MFCreateTopologyNode(MFTopologyType.SourcestreamNode, out outSourceNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSourceNodeForStream call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. Err=" + hr.ToString());
                }
                if (outSourceNode == null)
                {
                    throw new Exception("CreateSourceNodeForStream call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. outSourceNode == null");
                }

                // Set attribute: Pointer to the media source.
                hr = outSourceNode.SetUnknown(MFAttributesClsid.MF_TOPONODE_SOURCE, pSource);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSourceNodeForStream call to pNode.SetUnknown on MF_TOPONODE_SOURCE failed. Err=" + hr.ToString());
                }

                // Set attribute: Pointer to the presentation descriptor.
                hr = outSourceNode.SetUnknown(MFAttributesClsid.MF_TOPONODE_PRESENTATION_DESCRIPTOR, sourcePresentationDescriptor);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSourceNodeForStream call to pNode.SetUnknown on MF_TOPONODE_PRESENTATION_DESCRIPTOR failed. Err=" + hr.ToString());
                }

                // Set attribute: Pointer to the stream descriptor.
                hr = outSourceNode.SetUnknown(MFAttributesClsid.MF_TOPONODE_STREAM_DESCRIPTOR, streamDescriptor);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSourceNodeForStream call to pNode.SetUnknown on MF_TOPONODE_STREAM_DESCRIPTOR failed. Err=" + hr.ToString());
                }

                // Return the IMFTopologyNode pointer to the caller.
                return outSourceNode;
            }
            catch
            {
                // If we failed, release the pnode
                if (outSourceNode != null)
                {
                    Marshal.ReleaseComObject(outSourceNode);
                }
                outSourceNode = null;
                throw;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create a topology node for EVR Video Renderer sink. The caller must
        /// release the returned node.
        /// </summary>
        /// <param name="videoWindowHandle">the handle to the window on which video streams will display</param>
        /// <returns>the ouput stream node</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static IMFTopologyNode CreateEVRRendererOutputNodeForStream(IntPtr videoWindowHandle)
        {
            HResult hr;
            IMFTopologyNode outputNode = null;
            IMFActivate pRendererActivate = null;
 
            try
            {
                // Create a downstream node.
                hr = MFExtern.MFCreateTopologyNode(MFTopologyType.OutputNode, out outputNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateEVRRendererOutputNodeForStream call to MFExtern.MFCreateTopologyNode failed. Err=" + hr.ToString());
                }
                if (outputNode == null)
                {
                    throw new Exception("CreateEVRRendererOutputNodeForStream call to MFExtern.MFCreateTopologyNode failed. outputNode == null");
                }

                // There are two ways to initialize an output node 
                //      1) From a pointer to the stream sink.
                //      2) From a pointer to an activation object for the media sink.
                // since we do not have a stream sink at this point we are going to go the 
                // activation object route. This is what we are doing below.

                // Create an activation object for the enhanced video renderer (EVR) media sink.
                hr = MFExtern.MFCreateVideoRendererActivate(videoWindowHandle, out pRendererActivate);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateEVRRendererOutputNodeForStream call to MFExtern.MFCreateVideoRendererActivate failed. Err=" + hr.ToString());
                }
                if (pRendererActivate == null)
                {
                    throw new Exception("CreateEVRRendererOutputNodeForStream call to MFExtern.MFCreateVideoRendererActivate failed. pRendererActivate == null");
                }

                // Set the IActivate object on the output node. Note that not all node types use
                // this object. On transform nodes this is IMFTransform or IMFActivate interface
                // and on output nodes it is a IMFStreamSink or IMFActivate interface. Not used
                // on source or tee nodes.
                hr = outputNode.SetObject(pRendererActivate);

                // Return the IMFTopologyNode pointer to the caller.
                return outputNode;
            }
            catch
            {
                // If we failed, release the pNode
                if (outputNode != null)
                {
                    Marshal.ReleaseComObject(outputNode);
                }
                throw;
            }
            finally
            {
                // Clean up.
                if (pRendererActivate != null)
                {
                    Marshal.ReleaseComObject(pRendererActivate);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create a topology node for SAR Audio Renderer sink. The caller must
        /// release the returned node.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static IMFTopologyNode CreateSARRendererOutputNodeForStream()
        {
            HResult hr;
            IMFTopologyNode outputNode = null;
            IMFActivate pRendererActivate = null;

            try
            {
                // Create a downstream node.
                hr = MFExtern.MFCreateTopologyNode(MFTopologyType.OutputNode, out outputNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSARRendererOutputNodeForStream call to MFExtern.MFCreateTopologyNode failed. Err=" + hr.ToString());
                }
                if (outputNode == null)
                {
                    throw new Exception("CreateSARRendererOutputNodeForStream call to MFExtern.MFCreateTopologyNode failed. outputNode == null");
                }

                // There are two ways to initialize an output node 
                //      1) From a pointer to the stream sink.
                //      2) From a pointer to an activation object for the media sink.
                // since we do not have a stream sink at this point we are going to go the 
                // activation object route. This is what we are doing below.

                // Create an activation object for the streamin audio renderer (SAR) media sink.
                hr = MFExtern.MFCreateAudioRendererActivate(out pRendererActivate);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateSARRendererOutputNodeForStream call to MFExtern.MFCreateAudioRendererActivate failed. Err=" + hr.ToString());
                }
                if (pRendererActivate == null)
                {
                    throw new Exception("CreateSARRendererOutputNodeForStream call to MFExtern.MFCreateAudioRendererActivate failed. pRendererActivate == null");
                }

                // Set the IActivate object on the output node. Note that not all node types use
                // this object. On transform nodes this is IMFTransform or IMFActivate interface
                // and on output nodes it is a IMFStreamSink or IMFActivate interface. Not used
                // on source or tee nodes.
                hr = outputNode.SetObject(pRendererActivate);

                // Return the IMFTopologyNode pointer to the caller.
                return outputNode;
            }
            catch
            {
                // If we failed, release the pNode
                if (outputNode != null)
                {
                    Marshal.ReleaseComObject(outputNode);
                }
                throw;
            }
            finally
            {
                // Clean up.
                if (pRendererActivate != null)
                {
                    Marshal.ReleaseComObject(pRendererActivate);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Configures the source reader with a video format. This is not as simple as
        /// it seems. Each video source can offer multiple formats and sizes. We have to
        /// match up our preferences with what the video source can handle
        /// </summary>
        /// <param name="sourceReader">the source reader we are configuring</param>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult ConfigureSourceReaderWithVideoFormat(IMFSourceReaderAsync sourceReader, TantaMFVideoFormatContainer videoFormatContainer)
        {
            HResult hr = HResult.S_OK;
            IMFMediaType mediaTypeObj = null;
            // this seems a reasonable maximum
            int maxFormatsToTestFor = 100;

            if (sourceReader == null)
            {
                // we failed
                throw new Exception("ConfigureSourceReaderWithVideoFormat no reader supplied");
            }

            if (videoFormatContainer == null)
            {
                // we failed
                throw new Exception("ConfigureSourceReaderWithVideoFormat no video format container supplied");
            }

            try
            {
                // the code below loops through all media types in the sourceReader. It converts them to a
                // temporary TantaMFVideoFormatContainer. Once we have that we compare it against the input
                // container. If we get a match we set the source reader to use tha media type.

                // loop through a reasonable number of mediaTypes looking for a match
                for (int typeIndex = 0; typeIndex < maxFormatsToTestFor; typeIndex++)
                {
                    // get the next media type
                    hr = sourceReader.GetNativeMediaType(TantaWMFUtils.MF_SOURCE_READER_FIRST_VIDEO_STREAM, typeIndex, out mediaTypeObj);
                    if (hr == HResult.MF_E_NO_MORE_TYPES)
                    {
                        // we are all done. The outSb container has been populated
                        return HResult.S_OK;
                    }
                    else if (hr != HResult.S_OK)
                    {
                        // we failed
                        throw new Exception("ConfigureSourceReaderWithVideoFormat failed on call to GetNativeMediaType, retVal=" + hr.ToString());
                    }

                    // get a temporary format container from the media type
                    TantaMFVideoFormatContainer tmpContainer = TantaMediaTypeInfo.GetVideoFormatContainerFromMediaTypeObject(mediaTypeObj, videoFormatContainer.VideoDevice);
                    if (tmpContainer == null)
                    {
                        // we failed
                        throw new Exception("ConfigureSourceReaderWithVideoFormat failed on call to GetVideoFormatContainerFromMediaTypeObject");
                    }

                    // does this container match the one that was passed in?
                    if (videoFormatContainer.CompareTo(tmpContainer) == 0)
                    {
                        // yes it matches. This is the format that was specified. We can configure with this
                        // set the media type on the reader
                        hr = sourceReader.SetCurrentMediaType(TantaWMFUtils.MF_SOURCE_READER_FIRST_VIDEO_STREAM, null, mediaTypeObj);
                        if (hr != HResult.S_OK)
                        {
                            // we failed
                            throw new Exception("ConfigureSourceReaderWithVideoFormat failed on call to SetCurrentMediaType, retVal=" + hr.ToString());
                        }

                        // release the media type
                        if(mediaTypeObj!=null)
                        {
                            Marshal.ReleaseComObject(mediaTypeObj);
                            mediaTypeObj = null;
                        }

                        // we are done
                        return HResult.S_OK;
                    }
                }

                // if we get here we failed, we could not match the input format
                return HResult.E_FAIL;
            }
            finally
            {
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Configures the source reader with a video format. This is not as simple as
        /// it seems. Each video source can offer multiple formats and sizes. We have to
        /// match up our preferences with what the video source can handle
        /// </summary>
        /// <param name="sourceReader">the source reader we are configuring</param>
        /// <param name="subTypes">a list of MediaSubtypes we are prepared to use in preferred order</param>
        /// <param name="useNonNativeTypes">if true we can also test for non native types</param>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult ConfigureSourceReaderWithVideoFormat(IMFSourceReaderAsync sourceReader, List<Guid> subTypes, bool useNonNativeTypes)
        {

            HResult hr = HResult.S_OK;

            IMFMediaType mediaTypeObj = null;
            Guid subtype;
            bool matchedNativeType = false;

            if (sourceReader == null)
            {
                // we failed
                throw new Exception("ConfigureSourceReaderWithVideoFormat no reader supplied");
            }

            try
            {
                // this call gets a format that is supported natively by the media source. 
                // The method queries the underlying media source for its native output format. 
                // Potentially, each source stream can produce more than one output format.
                // The dwMediaTypeIndex parameter can be used to loop through the available formats.
                // Generally, file sources offer just one format per stream, but capture devices might offer several formats.
                // The method returns a copy of the media type, so it is safe to modify the object received in 
                // the "out mediaTypeObj" parameter. This parameter has to be released though.

                // note this only gets the very first supported format. (thats what the zero does) we could conceivably 
                // use any of the others. For more information, see how GetSupportedVideoFormatsFromSourceReaderAsText 
                // produces a list. We are just taking the simplest case here.

                hr = sourceReader.GetNativeMediaType(TantaWMFUtils.MF_SOURCE_READER_FIRST_VIDEO_STREAM, 0, out mediaTypeObj);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("ConfigureSourceReaderWithVideoFormat failed on call to GetNativeMediaType, retVal=" + hr.ToString());
                }

                // Get the GUID value associated with a MF_MT_SUBTYPE key.
                hr = mediaTypeObj.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out subtype);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("ConfigureSourceReaderWithVideoFormat failed on call to mediaTypeObj.GetGUID, retVal=" + hr.ToString());
                }

                // now loop through and check, does this match any of the ones we want?
                foreach (Guid guidValue in subTypes)
                {
                    if (subtype == guidValue)
                    {
                        // set the media type on the reader
                        hr = sourceReader.SetCurrentMediaType(TantaWMFUtils.MF_SOURCE_READER_FIRST_VIDEO_STREAM, null, mediaTypeObj);
                        // flag it
                        matchedNativeType = true;
                        break;
                    }
                }
                if (matchedNativeType == false)
                {
                    // if we get here we failed
                    return HResult.E_FAIL;
                }
                return HResult.S_OK;
            }
            finally
            {


            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens the up a SourceReader.  
        /// 
        /// A SourceReader is an alternative to using the Media Session and the 
        /// Microsoft Media Foundation pipeline to process media data. The SourceReader
        /// encapsulates a lot of functionality you would otherwise need to handle 
        /// yourself. 
        /// 
        /// For example, if the media source delivers compressed data, you 
        /// can use the source reader to decode the data. In that case, the source 
        /// reader will load the correct decoder and manage the data flow between 
        /// the media source and the decoder. The source reader can also perform 
        /// some limited video processing such as color conversion from YUV to 
        /// RGB-32 etc.
        /// 
        /// NOTE: It is the callers responsibility to clean up and properly dispose
        ///       of the SourceReader object returned here.
        /// 
        /// </summary>
        /// <param name="sourceDevice">the Device we use for the source</param>
        /// <param name="asyncCallBackHandlerIn">the call back handler for async mode</param>
        /// <returns>an IMFSourceReaderAsync object or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static IMFSourceReaderAsync CreateSourceReaderAsyncFromDevice(TantaMFDevice sourceDevice, IMFSourceReaderCallback asyncCallBackHandlerIn)
        {
            HResult hr = HResult.S_OK;
            IMFMediaSource mediaSource = null;

            IMFAttributes attributeContainer = null;

            if (sourceDevice == null)
            {
                throw new Exception("CreateSourceReaderAsyncFromDevice: Null source device specified. Cannot continue.");
            }
            if (asyncCallBackHandlerIn == null)
            {
                throw new Exception("CreateSourceReaderAsyncFromDevice: asyncCallBackHandlerIn != null");
            }

            try
            {
                // use the device symbolic name to create the media source for the device. Media sources are objects that generate media data. 
                // For example, the data might come from a video file, a network stream, or a hardware device, such as a camera. Each 
                // media source contains one or more streams, and each stream delivers data of one type, such as audio or video.                
                mediaSource = TantaWMFUtils.GetMediaSourceFromTantaDevice(sourceDevice);
                if (mediaSource == null)
                {
                    throw new Exception("CreateSourceReaderAsyncFromDevice: mediaSource == null. Cannot continue.");
                }

                // Initialize an attribute store. The 2 is the number of initial attributes which can be stored 
                hr = MFExtern.MFCreateAttributes(out attributeContainer, 2);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CreateSourceReaderAsyncFromDevice failed on call to MFCreateAttributes, retVal=" + hr.ToString());
                }

                // Set our callback handler as an IUnknown pointer in the attribute container. 
                hr = attributeContainer.SetUnknown(MFAttributesClsid.MF_SOURCE_READER_ASYNC_CALLBACK, asyncCallBackHandlerIn);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CreateSourceReaderAsyncFromDevice failed on call to attributeContainer.SetUnknown, retVal=" + hr.ToString());
                }

                // Create the SourceReader. We will no longer need our media source object after this, the SourceReader
                // will maintain its own pointer into it and will clean it up properly when it is closed down.
                IMFSourceReader sourceReader;
                hr = MFExtern.MFCreateSourceReaderFromMediaSource(mediaSource, attributeContainer, out sourceReader);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CreateSourceReaderAsyncFromDevice failed on call to MFCreateSourceReaderFromMediaSource, retVal=" + hr.ToString());
                }
                return (IMFSourceReaderAsync)sourceReader;
            }
            finally
            {
                // make sure we release the attribute memory
                if (attributeContainer != null)
                {
                    Marshal.ReleaseComObject(attributeContainer);
                    attributeContainer = null;
                }

                // close and release the source device
                if (mediaSource != null)
                {
                    Marshal.ReleaseComObject(mediaSource);
                    mediaSource = null;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens the Source Reader object
        /// </summary>
        /// <param name="inFileName">the filename we write read from</param>
        /// <param name="wantAllowHardwareTransforms">if true we allow hardware transforms</param>
        /// <returns>a IMFSourceReader object or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static IMFSourceReader CreateSourceReaderSyncFromFile(string inputFileName, bool wantAllowHardwareTransforms)
        {
            HResult hr;
            IMFSourceReader workingReader = null;
            IMFAttributes sourceReaderAttributes = null;

            if ((inputFileName == null) || (inputFileName.Length == 0))
            {
                // we failed
                throw new Exception("CreateSourceReaderSyncFromFile: Invalid filename specified");
            }

            try
            {
                // create the attribute container we use to create the source reader
                hr = MFExtern.MFCreateAttributes(out sourceReaderAttributes, 1);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CreateSourceReaderSyncFromFile: Failed on call to MFCreateAttributes, retVal=" + hr.ToString());
                }
                if (sourceReaderAttributes == null)
                {
                    // we failed
                    throw new Exception("CreateSourceReaderSyncFromFile: Failed to create Source Reader Attributes, Nothing will work.");
                }
                hr = sourceReaderAttributes.SetUINT32(MFAttributesClsid.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, wantAllowHardwareTransforms ? 1 : 0);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CreateSourceReaderSyncFromFile: Failed on call to sourceReaderAttributes.SetUINT32, retVal=" + hr.ToString());
                }

                // Create the SourceReader. This takes the URL of an input file or a pointer to a byte stream and
                // creates the media source internally. 
                hr = MFExtern.MFCreateSourceReaderFromURL(inputFileName, sourceReaderAttributes, out workingReader);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CreateSourceReaderSyncFromFile: Failed on call to MFCreateSourceReaderFromURL, retVal=" + hr.ToString());
                }
                if (workingReader == null)
                {
                    // we failed
                    throw new Exception("CreateSourceReaderSyncFromFile: Failed to create Source Reader, Nothing will work.");
                }
            }
            catch (Exception ex)
            {
                // note this clean up is in the Catch block not the finally block. 
                // if there are no errors we return it to the caller. The caller
                // is expected to clean up after itself
                if (workingReader != null)
                {
                    // clean up. Nothing else has this yet
                    Marshal.ReleaseComObject(workingReader);
                    workingReader = null;
                }
                workingReader = null;
                throw ex;
            }
            finally
            {
                if (sourceReaderAttributes != null)
                {
                    Marshal.ReleaseComObject(sourceReaderAttributes);
                }
            }

            return workingReader;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens the Sink Writer object
        /// </summary>
        /// <param name="outputFileName">the filename we write out to</param>
        /// <param name="wantAllowHardwareTransforms">if true we allow hardware transforms</param>
        /// <returns>an IMFSinkWriter object or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static IMFSinkWriter CreateSinkWriterFromFile(string outputFileName, bool wantAllowHardwareTransforms)
        {
            HResult hr;
            IMFSinkWriter workingWriter = null;
            IMFAttributes sinkWriterAttributes = null;

            if ((outputFileName == null) || (outputFileName.Length == 0))
            {
                // we failed
                throw new Exception("CreateSinkWriterFromFile: Invalid filename specified");
            }

            try
            {
                // create the attribute container we use to create the source reader
                hr = MFExtern.MFCreateAttributes(out sinkWriterAttributes, 1);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CreateSinkWriterFromFile: Failed on call to MFCreateAttributes, retVal=" + hr.ToString());
                }
                if (sinkWriterAttributes == null)
                {
                    // we failed
                    throw new Exception("CreateSinkWriterFromFile: Failed to create Sink Writer Attributes, Nothing will work.");
                }
                hr = sinkWriterAttributes.SetUINT32(MFAttributesClsid.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS, wantAllowHardwareTransforms ? 1 : 0);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CreateSinkWriterFromFile: Failed on call to sourceReaderAttributes.SetUINT32, retVal=" + hr.ToString());
                }

                // Create the sink writer. This takes the URL of an output file or a pointer to a byte stream and
                // creates the media sink internally. You could also use the more round-about 
                // MFCreateSinkWriterFromMediaSink takes a pointer to a media sink that has already been created by
                // the application. If you are using one of the built-in media sinks, the MFCreateSinkWriterFromURL 
                // function is preferable, because the caller does not need to configure the media sink. 
                hr = MFExtern.MFCreateSinkWriterFromURL(outputFileName, null, sinkWriterAttributes, out workingWriter);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("CreateSinkWriterFromFile: Failed on call to MFCreateSinkWriterFromURL, retVal=" + hr.ToString());
                }
                if (workingWriter == null)
                {
                    // we failed
                    throw new Exception("CreateSinkWriterFromFile: Failed to create Sink Writer, Nothing will work.");
                }
            }
            catch (Exception ex)
            {
                // note this clean up is in the Catch block not the finally block. 
                // if there are no errors we return it to the caller. The caller
                // is expected to clean up after itself
                if (workingWriter != null)
                {
                    // clean up. Nothing else has this yet
                    Marshal.ReleaseComObject(workingWriter);
                    workingWriter = null;
                }
                workingWriter = null;
                throw ex;
            }
            finally
            {
                if (sinkWriterAttributes != null)
                {
                    Marshal.ReleaseComObject(sinkWriterAttributes);
                }
            }
            return workingWriter;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Copies attribute data from one IMFAttributes container to another
        /// </summary>
        /// <param name="srcAttr">the source Attribute</param>
        /// <param name="tgtAttr">the destination Attribute</param>
        /// <param name="key">The key in the attribute we copy</param>
        /// <returns>z - success, nz - fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult CopyAttributeData(IMFAttributes srcAttr, IMFAttributes tgtAttr, Guid key)
        {
            PropVariant var = new PropVariant();
            HResult hr = HResult.S_OK;

            if (srcAttr == null) return HResult.S_FALSE;
            if (tgtAttr == null) return HResult.S_FALSE;

            // get the source data
            hr = srcAttr.GetItem(key, var);
            if (hr != HResult.S_OK) return hr;

            // get the target data
            hr = tgtAttr.SetItem(key, var);
            return hr;
        }


        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Returns a list of devices matching the specified category. Never returns
        /// null. Largely derived from the MF.Net sample code.
        /// </summary>
        /// <param name="attributeType">this tells us if we want a source or a sink: IE MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE</param>
        /// <param name="FilterCategory">This will be CLSID.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID for video devices</param>
        /// <returns>a list of devices</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static List<TantaMFDevice> GetDevicesByCategory(Guid attributeType, Guid filterCategory)
        {
            // our return value
            List<TantaMFDevice> outList = new List<TantaMFDevice>();
            IMFActivate[] deviceArr;
            int numDevices = 0;
            HResult hr = 0;
            IMFAttributes attributeContainer = null;

            try
            {
                // Initialize an attribute store. We will use this to 
                // specify the enumeration parameters.
                hr = MFExtern.MFCreateAttributes(out attributeContainer, 1);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("GetDevicesByCategory failed on call to MFCreateAttributes, retVal=" + hr.ToString());
                }
                if (attributeContainer == null)
                {
                    // we failed
                    throw new Exception("GetDevicesByCategory failed on call to MFEnumDeviceSources, attributeContainer == MFAttributesClsid.null");
                }

                // populate the attribute container
                hr = attributeContainer.SetGUID(attributeType, filterCategory);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("GetDevicesByCategory failed setting up the attributes, retVal=" + hr.ToString());
                }

                // Enumerate the devices. 
                hr = MFExtern.MFEnumDeviceSources(attributeContainer, out deviceArr, out numDevices);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("GetDevicesByCategory failed on call to MFEnumDeviceSources, retVal=" + hr.ToString());
                }
                if (deviceArr == null)
                {
                    // this can happen if there are no devices on the system
                    // just set up the array 
                    deviceArr = new IMFActivate[0];
                }

                // deviceArr here is an array of IMFActivate containers. We have to be careful with these. 

                // The IMFActivate container enables the application to defer the creation of an object. Typically, the application 
                // calls some function that returns an IMFActivate pointer and passes this to another component. 
                // This component calls ActivateObject at a later time to create the object. You can only an IMFActivate 
                // container to create one object. After that you have to get another - if you try to re-use it you will get 
                // a variety of errors one of which can be MF_E_SHUTDOWN even though the MFSubstrate has not been shutdown
                // and is, in fact, still active

                // So, we could store the IMFActivate containers in this array to later create a media source. However, that is a bit
                // dangerous. If some other component uses it then it can never be re-used. Even if it gets released properly then
                // how do we use that device again without enumerating everthing all over again. 

                // What we do is use the activator to get the friendly name and symbolic link name for the device. These
                // are just strings. Later we can use TantaWMFUtils.GetMediaSourceFromTantaDevice to build a video source
                // from the symbolic name and the activator here will have long since been cleaned up and released.

                // add the devices to our list as TantaMFDevices
                for (int i = 0; i < numDevices; i++)
                {
                    // extract the friendlyName and symbolicLinkName
                    string symbolicLinkName = GetStringForKeyFromActivator(deviceArr[i], MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK);
                    string friendlyName = GetStringForKeyFromActivator(deviceArr[i], MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME);

                    // create the new TantaMFDevice
                    outList.Add(new TantaMFDevice(friendlyName, symbolicLinkName, filterCategory));

                    // clean up our activator
                    Marshal.ReleaseComObject(deviceArr[i]);
                }
            }
            finally
            {
                // make sure we release the attribute memory
                if (attributeContainer != null)
                {
                    Marshal.ReleaseComObject(attributeContainer);
                }
            }
            return outList;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a string for a given key value from a device activator. Will never return null
        /// will return ""
        /// </summary>
        /// <param name="activatorContainer">the activator container object</param>
        /// <param name="guidKey">the key for the string we lookup</param>
        /// <returns>the string or empty for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static string GetStringForKeyFromActivator(IMFActivate activatorContainer, Guid guidKey)
        {
            string allocatedStr = "";
            HResult hr = 0;
            int iSize = 0;

            if (activatorContainer == null) return "";

            // get it now. 
            hr = activatorContainer.GetAllocatedString(
                guidKey,
                out allocatedStr,
                out iSize
                );
            if (hr != HResult.S_OK) return "";

            // sanity check
            if (allocatedStr == null) allocatedStr = "";
            return allocatedStr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a Guid for a given key value from a device activator. Will never return null
        /// will return ""
        /// </summary>
        /// <param name="activatorContainer">the activator container object</param>
        /// <param name="guidKey">the key for the string we lookup</param>
        /// <returns>the guid or Guid.Empty for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static Guid GetGuidForKeyFromActivator(IMFActivate activatorContainer, Guid guidKey)
        {
            Guid outGuid = Guid.Empty;
            HResult hr = 0;

            if (activatorContainer == null) return Guid.Empty;

            // get it now. 
            hr = activatorContainer.GetGUID(
                guidKey,
                out outGuid
                );
            if (hr != HResult.S_OK) return Guid.Empty;

            return outGuid;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all attributes contained in an object implementing the IMFAttributes interface and displays
        /// them as a human readable name. 
        /// 
        /// Adapted from
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ee663602(v=vs.85).aspx
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="attributesContainer">the attributes container</param>
        /// <param name="maxAttributes">the maximum number of attributes</param>
        /// <param name="outSb">The output string</param>
        /// <param name="attributesToIgnore">a list of the attributes we ignore and do not report, can be null</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult EnumerateAllAttributeNamesAsText(IMFAttributes attributesContainer, List<string> attributesToIgnore, int maxAttributes, out StringBuilder outSb)
        {

            Guid guid;
            PropVariant pv = new PropVariant();

            // we always return something here
            outSb = new StringBuilder();

            // sanity check
            if (attributesContainer == null) return HResult.E_FAIL;

            // loop through all possible attributes
            for (int attrIndex = 0; attrIndex < maxAttributes; attrIndex++)
            {
                // get the attribute from the mediaType object
                HResult hr = attributesContainer.GetItemByIndex(attrIndex, out guid, pv);
                if (hr == HResult.E_INVALIDARG)
                {
                    // we are all done, outSb should be updated
                    return HResult.S_OK;
                }
                if (hr != HResult.S_OK)
                {
                    // we failed
                    return HResult.E_FAIL;
                }
                string outName = TantaWMFUtils.ConvertGuidToName(guid);
                // are we ignoring certain ones
                if ((attributesToIgnore != null) && (attributesToIgnore.Contains(outName))) continue;
                outSb.Append(outName + ",");
            }

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all attribute names and values contained in an object 
        /// implementing the IMFAttributes interface and returns them as a list
        /// of human readable name strings.
        /// 
        /// Adapted from
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ee663602(v=vs.85).aspx
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="attributesContainer">the attributes container</param>
        /// <param name="maxAttributes">the maximum number of attributes</param>
        /// <param name="attrNamesAndValuesList">The output names and values</param>
        /// <param name="attributesToIgnore">a list of the attributes we ignore and do not report, can be null</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult EnumerateAllAttributeNamesAndValuesAsText(IMFAttributes attributesContainer, List<string> attributesToIgnore, int maxAttributes, out List<string> attrNamesAndValuesList)
        {

            Guid guid;
            PropVariant pv = new PropVariant();
  
            // we always return something here
            attrNamesAndValuesList = new List<string>();

            // sanity check
            if (attributesContainer == null) return HResult.E_FAIL;

            // loop through all possible attributes
            for (int attrIndex = 0; attrIndex < maxAttributes; attrIndex++)
            {
                // get the attribute from the mediaType object
                HResult hr = attributesContainer.GetItemByIndex(attrIndex, out guid, pv);
                if (hr == HResult.E_INVALIDARG)
                {
                    // we are all done, outSb should be updated
                    return HResult.S_OK;
                }
                if (hr != HResult.S_OK)
                {
                    // we failed
                    return HResult.E_FAIL;
                }
                string outName = TantaWMFUtils.ConvertGuidToName(guid);
                // are we ignoring certain ones
                if ((attributesToIgnore != null) && (attributesToIgnore.Contains(outName))) continue;
   
                // now convert the values
                string valueStr = ConvertPropVariantToString(pv, true);

                attrNamesAndValuesList.Add(outName + "=" + valueStr);
            }

            attrNamesAndValuesList.Sort();

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the values out of a propvariant as a string
        /// 
        /// </summary>
        /// <returns>The ToString() of the propvariant or "" for fail. Will never return null</returns>
        /// <param name="propVariant">the propvariant</param>
        /// <param name="appendGuidToGuidNames">if true we append the guid value to the guid name</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static string ConvertPropVariantToString(PropVariant propVariant, bool appendGuidToGuidNames)
        {
            if (propVariant == null) return "no value";
            if(propVariant.GetVariantType() == ConstPropVariant.VariantType.Guid)
            {
                Guid pvGuid = propVariant.GetGuid();
                if (appendGuidToGuidNames == true)
                {
                    return TantaWMFUtils.ConvertGuidToName(pvGuid)+ " ("+ pvGuid.ToString()+")";
                }
                else
                {
                    return TantaWMFUtils.ConvertGuidToName(pvGuid);
                }
            }
            return propVariant.ToString();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all input types from an Activator of a Transform 
        /// 
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="activatorObject">the activator object</param>
        /// <param name="rtInfoList">a list of populated MFTRegisterTypeInfo objects</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult GetInputTypesForTransformFromActivator(IMFActivate activatorObject, out List<MFTRegisterTypeInfo> rtInfoList)
        {
            int outSize;
            IntPtr outBlob = IntPtr.Zero;

            rtInfoList = new List<MFTRegisterTypeInfo>();

            if (activatorObject == null) return HResult.E_FAIL;

            // get the data from the activator. This comes out in a blob which is actually
            // an array of MFT_REGISTER_TYPE_INFO structs. 
            HResult hr = activatorObject.GetAllocatedBlob(MFAttributesClsid.MFT_INPUT_TYPES_Attributes, out outBlob, out outSize);
            if (hr != HResult.S_OK)
            {
                return HResult.E_FAIL;
            }

            // get the size of a MFTRegisterTypeInfo class. We have to 
            // use Marshal because Guids are supposedly not of fixed size.
            // The only reason this works is because the MFTRegisterTypeInfo
            // class has a "StructLayout(LayoutKind.Sequential)" decoration
            int sizeOfMFTRegisterTypeInfo = Marshal.SizeOf(typeof(MFTRegisterTypeInfo));
            if (sizeOfMFTRegisterTypeInfo <= 0) return HResult.E_FAIL;

            // calculate the number of records in the blob
            int numRecords = outSize / sizeOfMFTRegisterTypeInfo;

            // to get at the information in the blob, we convert the start of each 
            // MFT_REGISTER_TYPE_INFO struct to an IntPtr then copy it into a
            // MFTRegisterTypeInfo class
            for (int i = 0; i < numRecords; i++)
            {
                // get a pointer to the next MFT_REGISTER_TYPE_INFO struct
                IntPtr intPtrToStruct = new IntPtr(outBlob.ToInt64() + i * sizeOfMFTRegisterTypeInfo);
                // copy the contents at that pointer into a MFTRegisterTypeInfo class 
                MFTRegisterTypeInfo tmpRTInfo = Marshal.PtrToStructure<MFTRegisterTypeInfo>(intPtrToStruct);
                if (tmpRTInfo == null) continue;
                // add it to our container
                rtInfoList.Add(tmpRTInfo);
            }

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets a list of all output types from an Activator of a Transform 
        /// 
        /// </summary>
        /// <returns>S_OK for success, nz for fail</returns>
        /// <param name="activatorObject">the activator object</param>
        /// <param name="rtInfoList">a list of populated MFTRegisterTypeInfo objects</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static HResult GetOutputTypesForTransformFromActivator(IMFActivate activatorObject, out List<MFTRegisterTypeInfo> rtInfoList)
        {
            int outSize;
            IntPtr outBlob = IntPtr.Zero;

            rtInfoList = new List<MFTRegisterTypeInfo>();

            if (activatorObject == null) return HResult.E_FAIL;

            // get the data from the activator. This comes out in a blob which is actually
            // an array of MFT_REGISTER_TYPE_INFO structs. 
            HResult hr = activatorObject.GetAllocatedBlob(MFAttributesClsid.MFT_OUTPUT_TYPES_Attributes, out outBlob, out outSize);
            if (hr != HResult.S_OK)
            {
                return HResult.E_FAIL;
            }

            // get the size of a MFTRegisterTypeInfo class. We have to 
            // use Marshal because Guids are supposedly not of fixed size.
            // The only reason this works is because the MFTRegisterTypeInfo
            // class has a "StructLayout(LayoutKind.Sequential)" decoration
            int sizeOfMFTRegisterTypeInfo = Marshal.SizeOf(typeof(MFTRegisterTypeInfo));
            if (sizeOfMFTRegisterTypeInfo <= 0) return HResult.E_FAIL;

            // calculate the number of records in the blob
            int numRecords = outSize / sizeOfMFTRegisterTypeInfo;

            // to get at the information in the blob, we convert the start of each 
            // MFT_REGISTER_TYPE_INFO struct to an IntPtr then copy it into a
            // MFTRegisterTypeInfo class
            for (int i = 0; i < numRecords; i++)
            {
                // get a pointer to the next MFT_REGISTER_TYPE_INFO struct
                IntPtr intPtrToStruct = new IntPtr(outBlob.ToInt64() + i * sizeOfMFTRegisterTypeInfo);
                // copy the contents at that pointer into a MFTRegisterTypeInfo class 
                MFTRegisterTypeInfo tmpRTInfo = Marshal.PtrToStructure<MFTRegisterTypeInfo>(intPtrToStruct);
                if (tmpRTInfo == null) continue;
                // add it to our container
                rtInfoList.Add(tmpRTInfo);
            }

            return HResult.S_OK;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Returns a media source from the contents of a TantaMFDevice
        /// </summary>
        /// <param name="sourceDevice">the source device</param>
        /// <returns>a IMFMediaSource or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static IMFMediaSource GetMediaSourceFromTantaDevice(TantaMFDevice sourceDevice)
        {
            IMFMediaSource mediaSource = null;
            HResult hr = 0;
            IMFAttributes attributeContainer = null;

            try
            {
                if (sourceDevice == null) 
                {
                    // we failed
                    throw new Exception("GetMediaSourceFromTantaDevice sourceDevice == null");
                }
                if ((sourceDevice.SymbolicName == null) || (sourceDevice.SymbolicName.Length == 0))
                {
                    // we failed
                    throw new Exception("GetMediaSourceFromTantaDevice failed null or bad symbolicLinkStr");
                }
                if (sourceDevice.DeviceType == Guid.Empty)
                {
                    // we failed
                    throw new Exception("GetMediaSourceFromTantaDevice DeviceType == Guid.Empty");
                }

                // Initialize an attribute store. We will use this to 
                // specify the device parameters.
                hr = MFExtern.MFCreateAttributes(out attributeContainer, 2);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("GetMediaSourceFromTantaDevice failed on call to MFCreateAttributes, retVal=" + hr.ToString());
                }
                if (attributeContainer == null)
                {
                    // we failed
                    throw new Exception("GetMediaSourceFromTantaDevice failed on call to MFEnumDeviceSources, attributeContainer == null");
                }

                // setup the attribute container, it is always a MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE here
                hr = attributeContainer.SetGUID(MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, sourceDevice.DeviceType);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("GetMediaSourceFromTantaDevice failed setting up the attributes, retVal=" + hr.ToString());
                }

                // set the formal (symbolic name) name of the device as an attribute.
                hr = attributeContainer.SetString(MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK, sourceDevice.SymbolicName);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("GetMediaSourceFromTantaDevice failed setting up the symbolic name, retVal=" + hr.ToString());
                }

                // get the media source from the symbolic name 
                hr = MFExtern.MFCreateDeviceSource(attributeContainer, out mediaSource);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("GetMediaSourceFromTantaDevice failed on call to MFCreateDeviceSource, retVal=" + hr.ToString());
                }
            }
            finally
            {
                // make sure we release the attribute memory
                if (attributeContainer != null)
                {
                    Marshal.ReleaseComObject(attributeContainer);
                }
            }
            return mediaSource;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Returns a media source from a file name
        /// </summary>
        /// <param name="mediaFileName">the full name and path of the media file</param>
        /// <returns>a IMFMediaSource or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static IMFMediaSource GetMediaSourceFromFile(string mediaFileName)
        {
            IMFMediaSource mediaSource = null;
            IMFSourceResolver pSourceResolver = null;
            object pSource = null;
            HResult hr = 0;

            try
            {
                // As with so many things WMF, the creation of the media source is indirect
                // We now create a source resolver which will be used to create a media source
                //  from a URL, filename or byte stream. The call below returns an 
                // IMFSourceResolver interface pointer.
                hr = MFExtern.MFCreateSourceResolver(out pSourceResolver);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetMediaSourceFromFile call to MFExtern.MFCreateSourceResolver failed. Err=" + hr.ToString());
                }
                if (pSourceResolver == null)
                {
                    throw new Exception("GetMediaSourceFromFile call to MFExtern.MFCreateSourceResolver failed. pSourceResolver == null");
                }

                // here we use our source resolver to create the media source
                MFObjectType objectType = MFObjectType.Invalid;
                hr = pSourceResolver.CreateObjectFromURL(
                        mediaFileName,              // URL (file path and name) of the source.
                        MFResolution.MediaSource,   // Create a source object.
                        null,                       // Optional property store.
                        out objectType,             // Receives the created object type.
                        out pSource                 // Receives a pointer to the media source.
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("GetMediaSourceFromFile call to pSourceResolver.CreateObjectFromURL failed. Err=" + hr.ToString());
                }
                if (pSource == null)
                {
                    throw new Exception("GetMediaSourceFromFile call to pSourceResolver.CreateObjectFromURL failed. pSource == null");
                }
                // Cast the output into our media source object
                mediaSource = (IMFMediaSource)pSource;

            }
            finally
            {
                // make sure we clean up
                if (pSourceResolver != null)
                {
                    Marshal.ReleaseComObject(pSourceResolver);
                }

            }
            return mediaSource;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a guid to a name. Never returns null. Inspired by 
        ///   https://msdn.microsoft.com/en-us/library/windows/desktop/ee663602(v=vs.85).aspx
        /// clipped and adapted from the MF.Net source. There still appear to be 
        /// plenty in there I have not represented here.
        /// </summary>
        /// <returns>the Guid as a text name or empty string for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public static string ConvertGuidToName(Guid guidToConvert)
        {
            if (guidToConvert == MFMediaType.Default) return "MFMediaType";
            if (guidToConvert == MFMediaType.Audio) return "MFMediaType_Audio";
            if (guidToConvert == MFMediaType.Video) return "MFMediaType_Video";
            if (guidToConvert == MFMediaType.Protected) return "MFMediaType_Protected";
            if (guidToConvert == MFMediaType.SAMI) return "MFMediaType_SAMI";
            if (guidToConvert == MFMediaType.Script) return "MFMediaType_Script";
            if (guidToConvert == MFMediaType.Image) return "MFMediaType_Image";
            if (guidToConvert == MFMediaType.HTML) return "MFMediaType_HTML";
            if (guidToConvert == MFMediaType.Binary) return "MFMediaType_Binary";
            if (guidToConvert == MFMediaType.FileTransfer) return "MFMediaType_FileTransfer";
            if (guidToConvert == MFMediaType.Stream) return "MFMediaType_Stream";

            if (guidToConvert == MFMediaType.Base) return "Base";
            if (guidToConvert == MFMediaType.PCM) return "PCM";
            if (guidToConvert == MFMediaType.Float) return "Float";
            if (guidToConvert == MFMediaType.DTS) return "DTS";
            if (guidToConvert == MFMediaType.Dolby_AC3_SPDIF) return "Dolby_AC3_SPDIF";
            if (guidToConvert == MFMediaType.DRM) return "DRM";
            if (guidToConvert == MFMediaType.WMAudioV8) return "WMAudioV8";
            if (guidToConvert == MFMediaType.WMAudioV9) return "WMAudioV9";
            if (guidToConvert == MFMediaType.WMAudio_Lossless) return "WMAudio_Lossless";
            if (guidToConvert == MFMediaType.WMASPDIF) return "WMASPDIF";
            if (guidToConvert == MFMediaType.MSP1) return "MSP1";
            if (guidToConvert == MFMediaType.MP3) return "MP3";
            if (guidToConvert == MFMediaType.MPEG) return "MPEG";
            if (guidToConvert == MFMediaType.AAC) return "AAC";
            if (guidToConvert == MFMediaType.ADTS) return "ADTS";
            if (guidToConvert == MFMediaType.AMR_NB) return "AMR_NB";
            if (guidToConvert == MFMediaType.AMR_WB) return "AMR_WB";
            if (guidToConvert == MFMediaType.AMR_WP) return "AMR_WP";

            // {00000000-767a-494d-b478-f29d25dc9037}       MFMPEG4Format_Base
            if (guidToConvert == MFMediaType.MFMPEG4Format) return "MFMPEG4Format";

            if (guidToConvert == MFMediaType.RGB32) return "RGB32";
            if (guidToConvert == MFMediaType.ARGB32) return "ARGB32";
            if (guidToConvert == MFMediaType.RGB24) return "RGB24";
            if (guidToConvert == MFMediaType.RGB555) return "RGB555";
            if (guidToConvert == MFMediaType.RGB565) return "RGB565";
            if (guidToConvert == MFMediaType.RGB8) return "RGB8";
            if (guidToConvert == MFMediaType.AI44) return "AI44";
            if (guidToConvert == MFMediaType.AYUV) return "AYUV";
            if (guidToConvert == MFMediaType.YUY2) return "YUY2";
            if (guidToConvert == MFMediaType.YVYU) return "YVYU";
            if (guidToConvert == MFMediaType.YVU9) return "YVU9";
            if (guidToConvert == MFMediaType.UYVY) return "UYVY";
            if (guidToConvert == MFMediaType.NV11) return "NV11";
            if (guidToConvert == MFMediaType.NV12) return "NV12";
            if (guidToConvert == MFMediaType.YV12) return "YV12";
            if (guidToConvert == MFMediaType.I420) return "I420";
            if (guidToConvert == MFMediaType.IYUV) return "IYUV";
            if (guidToConvert == MFMediaType.Y210) return "Y210";
            if (guidToConvert == MFMediaType.Y216) return "Y216";
            if (guidToConvert == MFMediaType.Y410) return "Y410";
            if (guidToConvert == MFMediaType.Y416) return "Y416";
            if (guidToConvert == MFMediaType.Y41P) return "Y41P";
            if (guidToConvert == MFMediaType.Y41T) return "Y41T";
            if (guidToConvert == MFMediaType.Y42T) return "Y42T";
            if (guidToConvert == MFMediaType.P210) return "P210";
            if (guidToConvert == MFMediaType.P216) return "P216";
            if (guidToConvert == MFMediaType.P010) return "P010";
            if (guidToConvert == MFMediaType.P016) return "P016";
            if (guidToConvert == MFMediaType.v210) return "v210";
            if (guidToConvert == MFMediaType.v216) return "v216";
            if (guidToConvert == MFMediaType.v410) return "v410";
            if (guidToConvert == MFMediaType.MP43) return "MP43";
            if (guidToConvert == MFMediaType.MP4S) return "MP4S";
            if (guidToConvert == MFMediaType.M4S2) return "M4S2";
            if (guidToConvert == MFMediaType.MP4V) return "MP4V";
            if (guidToConvert == MFMediaType.WMV1) return "WMV1";
            if (guidToConvert == MFMediaType.WMV2) return "WMV2";
            if (guidToConvert == MFMediaType.WMV3) return "WMV3";
            if (guidToConvert == MFMediaType.WVC1) return "WVC1";
            if (guidToConvert == MFMediaType.MSS1) return "MSS1";
            if (guidToConvert == MFMediaType.MSS2) return "MSS2";
            if (guidToConvert == MFMediaType.MPG1) return "MPG1";
            if (guidToConvert == MFMediaType.DVSL) return "DVSL";
            if (guidToConvert == MFMediaType.DVSD) return "DVSD";
            if (guidToConvert == MFMediaType.DVHD) return "DVHD";
            if (guidToConvert == MFMediaType.DV25) return "DV25";
            if (guidToConvert == MFMediaType.DV50) return "DV50";
            if (guidToConvert == MFMediaType.DVH1) return "DVH1";
            if (guidToConvert == MFMediaType.DVC) return "DVC";
            if (guidToConvert == MFMediaType.H264) return "H264";
            if (guidToConvert == MFMediaType.MJPG) return "MJPG";
            if (guidToConvert == MFMediaType.O420) return "O420";
            if (guidToConvert == MFMediaType.HEVC) return "HEVC";
            if (guidToConvert == MFMediaType.HEVC_ES) return "HEVC_ES";

            if (guidToConvert == MFMediaType.H265) return "H265";
            if (guidToConvert == MFMediaType.VP80) return "VP80";
            if (guidToConvert == MFMediaType.VP90) return "VP90";

            if (guidToConvert == MFMediaType.FLAC) return "FLAC";
            if (guidToConvert == MFMediaType.ALAC) return "ALAC";

            if (guidToConvert == MFMediaType.MPEG2) return "MPEG2";
            if (guidToConvert == MFMediaType.MFVideoFormat_H264_ES) return "MFVideoFormat_H264_ES";
            if (guidToConvert == MFMediaType.MFAudioFormat_Dolby_AC3) return "MFAudioFormat_Dolby_AC3";
            if (guidToConvert == MFMediaType.MFAudioFormat_Dolby_DDPlus) return "MFAudioFormat_Dolby_DDPlus";
            // removed by MS - if(guidToConvert == MFMediaType.MFAudioFormat_QCELP) return "MFAudioFormat_QCELP";

            if (guidToConvert == MFMediaType.MFAudioFormat_Vorbis) return "MFAudioFormat_Vorbis";
            if (guidToConvert == MFMediaType.MFAudioFormat_LPCM) return "MFAudioFormat_LPCM";
            if (guidToConvert == MFMediaType.MFAudioFormat_PCM_HDCP) return "MFAudioFormat_PCM_HDCP";
            if (guidToConvert == MFMediaType.MFAudioFormat_Dolby_AC3_HDCP) return "MFAudioFormat_Dolby_AC3_HDCP";
            if (guidToConvert == MFMediaType.MFAudioFormat_AAC_HDCP) return "MFAudioFormat_AAC_HDCP";
            if (guidToConvert == MFMediaType.MFAudioFormat_ADTS_HDCP) return "MFAudioFormat_ADTS_HDCP";
            if (guidToConvert == MFMediaType.MFAudioFormat_Base_HDCP) return "MFAudioFormat_Base_HDCP";
            if (guidToConvert == MFMediaType.MFVideoFormat_H264_HDCP) return "MFVideoFormat_H264_HDCP";
            if (guidToConvert == MFMediaType.MFVideoFormat_Base_HDCP) return "MFVideoFormat_Base_HDCP";

            if (guidToConvert == MFMediaType.MPEG2Transport) return "MPEG2Transport";
            if (guidToConvert == MFMediaType.MPEG2Program) return "MPEG2Program";

            if (guidToConvert == MFMediaType.V216_MS) return "V216_MS";
            if (guidToConvert == MFMediaType.V410_MS) return "V410_MS";

            // Audio Renderer Attributes
            if (guidToConvert == MFAttributesClsid.MF_AUDIO_RENDERER_ATTRIBUTE_ENDPOINT_ID) return "MF_AUDIO_RENDERER_ATTRIBUTE_ENDPOINT_ID";
            if (guidToConvert == MFAttributesClsid.MF_AUDIO_RENDERER_ATTRIBUTE_ENDPOINT_ROLE) return "MF_AUDIO_RENDERER_ATTRIBUTE_ENDPOINT_ROLE";
            if (guidToConvert == MFAttributesClsid.MF_AUDIO_RENDERER_ATTRIBUTE_FLAGS) return "MF_AUDIO_RENDERER_ATTRIBUTE_FLAGS";
            if (guidToConvert == MFAttributesClsid.MF_AUDIO_RENDERER_ATTRIBUTE_SESSION_ID) return "MF_AUDIO_RENDERER_ATTRIBUTE_SESSION_ID";

            // Byte Stream Attributes
            if (guidToConvert == MFAttributesClsid.MF_BYTESTREAM_ORIGIN_NAME) return "MF_BYTESTREAM_ORIGIN_NAME";
            if (guidToConvert == MFAttributesClsid.MF_BYTESTREAM_CONTENT_TYPE) return "MF_BYTESTREAM_CONTENT_TYPE";
            if (guidToConvert == MFAttributesClsid.MF_BYTESTREAM_DURATION) return "MF_BYTESTREAM_DURATION";
            if (guidToConvert == MFAttributesClsid.MF_BYTESTREAM_LAST_MODIFIED_TIME) return "MF_BYTESTREAM_LAST_MODIFIED_TIME";
            if (guidToConvert == MFAttributesClsid.MF_BYTESTREAM_IFO_FILE_URI) return "MF_BYTESTREAM_IFO_FILE_URI";
            if (guidToConvert == MFAttributesClsid.MF_BYTESTREAM_DLNA_PROFILE_ID) return "MF_BYTESTREAM_DLNA_PROFILE_ID";
            if (guidToConvert == MFAttributesClsid.MF_BYTESTREAM_EFFECTIVE_URL) return "MF_BYTESTREAM_EFFECTIVE_URL";
            if (guidToConvert == MFAttributesClsid.MF_BYTESTREAM_TRANSCODED) return "MF_BYTESTREAM_TRANSCODED";

            // Enhanced Video Renderer Attributes
            if (guidToConvert == MFAttributesClsid.MF_ACTIVATE_CUSTOM_VIDEO_MIXER_ACTIVATE) return "MF_ACTIVATE_CUSTOM_VIDEO_MIXER_ACTIVATE";
            if (guidToConvert == MFAttributesClsid.MF_ACTIVATE_CUSTOM_VIDEO_MIXER_CLSID) return "MF_ACTIVATE_CUSTOM_VIDEO_MIXER_CLSID";
            if (guidToConvert == MFAttributesClsid.MF_ACTIVATE_CUSTOM_VIDEO_MIXER_FLAGS) return "MF_ACTIVATE_CUSTOM_VIDEO_MIXER_FLAGS";
            if (guidToConvert == MFAttributesClsid.MF_ACTIVATE_CUSTOM_VIDEO_PRESENTER_ACTIVATE) return "MF_ACTIVATE_CUSTOM_VIDEO_PRESENTER_ACTIVATE";
            if (guidToConvert == MFAttributesClsid.MF_ACTIVATE_CUSTOM_VIDEO_PRESENTER_CLSID) return "MF_ACTIVATE_CUSTOM_VIDEO_PRESENTER_CLSID";
            if (guidToConvert == MFAttributesClsid.MF_ACTIVATE_CUSTOM_VIDEO_PRESENTER_FLAGS) return "MF_ACTIVATE_CUSTOM_VIDEO_PRESENTER_FLAGS";
            if (guidToConvert == MFAttributesClsid.MF_ACTIVATE_VIDEO_WINDOW) return "MF_ACTIVATE_VIDEO_WINDOW";
            if (guidToConvert == MFAttributesClsid.MF_SA_REQUIRED_SAMPLE_COUNT) return "MF_SA_REQUIRED_SAMPLE_COUNT";
            if (guidToConvert == MFAttributesClsid.MF_SA_REQUIRED_SAMPLE_COUNT_PROGRESSIVE) return "MF_SA_REQUIRED_SAMPLE_COUNT_PROGRESSIVE";
            if (guidToConvert == MFAttributesClsid.MF_SA_MINIMUM_OUTPUT_SAMPLE_COUNT) return "MF_SA_MINIMUM_OUTPUT_SAMPLE_COUNT";
            if (guidToConvert == MFAttributesClsid.MF_SA_MINIMUM_OUTPUT_SAMPLE_COUNT_PROGRESSIVE) return "MF_SA_MINIMUM_OUTPUT_SAMPLE_COUNT_PROGRESSIVE";
            if (guidToConvert == MFAttributesClsid.VIDEO_ZOOM_RECT) return "VIDEO_ZOOM_RECT";

            // Event Attributes

            // removed by MS - if(guidToConvert == MFAttributesClsid.MF_EVENT_FORMAT_CHANGE_REQUEST_SOURCE_SAR) return "MF_EVENT_FORMAT_CHANGE_REQUEST_SOURCE_SAR";

            // MF_EVENT_DO_THINNING {321EA6FB-DAD9-46e4-B31D-D2EAE7090E30}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_DO_THINNING) return "MF_EVENT_DO_THINNING";

            // MF_EVENT_OUTPUT_NODE {830f1a8b-c060-46dd-a801-1c95dec9b107}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_OUTPUT_NODE) return "MF_EVENT_OUTPUT_NODE";

            // MF_EVENT_MFT_INPUT_STREAM_ID {F29C2CCA-7AE6-42d2-B284-BF837CC874E2}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_MFT_INPUT_STREAM_ID) return "MF_EVENT_MFT_INPUT_STREAM_ID";

            // MF_EVENT_MFT_CONTEXT {B7CD31F1-899E-4b41-80C9-26A896D32977}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_MFT_CONTEXT) return "MF_EVENT_MFT_CONTEXT";

            // MF_EVENT_PRESENTATION_TIME_OFFSET {5AD914D1-9B45-4a8d-A2C0-81D1E50BFB07}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_PRESENTATION_TIME_OFFSET) return "MF_EVENT_PRESENTATION_TIME_OFFSET";

            // MF_EVENT_SCRUBSAMPLE_TIME {9AC712B3-DCB8-44d5-8D0C-37455A2782E3}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_SCRUBSAMPLE_TIME) return "MF_EVENT_SCRUBSAMPLE_TIME";

            // MF_EVENT_SESSIONCAPS {7E5EBCD0-11B8-4abe-AFAD-10F6599A7F42}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_SESSIONCAPS) return "MF_EVENT_SESSIONCAPS";

            // MF_EVENT_SESSIONCAPS_DELTA {7E5EBCD1-11B8-4abe-AFAD-10F6599A7F42}
            // Type: UINT32
            if (guidToConvert == MFAttributesClsid.MF_EVENT_SESSIONCAPS_DELTA) return "MF_EVENT_SESSIONCAPS_DELTA";

            // MF_EVENT_SOURCE_ACTUAL_START {a8cc55a9-6b31-419f-845d-ffb351a2434b}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_SOURCE_ACTUAL_START) return "MF_EVENT_SOURCE_ACTUAL_START";

            // MF_EVENT_SOURCE_CHARACTERISTICS {47DB8490-8B22-4f52-AFDA-9CE1B2D3CFA8}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_SOURCE_CHARACTERISTICS) return "MF_EVENT_SOURCE_CHARACTERISTICS";

            // MF_EVENT_SOURCE_CHARACTERISTICS_OLD {47DB8491-8B22-4f52-AFDA-9CE1B2D3CFA8}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_SOURCE_CHARACTERISTICS_OLD) return "MF_EVENT_SOURCE_CHARACTERISTICS_OLD";

            // MF_EVENT_SOURCE_FAKE_START {a8cc55a7-6b31-419f-845d-ffb351a2434b}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_SOURCE_FAKE_START) return "MF_EVENT_SOURCE_FAKE_START";

            // MF_EVENT_SOURCE_PROJECTSTART {a8cc55a8-6b31-419f-845d-ffb351a2434b}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_SOURCE_PROJECTSTART) return "MF_EVENT_SOURCE_PROJECTSTART";

            // MF_EVENT_SOURCE_TOPOLOGY_CANCELED {DB62F650-9A5E-4704-ACF3-563BC6A73364}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_SOURCE_TOPOLOGY_CANCELED) return "MF_EVENT_SOURCE_TOPOLOGY_CANCELED";

            // MF_EVENT_START_PRESENTATION_TIME {5AD914D0-9B45-4a8d-A2C0-81D1E50BFB07}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_START_PRESENTATION_TIME) return "MF_EVENT_START_PRESENTATION_TIME";

            // MF_EVENT_START_PRESENTATION_TIME_AT_OUTPUT {5AD914D2-9B45-4a8d-A2C0-81D1E50BFB07}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_START_PRESENTATION_TIME_AT_OUTPUT) return "MF_EVENT_START_PRESENTATION_TIME_AT_OUTPUT";

            // MF_EVENT_TOPOLOGY_STATUS {30C5018D-9A53-454b-AD9E-6D5F8FA7C43B}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_TOPOLOGY_STATUS) return "MF_EVENT_TOPOLOGY_STATUS";

            // MF_EVENT_STREAM_METADATA_KEYDATA {CD59A4A1-4A3B-4BBD-8665-72A40FBEA776}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_STREAM_METADATA_KEYDATA) return "MF_EVENT_STREAM_METADATA_KEYDATA";

            // MF_EVENT_STREAM_METADATA_CONTENT_KEYIDS {5063449D-CC29-4FC6-A75A-D247B35AF85C}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_STREAM_METADATA_CONTENT_KEYIDS) return "MF_EVENT_STREAM_METADATA_CONTENT_KEYIDS";

            // MF_EVENT_STREAM_METADATA_SYSTEMID {1EA2EF64-BA16-4A36-8719-FE7560BA32AD}
            if (guidToConvert == MFAttributesClsid.MF_EVENT_STREAM_METADATA_SYSTEMID) return "MF_EVENT_STREAM_METADATA_SYSTEMID";

            if (guidToConvert == MFAttributesClsid.MF_SESSION_APPROX_EVENT_OCCURRENCE_TIME) return "MF_SESSION_APPROX_EVENT_OCCURRENCE_TIME";

            // Media Session Attributes

            if (guidToConvert == MFAttributesClsid.MF_SESSION_CONTENT_PROTECTION_MANAGER) return "MF_SESSION_CONTENT_PROTECTION_MANAGER";
            if (guidToConvert == MFAttributesClsid.MF_SESSION_GLOBAL_TIME) return "MF_SESSION_GLOBAL_TIME";
            if (guidToConvert == MFAttributesClsid.MF_SESSION_QUALITY_MANAGER) return "MF_SESSION_QUALITY_MANAGER";
            if (guidToConvert == MFAttributesClsid.MF_SESSION_REMOTE_SOURCE_MODE) return "MF_SESSION_REMOTE_SOURCE_MODE";
            if (guidToConvert == MFAttributesClsid.MF_SESSION_SERVER_CONTEXT) return "MF_SESSION_SERVER_CONTEXT";
            if (guidToConvert == MFAttributesClsid.MF_SESSION_TOPOLOADER) return "MF_SESSION_TOPOLOADER";

            // Media Type Attributes

            // {48eba18e-f8c9-4687-bf11-0a74c9f96a8f}   MF_MT_MAJOR_TYPE                {GUID}
            if (guidToConvert == MFAttributesClsid.MF_MT_MAJOR_TYPE) return "MF_MT_MAJOR_TYPE";

            // {f7e34c9a-42e8-4714-b74b-cb29d72c35e5}   MF_MT_SUBTYPE                   {GUID}
            if (guidToConvert == MFAttributesClsid.MF_MT_SUBTYPE) return "MF_MT_SUBTYPE";

            // {c9173739-5e56-461c-b713-46fb995cb95f}   MF_MT_ALL_SAMPLES_INDEPENDENT   {UINT32 (BOOL)}
            if (guidToConvert == MFAttributesClsid.MF_MT_ALL_SAMPLES_INDEPENDENT) return "MF_MT_ALL_SAMPLES_INDEPENDENT";

            // {b8ebefaf-b718-4e04-b0a9-116775e3321b}   MF_MT_FIXED_SIZE_SAMPLES        {UINT32 (BOOL)}
            if (guidToConvert == MFAttributesClsid.MF_MT_FIXED_SIZE_SAMPLES) return "MF_MT_FIXED_SIZE_SAMPLES";

            // {3afd0cee-18f2-4ba5-a110-8bea502e1f92}   MF_MT_COMPRESSED                {UINT32 (BOOL)}
            if (guidToConvert == MFAttributesClsid.MF_MT_COMPRESSED) return "MF_MT_COMPRESSED";

            // {dad3ab78-1990-408b-bce2-eba673dacc10}   MF_MT_SAMPLE_SIZE               {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_SAMPLE_SIZE) return "MF_MT_SAMPLE_SIZE";

            // 4d3f7b23-d02f-4e6c-9bee-e4bf2c6c695d     MF_MT_WRAPPED_TYPE              {Blob}
            if (guidToConvert == MFAttributesClsid.MF_MT_WRAPPED_TYPE) return "MF_MT_WRAPPED_TYPE";

            // {37e48bf5-645e-4c5b-89de-ada9e29b696a}   MF_MT_AUDIO_NUM_CHANNELS            {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_NUM_CHANNELS) return "MF_MT_AUDIO_NUM_CHANNELS";

            // {5faeeae7-0290-4c31-9e8a-c534f68d9dba}   MF_MT_AUDIO_SAMPLES_PER_SECOND      {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_SAMPLES_PER_SECOND) return "MF_MT_AUDIO_SAMPLES_PER_SECOND";

            // {fb3b724a-cfb5-4319-aefe-6e42b2406132}   MF_MT_AUDIO_FLOAT_SAMPLES_PER_SECOND {double}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_FLOAT_SAMPLES_PER_SECOND) return "MF_MT_AUDIO_FLOAT_SAMPLES_PER_SECOND";

            // {1aab75c8-cfef-451c-ab95-ac034b8e1731}   MF_MT_AUDIO_AVG_BYTES_PER_SECOND    {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_AVG_BYTES_PER_SECOND) return "MF_MT_AUDIO_AVG_BYTES_PER_SECOND";

            // {322de230-9eeb-43bd-ab7a-ff412251541d}   MF_MT_AUDIO_BLOCK_ALIGNMENT         {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_BLOCK_ALIGNMENT) return "MF_MT_AUDIO_BLOCK_ALIGNMENT";

            // {f2deb57f-40fa-4764-aa33-ed4f2d1ff669}   MF_MT_AUDIO_BITS_PER_SAMPLE         {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_BITS_PER_SAMPLE) return "MF_MT_AUDIO_BITS_PER_SAMPLE";

            // {d9bf8d6a-9530-4b7c-9ddf-ff6fd58bbd06}   MF_MT_AUDIO_VALID_BITS_PER_SAMPLE   {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_VALID_BITS_PER_SAMPLE) return "MF_MT_AUDIO_VALID_BITS_PER_SAMPLE";

            // {aab15aac-e13a-4995-9222-501ea15c6877}   MF_MT_AUDIO_SAMPLES_PER_BLOCK       {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_SAMPLES_PER_BLOCK) return "MF_MT_AUDIO_SAMPLES_PER_BLOCK";

            // {55fb5765-644a-4caf-8479-938983bb1588}`  MF_MT_AUDIO_CHANNEL_MASK            {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_CHANNEL_MASK) return "MF_MT_AUDIO_CHANNEL_MASK";

            // {9d62927c-36be-4cf2-b5c4-a3926e3e8711}`  MF_MT_AUDIO_FOLDDOWN_MATRIX         {BLOB, MFFOLDDOWN_MATRIX}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_FOLDDOWN_MATRIX) return "MF_MT_AUDIO_FOLDDOWN_MATRIX";

            // {0x9d62927d-36be-4cf2-b5c4-a3926e3e8711}`  MF_MT_AUDIO_WMADRC_PEAKREF         {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_WMADRC_PEAKREF) return "MF_MT_AUDIO_WMADRC_PEAKREF";

            // {0x9d62927e-36be-4cf2-b5c4-a3926e3e8711}`  MF_MT_AUDIO_WMADRC_PEAKTARGET        {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_WMADRC_PEAKTARGET) return "MF_MT_AUDIO_WMADRC_PEAKTARGET";

            // {0x9d62927f-36be-4cf2-b5c4-a3926e3e8711}`  MF_MT_AUDIO_WMADRC_AVGREF         {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_WMADRC_AVGREF) return "MF_MT_AUDIO_WMADRC_AVGREF";

            // {0x9d629280-36be-4cf2-b5c4-a3926e3e8711}`  MF_MT_AUDIO_WMADRC_AVGTARGET      {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_WMADRC_AVGTARGET) return "MF_MT_AUDIO_WMADRC_AVGTARGET";

            // {a901aaba-e037-458a-bdf6-545be2074042}   MF_MT_AUDIO_PREFER_WAVEFORMATEX     {UINT32 (BOOL)}
            if (guidToConvert == MFAttributesClsid.MF_MT_AUDIO_PREFER_WAVEFORMATEX) return "MF_MT_AUDIO_PREFER_WAVEFORMATEX";

            // {BFBABE79-7434-4d1c-94F0-72A3B9E17188} MF_MT_AAC_PAYLOAD_TYPE       {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AAC_PAYLOAD_TYPE) return "MF_MT_AAC_PAYLOAD_TYPE";

            // {7632F0E6-9538-4d61-ACDA-EA29C8C14456} MF_MT_AAC_AUDIO_PROFILE_LEVEL_INDICATION       {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AAC_AUDIO_PROFILE_LEVEL_INDICATION) return "MF_MT_AAC_AUDIO_PROFILE_LEVEL_INDICATION";

            // {1652c33d-d6b2-4012-b834-72030849a37d}   MF_MT_FRAME_SIZE                {UINT64 (HI32(Width),LO32(Height))}
            if (guidToConvert == MFAttributesClsid.MF_MT_FRAME_SIZE) return "MF_MT_FRAME_SIZE";

            // {c459a2e8-3d2c-4e44-b132-fee5156c7bb0}   MF_MT_FRAME_RATE                {UINT64 (HI32(Numerator),LO32(Denominator))}
            if (guidToConvert == MFAttributesClsid.MF_MT_FRAME_RATE) return "MF_MT_FRAME_RATE";

            // {c6376a1e-8d0a-4027-be45-6d9a0ad39bb6}   MF_MT_PIXEL_ASPECT_RATIO        {UINT64 (HI32(Numerator),LO32(Denominator))}
            if (guidToConvert == MFAttributesClsid.MF_MT_PIXEL_ASPECT_RATIO) return "MF_MT_PIXEL_ASPECT_RATIO";

            // {8772f323-355a-4cc7-bb78-6d61a048ae82}   MF_MT_DRM_FLAGS                 {UINT32 (anyof MFVideoDRMFlags)}
            if (guidToConvert == MFAttributesClsid.MF_MT_DRM_FLAGS) return "MF_MT_DRM_FLAGS";

            // {24974215-1B7B-41e4-8625-AC469F2DEDAA}   MF_MT_TIMESTAMP_CAN_BE_DTS      {UINT32 (BOOL)}
            if (guidToConvert == MFAttributesClsid.MF_MT_TIMESTAMP_CAN_BE_DTS) return "MF_MT_TIMESTAMP_CAN_BE_DTS";

            // {A20AF9E8-928A-4B26-AAA9-F05C74CAC47C}   MF_MT_MPEG2_STANDARD            {UINT32 (0 for default MPEG2, 1  to use ATSC standard, 2 to use DVB standard, 3 to use ARIB standard)}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG2_STANDARD) return "MF_MT_MPEG2_STANDARD";

            // {5229BA10-E29D-4F80-A59C-DF4F180207D2}   MF_MT_MPEG2_TIMECODE            {UINT32 (0 for no timecode, 1 to append an 4 byte timecode to the front of each transport packet)}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG2_TIMECODE) return "MF_MT_MPEG2_TIMECODE";

            // {825D55E4-4F12-4197-9EB3-59B6E4710F06}   MF_MT_MPEG2_CONTENT_PACKET      {UINT32 (0 for no content packet, 1 to append a 14 byte Content Packet header according to the ARIB specification to the beginning a transport packet at 200-1000 ms intervals.)}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG2_CONTENT_PACKET) return "MF_MT_MPEG2_CONTENT_PACKET";

            //
            // VIDEO - H264 extra data
            //

            // {F5929986-4C45-4FBB-BB49-6CC534D05B9B}  {UINT32, UVC 1.5 H.264 format descriptor: bMaxCodecConfigDelay}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_MAX_CODEC_CONFIG_DELAY) return "MF_MT_H264_MAX_CODEC_CONFIG_DELAY";

            // {C8BE1937-4D64-4549-8343-A8086C0BFDA5} {UINT32, UVC 1.5 H.264 format descriptor: bmSupportedSliceModes}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_SUPPORTED_SLICE_MODES) return "MF_MT_H264_SUPPORTED_SLICE_MODES";

            // {89A52C01-F282-48D2-B522-22E6AE633199} {UINT32, UVC 1.5 H.264 format descriptor: bmSupportedSyncFrameTypes}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_SUPPORTED_SYNC_FRAME_TYPES) return "MF_MT_H264_SUPPORTED_SYNC_FRAME_TYPES";

            // {E3854272-F715-4757-BA90-1B696C773457} {UINT32, UVC 1.5 H.264 format descriptor: bResolutionScaling}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_RESOLUTION_SCALING) return "MF_MT_H264_RESOLUTION_SCALING";

            // {9EA2D63D-53F0-4A34-B94E-9DE49A078CB3} {UINT32, UVC 1.5 H.264 format descriptor: bSimulcastSupport}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_SIMULCAST_SUPPORT) return "MF_MT_H264_SIMULCAST_SUPPORT";

            // {6A8AC47E-519C-4F18-9BB3-7EEAAEA5594D} {UINT32, UVC 1.5 H.264 format descriptor: bmSupportedRateControlModes}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_SUPPORTED_RATE_CONTROL_MODES) return "MF_MT_H264_SUPPORTED_RATE_CONTROL_MODES";

            // {45256D30-7215-4576-9336-B0F1BCD59BB2}  {Blob of size 20 * sizeof(WORD), UVC 1.5 H.264 format descriptor: wMaxMBperSec*}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_MAX_MB_PER_SEC) return "MF_MT_H264_MAX_MB_PER_SEC";

            // {60B1A998-DC01-40CE-9736-ABA845A2DBDC}         {UINT32, UVC 1.5 H.264 frame descriptor: bmSupportedUsages}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_SUPPORTED_USAGES) return "MF_MT_H264_SUPPORTED_USAGES";

            // {BB3BD508-490A-11E0-99E4-1316DFD72085}         {UINT32, UVC 1.5 H.264 frame descriptor: bmCapabilities}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_CAPABILITIES) return "MF_MT_H264_CAPABILITIES";

            // {F8993ABE-D937-4A8F-BBCA-6966FE9E1152}         {UINT32, UVC 1.5 H.264 frame descriptor: bmSVCCapabilities}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_SVC_CAPABILITIES) return "MF_MT_H264_SVC_CAPABILITIES";

            // {359CE3A5-AF00-49CA-A2F4-2AC94CA82B61}         {UINT32, UVC 1.5 H.264 Probe/Commit Control: bUsage}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_USAGE) return "MF_MT_H264_USAGE";

            //{705177D8-45CB-11E0-AC7D-B91CE0D72085}          {UINT32, UVC 1.5 H.264 Probe/Commit Control: bmRateControlModes}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_RATE_CONTROL_MODES) return "MF_MT_H264_RATE_CONTROL_MODES";

            //{85E299B2-90E3-4FE8-B2F5-C067E0BFE57A}          {UINT64, UVC 1.5 H.264 Probe/Commit Control: bmLayoutPerStream}
            if (guidToConvert == MFAttributesClsid.MF_MT_H264_LAYOUT_PER_STREAM) return "MF_MT_H264_LAYOUT_PER_STREAM";

            // {4d0e73e5-80ea-4354-a9d0-1176ceb028ea}   MF_MT_PAD_CONTROL_FLAGS         {UINT32 (oneof MFVideoPadFlags)}
            if (guidToConvert == MFAttributesClsid.MF_MT_PAD_CONTROL_FLAGS) return "MF_MT_PAD_CONTROL_FLAGS";

            // {68aca3cc-22d0-44e6-85f8-28167197fa38}   MF_MT_SOURCE_CONTENT_HINT       {UINT32 (oneof MFVideoSrcContentHintFlags)}
            if (guidToConvert == MFAttributesClsid.MF_MT_SOURCE_CONTENT_HINT) return "MF_MT_SOURCE_CONTENT_HINT";

            // {65df2370-c773-4c33-aa64-843e068efb0c}   MF_MT_CHROMA_SITING             {UINT32 (anyof MFVideoChromaSubsampling)}
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_CHROMA_SITING) return "MF_MT_VIDEO_CHROMA_SITING";

            // {e2724bb8-e676-4806-b4b2-a8d6efb44ccd}   MF_MT_INTERLACE_MODE            {UINT32 (oneof MFVideoInterlaceMode)}
            if (guidToConvert == MFAttributesClsid.MF_MT_INTERLACE_MODE) return "MF_MT_INTERLACE_MODE";

            // {5fb0fce9-be5c-4935-a811-ec838f8eed93}   MF_MT_TRANSFER_FUNCTION         {UINT32 (oneof MFVideoTransferFunction)}
            if (guidToConvert == MFAttributesClsid.MF_MT_TRANSFER_FUNCTION) return "MF_MT_TRANSFER_FUNCTION";

            // {dbfbe4d7-0740-4ee0-8192-850ab0e21935}   MF_MT_VIDEO_PRIMARIES           {UINT32 (oneof MFVideoPrimaries)}
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_PRIMARIES) return "MF_MT_VIDEO_PRIMARIES";

            // {47537213-8cfb-4722-aa34-fbc9e24d77b8}   MF_MT_CUSTOM_VIDEO_PRIMARIES    {BLOB (MT_CUSTOM_VIDEO_PRIMARIES)}
            if (guidToConvert == MFAttributesClsid.MF_MT_CUSTOM_VIDEO_PRIMARIES) return "MF_MT_CUSTOM_VIDEO_PRIMARIES";

            // {3e23d450-2c75-4d25-a00e-b91670d12327}   MF_MT_YUV_MATRIX                {UINT32 (oneof MFVideoTransferMatrix)}
            if (guidToConvert == MFAttributesClsid.MF_MT_YUV_MATRIX) return "MF_MT_YUV_MATRIX";

            // {53a0529c-890b-4216-8bf9-599367ad6d20}   MF_MT_VIDEO_LIGHTING            {UINT32 (oneof MFVideoLighting)}
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_LIGHTING) return "MF_MT_VIDEO_LIGHTING";

            // {c21b8ee5-b956-4071-8daf-325edf5cab11}   MF_MT_VIDEO_NOMINAL_RANGE       {UINT32 (oneof MFNominalRange)}
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_NOMINAL_RANGE) return "MF_MT_VIDEO_NOMINAL_RANGE";

            // {66758743-7e5f-400d-980a-aa8596c85696}   MF_MT_GEOMETRIC_APERTURE        {BLOB (MFVideoArea)}
            if (guidToConvert == MFAttributesClsid.MF_MT_GEOMETRIC_APERTURE) return "MF_MT_GEOMETRIC_APERTURE";

            // {d7388766-18fe-48c6-a177-ee894867c8c4}   MF_MT_MINIMUM_DISPLAY_APERTURE  {BLOB (MFVideoArea)}
            if (guidToConvert == MFAttributesClsid.MF_MT_MINIMUM_DISPLAY_APERTURE) return "MF_MT_MINIMUM_DISPLAY_APERTURE";

            // {79614dde-9187-48fb-b8c7-4d52689de649}   MF_MT_PAN_SCAN_APERTURE         {BLOB (MFVideoArea)}
            if (guidToConvert == MFAttributesClsid.MF_MT_PAN_SCAN_APERTURE) return "MF_MT_PAN_SCAN_APERTURE";

            // {4b7f6bc3-8b13-40b2-a993-abf630b8204e}   MF_MT_PAN_SCAN_ENABLED          {UINT32 (BOOL)}
            if (guidToConvert == MFAttributesClsid.MF_MT_PAN_SCAN_ENABLED) return "MF_MT_PAN_SCAN_ENABLED";

            // {20332624-fb0d-4d9e-bd0d-cbf6786c102e}   MF_MT_AVG_BITRATE               {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AVG_BITRATE) return "MF_MT_AVG_BITRATE";

            // {799cabd6-3508-4db4-a3c7-569cd533deb1}   MF_MT_AVG_BIT_ERROR_RATE        {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_AVG_BIT_ERROR_RATE) return "MF_MT_AVG_BIT_ERROR_RATE";

            // {c16eb52b-73a1-476f-8d62-839d6a020652}   MF_MT_MAX_KEYFRAME_SPACING      {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_MAX_KEYFRAME_SPACING) return "MF_MT_MAX_KEYFRAME_SPACING";

            // {644b4e48-1e02-4516-b0eb-c01ca9d49ac6}   MF_MT_DEFAULT_STRIDE            {UINT32 (INT32)} // in bytes
            if (guidToConvert == MFAttributesClsid.MF_MT_DEFAULT_STRIDE) return "MF_MT_DEFAULT_STRIDE";

            // {6d283f42-9846-4410-afd9-654d503b1a54}   MF_MT_PALETTE                   {BLOB (array of MFPaletteEntry - usually 256)}
            if (guidToConvert == MFAttributesClsid.MF_MT_PALETTE) return "MF_MT_PALETTE";

            // {b6bc765f-4c3b-40a4-bd51-2535b66fe09d}   MF_MT_USER_DATA                 {BLOB}
            if (guidToConvert == MFAttributesClsid.MF_MT_USER_DATA) return "MF_MT_USER_DATA";

            // {73d1072d-1870-4174-a063-29ff4ff6c11e}
            if (guidToConvert == MFAttributesClsid.MF_MT_AM_FORMAT_TYPE) return "MF_MT_AM_FORMAT_TYPE";

            // {ad76a80b-2d5c-4e0b-b375-64e520137036}   MF_MT_VIDEO_PROFILE             {UINT32}    This is an alias of  MF_MT_MPEG2_PROFILE
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_PROFILE) return "MF_MT_VIDEO_PROFILE";

            // {96f66574-11c5-4015-8666-bff516436da7}   MF_MT_VIDEO_LEVEL               {UINT32}    This is an alias of  MF_MT_MPEG2_LEVEL
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_LEVEL) return "MF_MT_VIDEO_LEVEL";

            // {91f67885-4333-4280-97cd-bd5a6c03a06e}   MF_MT_MPEG_START_TIME_CODE      {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG_START_TIME_CODE) return "MF_MT_MPEG_START_TIME_CODE";

            // {ad76a80b-2d5c-4e0b-b375-64e520137036}   MF_MT_MPEG2_PROFILE             {UINT32 (oneof AM_MPEG2Profile)}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG2_PROFILE) return "MF_MT_MPEG2_PROFILE";

            // {96f66574-11c5-4015-8666-bff516436da7}   MF_MT_MPEG2_LEVEL               {UINT32 (oneof AM_MPEG2Level)}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG2_LEVEL) return "MF_MT_MPEG2_LEVEL";

            // {31e3991d-f701-4b2f-b426-8ae3bda9e04b}   MF_MT_MPEG2_FLAGS               {UINT32 (anyof AMMPEG2_xxx flags)}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG2_FLAGS) return "MF_MT_MPEG2_FLAGS";

            // {3c036de7-3ad0-4c9e-9216-ee6d6ac21cb3}   MF_MT_MPEG_SEQUENCE_HEADER      {BLOB}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG_SEQUENCE_HEADER) return "MF_MT_MPEG_SEQUENCE_HEADER";

            // {84bd5d88-0fb8-4ac8-be4b-a8848bef98f3}   MF_MT_DV_AAUX_SRC_PACK_0        {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_DV_AAUX_SRC_PACK_0) return "MF_MT_DV_AAUX_SRC_PACK_0";

            // {f731004e-1dd1-4515-aabe-f0c06aa536ac}   MF_MT_DV_AAUX_CTRL_PACK_0       {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_DV_AAUX_CTRL_PACK_0) return "MF_MT_DV_AAUX_CTRL_PACK_0";

            // {720e6544-0225-4003-a651-0196563a958e}   MF_MT_DV_AAUX_SRC_PACK_1        {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_DV_AAUX_SRC_PACK_1) return "MF_MT_DV_AAUX_SRC_PACK_1";

            // {cd1f470d-1f04-4fe0-bfb9-d07ae0386ad8}   MF_MT_DV_AAUX_CTRL_PACK_1       {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_DV_AAUX_CTRL_PACK_1) return "MF_MT_DV_AAUX_CTRL_PACK_1";

            // {41402d9d-7b57-43c6-b129-2cb997f15009}   MF_MT_DV_VAUX_SRC_PACK          {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_DV_VAUX_SRC_PACK) return "MF_MT_DV_VAUX_SRC_PACK";

            // {2f84e1c4-0da1-4788-938e-0dfbfbb34b48}   MF_MT_DV_VAUX_CTRL_PACK         {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_DV_VAUX_CTRL_PACK) return "MF_MT_DV_VAUX_CTRL_PACK";

            // {5315d8a0-87c5-4697-b793-666c67c49b}         MF_MT_VIDEO_3D_FORMAT           {UINT32 (anyof MFVideo3DFormat)}
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_3D_FORMAT) return "MF_MT_VIDEO_3D_FORMAT";

            // {BB077E8A-DCBF-42eb-AF60-418DF98AA495}       MF_MT_VIDEO_3D_NUM_VIEW         {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_3D_NUM_VIEWS) return "MF_MT_VIDEO_3D_NUM_VIEWS";

            // {6D4B7BFF-5629-4404-948C-C634F4CE26D4}       MF_MT_VIDEO_3D_LEFT_IS_BASE     {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_3D_LEFT_IS_BASE) return "MF_MT_VIDEO_3D_LEFT_IS_BASE";

            // {EC298493-0ADA-4ea1-A4FE-CBBD36CE9331}       MF_MT_VIDEO_3D_FIRST_IS_LEFT    {UINT32 (BOOL)}
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_3D_FIRST_IS_LEFT) return "MF_MT_VIDEO_3D_FIRST_IS_LEFT";

            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_ROTATION) return "MF_MT_VIDEO_ROTATION";

            // Sample Attributes

            if (guidToConvert == MFAttributesClsid.MFSampleExtension_DecodeTimestamp) return "MFSampleExtension_DecodeTimestamp";

            if (guidToConvert == MFAttributesClsid.MFSampleExtension_VideoEncodeQP) return "MFSampleExtension_VideoEncodeQP";

            if (guidToConvert == MFAttributesClsid.MFSampleExtension_VideoEncodePictureType) return "MFSampleExtension_VideoEncodePictureType";

            if (guidToConvert == MFAttributesClsid.MFSampleExtension_FrameCorruption) return "MFSampleExtension_FrameCorruption";

            // {941ce0a3-6ae3-4dda-9a08-a64298340617}   MFSampleExtension_BottomFieldFirst
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_BottomFieldFirst) return "MFSampleExtension_BottomFieldFirst";

            // MFSampleExtension_CleanPoint {9cdf01d8-a0f0-43ba-b077-eaa06cbd728a}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_CleanPoint) return "MFSampleExtension_CleanPoint";

            // {6852465a-ae1c-4553-8e9b-c3420fcb1637}   MFSampleExtension_DerivedFromTopField
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_DerivedFromTopField) return "MFSampleExtension_DerivedFromTopField";

            // MFSampleExtension_MeanAbsoluteDifference {1cdbde11-08b4-4311-a6dd-0f9f371907aa}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_MeanAbsoluteDifference) return "MFSampleExtension_MeanAbsoluteDifference";

            // MFSampleExtension_LongTermReferenceFrameInfo {9154733f-e1bd-41bf-81d3-fcd918f71332}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_LongTermReferenceFrameInfo) return "MFSampleExtension_LongTermReferenceFrameInfo";

            // MFSampleExtension_ROIRectangle {3414a438-4998-4d2c-be82-be3ca0b24d43}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_ROIRectangle) return "MFSampleExtension_ROIRectangle";

            // MFSampleExtension_PhotoThumbnail {74BBC85C-C8BB-42DC-B586DA17FFD35DCC}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_PhotoThumbnail) return "MFSampleExtension_PhotoThumbnail";

            // MFSampleExtension_PhotoThumbnailMediaType {61AD5420-EBF8-4143-89AF6BF25F672DEF}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_PhotoThumbnailMediaType) return "MFSampleExtension_PhotoThumbnailMediaType";

            // MFSampleExtension_CaptureMetadata
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_CaptureMetadata) return "MFSampleExtension_CaptureMetadata";

            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_PHOTO_FRAME_FLASH) return "MF_CAPTURE_METADATA_PHOTO_FRAME_FLASH";

            // MFSampleExtension_Discontinuity {9cdf01d9-a0f0-43ba-b077-eaa06cbd728a}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Discontinuity) return "MFSampleExtension_Discontinuity";

            // {b1d5830a-deb8-40e3-90fa-389943716461}   MFSampleExtension_Interlaced
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Interlaced) return "MFSampleExtension_Interlaced";

            // {304d257c-7493-4fbd-b149-9228de8d9a99}   MFSampleExtension_RepeatFirstField
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_RepeatFirstField) return "MFSampleExtension_RepeatFirstField";

            // {9d85f816-658b-455a-bde0-9fa7e15ab8f9}   MFSampleExtension_SingleField
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_SingleField) return "MFSampleExtension_SingleField";

            // MFSampleExtension_Token {8294da66-f328-4805-b551-00deb4c57a61}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Token) return "MFSampleExtension_Token";

            // MFSampleExtension_3DVideo                    {F86F97A4-DD54-4e2e-9A5E-55FC2D74A005}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_3DVideo) return "MFSampleExtension_3DVideo";

            // MFSampleExtension_3DVideo_SampleFormat       {08671772-E36F-4cff-97B3-D72E20987A48}
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_3DVideo_SampleFormat) return "MFSampleExtension_3DVideo_SampleFormat";

            if (guidToConvert == MFAttributesClsid.MFSampleExtension_MaxDecodeFrameSize) return "MFSampleExtension_MaxDecodeFrameSize";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_AccumulatedNonRefPicPercent) return "MFSampleExtension_AccumulatedNonRefPicPercent";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_SubSample_Mapping) return "MFSampleExtension_Encryption_SubSample_Mapping";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_ClearSliceHeaderData) return "MFSampleExtension_Encryption_ClearSliceHeaderData";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_HardwareProtection_KeyInfoID) return "MFSampleExtension_Encryption_HardwareProtection_KeyInfoID";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_HardwareProtection_KeyInfo) return "MFSampleExtension_Encryption_HardwareProtection_KeyInfo";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_HardwareProtection_VideoDecryptorContext) return "MFSampleExtension_Encryption_HardwareProtection_VideoDecryptorContext";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_Opaque_Data) return "MFSampleExtension_Encryption_Opaque_Data";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_NALULengthInfo) return "MFSampleExtension_NALULengthInfo";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_NALUTypes) return "MFSampleExtension_Encryption_NALUTypes";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_SPSPPSData) return "MFSampleExtension_Encryption_SPSPPSData";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_SEIData) return "MFSampleExtension_Encryption_SEIData";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_HardwareProtection) return "MFSampleExtension_Encryption_HardwareProtection";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_ClosedCaption_CEA708) return "MFSampleExtension_ClosedCaption_CEA708";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_DirtyRects) return "MFSampleExtension_DirtyRects";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_MoveRegions) return "MFSampleExtension_MoveRegions";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_HDCP_FrameCounter) return "MFSampleExtension_HDCP_FrameCounter";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_MDLCacheCookie) return "MFSampleExtension_MDLCacheCookie";

            // Sample Grabber Sink Attributes
            if (guidToConvert == MFAttributesClsid.MF_SAMPLEGRABBERSINK_SAMPLE_TIME_OFFSET) return "MF_SAMPLEGRABBERSINK_SAMPLE_TIME_OFFSET";

            // Stream descriptor Attributes

            if (guidToConvert == MFAttributesClsid.MF_SD_LANGUAGE) return "MF_SD_LANGUAGE";
            if (guidToConvert == MFAttributesClsid.MF_SD_PROTECTED) return "MF_SD_PROTECTED";
            if (guidToConvert == MFAttributesClsid.MF_SD_SAMI_LANGUAGE) return "MF_SD_SAMI_LANGUAGE";

            // Topology Attributes
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_NO_MARKIN_MARKOUT) return "MF_TOPOLOGY_NO_MARKIN_MARKOUT";
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_PROJECTSTART) return "MF_TOPOLOGY_PROJECTSTART";
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_PROJECTSTOP) return "MF_TOPOLOGY_PROJECTSTOP";
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_RESOLUTION_STATUS) return "MF_TOPOLOGY_RESOLUTION_STATUS";

            // Topology Node Attributes
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_CONNECT_METHOD) return "MF_TOPONODE_CONNECT_METHOD";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_D3DAWARE) return "MF_TOPONODE_D3DAWARE";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_DECODER) return "MF_TOPONODE_DECODER";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_DECRYPTOR) return "MF_TOPONODE_DECRYPTOR";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_DISABLE_PREROLL) return "MF_TOPONODE_DISABLE_PREROLL";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_DISCARDABLE) return "MF_TOPONODE_DISCARDABLE";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_DRAIN) return "MF_TOPONODE_DRAIN";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_ERROR_MAJORTYPE) return "MF_TOPONODE_ERROR_MAJORTYPE";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_ERROR_SUBTYPE) return "MF_TOPONODE_ERROR_SUBTYPE";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_ERRORCODE) return "MF_TOPONODE_ERRORCODE";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_FLUSH) return "MF_TOPONODE_FLUSH";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_LOCKED) return "MF_TOPONODE_LOCKED";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_MARKIN_HERE) return "MF_TOPONODE_MARKIN_HERE";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_MARKOUT_HERE) return "MF_TOPONODE_MARKOUT_HERE";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_MEDIASTART) return "MF_TOPONODE_MEDIASTART";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_MEDIASTOP) return "MF_TOPONODE_MEDIASTOP";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_NOSHUTDOWN_ON_REMOVE) return "MF_TOPONODE_NOSHUTDOWN_ON_REMOVE";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_PRESENTATION_DESCRIPTOR) return "MF_TOPONODE_PRESENTATION_DESCRIPTOR";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_PRIMARYOUTPUT) return "MF_TOPONODE_PRIMARYOUTPUT";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_RATELESS) return "MF_TOPONODE_RATELESS";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_SEQUENCE_ELEMENTID) return "MF_TOPONODE_SEQUENCE_ELEMENTID";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_SOURCE) return "MF_TOPONODE_SOURCE";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_STREAM_DESCRIPTOR) return "MF_TOPONODE_STREAM_DESCRIPTOR";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_STREAMID) return "MF_TOPONODE_STREAMID";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_TRANSFORM_OBJECTID) return "MF_TOPONODE_TRANSFORM_OBJECTID";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_WORKQUEUE_ID) return "MF_TOPONODE_WORKQUEUE_ID";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_WORKQUEUE_MMCSS_CLASS) return "MF_TOPONODE_WORKQUEUE_MMCSS_CLASS";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_WORKQUEUE_MMCSS_TASKID) return "MF_TOPONODE_WORKQUEUE_MMCSS_TASKID";

            // Transform Attributes
            if (guidToConvert == MFAttributesClsid.MF_ACTIVATE_MFT_LOCKED) return "MF_ACTIVATE_MFT_LOCKED";
            if (guidToConvert == MFAttributesClsid.MF_SA_D3D_AWARE) return "MF_SA_D3D_AWARE";
            if (guidToConvert == MFAttributesClsid.MFT_SUPPORT_3DVIDEO) return "MFT_SUPPORT_3DVIDEO";

            // {53476A11-3F13-49fb-AC42-EE2733C96741} MFT_SUPPORT_DYNAMIC_FORMAT_CHANGE {UINT32 (BOOL)}
            if (guidToConvert == MFAttributesClsid.MFT_SUPPORT_DYNAMIC_FORMAT_CHANGE) return "MFT_SUPPORT_DYNAMIC_FORMAT_CHANGE";

            if (guidToConvert == MFAttributesClsid.MFT_REMUX_MARK_I_PICTURE_AS_CLEAN_POINT) return "MFT_REMUX_MARK_I_PICTURE_AS_CLEAN_POINT";
            if (guidToConvert == MFAttributesClsid.MFT_ENCODER_SUPPORTS_CONFIG_EVENT) return "MFT_ENCODER_SUPPORTS_CONFIG_EVENT";

            // Presentation Descriptor Attributes

            if (guidToConvert == MFAttributesClsid.MF_PD_APP_CONTEXT) return "MF_PD_APP_CONTEXT";
            if (guidToConvert == MFAttributesClsid.MF_PD_DURATION) return "MF_PD_DURATION";
            if (guidToConvert == MFAttributesClsid.MF_PD_LAST_MODIFIED_TIME) return "MF_PD_LAST_MODIFIED_TIME";
            if (guidToConvert == MFAttributesClsid.MF_PD_MIME_TYPE) return "MF_PD_MIME_TYPE";
            if (guidToConvert == MFAttributesClsid.MF_PD_PMPHOST_CONTEXT) return "MF_PD_PMPHOST_CONTEXT";
            if (guidToConvert == MFAttributesClsid.MF_PD_SAMI_STYLELIST) return "MF_PD_SAMI_STYLELIST";
            if (guidToConvert == MFAttributesClsid.MF_PD_TOTAL_FILE_SIZE) return "MF_PD_TOTAL_FILE_SIZE";
            if (guidToConvert == MFAttributesClsid.MF_PD_AUDIO_ENCODING_BITRATE) return "MF_PD_AUDIO_ENCODING_BITRATE";
            if (guidToConvert == MFAttributesClsid.MF_PD_VIDEO_ENCODING_BITRATE) return "MF_PD_VIDEO_ENCODING_BITRATE";

            // wmcontainer.h Attributes
            if (guidToConvert == MFAttributesClsid.MFASFSampleExtension_SampleDuration) return "MFASFSampleExtension_SampleDuration";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_SampleKeyID) return "MFSampleExtension_SampleKeyID";

            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_FILE_ID) return "MF_PD_ASF_FILEPROPERTIES_FILE_ID";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_CREATION_TIME) return "MF_PD_ASF_FILEPROPERTIES_CREATION_TIME";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_PACKETS) return "MF_PD_ASF_FILEPROPERTIES_PACKETS";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_PLAY_DURATION) return "MF_PD_ASF_FILEPROPERTIES_PLAY_DURATION";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_SEND_DURATION) return "MF_PD_ASF_FILEPROPERTIES_SEND_DURATION";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_PREROLL) return "MF_PD_ASF_FILEPROPERTIES_PREROLL";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_FLAGS) return "MF_PD_ASF_FILEPROPERTIES_FLAGS";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_MIN_PACKET_SIZE) return "MF_PD_ASF_FILEPROPERTIES_MIN_PACKET_SIZE";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_MAX_PACKET_SIZE) return "MF_PD_ASF_FILEPROPERTIES_MAX_PACKET_SIZE";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_FILEPROPERTIES_MAX_BITRATE) return "MF_PD_ASF_FILEPROPERTIES_MAX_BITRATE";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_CONTENTENCRYPTION_TYPE) return "MF_PD_ASF_CONTENTENCRYPTION_TYPE";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_CONTENTENCRYPTION_KEYID) return "MF_PD_ASF_CONTENTENCRYPTION_KEYID";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_CONTENTENCRYPTION_SECRET_DATA) return "MF_PD_ASF_CONTENTENCRYPTION_SECRET_DATA";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_CONTENTENCRYPTION_LICENSE_URL) return "MF_PD_ASF_CONTENTENCRYPTION_LICENSE_URL";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_CONTENTENCRYPTIONEX_ENCRYPTION_DATA) return "MF_PD_ASF_CONTENTENCRYPTIONEX_ENCRYPTION_DATA";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_LANGLIST) return "MF_PD_ASF_LANGLIST";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_LANGLIST_LEGACYORDER) return "MF_PD_ASF_LANGLIST_LEGACYORDER";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_MARKER) return "MF_PD_ASF_MARKER";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_SCRIPT) return "MF_PD_ASF_SCRIPT";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_CODECLIST) return "MF_PD_ASF_CODECLIST";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_METADATA_IS_VBR) return "MF_PD_ASF_METADATA_IS_VBR";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_METADATA_V8_VBRPEAK) return "MF_PD_ASF_METADATA_V8_VBRPEAK";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_METADATA_V8_BUFFERAVERAGE) return "MF_PD_ASF_METADATA_V8_BUFFERAVERAGE";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_METADATA_LEAKY_BUCKET_PAIRS) return "MF_PD_ASF_METADATA_LEAKY_BUCKET_PAIRS";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_DATA_START_OFFSET) return "MF_PD_ASF_DATA_START_OFFSET";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_DATA_LENGTH) return "MF_PD_ASF_DATA_LENGTH";
            if (guidToConvert == MFAttributesClsid.MF_SD_ASF_EXTSTRMPROP_LANGUAGE_ID_INDEX) return "MF_SD_ASF_EXTSTRMPROP_LANGUAGE_ID_INDEX";
            if (guidToConvert == MFAttributesClsid.MF_SD_ASF_EXTSTRMPROP_AVG_DATA_BITRATE) return "MF_SD_ASF_EXTSTRMPROP_AVG_DATA_BITRATE";
            if (guidToConvert == MFAttributesClsid.MF_SD_ASF_EXTSTRMPROP_AVG_BUFFERSIZE) return "MF_SD_ASF_EXTSTRMPROP_AVG_BUFFERSIZE";
            if (guidToConvert == MFAttributesClsid.MF_SD_ASF_EXTSTRMPROP_MAX_DATA_BITRATE) return "MF_SD_ASF_EXTSTRMPROP_MAX_DATA_BITRATE";
            if (guidToConvert == MFAttributesClsid.MF_SD_ASF_EXTSTRMPROP_MAX_BUFFERSIZE) return "MF_SD_ASF_EXTSTRMPROP_MAX_BUFFERSIZE";
            if (guidToConvert == MFAttributesClsid.MF_SD_ASF_STREAMBITRATES_BITRATE) return "MF_SD_ASF_STREAMBITRATES_BITRATE";
            if (guidToConvert == MFAttributesClsid.MF_SD_ASF_METADATA_DEVICE_CONFORMANCE_TEMPLATE) return "MF_SD_ASF_METADATA_DEVICE_CONFORMANCE_TEMPLATE";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_INFO_HAS_AUDIO) return "MF_PD_ASF_INFO_HAS_AUDIO";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_INFO_HAS_VIDEO) return "MF_PD_ASF_INFO_HAS_VIDEO";
            if (guidToConvert == MFAttributesClsid.MF_PD_ASF_INFO_HAS_NON_AUDIO_VIDEO) return "MF_PD_ASF_INFO_HAS_NON_AUDIO_VIDEO";
            if (guidToConvert == MFAttributesClsid.MF_ASFSTREAMCONFIG_LEAKYBUCKET1) return "MF_ASFSTREAMCONFIG_LEAKYBUCKET1";
            if (guidToConvert == MFAttributesClsid.MF_ASFSTREAMCONFIG_LEAKYBUCKET2) return "MF_ASFSTREAMCONFIG_LEAKYBUCKET2";

            // Arbitrary

            // {9E6BD6F5-0109-4f95-84AC-9309153A19FC}   MF_MT_ARBITRARY_HEADER          {MT_ARBITRARY_HEADER}
            if (guidToConvert == MFAttributesClsid.MF_MT_ARBITRARY_HEADER) return "MF_MT_ARBITRARY_HEADER";

            // {5A75B249-0D7D-49a1-A1C3-E0D87F0CADE5}   MF_MT_ARBITRARY_FORMAT          {Blob}
            if (guidToConvert == MFAttributesClsid.MF_MT_ARBITRARY_FORMAT) return "MF_MT_ARBITRARY_FORMAT";

            // Image

            // {ED062CF4-E34E-4922-BE99-934032133D7C}   MF_MT_IMAGE_LOSS_TOLERANT       {UINT32 (BOOL)}
            if (guidToConvert == MFAttributesClsid.MF_MT_IMAGE_LOSS_TOLERANT) return "MF_MT_IMAGE_LOSS_TOLERANT";

            // MPEG-4 Media Type Attributes

            // {261E9D83-9529-4B8F-A111-8B9C950A81A9}   MF_MT_MPEG4_SAMPLE_DESCRIPTION   {BLOB}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG4_SAMPLE_DESCRIPTION) return "MF_MT_MPEG4_SAMPLE_DESCRIPTION";

            // {9aa7e155-b64a-4c1d-a500-455d600b6560}   MF_MT_MPEG4_CURRENT_SAMPLE_ENTRY {UINT32}
            if (guidToConvert == MFAttributesClsid.MF_MT_MPEG4_CURRENT_SAMPLE_ENTRY) return "MF_MT_MPEG4_CURRENT_SAMPLE_ENTRY";

            // Save original format information for AVI and WAV files

            // {d7be3fe0-2bc7-492d-b843-61a1919b70c3}   MF_MT_ORIGINAL_4CC               (UINT32)
            if (guidToConvert == MFAttributesClsid.MF_MT_ORIGINAL_4CC) return "MF_MT_ORIGINAL_4CC";

            // {8cbbc843-9fd9-49c2-882f-a72586c408ad}   MF_MT_ORIGINAL_WAVE_FORMAT_TAG   (UINT32)
            if (guidToConvert == MFAttributesClsid.MF_MT_ORIGINAL_WAVE_FORMAT_TAG) return "MF_MT_ORIGINAL_WAVE_FORMAT_TAG";

            // Video Capture Media Type Attributes

            // {D2E7558C-DC1F-403f-9A72-D28BB1EB3B5E}   MF_MT_FRAME_RATE_RANGE_MIN      {UINT64 (HI32(Numerator),LO32(Denominator))}
            if (guidToConvert == MFAttributesClsid.MF_MT_FRAME_RATE_RANGE_MIN) return "MF_MT_FRAME_RATE_RANGE_MIN";

            // {E3371D41-B4CF-4a05-BD4E-20B88BB2C4D6}   MF_MT_FRAME_RATE_RANGE_MAX      {UINT64 (HI32(Numerator),LO32(Denominator))}
            if (guidToConvert == MFAttributesClsid.MF_MT_FRAME_RATE_RANGE_MAX) return "MF_MT_FRAME_RATE_RANGE_MAX";

            if (guidToConvert == MFAttributesClsid.MF_LOW_LATENCY) return "MF_LOW_LATENCY";

            // {E3F2E203-D445-4B8C-9211-AE390D3BA017}  {UINT32} Maximum macroblocks per second that can be handled by MFT
            if (guidToConvert == MFAttributesClsid.MF_VIDEO_MAX_MB_PER_SEC) return "MF_VIDEO_MAX_MB_PER_SEC";
            if (guidToConvert == MFAttributesClsid.MF_VIDEO_PROCESSOR_ALGORITHM) return "MF_VIDEO_PROCESSOR_ALGORITHM";

            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_DXVA_MODE) return "MF_TOPOLOGY_DXVA_MODE";
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_ENABLE_XVP_FOR_PLAYBACK) return "MF_TOPOLOGY_ENABLE_XVP_FOR_PLAYBACK";

            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_STATIC_PLAYBACK_OPTIMIZATIONS) return "MF_TOPOLOGY_STATIC_PLAYBACK_OPTIMIZATIONS";
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_PLAYBACK_MAX_DIMS) return "MF_TOPOLOGY_PLAYBACK_MAX_DIMS";

            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_HARDWARE_MODE) return "MF_TOPOLOGY_HARDWARE_MODE";
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_PLAYBACK_FRAMERATE) return "MF_TOPOLOGY_PLAYBACK_FRAMERATE";
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_DYNAMIC_CHANGE_NOT_ALLOWED) return "MF_TOPOLOGY_DYNAMIC_CHANGE_NOT_ALLOWED";
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_ENUMERATE_SOURCE_TYPES) return "MF_TOPOLOGY_ENUMERATE_SOURCE_TYPES";
            if (guidToConvert == MFAttributesClsid.MF_TOPOLOGY_START_TIME_ON_PRESENTATION_SWITCH) return "MF_TOPOLOGY_START_TIME_ON_PRESENTATION_SWITCH";

            if (guidToConvert == MFAttributesClsid.MF_PD_PLAYBACK_ELEMENT_ID) return "MF_PD_PLAYBACK_ELEMENT_ID";
            if (guidToConvert == MFAttributesClsid.MF_PD_PREFERRED_LANGUAGE) return "MF_PD_PREFERRED_LANGUAGE";
            if (guidToConvert == MFAttributesClsid.MF_PD_PLAYBACK_BOUNDARY_TIME) return "MF_PD_PLAYBACK_BOUNDARY_TIME";
            if (guidToConvert == MFAttributesClsid.MF_PD_AUDIO_ISVARIABLEBITRATE) return "MF_PD_AUDIO_ISVARIABLEBITRATE";

            if (guidToConvert == MFAttributesClsid.MF_SD_STREAM_NAME) return "MF_SD_STREAM_NAME";
            if (guidToConvert == MFAttributesClsid.MF_SD_MUTUALLY_EXCLUSIVE) return "MF_SD_MUTUALLY_EXCLUSIVE";

            if (guidToConvert == MFAttributesClsid.MF_SAMPLEGRABBERSINK_IGNORE_CLOCK) return "MF_SAMPLEGRABBERSINK_IGNORE_CLOCK";
            if (guidToConvert == MFAttributesClsid.MF_BYTESTREAMHANDLER_ACCEPTS_SHARE_WRITE) return "MF_BYTESTREAMHANDLER_ACCEPTS_SHARE_WRITE";

            if (guidToConvert == MFAttributesClsid.MF_TRANSCODE_CONTAINERTYPE) return "MF_TRANSCODE_CONTAINERTYPE";
            if (guidToConvert == MFAttributesClsid.MF_TRANSCODE_SKIP_METADATA_TRANSFER) return "MF_TRANSCODE_SKIP_METADATA_TRANSFER";
            if (guidToConvert == MFAttributesClsid.MF_TRANSCODE_TOPOLOGYMODE) return "MF_TRANSCODE_TOPOLOGYMODE";
            if (guidToConvert == MFAttributesClsid.MF_TRANSCODE_ADJUST_PROFILE) return "MF_TRANSCODE_ADJUST_PROFILE";

            if (guidToConvert == MFAttributesClsid.MF_TRANSCODE_ENCODINGPROFILE) return "MF_TRANSCODE_ENCODINGPROFILE";
            if (guidToConvert == MFAttributesClsid.MF_TRANSCODE_QUALITYVSSPEED) return "MF_TRANSCODE_QUALITYVSSPEED";
            if (guidToConvert == MFAttributesClsid.MF_TRANSCODE_DONOT_INSERT_ENCODER) return "MF_TRANSCODE_DONOT_INSERT_ENCODER";

            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE) return "MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE";
            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_HW_SOURCE) return "MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_HW_SOURCE";
            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME) return "MF_DEVSOURCE_ATTRIBUTE_FRIENDLY_NAME";
            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_MEDIA_TYPE) return "MF_DEVSOURCE_ATTRIBUTE_MEDIA_TYPE";
            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_CATEGORY) return "MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_CATEGORY";
            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK) return "MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_SYMBOLIC_LINK";
            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_MAX_BUFFERS) return "MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_MAX_BUFFERS";
            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_ENDPOINT_ID) return "MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_ENDPOINT_ID";
            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_ROLE) return "MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_ROLE";

            if (guidToConvert == MFAttributesClsid.MFSampleExtension_DeviceTimestamp) return "MFSampleExtension_DeviceTimestamp";

            if (guidToConvert == MFAttributesClsid.MF_TRANSFORM_ASYNC) return "MF_TRANSFORM_ASYNC";
            if (guidToConvert == MFAttributesClsid.MF_TRANSFORM_ASYNC_UNLOCK) return "MF_TRANSFORM_ASYNC_UNLOCK";
            if (guidToConvert == MFAttributesClsid.MF_TRANSFORM_FLAGS_Attribute) return "MF_TRANSFORM_FLAGS_Attribute";
            if (guidToConvert == MFAttributesClsid.MF_TRANSFORM_CATEGORY_Attribute) return "MF_TRANSFORM_CATEGORY_Attribute";
            if (guidToConvert == MFAttributesClsid.MFT_TRANSFORM_CLSID_Attribute) return "MFT_TRANSFORM_CLSID_Attribute";
            if (guidToConvert == MFAttributesClsid.MFT_INPUT_TYPES_Attributes) return "MFT_INPUT_TYPES_Attributes";
            if (guidToConvert == MFAttributesClsid.MFT_OUTPUT_TYPES_Attributes) return "MFT_OUTPUT_TYPES_Attributes";
            if (guidToConvert == MFAttributesClsid.MFT_ENUM_HARDWARE_URL_Attribute) return "MFT_ENUM_HARDWARE_URL_Attribute";
            if (guidToConvert == MFAttributesClsid.MFT_FRIENDLY_NAME_Attribute) return "MFT_FRIENDLY_NAME_Attribute";
            if (guidToConvert == MFAttributesClsid.MFT_CONNECTED_STREAM_ATTRIBUTE) return "MFT_CONNECTED_STREAM_ATTRIBUTE";
            if (guidToConvert == MFAttributesClsid.MFT_CONNECTED_TO_HW_STREAM) return "MFT_CONNECTED_TO_HW_STREAM";
            if (guidToConvert == MFAttributesClsid.MFT_PREFERRED_OUTPUTTYPE_Attribute) return "MFT_PREFERRED_OUTPUTTYPE_Attribute";
            if (guidToConvert == MFAttributesClsid.MFT_PROCESS_LOCAL_Attribute) return "MFT_PROCESS_LOCAL_Attribute";
            if (guidToConvert == MFAttributesClsid.MFT_PREFERRED_ENCODER_PROFILE) return "MFT_PREFERRED_ENCODER_PROFILE";
            if (guidToConvert == MFAttributesClsid.MFT_HW_TIMESTAMP_WITH_QPC_Attribute) return "MFT_HW_TIMESTAMP_WITH_QPC_Attribute";
            if (guidToConvert == MFAttributesClsid.MFT_FIELDOFUSE_UNLOCK_Attribute) return "MFT_FIELDOFUSE_UNLOCK_Attribute";
            if (guidToConvert == MFAttributesClsid.MFT_CODEC_MERIT_Attribute) return "MFT_CODEC_MERIT_Attribute";
            if (guidToConvert == MFAttributesClsid.MFT_ENUM_TRANSCODE_ONLY_ATTRIBUTE) return "MFT_ENUM_TRANSCODE_ONLY_ATTRIBUTE";

            if (guidToConvert == MFAttributesClsid.MF_MP2DLNA_USE_MMCSS) return "MF_MP2DLNA_USE_MMCSS";
            if (guidToConvert == MFAttributesClsid.MF_MP2DLNA_VIDEO_BIT_RATE) return "MF_MP2DLNA_VIDEO_BIT_RATE";
            if (guidToConvert == MFAttributesClsid.MF_MP2DLNA_AUDIO_BIT_RATE) return "MF_MP2DLNA_AUDIO_BIT_RATE";
            if (guidToConvert == MFAttributesClsid.MF_MP2DLNA_ENCODE_QUALITY) return "MF_MP2DLNA_ENCODE_QUALITY";
            if (guidToConvert == MFAttributesClsid.MF_MP2DLNA_STATISTICS) return "MF_MP2DLNA_STATISTICS";

            if (guidToConvert == MFAttributesClsid.MF_SINK_WRITER_ASYNC_CALLBACK) return "MF_SINK_WRITER_ASYNC_CALLBACK";
            if (guidToConvert == MFAttributesClsid.MF_SINK_WRITER_DISABLE_THROTTLING) return "MF_SINK_WRITER_DISABLE_THROTTLING";
            if (guidToConvert == MFAttributesClsid.MF_SINK_WRITER_D3D_MANAGER) return "MF_SINK_WRITER_D3D_MANAGER";
            if (guidToConvert == MFAttributesClsid.MF_SINK_WRITER_ENCODER_CONFIG) return "MF_SINK_WRITER_ENCODER_CONFIG";
            if (guidToConvert == MFAttributesClsid.MF_READWRITE_DISABLE_CONVERTERS) return "MF_READWRITE_DISABLE_CONVERTERS";
            if (guidToConvert == MFAttributesClsid.MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS) return "MF_READWRITE_ENABLE_HARDWARE_TRANSFORMS";
            if (guidToConvert == MFAttributesClsid.MF_READWRITE_MMCSS_CLASS) return "MF_READWRITE_MMCSS_CLASS";
            if (guidToConvert == MFAttributesClsid.MF_READWRITE_MMCSS_PRIORITY) return "MF_READWRITE_MMCSS_PRIORITY";
            if (guidToConvert == MFAttributesClsid.MF_READWRITE_MMCSS_CLASS_AUDIO) return "MF_READWRITE_MMCSS_CLASS_AUDIO";
            if (guidToConvert == MFAttributesClsid.MF_READWRITE_MMCSS_PRIORITY_AUDIO) return "MF_READWRITE_MMCSS_PRIORITY_AUDIO";
            if (guidToConvert == MFAttributesClsid.MF_READWRITE_D3D_OPTIONAL) return "MF_READWRITE_D3D_OPTIONAL";

            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_ASYNC_CALLBACK) return "MF_SOURCE_READER_ASYNC_CALLBACK";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_D3D_MANAGER) return "MF_SOURCE_READER_D3D_MANAGER";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_DISABLE_DXVA) return "MF_SOURCE_READER_DISABLE_DXVA";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_MEDIASOURCE_CONFIG) return "MF_SOURCE_READER_MEDIASOURCE_CONFIG";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_MEDIASOURCE_CHARACTERISTICS) return "MF_SOURCE_READER_MEDIASOURCE_CHARACTERISTICS";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING) return "MF_SOURCE_READER_ENABLE_VIDEO_PROCESSING";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_DISCONNECT_MEDIASOURCE_ON_SHUTDOWN) return "MF_SOURCE_READER_DISCONNECT_MEDIASOURCE_ON_SHUTDOWN";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING) return "MF_SOURCE_READER_ENABLE_ADVANCED_VIDEO_PROCESSING";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_DISABLE_CAMERA_PLUGINS) return "MF_SOURCE_READER_DISABLE_CAMERA_PLUGINS";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_ENABLE_TRANSCODE_ONLY_TRANSFORMS) return "MF_SOURCE_READER_ENABLE_TRANSCODE_ONLY_TRANSFORMS";


            // Misc W8 attributes
            if (guidToConvert == MFAttributesClsid.MF_ENABLE_3DVIDEO_OUTPUT) return "MF_ENABLE_3DVIDEO_OUTPUT";
            if (guidToConvert == MFAttributesClsid.MF_SA_D3D11_BINDFLAGS) return "MF_SA_D3D11_BINDFLAGS";
            if (guidToConvert == MFAttributesClsid.MF_SA_D3D11_USAGE) return "MF_SA_D3D11_USAGE";
            if (guidToConvert == MFAttributesClsid.MF_SA_D3D11_AWARE) return "MF_SA_D3D11_AWARE";
            if (guidToConvert == MFAttributesClsid.MF_SA_D3D11_SHARED) return "MF_SA_D3D11_SHARED";
            if (guidToConvert == MFAttributesClsid.MF_SA_D3D11_SHARED_WITHOUT_MUTEX) return "MF_SA_D3D11_SHARED_WITHOUT_MUTEX";
            if (guidToConvert == MFAttributesClsid.MF_SA_BUFFERS_PER_SAMPLE) return "MF_SA_BUFFERS_PER_SAMPLE";
            if (guidToConvert == MFAttributesClsid.MFT_DECODER_EXPOSE_OUTPUT_TYPES_IN_NATIVE_ORDER) return "MFT_DECODER_EXPOSE_OUTPUT_TYPES_IN_NATIVE_ORDER";
            if (guidToConvert == MFAttributesClsid.MFT_DECODER_FINAL_VIDEO_RESOLUTION_HINT) return "MFT_DECODER_FINAL_VIDEO_RESOLUTION_HINT";
            if (guidToConvert == MFAttributesClsid.MFT_ENUM_HARDWARE_VENDOR_ID_Attribute) return "MFT_ENUM_HARDWARE_VENDOR_ID_Attribute";
            if (guidToConvert == MFAttributesClsid.MF_WVC1_PROG_SINGLE_SLICE_CONTENT) return "MF_WVC1_PROG_SINGLE_SLICE_CONTENT";
            if (guidToConvert == MFAttributesClsid.MF_PROGRESSIVE_CODING_CONTENT) return "MF_PROGRESSIVE_CODING_CONTENT";
            if (guidToConvert == MFAttributesClsid.MF_NALU_LENGTH_SET) return "MF_NALU_LENGTH_SET";
            if (guidToConvert == MFAttributesClsid.MF_NALU_LENGTH_INFORMATION) return "MF_NALU_LENGTH_INFORMATION";
            if (guidToConvert == MFAttributesClsid.MF_USER_DATA_PAYLOAD) return "MF_USER_DATA_PAYLOAD";
            if (guidToConvert == MFAttributesClsid.MF_MPEG4SINK_SPSPPS_PASSTHROUGH) return "MF_MPEG4SINK_SPSPPS_PASSTHROUGH";
            if (guidToConvert == MFAttributesClsid.MF_MPEG4SINK_MOOV_BEFORE_MDAT) return "MF_MPEG4SINK_MOOV_BEFORE_MDAT";
            if (guidToConvert == MFAttributesClsid.MF_STREAM_SINK_SUPPORTS_HW_CONNECTION) return "MF_STREAM_SINK_SUPPORTS_HW_CONNECTION";
            if (guidToConvert == MFAttributesClsid.MF_STREAM_SINK_SUPPORTS_ROTATION) return "MF_STREAM_SINK_SUPPORTS_ROTATION";
            if (guidToConvert == MFAttributesClsid.MF_DISABLE_LOCALLY_REGISTERED_PLUGINS) return "MF_DISABLE_LOCALLY_REGISTERED_PLUGINS";
            if (guidToConvert == MFAttributesClsid.MF_LOCAL_PLUGIN_CONTROL_POLICY) return "MF_LOCAL_PLUGIN_CONTROL_POLICY";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_WORKQUEUE_MMCSS_PRIORITY) return "MF_TOPONODE_WORKQUEUE_MMCSS_PRIORITY";
            if (guidToConvert == MFAttributesClsid.MF_TOPONODE_WORKQUEUE_ITEM_PRIORITY) return "MF_TOPONODE_WORKQUEUE_ITEM_PRIORITY";
            if (guidToConvert == MFAttributesClsid.MF_AUDIO_RENDERER_ATTRIBUTE_STREAM_CATEGORY) return "MF_AUDIO_RENDERER_ATTRIBUTE_STREAM_CATEGORY";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_PROTECTED_SURFACE) return "MFPROTECTION_PROTECTED_SURFACE";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_DISABLE_SCREEN_SCRAPE) return "MFPROTECTION_DISABLE_SCREEN_SCRAPE";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_VIDEO_FRAMES) return "MFPROTECTION_VIDEO_FRAMES";
            if (guidToConvert == MFAttributesClsid.MFPROTECTIONATTRIBUTE_BEST_EFFORT) return "MFPROTECTIONATTRIBUTE_BEST_EFFORT";
            if (guidToConvert == MFAttributesClsid.MFPROTECTIONATTRIBUTE_FAIL_OVER) return "MFPROTECTIONATTRIBUTE_FAIL_OVER";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_GRAPHICS_TRANSFER_AES_ENCRYPTION) return "MFPROTECTION_GRAPHICS_TRANSFER_AES_ENCRYPTION";
            if (guidToConvert == MFAttributesClsid.MF_XVP_DISABLE_FRC) return "MF_XVP_DISABLE_FRC";
            if (guidToConvert == MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_SYMBOLIC_LINK) return "MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_SYMBOLIC_LINK";
            if (guidToConvert == MFAttributesClsid.MF_DEVICESTREAM_IMAGE_STREAM) return "MF_DEVICESTREAM_IMAGE_STREAM";
            if (guidToConvert == MFAttributesClsid.MF_DEVICESTREAM_INDEPENDENT_IMAGE_STREAM) return "MF_DEVICESTREAM_INDEPENDENT_IMAGE_STREAM";
            if (guidToConvert == MFAttributesClsid.MF_DEVICESTREAM_STREAM_ID) return "MF_DEVICESTREAM_STREAM_ID";
            if (guidToConvert == MFAttributesClsid.MF_DEVICESTREAM_STREAM_CATEGORY) return "MF_DEVICESTREAM_STREAM_CATEGORY";
            if (guidToConvert == MFAttributesClsid.MF_DEVICESTREAM_TRANSFORM_STREAM_ID) return "MF_DEVICESTREAM_TRANSFORM_STREAM_ID";
            if (guidToConvert == MFAttributesClsid.MF_DEVICESTREAM_EXTENSION_PLUGIN_CLSID) return "MF_DEVICESTREAM_EXTENSION_PLUGIN_CLSID";
            if (guidToConvert == MFAttributesClsid.MF_DEVICESTREAM_EXTENSION_PLUGIN_CONNECTION_POINT) return "MF_DEVICESTREAM_EXTENSION_PLUGIN_CONNECTION_POINT";
            if (guidToConvert == MFAttributesClsid.MF_DEVICESTREAM_TAKEPHOTO_TRIGGER) return "MF_DEVICESTREAM_TAKEPHOTO_TRIGGER";
            if (guidToConvert == MFAttributesClsid.MF_DEVICESTREAM_MAX_FRAME_BUFFERS) return "MF_DEVICESTREAM_MAX_FRAME_BUFFERS";

            // Windows X attributes
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_FRAME_RAWSTREAM) return "MF_CAPTURE_METADATA_FRAME_RAWSTREAM";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_FOCUSSTATE) return "MF_CAPTURE_METADATA_FOCUSSTATE";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_REQUESTED_FRAME_SETTING_ID) return "MF_CAPTURE_METADATA_REQUESTED_FRAME_SETTING_ID";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_EXPOSURE_TIME) return "MF_CAPTURE_METADATA_EXPOSURE_TIME";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_EXPOSURE_COMPENSATION) return "MF_CAPTURE_METADATA_EXPOSURE_COMPENSATION";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_ISO_SPEED) return "MF_CAPTURE_METADATA_ISO_SPEED";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_LENS_POSITION) return "MF_CAPTURE_METADATA_LENS_POSITION";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_SCENE_MODE) return "MF_CAPTURE_METADATA_SCENE_MODE";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_FLASH) return "MF_CAPTURE_METADATA_FLASH";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_FLASH_POWER) return "MF_CAPTURE_METADATA_FLASH_POWER";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_WHITEBALANCE) return "MF_CAPTURE_METADATA_WHITEBALANCE";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_ZOOMFACTOR) return "MF_CAPTURE_METADATA_ZOOMFACTOR";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_FACEROIS) return "MF_CAPTURE_METADATA_FACEROIS";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_FACEROITIMESTAMPS) return "MF_CAPTURE_METADATA_FACEROITIMESTAMPS";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_FACEROICHARACTERIZATIONS) return "MF_CAPTURE_METADATA_FACEROICHARACTERIZATIONS";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_ISO_GAINS) return "MF_CAPTURE_METADATA_ISO_GAINS";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_SENSORFRAMERATE) return "MF_CAPTURE_METADATA_SENSORFRAMERATE";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_WHITEBALANCE_GAINS) return "MF_CAPTURE_METADATA_WHITEBALANCE_GAINS";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_METADATA_HISTOGRAM) return "MF_CAPTURE_METADATA_HISTOGRAM";

            if (guidToConvert == MFAttributesClsid.MF_SINK_VIDEO_PTS) return "MF_SINK_VIDEO_PTS";
            if (guidToConvert == MFAttributesClsid.MF_SINK_VIDEO_NATIVE_WIDTH) return "MF_SINK_VIDEO_NATIVE_WIDTH";
            if (guidToConvert == MFAttributesClsid.MF_SINK_VIDEO_NATIVE_HEIGHT) return "MF_SINK_VIDEO_NATIVE_HEIGHT";
            if (guidToConvert == MFAttributesClsid.MF_SINK_VIDEO_DISPLAY_ASPECT_RATIO_NUMERATOR) return "MF_SINK_VIDEO_DISPLAY_ASPECT_RATIO_NUMERATOR";
            if (guidToConvert == MFAttributesClsid.MF_SINK_VIDEO_DISPLAY_ASPECT_RATIO_DENOMINATOR) return "MF_SINK_VIDEO_DISPLAY_ASPECT_RATIO_DENOMINATOR";
            if (guidToConvert == MFAttributesClsid.MF_BD_MVC_PLANE_OFFSET_METADATA) return "MF_BD_MVC_PLANE_OFFSET_METADATA";
            if (guidToConvert == MFAttributesClsid.MF_LUMA_KEY_ENABLE) return "MF_LUMA_KEY_ENABLE";
            if (guidToConvert == MFAttributesClsid.MF_LUMA_KEY_LOWER) return "MF_LUMA_KEY_LOWER";
            if (guidToConvert == MFAttributesClsid.MF_LUMA_KEY_UPPER) return "MF_LUMA_KEY_UPPER";
            if (guidToConvert == MFAttributesClsid.MF_USER_EXTENDED_ATTRIBUTES) return "MF_USER_EXTENDED_ATTRIBUTES";
            if (guidToConvert == MFAttributesClsid.MF_INDEPENDENT_STILL_IMAGE) return "MF_INDEPENDENT_STILL_IMAGE";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_PROTECTION_MANAGER_PROPERTIES) return "MF_MEDIA_PROTECTION_MANAGER_PROPERTIES";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_HARDWARE) return "MFPROTECTION_HARDWARE";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_HDCP_WITH_TYPE_ENFORCEMENT) return "MFPROTECTION_HDCP_WITH_TYPE_ENFORCEMENT";
            if (guidToConvert == MFAttributesClsid.MF_XVP_CALLER_ALLOCATES_OUTPUT) return "MF_XVP_CALLER_ALLOCATES_OUTPUT";
            if (guidToConvert == MFAttributesClsid.MF_DEVICEMFT_EXTENSION_PLUGIN_CLSID) return "MF_DEVICEMFT_EXTENSION_PLUGIN_CLSID";
            if (guidToConvert == MFAttributesClsid.MF_DEVICEMFT_CONNECTED_FILTER_KSCONTROL) return "MF_DEVICEMFT_CONNECTED_FILTER_KSCONTROL";
            if (guidToConvert == MFAttributesClsid.MF_DEVICEMFT_CONNECTED_PIN_KSCONTROL) return "MF_DEVICEMFT_CONNECTED_PIN_KSCONTROL";
            if (guidToConvert == MFAttributesClsid.MF_DEVICE_THERMAL_STATE_CHANGED) return "MF_DEVICE_THERMAL_STATE_CHANGED";
            if (guidToConvert == MFAttributesClsid.MF_ACCESS_CONTROLLED_MEDIASOURCE_SERVICE) return "MF_ACCESS_CONTROLLED_MEDIASOURCE_SERVICE";
            if (guidToConvert == MFAttributesClsid.MF_CONTENT_DECRYPTOR_SERVICE) return "MF_CONTENT_DECRYPTOR_SERVICE";
            if (guidToConvert == MFAttributesClsid.MF_CONTENT_PROTECTION_DEVICE_SERVICE) return "MF_CONTENT_PROTECTION_DEVICE_SERVICE";
            if (guidToConvert == MFAttributesClsid.MF_SD_AUDIO_ENCODER_DELAY) return "MF_SD_AUDIO_ENCODER_DELAY";
            if (guidToConvert == MFAttributesClsid.MF_SD_AUDIO_ENCODER_PADDING) return "MF_SD_AUDIO_ENCODER_PADDING";

            if (guidToConvert == MFAttributesClsid.MFT_END_STREAMING_AWARE) return "MFT_END_STREAMING_AWARE";
            if (guidToConvert == MFAttributesClsid.MF_SA_D3D11_ALLOW_DYNAMIC_YUV_TEXTURE) return "MF_SA_D3D11_ALLOW_DYNAMIC_YUV_TEXTURE";
            if (guidToConvert == MFAttributesClsid.MFT_DECODER_QUALITY_MANAGEMENT_CUSTOM_CONTROL) return "MFT_DECODER_QUALITY_MANAGEMENT_CUSTOM_CONTROL";
            if (guidToConvert == MFAttributesClsid.MFT_DECODER_QUALITY_MANAGEMENT_RECOVERY_WITHOUT_ARTIFACTS) return "MFT_DECODER_QUALITY_MANAGEMENT_RECOVERY_WITHOUT_ARTIFACTS";

            if (guidToConvert == MFAttributesClsid.MF_SOURCE_READER_D3D11_BIND_FLAGS) return "MF_SOURCE_READER_D3D11_BIND_FLAGS";
            if (guidToConvert == MFAttributesClsid.MF_MEDIASINK_AUTOFINALIZE_SUPPORTED) return "MF_MEDIASINK_AUTOFINALIZE_SUPPORTED";
            if (guidToConvert == MFAttributesClsid.MF_MEDIASINK_ENABLE_AUTOFINALIZE) return "MF_MEDIASINK_ENABLE_AUTOFINALIZE";
            if (guidToConvert == MFAttributesClsid.MF_READWRITE_ENABLE_AUTOFINALIZE) return "MF_READWRITE_ENABLE_AUTOFINALIZE";

            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_BROWSER_COMPATIBILITY_MODE_IE_EDGE) return "MF_MEDIA_ENGINE_BROWSER_COMPATIBILITY_MODE_IE_EDGE";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_TELEMETRY_APPLICATION_ID) return "MF_MEDIA_ENGINE_TELEMETRY_APPLICATION_ID";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_TIMEDTEXT) return "MF_MEDIA_ENGINE_TIMEDTEXT";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_CONTINUE_ON_CODEC_ERROR) return "MF_MEDIA_ENGINE_CONTINUE_ON_CODEC_ERROR";

            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_CAMERA_STREAM_BLOCKED) return "MF_CAPTURE_ENGINE_CAMERA_STREAM_BLOCKED";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_CAMERA_STREAM_UNBLOCKED) return "MF_CAPTURE_ENGINE_CAMERA_STREAM_UNBLOCKED";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_ENABLE_CAMERA_STREAMSTATE_NOTIFICATION) return "MF_CAPTURE_ENGINE_ENABLE_CAMERA_STREAMSTATE_NOTIFICATION";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_SELECTEDCAMERAPROFILE) return "MF_CAPTURE_ENGINE_SELECTEDCAMERAPROFILE";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_SELECTEDCAMERAPROFILE_INDEX) return "MF_CAPTURE_ENGINE_SELECTEDCAMERAPROFILE_INDEX";

            if (guidToConvert == MFAttributesClsid.EVRConfig_AllowBatching) return "EVRConfig_AllowBatching";
            if (guidToConvert == MFAttributesClsid.EVRConfig_AllowDropToBob) return "EVRConfig_AllowDropToBob";
            if (guidToConvert == MFAttributesClsid.EVRConfig_AllowDropToHalfInterlace) return "EVRConfig_AllowDropToHalfInterlace";
            if (guidToConvert == MFAttributesClsid.EVRConfig_AllowDropToThrottle) return "EVRConfig_AllowDropToThrottle";
            if (guidToConvert == MFAttributesClsid.EVRConfig_AllowScaling) return "EVRConfig_AllowScaling";
            if (guidToConvert == MFAttributesClsid.EVRConfig_ForceBatching) return "EVRConfig_ForceBatching";
            if (guidToConvert == MFAttributesClsid.EVRConfig_ForceBob) return "EVRConfig_ForceBob";
            if (guidToConvert == MFAttributesClsid.EVRConfig_ForceHalfInterlace) return "EVRConfig_ForceHalfInterlace";
            if (guidToConvert == MFAttributesClsid.EVRConfig_ForceScaling) return "EVRConfig_ForceScaling";
            if (guidToConvert == MFAttributesClsid.EVRConfig_ForceThrottle) return "EVRConfig_ForceThrottle";
            if (guidToConvert == MFAttributesClsid.MF_ASFPROFILE_MAXPACKETSIZE) return "MF_ASFPROFILE_MAXPACKETSIZE";
            if (guidToConvert == MFAttributesClsid.MF_ASFPROFILE_MINPACKETSIZE) return "MF_ASFPROFILE_MINPACKETSIZE";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_D3D_MANAGER) return "MF_CAPTURE_ENGINE_D3D_MANAGER";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_DECODER_MFT_FIELDOFUSE_UNLOCK_Attribute) return "MF_CAPTURE_ENGINE_DECODER_MFT_FIELDOFUSE_UNLOCK_Attribute";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_DISABLE_DXVA) return "MF_CAPTURE_ENGINE_DISABLE_DXVA";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_DISABLE_HARDWARE_TRANSFORMS) return "MF_CAPTURE_ENGINE_DISABLE_HARDWARE_TRANSFORMS";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_ENCODER_MFT_FIELDOFUSE_UNLOCK_Attribute) return "MF_CAPTURE_ENGINE_ENCODER_MFT_FIELDOFUSE_UNLOCK_Attribute";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_EVENT_GENERATOR_GUID) return "MF_CAPTURE_ENGINE_EVENT_GENERATOR_GUID";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_EVENT_STREAM_INDEX) return "MF_CAPTURE_ENGINE_EVENT_STREAM_INDEX";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_MEDIASOURCE_CONFIG) return "MF_CAPTURE_ENGINE_MEDIASOURCE_CONFIG";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_OUTPUT_MEDIA_TYPE_SET) return "MF_CAPTURE_ENGINE_OUTPUT_MEDIA_TYPE_SET";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_RECORD_SINK_AUDIO_MAX_PROCESSED_SAMPLES) return "MF_CAPTURE_ENGINE_RECORD_SINK_AUDIO_MAX_PROCESSED_SAMPLES";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_RECORD_SINK_AUDIO_MAX_UNPROCESSED_SAMPLES) return "MF_CAPTURE_ENGINE_RECORD_SINK_AUDIO_MAX_UNPROCESSED_SAMPLES";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_RECORD_SINK_VIDEO_MAX_PROCESSED_SAMPLES) return "MF_CAPTURE_ENGINE_RECORD_SINK_VIDEO_MAX_PROCESSED_SAMPLES";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_RECORD_SINK_VIDEO_MAX_UNPROCESSED_SAMPLES) return "MF_CAPTURE_ENGINE_RECORD_SINK_VIDEO_MAX_UNPROCESSED_SAMPLES";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_USE_AUDIO_DEVICE_ONLY) return "MF_CAPTURE_ENGINE_USE_AUDIO_DEVICE_ONLY";
            if (guidToConvert == MFAttributesClsid.MF_CAPTURE_ENGINE_USE_VIDEO_DEVICE_ONLY) return "MF_CAPTURE_ENGINE_USE_VIDEO_DEVICE_ONLY";
            if (guidToConvert == MFAttributesClsid.MF_SOURCE_STREAM_SUPPORTS_HW_CONNECTION) return "MF_SOURCE_STREAM_SUPPORTS_HW_CONNECTION";
            if (guidToConvert == MFAttributesClsid.MF_VIDEODSP_MODE) return "MF_VIDEODSP_MODE";
            if (guidToConvert == MFAttributesClsid.MFASFSPLITTER_PACKET_BOUNDARY) return "MFASFSPLITTER_PACKET_BOUNDARY";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_DeviceReferenceSystemTime) return "MFSampleExtension_DeviceReferenceSystemTime";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_VideoDSPMode) return "MFSampleExtension_VideoDSPMode";

            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_CALLBACK) return "MF_MEDIA_ENGINE_CALLBACK";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_DXGI_MANAGER) return "MF_MEDIA_ENGINE_DXGI_MANAGER";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_EXTENSION) return "MF_MEDIA_ENGINE_EXTENSION";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_PLAYBACK_HWND) return "MF_MEDIA_ENGINE_PLAYBACK_HWND";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_OPM_HWND) return "MF_MEDIA_ENGINE_OPM_HWND";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_PLAYBACK_VISUAL) return "MF_MEDIA_ENGINE_PLAYBACK_VISUAL";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_COREWINDOW) return "MF_MEDIA_ENGINE_COREWINDOW";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT) return "MF_MEDIA_ENGINE_VIDEO_OUTPUT_FORMAT";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_CONTENT_PROTECTION_FLAGS) return "MF_MEDIA_ENGINE_CONTENT_PROTECTION_FLAGS";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_CONTENT_PROTECTION_MANAGER) return "MF_MEDIA_ENGINE_CONTENT_PROTECTION_MANAGER";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_AUDIO_ENDPOINT_ROLE) return "MF_MEDIA_ENGINE_AUDIO_ENDPOINT_ROLE";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_AUDIO_CATEGORY) return "MF_MEDIA_ENGINE_AUDIO_CATEGORY";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_STREAM_CONTAINS_ALPHA_CHANNEL) return "MF_MEDIA_ENGINE_STREAM_CONTAINS_ALPHA_CHANNEL";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_BROWSER_COMPATIBILITY_MODE) return "MF_MEDIA_ENGINE_BROWSER_COMPATIBILITY_MODE";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_SOURCE_RESOLVER_CONFIG_STORE) return "MF_MEDIA_ENGINE_SOURCE_RESOLVER_CONFIG_STORE";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_NEEDKEY_CALLBACK) return "MF_MEDIA_ENGINE_NEEDKEY_CALLBACK";
            if (guidToConvert == MFAttributesClsid.MF_MEDIA_ENGINE_TRACK_ID) return "MF_MEDIA_ENGINE_TRACK_ID";

            if (guidToConvert == MFAttributesClsid.MFPROTECTION_ACP) return "MFPROTECTION_ACP";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_CGMSA) return "MFPROTECTION_CGMSA";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_CONSTRICTAUDIO) return "MFPROTECTION_CONSTRICTAUDIO";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_CONSTRICTVIDEO) return "MFPROTECTION_CONSTRICTVIDEO";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_CONSTRICTVIDEO_NOOPM) return "MFPROTECTION_CONSTRICTVIDEO_NOOPM";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_DISABLE) return "MFPROTECTION_DISABLE";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_FFT) return "MFPROTECTION_FFT";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_HDCP) return "MFPROTECTION_HDCP";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_TRUSTEDAUDIODRIVERS) return "MFPROTECTION_TRUSTEDAUDIODRIVERS";
            if (guidToConvert == MFAttributesClsid.MFPROTECTION_WMDRMOTA) return "MFPROTECTION_WMDRMOTA";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_SampleID) return "MFSampleExtension_Encryption_SampleID";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Encryption_SubSampleMappingSplit) return "MFSampleExtension_Encryption_SubSampleMappingSplit";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_PacketCrossOffsets) return "MFSampleExtension_PacketCrossOffsets";
            if (guidToConvert == MFAttributesClsid.MFSampleExtension_Content_KeyID) return "MFSampleExtension_Content_KeyID";
            if (guidToConvert == MFAttributesClsid.MF_MSE_ACTIVELIST_CALLBACK) return "MF_MSE_ACTIVELIST_CALLBACK";
            if (guidToConvert == MFAttributesClsid.MF_MSE_BUFFERLIST_CALLBACK) return "MF_MSE_BUFFERLIST_CALLBACK";
            if (guidToConvert == MFAttributesClsid.MF_MSE_CALLBACK) return "MF_MSE_CALLBACK";
            if (guidToConvert == MFAttributesClsid.MF_MT_VIDEO_3D) return "MF_MT_VIDEO_3D";
            return "Unknown";
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a 100ns value to a human readable HH:MM:SS time (like 1:32:43)
        /// </summary>
        /// <param name="presentationClockin100ns">the presentation clock value in 100ns units</param>
        /// <returns>a human readable HH:MM:SS time</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static string ConvertPresentationClockToHHMMSSTime(UInt64 presentationClockin100ns)
        {
            StringBuilder sb = new StringBuilder();

            // lets get it into seconds, discard subseconds
            UInt64 presentationClockInSec = presentationClockin100ns / 10000000;
            UInt64 presentationClockInMin = presentationClockInSec / 60;
            UInt64 presentationClockInHours = presentationClockInMin / 60;

            UInt64 remainderSec = presentationClockInSec % 60;
            UInt64 remainderMin = presentationClockInMin % 60;

            // return the calculated time
            sb.Append(presentationClockInHours.ToString("00") + ":" + remainderMin.ToString("00") + ":" + remainderSec.ToString("00"));

            return sb.ToString();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Adds seconds values to a 100ns clock value. Will never go less than zero, 
        /// will never exceed the maximum time
        /// </summary>
        /// <param name="presentationClockin100ns">the presentation clock value in 100ns units</param>
        /// <param name="maxPresentationTime100ns">the maximum presentation time</param>
        /// <param name="secondsToAdd">the number of seconds to add. Can be negative</param>
        /// <returns>the new presentation clock. Will never be negative, can be zero, will never exceed the maximum time</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static UInt64 AddSecondsTo100nsTime(int secondsToAdd, UInt64 presentationClockin100ns, UInt64 maxPresentationTime100ns)
        {
            // convert to a signed value
            Int64 tmpClockin100ns = (Int64)presentationClockin100ns;
            int secondsToAddIn100ns = secondsToAdd * 10000000;
            // do the math
            tmpClockin100ns = tmpClockin100ns + (Int64)secondsToAddIn100ns;
            // check for negative
            if (tmpClockin100ns < 0) tmpClockin100ns = 0;
            // check for too high
            UInt64 outTime100ns = (UInt64)tmpClockin100ns;
            if (outTime100ns > maxPresentationTime100ns) outTime100ns = maxPresentationTime100ns;
            // return it
            return outTime100ns;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a structure to a byte array.
        /// 
        /// Original source:
        /// https://stackoverflow.com/questions/3278827/how-to-convert-a-structure-to-a-byte-array-in-c
        /// 
        /// </summary>
        /// <param name="structToConvert">the structure to convert</param>
        /// <returns>the contents of the structure as a byte array</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static byte[] ConvertStructureToByteArray(object structToConvert)
        {
            int len = Marshal.SizeOf(structToConvert);
            byte[] arr = new byte[len];
            IntPtr ptr = Marshal.AllocHGlobal(len);
            Marshal.StructureToPtr(structToConvert, ptr, true);
            Marshal.Copy(ptr, arr, 0, len);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a structure to a byte array.
        /// 
        /// Original source:
        /// https://stackoverflow.com/questions/3278827/how-to-convert-a-structure-to-a-byte-array-in-c
        /// 
        /// </summary>
        /// <param name="convertedStruct">the converted structure is returned here</param>
        /// <param name="bytearray">the byte array to convert</param>
        /// <returns>the contents of the byte array as a structure</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static void ConvertByteArrayToStructure(byte[] bytearray, ref object convertedStruct)
        {
            int len = Marshal.SizeOf(convertedStruct);
            IntPtr i = Marshal.AllocHGlobal(len);
            Marshal.Copy(bytearray, 0, i, len);
            convertedStruct = Marshal.PtrToStructure(i, convertedStruct.GetType());
            Marshal.FreeHGlobal(i);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Builds a filename with a timestamp.
        /// 
        /// Original source:
        /// https://stackoverflow.com/questions/3278827/how-to-convert-a-structure-to-a-byte-array-in-c
        /// 
        /// </summary>
        /// <param name="fileNamePrefix">the prefix to use for the filename</param>
        /// <param name="fileNameExtension">the extension to use for the filename (including the dot)</param>
        /// <returns>the filename with a timestamp in it</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public static string BuildFilenameWithTimeStamp(string fileNamePrefix, string fileNameExtension)
        {
            StringBuilder sb = new StringBuilder();

            if ((fileNamePrefix != null) && (fileNamePrefix.Length != 0))
            {
                sb.Append(fileNamePrefix);
            }

            sb.Append(DateTime.Now.ToString("yyyyMMddHHmmssfff"));

            if ((fileNameExtension != null) && (fileNameExtension.Length != 0))
            {
                sb.Append(fileNameExtension);
            }
            return sb.ToString();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Check to see if two IMFMediaTypes are identical. Copied straight out 
        /// of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="a">First media type.</param>
        /// <param name="b">Second media type.</param>
        /// <returns>S_Ok if identical, else MF_E_INVALIDTYPE.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static HResult IsMediaTypeIdentical(IMFMediaType a, IMFMediaType b)
        {
            // Otherwise, proposed input must be identical to output.
            MFMediaEqual flags;
            HResult hr = a.IsEqual(b, out flags);

            // IsEqual can return S_FALSE. Treat this as failure.
            if (hr != HResult.S_OK)
            {
                hr = HResult.MF_E_INVALIDTYPE;
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Check a media type for a matching Major type and Subtype. Copied straight out 
        /// of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="pmt">IMFMediaType to check</param>
        /// <param name="gMajorType">MajorType to check for.</param>
        /// <param name="gSubtype">SubType to check for.</param>
        /// <returns>S_Ok if match, else MF_E_INVALIDTYPE.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static HResult CheckMediaType(IMFMediaType pmt, Guid gMajorType, Guid gSubtype)
        {
            Guid major_type;

            // Major type must be video.
            HResult hr = HResult.S_OK;
            MFError throwonhr;

            throwonhr = pmt.GetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, out major_type);

            if (major_type == gMajorType)
            {
                Guid subtype;

                // Get the subtype GUID.
                throwonhr = pmt.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out subtype);

                if (subtype != gSubtype)
                {
                    hr = HResult.MF_E_INVALIDTYPE;
                }
            }
            else
            {
                hr = HResult.MF_E_INVALIDTYPE;
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Check a media type for a matching Major type and a list of Subtypes.
        ///  Copied straight out of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="pmt">IMFMediaType to check</param>
        /// <param name="gMajorType">MajorType to check for.</param>
        /// <param name="gSubtypes">Array of subTypes to check for.</param>
        /// <returns>S_Ok if match, else MF_E_INVALIDTYPE.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static HResult CheckMediaType(IMFMediaType pmt, Guid gMajorType, Guid[] gSubTypes)
        {
            Guid major_type;

            // Major type must be video.
            HResult hr = HResult.S_OK;
            MFError throwonhr;

            throwonhr = pmt.GetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, out major_type);

            if (major_type == gMajorType)
            {
                Guid subtype;

                // Get the subtype GUID.
                throwonhr = pmt.GetGUID(MFAttributesClsid.MF_MT_SUBTYPE, out subtype);

                // Look for the subtype in our list of accepted types.
                hr = HResult.MF_E_INVALIDTYPE;
                for (int i = 0; i < gSubTypes.Length; i++)
                {
                    if (subtype == gSubTypes[i])
                    {
                        hr = HResult.S_OK;
                        break;
                    }
                }
            }
            else
            {
                hr = HResult.MF_E_INVALIDTYPE;
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create a partial media type from a Major and Sub type. Copied straight 
        /// out of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="gMajorType">Major type.</param>
        /// <param name="gSubtype">Sub type.</param>
        /// <returns>Newly created media type.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static IMFMediaType CreatePartialMediaType(Guid gMajorType, Guid gSubtype)
        {
            IMFMediaType ppmt;
            MFError throwonhr;

            throwonhr = MFExtern.MFCreateMediaType(out ppmt);
            throwonhr = ppmt.SetGUID(MFAttributesClsid.MF_MT_MAJOR_TYPE, gMajorType);
            throwonhr = ppmt.SetGUID(MFAttributesClsid.MF_MT_SUBTYPE, gSubtype);

            return ppmt;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Create an array of partial media types using an array of subtypes. Copied straight 
        /// out of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="dwTypeIndex">Index into the array.</param>
        /// <param name="gMajorType">Major type.</param>
        /// <param name="gSubTypes">Array of subtypes.</param>
        /// <param name="ppmt">Newly created media type.</param>
        /// <returns>S_Ok if valid index, else MF_E_NO_MORE_TYPES.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static HResult CreatePartialMediaType(int dwTypeIndex, Guid gMajorType, Guid[] gSubTypes, out IMFMediaType ppmt)
        {
            HResult hr;

            if (dwTypeIndex < gSubTypes.Length)
            {
                ppmt = CreatePartialMediaType(gMajorType, gSubTypes[dwTypeIndex]);
                hr = HResult.S_OK;
            }
            else
            {
                ppmt = null;
                hr = HResult.MF_E_NO_MORE_TYPES;
            }

            return hr;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Return a duplicate of a media type. Null input gives null output. 
        /// Copied straight out of the MF.Net samples MFTBase source code.
        /// </summary>
        /// <param name="inType">The IMFMediaType to clone or null.</param>
        /// <returns>Duplicate IMFMediaType or null.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Moved in here
        /// </history>
        public static IMFMediaType CloneMediaType(IMFMediaType inType)
        {
            IMFMediaType outType = null;
            HResult hr;

            if (inType != null)
            {

                hr = MFExtern.MFCreateMediaType(out outType);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CloneMediaType call to MFCreateMediaType failed. Err=" + hr.ToString());
                }
                hr = inType.CopyAllItems(outType);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CloneMediaType call to CopyAllItems failed. Err=" + hr.ToString());
                }
            }

            return outType;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Creates a IMFSample from IMFMediaBuffer data. Will throw an exception
        /// for anything it does not like
        /// </summary>
        /// <param name="sourceSampleFlags">the sample flags</param>
        /// <param name="sourceSampleSize">the sample size</param>
        /// <param name="sourceSampleDuration">the sample duration</param>
        /// <param name="sourceSampleTimeStamp">the sample time</param>
        /// <param name="sourceSampleBuffer">the sample buffer</param>
        /// <param name="sourceAttributes">the attributes for the sample - we copy these</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public static IMFSample CreateMediaSampleFromBuffer(int sourceSampleFlags, long sourceSampleTimeStamp, long sourceSampleDuration, IMFMediaBuffer sourceSampleBuffer, int sourceSampleSize, IMFAttributes sourceAttributes)
        {
            IntPtr srcRawDataPtr = IntPtr.Zero;
            bool srcIs2D = false;
            int srcStride;

            // in C# the actual video data is down in the unmanaged heap. We have to get
            // an intptr to the data in order to copy it. In C# this involves a bit
            // of marshaling
            try
            {
                // Lock the input buffer. Use the IMF2DBuffer interface  
                // (if available) as it is faster
                if ((sourceSampleBuffer is IMF2DBuffer) == false)
                {
                    // not an IMF2DBuffer - get the raw data from the IMFMediaBuffer 
                    int maxLen = 0;
                    int currentLen = 0;
                    TantaWMFUtils.LockIMFMediaBufferAndGetRawData(sourceSampleBuffer, out srcRawDataPtr, out maxLen, out currentLen);

                    // now make the call to the version of this function which accepts only IntPtrs as the source
                    return CreateMediaSampleFromIntPtr(sourceSampleFlags, sourceSampleTimeStamp, sourceSampleDuration, srcRawDataPtr, sourceSampleSize, sourceAttributes);
                }
                else
                {
                    // we are an IMF2DBuffer, we get the stride here as well
                    TantaWMFUtils.LockIMF2DBufferAndGetRawData((sourceSampleBuffer as IMF2DBuffer), out srcRawDataPtr, out srcStride);
                    srcIs2D = true;                   

                    // now make the call to the version of this function which accepts only IntPtrs as the source
                    return CreateMediaSampleFromIntPtr(sourceSampleFlags, sourceSampleTimeStamp, sourceSampleDuration, srcRawDataPtr, sourceSampleSize, sourceAttributes);
                }

            }
            finally
            {
                if (srcIs2D == false) TantaWMFUtils.UnLockIMFMediaBuffer(sourceSampleBuffer);
                else TantaWMFUtils.UnLockIMF2DBuffer((sourceSampleBuffer as IMF2DBuffer));
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Creates a IMFSample from a IntPtr to Buffer data. Will throw an exception
        /// for anything it does not like. Always creates a sample with an 
        /// IMFMediaBuffer
        /// </summary>
        /// <param name="sourceSampleFlags">the sample flags</param>
        /// <param name="sourceSampleSize">the sample size</param>
        /// <param name="sourceSampleDuration">the sample duration</param>
        /// <param name="sourceSampleTimeStamp">the sample time</param>
        /// <param name="sourceSampleIntPtr">the source IntPtr</param>
        /// <param name="sourceAttributes">the attributes for the sample - we copy these</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported in
        /// </history>
        public static IMFSample CreateMediaSampleFromIntPtr(int sourceSampleFlags, long sourceSampleTimeStamp, long sourceSampleDuration, IntPtr sourceSampleIntPtr, int sourceSampleSize, IMFAttributes sourceAttributes)
        {
            HResult hr;
            IMFSample outputSample = null;
            IMFMediaBuffer outputBuffer = null;
            IntPtr destRawDataPtr = IntPtr.Zero;

            try
            {
                // Create a new sample
                hr = MFExtern.MFCreateSample(out outputSample);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateMediaSampleFromIntPtr call to sourcePresentationDescriptor.MFCreateSample failed. Err=" + hr.ToString());
                }
                if (outputSample == null)
                {
                    throw new Exception("CreateMediaSampleFromIntPtr call to sourcePresentationDescriptor.MFCreateSample failed. outputSample == null");
                }

                // Allocate an output buffer.
                hr = MFExtern.MFCreateMemoryBuffer(sourceSampleSize, out outputBuffer);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateMediaSampleFromIntPtr call to MFCreateMemoryBuffer failed. Err=" + hr.ToString());
                }
                if (outputBuffer == null)
                {
                    throw new Exception("CreateMediaSampleFromIntPtr call to MFCreateMemoryBuffer failed. outputBuffer == null");
                }

                // add the buffer to the sample
                hr = outputSample.AddBuffer(outputBuffer);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateMediaSampleFromIntPtr call to AddBuffer failed. Err=" + hr.ToString());
                }

                // get a pointer to the raw data from the output buffer. We need this in
                // order to copy the input raw data across
                int maxLen = 0;
                int currentLen = 0;
                TantaWMFUtils.LockIMFMediaBufferAndGetRawData(outputBuffer, out destRawDataPtr, out maxLen, out currentLen);

                // now that we have the input data and a pointer to the destination area
                // do the work to copy it across.
                CopyMemory(destRawDataPtr, sourceSampleIntPtr, sourceSampleSize);

                // Set the data size on the output buffer. 
                hr = outputBuffer.SetCurrentLength(sourceSampleSize);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateMediaSampleFromIntPtr call to SetCurrentLength failed. Err=" + hr.ToString());
                }

                // Set the sample time stamp
                hr = outputSample.SetSampleTime(sourceSampleTimeStamp);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateMediaSampleFromIntPtr call to SetSampleTime failed. Err=" + hr.ToString());
                }

                // set the sample duration
                hr = outputSample.SetSampleDuration(sourceSampleDuration);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("CreateMediaSampleFromIntPtr call to SetSampleDuration failed. Err=" + hr.ToString());
                }

                // set the attributes
                if (sourceAttributes != null)
                {
                    if ((outputSample is IMFAttributes) == true)
                    {
                        sourceAttributes.CopyAllItems((outputSample as IMFAttributes));
                    }
                }

            }
            finally
            {
                TantaWMFUtils.UnLockIMFMediaBuffer(outputBuffer);

                if(outputBuffer!=null)
                {
                    Marshal.ReleaseComObject(outputBuffer);
                    outputBuffer = null;
                }
            }


            return outputSample;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary> 
        /// Gets a unique RuntimeCallableWrapper (RCW) so a call to ReleaseComObject 
        /// on the returned object won't wipe the original object.
        /// </summary>
        /// <param name="o">Object to wrap.  Can be null.</param>
        /// <returns>Wrapped object.</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static object GetUniqueRCW(object o)
        {
            object oret;

            if (o != null)
            {
                IntPtr p = Marshal.GetIUnknownForObject(o);
                try
                {
                    oret = Marshal.GetUniqueObjectForIUnknown(p);
                }
                finally
                {
                    Marshal.Release(p);
                }
            }
            else
            {
                oret = null;
            }

            return oret;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Locks an IMFMediaBuffer and returns an IntPtr to the raw data. You must call 
        /// UnLockIMFBuffer. Note that LockIMF2DBufferAndGetRawData is preferred
        /// because it is faster but not all Media Buffers are IMF2DBuffers so you have
        /// to check. Make sure to call the appropriate version of unlock.
        /// 
        /// Note: the IMFMediaBuffer lock does not return the stride - you have to obtain
        ///       that elsewhere.
        ///
        /// </summary>
        /// <param name="mediaBuffer">The IMFMediaBuffer object</param>
        /// <param name="rawDataPtr">the pointer to the raw data</param>
        /// <param name="maxLen">Receives the maximum amount of data that can be written to the buffer.</param>
        /// <param name="currentLen">Receives the length of the valid data in the buffer, in bytes.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static void LockIMFMediaBufferAndGetRawData(IMFMediaBuffer mediaBuffer, out IntPtr rawDataPtr, out int maxLen, out int currentLen)
        {
            // init
            rawDataPtr = IntPtr.Zero;
            maxLen = 0;
            currentLen = 0;

            if (mediaBuffer == null) throw new Exception("LockMediaBufferAndGetRawData mediaBuffer == null");

            // must call an UnLockIMFBuffer
            HResult hr = mediaBuffer.Lock(out rawDataPtr, out maxLen, out currentLen);
            if (hr != HResult.S_OK)
            {
                throw new Exception("LockMediaBufferAndGetRawData call to mediaBuffer.Lock failed. Err=" + hr.ToString());
            }
            return;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Locks an IMF2DBuffer and returns an IntPtr to the raw data. You must call 
        /// UnLockIMF2DBuffer. Note that LockIMF2DBufferAndGetRawData is preferred
        /// to LockIMFBufferAndGetRawData because it is faster but not all Media Buffers 
        /// are IMF2DBuffers so you have to check. Make sure to call the appropriate 
        /// version of unlock.
        /// 
        ///
        /// </summary>
        /// <param name="mediaBuffer">The IMFMediaBuffer object</param>
        /// <param name="rawDataPtr">the pointer to the raw data</param>
        /// <param name="bufferStride">Receives the stride of the buffer.</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static void LockIMF2DBufferAndGetRawData(IMF2DBuffer media2DBuffer, out IntPtr rawDataPtr, out int bufferStride)
        {
            // init
            rawDataPtr = IntPtr.Zero;
            bufferStride = 0;

            if (media2DBuffer == null) throw new Exception("LockIMF2DBufferAndGetRawData media2DBuffer == null");

            // must call an UnLockIMF2DBuffer
            HResult hr = media2DBuffer.Lock2D(out rawDataPtr, out bufferStride);
            if (hr != HResult.S_OK)
            {
                throw new Exception("LockIMF2DBufferAndGetRawData call to media2DBuffer.Lock2D failed. Err=" + hr.ToString());
            }
            return;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// UnLocks a previoiusly locked IMFMediaBuffer. There is also an 
        /// UnLockIMF2DBuffer make sure you call the one you locked it with
        ///
        /// </summary>
        /// <param name="mediaBuffer">The IMFMediaBuffer object</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static void UnLockIMFMediaBuffer(IMFMediaBuffer mediaBuffer)
        {
            HResult hr = mediaBuffer.Unlock();
            if (hr != HResult.S_OK)
            {
                throw new Exception("UnLockIMFMediaBuffer call to mediaBuffer.Unlock failed. Err=" + hr.ToString());
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// UnLocks a previoiusly locked IMF2DBuffer. There is also an 
        /// UnLockIMFMediaBuffer make sure you call the one you locked it with
        ///
        /// </summary>
        /// <param name="media2DBuffer">The IMF2DBuffer object</param>
        /// <history>
        ///    01 Nov 18  Cynic - Ported In
        /// </history>
        public static void UnLockIMF2DBuffer(IMF2DBuffer media2DBuffer)
        {
            HResult hr = media2DBuffer.Unlock2D();
            if (hr != HResult.S_OK)
            {
                throw new Exception("UnLockIMFMediaBuffer call to media2DBuffer.Unlock2D failed. Err=" + hr.ToString());
            }
        }


        #region Externs

        [DllImport("Kernel32.dll"), System.Security.SuppressUnmanagedCodeSecurity]
        private static extern void CopyMemory(IntPtr Destination, IntPtr Source, int Length);

        [DllImport("kernel32.dll"), System.Security.SuppressUnmanagedCodeSecurity]
        private static extern void FillMemory(IntPtr destination, int len, byte val);

        #endregion

    }
}

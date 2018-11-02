using System;
using System.Collections.Generic;
using System.Text;

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

/// #########
/// Note: the three letter "OIS" prefix used here is an acronym for "OfItselfSo.com" this softwares home website.
/// #########

namespace OISCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
    /// <summary>
    /// Misc. Useful Utility code, only static stuff in here
    /// </summary>
    /// <history>
    ///    03 Nov 09  Cynic - Started
    /// </history>
    public static class OISUtils
    {
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a string with \n and \r\n to use \r\n everywhere. 
        /// </summary>
        /// <param name="strIn">the string to convert</param>
        /// <returns>converted string</returns>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public static string ConvertToConsistentCRLF(string strIn)
        {
            return ConvertToConsistentCRLF(strIn, false);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a string with \n and \r\n to use \r\n everywhere. Also can remove
        /// consecutive new lines.
        /// </summary>
        /// <param name="strIn">the string to convert</param>
        /// <param name="wantConsecutiveLineRemoval">if true we remove consecutive new lines</param>
        /// <returns>converted string</returns>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public static string ConvertToConsistentCRLF(string strIn, bool wantConsecutiveLineRemoval)
        {
            if (strIn == null) return null;
            StringBuilder tmpStr = new StringBuilder(strIn);
            // to get a consistent CRLF we do this
            tmpStr = tmpStr.Replace("\r", "");
            if (wantConsecutiveLineRemoval == true)
            {
                tmpStr = tmpStr.Replace("\n\n", "\n");
                tmpStr = tmpStr.Replace("\n\n", "\n");
                tmpStr = tmpStr.Replace("\n\n", "\n");
            }
            tmpStr = tmpStr.Replace("\n", "\r\n");
            return tmpStr.ToString();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a number of seconds to a human readable time interval.
        /// For example 90 seconds is returned as 1 min 30 sec
        /// </summary>
        /// <param name="seconds">the number of seconds</param>
        /// <returns>time interval string</returns>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public static string ConvertSecondsToHumanReadableTimeInterval(int seconds)
        {
            TimeSpan runTime = new TimeSpan(TimeSpan.TicksPerSecond * seconds);
            return ConvertTimeSpanToHumanReadableTimeInterval(runTime);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Converts a number of seconds to a human readable time interval.
        /// For example 90 seconds is returned as 1 min 30 sec
        /// </summary>
        /// <param name="runTime">the runtime as a TimeSpan value</param>
        /// <returns>time interval string</returns>
        /// <history>
        ///    03 Nov 09  Cynic - Started
        /// </history>
        public static string ConvertTimeSpanToHumanReadableTimeInterval(TimeSpan runTime)
        {
            int days;
            int hours;
            int minutes;
            int seconds;

            // set up the string
            days = runTime.Days;
            hours = runTime.Hours;
            minutes = runTime.Minutes;
            seconds = runTime.Seconds;
            if ((minutes == 0) && (hours == 0) && (days == 0))
            {
                return seconds.ToString() + " sec.";
            }
            else if ((hours == 0) && (days == 0))
            {
                return minutes.ToString() + " min. " + seconds.ToString() + " sec.";
            }
            else if (days == 0)
            {
                return hours.ToString() + " hours " + minutes.ToString() + " min. " + seconds.ToString() + " sec.";
            }
            else return days.ToString() + " days " + hours.ToString() + " hours " + minutes.ToString() + " min " + seconds.ToString() + "sec";
        }
    }
}

using System;
using System.Windows.Forms;
using System.Threading;
using System.Collections;
using System.IO;
using System.Drawing;
using Microsoft.Win32;

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
    /// Parent of most non-form or control objects in the OIS apps. 
    /// </summary>
    /// <history>
    ///    04 Nov 09  Cynic - Started
    /// </history>
    public abstract class OISObjBase
    {
        // the logger instance
        public OISLogger g_Logger;

#if DEBUG
        // these are only for performance testing, not present in release builds
        public TimeSpan tickCounter0;
        public TimeSpan tickCounter1;
        public TimeSpan tickCounter2;
        public TimeSpan tickCounter3;
        public TimeSpan tickCounter4;
        public TimeSpan tickCounter5;
        public TimeSpan tickCounter6;
#endif

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        public OISObjBase()
        {
            // create the singleton logger instance
            if (g_Logger == null)
            {
                // Acquire the Singleton g_Logger instance
                g_Logger = OISLogger.OISLoggerInstance;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A wrapper to launch a modal Message box, with logging
        /// </summary>
        /// <param name="boxText">The text to display in the box</param>
        /// <param name="boxTitle">The box title</param>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        public void OISMessageBox(string boxText, string boxTitle)
        {
            if (boxTitle == null) boxTitle = "";
            if (boxText == null) boxText = "";
            LogMessage(boxTitle + " " + boxText);
            MessageBox.Show(boxText, boxTitle);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A wrapper to launch a modal Message box, with logging
        /// </summary>
        /// <param name="boxText">The text to display in the box</param>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        public void OISMessageBox(string boxText)
        {
            if (boxText == null) boxText = "";
            LogMessage(boxText);
            MessageBox.Show(boxText);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Simple wrapper for the most common record message call
        /// </summary>
        /// <param name="msgText">Text to Write to the Log</param>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        public void LogMessage(string msgText)
        {
            // write it out to the log - but prepend the object type name
            if(g_Logger!=null) g_Logger.RecordMessage(this.GetType().ToString()+ ": "+msgText);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=  
        /// <summary>
        /// Simple wrapper for the most common debug mode record message call
        /// </summary>
        /// <param name="msgText">Text to Write to the Log</param>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
#if DEBUG
        public void DebugMessage(string msgText)
        {
            // write it out to the log in debug mode - but prepend the object type name
            if(g_Logger!=null) g_Logger.RecordMessage(this.GetType().ToString()+ ": "+msgText);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Does nothing but place a marker in the text that will not compile out
        /// of debug mode. This should be used to mark stuff that absolutely must be
        /// done before release
        /// </summary>
        /// <param name="msgText">The thing To Do</param>
        /// <history>
        ///    04 Nov 09  Cynic - Started
        /// </history>
        public void DebugTODO(string msgText)
        {
        }
#endif
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.IO;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MediaFoundation;
using MediaFoundation.ReadWrite;
using MediaFoundation.Misc;
using MediaFoundation.Transform;
using MediaFoundation.Alt;
using MediaFoundation.EVR;
using OISCommon;
using System.Reflection;

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
/// The MF.Net library itself is licensed under the GNU Library or Lesser General Public License version 2.0 (LGPLv2)
/// 

/// SUPER IMPORTANT NOTE: You MUST use the [MTAThread] to decorate your entry point method. If you use the default [STAThread] 
/// you may get errors. See the Program.cs file for details.

/// The function of this control is to to play video from a file on a panel  
/// owned by this control. In reality, there is not much functionality in 
/// this control other than handling the resizing. It does give you a control 
/// you can drop onto a form though.
/// 
/// If you wish to see a much more comprehensive version of this control with
/// a lot of screen and session manipulation functionality, see the 
/// ctlTantaEVRFilePlayer and the TantaFilePlaybackAdvanced sample application.

namespace TantaCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// A simple class to play video from a file on a panel owned by this control. 
    /// 
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Originally Written
    /// </history>
    public partial class ctlTantaEVRStreamDisplay : ctlOISBase
    {

        // The Enhanced Video Renderer(EVR) implements this interface and it 
        // controls how the EVR presenter displays video. This object must be given
        // to this control by the entity setting up the topology. We do not release 
        // this here. The entity that gave this to us is expected to release it.
        protected IMFVideoDisplayControl evrVideoDisplay;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public ctlTantaEVRStreamDisplay()
        {
            InitializeComponent();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Initialize the media player. Should be called only once. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void InitMediaPlayer()
        {
            // for future use
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Close down this control. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void ShutDownFilePlayer()
        {
            CloseAllMediaDevices();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Close down all media devices
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void CloseAllMediaDevices()
        {
            LogMessage("CloseAllMediaDevices called");

            // we do not release this here. The entity that gave this 
            // to us is expected to release it.
            evrVideoDisplay = null;       
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/sets the EVR video display control. This must be given to this
        /// control by the entity setting up the EVR. If you wish to see a version
        /// of the control that does this for itself have a look at the 
        /// EVRFilePlayer control.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IMFVideoDisplayControl EVRVideoDisplay
        {
            get
            {
                return evrVideoDisplay;
            }
            set
            {
                evrVideoDisplay = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the display panel control. There is no set accessor as this control
        /// is built in the form designer.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        private Panel DisplayPanel
        {
            get
            {
                return this.panelDisplayPanel;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the handle of the display panel control. There is no set accessor as this control
        /// is built in the form designer.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IntPtr DisplayPanelHandle
        {
            get
            {
                return this.panelDisplayPanel.Handle;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the current directory in which we save the snapshots. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string SnapshotDirectory
        {
            get
            {
                // just return the my documents folder
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
             }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle size changed events on this control
        /// </summary>
        /// <param name="errMsg">the error message</param>
        /// <param name="ex">the exception. Can be null</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void ctlTantaEVRStreamDisplay_SizeChanged(object sender, EventArgs e)
        {
            LogMessage("ctlTantaEVRStreamDisplay_SizeChanged");

            HResult hr;
            if (evrVideoDisplay == null) return;

            try
            {
                // we are going to use the size changed event to reset the size of the video on the display. 
                // This is controlled by two windows. 

                // The source window determines which portion of the video is displayed. It is specified in 
                // normalized coordinates. In other words, a value between 0 and 1. To display the entire video 
                // image we would set the source rectangle to { 0, 0, 1, 1}. To display the bottom right quarter
                // we would set it to { 0.75f, 0.75f, 1, 1}. The default source rectangle is { 0, 0, 1, 1}.

                // The destination rectangle defines a rectangle within the clipping window (the video surface) where the video appears. 
                // in this control this is the surface of a child panel control. This values is specified in pixels, relative to 
                // the client area of the control. To fill the entire control, set the destination rectangle to { 0, 0, width, height},
                // where width and height are dimensions of the window client area.

                MFRect destinationRect = new MFRect();
                MFVideoNormalizedRect sourceRect = new MFVideoNormalizedRect();

                // populate a MFVideoNormalizedRect structure that specifies the source rectangle. 
                // This parameter can be NULL. If this parameter is NULL, the source rectangle does not change.
                sourceRect.left = 0;
                sourceRect.right = 1;
                sourceRect.top = 0;
                sourceRect.bottom = 1;         

                // populate the destination rectangle. This parameter can be NULL. If this parameter is NULL, 
                // the destination rectangle does not change.
                destinationRect.left = 0;
                destinationRect.top = 0;
                destinationRect.right = panelDisplayPanel.Width;
                destinationRect.bottom = panelDisplayPanel.Height;

                // now set the video display coordinates
                hr = evrVideoDisplay.SetVideoPosition(sourceRect, destinationRect);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("ctlTantaEVRStreamDisplay_SizeChanged failed. Err=" + hr.ToString());
                }
            }
            catch (Exception ex)
            {
                LogMessage("ctlTantaEVRStreamDisplay_SizeChanged failed exception happened. ex=" + ex.Message);
        //        NotifyPlayerErrored(ex.Message, ex);
            }
        }
    }
}

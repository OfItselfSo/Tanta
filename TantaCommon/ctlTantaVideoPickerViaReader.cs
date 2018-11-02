using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
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
using OISCommon;
using TantaCommon;

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

/// The function of this control is to display the video devices on a system and their associated video formats. The user
/// can choose a specific supported format from a list. The selected format will be communicated back to the owner via an event.
/// /// This version uses a Source Reader to find the available video media types. There
/// is an alternative version which uses the Media Source (see the ctlTantaVideoPicker control).


namespace TantaCommon
{

    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// A class to display the video devices on a system and their associated
    /// video formats. Note that available formats vary widely between video
    ///  devices (and device drivers)
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public partial class ctlTantaVideoPickerViaReader : ctlOISBase
    {
        // this is the delegate we use to pass back picked device information
        public delegate void VideoDevicePickedEventHandler(object sender, TantaMFDevice videoDevice);
        public VideoDevicePickedEventHandler VideoDevicePickedEvent = null;

        // this is the delegate we use to pass back picked video format information
        public delegate void VideoFormatPickedEventHandler(object sender, TantaMFVideoFormatContainer videoFormatCont);
        public VideoFormatPickedEventHandler VideoFormatPickedEvent = null;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public ctlTantaVideoPickerViaReader()
        {
            InitializeComponent();            
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Displays the video devices on the system. Expects the MF system to have
        /// been started.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public void DisplayVideoCaptureDevices()
        {
            StringBuilder sb = new StringBuilder();

            // Query MF for the devices, can also use MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_AUDCAP_GUID 
            // here to see the audio capture devices
            List<TantaMFDevice> vcDevices = TantaWMFUtils.GetDevicesByCategory(MFAttributesClsid.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE, CLSID.MF_DEVSOURCE_ATTRIBUTE_SOURCE_TYPE_VIDCAP_GUID);
            if (vcDevices == null) return;

            foreach (TantaMFDevice mfDevice in vcDevices)
            {
                sb.Append("FriendlyName:" + mfDevice.FriendlyName);
                sb.Append("\r\n");
                sb.Append("Symbolic Name:" + mfDevice.SymbolicName);
                sb.Append("\r\n");
                sb.Append("\r\n");
            }
            // add all known devices
            comboBoxCaptureDevices.DataSource = vcDevices;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the currently chosen device
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TantaMFDevice CurrentDevice
        {
            get
            {
                return (TantaMFDevice)comboBoxCaptureDevices.SelectedItem;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle a selected index changed on our video capture device combo box
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void comboBoxCaptureDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            // reset this
            // display this
            DisplayVideoFormatsForCurrentCaptureDevice();
            // just send an event
            if (VideoDevicePickedEvent != null) VideoDevicePickedEvent(this, CurrentDevice);
            // just send a clearing event
            if (VideoFormatPickedEvent != null) VideoFormatPickedEvent(this, null);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Displays the video formats for the currently selected video device. This
        /// is more complicated than it looks. We have to open the video source, convert
        /// that to a Media Source and then interrogate the that source to find a list
        /// of video formats.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void DisplayVideoFormatsForCurrentCaptureDevice()
        {
            IMFSourceReaderAsync tmpSourceReader = null;
            List<TantaMFVideoFormatContainer> formatList;
            HResult hr;

            try
            {
                // clear what we have now
                listViewSupportedFormats.Clear();
                // reset this
                listViewSupportedFormats.ListViewItemSorter = null;

                // get the currently selected device
                TantaMFDevice currentDevice = (TantaMFDevice)comboBoxCaptureDevices.SelectedItem;
                if (currentDevice == null) return;

                // open up the media source
                tmpSourceReader = TantaWMFUtils.CreateSourceReaderAsyncFromDevice(currentDevice, null);
                if (tmpSourceReader == null)
                {
                    throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice, CreateSourceReaderAsyncFromDevice did not return a media source. Cannot continue.");
                }

                // now get a list of all supported formats from the video device
                hr = TantaMediaTypeInfo.GetSupportedVideoFormatsFromSourceReaderInFormatContainers(currentDevice, tmpSourceReader, 100, out formatList);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice, GetSupportedVideoFormatsFromSourceReaderInFormatContainers failed. HR=" + hr.ToString());
                }
                if (formatList == null)
                {
                    throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice, GetSupportedVideoFormatsFromSourceReaderInFormatContainers did not return a format list. Cannot continue.");
                }

                // now display the formats
                foreach (TantaMFVideoFormatContainer videoFormat in formatList)
                {
                    ListViewItem lvi = new ListViewItem(new[] { videoFormat.SubTypeAsString, videoFormat.FrameSizeAsString, videoFormat.FrameRateAsString, videoFormat.FrameRateMaxAsString, videoFormat.AllAttributes });
                    lvi.Tag = videoFormat;
                    listViewSupportedFormats.Items.Add(lvi);
                }

                listViewSupportedFormats.Columns.Add("Type", 70);
                listViewSupportedFormats.Columns.Add("FrameSize WxH", 100);
                listViewSupportedFormats.Columns.Add("FrameRate f/s", 100);
                listViewSupportedFormats.Columns.Add("FrameRateMax f/s", 100);
                listViewSupportedFormats.Columns.Add("All Attributes", 2500);
            }
            finally
            {
                if (tmpSourceReader != null)
                {
                    // close and release
                    Marshal.ReleaseComObject(tmpSourceReader);
                    tmpSourceReader = null;
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles a selected index changed on a video format in the list view
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void listViewSupportedFormats_SelectedIndexChanged(object sender, EventArgs e)
        {
            // just send a clearing event
            if (VideoFormatPickedEvent != null) VideoFormatPickedEvent(this, null);

            if (CurrentDevice == null) return;
            TantaMFVideoFormatContainer selectedVideoFormat = GetSelectedVideoFormatContainer();
            if (selectedVideoFormat == null) return;

            // just send the event
            if (VideoFormatPickedEvent != null) VideoFormatPickedEvent(this, GetSelectedVideoFormatContainer());
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the selected Video Format Container. Will return null.
        /// </summary>
        /// <returns>the selected video format container or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private TantaMFVideoFormatContainer GetSelectedVideoFormatContainer()
        { 
            if (listViewSupportedFormats.SelectedItems.Count == 0) return null;

            ListViewItem lvi = listViewSupportedFormats.SelectedItems[0];
            if (lvi == null) return null;
            if (lvi.Tag == null) return null;
            if ((lvi.Tag is TantaMFVideoFormatContainer) == false) return null;

            return (lvi.Tag as TantaMFVideoFormatContainer);
         }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles a double click on a video format in the list view
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void listViewSupportedFormats_DoubleClick(object sender, EventArgs e)
        {
            LogMessage("listViewSupportedFormats_DoubleClick");
            // send the event to report this
            if (VideoFormatPickedEvent != null) VideoFormatPickedEvent(this, GetSelectedVideoFormatContainer());
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles a double click on the list view. We do the sorting here
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void listViewSupportedFormats_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Set the ListViewItemSorter property to a new ListViewItemComparer
            // object.
            listViewSupportedFormats.ListViewItemSorter = new ListViewItemCompareAsText(e.Column);
            // Call the sort method to manually sort.
            listViewSupportedFormats.Sort();
        }
    }
}

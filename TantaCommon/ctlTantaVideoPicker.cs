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
/// This version uses a Media Source and Stream Descriptor to find the available video media types. There
/// is an alternative version which uses the Source Reader (see the ctlTantaVideoPickerViaReader control).

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
    public partial class ctlTantaVideoPicker : ctlOISBase
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
        public ctlTantaVideoPicker()
        {
            InitializeComponent();            
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Displays the video devices on the system. Expects the MF system to have
        /// been started.
        /// 
        /// NOTE: this function will throw exceptions - caller must trap them
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
        /// 
        /// NOTE: this function will throw exceptions - caller must trap them
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void DisplayVideoFormatsForCurrentCaptureDevice()
        {
            IMFPresentationDescriptor sourcePresentationDescriptor = null;
            int sourceStreamCount = 0;
            bool streamIsSelected = false;
            IMFStreamDescriptor videoStreamDescriptor = null;
            IMFMediaTypeHandler typeHandler = null;
            int mediaTypeCount = 0;

            List<TantaMFVideoFormatContainer> formatList = new List<TantaMFVideoFormatContainer>();
            HResult hr;
            IMFMediaSource mediaSource = null;

            try
            {
                // clear what we have now
                listViewSupportedFormats.Clear();
                // reset this
                listViewSupportedFormats.ListViewItemSorter = null;

                // get the currently selected device
                TantaMFDevice currentDevice = (TantaMFDevice)comboBoxCaptureDevices.SelectedItem;
                if (currentDevice == null)
                {
                    throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice currentDevice == null");
                }

                // use the device symbolic name to create the media source for the video device. Media sources are objects that generate media data. 
                // For example, the data might come from a video file, a network stream, or a hardware device, such as a camera. Each 
                // media source contains one or more streams, and each stream delivers data of one type, such as audio or video.                
                mediaSource = TantaWMFUtils.GetMediaSourceFromTantaDevice(currentDevice);
                if (mediaSource == null)
                {
                    throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice call to mediaSource == null");
                }

                // A presentation is a set of related media streams that share a common presentation time.
                // we don't need that functionality in this app but we do need to presentation descriptor
                // to find out the stream descriptors, these will give us the media types on offer
                hr = mediaSource.CreatePresentationDescriptor(out sourcePresentationDescriptor);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice call to mediaSource.CreatePresentationDescriptor failed. Err=" + hr.ToString());
                }
                if (sourcePresentationDescriptor == null)
                {
                    throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice call to mediaSource.CreatePresentationDescriptor failed. sourcePresentationDescriptor == null");
                }

                // Now we get the number of stream descriptors in the presentation. 
                // A presentation descriptor contains a list of one or more 
                // stream descriptors. 
                hr = sourcePresentationDescriptor.GetStreamDescriptorCount(out sourceStreamCount);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice call to sourcePresentationDescriptor.GetStreamDescriptorCount failed. Err=" + hr.ToString());
                }
                if (sourceStreamCount == 0)
                {
                    throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice call to sourcePresentationDescriptor.GetStreamDescriptorCount failed. sourceStreamCount == 0");
                }

                // look for the video stream
                for (int i = 0; i < sourceStreamCount; i++)
                {
                    // we require the major type to be video
                    Guid guidMajorType = TantaWMFUtils.GetMajorMediaTypeFromPresentationDescriptor(sourcePresentationDescriptor, i);
                    if (guidMajorType != MFMediaType.Video) continue;

                    // we also require the stream to be enabled
                    hr = sourcePresentationDescriptor.GetStreamDescriptorByIndex(i, out streamIsSelected, out videoStreamDescriptor);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(v) failed. Err=" + hr.ToString());
                    }
                    if (videoStreamDescriptor == null)
                    {
                        throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(v) failed. videoStreamDescriptor == null");
                    }
                    // if the stream is not selected (enabled) look for the next
                    if (streamIsSelected == false)
                    {
                        Marshal.ReleaseComObject(videoStreamDescriptor);
                        videoStreamDescriptor = null;
                        continue;
                    }

                    // Get the media type handler for the stream. IMFMediaTypeHandler 
                    // interface is a standard way of looking at the media types on an stream
                    hr = videoStreamDescriptor.GetMediaTypeHandler(out typeHandler);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("call to videoStreamDescriptor.GetMediaTypeHandler failed. Err=" + hr.ToString());
                    }
                    if (typeHandler == null)
                    {
                        throw new Exception("call to videoStreamDescriptor.GetMediaTypeHandler failed. typeHandler == null");
                    }
                    // Now we get the number of media types in the stream descriptor.
                    hr = typeHandler.GetMediaTypeCount(out mediaTypeCount);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice call to typeHandler.GetMediaTypeCount failed. Err=" + hr.ToString());
                    }
                    if (mediaTypeCount == 0)
                    {
                        throw new Exception("DisplayVideoFormatsForCurrentCaptureDevice call to typeHandler.GetMediaTypeCount failed. mediaTypeCount == 0");
                    }

                    // now loop through each media type
                    for (int mediaTypeId = 0; mediaTypeId < mediaTypeCount; mediaTypeId++)
                    {
                        // Now we have the handler, get the media type.
                        IMFMediaType workingMediaType = null;
                        hr = typeHandler.GetMediaTypeByIndex(mediaTypeId, out workingMediaType);
                        if (hr != HResult.S_OK)
                        {
                            throw new Exception("GetMediaTypeFromStreamDescriptorById call to typeHandler.GetMediaTypeByIndex failed. Err=" + hr.ToString());
                        }
                        if (workingMediaType == null)
                        {
                            throw new Exception("GetMediaTypeFromStreamDescriptorById call to typeHandler.GetMediaTypeByIndex failed. workingMediaType == null");
                        }
                        TantaMFVideoFormatContainer tmpContainer = TantaMediaTypeInfo.GetVideoFormatContainerFromMediaTypeObject(workingMediaType, currentDevice);
                        if (tmpContainer == null)
                        {
                            // we failed
                            throw new Exception("GetSupportedVideoFormatsFromSourceReaderInFormatContainers failed on call to GetVideoFormatContainerFromMediaTypeObject");
                        }
                        // now add it
                        formatList.Add(tmpContainer);
                        Marshal.ReleaseComObject(workingMediaType);
                        workingMediaType = null;                        
                    }

                    // NOTE: we only do the first enabled video stream we find.
                    // it is possible to have more but our control
                    // cannot cope with that
                    break;
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
                // close and release
                if (mediaSource != null)
                {
                    Marshal.ReleaseComObject(mediaSource);
                    mediaSource = null;
                }
                if (sourcePresentationDescriptor != null)
                {
                    Marshal.ReleaseComObject(sourcePresentationDescriptor);
                    sourcePresentationDescriptor = null;
                }
                if (videoStreamDescriptor != null)
                {
                    Marshal.ReleaseComObject(videoStreamDescriptor);
                    videoStreamDescriptor = null;
                }
                if (typeHandler != null)
                {
                    Marshal.ReleaseComObject(typeHandler);
                    typeHandler = null;
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

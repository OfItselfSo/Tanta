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

/// The function of this control is to display the transforms on a system and associated information. The user
/// can choose a specific transform from a list. The selected information will be communicated back to the owner via an event.

namespace TantaCommon
{

    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// A class to display the transforms on a system and associated
    /// information. Note that available capabilities will vary widely between 
    /// transforms
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public partial class ctlTantaTransformPicker : ctlOISBase
    {
        // this is the delegate we use to pass back picked transform category information
        public delegate void TransformCategoryPickedEventHandler(object sender, TantaGuidNamePair transformCategory);
        public TransformCategoryPickedEventHandler TransformCategoryPickedEvent = null;

        // this is the delegate we use to pass back picked transform information
        public delegate void TransformPickedEventHandler(object sender, TantaMFTCapabilityContainer transformContainer);
        public TransformPickedEventHandler TransformPickedEvent = null;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public ctlTantaTransformPicker()
        {
            InitializeComponent();            
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Displays the transform categories on the system. Expects the MF system to have
        /// been started.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public void DisplayTransformCategories()
        {
            StringBuilder sb = new StringBuilder();

            List<TantaGuidNamePair> trnsCategories = new List<TantaGuidNamePair>();

            // just add them all
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_VIDEO_DECODER", MFTransformCategory.MFT_CATEGORY_VIDEO_DECODER));
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_VIDEO_ENCODER", MFTransformCategory.MFT_CATEGORY_VIDEO_ENCODER));
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_VIDEO_EFFECT", MFTransformCategory.MFT_CATEGORY_VIDEO_EFFECT));
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_MULTIPLEXER", MFTransformCategory.MFT_CATEGORY_MULTIPLEXER));
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_DEMULTIPLEXER", MFTransformCategory.MFT_CATEGORY_DEMULTIPLEXER));
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_AUDIO_DECODER", MFTransformCategory.MFT_CATEGORY_AUDIO_DECODER));
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_AUDIO_ENCODER", MFTransformCategory.MFT_CATEGORY_AUDIO_ENCODER));
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_AUDIO_EFFECT", MFTransformCategory.MFT_CATEGORY_AUDIO_EFFECT));
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_VIDEO_PROCESSOR", MFTransformCategory.MFT_CATEGORY_VIDEO_PROCESSOR));
            trnsCategories.Add(new TantaGuidNamePair("MFT_CATEGORY_OTHER", MFTransformCategory.MFT_CATEGORY_OTHER));

            // add all known categories
            comboBoxTransformCategories.DataSource = trnsCategories;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the currently chosen transform category
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TantaGuidNamePair CurrentTransformCategory
        {
            get
            {
                return (TantaGuidNamePair)comboBoxTransformCategories.SelectedItem;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle a selected index changed on our transform categries combo box
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void comboBoxTransformCategories_SelectedIndexChanged(object sender, EventArgs e)
        {
            // reset this
            // display this
            DisplayTransformsForCurrentCategory();
            // just send an event
            if (TransformCategoryPickedEvent != null) TransformCategoryPickedEvent(this, CurrentTransformCategory);
            // just send a clearing event
            if (TransformPickedEvent != null) TransformPickedEvent(this, null);

            // clear down the transform info panel
            ClearTransformInfoPanel();

            // for some reason the listview does not automatically draw its 
            // horizontal scroll bar until resized. We do this here now.
            listViewAvailableTransforms.Width = listViewAvailableTransforms.Width + 1;
            listViewAvailableTransforms.Width = listViewAvailableTransforms.Width - 1;

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Displays the transforms for the currently selected category. Since each
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void DisplayTransformsForCurrentCategory()
        {
            int numResults;
            IMFActivate[] activatorArray;
            List<TantaMFTCapabilityContainer> transformList = new List<TantaMFTCapabilityContainer>();
            HResult hr;

            try
            {
                // clear what we have now
                listViewAvailableTransforms.Clear();
                // reset this
                listViewAvailableTransforms.ListViewItemSorter = null;

                // get the currently selected major category
                TantaGuidNamePair currentCategory = (TantaGuidNamePair)comboBoxTransformCategories.SelectedItem;
                if (currentCategory == null) return;

                // we have multiple sub-categories. These are set by specific flags on the MFTEnumX call. We iterate
                // through each flag and get the matching transforms. If we already have it we just set the flag on
                // the exisiting one to show it is in multiple sub-categories

                foreach (MFT_EnumFlag flagVal in Enum.GetValues(typeof(MFT_EnumFlag)))
                {
                    // we do not need this one
                    if (flagVal == MFT_EnumFlag.None) continue;
                    // The documentation states that there is no way to enumerate just local MFTs and nothing else. 
                    // Setting Flags equal to MFT_ENUM_FLAG_LOCALMFT is equivalent to including the MFT_ENUM_FLAG_SYNCMFT flag 
                    // which messes us up. This also appears to be true for the FieldOfUse and transcode only flags so we
                    // do not include them
                    if (flagVal == MFT_EnumFlag.LocalMFT) continue;
                    if (flagVal == MFT_EnumFlag.FieldOfUse) continue;
                    if (flagVal == MFT_EnumFlag.TranscodeOnly) continue;
                    // some of the higher flags are just for sorting the return results 
                    if (flagVal >= MFT_EnumFlag.All) break;

                    hr = MFExtern.MFTEnumEx(currentCategory.GuidValue, flagVal, null, null, out activatorArray, out numResults);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("DisplayTransformsForCurrentCategory, call to MFExtern.MFTEnumEx failed. HR=" + hr.ToString());
                    }

                    // now loop through the returned activators
                    for (int i = 0; i < numResults; i++)
                    {

                        // extract the friendlyName and symbolicLinkName
                        Guid outGuid = TantaWMFUtils.GetGuidForKeyFromActivator(activatorArray[i], MFAttributesClsid.MFT_TRANSFORM_CLSID_Attribute);
                        string friendlyName = TantaWMFUtils.GetStringForKeyFromActivator(activatorArray[i], MFAttributesClsid.MFT_FRIENDLY_NAME_Attribute);

                        // create a new TantaMFTCapabilityContainer for it
                        TantaMFTCapabilityContainer workingMFTContainer = new TantaMFTCapabilityContainer(friendlyName, outGuid, currentCategory);
                        // do we have this in our list yet 
                        int index = transformList.FindIndex(x => x.TransformGuidValue == workingMFTContainer.TransformGuidValue);
                        if (index >= 0)
                        {
                            // yes, it does contain this transform, just record the new sub-category
                            transformList[index].EnumFlags |= flagVal;
                        }
                        else
                        {
                            // no, it does not contain this transform yet, set the sub-category
                            workingMFTContainer.EnumFlags = flagVal;
                            // and add it
                            transformList.Add(workingMFTContainer);

                            if ((activatorArray[i] is IMFAttributes)==true)
                            {
                                StringBuilder outSb = null;
                                List<string> attributesToIgnore = new List<string>();
                                attributesToIgnore.Add("MFT_FRIENDLY_NAME_Attribute");
                                attributesToIgnore.Add("MFT_TRANSFORM_CLSID_Attribute");
                                attributesToIgnore.Add("MF_TRANSFORM_FLAGS_Attribute");
                                attributesToIgnore.Add("MF_TRANSFORM_CATEGORY_Attribute");
                                hr = TantaWMFUtils.EnumerateAllAttributeNamesAsText((activatorArray[i] as IMFAttributes), attributesToIgnore, 100, out outSb);
                               
                            }
                        }

                        // clean up our activator
                        Marshal.ReleaseComObject(activatorArray[i]);
                    }
                }

                // now display the transforms
                foreach (TantaMFTCapabilityContainer mftCapability in transformList)
                {
                    ListViewItem lvi = new ListViewItem(new[] { mftCapability.TransformFriendlyName, mftCapability.IsSyncMFT, mftCapability.IsAsyncMFT, mftCapability.IsHardware, /* mftCapability.IsFieldOfUse, mftCapability.IsLocalMFT, mftCapability.IsTranscodeOnly, */ mftCapability.TransformGuidValueAsString});
                    lvi.Tag = mftCapability;
                    listViewAvailableTransforms.Items.Add(lvi);
                }

                listViewAvailableTransforms.Columns.Add("Name", 250);
                listViewAvailableTransforms.Columns.Add("IsSyncMFT", 70);
                listViewAvailableTransforms.Columns.Add("IsAsyncMFT", 90);
                listViewAvailableTransforms.Columns.Add("IsHardware", 90);
              //  listViewAvailableTransforms.Columns.Add("IsFieldOfUse", 90);
              //  listViewAvailableTransforms.Columns.Add("IsLocalMFT", 90);
              //  listViewAvailableTransforms.Columns.Add("IsTranscodeOnly", 90);
                listViewAvailableTransforms.Columns.Add("Guid", 200);
            }
            finally
            {
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles a selected index changed on a transform in the list view
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void listViewAvailableTransforms_SelectedIndexChanged(object sender, EventArgs e)
        {
            // just send a clearing event
            if (TransformPickedEvent != null) TransformPickedEvent(this, null);

            if (CurrentTransformCategory == null) return;
            TantaMFTCapabilityContainer selectedTransform = GetSelectedTransformContainer();
            if (selectedTransform == null) return;

            // just send the event
            if (TransformPickedEvent != null) TransformPickedEvent(this, GetSelectedTransformContainer());

            // set the display
            SetTransformInfoPanel(selectedTransform);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the transform information panel on the control
        /// </summary>
        /// <param name="transformToDisplay">the transform to display</param>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void SetTransformInfoPanel(TantaMFTCapabilityContainer transformToDisplay)
        {
            List<IMFMediaType> outputTypes = null;
            List<IMFMediaType> inputTypes = null;
            StringBuilder displaySb = new StringBuilder();
            StringBuilder outputSb = new StringBuilder();
            StringBuilder inputSb = new StringBuilder();
            StringBuilder headerSb = new StringBuilder();
            StringBuilder outSb;
            HResult hr;

            // clear it down
            ClearTransformInfoPanel();

            if (transformToDisplay == null) return;

            // set up our header information
            headerSb.Append(transformToDisplay.TransformFriendlyName);
            headerSb.Append("\r\n");
            headerSb.Append(transformToDisplay.TransformGuidValueAsString);
            headerSb.Append("\r\n");
            if (transformToDisplay.IsAsyncMFT == "x") headerSb.Append("IsAsyncMFT" + ", ");
            if (transformToDisplay.IsSyncMFT == "x") headerSb.Append("IsSyncMFT" + ", ");
            if (transformToDisplay.IsFieldOfUse == "x") headerSb.Append("IsFieldOfUse" + ", ");

            // we do not include these, the enum function does not give us this
            // if (transformToDisplay.IsHardware == "x") headerSb.Append("IsHardware" + ", ");
            // if (transformToDisplay.IsLocalMFT == "x") headerSb.Append("IsLocalMFT" + ", ");
            // if (transformToDisplay.IsTranscodeOnly == "x") headerSb.Append("IsTranscodeOnly" + ", ");
            headerSb.Append("\r\n");

            try
            {
                // populate the RichText box with the input media type capabilities
                inputTypes = TantaWMFUtils.GetInputMediaTypesFromTransformByGuid(transformToDisplay.TransformGuidValue, false);

                // do we have any input types?
                if ((inputTypes != null) && (inputTypes.Count != 0))
                {
                    // go through the types
                    foreach (IMFMediaType mediaType in inputTypes)
                    {
                        // the major media type
                        hr = TantaMediaTypeInfo.GetMediaMajorTypeAsText(mediaType, out outSb);
                        if (hr != HResult.S_OK) continue;
                        if (outSb == null) continue;
                        inputSb.Append(outSb);
                        inputSb.Append("\r\n");

                        // the sub media type
                        hr = TantaMediaTypeInfo.GetMediaSubTypeAsText(mediaType, out outSb);
                        if (hr != HResult.S_OK) continue;
                        if (outSb == null) continue;
                        inputSb.Append(outSb);
                        inputSb.Append("\r\n");

                        // enumerate all of the possible Attributes so we can see which ones are present that we did not report on
                        StringBuilder allAttrs = new StringBuilder();
                        hr = TantaMediaTypeInfo.EnumerateAllAttributeNamesInMediaTypeAsText(mediaType, true, true, TantaWMFUtils.MAX_TYPES_TESTED_PER_TRANSFORM, out allAttrs);
                        if (hr != HResult.S_OK) continue;
                        char[] charsToTrim = { ',', '.', ' ' };
                        inputSb.Append("OtherAttrs=" + allAttrs.ToString().TrimEnd(charsToTrim));
                        inputSb.Append("\r\n");

                        inputSb.Append("\r\n");
                    }
                }
            }
            finally
            {
                // release the list of media type objects
                if ((inputTypes != null) && (inputTypes.Count != 0))
                {
                    foreach (IMFMediaType mediaType in inputTypes)
                    {
                        Marshal.ReleaseComObject(mediaType);
                    }
                }
            }

            try
            {
                // populate the RichText box with the output media type capabilities
                outputTypes = TantaWMFUtils.GetOutputMediaTypesFromTransformByGuid(transformToDisplay.TransformGuidValue, false);

                // do we have any output types?
                if ((outputTypes != null) && (outputTypes.Count != 0))
                {
                    // go through the types
                    foreach (IMFMediaType mediaType in outputTypes)
                    {
                        // the major media type
                        hr = TantaMediaTypeInfo.GetMediaMajorTypeAsText(mediaType, out outSb);
                        if (hr != HResult.S_OK) continue;
                        if (outSb == null) continue;
                        outputSb.Append(outSb);
                        outputSb.Append("\r\n");

                        // the sub media type
                        hr = TantaMediaTypeInfo.GetMediaSubTypeAsText(mediaType, out outSb);
                        if (hr != HResult.S_OK) continue;
                        if (outSb == null) continue;
                        outputSb.Append(outSb);
                        outputSb.Append("\r\n");

                        // enumerate all of the possible Attributes so we can see which ones are present that we did not report on
                        StringBuilder allAttrs = new StringBuilder();
                        hr = TantaMediaTypeInfo.EnumerateAllAttributeNamesInMediaTypeAsText(mediaType, true, true, TantaWMFUtils.MAX_TYPES_TESTED_PER_TRANSFORM, out allAttrs);
                        if (hr != HResult.S_OK) continue;
                        char[] charsToTrim = { ',', '.', ' ' };
                        outputSb.Append("OtherAttrs=" + allAttrs.ToString().TrimEnd(charsToTrim));
                        outputSb.Append("\r\n");

                        outputSb.Append("\r\n");
                    }
                }
            }
            finally
            {
                // release the list of media type objects
                if ((outputTypes!=null) && (outputTypes.Count !=0))
                {
                    foreach(IMFMediaType mediaType in outputTypes)
                    {
                        Marshal.ReleaseComObject(mediaType);
                    }
                }
            }

            // display what we have
            displaySb.Append(headerSb);
            displaySb.Append("\r\n");
            displaySb.Append("\r\n");

            displaySb.Append("####\r\n");
            displaySb.Append("#### INPUT TYPES\r\n");
            displaySb.Append("####\r\n");
            displaySb.Append("\r\n");
            if (inputSb.Length > 0)
            {
                displaySb.Append(inputSb);
            }
            else
            {
                displaySb.Append("<not known>");
                displaySb.Append("\r\n");
            }
            displaySb.Append("\r\n");

            displaySb.Append("####\r\n");
            displaySb.Append("#### OUTPUT TYPES\r\n");
            displaySb.Append("####\r\n");
            displaySb.Append("\r\n");
            if (outputSb.Length > 0)
            {
                displaySb.Append(outputSb);
            }
            else
            {
                displaySb.Append("<not known>");
                displaySb.Append("\r\n");
            }
            displaySb.Append("\r\n");

            richTextBoxtTransformDetails.Text = displaySb.ToString();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// clears the transform information panel on the control
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void ClearTransformInfoPanel()
        {
            // clear this
            richTextBoxtTransformDetails.Text = "";
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the selected transform Container. Will return null.
        /// </summary>
        /// <returns>the selected transform container or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private TantaMFTCapabilityContainer GetSelectedTransformContainer()
        { 
            if (listViewAvailableTransforms.SelectedItems.Count == 0) return null;

            ListViewItem lvi = listViewAvailableTransforms.SelectedItems[0];
            if (lvi == null) return null;
            if (lvi.Tag == null) return null;
            if ((lvi.Tag is TantaMFTCapabilityContainer) == false) return null;

            return (lvi.Tag as TantaMFTCapabilityContainer);
         }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles a double click on a transform in the list view
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void listViewAvailableTransforms_DoubleClick(object sender, EventArgs e)
        {
            LogMessage("listViewAvailableTransforms_DoubleClick");

            // get the selected transform
            TantaMFTCapabilityContainer selectedTransform = GetSelectedTransformContainer();
            if (selectedTransform == null) return;


            // send the event to report this
            if (TransformPickedEvent != null) TransformPickedEvent(this, selectedTransform);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles a double click on the list view. We do the sorting here
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void listViewAvailableTransforms_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            // Set the ListViewItemSorter property to a new ListViewItemComparer
            // object.
            listViewAvailableTransforms.ListViewItemSorter = new ListViewItemCompareAsText(e.Column);
            // Call the sort method to manually sort.
            listViewAvailableTransforms.Sort();
        }
    }
}

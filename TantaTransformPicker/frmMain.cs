using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
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

/// The function of this app is to display the transforms on a system and some associated items of information. The user
/// can choose a specific transform from a list. It does not do anything with that information beyond displaying it.

namespace TantaTransformPicker
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// The main form for the application
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Started
    /// </history>
    public partial class frmMain : frmOISBase
    {
        private const string DEFAULTLOGDIR = @"C:\Dump\Project Logs";
        private const string APPLICATION_NAME = "TantaTransformPicker";
        private const string APPLICATION_VERSION = "01.00";

        public const string NO_SELECTED_FORMAT = " <No Selected Format>";

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        public frmMain()
        {
            bool retBOOL = false;
            HResult hr = 0;

            // set the current directory equal to the exe directory. We do this because
            // people can start from a link and if the start-in directory is not right
            // it can put the log file in strange places
            Directory.SetCurrentDirectory(Application.StartupPath);

            // set up the Singleton g_Logger instance. Simply using it in a test
            // creates it.
            if (g_Logger == null)
            {
                // did not work, nothing will start say so now in a generic way
                MessageBox.Show("Logger Class Failed to Initialize. Nothing will work well.");
                return;
            }
            // record this in the logger for everybodys use
            g_Logger.ApplicationMainForm = this;
            g_Logger.DefaultDialogBoxTitle = APPLICATION_NAME;
            try
            {
                // set the icon for this form and for all subsequent forms
                g_Logger.AppIcon = new Icon(GetType(), "App.ico");
                this.Icon = new Icon(GetType(), "App.ico");
            }
            catch (Exception)
            {
            }

            // Register the global error handler as soon as we can in Main
            // to make sure that we catch as many exceptions as possible
            // this is a last resort. All execeptions should really be trapped
            // and handled by the code.
            OISGlobalExceptions ex1 = new OISGlobalExceptions();
            Application.ThreadException += new ThreadExceptionEventHandler(ex1.OnThreadException);

            // set the culture so our numbers convert consistently
            System.Threading.Thread.CurrentThread.CurrentCulture = g_Logger.GetDefaultCulture();

            InitializeComponent();

            // set up our logging
            retBOOL = g_Logger.InitLogging(DEFAULTLOGDIR, APPLICATION_NAME, false);
            if (retBOOL == false)
            {
                // did not work, nothing will start say so now in a generic way
                MessageBox.Show("The log file failed to create. No log file will be recorded.");
            }
            // pump out the header
            g_Logger.EmitStandardLogfileheader(APPLICATION_NAME);
            LogMessage("");
            LogMessage("Version: " + APPLICATION_VERSION);
            LogMessage("");

            // we always have to initialize MF. The 0x00020070 here is the WMF version 
            // number used by the MF.Net samples. Not entirely sure if it is appropriate
            hr = MFExtern.MFStartup(0x00020070, MFStartup.Full);
            if (hr != 0)
            {
                LogMessage("Constructor: call to MFExtern.MFStartup returned " + hr.ToString());
            }

            // set up our Video Picker Control
            ctlTantaTransformPicker1.TransformPickedEvent += new ctlTantaTransformPicker.TransformPickedEventHandler(TransformPickedHandler);

            // set this
            buttonSelectedFormat.Text = NO_SELECTED_FORMAT;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The form loaded handler
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void frmMain_Load(object sender, EventArgs e)
        {
            LogMessage("frmMain_Load");
            ctlTantaTransformPicker1.DisplayTransformCategories();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// The form closing handler
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            LogMessage("frmMain_FormClosing");
            try
            {
                // do everything to close all media devices
                CloseAllMediaDevices();

                // Shut down MF
                MFExtern.MFShutdown();
            }
            catch
            {
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A centralized place to close down all media devices 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void CloseAllMediaDevices()
        {
            // nothing to do here. 
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        ///  Handle a picked transform and associated information
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void TransformPickedHandler(object sender, TantaMFTCapabilityContainer videoFormatCont)
        {
            string mfTransformName = "<unknown transform>";
            string categoryName = "<unknown category>";

            // set these now
            if (videoFormatCont != null)
            {
                categoryName = videoFormatCont.MFTCategoryFriendlyName;
                mfTransformName = videoFormatCont.TransformFriendlyName;
                // set the button text appropriately
                buttonSelectedFormat.Text = mfTransformName + " " + categoryName;
                // save the container here - this is the last one that came in
                buttonSelectedFormat.Tag = videoFormatCont;
            }
            else
            {
                // set the button text appropriately
                buttonSelectedFormat.Text = mfTransformName + " " + categoryName;
                // save the container here - this is the last one that came in
                buttonSelectedFormat.Tag = null;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles a press on the buttonSelectedFormat button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Started
        /// </history>
        private void buttonSelectedFormat_Click(object sender, EventArgs e)
        {
            LogMessage("buttonSelectedFormat_Click");

            string mfTransformName = "<unknown transform>";
            string categoryName = "<unknown category>";

            if ((buttonSelectedFormat.Tag == null) || ((buttonSelectedFormat.Tag is TantaMFTCapabilityContainer) == false))
            {
                MessageBox.Show("No transform and category selected");
                return;
            }
            if ((buttonSelectedFormat.Tag as TantaMFTCapabilityContainer).TransformFriendlyName.Length == 0)
            {
                MessageBox.Show("Invalid transform name");
                return;
            }

            // set these now
            mfTransformName = (buttonSelectedFormat.Tag as TantaMFTCapabilityContainer).TransformFriendlyName;
            categoryName = (buttonSelectedFormat.Tag as TantaMFTCapabilityContainer).MFTCategoryFriendlyName;

            // all we do here is display the info. In other apps we would use this to set up
            // the video source

            ctlTantaTransformPicker1.Enabled = false;
            MessageBox.Show("You have chosen\n\n" + mfTransformName + "\n\n" + categoryName, "Transform Summary");
            ctlTantaTransformPicker1.Enabled = true;
        }

    }
}

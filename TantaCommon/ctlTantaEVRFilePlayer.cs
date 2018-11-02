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

/// The function of this control is to play a video on the controls visible area. The user
/// can choose to start, pause and stop the playback and
/// can fast forward, or seek forward and reverse and play in slow motion.
/// demonstrations of how to tie the seek postion to a scroll bar are provided
/// as are examples of how to change the volume and mute the sound.
/// 
namespace TantaCommon
{
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
    /// <summary>
    /// A class to play video from a file on a panel owned by this control. We
    /// restrict the drawing region to this panel so that we can implement other
    /// controls (buttons & etc) without drawing over the top of them.
    /// </summary>
    /// <history>
    ///    01 Nov 18  Cynic - Originally Written
    /// </history>
    public partial class ctlTantaEVRFilePlayer : ctlOISBase
    {
        // used when saving bit map snapshots of the video screen
        private const string BITMAP_EXTENSION = ".bmp";
        private const string BITMAP_PREFIX = "TantaBitmap";

        // this is the amount (relative to 1) that we increment or decrement 
        // in order to be able to fast forward or rewind
        const float DEFAULT_PLAYBACKRATE_INCREMENT = 0.2f;
        const float DEFAULT_AUDIOVOLUME_INCREMENT = 0.1f;

        // The current state of the EVR Player
        private TantaEVRPlayerStateEnum playerState = TantaEVRPlayerStateEnum.Ready;

        // a flag to indicate if the rate is currently changing so we do not do this 
        // to quickly
        private bool rateIsChanging = false;

        // this is the duration (in 100ns units) of the video that is playing
        private UInt64 videoDuration = 0;

        // the video file and path we wish to play
        private string videoFileAndPathToPlay = "";

        // the call back handler for the mediaSession
        private TantaAsyncCallbackHandler mediaSessionAsyncCallbackHandler = null;

        // A session provides playback controls for the media content. The Media Session and the protected media path (PMP) session objects 
        // expose this interface. This interface is the primary interface that applications use to control the Media Foundation pipeline.
        protected IMFMediaSession mediaSession;

        // Media sources are objects that generate media data. For example, the data might come from a video file, a network stream, 
        // or a hardware device, such as a camera. Each media source contains one or more streams, and each stream delivers 
        // data of one type, such as audio or video.
        protected IMFMediaSource mediaSource;

        // The Enhanced Video Renderer(EVR) implements this interface and it controls how the EVR presenter displays video.
        protected IMFVideoDisplayControl evrVideoDisplay;

        // if we are using a transform (as a binary) this will be non-null
        protected IMFTransform videoTransform = null;
        // if we are using a transform (as a guid) this will be non-empty
        protected Guid videoTransformGuid = Guid.Empty;
        // this is the node in the topology which contains the transform
        // it can be null if there is no transform
        private IMFTopologyNode videoTransformNode = null;

        // our player state changed delegate + event
        public delegate void TantaEVRFilePlayerStateChangedEvent_Delegate(object sender, TantaEVRPlayerStateEnum playerState);
        public TantaEVRFilePlayerStateChangedEvent_Delegate TantaEVRFilePlayerStateChangedEvent = null;

        // our player error occurred delegate + event
        public delegate void TantaEVRFilePlayerErrorEvent_Delegate(object sender, string errMsg, Exception ex);
        public TantaEVRFilePlayerErrorEvent_Delegate TantaEVRFilePlayerErrorEvent = null;

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Constructor
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public ctlTantaEVRFilePlayer()
        {
            InitializeComponent();

            SyncScreenStateToVideoFileAndPlayingState();

            SetupVideoPositionScrollBar();

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
        /// Close down this control. There is no easy restart from this.
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
            HResult hr;

            // set the player state
            PlayerState = TantaEVRPlayerStateEnum.Ready;
            // reset this flag
            rateIsChanging = false;
            // reset this
            VideoDuration = 0;

            LogMessage("CloseAllMediaDevices called");
       
            // stop any messaging or events in the call back handler
            if (mediaSessionAsyncCallbackHandler != null) mediaSessionAsyncCallbackHandler.ShutDown();
            // close and release our call back handler
            if (mediaSessionAsyncCallbackHandler != null)
            {
                mediaSessionAsyncCallbackHandler.ShutDown();
                mediaSessionAsyncCallbackHandler = null;
            }

            // release the video display
            if (evrVideoDisplay != null)
            {
                Marshal.ReleaseComObject(evrVideoDisplay);
                evrVideoDisplay = null;
            }

            // close the session
            if (mediaSession != null)
            {
                hr = mediaSession.Close();
                if (hr != HResult.S_OK)
                {
                    // just log it
                    LogMessage("CloseAllMediaDevices call to mediaSession.Close failed. Err=" + hr.ToString());
                }
            }

            // Shut down the media source
            if (mediaSource != null)
            {
                hr = mediaSource.Shutdown();
                if (hr != HResult.S_OK)
                {
                    // just log it
                    LogMessage("CloseAllMediaDevices call to mediaSource.Shutdown failed. Err=" + hr.ToString());
                }
                Marshal.ReleaseComObject(mediaSource);
                mediaSource = null;
            }

            // Shut down the media session.
            if (mediaSession != null)
            {
                hr = mediaSession.Shutdown();
                if (hr != HResult.S_OK)
                {
                    // just log it
                    LogMessage("CloseAllMediaDevices call to mediaSession.Shutdown failed. Err=" + hr.ToString());
                }
                Marshal.ReleaseComObject(mediaSession);
                mediaSession = null;
            }

            // null this out. It will already have been safely released
            // when the session is shutdown. We just don't want a 
            // record of it anymore
            VideoTransformNode = null;

        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Opens the video file and prepares the media session and topology. Uses
        /// the VideoFileAndPathToPlay set in the control. Will throw an exception
        /// on any error.
        /// 
        /// Once the video session and topology are setup a MESessionTopologySet event
        /// will be triggered in the callback handler. After that the events there
        /// trigger other events and everything rolls along automatically.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void OpenVideoFileAndPrepareSessionAndPlay()
        {
            HResult hr;
            IMFSourceResolver sourceResolverObj = null;
            IMFTopology pTopology = null;
            IMFPresentationDescriptor sourcePresentationDescriptor = null;
            int sourceStreamCount = 0;
            bool streamIsSelected = false;
            IMFStreamDescriptor audioStreamDescriptor = null;
            IMFStreamDescriptor videoStreamDescriptor = null;
            IMFTopologyNode sourceAudioNode = null;
            IMFTopologyNode sourceVideoNode = null;
            IMFTopologyNode outputSinkNodeAudio = null;
            IMFTopologyNode outputSinkNodeVideo = null;
            IMFMediaType currentAudioMediaType = null;
            IMFMediaType currentVideoMediaType = null;

            LogMessage("OpenVideoFileAndPrepareSessionAndPlay ");

            // we sanity check the filename - the existence of the path and  file 
            // should have been checked before this call
            if (File.Exists(VideoFileAndPathToPlay) == false)
            {
                OISMessageBox("No video file to play.");
                return;
            }

            // we only permit this action if we are not playing
            if (PlayerState != TantaEVRPlayerStateEnum.Ready)
            {
                OISMessageBox("A video is currently playing.");
                return;
            }
            try
            {
                // reset everything
                CloseAllMediaDevices();

                // Set our state to "open pending"
                PlayerState = TantaEVRPlayerStateEnum.OpenPending;

                // Create the media session.
                hr = MFExtern.MFCreateMediaSession(null, out mediaSession);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to MFExtern.MFCreateMediaSession failed. Err=" + hr.ToString());
                }
                if (mediaSession == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to MFExtern.MFCreateMediaSession failed. mediaSession == null");
                }

                // set up our media session call back handler.
                mediaSessionAsyncCallbackHandler = new TantaAsyncCallbackHandler();
                mediaSessionAsyncCallbackHandler.Initialize();
                mediaSessionAsyncCallbackHandler.MediaSession = mediaSession;
                mediaSessionAsyncCallbackHandler.MediaSessionAsyncCallBackError = HandleMediaSessionAsyncCallBackErrors;
                mediaSessionAsyncCallbackHandler.MediaSessionAsyncCallBackEvent = HandleMediaSessionAsyncCallBackEvent;

                // Register the callback handler with the session and tell it that events can
                // start. This does not actually trigger an event it just lets the media session 
                // know that it can now send them if it wishes to do so.
                hr = mediaSession.BeginGetEvent(mediaSessionAsyncCallbackHandler, null);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to mediaSession.BeginGetEvent failed. Err=" + hr.ToString());
                }

                // Create a new topology.  A topology describes a collection of media sources, sinks, and transforms that are 
                // connected in a certain order. These objects are represented within the topology by topology nodes, 
                // which expose the IMFTopologyNode interface. A topology describes the path of multimedia data through these nodes.
                hr = MFExtern.MFCreateTopology(out pTopology);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to MFExtern.MFCreateTopology failed. Err=" + hr.ToString());
                }
                if (pTopology == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to MFExtern.MFCreateTopology failed. pTopology == null");
                }

                // ####
                // #### we now create the media source, this is an file with audio and video (mp4)
                // ####

                // use the file name to create the media source for the media device. Media sources are objects that generate media data. 
                // For example, the data might come from a video file, a network stream, or a hardware device, such as a camera. Each 
                // media source contains one or more streams, and each stream delivers data of one type, such as audio or video.                
                mediaSource = TantaWMFUtils.GetMediaSourceFromFile(VideoFileAndPathToPlay);
                if (mediaSource == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to mediaSource == null");
                }

                // A presentation is a set of related media streams that share a common presentation time.  We now get a copy of the media 
                // source's presentation descriptor. Applications can use the presentation descriptor to select streams 
                // and to get information about the source content.
                hr = mediaSource.CreatePresentationDescriptor(out sourcePresentationDescriptor);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to mediaSource.CreatePresentationDescriptor failed. Err=" + hr.ToString());
                }
                if (sourcePresentationDescriptor == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to mediaSource.CreatePresentationDescriptor failed. sourcePresentationDescriptor == null");
                }

                // Now we get the number of stream descriptors in the presentation. A presentation descriptor contains a list of one or more 
                // stream descriptors. These describe the streams in the presentation. Streams can be either selected or deselected. Only the 
                // selected streams produce data. Deselected streams are not active and do not produce any data. 
                hr = sourcePresentationDescriptor.GetStreamDescriptorCount(out sourceStreamCount);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to sourcePresentationDescriptor.GetStreamDescriptorCount failed. Err=" + hr.ToString());
                }
                if (sourceStreamCount == 0)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to sourcePresentationDescriptor.GetStreamDescriptorCount failed. sourceStreamCount == 0");
                }

                // We look at each stream here
                // there will usually be more than one and can be multiple ones
                //  of each type (video, audio etc). Usually only one is enabled. 

                // Because this is app is for the purposes of demonstration we are going to 
                // do each stream type separately rather - one loop for each. Normally 
                // you would just look at the type inside the loop and then make decisions
                // about what to do with it. We are going to "unroll" this process to make 
                // it obvious. We use the first "selected" stream of the appropriate type 
                // we come to

                // first look for the video stream
                for (int i = 0; i < sourceStreamCount; i++)
                {
                    // we require the major type to be video
                    Guid guidMajorType = TantaWMFUtils.GetMajorMediaTypeFromPresentationDescriptor(sourcePresentationDescriptor, i);
                    if (guidMajorType != MFMediaType.Video) continue;

                    // we also require the stream to be enabled
                    hr = sourcePresentationDescriptor.GetStreamDescriptorByIndex(i, out streamIsSelected, out videoStreamDescriptor);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(v) failed. Err=" + hr.ToString());
                    }
                    if (videoStreamDescriptor == null)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(v) failed. videoStreamDescriptor == null");
                    }
                    // if the stream is selected, leave now we will release the videoStream descriptor later
                    if (streamIsSelected == true) break;

                    // release the one we are not using
                    if (videoStreamDescriptor != null)
                    {
                        Marshal.ReleaseComObject(videoStreamDescriptor);
                        videoStreamDescriptor = null;
                    }
                }

                // next look for the audio stream
                for (int i = 0; i < sourceStreamCount; i++)
                {
                    // we require the major type to be audio
                    Guid guidMajorType = TantaWMFUtils.GetMajorMediaTypeFromPresentationDescriptor(sourcePresentationDescriptor, i);
                    if (guidMajorType != MFMediaType.Audio) continue;

                    // we also require the stream to be enabled
                    hr = sourcePresentationDescriptor.GetStreamDescriptorByIndex(i, out streamIsSelected, out audioStreamDescriptor);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(a) failed. Err=" + hr.ToString());
                    }
                    if (audioStreamDescriptor == null)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to sourcePresentationDescriptor.GetStreamDescriptorByIndex(a) failed. audioStreamDescriptor == null");
                    }
                    // if the stream is selected, leave now we will release the audioStream descriptor later
                    if (streamIsSelected == true) break;

                    // release the one we are not using
                    if (audioStreamDescriptor != null)
                    {
                        Marshal.ReleaseComObject(audioStreamDescriptor);
                        audioStreamDescriptor = null;
                    }
                }

                // by the time we get here we should have a audio and video StreamDescriptors if
                // we do not, then we cannot proceed. Of course the MP4 file could have only
                // audio or video. However, I don't want to code around those special cases here
                // we assume the file has both audio and video streams.
                if (videoStreamDescriptor == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. videoStreamDescriptor == null");
                }
                if (audioStreamDescriptor == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to sourcePresentationDescriptor.GetStreamDescriptorByIndex failed. audioStreamDescriptor == null");
                }

                // ####
                // #### we now create the media sink, we need the types from the stream to do 
                // #### this which is why we wait until now to set it up
                // ####

                currentVideoMediaType = TantaWMFUtils.GetCurrentMediaTypeFromStreamDescriptor(videoStreamDescriptor);
                if (currentVideoMediaType == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to currentVideoMediaType == null");
                }
                currentAudioMediaType = TantaWMFUtils.GetCurrentMediaTypeFromStreamDescriptor(audioStreamDescriptor);
                if (currentAudioMediaType == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to currentAudioMediaType == null");
                }

                // ####
                // #### we now make up a topology branch for the video stream
                // ####

                // Create a source Video node for this stream.
                sourceVideoNode = TantaWMFUtils.CreateSourceNodeForStream(mediaSource, sourcePresentationDescriptor, videoStreamDescriptor);
                if (sourceVideoNode == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to CreateSourceNodeForStream(v) failed. sourceAudioNode == null");
                }

                // Create a source Audio node for this stream.
                sourceAudioNode = TantaWMFUtils.CreateSourceNodeForStream(mediaSource, sourcePresentationDescriptor, audioStreamDescriptor);
                if (sourceAudioNode == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to CreateSourceNodeForStream(a) failed. sourceAudioNode == null");
                }

                // Create the output node for the video renderer. Note we supply the handle of of a panel control 
                // to this call so it can be given to the video streams we are configuring. This displays
                // the video on the panels surface.
                outputSinkNodeVideo = TantaWMFUtils.CreateEVRRendererOutputNodeForStream(this.panelDisplayPanel.Handle);
                if (outputSinkNodeVideo == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to CreateOutputNodeForStream(v) failed. outputSinkNodeVideo == null");
                }

                // Create the output node for the audio renderer. 
                outputSinkNodeAudio = TantaWMFUtils.CreateSARRendererOutputNodeForStream();
                if (outputSinkNodeAudio == null)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to CreateOutputNodeForStream(a) failed. outputSinkNodeAudio == null");
                }

                // Add the nodes to the topology. First the source nodes
                hr = pTopology.AddNode(sourceVideoNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to pTopology.AddNode(sourceAudioNode) failed. Err=" + hr.ToString());
                }
                hr = pTopology.AddNode(sourceAudioNode);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to pTopology.AddNode(sourceAudioNode) failed. Err=" + hr.ToString());
                }

                // add the output Nodes
                hr = pTopology.AddNode(outputSinkNodeVideo);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to pTopology.AddNode(outputSinkNodeVideo) failed. Err=" + hr.ToString());
                }
                hr = pTopology.AddNode(outputSinkNodeAudio);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to pTopology.AddNode(outputSinkNodeAudio) failed. Err=" + hr.ToString());
                }

                // do we have a video transform, and is our stream of the video type? 
                // Because this is a demo version we can either inject these transforms 
                // into the video branch if we are given a binary object or if we are 
                // given the guid of an already registered transform in a dll. We 
                // use the binary in preference to the guid if we have both (we shouldn't)
                if (VideoTransform != null)
                {
                    // we do have an MFT transform object. Insert it into the topology between the source and output
                    IMFTopologyNode tmpTransformNode = null;

                    // Create the  transform node.
                    hr = MFExtern.MFCreateTopologyNode(MFTopologyType.TransformNode, out tmpTransformNode);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to MFExtern.MFCreateTopologyNode failed. Err=" + hr.ToString());
                    }

                    // set the transform object (it is an IMFTransform) as an object on the transform node. Since it is already there
                    // the topology does not need a GUID or activator to create it
                    hr = tmpTransformNode.SetObject(VideoTransform);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to tmpTransformNode.SetObject failed. Err=" + hr.ToString());
                    }

                    // Add the transform node to the topology.
                    hr = pTopology.AddNode(tmpTransformNode);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to pTopology.AddNode failed. Err=" + hr.ToString());
                    }
                    // also save the node here, it will get released later
                    VideoTransformNode = tmpTransformNode;

                    // Connect the source node to the transform node.
                    hr = sourceVideoNode.ConnectOutput(0, tmpTransformNode, 0);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to pSourceNode.ConnectOutput(b) failed. Err=" + hr.ToString());
                    }

                    // Connect the transform node to the output node.
                    hr = tmpTransformNode.ConnectOutput(0, outputSinkNodeVideo, 0);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to tmpTransformNode.ConnectOutput(b) failed. Err=" + hr.ToString());
                    }
                }
                else if (VideoTransformGuid != Guid.Empty)
                {
                    // yes we do, add the transform GUID 
                    // we do have an MFT transform guid. Insert it into the topology between the source and output
                    // the transform object will get properly added when the Topology is resolved
                    IMFTopologyNode tmpTransformNode = null;

                    // Create the  transform node.
                    hr = MFExtern.MFCreateTopologyNode(MFTopologyType.TransformNode, out tmpTransformNode);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to MFExtern.MFCreateTopologyNode failed. Err=" + hr.ToString());
                    }

                    // set the transform Guid on the transform node. Since this is an attribute we also 
                    // have to tell it what the guid means - hence the MF_TOPONODE_TRANSFORM_OBJECTID as a key
                    hr = tmpTransformNode.SetGUID(MFAttributesClsid.MF_TOPONODE_TRANSFORM_OBJECTID, VideoTransformGuid);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to tmpTransformNode.SetGUID failed. Err=" + hr.ToString());
                    }

                    // Add the transform node to the topology.
                    hr = pTopology.AddNode(tmpTransformNode);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to pTopology.AddNode failed. Err=" + hr.ToString());
                    }
                    // also save the node here, it will get released later
                    VideoTransformNode = tmpTransformNode;

                    // Connect the source node to the transform node.
                    hr = sourceVideoNode.ConnectOutput(0, tmpTransformNode, 0);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to pSourceNode.ConnectOutput(b) failed. Err=" + hr.ToString());
                    }

                    // Connect the transform node to the output node.
                    hr = tmpTransformNode.ConnectOutput(0, outputSinkNodeVideo, 0);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to tmpTransformNode.ConnectOutput(b) failed. Err=" + hr.ToString());
                    }
                }
                else
                {
                    // Note even though the streamID from the source may be non zero it the output index of this node
                    // is still 0 since that is the only stream we have configured on it. 
                    hr = sourceVideoNode.ConnectOutput(0, outputSinkNodeVideo, 0);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to  sourceVideoNode.ConnectOutput failed. Err=" + hr.ToString());
                    }
                }

                // the audio node just connects up source to sink. We do not
                // support audio transforms here (although we could).
                hr = sourceAudioNode.ConnectOutput(0, outputSinkNodeAudio, 0);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay call to  sourceAudioNode.ConnectOutput failed. Err=" + hr.ToString());
                }

                // Set the topology on the media session.
                // If SetTopology succeeds, the media session will queue an
                // MESessionTopologySet event.
                hr = mediaSession.SetTopology(0, pTopology);
                if (hr != HResult.S_OK)
                {
                    // we failed
                    throw new Exception("OpenVideoFileAndPrepareSessionAndPlay mediaSession.SetTopology failed, retVal=" + hr.ToString());
                }

                // Release the topology
                if (pTopology != null)
                {
                    Marshal.ReleaseComObject(pTopology);
                }

                // get the duration from the presentation descriptor now. This is nothing to do 
                // with the creation of the topology. We will eventually need this so
                // we can tell the user how long the video is. We have to get it from the
                // presentation descriptor and so might as well just get it here
                VideoDuration = TantaWMFUtils.GetDurationFromPresentationDescriptor(sourcePresentationDescriptor);

            }
            catch (Exception ex)
            {
                LogMessage("OpenVideoFileAndPrepareSessionAndPlay Error: " + ex.Message);
                OISMessageBox(ex.Message);
            }
            finally
            {
                // Clean up
                if (sourceResolverObj != null)
                {
                    Marshal.ReleaseComObject(sourceResolverObj);
                }
                if (sourcePresentationDescriptor != null)
                {
                    Marshal.ReleaseComObject(sourcePresentationDescriptor);
                }
                if (audioStreamDescriptor != null)
                {
                    Marshal.ReleaseComObject(audioStreamDescriptor);
                }
                if (videoStreamDescriptor != null)
                {
                    Marshal.ReleaseComObject(videoStreamDescriptor);
                }
                if (sourceAudioNode != null)
                {
                    Marshal.ReleaseComObject(sourceAudioNode);
                }
                if (sourceVideoNode != null)
                {
                    Marshal.ReleaseComObject(sourceVideoNode);
                }
                if (outputSinkNodeVideo != null)
                {
                    Marshal.ReleaseComObject(outputSinkNodeVideo);
                }
                if (outputSinkNodeAudio != null)
                {
                    Marshal.ReleaseComObject(outputSinkNodeAudio);
                }
                if (currentAudioMediaType != null)
                {
                    Marshal.ReleaseComObject(currentAudioMediaType);
                }
                if (currentVideoMediaType != null)
                {
                    Marshal.ReleaseComObject(currentVideoMediaType);
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles presses on the Play button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonPlay_Click(object sender, EventArgs e)
        {
            LogMessage("buttonPlay_Click called");
            OpenVideoFileAndPrepareSessionAndPlay();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles presses on the Pause button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonPause_Click(object sender, EventArgs e)
        {
            HResult hr;
            LogMessage("buttonPause_Click called");

            if (mediaSession == null) return;
            if (mediaSource == null) return;

            try
            {
                if (PlayerState == TantaEVRPlayerStateEnum.Paused)
                {
                    // we are already paused - we restart
                    hr = mediaSession.Start(Guid.Empty, new PropVariant());
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("buttonPause_Click call to mediaSession.Start failed. Err=" + hr.ToString());
                    }
                }
                else
                {
                    hr = mediaSession.Pause();
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("buttonPause_Click call to  mediaSession.Pause() failed. Err=" + hr.ToString());
                    }
                    PlayerState = TantaEVRPlayerStateEnum.PausePending;
                }

                this.SyncScreenStateToVideoFileAndPlayingState();
                // reset this flag
                rateIsChanging = false;
            }
            catch (Exception ex)
            {
                LogMessage("buttonPause_Click Exception ex="+ex.Message);
                if (TantaEVRFilePlayerErrorEvent != null) TantaEVRFilePlayerErrorEvent(this, ex.Message, ex);
            }

            return;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles presses on the Stop button
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonStop_Click(object sender, EventArgs e)
        {
            LogMessage("buttonStop_Click called");
            HResult hr;

            if (mediaSession == null) return;
            if (mediaSource == null) return;

            try
            {
                hr = mediaSession.Stop();
                if (hr != HResult.S_OK)
                {
                    throw new Exception("buttonStop_Click call to  mediaSession.Stop() failed. Err=" + hr.ToString());
                }
                // make sure we unmute
                TantaWMFUtils.SetAudioMuteStateOnSession(mediaSession, false);
                // make sure we reset the volume
                TantaWMFUtils.SetAudioVolumeOnSession(mediaSession, 1.0f);

                PlayerState = TantaEVRPlayerStateEnum.Ready;
                VideoDuration = 0;
                // reset this flag
                rateIsChanging = false;
                this.SyncScreenStateToVideoFileAndPlayingState();
            }
            catch (Exception ex)
            {
                LogMessage("buttonStop_Click Exception ex=" + ex.Message);
                if (TantaEVRFilePlayerErrorEvent != null) TantaEVRFilePlayerErrorEvent(this, ex.Message, ex);
            }

            return;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A button to seek 5 seconds forward from the current position
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void button5SecPlus_Click(object sender, EventArgs e)
        {
            LogMessage("button5SecPlus_Click called");

            // we only permit this action if we are started or paused
            if (((PlayerState == TantaEVRPlayerStateEnum.Started) || (PlayerState == TantaEVRPlayerStateEnum.Paused))==false)
            {
                LogMessage("button5SecPlus_Click, PlayerState != TantaEVRPlayerStateEnum.Started quitting now");
                return;
            }

            // IMPORTANT NOTE: the technique below works well for a simple button press. If you
            // are sending a flood of seek actions (say you are reacting to the user manipulating
            // a scroll bar) then you need to make sure you do not overwhelm the session
            // with seek commands. Look online under "IMFSession scrubbing" for more details

            // In order to seek forward 5sec really all we do here is add 
            // 5 Sec (in 100ns chunks) to the current presentation clock
            // and call the session start function again

            // get the current time
            UInt64 presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
            // calc the new time, this call checks so as not to make the new time go out of bounds
            presentationTime = TantaWMFUtils.AddSecondsTo100nsTime(5, presentationTime, VideoDuration);

            // perform the seek, note we have to convert the new presentation time to an Int64
            // this is the way that WMF uses it even though it will never go negative.
            HResult hr = mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
            if (hr != HResult.S_OK)
            {
                throw new Exception("button5SecPlus_Click call to mediaSession.Start failed. Err=" + hr.ToString());
            }
            // were we paused? Make sure we stay paused
            if(PlayerState == TantaEVRPlayerStateEnum.Paused)
            {
                hr = mediaSession.Pause();
                if (hr != HResult.S_OK)
                {
                    throw new Exception("button5SecPlus_Click call to  mediaSession.Pause() failed. Err=" + hr.ToString());
                }
                PlayerState = TantaEVRPlayerStateEnum.PausePending;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A button to seek 5 seconds backward from the current position
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void button5SecMinus_Click(object sender, EventArgs e)
        {
            LogMessage("button5SecMinus_Click called");

            // we only permit this action if we are started or paused
            if (((PlayerState == TantaEVRPlayerStateEnum.Started) || (PlayerState == TantaEVRPlayerStateEnum.Paused)) == false)
            {
                LogMessage("button5SecMinus_Click, PlayerState != TantaEVRPlayerStateEnum.Started quitting now");
                return;
            }

            // IMPORTANT NOTE: the technique below works well for a simple button press. If you
            // are sending a flood of seek actions (say you are reacting to the user manipulating
            // a scroll bar) then you need to make sure you do not overwhelm the session
            // with seek commands. Look online under "IMFSession scrubbing" for more details

            // In order to seek backward 5sec really all we do here is subtract 
            // 5 Sec (in 100ns chunks) from the current presentation clock
            // and call the session start function again

            // get the current time
            UInt64 presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
            // calc the new time, this call checks so as not to make the new time go out of bounds
            presentationTime = TantaWMFUtils.AddSecondsTo100nsTime(-5, presentationTime, VideoDuration);

            // perform the seek, note we have to convert the new presentation time to an Int64
            // this is the way that WMF uses it even though it will never go negative.
            HResult hr = mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
            if (hr != HResult.S_OK)
            {
                throw new Exception("button5SecMinus_Click call to mediaSession.Start failed. Err=" + hr.ToString());
            }
            // were we paused? Make sure we stay paused
            if (PlayerState == TantaEVRPlayerStateEnum.Paused)
            {
                hr = mediaSession.Pause();
                if (hr != HResult.S_OK)
                {
                    throw new Exception("button5SecMinus_Click call to  mediaSession.Pause() failed. Err=" + hr.ToString());
                }
                PlayerState = TantaEVRPlayerStateEnum.PausePending;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A button to fast forward, not all speeds are supported
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonFastForward_Click(object sender, EventArgs e)
        {
            IMFRateSupport rateSupportService = null;
            bool retBool;

            LogMessage("buttonFastForward_Click called");

            try
            {
                // if the rate is currently changing we do not do this again
                // the current rate change has to finish before we permit this
                if (rateIsChanging == true) return;

                UInt64 presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
                // there are various transitions that are permitted and various ones that are not
                // since, in this implementation, we never have a rewind playback rate (a negative one)
                // we can just use pause. To transition from negative to zero or positive to negative
                // we would have to stop the media session.
                mediaSession.Pause();

                float rateIncrement = DEFAULT_PLAYBACKRATE_INCREMENT;
                float newRate;
                retBool = TantaWMFUtils.IncrementPlaybackRateOnSession(mediaSession, rateIncrement, out newRate);
                if (retBool != true)
                {
                    LogMessage("IncrementPlaybackRateOnSession declined.");
                    // start it back up anyways
                    mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
                    PlayerState = TantaEVRPlayerStateEnum.StartPending;
                    return;
                }
                // flag this, it gets reset later by an event
                rateIsChanging = true;

                // check the new rate
                if (newRate != 1)
                {
                    // at any rate other than 1 we mute
                    retBool = TantaWMFUtils.SetAudioMuteStateOnSession(mediaSession, true);
                }
                else
                {
                    // unmute
                    retBool = TantaWMFUtils.SetAudioMuteStateOnSession(mediaSession, false);
                }
                // start the session back up
                mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
                PlayerState = TantaEVRPlayerStateEnum.StartPending;
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
        /// A button to rewind, note this slows the speed down icrementally and will
        /// reverse if that is supported
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonRewind_Click(object sender, EventArgs e)
        {
            IMFRateSupport rateSupportService = null;
            bool retBool;

            LogMessage("buttonFastForward_Click called");

            try
            {
                // if the rate is currently changing we do not do this again
                // the current rate change has to finish before we permit this
                if (rateIsChanging == true) return;

                UInt64 presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
                // there are various transitions that are permitted and various ones that are not
                // since, in this implementation, we never have a rewind playback rate (a negative one)
                // we can just use pause. To transition from negative to zero or positive to negative
                // we would have to stop the media session.
                mediaSession.Pause();

                // use a negative increment here
                float rateIncrement = (DEFAULT_PLAYBACKRATE_INCREMENT*-1);
                float newRate = 0;
                retBool = TantaWMFUtils.IncrementPlaybackRateOnSession(mediaSession, rateIncrement, out newRate);
                if (retBool != true)
                {
                    LogMessage("IncrementPlaybackRateOnSession declined.");
                    // start it back up anyways
                    mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
                    PlayerState = TantaEVRPlayerStateEnum.StartPending;
                    return;
                }
                // flag this, it gets reset later by an event
                rateIsChanging = true;

                // check the new rate
                if (newRate != 1)
                {
                    // at any rate other than 1 we mute
                    retBool = TantaWMFUtils.SetAudioMuteStateOnSession(mediaSession, true);
                }
                else
                {
                    // unmute
                    retBool = TantaWMFUtils.SetAudioMuteStateOnSession(mediaSession, false);
                }
                // start the session back up
                mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
                PlayerState = TantaEVRPlayerStateEnum.StartPending;
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
        /// A button to toggle mute on and off
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonMute_Click(object sender, EventArgs e)
        {
            // this does it all.
            TantaWMFUtils.ToggleAudioMuteStateOnSession(mediaSession);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A button to increment the volume down
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonVolumeDown_Click(object sender, EventArgs e)
        {
            // increment the volume down
            bool retBool = TantaWMFUtils.IncrementAudioVolumeOnSession(mediaSession, (-1.0f * DEFAULT_AUDIOVOLUME_INCREMENT));
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// A button to increment the volume down
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonVolumeUp_Click(object sender, EventArgs e)
        {
            // increment the volume up
            bool retBool = TantaWMFUtils.IncrementAudioVolumeOnSession(mediaSession, DEFAULT_AUDIOVOLUME_INCREMENT);
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
                // we put the snapshots in the video directory by default
                if ((VideoFileAndPathToPlay != null) && (VideoFileAndPathToPlay.Length != 0))
                {
                    return Path.GetDirectoryName(VideoFileAndPathToPlay);
                }
                // just return the my documents folder
                return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
             }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the current video file we wish to play. Will never get/set
        /// NULL
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public string VideoFileAndPathToPlay
        {
            get
            {
                if (videoFileAndPathToPlay == null) videoFileAndPathToPlay = "";
                return videoFileAndPathToPlay;
            }
            set
            {
                videoFileAndPathToPlay = value;
                if (videoFileAndPathToPlay == null) videoFileAndPathToPlay = "";
                SyncScreenStateToVideoFileAndPlayingState();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This is called periodically by external sources to force this control
        /// to update it screen.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public void UpdateScreen()
        {
            // make sure we are on the controls thread
            if (this.InvokeRequired == true)
            {
                Action d = new Action(UpdateScreen);
                this.Invoke(d, new object[] { });
                return;
            }

            // set our on-screen times
            SyncDurationValueOnScreenToReality();
            SetCurrentTimeValue();
            SetScrollBarToPresentationTime();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This updates the current duration value on the screen to reality
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SetCurrentTimeValue()
        {
            lock (this)
            {
                if (mediaSession != null)
                {
                    UInt64 presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
                    string currentTime = TantaWMFUtils.ConvertPresentationClockToHHMMSSTime(presentationTime);
                    bool isThinned;
                    float currentRate;
                    TantaWMFUtils.GetCurrentPlaybackRateFromSession(mediaSession, out isThinned, out currentRate);
                    labelCurrentTime.Text = currentTime + " (" + currentRate.ToString() + "x)";
                }
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// This updates the current duration value on the screen to reality
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SyncDurationValueOnScreenToReality()
        {
            labelDuration.Text = TantaWMFUtils.ConvertPresentationClockToHHMMSSTime(VideoDuration);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Syncs the onscreen state to the video file and playing state
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SyncScreenStateToVideoFileAndPlayingState()
        {
            // we need an invoke in here because this can be called
            // from other threads and we do NOT want to mess with 
            // the controls from anything but the form thread

            if (this.InvokeRequired == true)
            {
                Action d = new Action(SyncScreenStateToVideoFileAndPlayingState);
                this.Invoke(d, new object[] { });
                return;
            }
       
            // if we have no file then everything is off
            if (File.Exists(VideoFileAndPathToPlay) == false)
            {
                SetButtonEnabledState(false);
                return;
            }

            switch (PlayerState)
            {
                case TantaEVRPlayerStateEnum.Paused:
                case TantaEVRPlayerStateEnum.PausePending:
                case TantaEVRPlayerStateEnum.OpenPending:
                case TantaEVRPlayerStateEnum.StartPending:
                case TantaEVRPlayerStateEnum.Started:
                    SetButtonEnabledState(true);
                    buttonPlay.Enabled = false;
                    return;
                case TantaEVRPlayerStateEnum.Ready:
                    SetButtonEnabledState(false);
                    buttonPlay.Enabled = true;
                    return;
                default:
                    SetButtonEnabledState(true);
                    return;
            } 
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets all of the buttons to a specific state.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SetButtonEnabledState(bool enabledState)
        {
            buttonPlay.Enabled = enabledState;
            buttonPause.Enabled = enabledState;
            buttonStop.Enabled = enabledState;
            button5SecPlus.Enabled = enabledState;
            button5SecMinus.Enabled = enabledState;
            buttonFastForward.Enabled = enabledState;
            buttonRewind.Enabled = enabledState;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the current state of the player, will also send notices
        /// if the player state has changed
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public TantaEVRPlayerStateEnum PlayerState
        {
            get
            {
                return playerState;
            }
            set
            {
                // record the existing version
                TantaEVRPlayerStateEnum previousState = playerState;
                playerState = value;
                // send the event if changed
                if(previousState != playerState) NotifyPlayerStateChanged();
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the current video duration in 100ns units
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public UInt64 VideoDuration
        {
            get
            {
                return videoDuration;
            }
            set
            {
                videoDuration = value;
            }
        }
  
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles events reported by the media session TantaAsyncCallbackHandler 
        /// </summary>
        /// <param name="sender">the object sending the event</param>
        /// <param name="mediaEvent">the event generated by the media session. Do NOT release this here.</param>
        /// <param name="mediaEventType">the eventType, this is just an enum</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void HandleMediaSessionAsyncCallBackEvent(object sender, IMFMediaEvent pEvent, MediaEventType mediaEventType)
        {
           // LogMessage("Media Event Type " + mediaEventType.ToString());

            switch (mediaEventType)
            {
                case MediaEventType.MESessionTopologyStatus:
                    // Raised by the Media Session when the status of a topology changes. 
                    // Get the topology changed status code. This is an enum in the event
                    int i;
                    HResult hr = pEvent.GetUINT32(MFAttributesClsid.MF_EVENT_TOPOLOGY_STATUS, out i);
                    if (hr != HResult.S_OK)
                    {
                        throw new Exception("HandleMediaSessionAsyncCallBackEvent call to pEvent to get the status code failed. Err=" + hr.ToString());
                    }
                    // the one we are most interested in is i == MFTopoStatus.Ready
                    // which we get then the Topology is built and ready to run
                    HandleTopologyStatusChanged(pEvent, mediaEventType, (MFTopoStatus)i);
                    break;

                case MediaEventType.MESessionStarted:
                    // Raised when the IMFMediaSession::Start method completes asynchronously. 
                    PlayerState = TantaEVRPlayerStateEnum.Started;
                    break;

                case MediaEventType.MESessionPaused:
                    // Raised when the IMFMediaSession::Pause method completes asynchronously. 
                    PlayerState = TantaEVRPlayerStateEnum.Paused;
                    break;

                case MediaEventType.MESessionStopped:
                    // Raised when the IMFMediaSession::Stop method completes asynchronously.
                    break;

                case MediaEventType.MESessionClosed:
                    // Raised when the IMFMediaSession::Close method completes asynchronously. 
                    break;

                case MediaEventType.MESessionCapabilitiesChanged:
                    // Raised by the Media Session when the session capabilities change.
                    // You can use IMFMediaEvent::GetValue to figure out what they are
                    break;

                case MediaEventType.MESessionTopologySet:
                    // Raised after the IMFMediaSession::SetTopology method completes asynchronously. 
                    // The Media Session raises this event after it resolves the topology into a full topology and queues the topology for playback. 
                    break;

                case MediaEventType.MESessionNotifyPresentationTime:
                    // Raised by the Media Session when a new presentation starts. 
                    // This event indicates when the presentation will start and the offset between the presentation time and the source time.      
                    break;
                    
                case MediaEventType.MEEndOfPresentation:
                    // Raised by a media source when a presentation ends. This event signals that all streams 
                    // in the presentation are complete. The Media Session forwards this event to the application.
                    CloseAllMediaDevices();
                    break;

                case MediaEventType.MESessionRateChanged:
                    // Raised by the Media Session when the playback rate changes. This event is sent after the 
                    // IMFRateControl::SetRate method completes asynchronously. 
                    
                    // reset this flag, it prevents us hitting the EVR with too many rate changes too fast
                    rateIsChanging = false;
                    break;

                default:
                    LogMessage("Unhandled Media Event Type " + mediaEventType.ToString());
                    break;
            }
            // sync the screen buttons
            SyncScreenStateToVideoFileAndPlayingState();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles topology status changes reported by the media session TantaAsyncCallbackHandler 
        /// </summary>
        /// <param name="sender">the object sending the event</param>
        /// <param name="mediaEvent">the event generated by the media session. Do NOT release this here.</param>
        /// <param name="mediaEventType">the eventType</param>
        /// <param name="topoStatus">the topology status flag</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void HandleTopologyStatusChanged(IMFMediaEvent mediaEvent, MediaEventType mediaEventType, MFTopoStatus topoStatus)
        {
            LogMessage("HandleTopologyStatusChanged event type: " + mediaEventType.ToString() + ", topoStatus=" + topoStatus.ToString());

            if (topoStatus == MFTopoStatus.Ready)
            {
                MediaSessionTopologyNowReady(mediaEvent);
            }
            else
            {
                // we are not interested in any other status changes
                return;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Called when the topology status changes to ready. This status change
        /// is generally signaled by the media session when it is fully configured.
        /// </summary>
        /// <param name="sender">the object sending the event</param>
        /// <param name="mediaEvent">the event generated by the media session. Do NOT release this here.</param>
        /// <param name="mediaEventType">the eventType</param>
        /// <param name="topoStatus">the topology status flag</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void MediaSessionTopologyNowReady(IMFMediaEvent mediaEvent)
        {
            HResult hr;
            object evrVideoService;

            LogMessage("MediaSessionTopologyNowReady");

            // Ask for the IMFVideoDisplayControl interface. This interface is implemented by the EVR and is
            // exposed by the media session as a service.

            // Some interfaces in Media Foundation must be obtained by calling IMFGetService::GetService instead 
            // of by calling QueryInterface.The GetService method works like QueryInterface, but with the following differences:

            // It takes a service identifier GUID in addition to the interface identifier.
            // It can return a pointer to another object that implements the interface, instead of 
            // returning a pointer to the original object that is queried.

            // In some cases, an interface is returned as a service by one class of objects, and returned 
            // through QueryInterface by another class of objects. The reference pages for each interface 
            // indicate when to use GetService and when to use QueryInterface.

            // Note: This call is expected to fail if the source does not have video.

            try
            {
                // we need to get the active IMFVideoDisplayControl. The EVR presenter implements this interface
                // and it controls how the Enhanced Video Renderer (EVR) displays video.
                hr = MFExtern.MFGetService(
                    mediaSession,
                    MFServices.MR_VIDEO_RENDER_SERVICE,
                    typeof(IMFVideoDisplayControl).GUID,
                    out evrVideoService
                    );
                if (hr != HResult.S_OK)
                {
                    throw new Exception("MediaSessionTopologyNowReady call to MFExtern.MFGetService failed. Err=" + hr.ToString());
                }
                if (evrVideoService == null)
                {
                    throw new Exception("MediaSessionTopologyNowReady call to MFExtern.MFGetService failed. evrVideoService == null");
                }

                // set the video display now for later use
                evrVideoDisplay = evrVideoService as IMFVideoDisplayControl;
            }
            catch (InvalidCastException ex)
            {
                evrVideoDisplay = null;
                NotifyPlayerErrored(ex.Message, ex);
            }

            try
            {
                StartFilePlay();
            }
            catch (Exception ex)
            {
                NotifyPlayerErrored(ex.Message, ex);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Starts the play of the video
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void StartFilePlay()
        {
            LogMessage("StartFilePlay called");

            if(mediaSession == null)
            {
                LogMessage("StartFilePlay Failed.  mediaSession == null");
                return;
            }

            // the aspect ratio can be changed by uncommenting either of these lines
            // evrVideoDisplay.SetAspectRatioMode(MFVideoAspectRatioMode.None);
            // evrVideoDisplay.SetAspectRatioMode(MFVideoAspectRatioMode.PreservePicture);

            HResult hr = mediaSession.Start(Guid.Empty, new PropVariant());
            if (hr != HResult.S_OK)
            {
                throw new Exception("StartFilePlay call to mediaSession.Start failed. Err=" + hr.ToString());
            }
            PlayerState = TantaEVRPlayerStateEnum.StartPending;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handles errors reported by the media session TantaAsyncCallbackHandler 
        /// </summary>
        /// <param name="sender">the object sending the event</param>
        /// <param name="errMsg">the error message</param>
        /// <param name="ex">the exception. Can be null</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void HandleMediaSessionAsyncCallBackErrors(object sender, string errMsg, Exception ex)
        {
            if (errMsg == null) errMsg = "unknown error";
            LogMessage("HandleMediaSessionAsyncCallBackErrors Error" + errMsg);
            if (ex != null)
            {
                LogMessage("HandleMediaSessionAsyncCallBackErrors Exception trace = " + ex.StackTrace);
            }
            OISMessageBox("The media session reported an error\n\nPlease see the logfile.");
            // do everything to close all media devices
            CloseAllMediaDevices();
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Performs notifications if the PlayerState changes
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void NotifyPlayerStateChanged()
        {
            if (TantaEVRFilePlayerStateChangedEvent != null) TantaEVRFilePlayerStateChangedEvent(this, PlayerState);
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Performs notifications if there are errors
        /// </summary>
        /// <param name="errMsg">the error message</param>
        /// <param name="ex">the exception. Can be null</param>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void NotifyPlayerErrored(string errMsg, Exception ex)
        {
            if (TantaEVRFilePlayerErrorEvent != null) TantaEVRFilePlayerErrorEvent(this, errMsg, ex);
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
        private void ctlTantaEVRFilePlayer_SizeChanged(object sender, EventArgs e)
        {
            LogMessage("ctlTantaEVRFilePlayer_SizeChanged");

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
                    throw new Exception("ctlTantaEVRFilePlayer_SizeChanged failed. Err=" + hr.ToString());
                }

            }
            catch (Exception ex)
            {
                LogMessage("ctlTantaEVRFilePlayer_SizeChanged failed exception happened. ex=" + ex.Message);
                NotifyPlayerErrored(ex.Message, ex);
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sync's the settings on the horizontal scroll bar to the video file duration
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SetupVideoPositionScrollBar()
        {
            // the thumb itself has width, this is the LargeChange value. If we set it
            // up like this we get 0 to 1000 as the user drags it over the range of the
            // scrollbar
            scrollBarVideoPosition.Maximum = TantaWMFUtils.MAX_DURATION_RANGE + scrollBarVideoPosition.LargeChange-1;
            scrollBarVideoPosition.Minimum = 0;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Sets the current scrollbar position to the current video presentation time
        /// </summary>
        /// <returns>the presentation time, or -1 for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void SetScrollBarToPresentationTime()
        {
            if (mediaSession == null) return;

            // we only permit this action if we are started or paused
            if (((PlayerState == TantaEVRPlayerStateEnum.Started) || (PlayerState == TantaEVRPlayerStateEnum.Paused)) == false)
            {
                return;
            }

            try
            {
                UInt64 presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
                int scrollBarValue = TantaWMFUtils.ConvertVideoPostionToRangeValue(VideoDuration, presentationTime);
                this.scrollBarVideoPosition.Value = scrollBarValue;
//                LogMessage("SetScrollBarToPresentationTime scrollBarValue=" + scrollBarValue.ToString() + ", presentationTime="+ presentationTime.ToString());
            }
            catch { }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle a scroll bar changed 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void scrollBarVideoPosition_Scroll(object sender, ScrollEventArgs e)
        {
            HResult hr;
            UInt64 deltaTime = 0;
            UInt64 presentationTime = 0;

            // sanity check
            if (e == null) return;
            
            // for diagnostics
            //LogMessage("scrollBarVideoPosition_Scroll NewValue=" + e.NewValue + ", Type=" + e.Type.ToString());

            // we only permit this action if we are started or paused, We do NOT want to flood the 
            // session with seek requests. The act of seeking will set PlayerState to something else and it 
            // will be reset in the media sessions call back handler.
            if (((PlayerState == TantaEVRPlayerStateEnum.Started) || (PlayerState == TantaEVRPlayerStateEnum.Paused)) == false)
            {
                return;
            }

            // NOTE: the scroll bar is configured to have a MinimumValue of 0 and a Maximum Value such that when the Thumb is 
            //       at the right hand edge a value of 1000 is returned. This, when used with the VideoDuration value, makes
            //       it easy to convert between the scroll bar thumb position and a presentation time.

            // NOTE: in the code below we use a constant to derive a delta time which is then added to the currentPresentation 
            //       time. One might be tempted to just use the incoming NewValue of the thumb position to derive a presentation
            //       time. This does not work well if the video is playing. In between the time the mouse is clicked and this 
            //       event is processed the processing time may well have moved on and the Thumb appears to jump about. Adding
            //       a constant and deriving our own presentation time works much better

            switch(e.Type)
            {
                case ScrollEventType.EndScroll:
                    // all scrolling actions have ended
                    return;

                case ScrollEventType.LargeIncrement:
                    // the user clicked in the scroll bar to the right of the Thumb
                    // convert a value in the range of 0 – 1000 to a video position. In this
                    // case it is a delta offset
                    deltaTime = TantaWMFUtils.ConvertRangeValueToVideoPosition(VideoDuration, TantaWMFUtils.DEFAULT_LARGE_INCREMENT_FOR_DURATIONRANGE);
                    // get the current presentation time
                    presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
                    // calc a new time
                    presentationTime += deltaTime;
                    if (presentationTime > VideoDuration) presentationTime = VideoDuration;
                    // start the session with the new time
                    hr = mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
                    // flag this it will inhibit future calls until the session has completely started
                    PlayerState = TantaEVRPlayerStateEnum.StartPending;
                    return;

                case ScrollEventType.SmallIncrement:
                    // the user clicked on the arrow to the right of the Thumb
                    // convert a value in the range of 0 – 1000 to a video position. In this
                    // case it is a delta offset
                    deltaTime = TantaWMFUtils.ConvertRangeValueToVideoPosition(VideoDuration, TantaWMFUtils.DEFAULT_SMALL_INCREMENT_FOR_DURATIONRANGE);
                    // get the current presentation time
                    presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
                    // calc a new time
                    presentationTime += deltaTime;
                    if (presentationTime > VideoDuration) presentationTime = VideoDuration;
                    // start the session with the new time
                    hr = mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
                    PlayerState = TantaEVRPlayerStateEnum.StartPending;
                    return;

                case ScrollEventType.LargeDecrement:
                    // the user clicked in the scroll bar to the left of the Thumb
                    // convert a value in the range of 0 – 1000 to a video position. In this
                    // case it is a delta offset
                    deltaTime = TantaWMFUtils.ConvertRangeValueToVideoPosition(VideoDuration, TantaWMFUtils.DEFAULT_LARGE_INCREMENT_FOR_DURATIONRANGE);
                    // get the current presentation time
                    presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
                    // calc a new time
                    presentationTime -= deltaTime;
                    if (presentationTime > VideoDuration) presentationTime = 0;
                    // start the session with the new time
                    hr = mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
                    PlayerState = TantaEVRPlayerStateEnum.StartPending;
                    return;

                case ScrollEventType.SmallDecrement:
                    // the user clicked on the arrow to the left of the Thumb
                    // convert a value in the range of 0 – 1000 to a video position. In this
                    // case it is a delta offset
                    deltaTime = TantaWMFUtils.ConvertRangeValueToVideoPosition(VideoDuration, TantaWMFUtils.DEFAULT_SMALL_INCREMENT_FOR_DURATIONRANGE);
                    // get the current presentation time
                    presentationTime = TantaWMFUtils.GetPresentationTimeFromSession(mediaSession);
                    // calc a new time
                    presentationTime -= deltaTime;
                    if (presentationTime > VideoDuration) presentationTime = 0;
                    // start the session with the new time
                    hr = mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
                    PlayerState = TantaEVRPlayerStateEnum.StartPending;
                    return;

                case ScrollEventType.ThumbTrack:
                    // the user is dragging the Thumb control, use the new value here we do not know an offset
                    presentationTime = TantaWMFUtils.ConvertRangeValueToVideoPosition(VideoDuration, e.NewValue);
                    // start the session with the new time
                    hr = mediaSession.Start(Guid.Empty, new PropVariant((Int64)presentationTime));
                    PlayerState = TantaEVRPlayerStateEnum.StartPending;
                    return;

                default:
                    // just ignore
                    return;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Handle a press on the take snapshot button.
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        private void buttonTakeSnapShot_Click(object sender, EventArgs e)
        {
            HResult hr;
            BinaryWriter bitmapWriter = null;
            BitmapInfoHeader workingBitmapInfoHeader = new BitmapInfoHeader();
            IntPtr bitmapData = IntPtr.Zero;
            int bitmapDataSize = 0;
            long bitmapTimestamp = 0;

            // we have to be playing or paused
            if (((PlayerState == TantaEVRPlayerStateEnum.Started) || (PlayerState == TantaEVRPlayerStateEnum.Paused)) == false)
            {
                // just send a warning beep
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            try
            { 
                // set the size here. the docs briefly state you have to do this in a one 
                // liner towards the bottom. However, they REALLY mean it - nothing will 
                // work without this being done
                workingBitmapInfoHeader.Size = Marshal.SizeOf(typeof(BitmapInfoHeader));

                // get the image on the screen now. This will give us the image data and the
                // bitmap info header. However, be aware that there are two headers associated 
                // with every .bmp file. The first is  a file header (which we have to build 
                // ourselves) and  the second is an info header which we are given in the call below. 
                // Also note that the memory for the bitmapData variable we receive here needs to be freed
                hr = evrVideoDisplay.GetCurrentImage(workingBitmapInfoHeader, out bitmapData, out bitmapDataSize, out bitmapTimestamp);
                if (hr != HResult.S_OK)
                {
                    throw new Exception("buttonTakeSnapShot_Click failed. Err=" + hr.ToString());
                }

                // bitmapData is an IntPtr. Use Marshal to copy the video data out into a byte array
                // bitmapDataSize is the length of bitmapData
                byte[] managedArray = new byte[bitmapDataSize];
                Marshal.Copy(bitmapData, managedArray, 0, bitmapDataSize);

                // build the output filename. By default this is the directory of the playing video file
                string outputBitmapFile = Path.Combine(SnapshotDirectory, TantaWMFUtils.BuildFilenameWithTimeStamp(BITMAP_PREFIX, BITMAP_EXTENSION));

                // now we have to build and populate the bitmap fileheader. None of the 
                // documentation tells you that you have to this - but the bitmap file will
                // not be readable if you do not
                TantaBitMapFileHeader fileHeader = new TantaBitMapFileHeader();
                fileHeader.bfOffBits = (uint)(Marshal.SizeOf(fileHeader) + Marshal.SizeOf(workingBitmapInfoHeader));
                fileHeader.bfReserved1 = 0;
                fileHeader.bfReserved2 = 0;
                fileHeader.bfSize = (uint)(Marshal.SizeOf(fileHeader) + Marshal.SizeOf(workingBitmapInfoHeader) + bitmapDataSize);
                fileHeader.bfType = 0x4d42;

                // Create a binary writer to output the file. We will just populate the file
                // with the newly created fileheader, the info header we got from the 
                // GetCurrentImage call and the actual image data itself. We just write these
                // sequentially one after the other.
                bitmapWriter = new BinaryWriter(File.OpenWrite(outputBitmapFile));

                // convert the file header to a byte[]. This is surprisingly complex in C#
                byte[] fileBufferAsBytes = TantaWMFUtils.ConvertStructureToByteArray(fileHeader);
                // write the file header out to the new bitmap file
                bitmapWriter.Write(fileBufferAsBytes, 0, Marshal.SizeOf(fileHeader));

                // convert the info header to a byte[].
                byte[] infoBufferAsBytes = TantaWMFUtils.ConvertStructureToByteArray(workingBitmapInfoHeader);
                // write the info header out to the new bitmap file
                bitmapWriter.Write(infoBufferAsBytes, 0, workingBitmapInfoHeader.Size);

                // write the actual data of the bitmap
                bitmapWriter.Write(managedArray);

                // close up
                bitmapWriter.Flush();
                bitmapWriter.Close();
                bitmapWriter = null;

                // tell the user audibly that things went well
                System.Media.SystemSounds.Hand.Play();

            }
            catch
            {
                // we did not succeed
                System.Media.SystemSounds.Beep.Play();
            }
            finally
            {
                // clean up
                if(bitmapData != null)
                {
                    Marshal.FreeCoTaskMem(bitmapData);
                }
                if(bitmapWriter != null)
                {
                    bitmapWriter.Close();
                    bitmapWriter = null;
                }
            }  
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the current video transform object. Can be null
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IMFTransform VideoTransform
        {
            get
            {
                return videoTransform;
            }
            set
            {
                videoTransform = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the topology node for the current video transform. Can be null
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public IMFTopologyNode VideoTransformNode
        {
            get
            {
                return videoTransformNode;
            }
            set
            {
                videoTransformNode = value;
            }
        }
        
        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets/Sets the current video transform Guid. 
        /// </summary>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Guid VideoTransformGuid
        {
            get
            {
                return videoTransformGuid;
            }
            set
            {
                videoTransformGuid = value;
            }
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the attribute container of the current video transform. It 
        /// does not matter if the Transform was set via the Object or Guid method
        /// 
        /// NOTE: the caller MUST release the attribute container
        /// 
        /// </summary>
        /// <returns>the attribute collection or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public IMFAttributes GetTransformAttributes()
        {
            HResult hr;
            object transformObject;
            IMFAttributes attributeContainer = null;

            // this will get populated when we insert a 
            // transform of our choice into the video pipeline
            if (VideoTransformNode == null) return null;

            // get the transform object from the node
            hr = VideoTransformNode.GetObject(out transformObject);
            if (hr != HResult.S_OK) return null;
            if (transformObject == null) return null;
            if ((transformObject is IMFTransform)==false) return null;
      
            // get the attribute container from the transform. If you 
            // are using the Tanta base classes this attribute container
            // will be obtained in such a way so that it is safe for the
            // caller to release it.
            hr = (transformObject as IMFTransform).GetAttributes(out attributeContainer);
            if (hr != HResult.S_OK) return null;
            if (attributeContainer == null) return null;
            // return the attribute container
            return attributeContainer;
        }

        /// +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=
        /// <summary>
        /// Gets the current video transform object. It 
        /// does not matter if the Transform was set via the Object or Guid method
        /// 
        /// </summary>
        /// <returns>the transform or null for fail</returns>
        /// <history>
        ///    01 Nov 18  Cynic - Originally Written
        /// </history>
        public IMFTransform GetTransform()
        {
            HResult hr;
            object transformObject;

            // this will get populated when we insert a 
            // transform of our choice into the video pipeline
            if (VideoTransformNode == null) return null;

            // get the transform object from the node
            hr = VideoTransformNode.GetObject(out transformObject);
            if (hr != HResult.S_OK) return null;
            if (transformObject == null) return null;
            if ((transformObject is IMFTransform) == false) return null;
            return (transformObject as IMFTransform);
        }

    }
}

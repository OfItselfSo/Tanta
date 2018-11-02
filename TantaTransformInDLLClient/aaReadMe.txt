
####
#### The TantaTransformInDLLClient Project
####

This project is designed to work with the TantaTransformInDLL to demonstrate how a client
application can interact with a WMF Transform DLL which is accessed via the registry.

The companion application for this one, which contains the DLL, is called TantaTransformInDLL.
This solution forms the client and the TantaTransformInDLL provides a demonstration DLL 
which can rotate the video display. 

The TantaTransformInDLL client expects the MFTTantaVideoRotator_Sync to have been compiled
up and registered for COM access. See the comments in that sample for more information on 
this.

Other than its interaction with a registry based Transform, this code is basically a port
of the TantaFilePlaybackAdvanced sample. 

1) The main form offers controls to pick the video file and display it in the ctlTantaEVRFilePlayer control.
2) The ctlTantaEVRFilePlayer control creates the Media Session, the topology and EVR internally and handles
   all interaction with it.
3) The free online companion book, "Windows Media Foundation: Getting Started in C#" contains a detailed 
   discussion of the ctlTantaEVRFilePlayer control and the various operations it performs.
4) If the Rotator Transform option is set on the main form the MFTTantaVideoRotator_Sync.dll will 
   be loaded via COM and introduced into the video branch of the pipeline. This client knows
   which dll to load because the GUID for it is hardcoded into it. The TantaTransformInDLLClient 
   does not do the "discovery" process. See the TantaTransformPicker sample for a demonstration
   of that.
5) When the video is playing the client can configure the rotate mode on the transform dynamically.
   This is designed to demonstrate a communication process between the client and a transform
   dll loaded from the registry.

The demo mp4 files shipped with this sample code are open source mp4 files
found on: https://www.sample-videos.com/





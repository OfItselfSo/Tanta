TantaFilePlaybackSimple - see the GeneralNotes.txt file for notes and 
information common to all Tanta Projects.

####
#### The TantaFilePlaybackSimple Project
####

The function of this app is to demonstrate the simple play of a 
file containing video and audio streams. In the interests of 
reducing complexity, this application has no ability to control 
the flow of media information (other than stopping it entirely). 
If you are interested in things like pause, fast forward rewind, 
volume control and screen snapshots see the 
TantaFilePlaybackAdvanced sample application.

This project displays an mp4  file. It does this by
using a WMF Media Session, Pipeline (with two branches), Media Source
and Enhanced Video Renderer (EVR) and StreamingAudioRenderer (SAR).

The EVR and SAR are actually IMFMediaSinks and function as such in
pipeline although the topology handles the interaction with them.

The intent is to demonstrate the end-to-end operation of a pipeline
with two media streams terminating on two media renderer sinks.

Note that this code assumes the mp4 file has both audio and video 
streams and it will not work on mp4 files that do not. To implement 
support for audio only or video only mp4 files would not be hard 
but doing so would complicate the code and we are trying to keep 
things simple here..

TantaFilePlaybackAdvanced - see the GeneralNotes.txt file for notes and 
information common to all Tanta Projects.

####
#### The TantaFilePlaybackAdvanced Project
####

This project is designed to read a video file from disk and play 
it on the screen. It is a rewrite of the WMF Basic Playback sample.
If you wish to see a much simpler version of this functionality 
see the TantaFilePlaybackSimple sample application

1) The main form does not do much besides display pick the video file and display the ctlTantaEVRFilePlayer control.
2) The ctlTantaEVRFilePlayer control creates the Media Session, the topology and EVR internally and handles all interaction with it.
3) The free online companion book, "Windows Media Foundation - Getting Started in C#" contains a detailed discussion of 
       the ctlTantaEVRFilePlayer control and the various operations it performs.

The demo mp4 files shipped with this sample code are open source mp4 files
found on: https://www.sample-videos.com/





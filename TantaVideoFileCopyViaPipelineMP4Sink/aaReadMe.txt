See the GeneralNotes.txt file for notes and information common to all Tanta Projects.

####
#### The TantaVideoFileCopyViaPipelineMP4Sink Project
####

This project creates a copy of an mp4  file. It does this by
using a WMF Media Session, Pipeline (with two branches), Media Source
and Media Sink.

The intent is to demonstrate the end-to-end operation of a pipeline
with two media streams terminating on the same media sink.

Note that this code assumes the mp4 file has both audio and video 
streams and will not work on mp4 files that do not. To implement 
support for audio only or video only mp4 files would not be hard 
but doing so would complicate the code and we are trying to keep 
things simple here..

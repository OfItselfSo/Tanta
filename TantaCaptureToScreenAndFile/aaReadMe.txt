See the GeneralNotes.txt file for notes and information common to all Tanta Projects.

####
#### The TantaCaptureToScreenAndFile Project
####

This project displays the video stream from a video device attached
to the PC (a webcam) on the screen. It also, optionally, writes this
video stream to a file - thus recording it. 

This app creates a WMF Media Session, Pipeline Media Source
and Media Sink. The video device is the media source, and the 
Enhanced Video Renderer is the media sink. 

A Synchronous Transform is inserted into the topology between
the source and sink. Normally this transform just passes the 
input sample to the output. If a sink writer has been configured
on it then it will also present the sample it is processing to
the sink writer which then writes it to disk - thus recording it.

The intent is to demonstrate the end-to-end operation of a pipeline
with a transform in the middle acting as a "sample grabber" which
presents pipeline based media data to non pipeline objects.

Note that this is not the only way to implement such a recording 
mechanism. For example a "Tee" transform could be used to 
branch the pipeline and then an MP4 sink or SampleGrabber sink
could be used at the end of the second branch in order to
record the data.

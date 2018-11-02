See the GeneralNotes.txt file for notes and information common to 
all Tanta Projects.

####
#### The TantaVideoFileCopyViaPipelineAndWriter Project
####

This project creates a copy of an mp4 file with an audio and 
video stream. It does this by using a standard pipeline with a 
Sample Grabber Sink as the termination point. The SampleGrabber 
has the nice effect that every item of data that it gets (it 
just throws it away) is also presented to other objects in the system 
via a callback interface. The data is processed in the callback 
interface and passed to a SinkWriter.

The intent is to demonstrate the end-to-end operation of a hybrid
architecture with a two media streams. 

The native media types of the audio and video streams are used 
as output and this renders the output file identical to the 
input file. This could be changed by adjusting the output media
type on the Sink Writer.

This code may not run on the Windows 7 platform due to missing codecs.
It does run on Windows 10 with no issues.

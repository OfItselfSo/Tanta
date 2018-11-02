See the GeneralNotes.txt file for notes and information common to all 
Tanta Projects.

####
#### The TantaAudioFileCopyViaPipelineAndWriter Project
####

This project creates a copy of an mp3 audio file. It does this by
using a standard pipeline with a Sample Grabber Sink as the 
termination point. The SampleGrabber has the nice effect that 
every item of data that it gets (it just throws it away)
is also presented to other objects in the system via a callback 
interface. The data is processed in the callback interface and passed 
to a SinkWriter.

The intent is to demonstrate the end-to-end operation of a hybrid
architecture with a single media stream. 

This code may not run on the Windows 7 platform due to missing codecs.
It does run on Windows 10 with no issues.

See the GeneralNotes.txt file for notes and information common to all Tanta Projects.

####
#### The TantaVideoFileCopyViaReaderWriter Project
####

This project creates a copy of an mp4 media file by using 
a synchronous Source Reader and Sink Writer as the transfer 
mechanism. 

The native media types of the audio and video streams are used 
as output and this renders the output file identical to the 
input file. This could be changed by adjusting the output media
type on the Sink Writer.

The intent is to demonstrate the end-to-end operation of a media
transfer mechanism using a Source Reader and Sink Writer. For this
reason, the code has deliberately been kept very simple and
linear with many comments inlcuded along the way.
 
This code may not run on the Windows 7 platform due to missing codecs.
It does run on Windows 10 with no issues.

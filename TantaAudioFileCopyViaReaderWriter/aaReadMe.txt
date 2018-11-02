See the GeneralNotes.txt file for notes and information common to all Tanta Projects.

####
#### The TantaAudioFileCopyViaReaderWriter Project
####

This project creates a copy of an mp3 audio file. The application
performs the copy by using a synchronous Source Reader and Sink 
Writer as the transfer mechanism. 

The intent is to demonstrate the end-to-end operation of a 
synchronous Reader-Writer architecture with a single media 
stream. For this reason, the code has deliberately been kept 
very simple and linear with many comments inlcuded along the 
way.

The native media type of the audio stream is used 
as output and this renders the output file identical to the 
input file. This could be changed by adjusting the output media
type on the Sink Writer.

This code may not run on the Windows 7 platform due to missing codecs.
It does run on Windows 10 with no issues.


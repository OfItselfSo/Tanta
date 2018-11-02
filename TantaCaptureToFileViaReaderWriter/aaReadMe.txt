See the GeneralNotes.txt file for notes and information common to all 
Tanta Projects.

####
#### The TantaCaptureToFileViaReaderWriter Project
####

The function of this application is to demonstrate the process of capturing
video from a camera and writing that stream to a file. To do this it uses the
Source Reader and Sink Writer WMF objects in Asynchronous mode. Source Readers
and SinkWriters are containers which are often used to make certain WMF tasks
easier. The alternative is to use a Media Session, Media Source, Pipeline and
topology. 

This project is a re-write of the MF.Net WindowsCaptureToFile-2010 sample code.  

Specfically, this project does the following....

1) Interrogates the system and finds out which video devices are available.
2) Allow you to choose a video device to use as a media source.
3) Converts the media source into a SourceReader.  
4) Sets up a SinkWriter with a file so the video stream can be written to
   disk.
5) Set the capture to disk streams running in Async mode. See the 
   TantaVideoFileCopyViaReaderWriter for an example of synchronous mode
   SourceReaders and SinkWriters.
6) Properly close down and release various COM entities uses in the setup and 
   processing so that memory leaks are not generated.

General changes to the WindowsCaptureToFile-2010 sample code

1) The Ccapture.cs file, with its multiple classes and structures, has been split apart.
   Some of the functionality has been moved into general Tanta classes for re-use and the
   remainder is just setup as a linear sequence of actions activated right out of a 
   button press on the main form.
2) The callback handler, necessary for the async stream mechanism, has been moved into
   its own class.
4) The call back handler does not use the Windows message handler to report errors. 
   Instead it just calls a standard C# async delegate. 
3) The RegisterDeviceNotifications functionality present in the WindowsCaptureToFile-2010 
   sample code has been removed. Thus this sample code simply does not support the media
   source changing underneath it. Probably you would have to support this if you are 
   writing  for the general case
4) The fetching of the symbolic link name on the video device is removed. It is used
   for device loss notifications and is redundant since that code has been removed as well.

Note that this code uses the SourceReader and SourceWriter classes. This means
you will not see the Media Session and Pipeline that other Tanta Samples will use. 
The SourceReader and SourceWriter objects are explicitly configured and connected by
the code. There is no automatic Topology resolution because there is no user created 
Topology when using these two wrapper objects.
TantaTransformDirect - see the GeneralNotes.txt file for notes and 
information common to all Tanta Projects.

####
#### The TantaTransformDirect Project
####

The function of this app is to directly compile Transforms and add one of them to the video branch of the topology. This
application does not place its transforms in the registry or use transforms that are there. It is entirely devoted
to demonstrating the basics of transforms without the overhead of using COM to dig it out of the system. 

Fundamentally, this project displays an mp4 file. It does this by using a WMF Media Session, Pipeline (with two branches), 
Media Source and Enhanced Video Renderer (EVR) and StreamingAudioRenderer (SAR).

The EVR and SAR are actually IMFMediaSinks and function as such in pipeline although the topology handles the interaction 
with them.

A simpler version of this code (without the transforms) can be found in the TantaFilePlaybackSimple example app.

Note that this code assumes the mp4 file has both audio and video streams and it will not work on mp4 files that do not. 
To implement support for audio only or video only mp4 files would not be hard but doing so would complicate the code 
and we are trying to keep things simple here..

This code picks up a file off a disk and displays the video. The ctlEVRStreamDisplay is used to display the video.  
The TantaFilePlaybackAdvanced sample contains a much more fully implemented version of the video display
using the ctlTantaEVRFilePlayer control. If you need advanced functionality such as rewind, pause etc that is the
one to look at.

The user can choose a specific transform from a list of radio buttons. The video on screen will display the effects
of the transform. The transform must be chosen before the video is played. 

The user can also press a button and obtain information from the Transform if the Frame Counter transform is in 
use. This is intended to demonstrate that simple information exchanges between a running transform and other
objects is possible.

Note because the transform code is compiled directly into the application, you can set breakpoints and step through
the parts that interest you. Also, because everything inherits from the OISCommon classes, you can also put 
LogMessage, DebugMessage, and ReleaseMessage calls in various places to emit information to the log.

The transforms are:

  FrameCounter, MFTTantaFrameCounter_Sync:  A synchronous transform to count the video frames as they pass through it. 
                This is a very basic transform and it does not do anything with the count. It simply serves
                to demonstrate a very basic transform. Processes in-place.
                
  Grayscale,    MFTTantaGrayscale_Sync   A synchronous transform to change the video data to grayscale. This transform
                demonstrates copying the input data to the output data and a method for supporting multiple video 
                formats.
                
  Grayscale,    MFTTantaGrayscale_Async  An Asnchronous transform to change the video data to grayscale. This transform
                is identical to MFTTantaGrayscale_Sync except that it is asynchronous and uses in-place processing.
                
  WriteText,    MFTTantaWriteText_Sync   A synchronous transform to overwrite text on the video display. This transform
                uses in-place processing and supports only one input type. It demonstrates how to use both overlay and
                transparent fonts. 



# Tanta
This purpose of this project is to provide documentation and code samples which will enable people to develop applications for Windows Media Foundation in the C# language.

Normally, WMF is considered to be a C++ tool - however the full functionality of Windows Media Foundation is available to users of the C# language via the very capable open source MF.Net interface library.

Using C# and MF.Net you can source streams of media data, render those streams to the screen or save them to files. You can also manipulate the data as it passes through the Media Pipeline and make whatever changes to it you wish. The full debugging capabilities of Visual Studio are also available so you can step through your C# code to observe its operation.

The Tanta project has two parts: a free book (in pdf format) which provides detailed "get started" information and a series of C# Sample Projects which illustrate various WMF Concepts. 

##The Book

There is a book entitled "Windows Media Foundation: Getting Started in C#" available free of charge at the link [http://www.OfItselfSo.com/Tanta](http://www.OfItselfSo.com/Tanta). It is nearly 300 pages in length and, hopefully, along with the Sample Projects will enable you to write your own C# WMF applications. Although the book is intended for the C# programmer it does contain plenty of background material that would be of use to users of other languages.

It should be noted that the above book, although it is a free download, is copywrited material. Please do not distribute or copy it (in whole or in part) without permission. If there is sufficient interest a paper version may be made available at nominal cost. 

##The Sample Code

The Tanta Sample Projects are open source and released under the MIT License. It should be noted that some parts of the code in the Tanta Sample Projects are based on the MF.Net samples and that code, in turn, is derived from the original Microsoft samples. These have been placed in the public domain without copyright. The Windows Media Foundation: Getting Started in C# book available for download above contains a detailed discussion of most of the techniques used in these applications.

A selection of sample MP3 and MP4 files are available at the root of the Tanta Sample Application repository. All of the Tanta Sample Projects expect, by default, to read and write to a directory named "C:\Dump". You can save yourself a bit of typing if you create that directory and copy the sample files there.

There are 15 Sample Projects (listed below) but 4 of them will probably not work on Windows 7 due to the inavailability of various codecs on that system. Windows 10 and Visual Studio 2017 (the community edition is fine) is the recommended development platform.
The Tanta Sample Projects

-**TantaAudioFileCopyViaPipelineAndWriter**
    -Demonstrates the Hybrid Architecture by copying a single stream (audio) MP3 file. This application may not work on Windows 7 due to codec unavailability.

-**TantaAudioFileCopyViaPipelineMP3Sink**
    -Demonstrates the Pipeline Architecture by copying a single stream (audio) MP3 file.

-**TantaAudioFileCopyViaReaderWriter**
    -Demonstrates the Reader-Writer Architecture by copying a single stream (audio) MP3 file. This application may not work on Windows 7 due to codec unavailability.

-**TantaCaptureToFileViaReaderWriter**
    -Uses a Reader-Writer Architecture to capture video directly to a file.

-**TantaCaptureToScreenAndFile**
    -Uses a Hybrid Architecture to display video on the screen and, optionally, capture it to a file.

-**TantaFilePlaybackAdvanced**
    -Uses the Pipeline Architecture to play a media file containing audio and video tracks. This application uses the ctlTantaEVRFilePlayer control from the TantaCommon library and demonstrates various Pipeline control mechanisms such as Pause, Fast-Forward, Jump Scrolling and volume control etc.

-**TantaFilePlaybackSimple**
    -Uses the Pipeline Architecture to play a media file containing audio and video tracks. This application uses the ctlTantaEVRStreamDisplay control from the TantaCommon library to demonstrate simple audio and video playback with multiple streams.

-**TantaTransformDirect**
    -Uses the Pipeline Architecture to demonstrate how to use the Tanta Transform Base classes to build and add Transforms to a Topology. This application contains Transforms which count the video frames, convert the image to grayscale or write text on the video display - both Synchronous and Asynchronous Mode Transforms are demonstrated.

-**TantaTransformInDLLClient**
    -A Pipeline Architecture client which uses a DLL based Transform. This application also demonstrates various methods the client application can use to communicate with DLL based Transforms.

-**TantaTransformInDLL**
    -A project which creates a Transform as a DLL and also, optionally, registers it on the system. The Transform in this application rotates or mirrors the video on display.

-**TantaTransformPicker**
    -A project which uses the ctlTantaTransformPicker control in the TantaCommon library to enumerate and display the capabilities of the Transforms registered on the system.

-**TantaVideoFileCopyViaPipelineAndWriter**
    -Demonstrates the Hybrid Architecture by copying a two stream (audio and video) MP4 file. This application may not work on Windows 7 due to codec unavailability.

-**TantaVideoFileCopyViaPipelineMP4Sink**
    -Demonstrates the Pipeline Architecture by copying a two stream (audio and video) MP4 file.

-**TantaVideoFileCopyViaReaderWriter**
    -Demonstrates the Reader-Writer Architecture by copying a two stream (audio and video) MP4 file. This application may not work on Windows 7 due to codec unavailability.

-**TantaVideoFormats**
    -Uses the ctlTantaVideoPicker control from the TantaCommon library to show the video formats offered by the video capture devices (webcams) on the PC. 
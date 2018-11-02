
####
#### The TantaTransformInDLL Project
####

This project is designed to demonstrate how a transform can be compiled and configured
in the registry and accessed via a separate client. It also demonstrates a communication
method between the client and the dll which enables the client to modify the behaviour
of the transform while it is running. Specfically, this code will do the following:

1) Compile up a DLL which acts as a WMF Transform which can rotate the contents 
   of video on the screen.
2) The DLL will be automatically configured in the registry when it is compiled.
3) The DLL can also be manually configured in the registry by using the commands
   in the ManualRegister.txt file
4) The TantaTransformInDLLClient sample code provides a suitable client to 
   interact with this Transform. It expects this transform to have been 
   properly registered.

Some General Notes

1) You have to run this solution as Administrator or you will not have the permissions
   to automatically register the transform.
2) The automatic registration happens because the "Register for COM Interop" option
   is enabled on the Build tab of the TantaTransformInDLL project.
4) There are actually two registrations taking place. The first is registering the
   Transform DLL with COM which makes it available to the client and the second
   part is registering it so that it can be discovered by clients.
3) The TantaTransformInDLLClient sample code knows the GUID of the Rotator transform
   (it is hard coded into it) and so does not need to do the discovery process. The
   TantaTransformPicker sample application demonstrates the discovery process.
4) See the comments in the section around the DLLRegisterServer and DLLUnregisterServer
   functions for more information on the registry and discovery process.
5) The TantaTransformInDLLClient sample also demonstrates a communication mechanism
   which enables it to configure the Transform while it is running. Basically
   the client digs the instatiated Transform object out of the Topology node and sets
   an attribute on the Transform. The Rotator Transform knows to look for this and
   will dynamically adjust the rotate mode while the video is running.
6) Unlike this sample which uses the registry, the TantaTransform sample code 
   demonstrates how to use transforms which are compiled into the code (as classes). 
   It contains a client and multiple transforms. 


REM BuildAll - Builds all of the Tanta Sample Projects

C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaCommon\TantaCommon.csproj
if %errorlevel% neq 0 exit /b %errorlevel%

C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaAudioFileCopyViaPipelineAndWriter\TantaAudioFileCopyViaPipelineAndWriter.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaAudioFileCopyViaPipelineMP3Sink\TantaAudioFileCopyViaPipelineMP3Sink.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaAudioFileCopyViaReaderWriter\TantaAudioFileCopyViaReaderWriter.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaCaptureToFileViaReaderWriter\TantaCaptureToFileViaReaderWriter.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaCaptureToScreenAndFile\TantaCaptureToScreenAndFile.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaFilePlaybackAdvanced\TantaFilePlaybackAdvanced.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaFilePlaybackSimple\TantaFilePlaybackSimple.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaTransformDirect\TantaTransformDirect.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaTransformInDLLClient\TantaTransformInDLLClient.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaTransformPicker\TantaTransformPicker.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaVideoFileCopyViaPipelineAndWriter\TantaVideoFileCopyViaPipelineAndWriter.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaVideoFileCopyViaPipelineMP4Sink\TantaVideoFileCopyViaPipelineMP4Sink.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaVideoFileCopyViaReaderWriter\TantaVideoFileCopyViaReaderWriter.sln
if %errorlevel% neq 0 exit /b %errorlevel%
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaVideoFormats\TantaVideoFormats.sln
if %errorlevel% neq 0 exit /b %errorlevel%

REM this is last because it will fail if not running as Administrator
C:\Windows\Microsoft.Net\Framework\v4.0.30319\MSBuild.exe /t:Rebuild TantaTransformInDLL\TantaTransformInDLL.sln
if %errorlevel% neq 0 exit /b %errorlevel%

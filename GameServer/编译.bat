call "D:\\Program Files (x86)\\Microsoft Visual Studio 11.0\\Common7\\Tools\\VsDevCmd.bat"
:cd /d %~dp0
msbuild /t:rebuild /m:4 /verbosity:quiet /consoleloggerparameters:ErrorsOnly;NoSummary /property:OutDir=Bin\Debug\

if "%1" == "" pause
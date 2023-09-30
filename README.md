# idl2dynamo_converter
Translator ActiveX library (from IDL file) to C# NET DLL for Autodesk Dynamo as node-package library. Next there is a summary how use it:

## Summary about using

1. Download Windows SDK (f.e. with Visual Studio Installer any supported version, f.e. 10.0.19041.0);
2. Run oleview.exe (by default from path `C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\oleview.exe`);
3. Navigate to `Type Libraries` tab and find in List of installed COM servers needing you, f.e. `AutoCAD 2021 Type Library` and go to `view` representation of that COM server;
4. Save representation to IDL file and retyry step 3-4 for other target COM servers;
5. Using source code that library, navigate to `.\src\AX2LIB_Runner\Program.cs` and create `NET_DLL_Writer` for each created IDL;
6. Profit!

The video about steps above you can watch by [next link](https://drive.google.com/file/d/1d6aiRA5NFDPxGF83pzLpdmf_ZhiPmr1a/view?usp=sharing "").

## Summary about logic

Original IDL file (COM Server) consists of couple of Interfaces, Enums, Delegates and some other elements. Most useful in Autodesk Dynamo environmental are interfaces.
* For each intreface the will creating class-wrapper that have original intrface as public field with `_i` name;
* All `HRESULT` are considered as methods or fields (if have not any arguments);
* All other interfaces in arguments are marking as `dynamic` type;
* All enums in argument's list creating in Full-namespace mode;
* For each HRESULT and interface saving original `helpstring` data that transforming to VS description of element;
* For methods that using optional arguments creating a comment after argument's string (because Dynamo can not initialize wrapper methods with optional arguments, and parent class need change to `dynamic` type);
* Original logic with using `ref`, `out` argumens are save, but in library you need to remove all `ref` and `out` keywords from arguments.

Other information (and discussing) you can fing at https://forum.dynamobim.com.

# Sample packages as result

AutoCAD ActiveX API Wrapper: https://github.com/GeorgGrebenyuk/Dyn_ActiveX_ACAD_and_C3D;
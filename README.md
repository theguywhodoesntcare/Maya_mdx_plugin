# Autodesk Maya MDX Plugin
Plugin for Autodesk Maya, using .NET API and MEL API, able to import Warcraft 3 MDX800 (classic) models. Magos' MdxLib using as parser. Currently supports mesh import, rig, joint animations, and texture loading.

Plugin adds file translator and four MEL commands for setting up texture loading to Maya.

![](images/arthasillidan.png)

Made and tested for Autodesk Maya 2024.

# Installation
1. Download the latest release, or build plugin yourself. Copy **wc3ToMaya.nll.dll** and **MdxLib.dll** to binary plug-ins directory. For example **C:\Program Files\Autodesk\Maya2024\bin\plug-ins**.
2. Find **wc3ToMaya.nll.dll** in Plug-in Manager and set checkboxes.
![Plug-in](images/window.png)
3. *Optional*. If loading does not work and error “Invoking ExecuteInDefaultAppDomain() failed” is displayed in Script Edior, then add the **maya.exe.config** file to the **bin** folder (for example **C:\Program Files\Autodesk\Maya2024\bin**). See the known issues section for more information.
3. *Optional*. If you need automatic loading of textures, then use the MEL command **"wc3_setAssetDir;"** in the script editor, then select the root directory of the unpacked Warcraft. The following formats are available: .dds, .tga, .png, .tif, .tiff, .bmp, .jpg, .jpeg.
4. *Optional. Experimental*. If you need to convert .blp textures while importing, then integration with *Retera Model Studio 04.4k+* is possible. Use the MEL command **"wc3_setCLIApp;"**, then select ReteraModelStudio.exe. After this, the blp textures will be automatically converted to png (new file will be created near). *A large number of textures being converted on the fly may cause slow loading speeds.*

# Importing

Use File -> Import -> Warcraft MDX option. Model will be loaded into scene in a couple of seconds.

![](images/demongate.png)

Currently supports mesh data (vertices, faces, normals, uv, skin data), rig data (bones and helpers presented as joints, rig hierarchy), animations data (translate, rotate and scale of joints except Global sequences).

Other types of nodes except for bones and helpers are not supported. Geoset animations, material animations, cameras and lights are also not supported; all this is ignored. 

Plugin automatically creates a Lambert shader for single-layer materials, and a Layered shader for multi-layer materials.

Textures with a path to the file are supported; among procedural/dynamic textures (ReplaceableId), only Team Color (ReplaceableId 1) is supported, others are ignored. File Node for every non-replaceable texture will be created. Plugin will try to find textures in importing model folder (including subfolders), as well as in the assets directory (if it is installed using the appropriate MEL command).

Team Color is implemented as a Layered shader, with a custom color on the second layer.

![Team Color](images/paladin.gif)

# Clips Navigation

Plugin will create a track and clip for each sequence (except global sequences) in the *Time Editor*. Current animation can be specify with Solo Track and Mute Track functions. Immediately after import animation with id 0 will start playing.
![Time Editor](images/ghoul.png)

# Known Issues

- Error *"Invoking ExecuteInDefaultAppDomain() failed"* while loading plugin. Solution: add the maya.exe.config file to the bin folder (for example C:\Program Files\Autodesk\Maya2024\bin). More info [here](https://forums.autodesk.com/t5/maya-programming/odd-net-plugin-behaviour/td-p/8129246).
- Very rarely, a bug is possible when an incorrect texture file is applied to mesh. Solution: reimport model.
- Plugin imports animations with a default *Spline* controller, OR with a *Linear* controller if it is used in Warcraft model. 3DSMax TCB controller (the most common case in Blizzard models) is not supported in Maya. *InTan and OutTan values are ignoring.*

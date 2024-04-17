# Autodesk Maya MDX Plugin
Plugin for Autodesk Maya, able to import Warcraft 3 MDX800 (classic) models. Magos' MdxLib using as parser. Currently only supports mesh import, rig hierarchy (without skin), and texture loading. No animation.

![](images\arthasillidan.png)

Made and tested for Autodesk Maya 2024.

# Installation
1. Copy **wc3ToMaya.nll.dll** and **MdxLib.dll** to binary plug-ins directory. For example **C:\Program Files\Autodesk\Maya2024\bin\plug-ins**.
2. Find **wc3ToMaya.nll.dll** in Plug-in Manager and set checkboxes.
![Plug-in](images\window.png)
3. *Optional*. If you need automatic loading of textures, then use the MEL command "wc3_setAssetDir;" in the script editor, then select the root directory of the unpacked Warcraft. The following formats are available: .dds, .tga, .png, .tif, .tiff, .bmp, .jpg, .jpeg.
4. *Optional*. If you need to convert .blp textures while importing, then integration with *Retera Model Studio 04.4k+* is possible. Use the MEL command "wc3_setCLIApp;", then select ReteraModelStudio.exe. After this, the blp textures will be automatically converted to png (new file will be created near).

# Importing

Use File -> Import -> Warcraft MDX option. 

![](images\demongate.png)

Currently only supports mesh import, rig hierarchy (without skin), and texture loading (if the appropriate global variables are set in steps 3 and 4). 

The plugin automatically creates a Lambert shader for single-layer materials, and a Layered shader for multi-layer materials.

Textures with a path to the file are supported; among procedural/dynamic textures (ReplaceableId), only Team Color (ReplaceableId 1) is supported, others are ignored. Plugin creates File Node for every non-replaceable texture.

Team Color is implemented as a Layered shader, with a custom color on the second layer.

![Team Color](images\paladin.gif)
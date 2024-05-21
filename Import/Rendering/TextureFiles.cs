using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Autodesk.Maya.OpenMaya;
using MdxLib.Model;

namespace wc3ToMaya.Rendering
{
    internal class TextureFiles
    {
        static readonly MDGModifier dgMod = new MDGModifier();

        static readonly string[] Properties = { "coverage", "mirrorU", "mirrorV", "noiseUV",
            "offset", "repeatUV", "rotateFrame", "rotateUV", "stagger", "translateFrame",
            "vertexCameraOne", "vertexUvOne", "vertexUvThree", "vertexUvTwo", "wrapU", "wrapV" };
        
        // https://help.autodesk.com/view/MAYAUL/2024/ENU/index.html?contextId=NODES-FILE
        static readonly Dictionary<string, object> ColorSpaceProperties =  new Dictionary<string, object>
        {
            { "colorManagementConfigFileEnabled", true },
            { "colorManagementConfigFilePath", "/OCIO-configs/Maya2022-default/config.ocio" },
            { "colorManagementEnabled", true },
            { "colorSpace", "sRGB" }
        };
        // List of valid extensions for Maya
        static readonly string[] validExtensions = { ".dds", ".tga", ".png", ".tif", ".tiff", ".bmp", ".jpg", ".jpeg" };

        internal static Dictionary<string, (MFnDependencyNode, MFnDependencyNode)> CreateNodes(CModel model, string dirName)
        {
            var dictionary = new Dictionary<string, (MFnDependencyNode, MFnDependencyNode)>();
            MGlobal.displayInfo("\r\nWarcraft 3 Textures:");
            foreach (var texture in model.Textures)
            {
                if (texture.FileName.Length > 0)
                {
                    dictionary.Add(texture.FileName, CreateNodePair(texture.FileName, texture.WrapWidth, texture.WrapHeight, dirName));
                    MGlobal.displayInfo($" — {texture.FileName}");
                }
                else
                {
                    MGlobal.displayInfo($" — Replaceable ID {texture.ReplaceableId}");
                }
            }
            MGlobal.displayInfo("");
            return dictionary;
        }

        static (MFnDependencyNode, MFnDependencyNode) CreateNodePair(string texture, bool wrapU, bool wrapV, string dirName)
        {
            // create place2dTexture node
            var place2dNode = new MFnDependencyNode();
            var place2dTexture = place2dNode.create("place2dTexture");

            // Create file node
            var fileNode = new MFnDependencyNode();
            var textureFile = fileNode.create("file", Path.GetFileName(texture));

            // Connect place2dtexture and file
            place2dNode.ConnectPlugs(dgMod, fileNode, "outUV", "uvCoord");

            foreach (var property in Properties)
            {
                place2dNode.ConnectPlugs(dgMod, fileNode, property, property);
            }

            place2dNode.ConnectPlugs(dgMod, fileNode, "outUvFilterSize", "uvFilterSize");

            // https://xgm.guru/p/wc3/anti-seam-miniarticle
            // Set WrapUV if need
            place2dNode.SetPlugVal("wrapU", wrapU);
            place2dNode.SetPlugVal("wrapV", wrapV);

            // Set texture path
            fileNode.SetPlugVal("fileTextureName", GetTexturePath(texture, dirName));

            // Change default color space settings
            foreach (var property in ColorSpaceProperties)
            {
                fileNode.SetPlugVal(property.Key, property.Value);
            }

            dgMod.doIt();

            ReapplyColorSpaceRules(fileNode);

            return (place2dNode, fileNode);
        }
        static string GetTexturePath(string texture, string dirName)
        {
            string texturePath = texture.Substring(0, texture.LastIndexOf('.'));
            texturePath = texturePath.Replace("\\", "/");

            if (Directory.Exists(dirName)) // first try to find texture in 
            {
                foreach (string file in Directory.EnumerateFiles(dirName, "*.*", SearchOption.AllDirectories))
                {
                    string extension = Path.GetExtension(file);
                    string fileName = Path.GetFileNameWithoutExtension(file);

                    if (fileName == Path.GetFileNameWithoutExtension(texturePath) && validExtensions.Contains(extension))
                    {
                        return file;
                    }
                    else if (fileName == Path.GetFileNameWithoutExtension(texturePath) && extension == ".blp")
                    {
                        return ConvertBLP(file);
                    }
                }
            }

            string assetDir = GetAssetDirCommand.GetAssetDirectory(); // then try to get texture from asset directory
            if (assetDir != string.Empty && Directory.Exists(assetDir))
            {
                texturePath = $"{assetDir}/{texturePath}";

                foreach (string ext in validExtensions)
                {
                    string tempPath = texturePath + ext;
                    if (File.Exists(tempPath))
                    {
                        return tempPath;
                    }
                }

                if (Path.GetExtension(texture) == ".blp" && File.Exists(texture))
                {
                    return ConvertBLP(texture);
                }
                return texture;

            }
            return texture;
        }

        static string ConvertBLP(string texturePath)
        {
            string exePath = GetCLIApp.GetPath();
            if (exePath != string.Empty)
            {
                string workingDirectory = Path.GetDirectoryName(exePath);
                string textureNew = texturePath.Substring(0, texturePath.LastIndexOf('.')) + ".png";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"-convert \"/{texturePath}\" \"{textureNew}\"",
                    WorkingDirectory = workingDirectory
                };

                Process process = new Process { StartInfo = startInfo };
                process.Start();
                process.WaitForExit(); // ???

                return textureNew;
            }
            return texturePath;
        }
        private static void ReapplyColorSpaceRules(MFnDependencyNode fileNode)
        {
            MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.cmEnabled {fileNode.name}.colorManagementEnabled");
            MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.configFileEnabled {fileNode.name}.colorManagementConfigFileEnabled");
            MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.configFilePath {fileNode.name}.colorManagementConfigFilePath");
            MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.workingSpaceName {fileNode.name}.workingSpace");
        }
    }
}

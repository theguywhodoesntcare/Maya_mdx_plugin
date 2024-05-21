using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaRender.MHWRender;
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

        internal static Dictionary<string, (MFnDependencyNode, MFnDependencyNode)> CreateNodes(CModel model)
        {
            var dictionary = new Dictionary<string, (MFnDependencyNode, MFnDependencyNode)>();
            MGlobal.displayInfo("\r\nWarcraft 3 Textures:");
            foreach (var texture in model.Textures)
            {
                if (texture.FileName.Length > 0)
                {
                    dictionary.Add(texture.FileName, CreateNodePair(texture.FileName, texture.WrapWidth, texture.WrapHeight));
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

        static (MFnDependencyNode, MFnDependencyNode) CreateNodePair(string texture, bool wrapU, bool wrapV)
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
            fileNode.SetPlugVal("fileTextureName", GetTexturePath(texture));

            // Change default color space settings
            foreach (var property in ColorSpaceProperties)
            {
                fileNode.SetPlugVal(property.Key, property.Value);
            }

            dgMod.doIt();

            ReapplyColorSpaceRules(fileNode);

            return (place2dNode, fileNode);
        }
        static string GetTexturePath(string texture)
        {
            // Set texture path
            string texturePath = texture.Substring(0, texture.LastIndexOf('.'));
            texturePath = texturePath.Replace("\\", "/");

            if (GetAssetDirCommand.GetAssetDirectory() != string.Empty)
            {
                texturePath = $"{GetAssetDirCommand.GetAssetDirectory()}/{texturePath}";

                // List of valid extensions for Maya
                string[] validExtensions = { ".dds", ".tga", ".png", ".tif", ".tiff", ".bmp", ".jpg", ".jpeg" };

                string foundTexturePath = null;

                foreach (string ext in validExtensions)
                {
                    string tempPath = texturePath + ext;
                    if (File.Exists(tempPath))
                    {
                        foundTexturePath = tempPath;
                        break;
                    }
                }

                if (foundTexturePath != null)
                {
                    return foundTexturePath;
                }
                else
                {
                    if (Path.GetExtension(texture) == ".blp" && File.Exists(texture))
                    {
                        return ConvertBLP(texture, texturePath);
                    }
                    return texture;
                }
            }
            return texture;
        }

        private static void ReapplyColorSpaceRules(MFnDependencyNode fileNode)
        {
            MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.cmEnabled {fileNode.name}.colorManagementEnabled");
            MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.configFileEnabled {fileNode.name}.colorManagementConfigFileEnabled");
            MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.configFilePath {fileNode.name}.colorManagementConfigFilePath");
            MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.workingSpaceName {fileNode.name}.workingSpace");
        }
        static string ConvertBLP(string texture, string texturePath)
        {
            string exePath = GetCLIApp.GetPath();
            if (exePath != string.Empty)
            {
                string workingDirectory = Path.GetDirectoryName(exePath);
                string textureNew = texturePath + ".png";

                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = exePath,
                    Arguments = $"-convert \"{GetAssetDirCommand.GetAssetDirectory()}/{texture}\" \"{textureNew}\"",
                    WorkingDirectory = workingDirectory
                };

                Process process = new Process { StartInfo = startInfo };
                process.Start();
                process.WaitForExit(); // ???

                return textureNew;
            }
            return texture;
        }
    }
}

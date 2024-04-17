using Autodesk.Maya.OpenMaya;
using MdxLib.Model;
using wc3ToMaya.Rendering;
using System;
using System.Collections.Generic;

namespace wc3ToMaya
{
    internal static class MatCreator
    {
        // Graph Modifier
        static readonly MDGModifier dgMod = new MDGModifier();

        internal static void СreateMat(CMaterial wcMat, MFnMesh meshFn, Dictionary<string, (MFnDependencyNode, MFnDependencyNode)> textureDict)
        {
            //There are some problems with low-level creation of a shader group, so MEL commands are used

            // uniq name
            string uniqueId = DateTime.Now.Ticks.ToString();
            string shadingGroupName = "myShadingGroup" + uniqueId;

            // New shading group
            string shadingGroup = MGlobal.executeCommandStringResult($"sets -renderable true -noSurfaceShader true -empty -name {shadingGroupName}");

            // connect shading group to mesh
            MGlobal.executeCommand($"sets -e -forceElement {shadingGroupName} {meshFn.fullPathName}");

            if (wcMat.Layers.Count > 1)
            {
                // Create LayeredShader node
                MFnLayeredShader layeredShader = new MFnLayeredShader();
                MObject lsObject = layeredShader.create();
                string layeredShaderName;

                if (wcMat.Layers.Count == 2 && wcMat.Layers[0].Texture.Object.ReplaceableId == 1)
                {
                    // Team Color

                    var shader = CreateLambert(wcMat.Layers[1], textureDict, shadingGroupName, dgMod);

                    // Connect Lambert to first input
                    layeredShader.ConnectInputPropertyToNode(dgMod, 0, 0, shader, "outColor");
                    layeredShader.ConnectInputPropertyToNode(dgMod, 0, 1, shader, "outTransparency");

                    layeredShader.SetInputColor(1, (0.8f, 0.0f, 0.0f));
                    layeredShader.SetInputTransparency(1, (0f, 0f, 0f));

                    // Connect Layered to shading group
                    layeredShaderName = "wc3TeamColor" + uniqueId;
                    layeredShader.setName(layeredShaderName);
                    MGlobal.executeCommand($"connectAttr -f {layeredShaderName}.outColor {shadingGroupName}.surfaceShader");
                    dgMod.doIt();
                    return;
                }

                uint counter = 0;

                foreach (var layer in wcMat.Layers)
                {
                    var l_shader = CreateLambert(wcMat.Layers[0], textureDict, shadingGroupName, dgMod);

                    //Connect Lambert to input
                    layeredShader.ConnectInputPropertyToNode(dgMod, counter, 0, l_shader, "outColor");
                    layeredShader.ConnectInputPropertyToNode(dgMod, counter, 1, l_shader, "outTransparency");
                    counter++;
                }

                // Connect Layered to shading group
                layeredShaderName = "wc3MultiLayered" + uniqueId;
                layeredShader.setName(layeredShaderName);
                MGlobal.executeCommand($"connectAttr -f {layeredShaderName}.outColor {shadingGroupName}.surfaceShader");
                dgMod.doIt();
                return;
            }
            CreateLambert(wcMat.Layers[0], textureDict, shadingGroupName, dgMod);
            dgMod.doIt();
        }

        static MFnLambertShader CreateLambert(CMaterialLayer layer, Dictionary<string, (MFnDependencyNode, MFnDependencyNode)> textureDict, string shadingGroupName, MDGModifier dgMod)
        {
            string uniqueId = DateTime.Now.Ticks.ToString(); // uniq name
            string materialName = "wc3Mat" + uniqueId;

            MFnLambertShader shader = new MFnLambertShader();
            MObject phongshade = shader.create();
            shader.setName(materialName);

            MGlobal.executeCommand($"connectAttr -f {materialName}.outColor {shadingGroupName}.surfaceShader");

            string path = layer.Texture.Object.FileName;

            if (path.Length > 0)
            {
                var fileNode = textureDict[path].Item2;

                MPlug colorPlug1 = shader.findPlug("color");
                MPlug outColorPlug1 = fileNode.findPlug("outColor");
                dgMod.connect(outColorPlug1, colorPlug1);

                MPlug outTransp = fileNode.findPlug("outTransparency");
                MPlug transp = shader.findPlug("transparency");
                dgMod.connect(outTransp, transp);
            }
            return shader;
        }
    }
}

using Autodesk.Maya.OpenMaya;

namespace wc3ToMaya.Rendering
{
    internal static class Scene
    {
        internal static void SetViewportSettings() 
        {
            MGlobal.executeCommand("setAttr \"hardwareRenderingGlobals.transparencyAlgorithm\" 5;");
            MGlobal.executeCommand("modelEditor - edit - displayAppearance smoothShaded - activeOnly false modelPanel4;");
            MGlobal.executeCommand("modelEditor - e - displayTextures true modelPanel4;");
        }
        /*static internal void ReapplyColorSpaceRules()
        {
            // https://stackoverflow.com/questions/43693879/locked-file-texture-in-maya-shadingnode

            MItDependencyNodes it = new MItDependencyNodes(MFn.Type.kFileTexture);
            while (!it.isDone)
            {
                MObject obj = it.thisNode;
                MFnDependencyNode file = new MFnDependencyNode(obj);

                MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.cmEnabled {file.name}.colorManagementEnabled");
                MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.configFileEnabled {file.name}.colorManagementConfigFileEnabled");
                MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.configFilePath {file.name}.colorManagementConfigFilePath");
                MGlobal.executeCommand($"connectAttr defaultColorMgtGlobals.workingSpaceName {file.name}.workingSpace");

                it.next();
            }
        }*/
    }
}

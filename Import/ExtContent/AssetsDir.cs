using Autodesk.Maya.OpenMaya;

[assembly: MPxCommandClass(typeof(wc3ToMaya.SetAssetDirCommand), "wc3_setAssetDir")]
[assembly: MPxCommandClass(typeof(wc3ToMaya.GetAssetDirCommand), "wc3_getAssetDir")]

namespace wc3ToMaya
{
    public class SetAssetDirCommand : MPxCommand
    {
        public override void doIt(MArgList args)
        {
            // MEL-command for open fileDialog
            string melCommand = "fileDialog2 -fileMode 3 -dialogStyle 2";
            MStringArray result = new MStringArray();
            MGlobal.executeCommand(melCommand, result, false, false);

            if (result.length > 0)
            {
                // Save path to Maya global var
                MGlobal.executeCommand($"optionVar -stringValue wc3_assetDirectory \"{result[0]}\"");
                MGlobal.displayInfo("Asset directory has been set");
            }
        }
    }

    public class GetAssetDirCommand : MPxCommand
    {
        public override void doIt(MArgList args)
        {
            try
            {
                string command = "optionVar -q wc3_assetDirectory";
                MGlobal.executeCommand(command, out string assetDirectory);

                setResult(assetDirectory);
            }
            catch 
            {
                MGlobal.displayInfo("Set the asset directory!");
            }
        }

        public static string GetAssetDirectory()
        {
            string assetDirectory = string.Empty;
            try
            {
                // Get wc3_assetDirectory
                MGlobal.executeCommand("optionVar -q wc3_assetDirectory", out assetDirectory);
            }
            catch
            {
                MGlobal.displayInfo("Asset directory not specified");
            }
            return assetDirectory;
        }

    }
}
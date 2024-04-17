using Autodesk.Maya.OpenMaya;
using System.IO;

[assembly: MPxCommandClass(typeof(wc3ToMaya.SetCLIApp), "wc3_setCLIApp")]
[assembly: MPxCommandClass(typeof(wc3ToMaya.GetCLIApp), "wc3_getCLIApp")]

namespace wc3ToMaya
{
    public class SetCLIApp : MPxCommand
    {
        public override void doIt(MArgList args)
        {
            // MEL-command for open fileDialog
            string melCommand = "fileDialog2 -fileMode 1 -dialogStyle 2";
            MStringArray result = new MStringArray();
            MGlobal.executeCommand(melCommand, result, false, false);

            if (result.length > 0 && Path.GetExtension(result[0]) == ".exe")
            {
                // Save path to Maya global var
                MGlobal.executeCommand($"optionVar -stringValue wc3_CLIApp \"{result[0]}\"");
                MGlobal.displayInfo("Path to CLI app has been set");
            }
            else
            {
                MGlobal.displayInfo("Not an EXE file");
            }
        }
    }

    public class GetCLIApp : MPxCommand
    {
        public override void doIt(MArgList args)
        {
            try
            {
                string command = "optionVar -q wc3_CLIApp";
                MGlobal.executeCommand(command, out string cliAppPath);

                setResult(cliAppPath);
            }
            catch 
            {
                MGlobal.displayInfo("Set the path to EXE file!");
            }
        }

        public static string GetPath()
        {
            string cliAppPath = string.Empty;
            try
            {
                // Get wc3_assetDirectory
                MGlobal.executeCommand("optionVar -q wc3_CLIApp", out cliAppPath);
            }
            catch
            {
                MGlobal.displayInfo("Path to EXE not specified");
            }
            return cliAppPath;
        }

    }
}
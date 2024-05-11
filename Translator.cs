using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaAnim;
using MdxLib.Model;
using MdxLib.ModelFormats;
using System;
using System.Collections.Generic;
using System.IO;
using wc3ToMaya.Rendering;
using System.Globalization;
using System.Threading;
using wc3ToMaya.Animates;

[assembly: MPxFileTranslatorClass(typeof(wc3ToMaya.MyFormatTranslator), "Warcraft MDX", null, null, null)]

namespace wc3ToMaya
{
    public class MyFormatTranslator : MPxFileTranslator
    {
        public override void reader(MFileObject file, string optionsString, FileAccessMode mode)
        {
            CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                string filePath = file.fullName;

                CModel model = new CModel();

                using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    new CMdx().Load(filePath, fileStream, model);

                    Dictionary<INode, MFnIkJoint> nodeToJoint = Rig.CreateAndSaveData(model);
                    string selector = Mesh.Create(model, Path.GetFileNameWithoutExtension(filePath), nodeToJoint);
                    string composition = Animator.CreateComposition(model, Path.GetFileNameWithoutExtension(filePath));

                    Animator.ImportSequences(model, nodeToJoint, composition, selector);
                    Rig.RemoveTempPrefix(nodeToJoint);

                    Scene.ReapplyColorSpaceRules();
                    Scene.SetViewportSettings();
                }
            }
            catch (Exception ex)
            {
                MGlobal.displayInfo($"Error while importing Warcraft 3 File: {ex.Message}");
                MGlobal.displayInfo($"Source: {ex.Source}");
                MGlobal.displayInfo($"Stack Trace: {ex.StackTrace}");
                MGlobal.executeCommandOnIdle("ScriptEditor;");
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = originalCulture;
            }
        }

        public override bool haveReadMethod()
        {
            return true;
        }

        public override bool haveWriteMethod()
        {
            return false;
        }

        public override string defaultExtension()
        {
            return "mdx";
        }

        public override string filter()
        {
            return "*.mdx";
        }
    }
}
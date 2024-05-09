using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaAnim;
using MdxLib.Model;
using MdxLib.Animator;
using System.Collections.Generic;
using System.Text;

namespace wc3ToMaya.Animates
{
    internal class Animator
    {
        readonly static int customStart = 10;
        readonly static string tempPrefix = "TEMPNAME_";

        public static void ImportSequences(CModel model, Dictionary<INode, MFnIkJoint> nodeToJoint, string composition)
        {
            Dictionary<INode, MVector> nodeToPivot = new Dictionary<INode, MVector>();
            foreach (var pair in nodeToJoint)
            {
                MVector pivot = pair.Value.getTranslation(MSpace.Space.kTransform);
                nodeToPivot.Add(pair.Key, pivot);
            }


            timeEditorMEL.Enable();
            
            foreach (var seq in model.Sequences)
            {
                ImportSequence(model, seq.ObjectId, nodeToJoint, composition, nodeToPivot);
            }
        }
        private static void ImportSequence(CModel model, int id, Dictionary<INode, MFnIkJoint> nodeToJoint, string composition, Dictionary<INode, MVector> nodeToPivot)
        {
            (int start, int finish, int duration, string seqName) = GetSequenceProperties(model.Sequences[id]);

            if (char.IsDigit(seqName[0]))
            {
                seqName = seqName.Insert(0, "i");
            }

            foreach (var pair in nodeToJoint)
            {
                MVector pivot = nodeToPivot[pair.Key];
                foreach (var kf in pair.Key.Scaling)
                {
                    bool islinear = pair.Key.Scaling.Type == EInterpolationType.Linear;
                    if (kf.Time > finish) break;
                    if (kf.Time < start) continue;
                    int frameNumber = GetFrame(kf.Time, start);

                    MPoint scale = kf.Value.ToMPoint(false);

                    string melCommand = $"currentTime {frameNumber} ; scale -a -ws {scale.x} {scale.y} {scale.z} {pair.Value.name}; setKeyframe -breakdown 0 -hierarchy none -controlPoints 0 -shape 0 {pair.Value.name};";
                    MGlobal.executeCommand(melCommand);
                    if (islinear)
                    {
                        MGlobal.executeCommand($"keyTangent -itt linear -ott linear {pair.Value.name};");
                    }
                }
                foreach (var kf in pair.Key.Rotation)
                {
                    bool isLinear = pair.Key.Rotation.Type == EInterpolationType.Linear;
                    if (kf.Time > finish) break;
                    if (kf.Time < start) continue;
                    int frameNumber = GetFrame(kf.Time, start);

                    MEulerRotation euler = kf.Value.ToEuler();

                    string melCommand = $"currentTime {frameNumber} ; rotate -a -os {euler.x} {euler.y} {euler.z} {pair.Value.name}; setKeyframe -breakdown 0 -hierarchy none -controlPoints 0 -shape 0 {pair.Value.name};";
                    MGlobal.executeCommand(melCommand);
                    if (isLinear)
                    {
                        MGlobal.executeCommand($"keyTangent -itt linear -ott linear {pair.Value.name};");
                    }
                }
                foreach (var kf in pair.Key.Translation)
                {
                    bool islinear = pair.Key.Translation.Type == EInterpolationType.Linear;
                    if (kf.Time > finish) break;
                    if (kf.Time < start) continue;
                    int frameNumber = GetFrame(kf.Time, start);

                    MPoint pos = kf.Value.ToMPoint();
                    string melCommand = $"currentTime {frameNumber} ; move -ls -wd {pos.x + pivot.x} {pos.y + pivot.y} {pos.z + pivot.z} {pair.Value.name}; setKeyframe -breakdown 0 -hierarchy none -controlPoints 0 -shape 0 {pair.Value.name};";
                    MGlobal.executeCommand(melCommand);
                    if (islinear)
                    {
                        MGlobal.executeCommand($"keyTangent -itt linear -ott linear {pair.Value.name};"); 
                    }
                }
            }
            SelectRoot(nodeToJoint);
            StringBuilder sb = new StringBuilder();
            sb.Append($"timeEditorTracks -e -addTrack -1 -path \"{composition}\";\n");
            sb.Append($"timeEditorTracks -e -trackName \"{seqName}\" -path \"{composition}|track1\";\n");
            sb.Append($"timeEditorAnimSource -aso -type animCurveTL -type animCurveTA -type animCurveTT -type animCurveTU -addRelatedKG true -recursively -includeRoot -rsa 1 \"{tempPrefix}{seqName}\";\n");
            sb.Append($"timeEditorClip -startTime {customStart} -rootClipId -1  -duration {duration} -animSource \"{tempPrefix}{seqName}_AnimSource\" -track \"{composition}:{id}\" \"{seqName}\";\n");
            sb.Append($"rename \"{tempPrefix}{seqName}_AnimSource\" \"{seqName}_AnimSource\";\n");
            sb.Append($"timeEditorClip -e -clipId {id + 1} -name \"{seqName}\";\n");
            sb.Append($"timeEditorTracks -e -trackName \"{seqName}\" -trackIndex {id} {composition};\n");

            /*string com = $"timeEditorTracks -e -addTrack -1 -path \"{composition}\";\n" +
                             $"timeEditorTracks -e -trackName \"{seqName}\" -path \"{composition}|track1\";\n" +
                             $"timeEditorAnimSource -aso -type animCurveTL -type animCurveTA -type animCurveTT -type animCurveTU -addRelatedKG true -recursively -includeRoot -rsa 1 \"{tempPrefix}{seqName}\";\n" +
                             $"timeEditorClip -startTime {customStart} -rootClipId -1  -duration {duration} -animSource \"{tempPrefix}{seqName}_AnimSource\" -track \"{composition}:{id}\" \"{seqName}\";" +
                             $"rename \"{tempPrefix}{seqName}_AnimSource\" \"{seqName}_AnimSource\";" +
                             $"timeEditorClip - e - clipId {id + 1} - name \"{seqName}\";" +
                             $"timeEditorTracks -e -trackName \"{seqName}\" -trackIndex {id} Sorceress_Sorceress;";*/
            MGlobal.executeCommand(sb.ToString());

        }

        private static (int, int, int, string) GetSequenceProperties(CSequence seq)
        {
            int duration = seq.GetDuration();
            return (seq.IntervalStart, seq.IntervalEnd, duration, $"{seq.Name.Standardize()}_ID{seq.ObjectId}");
        }

        private static int GetFrame(int time, int start)
        {
            return time + 10 - start;
        }

        private static void SelectRoot(Dictionary<INode, MFnIkJoint> nodeToJoint)
        {
            MGlobal.executeCommand("select -clear");
            foreach (var pair in nodeToJoint)
            {
                if (pair.Key.Parent.Node == null)
                {
                    string name = pair.Value.name;
                    MGlobal.executeCommand($"select -add {name}");
                }
            }
        }
        internal static string CreateComposition(CModel model, string name)
        {
            MGlobal.executeCommand("TimeEditorWindow;");
            return MGlobal.executeCommandStringResult($"timeEditorComposition \"{name}_{model.Name}\";");
        }
    }
}

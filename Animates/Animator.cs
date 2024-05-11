using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaAnim;
using MdxLib.Model;
using MdxLib.Animator;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace wc3ToMaya.Animates
{
    internal class Animator
    {
        readonly static int customStart = 10;
        readonly static string tempPrefix = "TEMPNAME_";
        readonly static Dictionary<string, string> args = new Dictionary<string, string>
        {
            { "scale", "-a -os" },
            { "rotate", "-a -os"},
            { "move", "-ls -wd"}
        };

        public static void ImportSequences(CModel model, Dictionary<INode, MFnIkJoint> nodeToJoint, string composition, string selector)
        {
            if (model.Sequences.Count == 0 && model.GlobalSequences.Count == 0)
            {
                return;
            }
            
            Dictionary<INode, MVector> nodeToPivot = new Dictionary<INode, MVector>();
            StringBuilder sb = new StringBuilder();

            foreach (var pair in nodeToJoint)
            {
                MVector pivot = pair.Value.getTranslation(MSpace.Space.kTransform);
                nodeToPivot.Add(pair.Key, pivot);
            }

            timeEditorMEL.Enable();
            
            foreach (var seq in model.Sequences)
            {
                ImportSequence(model, seq.ObjectId, nodeToJoint, composition, nodeToPivot, sb);

                Thread.Sleep(1); // little bit magic!
                // For some reason this commands has no effect
                // We can't use Iddle option here, so thread sleep gonna help 
                MGlobal.executeCommand(selector);
                MGlobal.executeCommand("GoToBindPose;");
            }

            timeEditorMEL.SetSettings(composition, customStart, model.Sequences[0].GetDuration());
        }
        private static void ImportSequence(CModel model, int id, Dictionary<INode, MFnIkJoint> nodeToJoint, string composition, Dictionary<INode, MVector> nodeToPivot, StringBuilder sb)
        {
            (int start, int finish, int duration, string seqName) = GetSequenceProperties(model.Sequences[id]);
            
            foreach (var pair in nodeToJoint)
            {
                MVector pivot = nodeToPivot[pair.Key];
                string name = pair.Value.name;

                foreach (var kf in pair.Key.Scaling)
                {
                    // !TODO Temporary solution, later should sort keyframes into sequences in advance.
                    if (kf.Time < start) continue;
                    if (kf.Time > finish) break;
                    int frameNumber = GetFrame(kf.Time, start);

                    MPoint scale = kf.Value.ToMPoint(false);

                    SetKeyFrame(frameNumber, "scale", "scale", scale.x, scale.y, scale.z, name);
                }

                foreach (var kf in pair.Key.Rotation)
                {
                    if (kf.Time < start) continue;
                    if (kf.Time > finish) break;
                    int frameNumber = GetFrame(kf.Time, start);

                    //MEulerRotation euler = kf.Value.ToEuler();
                    //SetKeyFrame(frameNumber, "rotate", "rotate", euler.x, euler.y, euler.z, name);

                    // Euler is a bad way because it is gimbal lock
                    // Happens too often, especially in Death animations
                    // Use quaternions instead

                    MGlobal.executeCommand($"currentTime {frameNumber}");
                    MQuaternion quat = kf.Value.ToMQuaternion();
                    pair.Value.setRotationQuaternion(quat.x, quat.y, quat.z, quat.w, MSpace.Space.kObject);

                    MGlobal.executeCommand($"setKeyframe -bd 0 -hi none -cp 0 -s 0 -at \"rotate\" {name};"); ;
                }

                foreach (var kf in pair.Key.Translation)
                {
                    if (kf.Time < start) continue;
                    if (kf.Time > finish) break;

                    int frameNumber = GetFrame(kf.Time, start);
                    MPoint pos = kf.Value.ToMPoint();

                    SetKeyFrame(frameNumber, "move", "translate", pos.x + pivot.x, pos.y + pivot.y, pos.z + pivot.z, name);
                }

                // Force Maya to use quaternion interpolation
                // https://download.autodesk.com/us/maya/docs/Maya85/Commands/rotationInterpolation.html
                MGlobal.executeCommand($"rotationInterpolation -convert quaternion \"{pair.Value.name}.rotateX\"");

                Linearize(pair.Key, pair.Value);
            }

            SelectRoot(nodeToJoint);
            sb.Clear();
            sb.Append($"timeEditorTracks -e -addTrack -1 -path \"{composition}\";\n");
            sb.Append($"timeEditorTracks -e -trackName \"{seqName}\" -path \"{composition}|track1\";\n");
            sb.Append($"timeEditorAnimSource -aso -type animCurveTL -type animCurveTA -type animCurveTT -type animCurveTU -addRelatedKG true -recursively -includeRoot -rsa 1 \"{tempPrefix}{seqName}\";\n");
            sb.Append($"timeEditorClip -startTime {customStart} -rootClipId -1  -duration {duration} -animSource \"{tempPrefix}{seqName}_AnimSource\" -track \"{composition}:{id}\" \"{seqName}\";\n");
            sb.Append($"rename \"{tempPrefix}{seqName}_AnimSource\" \"{seqName}_AnimSource\";\n");
            sb.Append($"timeEditorClip -e -clipId {id + 1} -name \"{seqName}\";\n");
            sb.Append($"timeEditorTracks -e -trackName \"{seqName}\" -trackIndex {id} {composition};\n");

            MGlobal.executeCommand(sb.ToString());
        }

        private static void Linearize(INode node, MFnIkJoint joint)
        {
            if ( node.Translation.Type == EInterpolationType.Linear)
            {
                MGlobal.executeCommand($"keyTangent -itt linear -ott linear -at \"translate\" {joint.name};");
            }
            if (node.Rotation.Type == EInterpolationType.Linear)
            {
                MGlobal.executeCommand($"keyTangent -itt linear -ott linear -at \"rotate\" {joint.name};");
            }
            if (node.Scaling.Type == EInterpolationType.Linear)
            {
                MGlobal.executeCommand($"keyTangent -itt linear -ott linear -at \"scale\" {joint.name};");
            }
        }

        private static void SetKeyFrame(int time, string transformation, string type, double x, double y, double z, string name)
        {
            MGlobal.executeCommand($"currentTime {time}; {transformation} {args[transformation]} {x} {y} {z} {name}; setKeyframe -bd 0 -hi none -cp 0 -s 0 -at \"{type}\" {name};");
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
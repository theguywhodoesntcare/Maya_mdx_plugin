﻿using Autodesk.Maya.OpenMaya;
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
        readonly static int divisor = 2; // scale factor

        public static void ImportSequences(CModel model, Dictionary<INode, MFnIkJoint> nodeToJoint, string composition)
        {
            if (model.Sequences.Count == 0) //&& model.GlobalSequences.Count == 0)
            {
                return;
            }

            Dictionary<INode, MVector> nodeToPivot = new Dictionary<INode, MVector>();
            foreach (var pair in nodeToJoint)
            {
                MVector pivot = pair.Value.getTranslation(MSpace.Space.kTransform);
                nodeToPivot.Add(pair.Key, pivot);
            }

            StringBuilder sb = new StringBuilder();
            timeEditorMEL.Enable();
            SelectRoot(nodeToJoint);
            foreach (var seq in model.Sequences)
            {
                ImportSequence(model, seq.ObjectId, nodeToJoint, composition, nodeToPivot, sb);
            }
            
            timeEditorMEL.SetSettings(composition, customStart, model.Sequences[0].GetDuration() / divisor);
        }

        private static void ImportSequence(CModel model, int id, Dictionary<INode, MFnIkJoint> nodeToJoint, string composition, Dictionary<INode, MVector> nodeToPivot, StringBuilder sb)
        {
            (int start, int finish, int duration, string seqName) = GetSequenceProperties(model.Sequences[id]);
            
            foreach (var pair in nodeToJoint)
            {
                var node = pair.Key;
                var joint = pair.Value;

                MVector pivot = nodeToPivot[node];

                MTimeArray frames = new MTimeArray();
                MDoubleArray[] values = new MDoubleArray[3] { new MDoubleArray(), new MDoubleArray(), new MDoubleArray() }; // x, y, z Values
                MFnAnimCurve[] animCurves = new MFnAnimCurve[3] { new MFnAnimCurve(), new MFnAnimCurve(), new MFnAnimCurve() };

                var scalingList = node.Scaling.GetRange(start, finish);

                foreach (var kf in scalingList)
                {
                    int frameNumber = GetFrame(kf.Time, start, frames);

                    MPoint scale = kf.Value.ToMPoint(false);

                    frames.append(frameNumber);
                    SetXYZ(values, scale.x, scale.y, scale.z);
                }

                animCurves = FillCurves(animCurves, values, frames, joint, "scale");

                var rotList = node.Rotation.GetRange(start, finish);
                
                foreach (var kf in rotList)
                {
                    int frameNumber = GetFrame(kf.Time, start, frames);

                    MEulerRotation euler = kf.Value.ToMQuaternion().asEulerRotation;

                    frames.append(frameNumber);
                    SetXYZ(values, euler.x, euler.y, euler.z);
                }

                animCurves = FillCurves(animCurves, values, frames, joint, "rotate");

                var posList = node.Translation.GetRange(start, finish);

                foreach (var kf in posList)
                {
                    int frameNumber = GetFrame(kf.Time, start, frames);
                    MPoint pos = kf.Value.ToMPoint();

                    frames.append(frameNumber);
                    SetXYZ(values, pos.x + pivot.x, pos.y + pivot.y, pos.z + pivot.z);
                }

                FillCurves(animCurves, values, frames, joint, "translate");

                Linearize(node, joint);
            }

            // duration /= divisor;
            sb.Clear();
            sb.Append($"timeEditorTracks -e -addTrack -1 -path \"{composition}\";\n");
            sb.Append($"timeEditorTracks -e -trackName \"{seqName}\" -path \"{composition}|track1\";\n");
            sb.Append($"timeEditorAnimSource -aso -type animCurveTL -type animCurveTA -type animCurveTT -type animCurveTU -addRelatedKG true -recursively -includeRoot -rsa 1 \"{tempPrefix}{seqName}\";\n");
            sb.Append($"timeEditorClip -startTime {customStart} -rootClipId -1  -animSource \"{tempPrefix}{seqName}_AnimSource\" -track \"{composition}:{id}\" \"{seqName}\";\n"); // "-duration {duration}" arg was removed
            sb.Append($"rename \"{tempPrefix}{seqName}_AnimSource\" \"{seqName}_AnimSource\";\n");
            sb.Append($"timeEditorClip -e -clipId {id + 1} -name \"{seqName}\";\n");
            sb.Append($"timeEditorTracks -e -trackName \"{seqName}\" -trackIndex {id} {composition};\n");

            MGlobal.executeCommand(sb.ToString(), false, false);
        }
        private static void SetXYZ(MDoubleArray[] arrays, double x, double y, double z)
        {
            arrays[0].append(x);
            arrays[1].append(y);
            arrays[2].append(z);
        }
        private static MFnAnimCurve[] FillCurves(MFnAnimCurve[] animCurves, MDoubleArray[] values, MTimeArray frames, MFnIkJoint joint, string type)
        {
            if (frames.length > 0)
            {
                var (x, y, z) = (animCurves[0], animCurves[1], animCurves[2]);
                MObject obj = joint.objectProperty;

                x.create(obj, joint.attribute($"{type}X"));
                y.create(obj, joint.attribute($"{type}Y"));
                z.create(obj, joint.attribute($"{type}Z"));

                x.addKeys(frames, values[0]);
                y.addKeys(frames, values[1]);
                z.addKeys(frames, values[2]);
               
                if (type == "rotate")
                {
                    // Force Maya to use quaternion interpolation
                    // https://download.autodesk.com/us/maya/docs/Maya85/Commands/rotationInterpolation.html

                    // "quaternion" interpolation can cause unexpected spikes on curve between nearby keyframes
                    // so "quaternionSlerp" is used instead
                    MGlobal.executeCommand($"rotationInterpolation -c quaternionSlerp {x.name} {y.name} {z.name}");
                }
            }
            frames.clear();
            foreach (var arr in values)
            {
                arr.clear();
            }
            return new MFnAnimCurve[3] { new MFnAnimCurve(), new MFnAnimCurve(), new MFnAnimCurve() };
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
        private static (int, int, int, string) GetSequenceProperties(CSequence seq)
        {
            int duration = seq.GetDuration();
            return (seq.IntervalStart, seq.IntervalEnd, duration, $"{seq.Name.Standardize()}_ID{seq.ObjectId}");
        }
        private static int GetFrame(int time, int start, MTimeArray frames)
        {
            int f = (time - start) / divisor + customStart;
            while (frames.Contains(f))
            {
                f++;
            }

            return f;
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
    }
}
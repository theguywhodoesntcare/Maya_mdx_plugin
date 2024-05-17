using Autodesk.Maya.OpenMaya;
using MdxLib.Animator;
using MdxLib.Model;
using MdxLib.Primitives;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace wc3ToMaya.Animates
{
    public static class CModelSequenceExtensions
    {
        public static int GetDuration(this CSequence seq)
        {
            return seq.IntervalEnd - seq.IntervalStart + 1;
        }

        public static void Print(this CSequence seq)
        {
            MGlobal.displayInfo($"Sequence {seq.ObjectId} from {seq.IntervalStart} to {seq.IntervalEnd} with name {seq.Name}");
        }
    }
    public static class CAnimatorVector3Extensions
    {
        public static List<CAnimatorNode<CVector3>> GetRange(this CAnimator<CVector3> keyFrames, int start, int finish)
        {
            var kfList = keyFrames.ToList();
            kfList = kfList.Where(x => x.Time >= start && x.Time <= finish).ToList();
            return kfList;
        }
    }
    public static class CAnimatorVector4Extensions
    {
        public static List<CAnimatorNode<CVector4>> GetRange(this CAnimator<CVector4> keyFrames, int start, int finish)
        {
            var kfList = keyFrames.ToList();
            kfList = kfList.Where(x => x.Time >= start && x.Time <= finish).ToList();
            return kfList;
        }
    }
}

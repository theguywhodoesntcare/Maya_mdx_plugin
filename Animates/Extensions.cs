using Autodesk.Maya.OpenMaya;
using MdxLib.Model;

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
}

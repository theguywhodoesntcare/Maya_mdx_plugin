using Autodesk.Maya.OpenMaya;

namespace wc3ToMaya.Animates
{
    internal static class timeEditorMEL
    {
        internal static void FlipState()
        {
            MGlobal.executeCommand("TimeEditorSceneAuthoringToggle;");
        }

        internal static void Enable()
        {
            MGlobal.executeCommand("$isTEMuted = `timeEditor -q -mute`;\r\nif (!$isTEMuted) {\r\n    teToggleEditorMute(!$isTEMuted);\r\n};\r\nonTEMutedChanged();\r\n");
        }

        internal static void SetSettings(string composition, int start, int duration)
        {
            // It must be execute after all
            string command = $"TimeEditorSceneAuthoringToggle; timeEditorTracks -resetSolo;" +
                $"timeEditorTracks -e -trackSolo true -trackIndex 0 {composition};" +
                $"playbackOptions -min {start} -max {duration + start};" +
                "playButtonForward;";
            MGlobal.executeCommandOnIdle(command);
        }
    }
}

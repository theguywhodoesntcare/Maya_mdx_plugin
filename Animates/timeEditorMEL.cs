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

        internal static void Unfreeze()
        {
            // doesnt work
            // timeeditor still freezed after end of import

            FlipState();
            FlipState();
        }
    }
}

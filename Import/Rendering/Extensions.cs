using Autodesk.Maya.OpenMaya;

namespace wc3ToMaya.Rendering
{
    public static class MFnDependencyNodeExtensions
    {
        public static void ConnectPlugs(this MFnDependencyNode node1, MDGModifier dgMod, MFnDependencyNode node2, string plugName1, string plugName2)
        {
            dgMod.connect(node1.findPlug(plugName1), node2.findPlug(plugName2));
        }

        public static void SetPlugVal(this MFnDependencyNode fileNode, string plugName, object value)
        {
            var plug = fileNode.findPlug(plugName);

            if (value is bool v)
                plug.setBool(v);
            else if (value is string v1)
                plug.setString(v1);
        }
    }

    public static class MFnLayeredShaderExtensions
    {
        public static MPlug GetInput(this MFnLayeredShader layeredShader, uint index)
        {
            MPlug inputsPlug = layeredShader.findPlug("inputs");
            return inputsPlug.elementByLogicalIndex(index);
        }

        public static void SetInputColor(this MFnLayeredShader layeredShader, uint index, (float, float, float) rgb)
        {
            SetInputProperty(layeredShader, index, 0, rgb);
        }
        public static void SetInputTransparency(this MFnLayeredShader layeredShader, uint index, (float, float, float) rgb)
        {
            SetInputProperty(layeredShader, index, 1, rgb);
        }

        public static void SetInputProperty(this MFnLayeredShader layeredShader, uint index, uint type, (float, float, float) rgb)
        {
            var plug = layeredShader.GetInput(index);
            MPlug inputColor = plug.child(type); 

            inputColor.child(0).setValue(rgb.Item1); // Red
            inputColor.child(1).setValue(rgb.Item2); // Green
            inputColor.child(2).setValue(rgb.Item3); // Blue
        }

        public static void ConnectInputPropertyToNode(this MFnLayeredShader layeredShader, MDGModifier dgMod, uint index, uint type, MFnDependencyNode node, string plugName)
        {
            var input = layeredShader.GetInput(index);
            MPlug inputProp = input.child(type); 

            MPlug plug = node.findPlug(plugName);

            dgMod.connect(plug, inputProp);
        }
    }

}

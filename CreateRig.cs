using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaAnim;
using MdxLib.Model;
using MdxLib.Primitives;
using System.Collections.Generic;

namespace wc3ToMaya
{
    internal class Rig
    {
        readonly static string jointScale = "0.1";

        readonly static string tempPrefix = "_TEMPNAME_";

        internal static Dictionary<INode, MFnIkJoint> CreateAndSaveData(CModel model)
        {
            Dictionary<INode, List<INode>> parentToChildren = new Dictionary<INode, List<INode>>();
            Dictionary<INode, MFnIkJoint> nodeToJoint = new Dictionary<INode, MFnIkJoint>();

            foreach (CBone bone in model.Bones)
            {
                // Create Bones
                nodeToJoint.Add(bone, CreateJoint(bone));
            }

            foreach (CHelper helper in model.Helpers)
            {
                // Create Helpers
                nodeToJoint.Add(helper, CreateJoint(helper));
            }

            foreach (var link in nodeToJoint)
            {
                // Create Hierarchy
                INode node = link.Key;

                INode parent = node.Parent.Node;
                if (parent != null)
                {
                    if (!parentToChildren.ContainsKey(parent))
                    {
                        parentToChildren[parent] = new List<INode>();
                    }
                    parentToChildren[parent].Add(node);
                }
            }

            void TraverseNode(INode node)
            {
                if (parentToChildren.ContainsKey(node))
                {
                    foreach (INode child in parentToChildren[node])
                    {
                        CVector3 parentPivotPoint = node.PivotPoint;
                        CVector3 pivotPoint = child.PivotPoint;

                        CVector3 diff = new CVector3(pivotPoint.X - parentPivotPoint.X, pivotPoint.Y - parentPivotPoint.Y, pivotPoint.Z - parentPivotPoint.Z);

                        MVector jointPos = diff.ToMVector();

                        nodeToJoint[node].addChild(nodeToJoint[child].objectProperty);

                        nodeToJoint[child].setTranslation(jointPos, MSpace.Space.kPostTransform);
                        TraverseNode(child);
                    }
                }
            }

            foreach (INode node in model.Nodes)
            {
                if (node.Parent.Node == null)
                {
                    TraverseNode(node); //root
                }
            }

            MGlobal.executeCommand($"jointDisplayScale {jointScale};"); // change joint size

            return nodeToJoint;
        }
        private static MFnIkJoint CreateJoint(INode node)
        {
            MFnIkJoint joint = new MFnIkJoint();
            MObject jointObj = joint.create(MObject.kNullObj);
            joint.setName($"{tempPrefix}{node.Name}");

            CVector3 pivotPoint = node.PivotPoint; // world position

            MVector jointPos = pivotPoint.ToMVector();
            joint.setTranslation(jointPos, MSpace.Space.kTransform);

            return joint;
        }

        internal static void RemoveTempPrefix(Dictionary<INode, MFnIkJoint> nodeToJoint)
        {
            foreach (var pair in nodeToJoint)
            {
                pair.Value.setName(pair.Value.name.Replace(tempPrefix, ""));
            }
        }
    }
}

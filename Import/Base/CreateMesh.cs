using Autodesk.Maya.OpenMaya;
using Autodesk.Maya.OpenMayaAnim;
using MdxLib.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using wc3ToMaya.Rendering;

namespace wc3ToMaya
{
    internal class Mesh
    {
        internal static void Create(CModel model, Dictionary<INode, MFnIkJoint> nodeToJoint, string name, string dirName)
        {
            MSelectionList selList = new MSelectionList();
            var textureDict = TextureFiles.CreateNodes(model, dirName);

            foreach (CGeoset geoset in model.Geosets)
            {
                MPointArray points = new MPointArray(); 
                MFloatArray uArray = new MFloatArray(); 
                MFloatArray vArray = new MFloatArray(); 

                MIntArray polygonCounts = new MIntArray(); 
                MIntArray polygonConnects = new MIntArray();

                MIntArray vertexList = new MIntArray();
                MVectorArray normals = new MVectorArray();
                
                Dictionary<int, List<string>> matrices = new Dictionary<int, List<string>>();
                
                List<string> joints = new List<string>();
                foreach (var group in geoset.Groups)
                {
                    foreach (var node in group.Nodes)
                    {
                        joints.Add(nodeToJoint[node.Node.Node].name);
                    }
                }

                foreach (CGeosetVertex vertex in geoset.Vertices)
                {
                    points.append(vertex.Position.ToMPoint());

                    uArray.append(vertex.TexturePosition.X);
                    // Mirror V
                    vArray.append(1 - vertex.TexturePosition.Y);
                    
                    // v group
                    CGeosetGroup group = vertex.Group.Object;

                    List<string> names = new List<string>();
                    foreach (var node in group.Nodes)
                    {
                        names.Add(nodeToJoint[node.Node.Node].name);
                    }
                    matrices.Add(vertex.ObjectId, names);

                    //normals
                    vertexList.Add(vertex.ObjectId);
                    normals.Add(vertex.Normal.ToMVector(false));
                }

                GetFaces(geoset, polygonCounts, polygonConnects);

                MFnMesh meshFn = new MFnMesh();
                MObject mesh = meshFn.create(points.Count, polygonCounts.Count, points, polygonCounts, polygonConnects);

                meshFn.clearUVs();
                meshFn.setUVs(uArray, vArray);
                meshFn.assignUVs(polygonCounts, polygonConnects);

                SetNormals(meshFn, vertexList, normals);

                SetPivotToGeometricCenter(meshFn);

                // rename
                string meshNameBase = $"{name}_{model.Name}_{geoset.ObjectId}";
                meshNameBase = meshNameBase.Standardize();

                while (IsExist(meshNameBase)) // It allow us to safety load samename models
                {
                    meshNameBase = meshNameBase + "_copy";
                }
                string meshName = $"{meshNameBase}_shape";
                meshFn.setName(meshName.Standardize());

                // rename polySurface
                MObject parent = meshFn.parent(0);
                MFnDagNode dagNodeFn = new MFnDagNode(parent);
                string psName = $"{meshNameBase}_polySurface";
                dagNodeFn.setName(psName);

                CreateSkinClusterMEL(psName, matrices, joints, selList);

                MatCreator.СreateMat(geoset.Material.Object, meshFn, textureDict);
            }
        }
        static void GetFaces(CGeoset geoset, MIntArray polygonCounts, MIntArray polygonConnects)
        {
            int degenerativeFaces = 0;
            foreach (CGeosetFace face in geoset.Faces)
            {
                if (face.IsDegenerative()) // polygons with duplicate verts will cause an exception when calling assignUVs()
                {
                    degenerativeFaces++;
                    continue;
                }
                polygonCounts.append(3);
                polygonConnects.append(face.Vertex1.Object.ObjectId);
                polygonConnects.append(face.Vertex2.Object.ObjectId);
                polygonConnects.append(face.Vertex3.Object.ObjectId);
            }
            if (degenerativeFaces > 0) { MGlobal.displayWarning($"{degenerativeFaces} polygons with duplicate vertices were found and removed from the mesh. Check and correct input data"); }
        }
        static void SetNormals(MFnMesh meshFn, MIntArray verts, MVectorArray normals)
        {
            meshFn.setVertexNormals(normals, verts);
        }

        static void SetPivotToGeometricCenter(MFnMesh meshFn)
        {
            // get center
            MBoundingBox bbox = meshFn.boundingBox;
            MPoint center = bbox.center;

            // get Pivot attributes
            MFnTransform transformFn = new MFnTransform(meshFn.parent(0));
            MPlug rPivot = transformFn.findPlug("rotatePivot", true);
            MPlug sPivot = transformFn.findPlug("scalePivot", true);

            for (uint i = 0; i < 3; ++i)
            {
                rPivot.child(i).setDouble(center[i]);
                sPivot.child(i).setDouble(center[i]);
            }
        }

        static void CreateSkinClusterMEL(string meshName, Dictionary<int, List<string>> matrices, List<string> joints, MSelectionList selList)
        {
            selList.clear();
            selList.add(meshName);

            foreach (var jointName in joints)
            {                
                selList.add(jointName);
            }

            var clusterName = $"{meshName}SkinCluster";

            MGlobal.setActiveSelectionList(selList);
            MGlobal.executePythonCommand($"import maya.cmds as cmds\r\ncmds.skinCluster(n='{clusterName}')");
            var sb = new StringBuilder();
            foreach (var pair in matrices)
            {
                // Select vertex
                MGlobal.executeCommand($"select -r {meshName}.vtx[{pair.Key}];");

                // Get joints for vertex
                var influenceJoints = pair.Value;

                // Set joints as influence objects, dividing weight equally
                var weight = 1f / influenceJoints.Count;
                var weightStr = weight.ToString(CultureInfo.InvariantCulture);

                sb.Clear(); // Clear for reuse

                foreach (var joint in influenceJoints)
                {
                    sb.AppendFormat("-transformValue {0} {1} ", joint, weightStr); // weight matrix
                }
                sb.Length--;

                MGlobal.executeCommand($"skinPercent {sb} -normalize on {clusterName};");
            }
        }
        public static bool IsExist(string meshName)
        {
            string melCommand = $"objExists \"{meshName}_shape\"; objExists \"{meshName}_polySurface\"";
            MIntArray result = new MIntArray();
            MGlobal.executeCommand(melCommand, result);
            return result[0] == 1 || result[0] == 1;
        }
    }
}
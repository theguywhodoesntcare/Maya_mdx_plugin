using Autodesk.Maya.OpenMaya;
using MdxLib.Model;
using wc3ToMaya.Rendering;

namespace wc3ToMaya
{
    internal class Mesh
    {
        internal static void Create(CModel model, string name)
        {
            var textureDict = TextureFiles.CreateNodes(model);
            foreach (CGeoset geoset in model.Geosets)
            {
                MPointArray points = new MPointArray();
                MFloatArray uArray = new MFloatArray();
                MFloatArray vArray = new MFloatArray();
                MIntArray polygonCounts = new MIntArray();
                MIntArray polygonConnects = new MIntArray();

                foreach (CGeosetVertex vertex in geoset.Vertices)
                {
                    points.append(vertex.Position.ToMPoint());

                    uArray.append(vertex.TexturePosition.X);
                    // Mirror V
                    vArray.append(1 - vertex.TexturePosition.Y);
                }

                foreach (CGeosetFace face in geoset.Faces)
                {
                    polygonCounts.append(3);
                    polygonConnects.append(face.Vertex1.Object.ObjectId);
                    polygonConnects.append(face.Vertex2.Object.ObjectId);
                    polygonConnects.append(face.Vertex3.Object.ObjectId);
                }

                MFnMesh meshFn = new MFnMesh();
                MObject mesh = meshFn.create(points.Count, polygonCounts.Count, points, polygonCounts, polygonConnects);
                meshFn.setUVs(uArray, vArray);
                meshFn.assignUVs(polygonCounts, polygonConnects);

                SetPivotToGeometricCenter(meshFn);

                // rename
                meshFn.setName($"{name}_{model.Name}_{geoset.ObjectId}_shape");

                // rename polySurface
                MObject parent = meshFn.parent(0);
                MFnDagNode dagNodeFn = new MFnDagNode(parent);
                dagNodeFn.setName($"{name}_{model.Name}_{geoset.ObjectId}_polySurface");

                MatCreator.СreateMat(geoset.Material.Object, meshFn, textureDict);
            }
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
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;



namespace terrainOptimizer
{
    public class MeshBreakline : GH_Component
    {
        public MeshBreakline()
          : base("MeshBreakline", "Nickname",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("terrain", "terrain", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("breakline", "breakline", "", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;


        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("new terrain", "", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("outline", "", "", GH_ParamAccess.item);
        }


        HashSet<int> _facesToDelete;


        RTree tree;
        Mesh baseTerrain;
        Curve proposedBreakline;
        Polyline breakline;



        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref baseTerrain);
            DA.GetData(1, ref proposedBreakline);


            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (proposedBreakline.IsPolyline())
                proposedBreakline.TryGetPolyline(out breakline);
            else
                breakline = proposedBreakline.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

            Intersection.MeshRay(baseTerrain, new Ray3d(new Point3d(breakline[0].X, breakline[0].Y, -999), Vector3d.ZAxis), out int[] intersectedFaces);

            if (intersectedFaces.Length == 0)
                return;

            int currentFace = intersectedFaces[0];
            _facesToDelete = new HashSet<int>();
            _facesToDelete.Add(currentFace);

            for (int i = 0; i < breakline.Count - 1; i++)
            {
                int crossedEdge = -1;
                if (MeshTraversal.PointInMeshFace(ref baseTerrain, currentFace, breakline[i + 1]))
                    continue;

                var line = new Line(breakline[i], breakline[i + 1]);
                int intersection = MeshTraversal.FindNextFace(ref baseTerrain, currentFace, null, line, out crossedEdge);
                if (intersection == -1)
                    continue;
                else
                {
                    bool endPointContained = false;
                    while (!endPointContained)
                    {
                        if (intersection != -1)
                            currentFace = intersection;

                        _facesToDelete.Add(currentFace);
                        endPointContained = MeshTraversal.PointInMeshFace(ref baseTerrain, currentFace, breakline[i + 1]);
                        if (endPointContained)
                            continue;

                        intersection = MeshTraversal.FindNextFace(ref baseTerrain, currentFace, crossedEdge, line, out crossedEdge);
                        //endPointContained = true;
                    }

                }

            }


            var m = new Mesh();
            m.CopyFrom(baseTerrain);
            m.Faces.Clear();
            List<MeshFace> faces = new List<MeshFace>(_facesToDelete.Count);
            foreach (var face in _facesToDelete)
                faces.Add(baseTerrain.Faces[face]);

            m.Faces.AddFaces(faces);
            m.Compact();

            baseTerrain.Faces.ExtractFaces(_facesToDelete);


            var nakedEdges = m.GetNakedEdges();
            var patchMesh = new Mesh();
            if (nakedEdges != null)
            {
                var test = Brep.CreateTrimmedPlane(Plane.WorldXY, nakedEdges[0].ToPolylineCurve());

                patchMesh = Mesh.CreatePatch(nakedEdges[0], 0.1, test.Surfaces[0], null, new Curve[] { breakline.ToPolylineCurve() }, null, true, 10);
                patchMesh.RebuildNormals();

                baseTerrain.Append(patchMesh);
                baseTerrain.Compact();
            }


            //baseTerrain.Weld(Math.PI);

            Rhino.RhinoApp.WriteLine("Total: " + sw.ElapsedMilliseconds + " ms");
            sw.Stop();




            DA.SetData(0, baseTerrain);
            DA.SetData(1, nakedEdges[0]);

        }




        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("0abc77c2-fdc2-4aeb-8977-7de0434e40a8"); }
        }
    }
}

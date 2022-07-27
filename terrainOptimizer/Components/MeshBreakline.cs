using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Rhino.Geometry;



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
            pManager.AddMeshParameter("existing", "existing", "", GH_ParamAccess.item);
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


        // Used to store intersection results
        Dictionary<Point3d, bool> _vertices;
        Dictionary<Line, bool> _edges;

        RTree tree;
        Mesh baseTerrain;
        Curve proposedBreakline;



        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref baseTerrain);
            DA.GetData(1, ref proposedBreakline);


            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();


            Polyline breakline = proposedBreakline.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();



           // if (tree == null)
                tree = RTree.CreateMeshFaceTree(baseTerrain);

            _vertices = new Dictionary<Point3d, bool>();
            _edges = new Dictionary<Line, bool>();


            BoundingBox[] boxesOnBreakline = new BoundingBox[breakline.Count - 1];
            for (int i = 0; i < breakline.Count - 1; i++)
                boxesOnBreakline[i] = (new Polyline() { breakline[i], breakline[i + 1] }.BoundingBox);

            _facesToDelete = new HashSet<int>();
            foreach (var b in boxesOnBreakline)
                tree.Search(ScaleBoundingBox(b), FindFacesToDelete);



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
                //var reorderedOutline = ReorderPolylinePoints(breakline, nakedEdges[0]);
                //var patchMesh = CreateMeshFromBreakline(reorderedOutline, breakline);
                
                //var subdivided = nakedEdges[0].ToNurbsCurve().ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

                //List<Point3d> points = new List<Point3d>();
                //points.AddRange(subdivided);
                //points.AddRange(breakline);

                patchMesh = Mesh.CreatePatch(nakedEdges[0], 0.1, null, null, new Curve[] { proposedBreakline }, null, false, 100);
                patchMesh.RebuildNormals();

                //var patchMesh = Mesh.CreateFromTessellation(points, new Polyline[] { subdivided, breakline }, Plane.WorldXY, false);
                baseTerrain.Append(patchMesh);
                baseTerrain.Compact();
            }

            
            //baseTerrain.Weld(Math.PI);

            Rhino.RhinoApp.WriteLine("Total: " + sw.ElapsedMilliseconds + " ms");
            sw.Stop();




            DA.SetData(0, patchMesh);
            DA.SetData(1, nakedEdges[0]);
            //DA.SetData(1, platform);


        }



        private Point3d[] ReorderPolylinePoints(Polyline breakline, Polyline nakedClosedEdge)
        {
            int splitPoint = nakedClosedEdge.ClosestIndex(breakline[0]);

            Point3d[] sorted = new Point3d[nakedClosedEdge.Count];
            nakedClosedEdge.CopyTo(splitPoint, sorted, 0, nakedClosedEdge.Count - splitPoint);
            nakedClosedEdge.CopyTo(0, sorted, nakedClosedEdge.Count - splitPoint, splitPoint);

            return sorted;
        }

        private Mesh CreateMeshFromBreakline(Point3d[] outsidePolyline, Polyline breakline)
        {
            int count = outsidePolyline.Length;
            
            Mesh mesh = new Mesh();
            mesh.Vertices.AddVertices(outsidePolyline);
            mesh.Vertices.AddVertices(breakline);

            
            int current = count;
            int middleIndex = count + breakline.Count - 2;
            int step = 1;
            mesh.Faces.AddFace(0, 1, current);
            for (int i = 1; i < count; i++)
            {
                if (current > middleIndex)
                    step = -1;

                Point3d point = mesh.Vertices[i];
                if ((step < 0 && current == count) || point.DistanceToSquared(mesh.Vertices[current]) < point.DistanceToSquared(mesh.Vertices[current + step]))
                    mesh.Faces.AddFace(i, i + 1, current);
                else
                {
                    mesh.Faces.AddFace(i, current + step, current);
                    mesh.Faces.AddFace(i, i + 1, current + step);
                    current += step;
                }
            }
            mesh.Faces.AddFace(count - 1, 0, count);
            mesh.RebuildNormals();
            
            return mesh;
        }

        private BoundingBox ScaleBoundingBox(BoundingBox bbox)
        {
            bbox.Min = new Point3d(bbox.Min.X, bbox.Min.Y, -1000000);
            bbox.Max = new Point3d(bbox.Max.X, bbox.Max.Y, 1000000);
            return bbox;
        }



        void FindFacesToDelete(object sender, RTreeEventArgs e)
        {
            _facesToDelete.Add(e.Id);
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

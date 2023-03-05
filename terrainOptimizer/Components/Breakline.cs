using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer
{
    public class Breakline : GH_Component
    {
        public Breakline()
          : base("Breakline", "Breakline",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("breakline", "breakline", "", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;

        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("crv", "crv", "", GH_ParamAccess.item);
        }

        Mesh _baseTerrain;
        Curve _breakline;


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref _baseTerrain);
            DA.GetData(1, ref _breakline);


            Polyline insidePolyline;

            if (_breakline.IsPolyline())
                _breakline.TryGetPolyline(out insidePolyline);
            else
                insidePolyline = _breakline.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

            //Polyline insidePolyline = _breakline.ToPolyline(0.01, 0.01, 0.1, 10).ToPolyline();
            double[] polyline = new double[insidePolyline.Count * 3];
            for (int i = 0; i < insidePolyline.Count; i++)
            {
                int j = i * 3;
                polyline[j] = insidePolyline[i].X;
                polyline[j + 1] = insidePolyline[i].Y;
                polyline[j + 2] = insidePolyline[i].Z;
            }

            Rhino.Geometry.Intersect.Intersection.MeshRay(_baseTerrain, new Ray3d(new Point3d(insidePolyline[0].X, insidePolyline[0].Y, -999), Vector3d.ZAxis), out int[] intersectedFaces);

            if (intersectedFaces.Length == 0)
                return;


            var m = NativeMethods.CreateMeshFromFloatArray(_baseTerrain.Faces.ToIntArray(true), _baseTerrain.Faces.Count * 3);
            var verts = NativeMethods.CreateVertexGeometry(m, _baseTerrain.Vertices.ToFloatArray(), _baseTerrain.Vertices.Count * 3);
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();
            NativeMethods.ProjectPolylineToMesh(m, verts, polyline, intersectedFaces[0], polyline.Length);

            var f = NativeMethods.GCFacesToIntArray(m);
            var v = NativeMethods.GCVerticesToFloatArray(m, verts);
            var fi = NativeMethods.GCFacesCount(m);
            var vi = NativeMethods.GCVerticesCount(m);

            int[] faces = new int[fi];
            Marshal.Copy(f, faces, 0, fi);

            float[] vertices = new float[vi];
            Marshal.Copy(v, vertices, 0, vi);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"C++ Logic (New Method): {sw.ElapsedMilliseconds} ms");

            //NativeMethods.DeleteFacesArray(f);
            //NativeMethods.DeleteVertexArray(v);

            var result = new Mesh();
            for (int i = 0; i < fi; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < vi; i += 3)
                result.Vertices.Add(vertices[i], vertices[i + 1], vertices[i + 2]);
            result.RebuildNormals();

            DA.SetData(0, result);
            DA.SetData(1, insidePolyline.ToNurbsCurve());

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
            get { return new Guid("0abc97c2-fdc2-4aeb-8977-7de0219e40a8"); }
        }
    }
}

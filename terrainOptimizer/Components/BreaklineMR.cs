using System;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer
{
    public class BreaklineMR : GH_Component
    {
        public BreaklineMR()
          : base("BreaklineMR", "BreaklineMR",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("breakline", "breakline", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("reset", "reset", "", GH_ParamAccess.item);

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
        IntPtr meshA = IntPtr.Zero;


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref _baseTerrain);
            DA.GetData(1, ref _breakline);
            bool reset = false;
            DA.GetData(2, ref reset);
            //if (reset)
                meshA = IntPtr.Zero;

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            if (meshA == IntPtr.Zero)
            {
                meshA = NativeMethods.CreateMesh(_baseTerrain.Faces.ToIntArray(true), _baseTerrain.Faces.Count * 3, _baseTerrain.Vertices.ToFloatArray(), _baseTerrain.Vertices.Count * 3);
                sw.Stop();
                Rhino.RhinoApp.WriteLine($"Create Base Mesh: {sw.ElapsedMilliseconds} ms");
                sw.Restart();
            }


            Polyline insidePolyline;

            if (_breakline.IsPolyline())
                _breakline.TryGetPolyline(out insidePolyline);
            else
                insidePolyline = _breakline.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

            if (_breakline.IsClosed)
                insidePolyline.Add(insidePolyline[0]);

            float[] polyline = new float[insidePolyline.Count * 3];
            for (int i = 0; i < insidePolyline.Count; i++)
            {
                int j = i * 3;
                polyline[j] = (float)insidePolyline[i].X;
                polyline[j + 1] = (float)insidePolyline[i].Y;
                polyline[j + 2] = (float)insidePolyline[i].Z;
            }

            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Convert Polyline: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var p = NativeMethods.CutMeshWithPolyline(meshA, polyline, polyline.Length);

            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Cut Mesh: {sw.ElapsedMilliseconds} ms");

            int[] faces = new int[p.FacesLength];
            Marshal.Copy(p.Faces, faces, 0, p.FacesLength);

            float[] verts = new float[p.VerticesLength];
            Marshal.Copy(p.Vertices, verts, 0, p.VerticesLength);


            var result = new Mesh();
            for (int i = 0; i < p.FacesLength; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < p.VerticesLength; i += 3)
                result.Vertices.Add(verts[i], verts[i + 1], verts[i + 2]);
            //result.RebuildNormals();

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
            get { return new Guid("7abc57c2-fdc2-4aeb-8977-7de0219e40a8"); }
        }
    }
}

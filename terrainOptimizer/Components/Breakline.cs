using System;
using System.Linq;
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
            pManager.AddIntegerParameter("type", "type", "", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
        }


        IntPtr meshA = IntPtr.Zero;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh baseMesh = null;
            Curve breakline = null;
            int direction = 0;
            DA.GetData(0, ref baseMesh);
            DA.GetData(1, ref breakline);
            DA.GetData(2, ref direction);

            if (meshA == IntPtr.Zero)
                meshA = MeshApi.CreateMesh(baseMesh.Faces.ToIntArray(true), baseMesh.Faces.Count * 3, baseMesh.Vertices.ToFloatArray(), baseMesh.Vertices.Count * 3);


            Polyline insidePolyline;

            if (breakline.IsPolyline())
                breakline.TryGetPolyline(out insidePolyline);
            else
                insidePolyline = breakline.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

            if (breakline.IsClosed)
                insidePolyline.Add(insidePolyline[0]);

            float[] polyline = new float[insidePolyline.Count * 3];
            for (int i = 0; i < insidePolyline.Count; i++)
            {
                int j = i * 3;
                polyline[j] = (float)insidePolyline[i].X;
                polyline[j + 1] = (float)insidePolyline[i].Y;
                polyline[j + 2] = (float)insidePolyline[i].Z;
            }

            var mesh = MeshApi.CutMeshWithPolyline(meshA, polyline, polyline.Length, (MeshApi.CuttingOperation)direction);

            int[] faces = new int[mesh.FacesLength];
            Marshal.Copy(mesh.Faces, faces, 0, mesh.FacesLength);

            float[] verts = new float[mesh.VerticesLength];
            Marshal.Copy(mesh.Vertices, verts, 0, mesh.VerticesLength);

            var result = new Mesh();
            for (int i = 0; i < mesh.FacesLength; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < mesh.VerticesLength; i += 3)
                result.Vertices.Add(verts[i], verts[i + 1], verts[i + 2]);
            //result.RebuildNormals();

            DA.SetData(0, result);
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

using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer
{
    public class SoapFilm : GH_Component
    {
        public SoapFilm()
          : base("SoapFilm", "SoapFilm",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("breakline", "breakline", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("edgeLength", "edgeLength", "", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve breakline = null;
            double edgeLength = 0;
            DA.GetData(0, ref breakline);
            DA.GetData(1, ref edgeLength);

            Polyline baseCurve;

            if (breakline.IsPolyline())
                breakline.TryGetPolyline(out baseCurve);
            else
                baseCurve = breakline.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

            if (breakline.IsClosed)
                baseCurve.RemoveAt(baseCurve.Count - 1);

            float[] polyline = new float[baseCurve.Count * 3];
            for (int i = 0; i < baseCurve.Count; i++)
            {
                int j = i * 3;
                polyline[j] = (float)baseCurve[i].X;
                polyline[j + 1] = (float)baseCurve[i].Y;
                polyline[j + 2] = (float)baseCurve[i].Z;
            }

            var mesh = MeshApi.SoapFilm(polyline, polyline.Length, (float)edgeLength);

            int[] faces = new int[mesh.FacesLength];
            Marshal.Copy(mesh.Faces, faces, 0, mesh.FacesLength);

            float[] verts = new float[mesh.VerticesLength];
            Marshal.Copy(mesh.Vertices, verts, 0, mesh.VerticesLength);

            var result = new Mesh();
            for (int i = 0; i < mesh.FacesLength; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < mesh.VerticesLength; i += 3)
                result.Vertices.Add(verts[i], verts[i + 1], verts[i + 2]);

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
            get { return new Guid("7abc23c2-fdc2-4aeb-8977-6de0219e40a8"); }
        }
    }
}

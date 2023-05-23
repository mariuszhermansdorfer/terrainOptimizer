using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class AlphaWrap : GH_Component
    {

        public AlphaWrap()
          : base("alphaWrap", "alphaWrap",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("alpha", "alpha", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("offset", "offset", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
        }

        Mesh result;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            double alpha = 0.0;
            double offset = 0.0;
            DA.GetData(0, ref mesh);
            DA.GetData(1, ref alpha);
            DA.GetData(2, ref offset);

            var p = NativeMethods.TestWrap(mesh.Vertices.ToFloatArray(), mesh.Vertices.Count * 3, mesh.Faces.ToIntArray(true), mesh.Faces.Count * 3, alpha, offset);
            Rhino.RhinoApp.WriteLine(p.FacesLength.ToString());

            int[] faces = new int[p.FacesLength];
            System.Runtime.InteropServices.Marshal.Copy(p.Faces, faces, 0, p.FacesLength);

            float[] verts = new float[p.VerticesLength];
            System.Runtime.InteropServices.Marshal.Copy(p.Vertices, verts, 0, p.VerticesLength);

            result = new Mesh();
            for (int i = 0; i < p.FacesLength; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < p.VerticesLength; i += 3)
                result.Vertices.Add(verts[i], verts[i + 1], verts[i + 2]);

            DA.SetData(0, result);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("B5966C8D-BF3B-4357-BE77-6CE47236E989"); }
        }
    }
}
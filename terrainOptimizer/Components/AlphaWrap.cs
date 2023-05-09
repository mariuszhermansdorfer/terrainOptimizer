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
            pManager.AddBooleanParameter("run", "run", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            DA.GetData(0, ref run);

            if (!run)
                return;

            Mesh mesh = null;
            DA.GetData(1, ref mesh);

            var p = NativeMethods.TestWrap(mesh.Vertices.ToFloatArray(), mesh.Vertices.Count * 3, mesh.Faces.ToIntArray(true), mesh.Faces.Count * 3);
            Rhino.RhinoApp.WriteLine(p.FacesLength.ToString());

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
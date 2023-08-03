using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class FlowLines : GH_Component
    {
        public FlowLines()
          : base("FlowLines", "FlowLines",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("target", "target", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

        }
        IntPtr meshMR = IntPtr.Zero;

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Mesh mesh = new Mesh();
            double target = 0;

            DA.GetData(0, ref mesh);
            DA.GetData(1, ref target);


            if (meshMR == IntPtr.Zero)
                meshMR = MeshApi.CreateMesh(mesh.Faces.ToIntArray(true), mesh.Faces.Count * 3, mesh.Vertices.ToFloatArray(), mesh.Vertices.Count * 3);

            MeshApi.AnalyzeFlow(meshMR, (float) target);
          
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
            get { return new Guid("57D7B406-4367-451E-BAB6-442CCEF46F52"); }
        }
    }
}
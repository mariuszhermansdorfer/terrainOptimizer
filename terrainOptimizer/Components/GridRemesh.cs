using System;
using Grasshopper.Kernel;

using Rhino.Geometry;
using Geometry;




namespace terrainOptimizer.Components
{
    public class GridRemesh : GH_Component
    {

        public GridRemesh()
          : base("GridRemesh", "GridRemesh",
              "Description",
              "PHD", "Remesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("resolution", "resolution", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = new Mesh();
            double resolution = 0;

            DA.GetData(0, ref mesh);
            DA.GetData(1, ref resolution);


            var fMesh = new FastMesh(mesh);
            var grid = fMesh.GridRemesh((float)resolution);

            DA.SetData(0, grid.ToRhinoMesh());
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
            get { return new Guid("7FB5802C-232A-4E18-435E-E18C2E2F7596"); }
        }
    }
}
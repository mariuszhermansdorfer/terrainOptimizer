using System;
using Grasshopper.Kernel;

using Rhino.Geometry;
using MeshAPI;




namespace terrainOptimizer.Components
{
    public class GridRemeshWithCurve : GH_Component
    {

        public GridRemeshWithCurve()
          : base("GridRemeshWithCurve", "GridRemeshWithCurve",
              "Description",
              "PHD", "Remesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("resolution", "resolution", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("curve", "curve", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = new Mesh();
            double resolution = 0;
            Curve curve = null;

            DA.GetData(0, ref mesh);
            DA.GetData(1, ref resolution);
            DA.GetData(2, ref curve);


            var fMesh = new FastMesh(mesh);
            var grid = fMesh.GridRemeshWithCurve((float)resolution, curve);

            //var grid = FastMesh.CurveToGridMesh((float)resolution, curve);

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
            get { return new Guid("3FB5802C-777A-4E18-435E-E18C2E2F7596"); }
        }
    }
}
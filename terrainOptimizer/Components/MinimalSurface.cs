using System;
using Grasshopper.Kernel;
using Geometry;
using Rhino.Geometry;


namespace terrainOptimizer
{
    public class MinimalSurface : GH_Component
    {
        public MinimalSurface()
          : base("MinimalSurface", "MinimalSurface",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("curve", "curve", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("edgeLength", "edgeLength", "", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve inputCurve = null;
            double edgeLength = 0;

            DA.GetData(0, ref inputCurve);
            DA.GetData(1, ref edgeLength);

            var mesh = FastMesh.MinimalSurface(inputCurve, (float)edgeLength);
            DA.SetData(0, mesh.ToRhinoMesh());
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

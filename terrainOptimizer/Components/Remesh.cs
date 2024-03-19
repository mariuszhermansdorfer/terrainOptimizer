using System;
using Grasshopper.Kernel;
using Geometry;
using Rhino.Geometry;

namespace terrainOptimizer.Components
{
    public class Remesh : GH_Component
    {
        public Remesh()
          : base("remesh", "remesh",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("target", "target", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("iters", "iters", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("shift", "shift", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("sharpAngle", "sharpAngle", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Mesh mesh = new Mesh();
            double target = 0;
            int iterations = 0;
            double shift = 0;
            double sharpAngle = 0;
            DA.GetData(0, ref mesh);
            DA.GetData(1, ref target);
            DA.GetData(2, ref iterations);
            DA.GetData(3, ref shift);
            DA.GetData(4, ref sharpAngle);


            var fastMesh = new FastMesh(mesh);
            var result = fastMesh.Remesh((float)target, (float)shift, iterations, (float)sharpAngle);

            DA.SetData(0, result.ToRhinoMesh());
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
            get { return new Guid("57D7B604-4367-451E-BAB6-442CCEF46F52"); }
        }
    }
}
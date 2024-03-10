using System;
using Grasshopper.Kernel;
using MeshAPI;
using Rhino.Geometry;

namespace terrainOptimizer.Components
{
    public class Inflate : GH_Component
    {

        public Inflate()
          : base("Inflate", "Inflate",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("iterations", "iterations", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("pressure", "pressure", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = new Mesh();
            DA.GetData(0, ref mesh);

            int iterations = 0;
            double pressure = 0;

            DA.GetData(1, ref iterations);
            DA.GetData(2, ref pressure);

            var meshA = new FastMesh(mesh);
            var result = meshA.Inflate(iterations, (float) pressure);

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
            get { return new Guid("2FB7456C-745A-4E58-939E-E18C2E2F7596"); }
        }
    }
}
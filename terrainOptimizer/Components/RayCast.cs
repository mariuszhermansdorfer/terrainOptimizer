using System;
using Rhino.Geometry;
using Grasshopper.Kernel;
using terrainOptimizer.Helpers;
using System.Collections.Generic;
using MeshAPI;



namespace terrainOptimizer.Components
{
    public class RayCast : GH_Component
    {

        public RayCast()
          : base("RayCast", "RayCast",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("base", "base", "", GH_ParamAccess.item);
            pManager.AddPointParameter("samples", "samples", "", GH_ParamAccess.list);
            pManager.AddVectorParameter("directions", "dir", "", GH_ParamAccess.list);
            pManager.AddBooleanParameter("useGPU", "useGPU", "", GH_ParamAccess.item, false);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("result", "result", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh contextMesh = new Mesh();
            List<Point3d> samples = new List<Point3d>();
            List<Vector3d> directions = new List<Vector3d>();
            bool useGPU = false;
            DA.GetData(0, ref contextMesh);
            DA.GetDataList(1, samples);
            DA.GetDataList(2, directions);
            DA.GetData(3, ref useGPU);

            var mesh = new FastMesh(contextMesh);
            var result = RayCasting.RayCastOcclusions(mesh, samples.ToArray(), directions.ToArray(), useGPU);
            var res = RayCasting.RayCastIntersections(mesh, samples.ToArray(), directions.ToArray(), useGPU);

            //DA.SetDataList(0, result);

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
            get { return new Guid("2FB5801C-160A-4E58-939E-E18C2E2F7596"); }
        }
    }
}
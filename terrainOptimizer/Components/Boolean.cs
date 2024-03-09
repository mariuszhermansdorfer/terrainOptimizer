using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using MeshAPI;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class Boolean : GH_Component
    {

        public Boolean()
          : base("boolean", "boolean",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("type", "type", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("base", "base", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("cutter", "cutter", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int type = 0;
            DA.GetData(0, ref type);

            Mesh baseMesh = new Mesh();
            DA.GetData(1, ref baseMesh);

            Mesh cutter = new Mesh();
            DA.GetData(2, ref cutter);

            
            var meshA = new FastMesh(baseMesh);
            var meshB = new FastMesh(cutter);

            var result = meshA.BooleanMeshes(meshB, (Structs.BooleanOperation)type);

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
            get { return new Guid("2FB4508C-150A-4E58-939E-E18C2E2F7596"); }
        }
    }
}
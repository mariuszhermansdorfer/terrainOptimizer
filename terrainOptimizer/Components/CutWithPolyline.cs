using System;
using Grasshopper.Kernel;
using MeshAPI;
using Rhino.Geometry;

namespace terrainOptimizer
{
    public class CutWithPolyline : GH_Component
    {
        public CutWithPolyline()
          : base("CutWithPolyline", "CutWithPolyline",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("breakline", "breakline", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("type", "type", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("project", "project", "", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh baseMesh = null;
            Curve breakline = null;
            int direction = 0;
            bool project = false;
            DA.GetData(0, ref baseMesh);
            DA.GetData(1, ref breakline);
            DA.GetData(2, ref direction);
            DA.GetData(3, ref project);

            var mesh = new FastMesh(baseMesh);
            var result = mesh.CutWithPolyline(breakline, (Structs.CuttingOperation)direction, project);

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
            get { return new Guid("7abc57c2-fdc2-4aeb-8977-7de0219e40a8"); }
        }
    }
}

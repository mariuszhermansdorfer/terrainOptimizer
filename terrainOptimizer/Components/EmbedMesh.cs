using System;
using Grasshopper.Kernel;
using Geometry;
using Rhino.Geometry;

namespace terrainOptimizer.Components
{
    public class EmbedMesh : GH_Component
    {

        public EmbedMesh()
          : base("EmbedMesh", "EmbedMesh",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("base", "base", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("cutter", "cutter", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("fillAngle", "fillAngle", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("cutAngle", "cutAngle", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("anglePrecision", "anglePrecision", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh baseMesh = new Mesh();
            DA.GetData(0, ref baseMesh);

            Mesh cutter = new Mesh();
            DA.GetData(1, ref cutter);

            double fillAngle = 0;
            double cutAngle = 0;
            double anglePrecision = 0;

            DA.GetData(2, ref fillAngle);
            DA.GetData(3, ref cutAngle);
            DA.GetData(4, ref anglePrecision);

            var meshA = new FastMesh(baseMesh);
            var meshB = new FastMesh(cutter);

            var result = meshA.EmbedMesh(meshB, (float)fillAngle, (float)cutAngle, (float)anglePrecision);

            var r = result.ToRhinoMesh();

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
            get { return new Guid("2FB7808C-150A-4E58-939E-E18C2E2F7596"); }
        }
    }
}
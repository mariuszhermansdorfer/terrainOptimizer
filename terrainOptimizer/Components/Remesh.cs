using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

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
            pManager.AddBooleanParameter("run", "run", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("target", "target", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("iters", "iters", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("shift", "shift", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("sharpAngle", "sharpAngle", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("MeshLib", "MeshLib", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            DA.GetData(0, ref run);


            Mesh mesh = new Mesh();
            double target = 0;
            int iterations = 0;
            double shift = 0;
            double sharpAngle = 0;
            DA.GetData(1, ref mesh);
            DA.GetData(2, ref target);
            DA.GetData(3, ref iterations);
            DA.GetData(4, ref shift);
            DA.GetData(5, ref sharpAngle);


            var faces = mesh.Faces.ToIntArray(true);
            var vertices = mesh.Vertices.ToFloatArray();

            var meshMR = NativeMeshMethods.CreateMesh(faces, faces.Length, vertices, vertices.Length);
            var pMR = NativeMeshMethods.RemeshMesh(meshMR, (float)target, (float)shift, iterations, (float)sharpAngle);


            int[] facesMR = new int[pMR.FacesLength];
            Marshal.Copy(pMR.Faces, facesMR, 0, pMR.FacesLength);

            float[] vertsMR = new float[pMR.VerticesLength];
            Marshal.Copy(pMR.Vertices, vertsMR, 0, pMR.VerticesLength);


            var resultMR = new Mesh();
            for (int i = 0; i < pMR.FacesLength; i += 3)
                resultMR.Faces.AddFace(facesMR[i], facesMR[i + 1], facesMR[i + 2]);

            for (int i = 0; i < pMR.VerticesLength; i += 3)
                resultMR.Vertices.Add(vertsMR[i], vertsMR[i + 1], vertsMR[i + 2]);

            DA.SetData(0, resultMR);
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
            get { return new Guid("57D7B604-4367-451E-BAB6-442CCEF46F52"); }
        }
    }
}
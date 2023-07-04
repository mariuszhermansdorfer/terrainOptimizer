using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class DistanceMap : GH_Component
    {
        public DistanceMap()
          : base("DistanceMap", "DistanceMap",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("resolution", "resolution", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("distance", "distance", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {


            Mesh mesh = new Mesh();
            double resolution = 0;

            DA.GetData(0, ref mesh);
            DA.GetData(1, ref resolution);


            var faces = mesh.Faces.ToIntArray(true);
            var vertices = mesh.Vertices.ToFloatArray();

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            var meshMR = NativeMethods.CreateMesh(faces, faces.Length, vertices, vertices.Length);
            sw.Restart();
            var pMR = NativeMethods.Distance(meshMR, (float)resolution);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"MeshLib: {sw.ElapsedMilliseconds} ms");

            int[] resultFaces = new int[pMR.FacesLength];
            Marshal.Copy(pMR.Faces, resultFaces, 0, pMR.FacesLength);

            float[] resultVertices = new float[pMR.VerticesLength];
            Marshal.Copy(pMR.Vertices, resultVertices, 0, pMR.VerticesLength);


            var result = new Mesh();
            for (int i = 0; i < pMR.FacesLength; i += 3)
                result.Faces.AddFace(resultFaces[i], resultFaces[i + 1], resultFaces[i + 2]);

            for (int i = 0; i < pMR.VerticesLength; i += 3)
                result.Vertices.Add(resultVertices[i], resultVertices[i + 1], resultVertices[i + 2]);



            DA.SetData(0, result);
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
            get { return new Guid("5F3171E8-B984-4921-A9A1-8CD57A74CEBF"); }
        }
    }
}
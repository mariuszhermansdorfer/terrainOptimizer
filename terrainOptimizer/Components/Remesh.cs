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
            pManager.AddBooleanParameter("preserve", "preserve", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("iters", "iters", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("shift", "shift", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("sharpAngle", "sharpAngle", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("GC", "GC", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("CinoLib", "CinoLib", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("MeshLib", "MeshLib", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            DA.GetData(0, ref run);


            Mesh mesh = new Mesh();
            double target = 0;
            bool preserve = false;
            int iterations = 0;
            double shift = 0;
            double sharpAngle = 0;
            DA.GetData(1, ref mesh);
            DA.GetData(2, ref target);
            DA.GetData(3, ref preserve);
            DA.GetData(4, ref iterations);
            DA.GetData(5, ref shift);
            DA.GetData(6, ref sharpAngle);


            var faces = mesh.Faces.ToIntArray(true);
            var vertices = mesh.Vertices.ToFloatArray();

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            
            // Cino Lib

            var pointer = NativeMethods.Trimesh(faces, faces.Length, vertices, vertices.Length);
            sw.Restart();
            var p = NativeMethods.CinoRemesh(pointer, iterations + 1, target, preserve);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Cino: {sw.ElapsedMilliseconds} ms");


            int[] facesCino = new int[p.FacesLength];
            Marshal.Copy(p.Faces, facesCino, 0, p.FacesLength);

            float[] vertsCino = new float[p.VerticesLength];
            Marshal.Copy(p.Vertices, vertsCino, 0, p.VerticesLength);

            var resultCino = new Mesh();
            for (int i = 0; i < p.FacesLength; i += 3)
                resultCino.Faces.AddFace(facesCino[i], facesCino[i + 1], facesCino[i + 2]);

            for (int i = 0; i < p.VerticesLength; i += 3)
                resultCino.Vertices.Add(vertsCino[i], vertsCino[i + 1], vertsCino[i + 2]);


            // Geometry Central

            
            var m = NativeMethods.CreateMeshFromFloatArray(faces, faces.Length);
            var verts = NativeMethods.CreateVertexGeometry(m, vertices, vertices.Length);
            sw.Restart();
            NativeMethods.GCRemesh(m, verts, target, iterations + 1, 1);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Geometry Central: {sw.ElapsedMilliseconds} ms");
            var f = NativeMethods.GCFacesToIntArray(m);
            var v = NativeMethods.GCVerticesToFloatArray(m, verts);
            var fi = NativeMethods.GCFacesCount(m);
            var vi = NativeMethods.GCVerticesCount(m);

            int[] facesGC = new int[fi];
            Marshal.Copy(f, facesGC, 0, fi);

            float[] verticesGC = new float[vi];
            Marshal.Copy(v, verticesGC, 0, vi);


            var resultGC = new Mesh();
            for (int i = 0; i < fi; i += 3)
                resultGC.Faces.AddFace(facesGC[i], facesGC[i + 1], facesGC[i + 2]);

            for (int i = 0; i < vi; i += 3)
                resultGC.Vertices.Add(verticesGC[i], verticesGC[i + 1], verticesGC[i + 2]);

            // MRMesh
            var meshMR = NativeMethods.CreateMesh(faces, faces.Length, vertices, vertices.Length);
            sw.Restart();
            var pMR = NativeMethods.RemeshMesh(meshMR, (float)target, (float)shift, iterations, (float)sharpAngle);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"MeshLib: {sw.ElapsedMilliseconds} ms");

            int[] facesMR = new int[pMR.FacesLength];
            Marshal.Copy(pMR.Faces, facesMR, 0, pMR.FacesLength);

            float[] vertsMR = new float[pMR.VerticesLength];
            Marshal.Copy(pMR.Vertices, vertsMR, 0, pMR.VerticesLength);


            var resultMR = new Mesh();
            for (int i = 0; i < pMR.FacesLength; i += 3)
                resultMR.Faces.AddFace(facesMR[i], facesMR[i + 1], facesMR[i + 2]);

            for (int i = 0; i < pMR.VerticesLength; i += 3)
                resultMR.Vertices.Add(vertsMR[i], vertsMR[i + 1], vertsMR[i + 2]);


            // --------------------



            DA.SetData(0, resultGC);
            DA.SetData(1, resultCino);
            DA.SetData(2, resultMR);
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
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
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
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("mesh1", "mesh1", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            DA.GetData(0, ref run);


            Mesh mesh = new Mesh();
            double target = 0;
            bool preserve = false;
            int iterations = 0;
            DA.GetData(1, ref mesh);
            DA.GetData(2, ref target);
            DA.GetData(3, ref preserve);
            DA.GetData(4, ref iterations);

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            
            var pointer = NativeMethods.Trimesh(mesh.Faces.ToIntArray(true), mesh.Faces.Count * 3, mesh.Vertices.ToFloatArray(), mesh.Vertices.Count * 3);
            var p = NativeMethods.CinoRemesh(pointer, iterations, target, preserve);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Cino: {sw.ElapsedMilliseconds} ms");


            int[] faces1 = new int[p.FacesLength];
            Marshal.Copy(p.Faces, faces1, 0, p.FacesLength);

            float[] verts1 = new float[p.VerticesLength];
            Marshal.Copy(p.Vertices, verts1, 0, p.VerticesLength);

            var result1 = new Mesh();
            for (int i = 0; i < p.FacesLength; i += 3)
                result1.Faces.AddFace(faces1[i], faces1[i + 1], faces1[i + 2]);

            for (int i = 0; i < p.VerticesLength; i += 3)
                result1.Vertices.Add(verts1[i], verts1[i + 1], verts1[i + 2]);



            sw.Restart();
            var m = NativeMethods.CreateMeshFromFloatArray(mesh.Faces.ToIntArray(true), mesh.Faces.Count * 3);
            var verts = NativeMethods.CreateVertexGeometry(m, mesh.Vertices.ToFloatArray(), mesh.Vertices.Count * 3);
            NativeMethods.GCRemesh(m, verts, target, iterations, 1);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Geometry Central: {sw.ElapsedMilliseconds} ms");
            var f = NativeMethods.GCFacesToIntArray(m);
            var v = NativeMethods.GCVerticesToFloatArray(m, verts);
            var fi = NativeMethods.GCFacesCount(m);
            var vi = NativeMethods.GCVerticesCount(m);

            int[] faces = new int[fi];
            Marshal.Copy(f, faces, 0, fi);

            float[] vertices = new float[vi];
            Marshal.Copy(v, vertices, 0, vi);


            var result = new Mesh();
            for (int i = 0; i < fi; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < vi; i += 3)
                result.Vertices.Add(vertices[i], vertices[i + 1], vertices[i + 2]);


            // --------------------

            
            
            DA.SetData(0, result);
            DA.SetData(1, result1);
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
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class Raycasting : GH_Component
    {

        public Raycasting()
          : base("Raycasting", "Raycasting",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("run", "run", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("cutter", "cutter", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool run = false;
            DA.GetData(0, ref run);

            if (!run)
                return;
            Mesh mesh = null;
            Mesh cutter = null;
            DA.GetData(1, ref mesh);
            DA.GetData(2, ref cutter);

            System.IO.StreamWriter swr = new System.IO.StreamWriter("c:\\mesh.txt");
            var me = mesh.Vertices.ToFloatArray();
            foreach (var v in me)
                swr.WriteLine(v);

            var fa = mesh.Faces.ToIntArray(true);
            foreach (var v in fa)
                swr.WriteLine(v);

            swr.Close();

            Stopwatch sw = Stopwatch.StartNew();
            //NativeMethods.BVHTest();

            //sw.Stop();
            //Rhino.RhinoApp.WriteLine(sw.ElapsedMilliseconds.ToString());

            //sw.Restart();

            //NativeMethods.RaytracerTest(mesh.Vertices.ToFloatArray(), mesh.Vertices.Count * 3, mesh.Faces.ToIntArray(true), mesh.Faces.Count * 3);
            //sw.Stop();
            //Rhino.RhinoApp.WriteLine(sw.ElapsedMilliseconds.ToString());


            sw.Restart();
            NativeMethods.createBaseMesh(mesh.Vertices.ToFloatArray(), mesh.Vertices.Count * 3, mesh.Faces.ToIntArray(true), mesh.Faces.Count * 3,
                cutter.Vertices.ToFloatArray(), cutter.Vertices.Count * 3, cutter.Faces.ToIntArray(true), cutter.Faces.Count * 3);

            sw.Stop();
            Rhino.RhinoApp.WriteLine(sw.ElapsedMilliseconds.ToString());

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
            get { return new Guid("7CA84FCD-E059-4FD6-A611-37AD051EF99A"); }
        }
    }
}
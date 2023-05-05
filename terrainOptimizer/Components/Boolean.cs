using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Commands;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class Boolean : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the MyComponent1 class.
        /// </summary>
        public Boolean()
          : base("bool", "bool",
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

            Mesh baseTerrain = new Mesh();
            DA.GetData(1, ref baseTerrain);

            Mesh cutter = new Mesh();
            DA.GetData(2, ref cutter);

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
            var pointer = NativeMethods.CreateBoolMesh(baseTerrain.Faces.ToIntArray(true), baseTerrain.Faces.Count * 3, baseTerrain.Vertices.ToFloatArray(), baseTerrain.Vertices.Count * 3);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Create Base Mesh: {sw.ElapsedMilliseconds} ms");
            sw.Restart();
            var p = NativeMethods.CollideBaseWithCutter(pointer, cutter.Faces.ToIntArray(true), cutter.Faces.Count * 3, cutter.Vertices.ToFloatArray(), cutter.Vertices.Count * 3, type); 

            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Perform Boolean Operation: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            int[] faces = new int[p.FacesLength];
            Marshal.Copy(p.Faces, faces, 0, p.FacesLength);

            float[] verts = new float[p.VerticesLength];
            Marshal.Copy(p.Vertices, verts, 0, p.VerticesLength);

            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Copy Data Back: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var result = new Mesh();
            for (int i = 0; i < p.FacesLength; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < p.VerticesLength; i += 3)
                result.Vertices.Add(verts[i], verts[i + 1], verts[i + 2]);
            //result.RebuildNormals();

            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Rebuild mesh: {sw.ElapsedMilliseconds} ms");

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
            get { return new Guid("2FB3608C-150A-4E58-939E-E18C2E2F7596"); }
        }
    }
}
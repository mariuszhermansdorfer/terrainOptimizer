using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class OpenVDB : GH_Component
    {

        public OpenVDB()
          : base("OpenVDB", "OpenVDB",
              "Description",
              "PHD", "Subcategory")
        {
        }


        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("run", "run", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddBoxParameter("bbox", "bbox", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("voxelSize", "voxelSize", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
        }


        bool run;
        Mesh mesh;
        Box bBox;
        double voxelSize;
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            if (!DA.GetData(0, ref run))
                return;
            if (!run)
                return;

            DA.GetData(1, ref mesh);
            DA.GetData(2, ref bBox);
            DA.GetData(3, ref voxelSize);

            var min = bBox.BoundingBox.Min;
            var max = bBox.BoundingBox.Max;

            float[] verts = mesh.Vertices.ToFloatArray();
            int[] faces = mesh.Faces.ToIntArray(true);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            //NativeMethods.CreateMeshGridFromBoundingBox();

            var transform = NativeMethods.CreateTransform((float)voxelSize);
            var floatGrid = NativeMethods.CreateFloatGrid(transform);
            var bbox = NativeMethods.CreateBoundingBox(floatGrid, (float)min.X, (float)min.Y, (float)min.Z, (float)max.X, (float)max.Y, (float)max.Z);
            var meshGrid = NativeMethods.CreateMeshGrid(transform, verts, verts.Length, faces, faces.Length);
            NativeMethods.MergeGridsAndOutput((float)voxelSize, floatGrid, meshGrid, bbox);
            NativeMethods.DeleteBoundingBox(bbox);
            NativeMethods.DeleteFloatGrid(floatGrid);

            sw.Stop();
            Rhino.RhinoApp.WriteLine("VDB: " + sw.ElapsedMilliseconds);

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
            get { return new Guid("822B5464-A132-4F29-AA3E-C22D49A6C837"); }
        }
    }
}
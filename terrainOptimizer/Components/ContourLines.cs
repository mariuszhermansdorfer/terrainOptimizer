using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer
{
    public class IO : GH_Component
    {
        public IO()
          : base("ContourLines", "ContourLines",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("interval", "interval", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("spacing", "spacing", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("showLabels", "showLabels", "", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("contours", "contours", "", GH_ParamAccess.list);
            pManager.AddTextParameter("labels", "labels", "", GH_ParamAccess.list);
        }

        bool showLabels;
        IntPtr meshA = IntPtr.Zero;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh baseMesh = null;
            double interval = 0;
            double spacing = 0;
            showLabels = false;

            DA.GetData(0, ref baseMesh);
            DA.GetData(1, ref interval);
            DA.GetData(2, ref spacing);
            DA.GetData(3, ref showLabels);

            if (meshA == IntPtr.Zero)
                meshA = MeshApi.CreateMesh(baseMesh.Faces.ToIntArray(true), baseMesh.Faces.Count * 3, baseMesh.Vertices.ToFloatArray(), baseMesh.Vertices.Count * 3);
            var sw = Stopwatch.StartNew();
            var contours = MeshApi.CreateContours(meshA, (float)interval, showLabels, (float)spacing);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Contours: {sw.ElapsedMilliseconds} ms");

            int[] contourLengths = new int[contours.ContourCount];
            Marshal.Copy(contours.ContourVerticesLengths, contourLengths, 0, contours.ContourCount);

            List<Polyline> polylines = new List<Polyline>();
            int offset = 0;
            foreach (var length in contourLengths)
            {
                float[] verts = new float[length];
                Marshal.Copy(contours.ContourVertices + offset * sizeof(float), verts, 0, length);
                offset += length;
                
                Polyline polyline = new Polyline();
                for (int i = 0; i < length; i += 3)
                {
                    polyline.Add(new Point3f(verts[i], verts[i + 1], verts[i + 2]));
                }
                polylines.Add(polyline);
            }
            DA.SetDataList(0, polylines);

            if (!showLabels)
                return;

            float[] labels = new float[contours.LabelCount];
            Marshal.Copy(contours.LabelVertices, labels, 0, contours.LabelCount);

            float[] normals = new float[contours.LabelCount];
            Marshal.Copy(contours.LabelNormals, normals, 0, contours.LabelCount);

            labelsText = new List<Text3d>();
            for (int i = 0; i < contours.LabelCount; i += 3)
            {
                Point3d pt = new Point3d(labels[i], labels[i + 1], labels[i + 2]);
                Vector3d n = new Vector3d(normals[i], normals[i + 1], normals[i + 2]);
                n.Unitize();
                var plane = GetOrientedPlane(n, pt + n * 0.02);
                Text3d t = new Text3d(labels[i + 2].ToString("0.00"), plane, 0.08);
                labelsText.Add(t);
            }

            
        }
        List<Text3d> labelsText;
        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);
            if (showLabels)
            {
                foreach (var t in labelsText)
                    args.Display.Draw3dText(t, System.Drawing.Color.DarkRed);
            }

        }

        public static Plane GetOrientedPlane(Vector3d normal, Point3d point)
        {
            normal.Reverse();
            var up = Vector3d.ZAxis;
            if (normal.IsParallelTo(up) != 0)
                up = Vector3d.YAxis;
            var side = Vector3d.CrossProduct(normal, up);
            side.Unitize();
            up = Vector3d.CrossProduct(side, normal);
            up.Unitize();

            return new Plane(point, side, up);
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
            get { return new Guid("7abc48c2-fdc2-4aeb-8977-7de0219e40a8"); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Geometry.SpatialTrees;
using Rhino.Geometry;
using Rhino.Render.ChangeQueue;
using terrainOptimizer.Components;
using terrainOptimizer.Helpers;
using static terrainOptimizer.Helpers.ClipperApi;

namespace terrainOptimizer
{
    public class Offset : GH_Component
    {
        public Offset()
          : base("Offset", "Offset",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("curve", "curve", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("offset", "offset", "", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("result", "result", "", GH_ParamAccess.list);
        }



        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve breakline = null;
            double offset = 0;
            DA.GetData(0, ref breakline);
            DA.GetData(1, ref offset);


            Polyline insidePolyline;

            if (breakline.IsPolyline())
                breakline.TryGetPolyline(out insidePolyline);
            else
                insidePolyline = breakline.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

            //if (breakline.IsClosed)
            //    insidePolyline.Add(insidePolyline[0]);

            //insidePolyline.RemoveAt(insidePolyline.Count - 1);

            //double[] coordinates = new double[insidePolyline.Count * 3];

            //for (int i = 0, j = 0; i < insidePolyline.Count; i++, j += 3)
            //{
            //    coordinates[j] = insidePolyline[i].X;
            //    coordinates[j + 1] = insidePolyline[i].Y;
            //    coordinates[j + 2] = insidePolyline[i].Z;
            //}

            var sw = System.Diagnostics.Stopwatch.StartNew();
            //var path = ClipperApi.CreateClipperPath(coordinates, coordinates.Length, ClipperApi.Dim.ThreeD);

            //var simple = ClipperApi.Simplify(path, Dim.ThreeD, 0.03);
            //double[] verts1 = new double[simple.VerticesLength];
            //Marshal.Copy(simple.Vertices, verts1, 0, simple.VerticesLength);
            //sw.Stop();
            //Rhino.RhinoApp.WriteLine($"Simplify {sw.ElapsedMilliseconds} ms");
            //sw.Restart();
            //float[] polyline1 = new float[simple.VerticesLength / 3 * 2 + 2];

            //for (int i = 0, j = 0; i < simple.VerticesLength; i +=3, j += 2)
            //{
            //    polyline1[j] = (float)verts1[i];
            //    polyline1[j + 1] = (float)verts1[i + 1];
            //}

            //polyline1[polyline1.Length - 2] = polyline1[0];
            //polyline1[polyline1.Length - 1] = polyline1[1];

            float[] polyline = new float[insidePolyline.Count * 2];
            for (int i = 0, j = 0; i < insidePolyline.Count; i++, j += 2)
            {
                polyline[j] = (float)insidePolyline[i].X;
                polyline[j + 1] = (float)insidePolyline[i].Y;
               // polyline[j + 2] = (float)insidePolyline[i].Z;
            }

            
            var test = MeshApi.Offset(polyline, polyline.Length, (float)offset);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Offset {sw.ElapsedMilliseconds} ms");

            float[] verts = new float[test.VerticesLength * 2];
            Marshal.Copy(test.Vertices, verts, 0, test.VerticesLength * 2);

            int[] resultingPolylines = new int[test.FacesLength];
            Marshal.Copy(test.Faces, resultingPolylines, 0, test.FacesLength);

            List<Polyline> polylines = new List<Polyline>();

            int dataOffset = 0;
            for (int j = 0; j < test.FacesLength; j++)
            {
                Polyline pts = new Polyline();
                for (int i = 0; i < resultingPolylines[j] * 2; i += 2)
                {
                    pts.Add(new Point3d(verts[dataOffset + i], verts[dataOffset + i + 1], 0));
                }
                polylines.Add(pts);
                dataOffset += resultingPolylines[j] * 2;
            }

            
            //MeshApi.RawPolylinePointers ptr; // = new MeshApi.RawPolylinePointers();
            //var poly = insidePolyline.ToArray();
            //Polyline p;
            //unsafe
            //{
            //    fixed (Point3d* polyPointer = poly)
            //    {
            //        ptr = MeshApi.Offset(polyPointer, poly.Length, (float)offset);
            //    }

            //    var points = new Point3d[ptr.VerticesLength];

            //    fixed (Point3d* pointsPointer = points)
            //    {
            //        int* sourcePtr = (int*)ptr.Vertices;
            //        int* destPtr = (int*)pointsPointer;

            //        for (int i = 0; i < ptr.VerticesLength; i++)
            //        {
            //            *destPtr++ = *sourcePtr++;
            //            *destPtr++ = *sourcePtr++;
            //            destPtr++;
            //        }
            //    }
            //    p = new Polyline(points);
            //}

            DA.SetDataList(0, polylines);
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
            get { return new Guid("7abc57c4-fdc2-4aeb-8957-7de0219e40a8"); }
        }
    }
}

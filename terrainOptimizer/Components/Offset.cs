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
            pManager.AddCurveParameter("curve", "curve", "", GH_ParamAccess.list);
            pManager.AddNumberParameter("offset", "offset", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("epsilon", "epsilon", "", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("result", "result", "", GH_ParamAccess.list);
        }



        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Curve> breaklines = new List<Curve>();
            double offset = 0;
            double epsilon = 0;
            DA.GetDataList(0, breaklines);
            DA.GetData(1, ref offset);
            DA.GetData(2, ref epsilon);

            List<Polyline> insidePolylines = new List<Polyline>();

            foreach (var breakline in breaklines)
            {
                Polyline insidePolyline;

                if (breakline.IsPolyline())
                    breakline.TryGetPolyline(out insidePolyline);
                else
                    insidePolyline = breakline.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();
                
                insidePolylines.Add(insidePolyline);
            }


            var sw = System.Diagnostics.Stopwatch.StartNew();


            List<float> coordinates = new List<float>();
            foreach (var poly in insidePolylines)
            {
                foreach (var point in poly)
                {
                    coordinates.Add((float)point.X);
                    coordinates.Add((float)point.Y);
                    coordinates.Add((float)point.Z);
                }
            }

            int[] polylinesLength = new int[insidePolylines.Count];
            for (int i = 0; i < insidePolylines.Count; i++)
                polylinesLength[i] = insidePolylines[i].Count;
            
            var test = MeshApi.Offset(coordinates.ToArray(), insidePolylines.Count, polylinesLength, (float)offset, (float)epsilon, 10, 0.1f);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Offset {sw.ElapsedMilliseconds} ms");

            float[] verts = new float[test.VerticesLength * 3];
            Marshal.Copy(test.Vertices, verts, 0, test.VerticesLength * 3);

            int[] resultingPolylines = new int[test.FacesLength];
            Marshal.Copy(test.Faces, resultingPolylines, 0, test.FacesLength);

            List<Polyline> polylines = new List<Polyline>();

            int dataOffset = 0;
            for (int j = 0; j < test.FacesLength; j++)
            {
                Polyline pts = new Polyline();
                for (int i = 0; i < resultingPolylines[j] * 3; i += 3)
                {
                    pts.Add(new Point3d(verts[dataOffset + i], verts[dataOffset + i + 1], verts[dataOffset + i + 2]));
                }
                polylines.Add(pts);
                dataOffset += resultingPolylines[j] * 3;
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

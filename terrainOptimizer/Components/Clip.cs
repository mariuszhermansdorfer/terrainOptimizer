﻿using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class Clip : GH_Component
    {

        public Clip()
          : base("Clip", "Clip",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("crv", "crv", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("delta", "delta", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("miterLimit", "miterLimit", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("precision", "precision", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("arcTolerance", "arcTolerance", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("crv", "crv", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int scale = 1000;
            Curve crv = null;
            double delta = 0;
            double miterLimit = 0;
            int precision = 0;
            double arcTolerance = 0;
            DA.GetData(0, ref crv);
            DA.GetData(1, ref delta);
            DA.GetData(2, ref miterLimit);
            DA.GetData(3, ref precision);
            DA.GetData(4, ref arcTolerance);

            Polyline poly;

            if (crv.IsPolyline())
                crv.TryGetPolyline(out poly);
            else
                poly = crv.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

            float[] floats = new float[poly.Count * 3];

            int j = 0;
            for (int i = 0;i < poly.Count; i++)
            {
                floats[j] = (float)poly[i].X;
                j++;
                floats[j] = (float)poly[i].Y;
                j++;
                floats[j] = (float)poly[i].Z * scale;
                j++;
            }

            var p = NativeMethods.OffsetTest(floats, floats.Length, delta, miterLimit, precision, arcTolerance);

            float[] verts = new float[p.VerticesLength];
            System.Runtime.InteropServices.Marshal.Copy(p.Vertices, verts, 0, p.VerticesLength);

            Polyline outCurve = new Polyline();
            for (int i = 0;i < verts.Length; i+=3)
                outCurve.Add(new Point3d(verts[i], verts[i + 1], verts[i + 2]/scale));

            if (!outCurve.IsClosed)
                outCurve.Add(outCurve[0]);

            DA.SetData(0, outCurve);
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
            get { return new Guid("30EBFD86-C597-4229-8566-277F97542AE0"); }
        }
    }
}
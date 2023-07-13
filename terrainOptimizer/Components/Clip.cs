using System;
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
            pManager.AddNumberParameter("epsilon", "epsilon", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("simplify", "simplify", "", GH_ParamAccess.item);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("crv", "crv", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Curve crv = null;
            double delta = 0;
            double miterLimit = 0;
            int precision = 0;
            double epsilon = 0;
            bool simplify = false;
            DA.GetData(0, ref crv);
            DA.GetData(1, ref delta);
            DA.GetData(2, ref miterLimit);
            DA.GetData(3, ref precision);
            DA.GetData(4, ref epsilon);
            DA.GetData(5, ref simplify);

            Polyline poly;

            if (crv.IsPolyline())
                crv.TryGetPolyline(out poly);
            else
                poly = crv.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

            double[] coordinates = new double[poly.Count * 3];

            for (int i = 0, j = 0; i < poly.Count; i++, j += 3)
            {
                coordinates[j] = poly[i].X;
                coordinates[j + 1] = poly[i].Y;
                coordinates[j + 2] = poly[i].Z;
            }

            // Add variable offset values

            double[] offset = new double[poly.Count];
            for (int i = 0; i < poly.Count; i++)
                offset[i] = 0.1 + (double)i / (double) poly.Count;

            var offsetResult = NativePolylineMethods.VariableOffset3d(coordinates, coordinates.Length, offset, miterLimit, precision, simplify, epsilon);

            double[] verts = new double[offsetResult.VerticesLength];
            System.Runtime.InteropServices.Marshal.Copy(offsetResult.Vertices, verts, 0, offsetResult.VerticesLength);

            Polyline outCurve = new Polyline();
            for (int i = 0;i < verts.Length; i+=3)
                outCurve.Add(new Point3d(verts[i], verts[i + 1], verts[i + 2]));

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
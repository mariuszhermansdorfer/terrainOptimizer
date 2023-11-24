using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Display;
using Rhino.Geometry;
using Rhino.UI;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class Sculpt : GH_Component
    {

        public Sculpt()
          : base("Sculpt", "Sculpt",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("base", "base", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("radius", "radiues", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("result", "result", "", GH_ParamAccess.list);
        }

        static IntPtr mesh = IntPtr.Zero;
        Test a;
        conduit c;
        static float[] sizes;
        static List<Point3d> pts;
        static double radius = 0;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh baseMesh = new Mesh();
            DA.GetData(0, ref baseMesh);

            
            DA.GetData(1, ref radius);

            if (mesh == IntPtr.Zero)
                mesh = MeshApi.CreateMRMesh(baseMesh);

            if (a == null || !a.Enabled)
            {
                a = new Test() { Enabled = true };
                c = new conduit() { Enabled = true };
            }
                

            

            //DA.SetDataList(0, points);
            
        }

        public class conduit : Rhino.Display.DisplayConduit
        {
            protected override void DrawOverlay(DrawEventArgs e)
            {
                if (pts == null)
                    return;
                for (int i = 0; i < pts.Count; i++)
                    e.Display.DrawPoint(pts[i], PointStyle.RoundSimple, sizes[i] * 3, System.Drawing.Color.Black);

                base.DrawOverlay(e);
            }
        }

        public class Test : Rhino.UI.MouseCallback
        {
            protected override void OnMouseMove(MouseCallbackEventArgs e)
            {
                var k = e.View.ActiveViewport.ClientToWorld(e.ViewportPoint);
                Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport.GetFrustumLine(e.ViewportPoint.X, e.ViewportPoint.Y, out Line l);
                float[] ray = new float[6];
                ray[0] = (float)l.ToX;
                ray[1] = (float)l.ToY;
                ray[2] = (float)l.ToZ;
                ray[3] = (float)-l.Direction[0];
                ray[4] = (float)-l.Direction[1];
                ray[5] = (float)-l.Direction[2];

                var sw = System.Diagnostics.Stopwatch.StartNew();
                var p = MeshApi.Sculpt(mesh, ray, (float)radius);
                sw.Stop();
                Rhino.RhinoApp.WriteLine(sw.ElapsedMilliseconds.ToString() + " ms");

                if (p.VerticesLength == 0)
                    return;

                float[] verts = new float[p.VerticesLength];
                Marshal.Copy(p.Vertices, verts, 0, p.VerticesLength);

                sizes = new float[p.VertexValuesLength];
                Marshal.Copy(p.VertexValues, sizes, 0, p.VertexValuesLength);

                pts = new List<Point3d>();

                for (int i = 0; i < p.VerticesLength; i += 3)
                {
                    Point3d pt = new Point3d(verts[i], verts[i + 1], verts[i + 2]);
                    pts.Add(pt);
                }

                sw.Restart();
                Rhino.RhinoDoc.ActiveDoc.Views.Redraw();

                sw.Stop();

                Rhino.RhinoApp.WriteLine("Redraw: " + sw.ElapsedMilliseconds.ToString() + " ms");

                base.OnMouseMove(e);
            }


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
            get { return new Guid("2FB4508C-470A-4E58-939E-E18C2E2F7596"); }
        }
    }
}
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using static terrainOptimizer.Structs;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace terrainOptimizer
{
    public class GHC_TerrainOptimizer : GH_Component
    {
        Curve proposed;
        Curve existing;
        int subdivisions;

        public GHC_TerrainOptimizer()
          : base("terrainOptimizer", "Nickname",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddCurveParameter("proposed", "proposed", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("existing", "existing", "", GH_ParamAccess.item);
            pManager.AddIntegerParameter("subdivisions", "subdivisions", "", GH_ParamAccess.item, subdivisions);
            pManager.AddNumberParameter("slopes", "slopes", "", GH_ParamAccess.list);
            pManager.AddPointParameter("low", "low", "", GH_ParamAccess.list);
            pManager.AddPointParameter("high", "high", "", GH_ParamAccess.list);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;
            pManager[5].Optional = true;
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("lines", "", "", GH_ParamAccess.item);
            pManager.AddRectangleParameter("cut", "", "", GH_ParamAccess.item);
            pManager.AddRectangleParameter("fill", "", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("balance", "", "", GH_ParamAccess.item);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<double> slopes = new List<double>();
            List<Point3d> lowPoints = new List<Point3d>();
            List<Point3d> highPoints = new List<Point3d>();
            DA.GetData(0, ref proposed);
            DA.GetData(1, ref existing);
            DA.GetData(2, ref subdivisions);
            DA.GetDataList(3, slopes);
            DA.GetDataList(4, lowPoints);
            DA.GetDataList(5, highPoints);

            double domainLength = existing.PointAtEnd.X - existing.PointAtStart.X;
            double step = domainLength / subdivisions;

            var proposedNurbs = proposed.ToNurbsCurve();
            ControlPoints controlPoints = new ControlPoints(proposedNurbs.Points.Count);

            for (int i = 0; i < proposedNurbs.Points.Count; i++)
                controlPoints.Points[i] = new Point3d(proposedNurbs.Points[i].X, proposedNurbs.Points[i].Y, proposedNurbs.Points[i].Z);


            foreach (var low in lowPoints)
            {
                int closestPointIndex = 0;
                for (int i = 0; i < controlPoints.Length; i++)
                {
                    if (Math.Abs(low.X - controlPoints.Points[i].X) < Math.Abs(low.X - controlPoints.Points[closestPointIndex].X))
                        closestPointIndex = i;
                }
                controlPoints.Anchors[closestPointIndex] = Anchors.LowPoint;
            }

            foreach (var high in highPoints)
            {
                int closestPointIndex = 0;
                for (int i = 0; i < controlPoints.Length; i++)
                {
                    if (Math.Abs(high.X - controlPoints.Points[i].X) < Math.Abs(high.X - controlPoints.Points[closestPointIndex].X))
                        closestPointIndex = i;
                }
                controlPoints.Anchors[closestPointIndex] = Anchors.HighPoint;
            }


            controlPoints.Pegged[0] = true;
            controlPoints.Pegged[11] = true;

            bool ascending = false;
            for (int i = 0; i < controlPoints.Length - 1; i++)
            {
                if (controlPoints.Anchors[i] == Anchors.LowPoint)
                    ascending = true;

                if (controlPoints.Anchors[i] == Anchors.HighPoint)
                    ascending = false;

                if (ascending)
                    controlPoints.Ascending[i + 1] = true;

            }


            Polyline proposedProfile = new Polyline();
            proposedProfile.Add(controlPoints.Points[0]); // Add first point
            int reverse;
            for (int i = 1; i < controlPoints.Length - 1; i++)
            {
                double x = controlPoints.Points[i].X;
                double y = controlPoints.Points[i].Y;
                double dx = x - controlPoints.Points[i - 1].X;

                reverse = controlPoints.Ascending[i] ? 1 : -1;
                    
                double z = proposedProfile[i - 1].Z + dx * slopes[i - 1] * 0.01 * reverse;

                proposedProfile.Add(new Point3d(x, y, z));
            }
            proposedProfile.Add(controlPoints.Points[controlPoints.Length - 1]); // Add last point

            List<Point3d> pointsExisting = new List<Point3d>();
            List<Point3d> pointsProposed = new List<Point3d>();
            List<Rectangle3d> rectanglesCut = new List<Rectangle3d>();
            List<Rectangle3d> rectanglesFill = new List<Rectangle3d>();

            double balance = 0.0;

            for (int i = 0; i < subdivisions; i++)
            {
                Vector3d offset = new Vector3d(i * step, 0, 0);
                var line = new Line(existing.PointAtStart + offset, Vector3d.ZAxis);

                var _intersectionExisting = Rhino.Geometry.Intersect.Intersection.CurveLine(existing, line, 0.1, 0.1);
                var _intersectionProposed = Rhino.Geometry.Intersect.Intersection.CurveLine(proposedProfile.ToNurbsCurve(), line, 0.1, 0.1);
                pointsExisting.Add(_intersectionExisting[0].PointA);
                pointsProposed.Add(_intersectionProposed[0].PointA);
                if (i > 0)
                {
                    var rectangle = new Rectangle3d(Plane.WorldZX, pointsExisting[i], pointsProposed[i - 1]);
                    if (pointsExisting[i].Z - pointsProposed[i].Z > 0)
                    {
                        rectanglesCut.Add(rectangle);
                        balance -= rectangle.Area;
                    }
                    
                    else
                    {
                        balance += rectangle.Area;
                        rectanglesFill.Add(rectangle);
                    }
                        
                }
                   


            }

            pointsExisting.AddRange(pointsProposed);

            DA.SetData(0, proposedProfile);
            DA.SetDataList(1, rectanglesCut);
            DA.SetDataList(2, rectanglesFill);
            DA.SetData(3, balance);
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
            get { return new Guid("8f49e3db-3ad6-4d23-a675-ee8e1856fc22"); }
        }
    }
}

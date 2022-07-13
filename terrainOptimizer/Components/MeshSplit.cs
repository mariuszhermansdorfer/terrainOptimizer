using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;



namespace terrainOptimizer
{
    public class MeshSplit : GH_Component
    {
        public MeshSplit()
          : base("meshSplit", "Nickname",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("existing", "existing", "", GH_ParamAccess.item);
            pManager.AddCurveParameter("proposed", "proposed", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("cut slope", "cut slope", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("fill slope", "fill slope", "", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;

        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("base", "", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("cut", "", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("fill", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("outline", "", "", GH_ParamAccess.item);
            pManager.AddPointParameter("point", "", "", GH_ParamAccess.list);
        }

        List<int> boundingBoxSearchResults;
        RTree tree;
        Mesh existingGround;
        Curve newTopography;
        Polyline[] intersections;
        Polyline[] polyOverlaps;
        Mesh meshOverlaps;
        System.Threading.CancellationToken token;
        double slopeCut;
        double slopeFill;
        double precision = 0.01;
        IProgress<double> progress;
        Rhino.FileIO.TextLog textLog;

//Offset: 6 ms -> Check clipper
//Loft: 1 ms
//Search: 4 ms
//Intersect: 31 ms -> find different approach?
//Mesh: 1 ms
//Total: 0 ms


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref existingGround);
            DA.GetData(1, ref newTopography);
            DA.GetData(2, ref slopeCut);
            DA.GetData(3, ref slopeFill);


            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            if (tree == null)
                tree = RTree.CreateMeshFaceTree(existingGround);

            double distance = 5;
            var offsetCurve = newTopography.Offset(Point3d.Origin, Vector3d.ZAxis, 5, 0.01, CurveOffsetCornerStyle.None);
            offsetCurve[0].Translate(Vector3d.ZAxis * -distance * slopeFill);
            Rhino.RhinoApp.WriteLine("Offset: " + sw.ElapsedMilliseconds + " ms");
            sw.Restart();



            var loft = Brep.CreateFromLoft(new Curve[] { newTopography, offsetCurve[0] }, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
            Mesh _mesh = Mesh.CreateFromBrep(loft[0], MeshingParameters.FastRenderMesh)[0];
            Rhino.RhinoApp.WriteLine("Loft: " + sw.ElapsedMilliseconds + " ms");
            sw.Restart();

            var box = _mesh.GetBoundingBox(false);

            var xform = Transform.Scale(new Plane(box.Center, Vector3d.ZAxis), 1, 1, 1000); // Stretch in the Z-Axis to extend the search area
            box.Transform(xform);
            boundingBoxSearchResults = new List<int>();
            tree.Search(box, BoundingBoxCallback);

            //RTree cutterTree = RTree.CreateMeshFaceTree(_mesh);
            //RTree.SearchOverlaps(tree, cutterTree, 0.1, BoundingBoxCallback);

            var cutout = new Mesh();
            cutout.CopyFrom(existingGround);
            cutout.Faces.Clear();
            foreach (var face in boundingBoxSearchResults)
                cutout.Faces.AddFace(existingGround.Faces[face]);

            Rhino.RhinoApp.WriteLine("Search: " + sw.ElapsedMilliseconds + " ms");
            sw.Restart();


            Polyline top1 = newTopography.ToPolyline(-1, -1, 0.1, 0.1, 1, 0, 0.1, 2, false).ToPolyline();
            top1.DeleteShortSegments(0.1);

            List<Point3d> points = new List<Point3d>();
            for (int i = 0; i < top1.Count - 1; i++)
            {
                var ray = new Ray3d(top1[i], Vector3d.ZAxis * -1);
                var mrx = Rhino.Geometry.Intersect.Intersection.MeshRay(existingGround, ray);
                points.Add(ray.PointAt(mrx));
            }
            


            Rhino.RhinoApp.WriteLine("Ray: " + sw.ElapsedMilliseconds + " ms");
            sw.Restart();

            List<Mesh> intersectionMeshes = new List<Mesh> { cutout, _mesh };
            Rhino.Geometry.Intersect.Intersection.MeshMesh(intersectionMeshes, 0.00001, out intersections, false, out _, false, out _, null, token, null);

            Rhino.RhinoApp.WriteLine("Intersect: " + sw.ElapsedMilliseconds + " ms");
            sw.Restart();

            List<Curve> joinedCurves = new List<Curve>();
            foreach (var poly in intersections)
                joinedCurves.Add(poly.ToNurbsCurve());

            var test = Curve.JoinCurves(joinedCurves, 0.1, false);
            joinedCurves.Clear();
            foreach (var curve in test)
                if (curve.GetLength() > 0.1)
                    joinedCurves.Add(curve);

            List<Mesh> meshesFill = new List<Mesh>();
            Curve[] result = null;
            Point3d pt = new Point3d();
            Polyline bottom = null;

            if (joinedCurves.Count == 1 && joinedCurves[0].IsClosed) // newTerrain is completely above existing ground
            {
                //if (!Curve.DoDirectionsMatch(joinedCurves[0], newTopography))
                //    joinedCurves[0].Reverse();

                //double start;
                //joinedCurves[0].ClosestPoint(newTopography.PointAtStart, out start);
                //joinedCurves[0].ChangeClosedCurveSeam(start);
                //loft = Brep.CreateFromLoftRebuild(new Curve[] { newTopography, joinedCurves[0] }, Point3d.Unset, Point3d.Unset, LoftType.Normal, false, 100);
                //var mesh = Mesh.CreateFromBrep(loft[0], MeshingParameters.FastRenderMesh);
                //meshesFill.Add(mesh[0]);




                Polyline top = newTopography.ToPolyline(-1, -1, 0.1, 0.1, 1, 0, 0.1, 2, false).ToPolyline();
                top.DeleteShortSegments(0.1);

                // Intersect curve normal with the other curve and realign their starting points.

                if (!Curve.DoDirectionsMatch(joinedCurves[0], newTopography))
                    joinedCurves[0].Reverse();
                var normal = Vector3d.CrossProduct(newTopography.TangentAtStart, Vector3d.ZAxis);
                normal.Unitize();

                if (newTopography.Contains(newTopography.PointAtStart + normal * 0.1, Plane.WorldXY, 0.01) == PointContainment.Inside)
                    normal.Reverse();

                var line = new Line(newTopography.PointAtStart - Vector3d.ZAxis * 100, newTopography.PointAtStart + Vector3d.ZAxis * 100).ToNurbsCurve();
                var extrusion = Surface.CreateExtrusion(line, normal * 15);

                var intersection = Rhino.Geometry.Intersect.Intersection.CurveSurface(joinedCurves[0], extrusion, 0.01, 0.1);

                double start;
                joinedCurves[0].ClosestPoint(intersection[0].PointA, out start);
                joinedCurves[0].ChangeClosedCurveSeam(start);

                bottom = joinedCurves[0].ToPolyline(-1, -1, 0.1, 0.1, 1, 0, 0.1, 2, false).ToPolyline();
                bottom.DeleteShortSegments(0.1);

                int current = 1;
                Mesh finalMesh = new Mesh();
                finalMesh.Vertices.AddVertices(bottom);
                finalMesh.Vertices.AddVertices(top);

                finalMesh.Faces.AddFace(0, bottom.Count + 1, bottom.Count);
                for (int i = 1; i < bottom.Count ; i++)
                {
                    int topIndex = bottom.Count + current;

                    if(current == top.Count - 1)
                    {
                        finalMesh.Faces.AddFace(i, topIndex, i - 1);
                    }
                    else if (bottom[i].DistanceToSquared(top[current]) < bottom[i].DistanceToSquared(top[current + 1]))
                        finalMesh.Faces.AddFace(i, topIndex, i - 1);
                    else
                    {
                        finalMesh.Faces.AddFace(i, topIndex + 1, i - 1);
                        finalMesh.Faces.AddFace(topIndex + 1, topIndex, i - 1);
                        current++;
                    }
                }
                finalMesh.Faces.AddFace(bottom.Count - 1, 0, bottom.Count);
                finalMesh.Faces.AddFace(bottom.Count, bottom.Count + current, bottom.Count - 1);

                finalMesh.RebuildNormals();
                meshesFill.Add(finalMesh);


                Rhino.RhinoApp.WriteLine("Mesh: " + sw.ElapsedMilliseconds + " ms");
                sw.Restart();
            }
            else
            {
                //for (int i = 0; i < joinedCurves.Count; i++)
                foreach (var joinedCurve in joinedCurves)
                {
                    if (joinedCurve.IsClosed) // Closed curves only make sense while completely above ground
                        continue;

                    var cutter = joinedCurve.Extend(CurveEnd.Both, 0.1, CurveExtensionStyle.Line);
                    var extrusion = Surface.CreateExtrusion(cutter, Vector3d.ZAxis * 2);
                    extrusion.Translate(Vector3d.ZAxis * -1);
                    var topEdge = newTopography.DuplicateCurve();

                    result = topEdge.Split(extrusion, 0.00001, 0.01);

                    if (result.Length == 0)
                        continue;

                    //TODO which curve from result should be used?

                    if (!Curve.DoDirectionsMatch(joinedCurve, result[0]))
                        joinedCurve.Reverse();

                    loft = Brep.CreateFromLoft(new Curve[] { result[0], joinedCurve }, Point3d.Unset, Point3d.Unset, LoftType.Normal, false);
                    var mesh = Mesh.CreateFromBrep(loft[0], MeshingParameters.FastRenderMesh);
                    meshesFill.Add(mesh[0]);
                }
            }

            DA.SetDataList(2, meshesFill);
            DA.SetDataList(3, new Polyline[] {bottom});
            DA.SetDataList(4, points);

            Rhino.RhinoApp.WriteLine("Total: " + sw.ElapsedMilliseconds + " ms");
            sw.Stop();




        }

        void BoundingBoxCallback(object sender, RTreeEventArgs e)
        {
            boundingBoxSearchResults.Add(e.Id);
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
            get { return new Guid("0abc97c2-fdc2-4aeb-8977-7de0434e40a8"); }
        }
    }
}

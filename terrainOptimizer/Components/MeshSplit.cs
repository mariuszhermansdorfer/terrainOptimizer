using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
            pManager.AddNumberParameter("round corners", "round", "", GH_ParamAccess.item);

            pManager[0].Optional = true;
            pManager[1].Optional = true;
            pManager[2].Optional = true;
            pManager[3].Optional = true;
            pManager[4].Optional = true;

        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("base", "", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("cut", "", "", GH_ParamAccess.list);
            pManager.AddMeshParameter("fill", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("outline", "", "", GH_ParamAccess.item);
            pManager.AddPointParameter("point", "", "", GH_ParamAccess.list);
            pManager.AddBoxParameter("box", "", "", GH_ParamAccess.list);
            pManager.AddMeshParameter("new terrain", "", "", GH_ParamAccess.item);
            pManager.AddTextParameter("stats", "", "", GH_ParamAccess.item);
        }

        List<Rectangle3d> _rectanglesFullyOutside;
        Queue<Rectangle3d> _rectanglesToAnalyse;
        HashSet<int> _facesInBoundingBox;
        HashSet<int> _facesFullyOutside;
        HashSet<int> _facesToDelete;


        // Used to store intersection results
        Dictionary<Point3d, bool> _vertices;
        Dictionary<Line, bool> _edges;

        RTree tree;
        Mesh baseTerrain;
        Curve newTerrain;

        double slopeCut;
        double slopeFill;
        double roundCorners;
        double maxDistance = 100;
        double minDiagonalDepth = 100;


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref baseTerrain);
            DA.GetData(1, ref newTerrain);
            DA.GetData(2, ref slopeCut);
            DA.GetData(3, ref slopeFill);
            DA.GetData(4, ref roundCorners);
            minDiagonalDepth = 100;

            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();

            List<List<Point3d>> fillInsidePoints = new List<List<Point3d>> { new List<Point3d>() };
            List<List<Point3d>> fillOutsidePoints = new List<List<Point3d>> { new List<Point3d>() };
            List<List<Point3d>> cutInsidePoints = new List<List<Point3d>> { new List<Point3d>() };
            List<List<Point3d>> cutOutsidePoints = new List<List<Point3d>> { new List<Point3d>() };

            if (roundCorners > 0 && newTerrain.IsPolyline())
                newTerrain = Curve.CreateFilletCornersCurve(newTerrain, roundCorners, 0.00001, 0.00001);

            Polyline insidePolyline = newTerrain.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, false).ToPolyline();
            Polyline outline = CreateCutFillOutlinePoints(insidePolyline, cutInsidePoints, cutOutsidePoints, fillInsidePoints, fillOutsidePoints);
            if (!outline.IsClosed)
                outline.Add(outline[0]);

            var outlineMesh = Mesh.CreateFromClosedPolyline(outline);
            if (outlineMesh == null)
                return;

            List<Mesh> meshesFill = new List<Mesh>();
            MergeFirstAndLast(fillInsidePoints, fillOutsidePoints, out bool close);
            CreateMeshes(meshesFill, fillInsidePoints, fillOutsidePoints, close);

            List<Mesh> meshesCut = new List<Mesh>();
            MergeFirstAndLast(cutInsidePoints, cutOutsidePoints, out close);
            CreateMeshes(meshesCut, cutInsidePoints, cutOutsidePoints, close);



            Rhino.Geometry.Intersect.Intersection.MeshRay(baseTerrain, new Ray3d(new Point3d(outline[0].X, outline[0].Y, -999), Vector3d.ZAxis), out int[] intersectedFaces);

            if (intersectedFaces.Length == 0)
                return;


            _facesToDelete = MeshTraversal.FindFacesCrossedByPolyline(ref baseTerrain, outline, intersectedFaces[0]);
            var insideFaces = MeshTraversal.FindFacesWithinBoundary(ref baseTerrain, ref outlineMesh, _facesToDelete);

            _facesToDelete.UnionWith(insideFaces);


            var m = new Mesh();
            m.CopyFrom(baseTerrain);
            m.Faces.Clear();
            List<MeshFace> faces = new List<MeshFace>(_facesToDelete.Count);
            foreach (var face in _facesToDelete)
                faces.Add(baseTerrain.Faces[face]);

            m.Faces.AddFaces(faces);
            m.Compact();

            baseTerrain.Faces.ExtractFaces(_facesToDelete);


            if (!insidePolyline.IsClosed)
                insidePolyline.Add(insidePolyline[0]);
            var platform = Mesh.CreateFromClosedPolyline(insidePolyline);
            platform.RebuildNormals();
            if (platform.Normals[0].Z < 0)
                platform.Flip(true, true, true);


            var nakedEdges = m.GetNakedEdges();
            if (nakedEdges != null)
            {
                var patchMesh = CreateMeshWithHoles(new Polyline[] { nakedEdges[0], outline });
                baseTerrain.Append(patchMesh);
                baseTerrain.Compact();
            }

            
            //baseTerrain.Weld(Math.PI);
            Rhino.RhinoApp.WriteLine("Total: " + sw.ElapsedMilliseconds + " ms");
            sw.Stop();

            string stats = null;
            if (close && meshesFill.Count == 0)
            {
                double depth = Math.Sin(Math.Atan(slopeCut)) * minDiagonalDepth;
                double wedgeArea = Math.Sqrt(Math.Pow(minDiagonalDepth, 2) - Math.Pow(depth, 2)) * depth / 2;
                double sweptVolume = wedgeArea * insidePolyline.Length;
                double extrudedVolume = AreaMassProperties.Compute(FlattenPolyline(insidePolyline).ToPolylineCurve(), 0.1).Area * depth;

                stats = $"Depth: {Math.Round(depth, 2)} m \nVolume: {Math.Round(sweptVolume + extrudedVolume, 2)} m3";
            }



            DA.SetData(0, baseTerrain);
            DA.SetDataList(1, meshesCut);
            DA.SetDataList(2, meshesFill);
            DA.SetDataList(3, new Curve[] { outline.ToPolylineCurve() });
            //DA.SetDataList(4, new Point3d[] {l.From, l.To});
           // DA.SetDataList(5, bbb);
            DA.SetData(6, platform);
            DA.SetData(7, stats);

        }

        private Polyline FlattenPolyline(Polyline polyline)
        {
            Polyline planar = new Polyline();
            foreach (var pt in polyline)
                planar.Add(new Point3d(pt.X, pt.Y, 0));

            return planar;
        }

        private Polyline CreateCutFillOutlinePoints(Polyline insidePolyline, List<List<Point3d>> cutInsidePoints, List<List<Point3d>> cutOutsidePoints, List<List<Point3d>> fillInsidePoints, List<List<Point3d>> fillOutsidePoints)
        {
            Polyline outline = new Polyline();
            double slope;
            int multiplier = 1; // TODO check direction

            for (int i = 0; i < insidePolyline.Count; i++)
            {
                if (CheckIfFill(insidePolyline[i]))
                {
                    slope = -slopeFill;
                    int lastItem = cutInsidePoints.Count - 1;
                    if (cutInsidePoints[lastItem].Count > 0)
                    {
                        var midPoint = insidePolyline[i - 1] + (insidePolyline[i] - insidePolyline[i - 1]) / 2;
                        cutInsidePoints[lastItem].Add(midPoint);
                        fillInsidePoints[fillInsidePoints.Count - 1].Add(midPoint);
                        outline.Add(midPoint);

                        cutInsidePoints.Add(new List<Point3d>());
                        cutOutsidePoints.Add(new List<Point3d>());
                    }
                }
                else
                {
                    slope = slopeCut;
                    int lastItem = fillInsidePoints.Count - 1;
                    if (fillInsidePoints[lastItem].Count > 0)
                    {
                        var midPoint = insidePolyline[i - 1] + (insidePolyline[i] - insidePolyline[i - 1]) / 2;
                        fillInsidePoints[lastItem].Add(midPoint);
                        cutInsidePoints[cutInsidePoints.Count - 1].Add(midPoint);
                        outline.Add(midPoint);

                        fillInsidePoints.Add(new List<Point3d>());
                        fillOutsidePoints.Add(new List<Point3d>());
                    }
                }

                double t = insidePolyline.ClosestParameter(insidePolyline[i]);
                Vector3d normal = Vector3d.CrossProduct(insidePolyline.TangentAt(t), Vector3d.ZAxis * multiplier);
                normal.Unitize();
                normal.Z = slope;

                var ray = new Ray3d(insidePolyline[i], normal);
                var meshRayIntersection = Rhino.Geometry.Intersect.Intersection.MeshRay(baseTerrain, ray);

                if (meshRayIntersection < maxDistance)
                {
                    var point = ray.PointAt(meshRayIntersection);
                    outline.Add(point);
                    if (slope == -slopeFill)
                    {
                        fillInsidePoints[fillInsidePoints.Count - 1].Add(insidePolyline[i]);
                        fillOutsidePoints[fillInsidePoints.Count - 1].Add(point);
                    }
                    else
                    {
                        // TODO assign this parameter per each object
                        var slopeDistance = point.DistanceTo(insidePolyline[i]);
                        minDiagonalDepth = slopeDistance < minDiagonalDepth ? slopeDistance : minDiagonalDepth;
                        // END TODO

                        cutInsidePoints[cutInsidePoints.Count - 1].Add(insidePolyline[i]);
                        cutOutsidePoints[cutInsidePoints.Count - 1].Add(point);
                    }
                }
                else
                    outline.Add(insidePolyline[i]);
            }

            return outline;
        }

        private void FindRectanglesOutsideMesh(Queue<Rectangle3d> rectangles, Mesh mesh, int maxIterations)
        {
            int iteration = 0;
            while (rectangles.Count > 0)
            {
                iteration++;
                var rect = rectangles.Dequeue();
                if (iteration < 16)
                {
                    PushRectanglesToQueue(SubdivideRectangle(rect), rectangles);
                    continue;
                }

                Point3d[] rectangleCorners = new Point3d[4]
                {
                    rect.Corner(0),
                    rect.Corner(1),
                    rect.Corner(2),
                    rect.Corner(3)
                };
                int containmentCount = CheckIfPointInsideMeshShadow(rectangleCorners, mesh, true);

                if (containmentCount == 0)
                    _rectanglesFullyOutside.Add(rect);
                else if (containmentCount != 4 && iteration < maxIterations)
                    PushRectanglesToQueue(SubdivideRectangle(rect), rectangles);
            }
        }

        private void CheckFaceContainment(HashSet<int> faces, Mesh mesh, PolylineCurve outline)
        {
            foreach (var face in faces)
            {
                Point3d[] faceCorners = new Point3d[3] {
                    (Point3d)baseTerrain.Vertices[baseTerrain.Faces[face].A],
                    (Point3d)baseTerrain.Vertices[baseTerrain.Faces[face].B],
                    (Point3d)baseTerrain.Vertices[baseTerrain.Faces[face].C]
                };

                int containmentCount = CheckIfPointInsideMeshShadow(faceCorners, mesh, false);
                if (containmentCount > 0)
                {
                    _facesToDelete.Add(face);
                }
                else
                {
                    Line[] edges = new Line[3]
                    {
                    new Line(faceCorners[0].X, faceCorners[0].Y, 0, faceCorners[1].X, faceCorners[1].Y, 0),
                    new Line(faceCorners[1].X, faceCorners[1].Y, 0, faceCorners[2].X, faceCorners[2].Y, 0),
                    new Line(faceCorners[2].X, faceCorners[2].Y, 0, faceCorners[0].X, faceCorners[0].Y, 0),
                    };

                    foreach (var edge in edges)
                    {
                        if (_edges.TryGetValue(edge, out bool crossing))
                        {
                            if (crossing)
                            {
                                _facesToDelete.Add(face);
                                break;
                            }
                        }
                        else
                        {
                            var intersection = Rhino.Geometry.Intersect.Intersection.CurveCurve(outline, edge.ToNurbsCurve(), 0.001, 0.001);
                            if (intersection == null || intersection.Count == 0)
                            {
                                _edges.Add(new Line(edge.To, edge.From), false);
                            }
                            else
                            {
                                _edges.Add(new Line(edge.To, edge.From), true);
                                _facesToDelete.Add(face);
                                break;
                            }
                        }
                    }
                }
            }
        }

        private int CheckIfPointInsideMeshShadow(Point3d[] corners, Mesh mesh, bool accurate)
        {
            int counter = 0;
            if (mesh == null)
                return counter;

            foreach (var corner in corners)
            {
                bool contained;
                if (!_vertices.TryGetValue(corner, out contained))
                {
                    if (!corner.IsValid)
                        continue;
                    var pt = new Point3d(corner.X, corner.Y, -100000); // Make sure point is below the terrain
                    var ray = new Ray3d(pt, Vector3d.ZAxis);
                    var meshRayIntersection = Rhino.Geometry.Intersect.Intersection.MeshRay(mesh, ray);
                    contained = meshRayIntersection == double.NegativeInfinity ? false : true;
                    _vertices.Add(corner, contained);
                }
                if (contained)
                {
                    counter++;
                    if (!accurate)
                        return counter;
                }
            }
            return counter;
        }

        private BoundingBox ScaleBoundingBox(BoundingBox bbox)
        {
            bbox.Min = new Point3d(bbox.Min.X, bbox.Min.Y, -1000000);
            bbox.Max = new Point3d(bbox.Max.X, bbox.Max.Y, 1000000);
            return bbox;
        }

        private void PushRectanglesToQueue(Rectangle3d[] arryOfRectangles, Queue<Rectangle3d> queue)
        {
            foreach (var rect in arryOfRectangles)
            {
                var newRectangles = SubdivideRectangle(rect);
                foreach (var newRectangle in newRectangles)
                    queue.Enqueue(newRectangle);
            }
        }

        private Rectangle3d[] SubdivideRectangle(Rectangle3d rectangle)
        {
            var halfWidth = rectangle.Width / 2;
            var halfHeight = rectangle.Height / 2;
            var corner0 = rectangle.Corner(0);
            var corner1 = rectangle.Corner(0) + Vector3d.XAxis * halfWidth;
            var corner2 = rectangle.Corner(0) + Vector3d.YAxis * halfHeight;
            var corner3 = corner2 + Vector3d.XAxis * halfWidth;

            var result = new Rectangle3d[4];
            result[0] = new Rectangle3d(new Plane(corner0, Vector3d.ZAxis), halfWidth, halfHeight);
            result[1] = new Rectangle3d(new Plane(corner1, Vector3d.ZAxis), halfWidth, halfHeight);
            result[2] = new Rectangle3d(new Plane(corner2, Vector3d.ZAxis), halfWidth, halfHeight);
            result[3] = new Rectangle3d(new Plane(corner3, Vector3d.ZAxis), halfWidth, halfHeight);

            return result;
        }
        private void CreateMeshes(List<Mesh> resultingMeshes, List<List<Point3d>> inside, List<List<Point3d>> outside, bool close)
        {
            for (int i = 0; i < inside.Count; i++)
            {
                if (inside[i].Count == 0)
                    continue;

                Mesh meshSection = new Mesh();

                meshSection.Vertices.AddVertices(inside[i]);
                meshSection.Vertices.AddVertices(outside[i]);

                int index = inside[i].Count;
                if (close)
                {
                    for (int j = 0; j < index - 1; j++)
                    {
                        meshSection.Faces.AddFace(j, j + index + 1, j + index);
                        meshSection.Faces.AddFace(j + 1, j + index + 1, j);
                    }

                    meshSection.Faces.AddFace(index - 1, index, index + outside[i].Count - 1);
                    meshSection.Faces.AddFace(0, index, index - 1);
                }
                else
                {
                    meshSection.Faces.AddFace(0, 1, index); // Starting triangle
                    for (int j = 1; j < index - 1; j++)
                    {
                        meshSection.Faces.AddFace(j, j + index, j + index - 1);
                        meshSection.Faces.AddFace(j + 1, j + index, j);
                    }
                    meshSection.Faces.AddFace(index - 1, index + index - 3, index - 2); // Ending triangle
                }


                meshSection.RebuildNormals();
                resultingMeshes.Add(meshSection);
            }
        }

        private void MergeFirstAndLast(List<List<Point3d>> inside, List<List<Point3d>> outside, out bool close)
        {
            close = false;
            if (inside.Count > 1 && inside[inside.Count - 1].Count > 0)
            {
                if (inside[0][0].DistanceTo(inside[inside.Count - 1][inside[inside.Count - 1].Count - 1]) < 0.5)
                {
                    inside[inside.Count - 1].AddRange(inside[0]);
                    inside.RemoveAt(0);
                    outside[outside.Count - 1].AddRange(outside[0]);
                    outside.RemoveAt(0);
                }
            }
            else if (inside.Count == 1 && inside[0].Count > 0 && inside[0][0].DistanceTo(inside[0][inside[0].Count - 1]) < 0.5)
                close = true;

        }
        private bool CheckIfFill(Point3d point)
        {
            var ray = new Ray3d(point, -Vector3d.ZAxis);
            var meshRayIntersection = Rhino.Geometry.Intersect.Intersection.MeshRay(baseTerrain, ray);
            return meshRayIntersection == double.NegativeInfinity ? false : true;
        }

        void FindFacesToDelete(object sender, RTreeEventArgs e)
        {
            _facesToDelete.Add(e.Id);
        }

        void FindFacesOutside(object sender, RTreeEventArgs e)
        {
            _facesFullyOutside.Add(e.Id);
        }

        void FindFacesWithinBoundingBox(object sender, RTreeEventArgs e)
        {
            _facesInBoundingBox.Add(e.Id);
        }
        public static Mesh CreateMeshWithHoles(Polyline[] polylines)
        {

            int curvesCount = polylines.Length;
            int[] pointsRanges = new int[curvesCount];
            Point3d[][] pts = new Point3d[curvesCount][];

            Parallel.For(0, curvesCount, (i) =>
            {
                pts[i] = polylines[i].ToArray();
                pointsRanges[i] = pts[i].Length;
            });

            Point3d[] points = pts.SelectMany(x => x).ToArray();
            int total = points.Length; // Total amount of points

            int[] cis = new int[total];
            int[] AA = new int[total];
            int[] BB = new int[total];
            int[] CC = new int[total];
            int[] DD = new int[total];

            int start = 0;
            for (int k = 0; k < curvesCount; k++)
            {
                Parallel.For(0, pointsRanges[k], (i) =>
                {
                    int w = start + i;
                    cis[w] = k;
                    if (pointsRanges[k] < 4)
                    { // New case for triangular holes
                        AA[w] = start - 1;
                        BB[w] = w;
                        CC[w] = w;
                        DD[w] = start + pointsRanges[k];
                    }
                    else if (i == 0)
                    {
                        AA[w] = w;
                        BB[w] = w;
                        CC[w] = w + 1;
                        DD[w] = start + pointsRanges[k] - 1;
                    }
                    else if (i == 1)
                    {
                        AA[w] = w;
                        BB[w] = w;
                        CC[w] = w + 1;
                        DD[w] = start + pointsRanges[k];
                    }
                    else if (i == pointsRanges[k] - 2)
                    {
                        AA[w] = start - 1;
                        BB[w] = w - 1;
                        CC[w] = w;
                        DD[w] = w;
                    }
                    else if (i == pointsRanges[k] - 1)
                    {
                        AA[w] = start;
                        BB[w] = w - 1;
                        CC[w] = w;
                        DD[w] = w;
                    }
                    else
                    {
                        AA[w] = start - 1;
                        BB[w] = w - 1;
                        CC[w] = w + 1;
                        DD[w] = start + pointsRanges[k];
                    }
                });
                start += pointsRanges[k];
            }

            Mesh mesh = Mesh.CreateFromTessellation(points, polylines, Plane.WorldXY, false);

            ConcurrentBag<int> deadIndices = new ConcurrentBag<int>();
            ConcurrentBag<MeshFace> collidingFaces = new ConcurrentBag<MeshFace>();
            Parallel.For(0, mesh.Faces.Count, (i) =>
            {
                int a = mesh.Faces[i].A;
                int b = mesh.Faces[i].B;
                int c = mesh.Faces[i].C;

                if (a >= AA.Length || a >= BB.Length || a >= CC.Length || a >= DD.Length
                || b >= AA.Length || b >= BB.Length || b >= CC.Length || b >= DD.Length)
                {
                    collidingFaces.Add(mesh.Faces[i]);
                    return; // Skip if curves overlap
                }

                if (
                  ((b > AA[a] && b < BB[a]) || (b > CC[a] && b < DD[a])) ||
                  ((c > AA[a] && c < BB[a]) || (c > CC[a] && c < DD[a])) ||
                  ((c > AA[b] && c < BB[b]) || (c > CC[b] && c < DD[b]))
                )
                {
                    if (
                    cis[a] > 0 &&
                    points.Length > c && //Safety check to prevent component from crashing
                    PointInPolygon((points[a] + points[b] + points[c]) / 3, polylines[cis[a]].ToArray())
                    )
                        deadIndices.Add(i);
                }
            });

            if (collidingFaces.Count > 0) //Highlight overlapping faces
            {
                mesh.Faces.Clear();
                mesh.Faces.AddFaces(collidingFaces);
            }
            else
                mesh.Faces.DeleteFaces(deadIndices);

            return mesh;
        }

        public static bool PointInPolygon(Point3d p, Point3d[] polylineArray)
        {
            //Check whether a point is inside a polyline using the winding number method
            //http://geomalgorithms.com/a03-_inclusion.html

            int n = polylineArray.Length - 1;
            int windingNumber = 0;    // the winding number counter

            // loop through all edges of the polygon
            for (int i = 0; i < n; i++)
            {
                // edge from V[i] to V[i+1]
                if (polylineArray[i].Y <= p.Y)
                {         // start y <= P.y
                    if (polylineArray[i + 1].Y >= p.Y)      // an upward crossing
                    {
                        int left = IsLeft(polylineArray[i], polylineArray[i + 1], p);
                        if (left > 0)  // P left of edge
                            ++windingNumber;            // have a valid up intersect
                        else if (left == 0) // point on edge
                            return true;
                    }
                }
                else
                {                       // start y > P.y (no test needed)
                    if (polylineArray[i + 1].Y <= p.Y)     // a downward crossing
                    {
                        int left = IsLeft(polylineArray[i], polylineArray[i + 1], p);
                        if (left < 0)  // P right of edge
                            --windingNumber;            // have a valid down intersect
                        else if (left == 0) // point on edge
                            return true;
                    }
                }
            }
            if (windingNumber != 0)
                return true;
            else
                return false;
        }

        private static int IsLeft(Point3d p0, Point3d p1, Point3d p2)
        {
            //Helper function for the PointInPolygon method
            double calc = (p1.X - p0.X) * (p2.Y - p0.Y) - (p2.X - p0.X) * (p1.Y - p0.Y);
            if (calc > 0.00001) //precision necessary to make sure that points laying on edge are included as well
                return 1;
            else if (calc < -0.00001)
                return -1;
            else
                return 0;
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

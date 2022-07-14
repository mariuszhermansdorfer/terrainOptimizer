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
            pManager.AddMeshParameter("cut", "", "", GH_ParamAccess.list);
            pManager.AddMeshParameter("fill", "", "", GH_ParamAccess.list);
            pManager.AddCurveParameter("outline", "", "", GH_ParamAccess.item);
            pManager.AddPointParameter("point", "", "", GH_ParamAccess.list);
            pManager.AddBoxParameter("box", "", "", GH_ParamAccess.list);
            pManager.AddMeshParameter("new terrain", "", "", GH_ParamAccess.item);
        }

        HashSet<int> _facesToDelete;
        HashSet<int> _facesOnEdge;
        List<Rectangle3d> _rectanglesFullyInside;
        List<Rectangle3d> _rectanglesOnEdge;
        Queue<Rectangle3d> _rectanglesToAnalyse;
        Dictionary<Point3d, bool> _vertices;

        RTree tree;
        Mesh baseTerrain;
        Curve newTerrain;

        double slopeCut;
        double slopeFill;
        double maxDistance = 100;


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref baseTerrain);
            DA.GetData(1, ref newTerrain);
            DA.GetData(2, ref slopeCut);
            DA.GetData(3, ref slopeFill);


            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
            sw.Start();


            List<List<Point3d>> fillInsidePoints = new List<List<Point3d>> { new List<Point3d>() };
            List<List<Point3d>> fillOutsidePoints = new List<List<Point3d>> { new List<Point3d>() };
            List<List<Point3d>> cutInsidePoints = new List<List<Point3d>> { new List<Point3d>() };
            List<List<Point3d>> cutOutsidePoints = new List<List<Point3d>> { new List<Point3d>() };

            Polyline insidePolyline = newTerrain.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.005, 0.5, false).ToPolyline();
            Polyline outline = CreateCutFillOutlinePoints(insidePolyline, cutInsidePoints, cutOutsidePoints, fillInsidePoints, fillOutsidePoints);
            if (!outline.IsClosed)
                outline.Add(outline[0]);

            List<Mesh> meshesFill = new List<Mesh>();
            MergeFirstAndLast(fillInsidePoints, fillOutsidePoints, out bool close);
            CreateMeshes(meshesFill, fillInsidePoints, fillOutsidePoints, close);

            List<Mesh> meshesCut = new List<Mesh>();
            MergeFirstAndLast(cutInsidePoints, cutOutsidePoints, out close);
            CreateMeshes(meshesCut, cutInsidePoints, cutOutsidePoints, close);


            Rhino.RhinoApp.WriteLine("Cut & Fill mesh: " + sw.ElapsedMilliseconds + " ms");
            sw.Restart();

            var box = outline.BoundingBox;

            if (tree == null)
                tree = RTree.CreateMeshFaceTree(baseTerrain);

            _rectanglesFullyInside = new List<Rectangle3d>();
            _rectanglesOnEdge = new List<Rectangle3d>();
            _rectanglesToAnalyse = new Queue<Rectangle3d>();
            _vertices = new Dictionary<Point3d, bool>();
            
            var rectangle = new Rectangle3d(Plane.WorldXY, box.Corner(true, true, true), box.Corner(false, false, true));
            var initial = SubdivideRectangle(rectangle);

            PushRectanglesToQueue(initial, _rectanglesToAnalyse);
            int maxIterations = 20;
            var outlineMesh = Mesh.CreateFromClosedPolyline(outline);

            FindRectanglesInsideMesh(_rectanglesToAnalyse, outlineMesh, maxIterations);

            _facesToDelete = new HashSet<int>();
            foreach (var re in _rectanglesFullyInside)
                tree.Search(ScaleBoundingBox(re), FindFacesToDelete);

            _facesOnEdge = new HashSet<int>();
            foreach (var re in _rectanglesOnEdge)
                tree.Search(ScaleBoundingBox(re), FindFacesOnEdge);

            CheckFaceContainment(_facesOnEdge, outlineMesh);

            baseTerrain.Faces.ExtractFaces(_facesToDelete);

            Rhino.RhinoApp.WriteLine("Split: " + sw.ElapsedMilliseconds + " ms");
            sw.Stop();

            if (!insidePolyline.IsClosed)
                insidePolyline.Add(insidePolyline[0]);
            var platform = Mesh.CreateFromClosedPolyline(insidePolyline);
            platform.RebuildNormals();

            DA.SetData(0, baseTerrain);
            DA.SetDataList(1, meshesCut);
            DA.SetDataList(2, meshesFill);
            //DA.SetDataList(3, new Polyline[] { new Polyline(outline) });
            //DA.SetDataList(4, insidePolyline);
            //DA.SetDataList(5, bbb);
            DA.SetData(6, platform);

        }

        private Polyline CreateCutFillOutlinePoints(Polyline insidePolyline, List<List<Point3d>> cutInsidePoints, List<List<Point3d>> cutOutsidePoints, List<List<Point3d>> fillInsidePoints, List<List<Point3d>> fillOutsidePoints)
        {
            Polyline outline = new Polyline();
            double slope;
            double distance = 0;
            double maxParameter = insidePolyline.ClosestParameter(insidePolyline[insidePolyline.Count - 1]);
            int multiplier = 1; // TODO check direction

            for (int i = 0; i < insidePolyline.Count; i++)
            {
                if (CheckIfFill(insidePolyline[i]))
                {
                    slope = -slopeFill;
                    if (cutInsidePoints[cutInsidePoints.Count - 1].Count > 0)
                    {
                        cutInsidePoints.Add(new List<Point3d>());
                        cutOutsidePoints.Add(new List<Point3d>());
                    }
                }
                else
                {
                    slope = slopeCut;
                    if (fillInsidePoints[fillInsidePoints.Count - 1].Count > 0)
                    {
                        fillInsidePoints.Add(new List<Point3d>());
                        fillOutsidePoints.Add(new List<Point3d>());
                    }
                }

                if (i > 0)
                    distance += insidePolyline[i].DistanceTo(insidePolyline[i - 1]);
                double t = maxParameter * distance / insidePolyline.Length;
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
                        cutInsidePoints[cutInsidePoints.Count - 1].Add(insidePolyline[i]);
                        cutOutsidePoints[cutInsidePoints.Count - 1].Add(point);
                    }
                }
                else
                    outline.Add(insidePolyline[i]);
            }

            return outline;
        }

        private void FindRectanglesInsideMesh(Queue<Rectangle3d> rectangles, Mesh mesh, int maxIterations)
        {
            int iteration = 0;
            while (rectangles.Count > 0)
            {
                iteration++;
                var rect = rectangles.Dequeue();
                Point3d[] rectangleCorners = new Point3d[4]
                {
                    rect.Corner(0),
                    rect.Corner(1),
                    rect.Corner(2),
                    rect.Corner(3)
                };
                int containmentCount = CheckIfPointInsideMeshShadow(rectangleCorners, mesh);

                if (containmentCount == 0)
                    continue;
                if (containmentCount == 4)
                    _rectanglesFullyInside.Add(rect);
                else if (iteration < maxIterations)
                    PushRectanglesToQueue(SubdivideRectangle(rect), rectangles);
                else
                    _rectanglesOnEdge.Add(rect);
            }
        }

        private void CheckFaceContainment(HashSet<int> faces, Mesh mesh)
        {
            foreach (var face in faces)
            {
                Point3d[] faceCorners = new Point3d[3] {
                    (Point3d)baseTerrain.Vertices[baseTerrain.Faces[face].A],
                    (Point3d)baseTerrain.Vertices[baseTerrain.Faces[face].B],
                    (Point3d)baseTerrain.Vertices[baseTerrain.Faces[face].C]
                };

                int containmentCount = CheckIfPointInsideMeshShadow(faceCorners, mesh);
                if (containmentCount > 0)
                    _facesToDelete.Add(face);
            }
        }

        private int CheckIfPointInsideMeshShadow(Point3d[] corners, Mesh mesh)
        {
            int counter = 0;
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
                    counter++;
            }
            return counter;
        }

        private BoundingBox ScaleBoundingBox(Rectangle3d rectangle)
        {
            var bbox = rectangle.BoundingBox;
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
                Mesh fillMeshSection = new Mesh();

                fillMeshSection.Vertices.AddVertices(inside[i]);
                fillMeshSection.Vertices.AddVertices(outside[i]);

                int index = inside[i].Count;
                for (int j = 0; j < index; j++)
                {
                    fillMeshSection.Faces.AddFace(j, j + index + 1, j + index);
                    fillMeshSection.Faces.AddFace(j + 1, j + index + 1, j);
                }
                if (close)
                {
                    fillMeshSection.Faces.AddFace(index - 1, index, index + outside[i].Count - 1);
                    fillMeshSection.Faces.AddFace(0, index, index - 1);
                }

                fillMeshSection.RebuildNormals();
                resultingMeshes.Add(fillMeshSection);
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
            else if (inside[0].Count > 0 && inside[0][0].DistanceTo(inside[0][inside[0].Count - 1]) < 0.5)
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

        void FindFacesOnEdge(object sender, RTreeEventArgs e)
        {
            _facesOnEdge.Add(e.Id);
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

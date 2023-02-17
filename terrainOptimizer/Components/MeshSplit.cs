using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using DHARTAPI.Geometry;
using DHARTAPI.NativeUtils.CommonNativeArrays;
using DHARTAPI.RayTracing;
using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

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


        HashSet<int> _facesToDelete;

        Mesh _baseTerrain;
        Curve _newTerrain;

        MeshInfo _meshInfo;
        EmbreeBVH _bvh;

        double slopeCut;
        double slopeFill;
        double roundCorners;
        double maxDistance = 100;
        double minDiagonalDepth = 100;


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            DA.GetData(0, ref _baseTerrain);
            DA.GetData(1, ref _newTerrain);
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

            if (roundCorners > 0 && _newTerrain.IsPolyline())
                _newTerrain = Curve.CreateFilletCornersCurve(_newTerrain, roundCorners, 0.00001, 0.00001);

            Polyline insidePolyline = _newTerrain.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, false).ToPolyline();
            Polyline outline = CreateCutFillOutlinePoints(insidePolyline, cutInsidePoints, cutOutsidePoints, fillInsidePoints, fillOutsidePoints);
            if (!outline.IsClosed)
                outline.Add(outline[0]);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Initial raycasting: {sw.ElapsedMilliseconds} ms");
            sw.Restart();


            var outlineMesh = Mesh.CreateFromClosedPolyline(outline);
            if (outlineMesh == null)
                return;

            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Create outside mesh: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            List<Mesh> meshesFill = new List<Mesh>();
            MergeFirstAndLast(fillInsidePoints, fillOutsidePoints, out bool close);
            CreateMeshes(meshesFill, fillInsidePoints, fillOutsidePoints, close);
            //var fill = DefineMeshConnectivity(fillInsidePoints, fillOutsidePoints, close);


            List<Mesh> meshesCut = new List<Mesh>();
            MergeFirstAndLast(cutInsidePoints, cutOutsidePoints, out close);
            CreateMeshes(meshesCut, cutInsidePoints, cutOutsidePoints, close);



            Rhino.Geometry.Intersect.Intersection.MeshRay(_baseTerrain, new Ray3d(new Point3d(outline[0].X, outline[0].Y, -999), Vector3d.ZAxis), out int[] intersectedFaces);

            if (intersectedFaces.Length == 0)
                return;


            _facesToDelete = MeshTraversal.FindFacesCrossedByPolyline(ref _baseTerrain, outline, intersectedFaces[0]);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Faces crossed: {sw.ElapsedMilliseconds} ms");
            sw.Restart();
            var insideFaces = MeshTraversal.FindFacesWithinBoundary(ref _baseTerrain, ref outlineMesh, _facesToDelete);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Faces within boundary: {sw.ElapsedMilliseconds} ms");
            sw.Restart();
            _facesToDelete.UnionWith(insideFaces);

            var hole = _baseTerrain.Faces.ExtractFaces(_facesToDelete);


            if (!insidePolyline.IsClosed)
                insidePolyline.Add(insidePolyline[0]);

            var insideMesh = Mesh.CreateFromClosedPolyline(insidePolyline);


            insideMesh.RebuildNormals();
            if (insideMesh.Normals[0].Z < 0)
                insideMesh.Flip(true, true, true);
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Create inside mesh: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            var nakedEdges = hole.GetNakedEdges();
            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Get naked edges: {sw.ElapsedMilliseconds} ms");
            sw.Restart();



            if (nakedEdges != null)
            {
                //List<Point3d> pts = new List<Point3d>();
                //foreach (var p in nakedEdges[0])
                //    pts.Add(p);

                //foreach (var p in outline)
                //    pts.Add(p);

                //var patchMesh = Mesh.CreatePatch(nakedEdges[0], 1, null, new Curve[] { outline.ToNurbsCurve() }, null, pts, false, 0);

                var patchMesh = CreateMeshWithHoles(new Polyline[] { nakedEdges[0], outline });
                sw.Stop();
                Rhino.RhinoApp.WriteLine($"Create patch: {sw.ElapsedMilliseconds} ms");
                sw.Restart();
                _baseTerrain.Append(patchMesh);
                _baseTerrain.Compact();
            }




            sw.Restart();
            int iter = 3;
            float len = 1f;
            
            Parallel.For(0, meshesCut.Count, i =>
            {
                meshesCut[i] = Remesh(meshesCut[i], len, iter, 320);
            });


            Parallel.For(0, meshesFill.Count, i =>
            {
                meshesFill[i] = Remesh(meshesFill[i], len, iter, 320);
            });


            

            sw.Stop();
            Rhino.RhinoApp.WriteLine($"Remesh: {sw.ElapsedMilliseconds} ms");
            sw.Restart();

            //baseTerrain.Weld(Math.PI);


            string stats = null;
            if (close && meshesFill.Count == 0)
            {
                double depth = Math.Sin(Math.Atan(slopeCut)) * minDiagonalDepth;
                double wedgeArea = Math.Sqrt(Math.Pow(minDiagonalDepth, 2) - Math.Pow(depth, 2)) * depth / 2;
                double sweptVolume = wedgeArea * insidePolyline.Length;
                double extrudedVolume = AreaMassProperties.Compute(FlattenPolyline(insidePolyline).ToPolylineCurve(), 0.1).Area * depth;

                stats = $"Depth: {Math.Round(depth, 2)} m \nVolume: {Math.Round(sweptVolume + extrudedVolume, 2)} m3";
            }



            DA.SetData(0, _baseTerrain);
            DA.SetDataList(1, meshesCut);
            DA.SetDataList(2, meshesFill);
            DA.SetDataList(3, new Curve[] { outline.ToPolylineCurve() });
            //DA.SetDataList(4, new Point3d[] {l.From, l.To});
           // DA.SetDataList(5, bbb);
            DA.SetData(6, insideMesh);
            DA.SetData(7, stats);

        }

        private Mesh RemeshRawArray(Tuple<List<int>, List<float>> connectivity, float targetLength, int iterations, double angle)
        {
            
            var m = NativeMethods.CreateMeshFromFloatArray(connectivity.Item1.ToArray(), connectivity.Item1.Count);
            var verts = NativeMethods.CreateVertexGeometry(m, connectivity.Item2.ToArray(), connectivity.Item2.Count);
            NativeMethods.GCRemesh(m, verts, targetLength, iterations, 1);
            var f = NativeMethods.GCFacesToIntArray(m);
            var v = NativeMethods.GCVerticesToFloatArray(m, verts);
            var fi = NativeMethods.GCFacesCount(m);
            var vi = NativeMethods.GCVerticesCount(m);

            int[] faces = new int[fi];
            Marshal.Copy(f, faces, 0, fi);

            float[] vertices = new float[vi];
            Marshal.Copy(v, vertices, 0, vi);


            //NativeMethods.DeleteFacesArray(f);
            //NativeMethods.DeleteVertexArray(v);

            var result = new Mesh();
            for (int i = 0; i < fi; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < vi; i += 3)
                result.Vertices.Add(vertices[i], vertices[i + 1], vertices[i + 2]);

            return result;
        }

        private Mesh Remesh(Mesh mesh, float targetLength, int iterations, double angle)
        {
            var m = NativeMethods.CreateMeshFromFloatArray(mesh.Faces.ToIntArray(true), mesh.Faces.Count * 3);
            var verts = NativeMethods.CreateVertexGeometry(m, mesh.Vertices.ToFloatArray(), mesh.Vertices.Count * 3);
            NativeMethods.GCRemesh(m, verts, targetLength, iterations, 1);
            var f = NativeMethods.GCFacesToIntArray(m);
            var v = NativeMethods.GCVerticesToFloatArray(m, verts);
            var fi = NativeMethods.GCFacesCount(m);
            var vi = NativeMethods.GCVerticesCount(m);

            int[] faces = new int[fi];
            Marshal.Copy(f, faces, 0, fi);

            float[] vertices = new float[vi];
            Marshal.Copy(v, vertices, 0, vi);


            //NativeMethods.DeleteFacesArray(f);
            //NativeMethods.DeleteVertexArray(v);

            var result = new Mesh();
            for (int i = 0; i < fi; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < vi; i += 3)
                result.Vertices.Add(vertices[i], vertices[i + 1], vertices[i + 2]);

            return result;
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

            if (_meshInfo == null)
            {
                _meshInfo = new MeshInfo(_baseTerrain.Faces.ToIntArray(true), _baseTerrain.Vertices.ToFloatArray());
                _bvh = new EmbreeBVH(_meshInfo);
            }
                

            var knots = insidePolyline.ToNurbsCurve().Knots;

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

                Vector3d normal = Vector3d.CrossProduct(insidePolyline.TangentAt(knots[i]), Vector3d.ZAxis * multiplier);
                normal.Unitize();
                normal.Z = slope;

                float[] origin = new float[] { (float)insidePolyline[i].X, (float)insidePolyline[i].Y, (float)insidePolyline[i].Z };
                float[] direction = new float[] { (float)normal.X, (float)normal.Y, (float)normal.Z };
                var ray = new Ray3d(insidePolyline[i], normal);
                var intersection = EmbreeRaytracer.IntersectForDistance(_bvh, origin, direction, 100);

                if (intersection.distance < maxDistance)
                {
                    var point = ray.PointAt(intersection.distance);
                    outline.Add(point);
                    if (slope == -slopeFill)
                    {
                        fillInsidePoints[fillInsidePoints.Count - 1].Add(insidePolyline[i]);
                        fillOutsidePoints[fillInsidePoints.Count - 1].Add(point);
                    }
                    else
                    {
                        // Used to calculate volumes
                        // TODO assign this parameter per each object
                        //var slopeDistance = point.DistanceTo(insidePolyline[i]);
                        //minDiagonalDepth = slopeDistance < minDiagonalDepth ? slopeDistance : minDiagonalDepth;
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


        private List<Tuple<List<int>, List<float>>> DefineMeshConnectivity(List<List<Point3d>> inside, List<List<Point3d>> outside, bool close)
        {
            var result = new List<Tuple<List<int>, List<float>>>();
            for (int i = 0; i < inside.Count; i++)
            {
                if (inside[i].Count == 0)
                    continue;

                List<int> faces = new List<int>();
                List<float> vertices = new List<float>();

                for (int j = 0; j < inside[i].Count; j++)
                {
                    vertices.Add((float)inside[i][j].X);
                    vertices.Add((float)inside[i][j].Y);
                    vertices.Add((float)inside[i][j].Z);
                }

                for (int j = 0; j < outside[i].Count; j++)
                {
                    vertices.Add((float)outside[i][j].X);
                    vertices.Add((float)outside[i][j].Y);
                    vertices.Add((float)outside[i][j].Z);
                }

                int index = inside[i].Count;
                if (close)
                {
                    for (int j = 0; j < index - 1; j++)
                    {
                        faces.Add(j);
                        faces.Add(j + index + 1);
                        faces.Add(j + index);
                        faces.Add(j + 1);
                        faces.Add(j + index + 1);
                        faces.Add(j);
                    }
                    faces.Add(index - 1);
                    faces.Add(index);
                    faces.Add(index + outside[i].Count - 1);
                    faces.Add(0);
                    faces.Add(index);
                    faces.Add(index - 1);
                }
                else
                {
                    // Starting triangle
                    faces.Add(0);
                    faces.Add(1);
                    faces.Add(index);
                    for (int j = 1; j < index - 1; j++)
                    {
                        faces.Add(j);
                        faces.Add(j + index);
                        faces.Add(j + index - 1);
                        faces.Add(j + 1);
                        faces.Add(j + index);
                        faces.Add(j);
                    }
                    // Ending triangle
                    faces.Add(index - 1);
                    faces.Add(index + index - 3);
                    faces.Add(index - 2);
                }
                result.Add(new Tuple<List<int>, List<float>>(faces, vertices));
            }
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
            var meshRayIntersection = Rhino.Geometry.Intersect.Intersection.MeshRay(_baseTerrain, ray);
            return meshRayIntersection == double.NegativeInfinity ? false : true;
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

using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace terrainOptimizer
{
    public class GHC_MeshFlow : GH_Component
    {

        public List<Raindrop> raindrops;
        public HashSet<Point3d> visited;
        public Dictionary<Point3d, HashSet<int>> outputFaces;
        public Dictionary<Point3d, System.Drawing.Color> catchments;
        public List<Polyline> outputPolylines;
        public Mesh mesh;
        public List<int> meshFaces;

        public GHC_MeshFlow()
          : base("terrainOptimizer", "Nickname",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("points", "points", "", GH_ParamAccess.list);
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
            pManager.AddBooleanParameter("reset", "reset", "", GH_ParamAccess.item);

        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("lines", "", "", GH_ParamAccess.list);
            pManager.AddIntegerParameter("faces", "", "", GH_ParamAccess.list);
            pManager.AddMeshParameter("mesh", "", "", GH_ParamAccess.list);
        }
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            List<Point3d> outputPoints = new List<Point3d>();
            outputPolylines = new List<Polyline>();
           // List<Point3d> points = new List<Point3d>();

            bool reset = false;

            //DA.GetDataList(0, points);
            DA.GetData(1, ref mesh);
            DA.GetData(2, ref reset);

            mesh.RebuildNormals();


            if (raindrops == null || outputFaces == null || reset)
            {
                raindrops = new List<Raindrop>();
                outputFaces = new Dictionary<Point3d, HashSet<int>>();
                catchments = new Dictionary<Point3d, System.Drawing.Color>();
                meshFaces = new List<int>();
                for (int j = 0; j < mesh.Faces.Count; j++)
                    meshFaces.Add(j);

                //foreach (var pt in points)
                //{
                //    Raindrop raindrop = new Raindrop(mesh, pt);
                //    if (raindrop.Inactive)
                //        continue;

                //    raindrops.Add(raindrop);
                //}
            }

            var random = new Random();

            var pts = new List<Point3d>();
            int i = 0;


            while (i < 100 && meshFaces.Count > 0)
            {
                //break;
                var faceIndex = random.Next(meshFaces.Count);
                var pt = GetRandomPointOnMeshFace(mesh, meshFaces[faceIndex]);
                Raindrop raindrop = new Raindrop(mesh, meshFaces[faceIndex], pt);
                if (raindrop.Inactive)
                    continue;

                raindrops.Add(raindrop);
                if (meshFaces.Count >= 1)
                    meshFaces.RemoveAt(faceIndex);
                else
                    break;
                i++;
            }


            foreach (var raindrop in raindrops)
            {
                if (raindrop.BottomPoint != Point3d.Unset)
                {
                    outputPolylines.Add(raindrop.FlowLine);
                    if (!outputFaces.ContainsKey(raindrop.BottomPoint))
                    {
                        outputFaces.Add(raindrop.BottomPoint, raindrop.VisitedFaces);
                        catchments.Add(raindrop.BottomPoint, System.Drawing.Color.FromArgb(random.Next(256), random.Next(256), random.Next(256)));
                    }
                    else
                        outputFaces[raindrop.BottomPoint].UnionWith(raindrop.VisitedFaces);

                    continue;
                }
                raindrop.Flow();
                outputPoints.Add(raindrop.Location);
                outputPolylines.Add(raindrop.FlowLine);
            }

            mesh.VertexColors.CreateMonotoneMesh(System.Drawing.Color.Beige);

            foreach (var kvp in outputFaces)
            {
                foreach (var face in kvp.Value)
                {
                    var a = mesh.Faces[face].A;
                    var b = mesh.Faces[face].B;
                    var c = mesh.Faces[face].C;
                    mesh.VertexColors[a] = catchments[kvp.Key];
                    mesh.VertexColors[b] = catchments[kvp.Key];
                    mesh.VertexColors[c] = catchments[kvp.Key];
                }
            }


           // DA.SetDataList(0, pts);
            //DA.SetDataList(1, faces);
            //DA.SetData(2, mesh);
        }

        public override void DrawViewportWires(IGH_PreviewArgs args)
        {
            base.DrawViewportWires(args);
            //if (outputPolylines.Count > 0)
            //    foreach (var polyline in outputPolylines)
            //        args.Display.DrawPolyline(polyline, System.Drawing.Color.CornflowerBlue);
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            base.DrawViewportMeshes(args);
            args.Display.DrawMeshFalseColors(mesh);
        }
        
        private Point3d GetRandomPointOnMeshFace(Mesh mesh, int faceIndex)
        {
            var face = mesh.Faces[faceIndex];
            Vector3f ab = mesh.Vertices[face.B] - mesh.Vertices[face.A];
            Vector3f ac = mesh.Vertices[face.C] - mesh.Vertices[face.A];
            Vector3f bc = mesh.Vertices[face.C] - mesh.Vertices[face.B];

            float distanceToEdge = 0.1f;
            Point3f a = mesh.Vertices[face.A] + ab * distanceToEdge + ac * distanceToEdge;
            Point3f b = mesh.Vertices[face.B] + bc * distanceToEdge + ab * -distanceToEdge;
            Point3f c = mesh.Vertices[face.C] + bc * -distanceToEdge + ac * -distanceToEdge;

            Vector3f abp = b - a;
            Vector3f acp = c - a;

            var random = new Random();
            var u = random.Next(1000);
            var v = random.Next(1000 - u);

            return a + abp * u * 0.001f + acp * v * 0.001f;
        }

        public class Raindrop
        {
            private Mesh Mesh;
            private bool FlowingAlongEdge;
            public Point3d Location;
            public Vector3d FlowDirection;
            public int CurrentFace;
            public int? NextFace;
            public int ActiveEdge;
            public Point3d NextIntersection;
            public double Velocity = 0.5;
            public double RemainingDistance;
            public Polyline FlowLine;
            public Point3d BottomPoint = Point3d.Unset;
            public bool Inactive;
            public HashSet<int> VisitedFaces;
            public int? BottomFace;


            public Raindrop(Mesh mesh, int faceIndex, Point3d point)
            {
                Mesh = mesh;
                int[] faces;
                //Location = FindRainfallDropPoint(Mesh, point + new Vector3d(0, 0, 10), out faces);
                //Rhino.RhinoApp.WriteLine(Location.ToString());
                Location = point;

                CurrentFace = faceIndex;
                VisitedFaces = new HashSet<int>();

                FlowDirection = FindFlowDirectionVector(Mesh, CurrentFace, Location);
                if (FlowDirection == Vector3d.Zero)
                {
                    VisitedFaces.Add(CurrentFace);
                    BottomFace = CurrentFace;
                    BottomPoint = Location;
                    return;
                }

                if (!IntersectVectorWithFaceEdge(Mesh, CurrentFace, FlowDirection, Location, out NextFace, out NextIntersection))
                {
                    //Rhino.RhinoApp.WriteLine("intersection");
                    Inactive = true;
                    return;
                }
                RemainingDistance = (NextIntersection - Location).Length;

                FlowLine = new Polyline();
                FlowLine.Add(Location);
            }

            public void Flow()
            {
                if (Velocity < RemainingDistance)
                {
                    Location += FlowDirection * Velocity;
                    FlowLine.Add(Location);
                    RemainingDistance -= Velocity;
                }
                else
                {
                    Location = new Point3d(NextIntersection);
                    FlowLine.Add(Location);
                    if (!FlowingAlongEdge)
                    {
                        CurrentFace = (int)NextFace;
                        VisitedFaces.Add(CurrentFace);
                        FlowDirection = FindFlowDirectionVector(Mesh, CurrentFace, Location);

                        if (!IntersectVectorWithFaceEdge(Mesh, CurrentFace, FlowDirection, Location, out NextFace, out NextIntersection))
                            FlowAlongEdge(Mesh, ActiveEdge);

                    }
                    else
                        FindNextFace(Mesh, Location);

                    RemainingDistance = (NextIntersection - Location).Length;

                }
            }


            private void FindNextFace(Mesh mesh, Point3d vertex)
            {
                var topologyVertices = mesh.TopologyEdges.GetTopologyVertices(ActiveEdge);
                var meshVertex1 = mesh.TopologyVertices.MeshVertexIndices(topologyVertices.I)[0];
                var meshVertex2 = mesh.TopologyVertices.MeshVertexIndices(topologyVertices.J)[0];

                var lowerVertex = mesh.Vertices[meshVertex1].Z > mesh.Vertices[meshVertex2].Z ? topologyVertices.J : topologyVertices.I;
                var connectedEdges = mesh.TopologyVertices.ConnectedEdges(lowerVertex);

                var steepestEdge = new Vector3d();
                for (int i = 0; i < connectedEdges.Length; i++)
                {
                    var edgeLine = mesh.TopologyEdges.EdgeLine(connectedEdges[i]);
                    if (edgeLine.From != vertex)
                        edgeLine.Flip();

                    var flowVector = edgeLine.Direction;
                    flowVector.Unitize();

                    if (flowVector.Z < steepestEdge.Z)
                    {
                        steepestEdge = flowVector;
                        ActiveEdge = connectedEdges[i];
                    }
                }
                if (steepestEdge == new Vector3d())
                {
                    BottomFace = CurrentFace;
                    BottomPoint = vertex;
                    return;
                }

                CurrentFace = FindLowerFace(mesh, ActiveEdge);
                FlowDirection = FindFlowDirectionVector(mesh, CurrentFace, Location);

                if (!IntersectVectorWithFaceEdge(Mesh, CurrentFace, FlowDirection, Location, out NextFace, out NextIntersection))
                    FlowAlongEdge(Mesh, ActiveEdge);
                else
                    FlowingAlongEdge = false;

            }

            private int FindLowerFace(Mesh mesh, int edge)
            {

                var faces = mesh.TopologyEdges.GetConnectedFaces(edge);
                if (faces.Length == 1)
                    return faces[0];

                int? _nextFace;
                Point3d _nextIntersection;
                var flow0 = FindFlowDirectionVector(mesh, faces[0], Location);
                var face0 = IntersectVectorWithFaceEdge(mesh, faces[0], flow0, Location, out _nextFace, out _nextIntersection);
                var flow1 = FindFlowDirectionVector(mesh, faces[1], Location);
                var face1 = IntersectVectorWithFaceEdge(mesh, faces[1], flow1, Location, out _nextFace, out _nextIntersection);

                if (face0 && !face1)
                    return faces[0];
                else if (!face0 && face1)
                    return faces[1];
                else if (face0 && face1)
                {
                    flow0.Unitize();
                    flow1.Unitize();
                    return flow0.Z < flow1.Z ? faces[0] : faces[1];
                }
                else
                    return faces[0];
            }


            private void FlowAlongEdge(Mesh mesh, int edge)
            {
                var edgeLine = mesh.TopologyEdges.EdgeLine(edge);
                FlowDirection = edgeLine.FromZ > edgeLine.ToZ ? edgeLine.Direction : -edgeLine.Direction;
                FlowDirection.Unitize();
                NextIntersection = edgeLine.FromZ > edgeLine.ToZ ? edgeLine.To : edgeLine.From;
                FlowingAlongEdge = true;
            }

            private bool IntersectVectorWithFaceEdge(Mesh mesh, int face, Vector3d vector, Point3d samplePoint, out int? nextFace, out Point3d intersectionPoint)
            {
                var edges = mesh.TopologyEdges.GetEdgesForFace(face);
                nextFace = null;
                intersectionPoint = new Point3d();

                Line extendedVector = new Line(samplePoint, Point3d.Add(samplePoint, vector * 1000));

                double a, b;
                foreach (var edge in edges)
                {
                    var edgeLine = mesh.TopologyEdges.EdgeLine(edge);

                    if (edgeLine.MinimumDistanceTo(samplePoint) <= 0.000001)
                        continue;

                    if (Intersection.LineLine(edgeLine, extendedVector, out a, out b, 0.000001, true))
                    {
                        ActiveEdge = edge;
                        intersectionPoint = edgeLine.PointAt(a);
                        var connectedFaces = mesh.TopologyEdges.GetConnectedFaces(edge);
                        if (connectedFaces.Length == 1)
                            nextFace = connectedFaces[0];
                        else
                            nextFace = connectedFaces[0] != face ? connectedFaces[0] : connectedFaces[1];
                        return true;
                    }
                }

                return false;
            }

            private Point3d FindRainfallDropPoint(Mesh mesh, Point3d point, out int[] faces)
            {
                Ray3d ray = new Ray3d(point, -Vector3d.ZAxis);
                var intersection = Intersection.MeshRay(mesh, ray, out faces);
                return ray.PointAt(intersection);
            }

            private Vector3d FindFlowDirectionVector(Mesh mesh, int face, Point3d intersectionPoint)
            {
                var normal = mesh.FaceNormals[face];
                var flowVector = Vector3d.CrossProduct(Vector3d.ZAxis, normal);
                flowVector.Unitize();
                flowVector.Transform(Transform.Rotation(Math.PI * 1.5, normal, intersectionPoint));

                return flowVector;
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
            get { return new Guid("8f49e3db-3ad6-4d23-a675-ee8e1826fc22"); }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Rhino;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace terrainOptimizer
{
    public class MeshTraversal
    {
        public static int FindNextFace(ref Mesh mesh, int currentIndex, Line crossingLine)
        {
            var edgesForCurrentFace = mesh.TopologyEdges.GetEdgesForFace(currentIndex);
            for (int i = 0; i < edgesForCurrentFace.Length; i++)
            {
                var edge = mesh.TopologyEdges.EdgeLine(edgesForCurrentFace[i]);
                var intersectionFound = Intersection.LineLine(edge, crossingLine, out double a, out _);

                if (intersectionFound)
                {
                    var connectedFaces = mesh.TopologyEdges.GetConnectedFaces(edgesForCurrentFace[i]);
                    if (connectedFaces.Length == 1) // Naked edge
                        return -1;

                    if (connectedFaces.Length == 2)
                        return connectedFaces[0] != currentIndex ? connectedFaces[0] : connectedFaces[1];
                }
            }
            return -1;
        }

        public static bool PointInMeshFace(ref Mesh mesh, int faceIndex, Point3d point)
        {
            var a = (Point3d)mesh.Vertices[mesh.Faces[faceIndex].A];
            var b = (Point3d)mesh.Vertices[mesh.Faces[faceIndex].B];
            var c = (Point3d)mesh.Vertices[mesh.Faces[faceIndex].C];
            return PointInTriangle(a, b, c, point);
        }

        public static bool PointInTriangle(Point3d a, Point3d b, Point3d c, Point3d point)
        {
            double s1 = c.Y - a.Y;
            double s2 = c.X - a.X;
            double s3 = b.Y - a.Y;
            double s4 = point.Y - a.Y;

            double w1 = (a.X * s1 + s4 * s2 - point.X * s1) / (s3 * s2 - (b.X - a.X) * s1);
            double w2 = (s4 - w1 * s3) / s1;
            return w1 >= 0 && w2 >= 0 && (w1 + w2) <= 1;
        }
    }
}

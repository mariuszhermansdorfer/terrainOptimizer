using Rhino.Geometry;
using System;
using System.Runtime.InteropServices;

namespace terrainOptimizer.Helpers
{
    public class MeshApi
    {

        [StructLayout(LayoutKind.Sequential)]
        public struct RawMeshArrays
        {
            public IntPtr Faces;
            public int FacesLength;
            public IntPtr Vertices;
            public int VerticesLength;
            public IntPtr VertexValues;
            public int VertexValuesLength;
            public float Cut;
            public float Fill;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawPolylineArrays
        {
            public IntPtr ContourVertices;
            public IntPtr ContourVerticesLengths;
            public int ContourCount;
            public IntPtr LabelVertices;
            public IntPtr LabelNormals;
            public int LabelCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RawMeshPointers
        {
            public IntPtr Faces;
            public int FacesLength;
            public IntPtr Vertices;
            public int VerticesLength;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct RawPolylinePointers
        {
            public IntPtr Vertices;
            public int VerticesLength;
        };

        public enum BooleanOperation
        {
            InsideA,
            InsideB,
            OutsideA,
            OutsideB,
            Union,
            Intersection,
            DifferenceBA,
            DifferenceAB
        }

        public enum CuttingOperation { DeleteInside, DeleteOutside, None }

        [DllImport("MRMesh.dll")]
        public static extern IntPtr CreateMesh(int[] triangles, int trianglesLength, float[] coordinates, int coordinatesLength);

        [DllImport("MRMesh.dll")]
        public unsafe static extern IntPtr CreateMeshFromPointers(Point3f* verticesPtr, int verticesLength, int* trianglesPtr, int trianglesLength);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        public delegate bool RhinoProgressCallback(float progress);

        [DllImport("MRMesh.dll")]
        public static extern IntPtr ImportMesh(string path, float edgeLength, RhinoProgressCallback progressCallback);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays RetrieveMesh(IntPtr mesh);

        [DllImport("MRMesh.dll")]
        public static extern void DeleteMesh(IntPtr mesh);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays BooleanMeshes(IntPtr meshA, IntPtr meshb, BooleanOperation operation);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshPointers MixMeshes(IntPtr meshA, IntPtr meshb, float fillAngle, float cutAngle, float anglePrecision);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays CutMeshWithPolyline(IntPtr meshA, float[] coordinates, int coordinatesLength, CuttingOperation direction);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays RemeshMesh(IntPtr mesh, float targetLength, float shift, int iterations, float sharpAngle);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays Distance(IntPtr proposedMesh, IntPtr baseMesh, float resolution);
        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays GridRemesh(IntPtr proposedMesh, float resolution);

        [DllImport("MRMesh.dll")]
        public static extern RawPolylineArrays CreateContours(IntPtr mesh, float interval, bool showLabels, float spacing);

        [DllImport("MRMesh.dll")]
        public static extern RawPolylineArrays CreateContoursPointers(IntPtr mesh, float interval, bool showLabels, float spacing);

        [DllImport("MRMesh.dll")]
        public static extern void AnalyzeFlow(IntPtr mesh, float resolution);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays Sculpt(IntPtr mesh, float[] coordinates, float radius);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays SoapFilm(float[] coordinates, int coordinatesLength, float edgeLength, int iterations, float pressure);

        [DllImport("MRMesh.dll")]
        public unsafe static extern RawMeshArrays Offset(float[] coordinates, int polylineCount, int[] polylinesLength, float offset, float epsilon, int iterations, float smoothingFactor);

        public unsafe static IntPtr CreateMRMesh(Mesh mesh)
        {
            IntPtr ptr;
            unsafe
            {
                using (var meshAccess = mesh.GetUnsafeLock(true))
                {
                    Point3f* startOfVertexArray = meshAccess.VertexPoint3fArray(out int vertexArrayLength);
                    int[] faces = mesh.Faces.ToIntArray(true);
                    fixed (int* facesPointer = faces)
                    {
                        ptr = CreateMeshFromPointers(startOfVertexArray, mesh.Vertices.Count, facesPointer, faces.Length);
                    }

                    mesh.ReleaseUnsafeLock(meshAccess);
                }
            }
            return ptr;
        }

    }
}
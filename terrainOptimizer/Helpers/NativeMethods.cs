using System;
using System.Runtime.InteropServices;

namespace terrainOptimizer.Helpers
{
    public class NativeMethods
    {
       
        [DllImport("pmp.dll")]
        public static extern void DeleteFacesArray(IntPtr facesArray);

        [DllImport("pmp.dll")]
        public static extern void DeleteVertexArray(IntPtr vertexArray);

        [DllImport("geometry-central.dll")]
        public static extern IntPtr CreateMeshFromFloatArray(int[] faces, int facesLength);

        [DllImport("geometry-central.dll")]
        public static extern IntPtr CreateVertexGeometry(IntPtr mesh, float[] vertices, int verticesLength);

        [DllImport("geometry-central.dll")]
        public static extern void GCRemesh(IntPtr mesh, IntPtr vertexGeometry, double targetLength, int iterations, int style);

        [DllImport("geometry-central.dll")]
        public static extern IntPtr GCFacesToIntArray(IntPtr mesh);

        [DllImport("geometry-central.dll")]
        public static extern IntPtr GCVerticesToFloatArray(IntPtr mesh, IntPtr vertexGeometry);

        [DllImport("geometry-central.dll")]
        public static extern int GCVerticesCount(IntPtr mesh);

        [DllImport("geometry-central.dll")]
        public static extern int GCFacesCount(IntPtr mesh);

        [DllImport("geometry-central.dll")]
        public static extern void ProjectPolylineToMesh(IntPtr mesh, IntPtr vertexGeometry, double[] polyline, int faceId, int count);

        [DllImport("geometry-central.dll")]
        public static extern void CutMeshHole(IntPtr mesh, IntPtr vertexGeometry, double[] polyline, int faceId, int count); 

        [StructLayout(LayoutKind.Sequential)]
        public struct GCMesh
        {
            public IntPtr Mesh;
            public IntPtr Vertices;
        }


        [DllImport("openvdb.dll")]
        public static extern IntPtr CreateTransform(float voxelSize);

        [DllImport("openvdb.dll")]
        public static extern IntPtr CreateFloatGrid(IntPtr transformPtr);

        [DllImport("openvdb.dll")]
        public static extern IntPtr CreateBoundingBox(IntPtr floatGrid, float minX, float minY, float minZ, float maxX, float maxY, float maxZ);

        [DllImport("openvdb.dll")]
        public static extern IntPtr CreateMeshGrid(IntPtr transformPtr, float[] vertices, int verticesLength, int[] faces, int facesLength);

        [DllImport("openvdb.dll")]
        public static extern void MergeGridsAndOutput(float voxelSize, IntPtr gridPtr, IntPtr meshGridPtr, IntPtr bboxPtr);

        [DllImport("openvdb.dll")]
        public static extern void DeleteFloatGrid(IntPtr grid);

        [DllImport("openvdb.dll")]
        public static extern void DeleteBoundingBox(IntPtr grid);

        [DllImport("openvdb.dll")]
        public static extern void CreateMeshGridFromBoundingBox();



        [StructLayout(LayoutKind.Sequential)]
        public struct BoolMesh
        {
            public IntPtr Triangles;
            public IntPtr Coordinates;
            public IntPtr Labels;
            public int TrianglesLength;
            public int CoordinatesLength;
            public int LabelsLength;
        }

        [DllImport("mesh_booleans.dll")]
        public static extern IntPtr CreateBoolMesh(int[] triangles, int trianglesLength, float[] coordinates, int coordinatesLength);

        [DllImport("mesh_booleans.dll")]
        public static extern BoolResults CollideBaseWithCutter(IntPtr baseMesh, int[] triangles, int trianglesLength, float[] coordinates, int coordinatesLength, int operationType);

        [StructLayout(LayoutKind.Sequential)]
        public struct BoolResults
        {
            public IntPtr Faces;
            public int FacesLength;
            public IntPtr Vertices;
            public int VerticesLength;
        }

        [DllImport("mesh_booleans.dll")]
        public static extern IntPtr Trimesh(int[] triangles, int trianglesLength, float[] coordinates, int coordinatesLength);

        [DllImport("mesh_booleans.dll")]
        public static extern BoolResults CinoRemesh(IntPtr baseMesh, int iterations, double targetLength, bool preserve);


        [DllImport("embree_raytracer.dll")]
        public static extern void RaytracerTest(float[] vertices, int verticesLength, int[] faces, int facesLength);

        [DllImport("embree_raytracer.dll")]
        public static extern void createBaseMesh(float[] vertices1, int verticesLength1, int[] faces1, int facesLength1, float[] vertices2, int verticesLength2, int[] faces2, int facesLength2);

        [DllImport("embree_raytracer.dll")]
        public static extern void BVHTest();

        [StructLayout(LayoutKind.Sequential)]
        public struct OffsetResults
        {
            public IntPtr Vertices;
            public int VerticesLength;
        }
        [DllImport("ClipperApi.dll")]
        public static extern OffsetResults Offset(float[] coordinates, int numCoordinates, double delta, double miterLimit, int precision, bool simplify, double epsilon);

        [StructLayout(LayoutKind.Sequential)]
        public struct WrapResults
        {
            public IntPtr Faces;
            public int FacesLength;
            public IntPtr Vertices;
            public int VerticesLength;
        }

        [DllImport("rhino_mesh_wrap.dll")]
        public static extern WrapResults TestWrap(float[] vertices, int verticesLength, int[] faces, int facesLength);

    }
}

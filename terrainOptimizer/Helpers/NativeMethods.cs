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


    }
}

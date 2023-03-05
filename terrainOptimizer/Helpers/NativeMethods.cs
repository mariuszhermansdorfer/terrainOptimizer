using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace terrainOptimizer.Helpers
{
    public class NativeMethods
    {
        [DllImport("pmp.dll")]
        public static extern IntPtr CreateSurfaceMeshFromRawArrays(float[] vertices, int[] faces, int verticesLength, int facesLength);

        [DllImport("pmp.dll")]
        public static extern void Remesh(IntPtr mesh, float targetLength, int amountOfIterations, double angle);

        [DllImport("pmp.dll")]
        public static extern IntPtr FacesToIntArray(IntPtr mesh);

        [DllImport("pmp.dll")]
        public static extern IntPtr VerticesToFloatArray(IntPtr mesh);

        [DllImport("pmp.dll")]
        public static extern int VerticesCount(IntPtr mesh);

        [DllImport("pmp.dll")]
        public static extern int FacesCount(IntPtr mesh);

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
        public static extern void PokeFace(IntPtr mesh, IntPtr vertexGeometry, int faceId);

        [DllImport("geometry-central.dll")]
        public static extern void ProjectPolylineToMesh(IntPtr mesh, IntPtr vertexGeometry, double[] polyline, int faceId, int count);

        [DllImport("geometry-central.dll")]
        public static extern GCMesh MeshSurgery(IntPtr mesh, float[] vertices, int verticesLength, int faceId);

        [DllImport("geometry-central.dll")]
        public static extern void CutHole(IntPtr mesh, int faceId);

        [DllImport("geometry-central.dll")]
        public static extern void CutMeshHole(IntPtr mesh, IntPtr vertexGeometry, double[] polyline, int faceId, int count); 

        [StructLayout(LayoutKind.Sequential)]
        public struct GCMesh
        {
            public IntPtr Mesh;
            public IntPtr Vertices;
        }

    }
}

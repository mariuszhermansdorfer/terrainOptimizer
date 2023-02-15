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
    }
}

using Rhino.Geometry;
using System;
using System.Runtime.InteropServices;
using static MeshAPI.Structs;


namespace MeshAPI
{
    internal static class NativeMethods
    {
        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern IntPtr CreateMeshFromPointers(Point3f* vertices, int vertexCount, int* faces, int faceCount);

        [DllImport("MRMesh.dll")]
        internal static extern Structs.RawMeshPointers CopyMeshPointers(IntPtr mesh);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DeleteMesh(IntPtr mesh);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreeRawMeshData(ref Structs.RawMeshPointers rawMeshData);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void FreeAdditionalMeshData(ref Structs.AdditionalMeshData additionalMeshData);



        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr BooleanMeshes(IntPtr meshA, IntPtr meshb, Structs.BooleanOperation operation);
        
        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DistanceBetweenMeshes(IntPtr baseMesh, IntPtr proposedMesh, float resolution, out AdditionalMeshData extraData);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EmbedMesh(IntPtr meshA, IntPtr meshb, float fillAngle, float cutAngle, float anglePrecision);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GridRemesh(IntPtr proposedMesh, float resolution);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr Inflate(IntPtr mesh, int iterations, float pressure);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr MinimalSurface(float[] coordinates, int coordinatesLength, float edgeLength);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr Remesh(IntPtr mesh, float targetLength, float shift, int iterations, float sharpAngle);
    }
}
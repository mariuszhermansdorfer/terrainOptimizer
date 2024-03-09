using Rhino.Geometry;
using System;
using System.Runtime.InteropServices;


namespace MeshAPI
{
    internal static class NativeMethods
    {
        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal unsafe static extern IntPtr CreateMeshFromPointers(Point3f* vertices, int vertexCount, int* faces, int faceCount);

        [DllImport("MRMesh.dll")]
        internal static extern RawMeshPointers CopyMeshPointers(IntPtr mesh);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void DeleteMesh(IntPtr mesh);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GridRemesh(IntPtr proposedMesh, float resolution);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr BooleanMeshes(IntPtr meshA, IntPtr meshb, Structs.BooleanOperation operation);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr Remesh(IntPtr mesh, float targetLength, float shift, int iterations, float sharpAngle);
    }
}
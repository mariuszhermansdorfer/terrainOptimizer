using Rhino.Geometry;
using System;
using System.Runtime.InteropServices;
using static MeshAPI.Structs;


namespace MeshAPI
{
    internal static class NativeMethods
    {
        #region Create and dispose Meshes
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
        #endregion


        #region Mesh editing
        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr BooleanMeshes(IntPtr meshA, IntPtr meshb, Structs.BooleanOperation operation);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CurveToGridMesh(float[] coordinates, int coordinatesLength, float resolution);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr CutWithPolyline(IntPtr mesh, float[] coordinates, int coordinatesLength, Structs.CuttingOperation direction, bool project);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr DistanceBetweenMeshes(IntPtr baseMesh, IntPtr proposedMesh, float resolution, out AdditionalMeshData extraData);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr EmbedMesh(IntPtr meshA, IntPtr meshb, float fillAngle, float cutAngle, float anglePrecision);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr GridRemesh(IntPtr mesh, float resolution);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr ProjectGridToMesh(IntPtr mesh, float resolution, float[] coordinates, int coordinatesLength);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr Inflate(IntPtr mesh, int iterations, float pressure);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr MinimalSurface(float[] coordinates, int coordinatesLength, float edgeLength);

        [DllImport("MRMesh.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IntPtr Remesh(IntPtr mesh, float targetLength, float shift, int iterations, float sharpAngle);
        #endregion


        #region Common functionality
        [DllImport("MRCuda.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern void RayCast(IntPtr mesh, Point3d[] samplesArray, int samplesLength, Vector3d[] directionsArray, int directionsLength, bool useGPU, [In, Out] float[] occlusions, [In, Out] Point3d[] points);

        #endregion
    }
}
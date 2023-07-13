﻿using System;
using System.Runtime.InteropServices;

namespace terrainOptimizer.Helpers
{
    public class NativeMeshMethods
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
        };

        [DllImport("MRMesh.dll")]
        public static extern IntPtr CreateMesh(int[] triangles, int trianglesLength, float[] coordinates, int coordinatesLength);
       
        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays BooleanMeshes(IntPtr meshA, IntPtr meshb, BooleanOperation operation);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays CutMeshWithPolyline(IntPtr meshA, float[] coordinates, int coordinatesLength, bool remove);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays RemeshMesh(IntPtr mesh, float targetLength, float shift, int iterations, float sharpAngle);

        [DllImport("MRMesh.dll")]
        public static extern RawMeshArrays Distance(IntPtr proposedMesh, IntPtr baseMesh, float resolution);

    }
}

using System;
using System.Runtime.InteropServices;


namespace MeshAPI
{
    public class Structs
    {
        /// <summary>
        /// Represents mesh pointers specifically for faces, vertices, and normals, including lengths for faces and vertices.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct RawMeshPointers
        {
            public IntPtr Faces;
            public int FacesLength;
            public IntPtr Vertices;
            public int VerticesLength;
            public IntPtr Normals;
        };

        /// <summary>
        /// Holds additional mesh data including pointers to vertex values, vertices length, and parameters for fill and cut.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct AdditionalMeshData
        {
            public IntPtr VertexValues;
            public int VerticesLength;
            public float Fill;
            public float Cut;
        }

        /// <summary>
        /// Enumerates types of boolean operations that can be performed with meshes, including various inside/outside and set operations.
        /// </summary>
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

        /// <summary>
        /// Enumerates types of cutting operations that can be applied to meshes, specifying how the cut influences the mesh data.
        /// </summary>
        public enum CuttingOperation { 
            DeleteInside, 
            DeleteOutside, 
            None }
    }
}

using System;
using Rhino.Geometry;


namespace MeshAPI
{
    public class FastMesh : IDisposable
    {
        private IntPtr meshPtr = IntPtr.Zero;


        public FastMesh(Mesh mesh)
        {
            meshPtr = CreateMRMesh(mesh);
        }

        private static IntPtr CreateMRMesh(Mesh mesh)
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
                        ptr = NativeMethods.CreateMeshFromPointers(startOfVertexArray, mesh.Vertices.Count, facesPointer, faces.Length);
                    }

                    mesh.ReleaseUnsafeLock(meshAccess);
                }
            }
            return ptr;
        }

        public void Dispose()
        {
            if (meshPtr == IntPtr.Zero)
                return;

            NativeMethods.DeleteMesh(meshPtr);
            meshPtr = IntPtr.Zero;
        }

        public Mesh ToRhinoMesh()
        {
            RawMeshPointers pointers = NativeMethods.CopyMeshPointers(meshPtr);
            Mesh rhinoMesh = new Mesh();
            rhinoMesh.Vertices.Count = pointers.VerticesLength;
            rhinoMesh.Normals.Count = pointers.VerticesLength;
            rhinoMesh.Faces.Count = pointers.FacesLength;

            if (pointers.FacesLength <= 0)
                return rhinoMesh;

            unsafe
            {
                using (var meshAccess = rhinoMesh.GetUnsafeLock(true))
                {
                    Point3f* startOfVertexArray = meshAccess.VertexPoint3fArray(out int vertexArrayLength);
                    Vector3f* startOfNormalsArray = meshAccess.NormalVector3fArray(out int normalArrayLength);

                    Buffer.MemoryCopy((float*)pointers.Vertices, startOfVertexArray, vertexArrayLength * sizeof(Point3f), pointers.VerticesLength * sizeof(float) * 3);
                    Buffer.MemoryCopy((float*)pointers.Normals, startOfNormalsArray, vertexArrayLength * sizeof(Vector3f), pointers.VerticesLength * sizeof(float) * 3);

                    MeshFace* startOfFacesArray = meshAccess.FacesArray(out int facesArrayLength);
                    
                    int* sourcePtr = (int*)pointers.Faces;
                    int* destPtr = (int*)startOfFacesArray;
                    
                    for (int i = 0; i < pointers.FacesLength; i++)
                    {
                        *destPtr++ = *sourcePtr++;
                        *destPtr++ = *sourcePtr++;
                        *destPtr++ = *sourcePtr; // MeshFace.C & D are the same for a triangle face
                        *destPtr++ = *sourcePtr++;
                    }

                    rhinoMesh.ReleaseUnsafeLock(meshAccess);
                }
            }
            NativeMethods.FreeRawMeshData(ref pointers);
            return rhinoMesh;
        }


        public FastMesh BooleanMeshes(FastMesh cutterMesh, Structs.BooleanOperation operation)
        {
            IntPtr booleanResultPointer = NativeMethods.BooleanMeshes(meshPtr, cutterMesh.meshPtr, operation);

            return new FastMesh(booleanResultPointer);
        }

        public FastMesh GridRemesh(float resolution)
        {
            IntPtr gridMeshPointer = NativeMethods.GridRemesh(meshPtr, resolution);

            return new FastMesh(gridMeshPointer);
        }

        public FastMesh Remesh(float targetLength, float shift, int iterations, float sharpAngle)
        {
            IntPtr remeshPointer = NativeMethods.Remesh(meshPtr, targetLength, shift, iterations, sharpAngle);

            return new FastMesh(remeshPointer);
        }


        private FastMesh(IntPtr meshPtr)
        {
            this.meshPtr = meshPtr;
        }

        ~FastMesh()
        {
            Dispose();
        }
    }
}

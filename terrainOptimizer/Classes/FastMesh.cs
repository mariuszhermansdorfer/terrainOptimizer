using System;
using System.Runtime.InteropServices;
using Rhino.Geometry;
using static MeshAPI.Structs;


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
            Structs.RawMeshPointers pointers = NativeMethods.CopyMeshPointers(meshPtr);
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

        public FastMesh CutWithPolyline(Curve inputCurve, Structs.CuttingOperation direction, bool project)
        {
            var coordinates = Helpers.GetCurveCoordinates(inputCurve);
            IntPtr resultPointer = NativeMethods.CutWithPolyline(meshPtr, coordinates, coordinates.Length, direction, project);
            return new FastMesh(resultPointer);
        }

        public FastMesh DistanceBetweenMeshes(FastMesh proposedMesh, float resolution, out float[] vertexValues, out float cut, out float fill)
        {
            IntPtr distanceMeshPtr = NativeMethods.DistanceBetweenMeshes(meshPtr, proposedMesh.meshPtr, resolution, out AdditionalMeshData meshData);

            cut = meshData.Cut;
            fill = meshData.Fill;

            vertexValues = new float[meshData.VerticesLength];
            Marshal.Copy(meshData.VertexValues, vertexValues, 0, meshData.VerticesLength);
            NativeMethods.FreeAdditionalMeshData(ref meshData);

            return new FastMesh(distanceMeshPtr);
        }

        public FastMesh EmbedMesh(FastMesh newMesh, float fillAngle, float cutAngle, float anglePrecision)
        {
            IntPtr gridMeshPointer = NativeMethods.EmbedMesh(meshPtr, newMesh.meshPtr, fillAngle, cutAngle, anglePrecision);
            return new FastMesh(gridMeshPointer);
        }

        public FastMesh GridRemesh(float resolution)
        {
            IntPtr gridMeshPointer = NativeMethods.GridRemesh(meshPtr, resolution);
            return new FastMesh(gridMeshPointer);
        }

        public FastMesh Inflate(int iterations, float pressure)
        {
            IntPtr resultPointer = NativeMethods.Inflate(meshPtr, iterations, pressure);
            return new FastMesh(resultPointer);
        }

        public static FastMesh MinimalSurface(Curve inputCurve, float edgeLength)
        {
            var coordinates = Helpers.GetCurveCoordinates(inputCurve);
            IntPtr resultPointer = NativeMethods.MinimalSurface(coordinates, coordinates.Length, edgeLength);
            return new FastMesh(resultPointer);
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

using System;
using System.Runtime.InteropServices;
using Rhino.Geometry;
using static Geometry.Structs;


namespace Geometry
{
    /// <summary>
    /// Represents a high-performance mesh object that interfaces with native code for advanced mesh operations. 
    /// It encapsulates a pointer to a mesh structure in unmanaged memory and provides methods for various mesh manipulations, 
    /// including boolean operations, cutting, remeshing, and more.
    /// </summary>
    public class FastMesh : IDisposable
    {
        public IntPtr NativeMeshPointer = IntPtr.Zero;

        /// <summary>
        /// Initializes a new instance of the FastMesh class by converting a Rhino.Geometry.Mesh into a native mesh representation.
        /// </summary>
        /// <param name="mesh">The Rhino.Geometry.Mesh to be converted.</param>
        public FastMesh(Mesh mesh)
        {
            NativeMeshPointer = CreateFastMesh(mesh);
        }

        private static IntPtr CreateFastMesh(Mesh mesh)
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

        /// <summary>
        /// Releases the unmanaged resources used by the FastMesh.
        /// </summary>
        public void Dispose()
        {
            if (NativeMeshPointer == IntPtr.Zero)
                return;

            NativeMethods.DeleteMesh(NativeMeshPointer);
            NativeMeshPointer = IntPtr.Zero;
        }

        /// <summary>
        /// Converts the native mesh back into a Rhino.Geometry.Mesh.
        /// </summary>
        /// <returns>A new Mesh object representing the native mesh.</returns>
        public Mesh ToRhinoMesh()
        {
            Structs.RawMeshPointers pointers = NativeMethods.CopyMeshPointers(NativeMeshPointer);
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

        /// <summary>
        /// Performs a boolean operation between this mesh and another mesh.
        /// </summary>
        /// <param name="cutterMesh">The mesh to perform the operation with.</param>
        /// <param name="operation">The boolean operation to perform.</param>
        /// <returns>A new FastMesh instance resulting from the boolean operation.</returns>
        public FastMesh BooleanMeshes(FastMesh cutterMesh, Structs.BooleanOperation operation)
        {
            IntPtr booleanResultPointer = NativeMethods.BooleanMeshes(NativeMeshPointer, cutterMesh.NativeMeshPointer, operation);
            return new FastMesh(booleanResultPointer);
        }

        /// <summary>
        /// Generates a grid mesh from a curve with a specified resolution.
        /// </summary>
        /// <param name="resolution">The resolution of the grid mesh.</param>
        /// <param name="inputCurve">The curve to base the mesh on.</param>
        /// <returns>A new FastMesh instance representing the grid mesh.</returns>
        public static FastMesh CurveToGridMesh(float resolution, Curve inputCurve)
        {
            var coordinates = Helpers.GetCurveCoordinates(inputCurve);
            IntPtr gridMeshPointer = NativeMethods.CurveToGridMesh(coordinates, coordinates.Length, resolution);
            return new FastMesh(gridMeshPointer);
        }

        /// <summary>
        /// Cuts the mesh with a polyline derived from the provided curve, optionally projecting the cut.
        /// </summary>
        /// <param name="inputCurve">The curve to cut the mesh with.</param>
        /// <param name="direction">The direction of the cut.</param>
        /// <param name="project">Whether to project the cut through the mesh.</param>
        /// <returns>A new FastMesh instance representing the cut mesh.</returns>
        public FastMesh CutWithPolyline(Curve inputCurve, Structs.CuttingOperation direction, bool project)
        {
            var coordinates = Helpers.GetCurveCoordinates(inputCurve);
            IntPtr resultPointer = NativeMethods.CutWithPolyline(NativeMeshPointer, coordinates, coordinates.Length, direction, project);
            return new FastMesh(resultPointer);
        }

        /// <summary>
        /// Computes the distance between this mesh and another mesh, providing additional mesh data.
        /// </summary>
        /// <param name="comparisonMesh">The mesh to compare against.</param>
        /// <param name="resolution">The resolution for the comparison.</param>
        /// <param name="distancePerVertex">Outputs the distance per vertex.</param>
        /// <param name="cut">Outputs the cut value.</param>
        /// <param name="fill">Outputs the fill value.</param>
        /// <returns>A new FastMesh instance representing the distance mesh.</returns>
        public FastMesh DistanceBetweenMeshes(FastMesh comparisonMesh, float resolution, out float[] distancePerVertex, out float cut, out float fill)
        {
            IntPtr distanceMeshPtr = NativeMethods.DistanceBetweenMeshes(NativeMeshPointer, comparisonMesh.NativeMeshPointer, resolution, out AdditionalMeshData meshData);

            cut = meshData.Cut;
            fill = meshData.Fill;

            distancePerVertex = new float[meshData.VerticesLength];
            Marshal.Copy(meshData.VertexValues, distancePerVertex, 0, meshData.VerticesLength);
            NativeMethods.FreeAdditionalMeshData(ref meshData);

            return new FastMesh(distanceMeshPtr);
        }

        /// <summary>
        /// Embeds another mesh into this mesh, applying specified angles for filling and cutting.
        /// </summary>
        /// <param name="newMesh">The mesh to embed.</param>
        /// <param name="fillAngle">The fill angle.</param>
        /// <param name="cutAngle">The cut angle.</param>
        /// <param name="anglePrecision">The precision of the angles.</param>
        /// <returns>A new FastMesh instance representing the embedded mesh.</returns>
        public FastMesh EmbedMesh(FastMesh newMesh, float fillAngle, float cutAngle, float anglePrecision)
        {
            IntPtr gridMeshPointer = NativeMethods.EmbedMesh(NativeMeshPointer, newMesh.NativeMeshPointer, fillAngle, cutAngle, anglePrecision);
            return new FastMesh(gridMeshPointer);
        }

        /// <summary>
        /// Performs a grid remeshing of this mesh with a specified resolution.
        /// </summary>
        /// <param name="resolution">The resolution for the remeshing.</param>
        /// <returns>A new FastMesh instance representing the remeshed mesh.</returns>
        public FastMesh GridRemesh(float resolution)
        {
            IntPtr gridMeshPointer = NativeMethods.GridRemesh(NativeMeshPointer, resolution);
            return new FastMesh(gridMeshPointer);
        }

        /// <summary>
        /// Performs a grid remeshing of this mesh using a curve with a specified resolution.
        /// </summary>
        /// <param name="resolution">The resolution for the grid remeshing.</param>
        /// <param name="inputCurve">The curve to use in the remeshing process.</param>
        /// <returns>A new FastMesh instance representing the remeshed mesh.</returns>
        public FastMesh GridRemeshWithCurve(float resolution, Curve inputCurve)
        {
            var coordinates = Helpers.GetCurveCoordinates(inputCurve);
            IntPtr gridMeshPointer = NativeMethods.ProjectGridToMesh(NativeMeshPointer, resolution, coordinates, coordinates.Length);
            return new FastMesh(gridMeshPointer);
        }

        /// <summary>
        /// Inflates this mesh using a specified number of iterations and pressure.
        /// </summary>
        /// <param name="iterations">The number of iterations to inflate.</param>
        /// <param name="pressure">The pressure to use in inflation.</param>
        /// <returns>A new FastMesh instance representing the inflated mesh.</returns>
        public FastMesh Inflate(int iterations, float pressure)
        {
            IntPtr resultPointer = NativeMethods.Inflate(NativeMeshPointer, iterations, pressure);
            return new FastMesh(resultPointer);
        }

        /// <summary>
        /// Creates a minimal surface mesh from a curve with a specified edge length.
        /// </summary>
        /// <param name="inputCurve">The curve to base the minimal surface on.</param>
        /// <param name="edgeLength">The edge length of the minimal surface.</param>
        /// <returns>A new FastMesh instance representing the minimal surface mesh.</returns>
        public static FastMesh MinimalSurface(Curve inputCurve, float edgeLength)
        {
            var coordinates = Helpers.GetCurveCoordinates(inputCurve);
            IntPtr resultPointer = NativeMethods.MinimalSurface(coordinates, coordinates.Length, edgeLength);
            return new FastMesh(resultPointer);
        }

        /// <summary>
        /// Remeshes this mesh towards a target length, applying specified parameters for shift, iterations, and sharp angles.
        /// </summary>
        /// <param name="targetLength">The target length for the remeshing.</param>
        /// <param name="shift">The shift value for the remeshing process.</param>
        /// <param name="iterations">The number of iterations to perform.</param>
        /// <param name="sharpAngle">The angle to consider as sharp.</param>
        /// <returns>A new FastMesh instance representing the remeshed mesh.</returns>
        public FastMesh Remesh(float targetLength, float shift, int iterations, float sharpAngle)
        {
            IntPtr remeshPointer = NativeMethods.Remesh(NativeMeshPointer, targetLength, shift, iterations, sharpAngle);
            return new FastMesh(remeshPointer);
        }


        private FastMesh(IntPtr meshPtr)
        {
            this.NativeMeshPointer = meshPtr;
        }

        ~FastMesh()
        {
            Dispose();
        }
    }
}

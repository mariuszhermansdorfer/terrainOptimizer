using Rhino.Geometry;


namespace MeshAPI
{
    public class RayCasting
    {
        /// <summary>
        /// Performs ray casting on a mesh to compute occlusions for a set of sample points and directions, optionally utilizing GPU acceleration.
        /// Given a mesh, an array of sample points, and an array of direction vectors, this method computes the occlusion values for each sample point along its direction vector.
        /// The occlusion value represents the presence or absence of obstruction between the sample point and its projection along the direction vector.
        /// </summary>
        /// <param name="mesh">The mesh to perform ray casting against.</param>
        /// <param name="samplesArray">An array of points from which rays will be cast.</param>
        /// <param name="directionsArray">An array of direction vectors along which rays will be cast from the corresponding sample points.</param>
        /// <param name="useGPU">A boolean flag indicating whether to use GPU acceleration for the computation.</param>
        /// <returns>An array of floats representing the occlusion values for each sample point.</returns>
        public static float[] RayCastOcclusions(FastMesh mesh, Point3d[] samplesArray, Vector3d[] directionsArray, bool useGPU)
        {
            float[] occlusions = new float[samplesArray.Length];
            NativeMethods.RayCast(mesh.NativeMeshPointer, samplesArray, samplesArray.Length, directionsArray, directionsArray.Length, useGPU, occlusions, null);

            return occlusions;
        }

        /// <summary>
        /// Performs ray casting on a mesh to find intersection points for a set of sample points and directions, optionally utilizing GPU acceleration.
        /// Given a mesh, an array of sample points, and an array of direction vectors, this method computes the intersection points for each sample point along its direction vector.
        /// An intersection point is the point at which a ray, cast from a sample point along a direction vector, first intersects with the mesh.
        /// </summary>
        /// <param name="mesh">The mesh to perform ray casting against.</param>
        /// <param name="samplesArray">An array of points from which rays will be cast.</param>
        /// <param name="directionsArray">An array of direction vectors along which rays will be cast from the corresponding sample points.</param>
        /// <param name="useGPU">A boolean flag indicating whether to use GPU acceleration for the computation.</param>
        /// <returns>An array of Point3d representing the intersection points for each ray cast.</returns>
        public static Point3d[] RayCastIntersections(FastMesh mesh, Point3d[] samplesArray, Vector3d[] directionsArray, bool useGPU)
        {
            Point3d[] intersectionsPoints = new Point3d[samplesArray.Length * directionsArray.Length];
            NativeMethods.RayCast(mesh.NativeMeshPointer, samplesArray, samplesArray.Length, directionsArray, directionsArray.Length, useGPU, null, intersectionsPoints);

            return intersectionsPoints;
        }
    }
}

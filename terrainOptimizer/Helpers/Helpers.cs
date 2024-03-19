using Rhino.Geometry;


namespace MeshAPI
{
    internal class Helpers
    {
        /// <summary>
        /// Converts a Curve object into a flat array of float coordinates. The method attempts to convert the input curve to a polyline.
        /// If the curve is already a polyline, it directly extracts its vertices. Otherwise, it approximates the curve as a polyline
        /// based on specified tolerances and parameters, then extracts the vertices. Each vertex's X, Y, and Z coordinates are
        /// stored consecutively in the resulting array.
        /// </summary>
        /// <param name="inputCurve">The Curve object to be converted into coordinates.</param>
        /// <returns>A float array where every three consecutive elements represent the X, Y, and Z coordinates of a point on the polyline approximation of the input curve.</returns>
        public static float[] GetCurveCoordinates(Curve inputCurve)
        {
            Polyline inputPolyline;

            if (inputCurve.IsPolyline())
                inputCurve.TryGetPolyline(out inputPolyline);
            else
                inputPolyline = inputCurve.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.1, 0.01, 0.5, true).ToPolyline();

            float[] coordinates = new float[inputPolyline.Count * 3];
            for (int i = 0; i < inputPolyline.Count; i++)
            {
                int j = i * 3;
                coordinates[j] = (float)inputPolyline[i].X;
                coordinates[j + 1] = (float)inputPolyline[i].Y;
                coordinates[j + 2] = (float)inputPolyline[i].Z;
            }

            return coordinates;
        }
    }
}

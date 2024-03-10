using Rhino.Geometry;


namespace MeshAPI
{
    internal class Helpers
    {
        public static float[] GetCurveCoordinates(Curve inputCurve)
        {
            Polyline inputPolyline;

            if (inputCurve.IsPolyline())
                inputCurve.TryGetPolyline(out inputPolyline);
            else
                inputPolyline = inputCurve.ToPolyline(-1, -1, 0.1, 0.1, 6, 0.001, 0.001, 0.5, true).ToPolyline();

            if (inputCurve.IsClosed)
                inputPolyline.RemoveAt(inputPolyline.Count - 1);

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

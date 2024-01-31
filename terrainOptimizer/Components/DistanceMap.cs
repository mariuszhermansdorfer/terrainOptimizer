using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class DistanceMap : GH_Component
    {
        public DistanceMap()
          : base("DistanceMap", "DistanceMap",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("existing", "existing", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("proposed", "proposed", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("resolution", "resolution", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("maxDistance", "maxDistance", "", GH_ParamAccess.item, 5);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("difference", "difference", "", GH_ParamAccess.item);
        }

        private Mesh _result;

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Mesh existing = new Mesh();
            Mesh proposed = new Mesh();
            double resolution = 0;
            double maxDistance = 0;

            DA.GetData(0, ref existing);
            DA.GetData(1, ref proposed);
            DA.GetData(2, ref resolution);
            DA.GetData(3, ref maxDistance);


            var faces = proposed.Faces.ToIntArray(true);
            var vertices = proposed.Vertices.ToFloatArray();

            var faces1 = existing.Faces.ToIntArray(true);
            var vertices1 = existing.Vertices.ToFloatArray();

            System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

            var meshProposed = MeshApi.CreateMesh(faces, faces.Length, vertices, vertices.Length);
            var meshExisting = MeshApi.CreateMesh(faces1, faces1.Length, vertices1, vertices1.Length);
            sw.Restart();
            var pMR = MeshApi.Distance(meshProposed, meshExisting, (float)resolution);
            sw.Stop();

            int[] resultFaces = new int[pMR.FacesLength];
            Marshal.Copy(pMR.Faces, resultFaces, 0, pMR.FacesLength);

            float[] resultVertices = new float[pMR.VerticesLength];
            Marshal.Copy(pMR.Vertices, resultVertices, 0, pMR.VerticesLength);

            float[] resultValues = new float[pMR.VertexValuesLength];
            Marshal.Copy(pMR.VertexValues, resultValues, 0, pMR.VertexValuesLength);


            _result = new Mesh();
            for (int i = 0; i < pMR.FacesLength; i += 3)
                _result.Faces.AddFace(resultFaces[i], resultFaces[i + 1], resultFaces[i + 2]);

            for (int i = 0; i < pMR.VerticesLength; i += 3)
                _result.Vertices.Add(resultVertices[i], resultVertices[i + 1], resultVertices[i + 2]);

            for (int i = 0; i < pMR.VertexValuesLength; i++)
                _result.VertexColors.Add(FloatToColor(resultValues[i], (float)maxDistance));

            Rhino.RhinoApp.WriteLine($"Cut: {Math.Round(pMR.Cut, 2)} m3 | Fill: {Math.Round(pMR.Fill, 2)} m3 | Balance: {Math.Round(pMR.Cut + pMR.Fill, 2)} m3");
            _result.UnifyNormals();
            _result.Translate(new Vector3d(0, 0, 0.1));

           // DA.SetData(0, _result);
        }

        public override void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            if (_result == null)
                return;

            args.Display.DrawMeshFalseColors(_result);
        }

        static Color FloatToColor(float value, float max)
        {
            Color red = Color.Red;
            Color green = Color.Green;
            Color white = Color.White;

            if (value < 0)
            {
                if (value == float.MinValue)
                    return Color.FromArgb(60, 60, 60);
                // Map to red shades
                value = Math.Min(1, Math.Abs(value / max)); // Normalize value between 0-1
                return ColorLerp(white, red, value);
            }
            else if (value > 0)
            {
                // Map to green shades
                value = Math.Min(1, value / max); // Normalize value between 0-1
                return ColorLerp(white, green, value);
            }
            else
            {
                return white;
            }
        }

        static Color ColorLerp(Color color1, Color color2, float value)
        {
            float r = (color2.R - color1.R) * value + color1.R;
            float g = (color2.G - color1.G) * value + color1.G;
            float b = (color2.B - color1.B) * value + color1.B;

            return Color.FromArgb(
                (int)Math.Round(r),
                (int)Math.Round(g),
                (int)Math.Round(b));
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("5F3171E8-B984-4921-A9A1-8CD57A74CEBF"); }
        }
    }
}
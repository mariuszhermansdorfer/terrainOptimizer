using System;
using System.Drawing;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Graphs;
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
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("distance", "distance", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {

            Mesh existing = new Mesh();
            Mesh proposed = new Mesh();
            double resolution = 0;

            DA.GetData(0, ref existing);
            DA.GetData(1, ref proposed);
            DA.GetData(2, ref resolution);


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


            var result = new Mesh();
            for (int i = 0; i < pMR.FacesLength; i += 3)
                result.Faces.AddFace(resultFaces[i], resultFaces[i + 1], resultFaces[i + 2]);

            for (int i = 0; i < pMR.VerticesLength; i += 3)
                result.Vertices.Add(resultVertices[i], resultVertices[i + 1], resultVertices[i + 2]);

            for (int i = 0; i < pMR.VertexValuesLength; i++)
                result.VertexColors.Add(FloatToColor(resultValues[i]));

            Rhino.RhinoApp.WriteLine($"Cut: {pMR.Cut} m3 | Fill: {pMR.Fill} m3 | Time: {sw.ElapsedMilliseconds} ms");

            DA.SetData(0, result);
        }

        static Color FloatToColor(float value)
        {
            Color red = Color.Red;
            Color green = Color.Green;
            Color white = Color.White;

            float m = 4;

            if (value < 0)
            {
                // Map to red shades
                value = Math.Min(1, Math.Abs(value / m)); // Normalize value between 0-1
                return ColorLerp(white, red, value);
            }
            else if (value > 0)
            {
                // Map to green shades
                value = Math.Min(1, value / m); // Normalize value between 0-1
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
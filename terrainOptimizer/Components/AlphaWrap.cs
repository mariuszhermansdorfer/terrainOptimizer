using System;
using System.Threading;

using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class AlphaWrap : GH_Component
    {

        public AlphaWrap()
          : base("alphaWrap", "AW",
              "This plugin creates a \"skin\" or \"shell\" around a mesh. It's similar to stretching a flexible cover over an object to mimic its shape. This method is used to form a 3D model that represents the shape and details of the original data as closely as possible.",
              "PHD", "Subcategory")
        {
        }

        int patience = 60;
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "M", "Input mesh.", GH_ParamAccess.item);
            pManager.AddNumberParameter("alpha", "A", "Think of \"alpha\" like a rule for wrapping a gift. It sets how tightly or loosely the wrapping paper (like a flexible plastic film) can hug the gift (a collection of points). This decides what shapes, like bumps or holes, can appear on the wrapped gift. Only shapes larger than the size set by alpha are allowed. When the wrapping is done, all shapes on the gift will be smaller than the size set by alpha. So, alpha controls the final look of our wrapped gift.", GH_ParamAccess.item);
            pManager.AddNumberParameter("offset", "O", "The \"offset distance\" is like the space you leave between a sculpture and its mold. It affects how tight the mold fits the sculpture. A larger space makes a simpler and better-quality mold, but might smooth out some details. A smaller space keeps sharper details better. It's like choosing between a loosely-fitted cover that is easier to make and a tight one that captures more detail.", GH_ParamAccess.item);
            pManager.AddIntegerParameter("timeout", "T", "Amount of time (in seconds) you are willing to wait for the solution to finish before you run out of patience.", GH_ParamAccess.item, patience);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "M", "Resulting wrapped geometry.", GH_ParamAccess.item);
        }

        Mesh result;

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh mesh = null;
            double alpha = 0.0;
            double offset = 0.0;
            
            DA.GetData(0, ref mesh);
            DA.GetData(1, ref alpha);
            DA.GetData(2, ref offset);
            DA.GetData(3, ref patience);

            mesh.Faces.ConvertQuadsToTriangles();

            NativeMethods.WrapResults wrap = default;

            Thread thread = new Thread(() =>
            {
                wrap = NativeMethods.TestWrap(mesh.Vertices.ToFloatArray(), mesh.Vertices.Count * 3,
                    mesh.Faces.ToIntArray(true), mesh.Faces.Count * 3, alpha, offset);
            });

            try
            {
                thread.Start();

                if (!thread.Join(TimeSpan.FromSeconds(patience))) 
                {
                    thread.Abort();
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "AlphaWrap timed out and was aborted.");
                    return;
                }
            }
            catch (ThreadAbortException ex)
            {
                Thread.ResetAbort();
            }


            int[] faces = new int[wrap.FacesLength];
            System.Runtime.InteropServices.Marshal.Copy(wrap.Faces, faces, 0, wrap.FacesLength);

            float[] verts = new float[wrap.VerticesLength];
            System.Runtime.InteropServices.Marshal.Copy(wrap.Vertices, verts, 0, wrap.VerticesLength);

            result = new Mesh();
            for (int i = 0; i < wrap.FacesLength; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < wrap.VerticesLength; i += 3)
                result.Vertices.Add(verts[i], verts[i + 1], verts[i + 2]);

            result.RebuildNormals();

            DA.SetData(0, result);


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
            get { return new Guid("B5966C8D-BF3B-4357-BE77-6CE47236E989"); }
        }
    }
}
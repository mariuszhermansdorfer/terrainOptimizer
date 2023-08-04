﻿using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using terrainOptimizer.Helpers;

namespace terrainOptimizer
{
    public class import : GH_Component
    {
        public import()
          : base("import", "import",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("path", "path", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("edgeLength", "edgeLength", "", GH_ParamAccess.item);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string path = "";
            double edgeLength = 0;

            DA.GetData(0, ref path);
            DA.GetData(1, ref edgeLength);


            Rhino.UI.StatusBar.ShowProgressMeter(0, 100, "Import terrain data", true, true);


            IntPtr meshPtr = MeshApi.ImportMesh(path, (float)edgeLength, ReportProgress);
            var pMR = MeshApi.RetrieveMesh(meshPtr);

            Rhino.UI.StatusBar.HideProgressMeter();

            int[] facesMR = new int[pMR.FacesLength];
            Marshal.Copy(pMR.Faces, facesMR, 0, pMR.FacesLength);

            float[] vertsMR = new float[pMR.VerticesLength];
            Marshal.Copy(pMR.Vertices, vertsMR, 0, pMR.VerticesLength);


            var resultMR = new Rhino.Geometry.Mesh();
            for (int i = 0; i < pMR.FacesLength; i += 3)
                resultMR.Faces.AddFace(facesMR[i], facesMR[i + 1], facesMR[i + 2]);

            for (int i = 0; i < pMR.VerticesLength; i += 3)
                resultMR.Vertices.Add(vertsMR[i], vertsMR[i + 1], vertsMR[i + 2]);

            MeshApi.DeleteMesh(meshPtr);

            DA.SetData(0, resultMR);
            //    // Handle meshPtr...





        }

        static bool ReportProgress(float progress)
        {
            Rhino.UI.StatusBar.UpdateProgressMeter((int)(progress * 100), true);
            // Return true to continue, or false to cancel.
            return true;
        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("7abc48c2-fdc2-4aeb-8944-7de0219e40a8"); }
        }
    }
}
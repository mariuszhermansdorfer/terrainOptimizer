using System;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class Boolean : GH_Component
    {

        public Boolean()
          : base("boolean", "boolean",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddIntegerParameter("type", "type", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("base", "base", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("cutter", "cutter", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
        }
        IntPtr meshA = IntPtr.Zero;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int type = 0;
            DA.GetData(0, ref type);

            Mesh baseMesh = new Mesh();
            DA.GetData(1, ref baseMesh);

            Mesh cutter = new Mesh();
            DA.GetData(2, ref cutter);

            if (meshA == IntPtr.Zero)
                meshA = NativeMeshMethods.CreateMesh(baseMesh.Faces.ToIntArray(true), baseMesh.Faces.Count * 3, baseMesh.Vertices.ToFloatArray(), baseMesh.Vertices.Count * 3);
                        
            IntPtr meshB = NativeMeshMethods.CreateMesh(cutter.Faces.ToIntArray(true), cutter.Faces.Count * 3, cutter.Vertices.ToFloatArray(), cutter.Vertices.Count * 3);
            var p = NativeMeshMethods.BooleanMeshes(meshA, meshB, (NativeMeshMethods.BooleanOperation)type);
            

            int[] faces = new int[p.FacesLength];
            Marshal.Copy(p.Faces, faces, 0, p.FacesLength);

            float[] verts = new float[p.VerticesLength];
            Marshal.Copy(p.Vertices, verts, 0, p.VerticesLength);

            var result = new Mesh();
            for (int i = 0; i < p.FacesLength; i += 3)
                result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            for (int i = 0; i < p.VerticesLength; i += 3)
                result.Vertices.Add(verts[i], verts[i + 1], verts[i + 2]);
            //result.RebuildNormals();

            DA.SetData(0, result);
            
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
            get { return new Guid("2FB4508C-150A-4E58-939E-E18C2E2F7596"); }
        }
    }
}
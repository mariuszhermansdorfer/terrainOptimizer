using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using terrainOptimizer.Helpers;

namespace terrainOptimizer.Components
{
    public class MixMeshes : GH_Component
    {

        public MixMeshes()
          : base("MixMeshes", "MixMeshes",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddMeshParameter("base", "base", "", GH_ParamAccess.item);
            pManager.AddMeshParameter("cutter", "cutter", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("fillAngle", "fillAngle", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("cutAngle", "cutAngle", "", GH_ParamAccess.item);
            pManager.AddNumberParameter("anglePrecision", "anglePrecision", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("result", "result", "", GH_ParamAccess.item);
        }
        //IntPtr meshA = IntPtr.Zero;
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Mesh baseMesh = new Mesh();
            DA.GetData(0, ref baseMesh);

            Mesh cutter = new Mesh();
            DA.GetData(1, ref cutter);

            double fillAngle = 0;
            double cutAngle = 0;
            double anglePrecision = 0;

            DA.GetData(2, ref fillAngle);
            DA.GetData(3, ref cutAngle);
            DA.GetData(4, ref anglePrecision);

            //if (meshA == IntPtr.Zero)
            //var  meshA = MeshApi.CreateMesh(baseMesh.Faces.ToIntArray(true), baseMesh.Faces.Count * 3, baseMesh.Vertices.ToFloatArray(), baseMesh.Vertices.Count * 3);
            var meshA = MeshApi.CreateMRMesh(baseMesh);
            var meshB = MeshApi.CreateMRMesh(cutter);


            //IntPtr meshB = MeshApi.CreateMesh(cutter.Faces.ToIntArray(true), cutter.Faces.Count * 3, cutter.Vertices.ToFloatArray(), cutter.Vertices.Count * 3);
            var sw = Stopwatch.StartNew();
            var m = MeshApi.MixMeshes(meshA, meshB, (float)fillAngle, (float)cutAngle, (float)anglePrecision);
            sw.Stop();
            RhinoApp.WriteLine($"Mix meshes {sw.ElapsedMilliseconds} ms");
            //var p = MeshApi.RetrieveMesh(m);

            //MeshApi.DeleteMesh(meshB);
            ////We should also delete meshA once it's no longer needed
            //MeshApi.DeleteMesh(meshA);
            //MeshApi.DeleteMesh(meshB);

            //int[] faces = new int[p.FacesLength];
            //Marshal.Copy(p.Faces, faces, 0, p.FacesLength);

            //float[] verts = new float[p.VerticesLength];
            //Marshal.Copy(p.Vertices, verts, 0, p.VerticesLength);

            //var result = new Mesh();
            //for (int i = 0; i < p.FacesLength; i += 3)
            //    result.Faces.AddFace(faces[i], faces[i + 1], faces[i + 2]);

            //for (int i = 0; i < p.VerticesLength; i += 3)
            //    result.Vertices.Add(verts[i], verts[i + 1], verts[i + 2]);

            //result.Faces.CullDegenerateFaces();
            //result.RebuildNormals();

            //MeshApi.DeleteMesh(m);


            Mesh newMesh = new Mesh();
            newMesh = new Mesh();
            newMesh.Vertices.Count = m.VerticesLength;
            newMesh.Faces.Count = m.FacesLength;

            if (m.FacesLength > 0)
            {
                unsafe
                {
                    using (var meshAccess = newMesh.GetUnsafeLock(true))
                    {
                        Point3f* startOfVertexArray = meshAccess.VertexPoint3fArray(out int vertexArrayLength);
                        MeshFace* startOfFacesArray = meshAccess.FacesArray(out int facesArrayLength);
                        int* sourcePtr = (int*)m.Faces;
                        int* destPtr = (int*)startOfFacesArray;

                        Buffer.MemoryCopy((float*)m.Vertices, startOfVertexArray, vertexArrayLength * sizeof(Point3f), m.VerticesLength * sizeof(float) * 3);

                        for (int i = 0; i < m.FacesLength; i++)
                        {
                            *destPtr++ = *sourcePtr++;
                            *destPtr++ = *sourcePtr++;
                            *destPtr++ = *sourcePtr; // MeshFace.C & D are the same for a triangle face
                            *destPtr++ = *sourcePtr++;
                        }

                        newMesh.ReleaseUnsafeLock(meshAccess);
                    }
                }
            }
            // MeshApi.DeleteMesh(ptr);

            //Rhino.RhinoApp.WriteLine(newMesh.Faces.CullDegenerateFaces().ToString());

            DA.SetData(0, newMesh);

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
            get { return new Guid("2FB7808C-150A-4E58-939E-E18C2E2F7596"); }
        }
    }
}
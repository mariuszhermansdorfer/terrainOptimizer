using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;


namespace terrainOptimizer.Components
{
    public class GeometryFilter : GH_Component
    {
        private bool _needsUpdate = false;
        public GeometryFilter()
          : base("GeometryFilter", "GeometryFilter",
              "Description",
              "PHD", "Subcategory")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("layers", "layers", "", GH_ParamAccess.list);
            pManager.AddBooleanParameter("refresh", "refresh", "", GH_ParamAccess.item, false);
            pManager.AddBooleanParameter("includeInvisible", "includeInvisible", "", GH_ParamAccess.item, true);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddMeshParameter("mesh", "mesh", "", GH_ParamAccess.item);
        }


        private void Expire(object sender, EventArgs e)
        {
            if (_needsUpdate)
                ExpireSolution(true);
        }

        public override void RemovedFromDocument(GH_Document document)
        {
            RemoveEventHandlers();
        }

        private void SetFlag(object sender, Rhino.DocObjects.RhinoObjectEventArgs e)
        {
            _needsUpdate = true;
        }

        private void RemoveEventHandlers()
        {
            Rhino.RhinoDoc.AddRhinoObject -= SetFlag;
            Rhino.RhinoDoc.DeleteRhinoObject -= SetFlag;
            Rhino.RhinoApp.Idle -= Expire;
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            _needsUpdate = false;

            bool refresh = false;
            bool includeInvisible = false;
            List<string> layers = new List<string>();
            DA.GetDataList(0, layers);
            DA.GetData(1, ref refresh);
            DA.GetData(2, ref includeInvisible);

            RemoveEventHandlers();
            if (refresh)
            {
                Rhino.RhinoDoc.AddRhinoObject += SetFlag;
                Rhino.RhinoDoc.DeleteRhinoObject += SetFlag;
                Rhino.RhinoApp.Idle += Expire;
            }

            Mesh mesh = new Mesh();
            var layerTable = Rhino.RhinoDoc.ActiveDoc.Layers;

            foreach (var layerName in layers)
            {
                var layerIndex = Rhino.RhinoDoc.ActiveDoc.Layers.FindByFullPath(layerName, -1);
                if (layerIndex == -1)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Couldn't find layer {layerName}");
                    continue;
                }

                if (layerTable[layerIndex].IsVisible == false && includeInvisible == false)
                    continue;

                var objects = Rhino.RhinoDoc.ActiveDoc.Objects.FindByLayer(layerTable[layerIndex]);
                var doc = Rhino.RhinoDoc.ActiveDoc;
                if (objects == null)
                    continue;

                foreach (var rhinoObject in objects)
                {
                    var m = rhinoObject.GetMeshes(MeshType.Default);
                    mesh.Append(m);
                }
            }


            DA.SetData(0, mesh);
        }



        protected override System.Drawing.Bitmap Icon => null;


        public override Guid ComponentGuid => new Guid("5F3171E8-B984-3721-A9A1-5CD57A74CEBF");

    }
}
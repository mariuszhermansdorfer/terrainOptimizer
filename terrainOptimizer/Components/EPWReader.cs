using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Hosting;
using Grasshopper.Kernel;

using Newtonsoft.Json;
using Rhino.Geometry;

using HLA_Core;




namespace terrainOptimizer.Components
{
    public class EPWReader : GH_Component
    {

        public EPWReader()
          : base("EPWReader", "EPWReader",
              "Description",
              "PHD", "EPW")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("inputPath", "inputPath", "", GH_ParamAccess.item);
        }


        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

        }


        protected override void SolveInstance(IGH_DataAccess DA)
        {
            var allDataSources = HLA_Core.Microclimate.EPW_Files.RetrieveClosestEPWDataSource(new Point3d(8.62300, 26.72400, 0));
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
            get { return new Guid("2FB5808C-232A-4E18-935E-E18C2E2F7596"); }
        }
    }
}
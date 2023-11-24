using System;
using System.Diagnostics;
using Grasshopper.Kernel;
using RoboSharp;


namespace terrainOptimizer
{
    public class Robo : GH_Component
    {
        public Robo()
          : base("Robo", "Robo",
              "Description",
              "PHD", "Subcategory")
        {
        }
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("test", "test", "", GH_ParamAccess.list);
        }
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddCurveParameter("result", "result", "", GH_ParamAccess.list);
        }



        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Rhino.UI.StatusBar.ShowProgressMeter(0, 100, "Copy files from server", true, true);
            RoboCommand roboCopy = new RoboCommand();


            roboCopy.OnCopyProgressChanged += (sender, e) =>
            {
                Rhino.UI.StatusBar.UpdateProgressMeter((int)e.CurrentFileProgress, true);
            };

            roboCopy.OnCommandCompleted += (sender, e) =>
            {
                Rhino.RhinoApp.InvokeOnUiThread((Action)(() =>
                {
                    Rhino.UI.StatusBar.HideProgressMeter();

                    Eto.Forms.Application.Instance.Invoke(() =>
                    {
                        Eto.Forms.MessageBox.Show(
                            text: "All files were successfully copied from the server.",
                            type: Eto.Forms.MessageBoxType.Information,
                            buttons: Eto.Forms.MessageBoxButtons.OK,
                            caption: "Operation Complete"
                        );
                    });
                }));
            };

            roboCopy.CopyOptions.Source = "H:\\Denmark\\Copenhagen\\Sustainability\\Communications\\002_PP-presentations\\Tidligere præsentationer\\MRHE\\230516_Metaverse";
            roboCopy.CopyOptions.Destination = "C:\\Users\\mrhe\\Desktop\\destination";
            roboCopy.Start();
            
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
            get { return new Guid("7abc57c4-fdc3-4aeb-8957-4de0219e10a8"); }
        }
    }
}

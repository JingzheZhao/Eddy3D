using Eddy.Properties;
using EddyLib;
using Grasshopper.Kernel;
using System;
using System.Windows.Forms;
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class Paraview : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear,
        /// Subcategory the panel. If you use non-existing tab or panel names,
        /// new tabs/panels will automatically be created.
        /// </summary>
        public Paraview()
          : base("Paraview", "Paraview", "Paraview", "Eddy", "5 | PostProcessing")
        {
        }

        protected override void AppendAdditionalComponentMenuItems(ToolStripDropDown menu)
        {
            base.AppendAdditionalComponentMenuItems(menu);
            Menu_AppendItem(menu, "ParaView 4", Menu_DoClick, true, !paraViewVersion5);
        }

        private void Menu_DoClick(object sender, EventArgs e)
        {
            paraViewVersion5 = !paraViewVersion5;
            ExpireSolution(true);

        }
        public bool paraViewVersion5 = true;



        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            // First add our own field.
            writer.SetBoolean("ParaView", paraViewVersion5);
            // Then call the base class implementation.
            return base.Write(writer);
        }
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            // First read our own field.
            paraViewVersion5 = reader.GetBoolean("ParaView");
            // Then call the base class implementation.
            return base.Read(reader);
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Res", "Res", "Res", GH_ParamAccess.item);
            pManager.AddBooleanParameter("Run", "Run", "Run", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {

        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            int version = 0;

            // mode to select simulation environment
            if (!paraViewVersion5) { Message = "ParaView 4"; version = 4; }
            else { Message = "ParaView 5"; version = 5; }

            // read inputs
            //------------

            OFResult RES = null;
            DA.GetData(0, ref RES);

            bool run = false;
            DA.GetData("Run", ref run);

            if (!run)
            {
                return;
            }




            string paraViewPath = "\"" + EddyLib.Utilities.GetParaviewPath(version) + "\" " + "\"" + RES.WorkingDirectory + RES.Domain.BCond.windDirs[0] + "\\" + RES.Domain.BCond.windDirs[0] + @".foam" + "\"";
            EddyLib.Utilities.StartProcessCMD(paraViewPath, true, false, false);
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                Resources.Eddy_paraview;//return null;

        /// <summary>
        /// Each component must have a unique Guid to identify it.
        /// It is vital this Guid doesn't change otherwise old ghx files
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{2FEE37D4-A096-4F3C-9972-0EB3BB3A9CF1}");
    }
}

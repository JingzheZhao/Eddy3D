using Eddy.Properties;
using EddyLib;
using EddyLib.Compute;
using Grasshopper.Kernel;
using System;

// In order to load the result of this wizard, you will also need to add the output bin/ folder of
// this project to the list of loaded folder in Grasshopper. You can use the
// _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class SLURM_Runner_Component : GH_Component
    {
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.obscure; }
        }

        public SLURM_Runner_Component()
          : base("SLURM Runner", "SLURM", "Add batch running files for SLURM " + EddyVersion.toString(),
              EddyVersion.Name, "3 | Pre-Processing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Result", "Res", "Eddy Result", GH_ParamAccess.item);
            pManager.AddTextParameter("Charge Account", "CA", "Add the SLURM charge account as a string", GH_ParamAccess.item);
            pManager.AddTextParameter("Notification Email", "NE", "Add the SLURM notification email as a string", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Memory per CPU", "MCPU", "Specify RAM allocated per CPU, e.g. 10 for 10G", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Job duration", "JD", "Specify the duration of the SLURM job in hours, e.g. 10 for 10 hours", GH_ParamAccess.item);
            pManager.AddTextParameter("OpenFOAM load command", "OFLD", @"Specify the OpenFOAM load command as string, ""module load openfoam-org/8-mva2-off3dy""", GH_ParamAccess.item, @"module load openfoam-org/8-mva2-off3dy");
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
        /// <param name="DA">
        /// The DA object can be used to retrieve data from input parameters and to store data in
        /// output parameters.
        /// </param>
        protected override void SolveInstance(IGH_DataAccess DA)

        {
            string Result = "Writing out files...";
            Message = Result;

            OFResult RES = null;
            string chargeAccount = "";
            string notificationEmail = "";
            string OFloadCommand = "";
            int memPerCPU = 10;
            int durationOfJob = 10;

            DA.GetData(0, ref RES);
            DA.GetData(1, ref chargeAccount);
            DA.GetData(2, ref notificationEmail);
            DA.GetData(3, ref memPerCPU);
            DA.GetData(4, ref durationOfJob);
            DA.GetData(5, ref OFloadCommand);

            SLURM_Runner SLRM = new SLURM_Runner(RES, chargeAccount, notificationEmail, durationOfJob, memPerCPU, OFloadCommand);
            Message = SLRM.Result;
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface. Icons
        /// need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                return Resources.Eddy_misc;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. It is vital this Guid doesn't
        /// change otherwise old ghx files that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{1b8c3e35-d695-4061-a188-a5697ba02a0c}"); }
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace WindTunnel
{
    public class SimpleFoam : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SimpleFoam()
          : base("Simulation", "Simulation",
              "Simulation",
              "CFDTool", "Simulation")
        {
        }

        

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            pManager.AddIntegerParameter("iterations", "iter", "Specify the number of iterations.", GH_ParamAccess.item, 1000);
            pManager.AddIntegerParameter("writeInterval", "Write Interval", "Write Interval.", GH_ParamAccess.item, 20);
            pManager.AddIntegerParameter("keepTimeSteps", "KeepTimeSteps", "KeepTimeSteps.", GH_ParamAccess.item, 5);
            pManager.AddBooleanParameter("Run", "Run", "Run the solver.", GH_ParamAccess.item, false);
            
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
            //string filepath = @"C:\OF\";
            bool Run = false;
            string SingleCPU = @"simpleFoam >> log ";
            string MultipleCPU = @"pyFoamRunner.py --autosense-parallel simpleFoam >> log ";
            
            // workingDirectory = "";

            //public Box DomainBoundaryBox;
            //List<Brep> domain = new List<Brep>();

            //DA.GetDataList(0, domain);
            //DA.GetData(1, ref workingDirectory);

            int iter = 1000;
            int writeInterval = 20;
            int keepTimeSteps = 5;
            string workingDirectory = "";

            DA.GetData(1, ref iter);
            DA.GetData(2, ref writeInterval);
            DA.GetData(3, ref keepTimeSteps);
            DA.GetData(4, ref Run);

            OFDomainBuilder DOM = null;
            if (!DA.GetData(0, ref DOM)) { return; }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }



            if (Run == true)
            {
                
                var stlDir = Path.GetDirectoryName( DOM.workingDirectory + @"\constant\triSurface\");
                var stlFilenameBuildings = DOM.workingDirectory + @"\constant\triSurface\building.stl";
                var stlFilenameGround = DOM.workingDirectory + @"\constant\triSurface\ground.stl";

                if (!Directory.Exists(stlDir)) {
                    Directory.CreateDirectory(stlDir);
                }

                string systemDir = workingDirectory + @"\system\";
                string constantDir = workingDirectory + @"\constant\";
                string boundaryConditionsDir = workingDirectory + @"\0.org\";

                if (!Directory.Exists(systemDir))
                {
                    Directory.CreateDirectory(systemDir);
                }
                if (!Directory.Exists(constantDir))
                {
                    Directory.CreateDirectory(constantDir);
                }
                if (!Directory.Exists(boundaryConditionsDir))
                {
                    Directory.CreateDirectory(boundaryConditionsDir);
                }



                File.WriteAllText(Path.Combine(systemDir + "blockMeshDict"), StringTemplates.fvSchemes());
                File.WriteAllText(Path.Combine(systemDir + "blockMeshDict"), StringTemplates.fvSolution());
                File.WriteAllText(Path.Combine(systemDir + "blockMeshDict"), StringTemplates.fvSolution());
                File.WriteAllText(Path.Combine(systemDir + "blockMeshDict"), StringTemplates.meshQualityDict());
                
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "U"), BoundaryConditions.U());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "P"), BoundaryConditions.P());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "Omega"), BoundaryConditions.Omega());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "K"), BoundaryConditions.K());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "Epsilon"), BoundaryConditions.Epsilon());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "ABLConditions"), BoundaryConditions.ABLConditions());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "initialConditions"), BoundaryConditions.InitialConditions());


                File.WriteAllText(Path.Combine(systemDir + "controlDict"), StringTemplates.controlDict(iter, keepTimeSteps, writeInterval));

                string command = DOM.CPU > 1 ? MultipleCPU : SingleCPU;
                ProcessStartInfo psi = new ProcessStartInfo(@"C:\Users\pkastner\Documents\GitHub\WindTunnel\CallOF\bin\CallOF.exe", " -e " + command + " -f " + DOM.workingDirectory);
               
                Process p = new Process();
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();


               // OFLaunch.Run(command, StringTemplates.filePath);
            }
            //else
            //{
            //    return;
            //}

            
        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{7FF4A70C-DB4E-473C-BDC0-606CE58A979A}"); }
        }
    }
}

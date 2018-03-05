using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Grasshopper.Kernel.Types;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
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
              "Eddy", "Simulation")
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

            pManager.AddIntegerParameter("Mode", "Mode", "Robustness of the solver", GH_ParamAccess.item, 0);
            Param_Integer param = pManager[4] as Param_Integer;
            param.AddNamedValue("quick", 0);
            param.AddNamedValue("robust", 1);


            pManager.AddGenericParameter("Type", "Bcond", "", GH_ParamAccess.item);


            pManager.AddBooleanParameter("Run", "Run", "Run the solver.", GH_ParamAccess.item, false);
            
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            //pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.item);
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



           // OFBaseDomain DOM = null;
           // if (!DA.GetData(0, ref DOM)) { return; }
          

            // we can use class inheritance 

            OFBaseDomain DOM = null;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            if ((gobj.Value is OFBaseDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }


            BoundaryConditions BCInflow = null;

            GH_ObjectWrapper gobj2 = null;
            if (!DA.GetData(5, ref gobj2)) { }

            if ((gobj2.Value is BoundaryConditions))
            {
                BCInflow = (BoundaryConditions)gobj.Value;
            }
            if (BCInflow == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }

            
            string SingleCPU = @"""pyFoamPrepareCase.py . --no-mesh-create;simpleFoam""";
            string MultipleCPU = @"""renumberMesh -overwrite;pyFoamPrepareCase.py . --no-mesh-create;pyFoamDecompose.py --clear . """ + DOM.CPU +@""";pyFoamRunner.py --autosense-parallel simpleFoam""";
            
            
            int iter = 1000;
            int writeInterval = 20;
            int keepTimeSteps = 5;
            int mode = 0;
            

           

            DA.GetData(1, ref iter);
            DA.GetData(2, ref keepTimeSteps);
            DA.GetData(3, ref writeInterval);
            DA.GetData(4, ref mode); 
            
            DA.GetData(6, ref Run);

            DOM.iter = iter;
            DOM.writeInterval = writeInterval;
            DOM.keepTimeSteps = keepTimeSteps;



            if (Run == true)
            {
                
                var stlDir = Path.GetDirectoryName( DOM.workingDirectory + @"\constant\triSurface\");
                var stlFilenameBuildings = DOM.workingDirectory + @"\constant\triSurface\building.stl";
                var stlFilenameGround = DOM.workingDirectory + @"\constant\triSurface\ground.stl";

                if (!Directory.Exists(stlDir)) {
                    Directory.CreateDirectory(stlDir);
                }

                
                string constantDir = DOM.workingDirectory + @"\constant\";
                string boundaryConditionsDir = DOM.workingDirectory + @"\0.org\";

                if (!Directory.Exists(DOM.systemDirectory))
                {
                    Directory.CreateDirectory(DOM.systemDirectory);
                }
                if (!Directory.Exists(constantDir))
                {
                    Directory.CreateDirectory(constantDir);
                }
                if (!Directory.Exists(boundaryConditionsDir))
                {
                    Directory.CreateDirectory(boundaryConditionsDir);
                }

                File.WriteAllText(Path.Combine(DOM.systemDirectory + "controlDict"), StringTemplates.controlDict(iter, writeInterval, keepTimeSteps, null));

                
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "U"), BoundaryConditionTemplates.U(BCInflow));
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "p"), BoundaryConditionTemplates.P());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "omega"), BoundaryConditionTemplates.Omega());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "k"), BoundaryConditionTemplates.K());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "epsilon"), BoundaryConditionTemplates.Epsilon());
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "nut"), BoundaryConditionTemplates.Nut());

                File.WriteAllText(Path.Combine(boundaryConditionsDir + "ABLConditions"), BoundaryConditionTemplates.ABLConditions(BCInflow));
                File.WriteAllText(Path.Combine(boundaryConditionsDir + "initialConditions"), BoundaryConditionTemplates.InitialConditions());


                //Constant folder
                File.WriteAllText(Path.Combine(constantDir + "turbulenceProperties"), StringTemplates.turbulenceProperties());
                File.WriteAllText(Path.Combine(constantDir + "transportProperties"), StringTemplates.transportProperties());


                //Batch files
                File.WriteAllText(Path.Combine(DOM.workingDirectory + "run_mesh.bat"), StringTemplates.run_mesh());
                File.WriteAllText(Path.Combine(DOM.workingDirectory + "run_sim.bat"), StringTemplates.run_sim());
                File.WriteAllText(Path.Combine(DOM.workingDirectory + "run.bat"), StringTemplates.run());

                //string command = DOM.CPU > 1 ? MultipleCPU : SingleCPU;
                //ProcessStartInfo psi = new ProcessStartInfo(@"C:\Users\pkastner\Documents\GitHub\WindTunnel\VirtualWindTunnel\bin\CallOF.exe", " -e " + command + " -f " + DOM.workingDirectory);
                ////ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory + @"\CallOF.exe", " -e " + command + " -f " + DOM.workingDirectory);
               
                //Process p = new Process();
                //p.StartInfo = psi;
                //p.Start();
                //p.WaitForExit();


               // OFLaunch.Run(command, StringTemplates.filePath);
            }

            //string logFile = "";

            //using (FileStream stream = File.Open(DOM.workingDirectory + @"\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            //{
            //    using (StreamReader reader = new StreamReader(stream))
            //    {
            //        logFile = reader.ReadToEnd();
            //        //while (!reader.EndOfStream)
            //        //{

            //        //}

            //    }
            //}

            //DA.SetData(0, logFile);

            //if (logFile.Contains("End")) { AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Super!!"); }
            //// else if (logFile.Contains("End")) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Fast Super!!"); }
            //else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Nicht Super!!"); }


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

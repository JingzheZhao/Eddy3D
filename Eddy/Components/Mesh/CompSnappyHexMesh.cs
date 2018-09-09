using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Grasshopper.Kernel.Types;
using EddyLib;
using Eddy.Properties;
// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class SnappyHexMesh : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public SnappyHexMesh()
          : base("Mesh", "Mesh",
              "Mesh",
              "Eddy", "Mesh")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Dom", "Simulation Domain", GH_ParamAccess.item);
            pManager.AddIntegerParameter("AccBuilding", "AccBuilding", "Specify accuracy of building mesh.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("AccFeatures", "accFeatures", "Specify accuracy of building features (corners) mesh.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("AccRefinement", "accRefinement", "Specify accuracy of bounding box mesh.", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("AccGround", "accGround", "Specify accuracy of ground mesh.", GH_ParamAccess.item, 2);
            pManager.AddIntegerParameter("nLayer", "nLay", "Number of mesh layers.", GH_ParamAccess.item, 3);
            pManager.AddIntegerParameter("Mode", "Mode", @"Mode: 
0: No snapping, no layers
1: With Snapping, no layers
2: With Snapping, with layers", GH_ParamAccess.item, 2);
            Param_Integer param = pManager[3] as Param_Integer;
            param.AddNamedValue("No snapping, no layers", 0);
            param.AddNamedValue("With Snapping, no layers", 1);
            param.AddNamedValue("With Snapping, with layers", 2);
            pManager.AddBooleanParameter("Clean", "Clean", "Clean the mesh.", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.item);
            pManager.AddGenericParameter("Mesh", "Mesh", "Mesh", GH_ParamAccess.item);
            //pManager.AddGenericParameter("Cyl", "C", "Domain", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {


            //// EDDY LIC CHECK
            //Test.WriteHardwareId();
            //if (Test.Validate() || Test.ValidateTrial())
            //{
            //}
            //else
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Your trial period is over.");
            //    return;
            //}
            //// END EDDY LIC CHECK







            bool Run = false;


            //OFDomainBuilder DOM = null;
            //if (!DA.GetData(0, ref DOM)) { return; }
            //if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }


            // we can use class inheritance 
            OFCylDomain CylDom;
            OFBoxDomain BoxDom;
            OFBaseDomain DOM;

            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            if ((gobj.Value is OFCylDomain))
            {
                CylDom = (OFCylDomain)gobj.Value;
                DOM = (OFBaseDomain)gobj.Value;
            }
            else if ((gobj.Value is OFBoxDomain))
            {
                BoxDom = (OFBoxDomain)gobj.Value;
                DOM = (OFBaseDomain)gobj.Value;
            }

            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please provide a valid domain object"); return;
            }





            //string command = "";


            string SingleCPU = @"""surfaceFeatureExtract;snappyHexMesh -overwrite """; //-overwrite
            string MultipleCPU = @"""surfaceFeatureExtract;pyFoamDecompose.py --clear . " + DOM.CPU + @"; foamJob -parallel -screen snappyHexMesh -overwrite""";




            int accBuilding = 3;
            int accFeatures = 3;
            int accRefinement = 3;
            int accGround = 3;
            int nLayers = 3;
            int mode = 2;
            


            DA.GetData(1, ref accBuilding);
            DA.GetData(2, ref accFeatures);
            DA.GetData(3, ref accRefinement);
            DA.GetData(4, ref accGround);
            DA.GetData(5, ref nLayers);
            DA.GetData(6, ref mode);
            DA.GetData(7, ref Run);

            DOM.accBuildings = accBuilding;
            DOM.accFeatures = accFeatures;
            DOM.accRefinement = accRefinement;
            DOM.accGround = accGround;
            DOM.nLayers = nLayers;

            //needed for meshing purposes at this point in time
            DOM.iter = 1000;
            DOM.writeInterval = 10;
            DOM.keepTimeSteps = 2;

            DOM.meshingMode = mode;

            


            if (accBuilding >= 5 || accFeatures >= 5 || accRefinement >= 5 || accGround >= 5 || nLayers >= 5)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "A high number of refinment stages might significantly slow down mesh creation. Try to create a reasonable fine mesh with the Domain component.");
            }

            // Check for killed processes
            if (Utilities.DidProcessGetKilled(DOM.meshWorkingDirectory) == true)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Some processes got killed probably because to little RAM was available. Try to increase the RAM acclocated for the Docker virtual machine.");
            }

            //CLEAN UP THE OF MESS

            if (Run == true)
            {

                if (Directory.Exists(DOM.meshPolyMeshDirectory))
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(DOM.meshPolyMeshDirectory);
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }

                if (Directory.Exists(DOM.meshConstantDirectory + @"extendedFeatureEdgeMesh"))
                {
                    System.IO.DirectoryInfo di = new DirectoryInfo(DOM.meshConstantDirectory + @"extendedFeatureEdgeMesh");
                    foreach (FileInfo file in di.GetFiles())
                    {
                        file.Delete();
                    }
                    foreach (DirectoryInfo dir in di.GetDirectories())
                    {
                        dir.Delete(true);
                    }
                }

            }







            var meshStlDir = DOM.meshWorkingDirectory + @"\constant\triSurface\";
            var meshStlFilenameBuildings = DOM.meshWorkingDirectory + @"\constant\triSurface\building.stl";
            var meshStlFilenameGround = DOM.meshWorkingDirectory + @"\constant\triSurface\ground.stl";

            if (!Directory.Exists(meshStlDir))
            {
                Directory.CreateDirectory(meshStlDir);
            }


            if (!File.Exists(DOM.meshWorkingDirectory + @"\log"))
            {
                File.WriteAllText(DOM.meshWorkingDirectory + @"\log", "");
            }

            string meshSystemDir = DOM.meshWorkingDirectory + @"\system\";

            if (!Directory.Exists(meshSystemDir))
            {
                Directory.CreateDirectory(meshSystemDir);
            }

            Point3d locationInMesh = new Point3d();
            locationInMesh = DOM.locationInMesh;




            File.WriteAllText(Path.Combine(meshSystemDir + "snappyHexMeshDict"), StringTemplates.SnappyHexMeshDict(DOM));
            File.WriteAllText(Path.Combine(meshSystemDir + "surfaceFeatureExtractDict"), StringTemplates.SurfaceFeatureExtractDict());
            File.WriteAllText(Path.Combine(meshSystemDir + "fvSchemes"), StringTemplates.FvSchemesRobust1());
            File.WriteAllText(Path.Combine(meshSystemDir + "fvSolution"), StringTemplates.FvSolution(0));
            File.WriteAllText(Path.Combine(meshSystemDir + "meshQualityDict"), StringTemplates.MeshQualityDict());

            File.WriteAllText(Path.Combine(DOM.baseWorkingDirectory + "\\" + "run_checkBadMesh.bat"), StringTemplates.Run_checkBadMesh(DOM));


            //Autocalc number of CPUs
            if (DOM.autoCPUCalc == true)
            {
                DOM.CPU = Utilities.CPUAutoCalc(DOM.meshWorkingDirectory, DOM.CPU);
            }
            



            string command = DOM.CPU > 1 ? MultipleCPU : SingleCPU;


            if (Run == true)
            {





                //ProcessStartInfo psiSnappyHexMesh = new ProcessStartInfo(@"C:\Users\pkastner\Documents\GitHub\WindTunnel\VirtualWindTunnel\bin\CallOF.exe", " -e " + command + " -f " + DOM.workingDirectory);


                //psiSnappyHexMesh.UseShellExecute = false;
                //psiSnappyHexMesh.WorkingDirectory = DOM.workingDirectory;

                //Process pSnappyHexMesh = new Process();
                //pSnappyHexMesh.StartInfo = psiSnappyHexMesh;
                //pSnappyHexMesh.Start();
                //pSnappyHexMesh.WaitForExit();



                string logFile = "";

                using (FileStream stream = File.Open(DOM.meshWorkingDirectory + @"\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        logFile = reader.ReadToEnd();
                        
                        //while (!reader.EndOfStream)
                        //{

                        //}

                    }
                }

                DA.SetData(0, logFile);

                //if (logFile.Contains("End")) {      AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Super!!"); }
                // else if (logFile.Contains("End")) { AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Fast Super!!"); }
                //else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Nicht Super!!"); }




            }


            DA.SetData(1, DOM);

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
                return Resources.Eddy_snappy;
               // return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{6836B42F-FB09-48AF-BF41-A85D9D8FD913}"); }
        }
    }
}

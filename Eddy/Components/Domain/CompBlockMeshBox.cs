using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using System.Threading;
using Microsoft.VisualBasic.Devices;
using Grasshopper.Kernel.Types;
using EddyLib;


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class BlockMeshBox : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public BlockMeshBox()
          : base("DomainBox", "DomainBox",
              "DomainBox",
              "Eddy", "Domain")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Directory", "Dir", "Provide a working directory", GH_ParamAccess.item, @"C:\temp");

            pManager.AddGeometryParameter("Geometry", "Geo", "Building Geometry.", GH_ParamAccess.list);
            pManager.AddGeometryParameter("Terrain", "Terrain", "Terrain Geometry.", GH_ParamAccess.list);

            pManager.AddGenericParameter("BCond", "BCond", "BCond", GH_ParamAccess.item);

            pManager.AddNumberParameter("baseMesh", "baseMesh", "baseMesh", GH_ParamAccess.item, 5);

            //pManager.AddGenericParameter("RAM", "RAM", "RAM", GH_ParamAccess.item);
            pManager.AddIntegerParameter("CPUs", "CPUs", "Number of CPUs. Set to -1 to set the number of CPUs for the simulation automatically.", GH_ParamAccess.item, 1);

            pManager.AddBooleanParameter("Run", "Run", "Run the blockMesh component", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.item);
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            pManager.AddGenericParameter("B", "B", "Domain", GH_ParamAccess.item);

            ////Delete later
            //pManager.AddGenericParameter("D", "D", "D", GH_ParamAccess.item);
            //pManager.AddGenericParameter("E", "E", "E", GH_ParamAccess.item);

        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            bool Run = false;



            //public Box DomainBoundaryBox;
            List<GeometryBase> domain = new List<GeometryBase>();
            string baseWorkingDirectory = "";

            List<GeometryBase> terrain = new List<GeometryBase>();

            DA.GetDataList(1, domain);
            DA.GetDataList(2, terrain);

            DA.GetData(0, ref baseWorkingDirectory);



            double blockDimension = 0;
            //    double RAM = 0;
            int CPUs = 1;


            DA.GetData(3, ref blockDimension);
            //DA.GetData(4, ref RAM);
            DA.GetData(4, ref CPUs);
            DA.GetData(5, ref Run);

            BoundaryConditions BCond;
            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(2, ref gobj)) { }


            if ((gobj.Value is BoundaryConditions))
            {
                BCond = (BoundaryConditions)gobj.Value;
            }
            else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid boundary condition object"); return; }


            Mesh combinedMeshes = new Mesh();
            MeshingParameters mp = new MeshingParameters();

            Mesh terrainMeshes = new Mesh();


            if (BCond.windDir.Count > 1)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "For box-shaped domains you can only pass one wind direction per simulation setup."); return;
            }


            if (terrain != null)
            {
                foreach (GeometryBase b in terrain)
                {

                    if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        Mesh obj = (Mesh)b;
                        terrainMeshes.Append(obj);
                    }
                    else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                    {
                        Brep obj = (Brep)b;
                        var m = Mesh.CreateFromBrep(obj, mp);
                        foreach (Mesh mm in m) terrainMeshes.Append(mm);

                    }


                }
            }

       




            if (domain == null)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please reference an input geometry."); return;
            }
            else
            {
                foreach (GeometryBase b in domain)
                {

                    if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        Mesh obj = (Mesh)b;
                        combinedMeshes.Append(obj);
                    }
                    else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                    {
                        Brep obj = (Brep)b;
                        var m = Mesh.CreateFromBrep(obj, mp);
                        foreach (Mesh mm in m) combinedMeshes.Append(mm);

                    }


                }
            }

            // Those Breps are currently necessary to perform the point inclusion check for the probing components

            Brep inputBreps = new Brep();

            foreach (GeometryBase g in domain)
            {

                inputBreps.Append(Brep.TryConvertBrep(g));
            }



            if (Utilities.CheckLicence() == true)
            {

                // Check if Docker is running

                Utilities.WriteDockerInfo(baseWorkingDirectory);
                bool dockerRunning = false;
                if (Utilities.IsDockerRunning(baseWorkingDirectory))
                {
                    dockerRunning = true;
                }
                if (dockerRunning == false)
                {

                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, @"Docker is not running. Please start the application ""Docker for Windows"".");

                }



                //Fix paths

                baseWorkingDirectory = Utilities.FixDirectories(baseWorkingDirectory);


                OFBoxDomain DOMBOX = new OFBoxDomain(inputBreps, combinedMeshes, terrainMeshes, BCond, blockDimension, baseWorkingDirectory);

                if (CPUs == -1)
                {
                    DOMBOX.autoCPUCalc = true;
                }






                //DOM = OFDomainBuilder(domain, workingDirectory);

                if ((DOMBOX.xCells * blockDimension) > DOMBOX.dimX || (DOMBOX.yCells * blockDimension) > DOMBOX.dimY || (DOMBOX.zCells * blockDimension) > DOMBOX.dimZ)
                {
                    //  AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Your block dimensions need to be smaller than the domain.");
                }





                if (CPUs > Environment.ProcessorCount)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system does not have that many CPUs.");
                }

                //var totalGBRam = Convert.ToInt32((new ComputerInfo().TotalPhysicalMemory / (Math.Pow(1024, 2))) + 0.5);
                //if (RAM < 0 || RAM > totalGBRam)
                //{
                //    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system does not have that much RAM available.");
                //}






                //if (Settings.getCurrentRAM() != RAM)
                //{



                //    string newRAM = "Set-VM -StaticMemory -Name MobyLinuxVM -MemoryStartupBytes " + RAM + "GB";
                //    //var totalGBRam = 0 ;

                //    ProcessStartInfo psiNewRAM = new ProcessStartInfo(@"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe");
                //    psiNewRAM.Verb = "runas";
                //    psiNewRAM.Arguments = newRAM;

                //    Process pRAM = new Process();
                //    pRAM.StartInfo = psiNewRAM;
                //    pRAM.Start();
                //    pRAM.WaitForExit();

                //}

                //if (Settings.getCurrentCPUs(DOM) != CPUs)
                //{

                //    string newCPUs = @"Stop-VM -Name MobyLinuxVM;Set-VMProcessor MobyLinuxVM -Count '" + CPUs+ "';Start-VM -Name MobyLinuxVM";
                //    //var totalGBRam = 0 ;


                //    ProcessStartInfo psiNewCPUs = new ProcessStartInfo(@"C:\Windows\SysWOW64\WindowsPowerShell\v1.0\powershell.exe");
                //    psiNewCPUs.Verb = "runas";
                //    psiNewCPUs.Arguments = newCPUs;

                //    Process pRAM = new Process();
                //    pRAM.StartInfo = psiNewCPUs;
                //    pRAM.Start();
                //    pRAM.WaitForExit();

                //}


                //////

                var meshStlFilenameBuildings = DOMBOX.baseWorkingDirectory + @"\mesh\constant\triSurface\building.stl";
                var meshStlFilenameGround = DOMBOX.baseWorkingDirectory + @"\mesh\constant\triSurface\ground.stl";
                var meshStlFilenameGroundPerim = DOMBOX.baseWorkingDirectory + @"\mesh\constant\triSurface\ground_perim.stl";
                var meshBoundaryConditionsDirectory = DOMBOX.baseWorkingDirectory + @"\mesh\0.org\";


                if (!Directory.Exists(baseWorkingDirectory))
                {
                    Directory.CreateDirectory(baseWorkingDirectory);
                }


                if (!Directory.Exists(DOMBOX.meshStlDirectory))
                {
                    Directory.CreateDirectory(DOMBOX.meshStlDirectory);
                }


                STLExport.ExportBinary(meshStlFilenameBuildings, combinedMeshes);

                if (terrain != null)
                {
                    STLExport.ExportBinary(meshStlFilenameGround, DOMBOX.newBoxGround);
                }
                else
                {
                    STLExport.ExportBinary(meshStlFilenameGround, DOMBOX.newBoxGround);
                    STLExport.ExportBinary(meshStlFilenameGroundPerim, DOMBOX.newBoxGroundPerim);
                }



                if (!Directory.Exists(DOMBOX.meshSystemDirectory))
                {
                    Directory.CreateDirectory(DOMBOX.meshSystemDirectory);
                }
                if (!Directory.Exists(DOMBOX.meshConstantDirectory))
                {
                    Directory.CreateDirectory(DOMBOX.meshConstantDirectory);
                }
                if (!Directory.Exists(meshBoundaryConditionsDirectory))
                {
                    Directory.CreateDirectory(meshBoundaryConditionsDirectory);
                }


                File.WriteAllText(DOMBOX.meshSystemDirectory + @"\blockMeshDict", StringTemplates.BlockMeshDict(DOMBOX));
                File.WriteAllText(DOMBOX.baseWorkingDirectory + @"\mesh\case.foam", "");
                File.WriteAllText(DOMBOX.meshSystemDirectory + @"\controlDict", StringTemplates.ControlDict(DOMBOX, null, 0));

                if (!File.Exists(baseWorkingDirectory + @"\mesh\log"))
                {
                    File.WriteAllText(baseWorkingDirectory + @"\mesh\log", "");
                }



                //export RAD for DAYSIM
                if (!Directory.Exists(DOMBOX.baseWorkingDirectory + @"Rad\"))
                {
                    Directory.CreateDirectory(DOMBOX.baseWorkingDirectory + @"Rad\");
                }
                string radMat = @"
void plastic Generic_20
0
0
5 0.2 0.2 0.2 0 0 
";
                Mesh daysimMesh = new Mesh();
                daysimMesh.Append(combinedMeshes);
                // Todo: add ground plane to the above mesh

                File.WriteAllText(DOMBOX.baseWorkingDirectory + @"Rad\materials.rad", radMat);
                RadianceFiles.MeshProc(daysimMesh, DOMBOX.baseWorkingDirectory + @"Rad\scene.rad", "Generic_20");




                if (Run == true)
                {
                    /*
                    ProcessStartInfo psi = new ProcessStartInfo(Utilities.hardcodedAssemblyDir+@"\CallOF.exe", " -e " + command + " -f " + DOM.workingDirectory);
                    Process p = new Process();
                    p.StartInfo = psi;
                    p.Start();
                    p.WaitForExit();
                    //Thread.Sleep(500);
                    */

                    string logFile = "";

                    using (FileStream stream = File.Open(baseWorkingDirectory + @"\mesh\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Super!!");

                    // OFLaunch.Run(command, StringTemplates.filePath);
                }
                //else
                //{
                //    return;
                //}

                DA.SetData(1, DOMBOX);
                //if (mode == 0)
                //{
                DA.SetData(2, DOMBOX.newBoxDomain);

                ////Delete later
                //DA.SetData(3, DOM.pl);
                //DA.SetData(4, DOM.plGround);
                ////DA.SetData(5, DOM.);
                ////Delete later


                //}
                //else
                //{
                //    DA.SetData(2, DOM.newCylindricalDomain);
                //}

            }
            else
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Licence expired.");
            }


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
                return Properties.Resources.Eddy_domBox;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{0AD4BDF7-33AC-492D-ABF0-622A5488C8E2}"); }
        }
    }

}

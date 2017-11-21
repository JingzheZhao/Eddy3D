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


// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace WindTunnel
{
    public class BlockMesh : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public BlockMesh()
          : base("Domain", "Domain",
              "Domain",
              "CFDTool", "Domain")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBrepParameter("Geometry", "Geo", "Building Geometry. Add the volume for the virtual wind tunnel", GH_ParamAccess.list);
            pManager.AddTextParameter("Directory", "Dir", "Provide a working directory", GH_ParamAccess.item);


            pManager.AddIntegerParameter("Mode", "Mode", "Domain generation mode", GH_ParamAccess.item, 0);

            Param_Integer param = pManager[2] as Param_Integer;

            param.AddNamedValue("Box", 0);
            param.AddNamedValue("Cyl", 1);

            pManager.AddIntegerParameter("baseMesh", "baseMesh", "baseMesh", GH_ParamAccess.item, 20);

            pManager.AddGenericParameter("RAM", "RAM", "RAM", GH_ParamAccess.item);
            pManager.AddIntegerParameter("CPUs", "CPUs", "CPUs", GH_ParamAccess.item, 1);

            pManager.AddBooleanParameter("Run", "Run", "Run the blockMesh component", GH_ParamAccess.item, false);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.item);
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            pManager.AddGenericParameter("Cyl", "C", "Domain", GH_ParamAccess.item);
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
            string command = @"blockMesh";
            string workingDirectory = "";

            //public Box DomainBoundaryBox;
            List<Brep> domain = new List<Brep>();

            DA.GetDataList(0, domain);
            DA.GetData(1, ref workingDirectory);

            int mode = 0;
            int baseMesh = 0;
            double RAM = 0;
            int CPUs = 1;

            DA.GetData(2, ref mode);
            DA.GetData(3, ref baseMesh);

            DA.GetData(4, ref RAM);
            DA.GetData(5, ref CPUs);
            DA.GetData(6, ref Run);

            OFDomainBuilder DOM = new OFDomainBuilder(domain, workingDirectory, baseMesh);
            //DOM = OFDomainBuilder(domain, workingDirectory);



            if (CPUs == -1 || CPUs > Environment.ProcessorCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system does not have that many CPUs.");
            }

            var totalGBRam = Convert.ToInt32((new ComputerInfo().TotalPhysicalMemory / (Math.Pow(1024, 2))) + 0.5);
            if (RAM < 0 || RAM > totalGBRam)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system does not have that much RAM available.");
            }




            if (Run == true)
            {
                        
                
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



                MeshingParameters mp = new MeshingParameters();
                MeshingParameters mps = MeshingParameters.Smooth;
                List<Mesh> meshObjects = new List<Mesh>();

                foreach (Brep b in domain)
                {
                    meshObjects.AddRange(Mesh.CreateFromBrep(b, mp));

                }

                var stlDir = Path.GetDirectoryName(workingDirectory + @"\constant\triSurface\");
                var stlFilenameBuildings = workingDirectory + @"\constant\triSurface\building.stl";
                var stlFilenameGround = workingDirectory + @"\constant\triSurface\ground.stl";

                if (!Directory.Exists(stlDir))
                {
                    Directory.CreateDirectory(stlDir);
                }


                STLExport.ExportBinary(stlFilenameBuildings, meshObjects);

                if (mode == 0)
                {
                    STLExport.ExportBinary(stlFilenameGround, DOM.newBoxGround);
                }
                else
                {
                    STLExport.ExportBinary(stlFilenameGround, DOM.newCylGround);
                }


                string systemDir = workingDirectory + @"\system\";

                if (!Directory.Exists(systemDir))
                {
                    Directory.CreateDirectory(systemDir);
                }



                if (mode == 1)
                {
                    File.WriteAllText(Path.Combine(systemDir + "blockMeshDictCirc"), StringTemplates.circularDomainM4(DOM));
                    ProcessStartInfo m4 = new ProcessStartInfo(@"C:\Users\pkastner\Documents\GitHub\WindTunnel\CallOF\bin\CallOF.exe", " -e \"m4 ./system/blockMeshDictCirc > ./system/blockMeshDict\" -f " + DOM.workingDirectory);
                    //File.Delete(workingDirectory+@"\system\blockMeshDictCirc");
                    Process m4p = new Process();
                    m4p.StartInfo = m4;
                    m4p.Start();
                    m4p.WaitForExit();
                }
                else
                {
                    File.WriteAllText(Path.Combine(systemDir + "blockMeshDict"), StringTemplates.blockMeshDict(DOM));
                }


                
                ProcessStartInfo psi = new ProcessStartInfo(@"C:\Users\pkastner\Documents\GitHub\WindTunnel\CallOF\bin\CallOF.exe", " -e " + command + " -f " + DOM.workingDirectory);
                Process p = new Process();
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();
                //Thread.Sleep(500);


                string logFile = "";

                using (FileStream stream = File.Open(workingDirectory + @"\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
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

            DA.SetData(1, DOM);
            if (mode == 0)
            {
                DA.SetData(2, DOM.newBoxDomain);
            }
            else
            {
                DA.SetData(2, DOM.newCylindricalDomain);
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
            get { return new Guid("{0AD4BDF7-33AC-492D-ABF0-622A5488C8E2}"); }
        }
    }

}

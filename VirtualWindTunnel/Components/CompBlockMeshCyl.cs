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



// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
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
          : base("DomainCyl", "DomainCyl",
              "DomainCyl",
              "Eddy", "Domain")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGeometryParameter("Geometry", "Geo", "Building Geometry. Add the volume for the virtual wind tunnel", GH_ParamAccess.list);
            pManager.AddTextParameter("Directory", "Dir", "Provide a working directory", GH_ParamAccess.item);

            //pManager.AddGenericParameter("windDir", "windDir", "windDir", GH_ParamAccess.item);
            pManager.AddGenericParameter("BCond", "BCond", "BCond", GH_ParamAccess.list);
            
            //pManager.AddIntegerParameter("Mode", "Mode", "Domain generation mode", GH_ParamAccess.item, 0);

            //Param_Integer param = pManager[2] as Param_Integer;

            //param.AddNamedValue("Box", 0);
            //param.AddNamedValue("Cyl", 1);

            //pManager.AddIntegerParameter("baseMesh", "baseMesh", "baseMesh", GH_ParamAccess.item, 20);

            //pManager.AddIntegerParameter("divisionsX", "divisionsX", "divisionsX", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("divisionsOuterCirc", "divisionsOuterCirc", "divisionsOuterCirc", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("gradingPerim", "gradingPerim", "gradingPerim", GH_ParamAccess.item, 1);
            pManager.AddNumberParameter("scaleInnerR", "scaleInnerR", "scaleInnerR", GH_ParamAccess.item, 0.5);

            pManager.AddIntegerParameter("RAM", "RAM", "RAM", GH_ParamAccess.item, 2000);
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
            List<GeometryBase> domain = new List<GeometryBase>();
            DA.GetDataList(0, domain);


            DA.GetData(1, ref workingDirectory);

            List<BoundaryConditions> BCond = new List<BoundaryConditions>();

            
            List<GH_ObjectWrapper> gobj = new List<GH_ObjectWrapper>();
            if (!DA.GetDataList(2, gobj)) { }

            foreach (var obj in gobj)
            {
                if ((obj.Value is BoundaryConditions))
                {
                    BCond.Add((BoundaryConditions)obj.Value);
                }
                else  { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid boundary condition object"); return; }
            }

            //int mode = 0;
            //int baseMesh = 0;
            int RAM = 0;
            int CPUs = 1;
            double windDir = 0;
            int divisionsOuterCirc = 1;
            double gradingPerim = 1;
            double scaleFactorInnerRect = 0.5;
            

            //DA.GetData(2, ref mode);
            //DA.GetData(2, ref windDir);
            //DA.GetDataList(2, BCond);
            //DA.GetData(3, ref baseMesh);
            //DA.GetData(3, ref divisionsX);
            DA.GetData(3, ref divisionsOuterCirc);
            DA.GetData(4, ref gradingPerim);
            DA.GetData(5, ref scaleFactorInnerRect);

            DA.GetData(6, ref RAM);
            DA.GetData(7, ref CPUs);
            DA.GetData(8, ref Run);


            
            

            //OFDomainBuilder DOM = new OFDomainBuilder(domain, workingDirectory, baseMesh);
            //DOM = OFDomainBuilder(domain, workingDirectory);

            Mesh allTogether = new Mesh();
            MeshingParameters mp = new MeshingParameters();

            //Error handling

            if (CPUs == -1 || CPUs > Environment.ProcessorCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system does not have that many CPUs.");
            }

            var totalGBRam = Convert.ToInt32((new ComputerInfo().TotalPhysicalMemory / (Math.Pow(1024, 2))) + 0.5);
            if (RAM < 0 || RAM > totalGBRam)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system does not have that much RAM available.");
            }
            //if (divisionsZ <= 0)
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Divisions must be greater than 0.");

            //}
            //if (scaleFactorInnerRect <= 0 || scaleFactorInnerRect >= Math.Sqrt(0.5) )
            //{ 
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Scale factor must be greater than 0 and less than 0.7.");

            //}


            foreach (GeometryBase b in domain)
            {

                if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                {
                    Mesh obj = (Mesh)b;
                    allTogether.Append(obj);
                }
                else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                {
                    Brep obj = (Brep)b;
                    var m = Mesh.CreateFromBrep(obj, mp);
                    foreach (Mesh mm in m) allTogether.Append(mm);

                }


            }

            if ( !workingDirectory.EndsWith(@"\"))
            {
                workingDirectory = workingDirectory + @"\";
            }
            

            OFCylDomain DOMCYL = new OFCylDomain(allTogether, BCond, divisionsOuterCirc, gradingPerim, windDir, workingDirectory, CPUs, scaleFactorInnerRect);

            DOMCYL.gradingPerim = gradingPerim;
           
            



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




                var stlDir = Path.GetDirectoryName(workingDirectory + @"\constant\triSurface\");
                var stlFilenameBuildings = workingDirectory + @"\constant\triSurface\building.stl";
                var stlFilenameGround = workingDirectory + @"\constant\triSurface\ground.stl";
                var stlFilenameGroundPerim = workingDirectory + @"\constant\triSurface\ground_perim.stl";

                if (!Directory.Exists(stlDir))
                {
                    Directory.CreateDirectory(stlDir);
                }


                STLExport.ExportBinary(stlFilenameBuildings, allTogether);        
                STLExport.ExportBinary(stlFilenameGround, DOMCYL.DomainMeshGround);
                STLExport.ExportBinary(stlFilenameGroundPerim, DOMCYL.DomainMeshGroundPerim);
                


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


                File.WriteAllText(Path.Combine(systemDir + "blockMeshDict"), DOMCYL.stringyfyDomain2());
                File.WriteAllText(Path.Combine(workingDirectory + "log"), "");
                File.WriteAllText(Path.Combine(workingDirectory + "case.foam"), "");
                File.WriteAllText(Path.Combine(systemDir + "controlDict"), StringTemplates.controlDict(10000, 5, 5, null));



                  /*              
                //ProcessStartInfo psi = new ProcessStartInfo(Utilities.AssemblyDirectory +@"\CallOF.exe", " -e " + command + " -f " + DOMCYL.workingDirectory);
                ProcessStartInfo psi = new ProcessStartInfo(Utilities.hardcodedAssemblyDir+@"\CallOF.exe", " -e " + command + " -f " + DOMCYL.workingDirectory);
                Process p = new Process();
                p.StartInfo = psi;
                p.Start();
                p.WaitForExit();
                //Thread.Sleep(500);
                */


                string logFile = "";

                using (FileStream stream = File.Open(workingDirectory + @"\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        logFile = reader.ReadToEnd();
               
                    }
                }

                DA.SetData(0, logFile);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Super!!");

                      }
        

           DA.SetData(1, DOMCYL);           
           DA.SetData(2, DOMCYL.DomainMesh);
            



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
                return Properties.Resources.ED_cylDomain;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{DDB7971A-EBAD-4A6F-8BFB-E77FE24F73BD}"); }
        }
    }

}

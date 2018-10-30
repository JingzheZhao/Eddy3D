using EddyLib;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Microsoft.VisualBasic.Devices;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;


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
            pManager.AddBrepParameter("Geometry", "Geo", "Building Geometry.", GH_ParamAccess.list);
            pManager.AddTextParameter("Directory", "Dir", "Provide a working directory", GH_ParamAccess.item, @"C:\Users\%USERNAME%\Eddy\");


            pManager.AddGenericParameter("BCond", "BCond", "BCond", GH_ParamAccess.item);


            pManager.AddIntegerParameter("Radial divisions", "RadDiv", "Radial divisions", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Concentric grading", "ConcGrad", "Concentric grading", GH_ParamAccess.item, 1);
            pManager.AddIntegerParameter("Concentric divisions", "ConcDiv", "Concentric Divisions", GH_ParamAccess.item, 1);

            pManager.AddNumberParameter("Size of inner rectangle", "InnerR", "Size of inner rectangle", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Size of outer radius", "OuterR", "Size of outer radius", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Height", "Height", "Height", GH_ParamAccess.item, 0);


            pManager.AddIntegerParameter("CPUs", "CPUs", "Number of CPUs. Set to -1 to set the number of CPUs for the simulation automatically.", GH_ParamAccess.item, 1);


        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.item);
            pManager.AddGenericParameter("Domain", "Dom", "Domain", GH_ParamAccess.item);
            pManager.AddGenericParameter("Cylinder", "Cyl", "Cylinder", GH_ParamAccess.item);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {





            //string filepath = @"C:\OF\";
            //bool Run = false;
            //  string command = @"blockMesh";
            string baseWorkingDirectory = "";

            //public Box DomainBoundaryBox;
            List<GeometryBase> _domain = new List<GeometryBase>();
            DA.GetDataList(0, _domain);


            List<GeometryBase> domain = new List<GeometryBase>();
            foreach (var g in _domain)
            {
                if (g != null)
                {
                    domain.Add(g);
                }
            }



            


            DA.GetData(1, ref baseWorkingDirectory);

            BoundaryConditions BCond = null;
            DA.GetData(2, ref BCond);
            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(2, ref gobj)) { }
            if ((gobj.Value is BoundaryConditions))
            {
                BCond = ((BoundaryConditions)gobj.Value);
            }
            else { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid boundary condition object"); return; }





            int CPUs = 1;
            int divisionsOuterCirc = 1;
            int gradingPerim = 1;
            int divPerim = 1;
            double sizeInnerRect = 0;
            double sizeOuterCirc = 0;
            double sizeHeight = 0;



            DA.GetData(3, ref divisionsOuterCirc);
            DA.GetData(4, ref gradingPerim);
            DA.GetData(5, ref divPerim);
            DA.GetData(6, ref sizeInnerRect);
            DA.GetData(7, ref sizeOuterCirc);
            DA.GetData(8, ref sizeHeight);


            //DA.GetData(6, ref RAM);
            DA.GetData(9, ref CPUs);
            //DA.GetData(10, ref Run);



            Mesh combinedMeshes = new Mesh();
            MeshingParameters mp = new MeshingParameters();

            //Error handling

            // //c//c//temp/abc/mesh/

            //string windowsVersion = Utilities.GetOSInfo();
            bool isWindows7 = Utilities.IsWindows7;
            string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);


            //if (windowsVersion == "Windows 7" || windowsVersion == "Windows 8")
            //{
            //    if (!baseWorkingDirectory.StartsWith(userFolder, StringComparison.InvariantCultureIgnoreCase))
            //    {
            //        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "For Windows 7 and 8, the working directory must be in the user folder because of contrainst with a deprecated Docker version.."); return;
            //    }
            //}

            if (isWindows7)
            {
                if (!baseWorkingDirectory.StartsWith(userFolder, StringComparison.InvariantCultureIgnoreCase))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "For Windows 7 and 8, the working directory must be in the user folder because of constraint with a deprecated Docker version.."); return;
                }
            }


            if (CPUs > Environment.ProcessorCount)
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system does not have that many CPUs.");
            }



            var totalGBRam = Convert.ToInt32((new ComputerInfo().TotalPhysicalMemory / (Math.Pow(1024, 2))) + 0.5);
            //if (RAM < 0 || RAM > totalGBRam)
            //{
            //    AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Your system does not have that much RAM available.");
            //}
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
                    Mesh obj = new Mesh();
                    obj = (Mesh)b;
                    combinedMeshes.Append(obj);
                }
                else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                {
                    Brep obj = (Brep)b;
                    var m = Mesh.CreateFromBrep(obj, mp);
                    foreach (Mesh mm in m)
                    {
                        combinedMeshes.Append(mm);
                    }
                }


            }



            // Those Breps are currently necessary to perform the point inclusion check for the probing components

            Brep inputBreps = new Brep();

            foreach (GeometryBase g in domain)
            {

                inputBreps.Append(Brep.TryConvertBrep(g));
            }



            //Fix paths

            baseWorkingDirectory = Utilities.FixDirectories(baseWorkingDirectory);
            //string OFbaseWorkingDirectory = Utilities.ReformatWorkingDir(baseWorkingDirectory);





            if (Utilities.CheckLicence() == true)
            {


                OFCylDomain DOMCYL = new OFCylDomain(inputBreps, combinedMeshes, BCond, divisionsOuterCirc, gradingPerim, divPerim, CPUs, sizeInnerRect, sizeOuterCirc, sizeHeight, baseWorkingDirectory)
                {
                    CPUs = CPUs
                };
                if (CPUs == -1)
                {
                    DOMCYL.autoCPUCalc = true;
                }

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




                var meshStlFilenameBuildings = DOMCYL.baseWorkingDirectory + @"\mesh\constant\triSurface\building.stl";
                var meshStlFilenameGround = DOMCYL.baseWorkingDirectory + @"\mesh\constant\triSurface\ground.stl";
                var meshStlFilenameGroundPerim = DOMCYL.baseWorkingDirectory + @"\mesh\constant\triSurface\ground_perim.stl";
                var meshBoundaryConditionsDirectory = DOMCYL.baseWorkingDirectory + @"\mesh\0.org\";


                if (!Directory.Exists(DOMCYL.meshStlDirectory))
                {
                    Directory.CreateDirectory(DOMCYL.meshStlDirectory);
                }


                STLExport.ExportBinary(meshStlFilenameBuildings, combinedMeshes);
                STLExport.ExportBinary(meshStlFilenameGround, DOMCYL.DomainMeshGround);
                STLExport.ExportBinary(meshStlFilenameGroundPerim, DOMCYL.DomainMeshGroundPerim);


                if (!Directory.Exists(DOMCYL.meshSystemDirectory))
                {
                    Directory.CreateDirectory(DOMCYL.meshSystemDirectory);
                }
                if (!Directory.Exists(DOMCYL.meshConstantDirectory))
                {
                    Directory.CreateDirectory(DOMCYL.meshConstantDirectory);
                }
                if (!Directory.Exists(meshBoundaryConditionsDirectory))
                {
                    Directory.CreateDirectory(meshBoundaryConditionsDirectory);
                }


                File.WriteAllText(DOMCYL.meshSystemDirectory + @"\blockMeshDict", DOMCYL.StringyfyDomain2());
                File.WriteAllText(DOMCYL.baseWorkingDirectory + @"\mesh\case.foam", "");
                File.WriteAllText(DOMCYL.meshSystemDirectory + @"\controlDict", StringTemplates.ControlDict(DOMCYL, null, 0));

                if (!File.Exists(baseWorkingDirectory + @"\mesh\log"))
                {
                    File.WriteAllText(baseWorkingDirectory + @"\mesh\log", "");
                }





                //export RAD for DAYSIM
                if (!Directory.Exists(DOMCYL.baseWorkingDirectory + @"Rad\"))
                {
                    Directory.CreateDirectory(DOMCYL.baseWorkingDirectory + @"Rad\");
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

                File.WriteAllText(DOMCYL.baseWorkingDirectory + @"Rad\materials.rad", radMat);
                RadianceFiles.MeshProc(daysimMesh, DOMCYL.baseWorkingDirectory + @"Rad\scene.rad", "Generic_20");




                //if (Run == true)
                //{

                //}

                DA.SetData(1, DOMCYL);
                DA.SetData(2, DOMCYL.DomainMesh);


                string logFile = "";

                using (FileStream stream = File.Open(baseWorkingDirectory + @"\mesh\log", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        logFile = reader.ReadToEnd();

                    }
                }

                DA.SetData(0, logFile);
                AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Super!!");


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
        protected override System.Drawing.Bitmap Icon =>
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                Properties.Resources.Eddy_domCyl;

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid => new Guid("{DDB7971A-EBAD-4A6F-8BFB-E77FE24F73BD}");
    }

}

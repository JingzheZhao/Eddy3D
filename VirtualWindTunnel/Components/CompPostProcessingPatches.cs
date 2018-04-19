using System;
using System.Collections.Generic;
using System.IO;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System.Text;
using System.Linq;
using Grasshopper.Kernel.Parameters;
using System.Diagnostics;
using Grasshopper.Kernel.Types;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace Eddy
{
    public class PostProcessing : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public PostProcessing()
          : base("Patches", "Patches","postProcessing","Eddy","postProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);
            pManager.AddGenericParameter("topo", "topologies", "topologies", GH_ParamAccess.list);
           
            //pManager.AddTextParameter("topoName", "topoName", "topoName", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Mode", "Mode", "Mode", GH_ParamAccess.item, 0);

            Param_Integer param = pManager[2] as Param_Integer;
            param.AddNamedValue("cp_Patches", 0);
            param.AddNamedValue("V_dot_Patches", 1);



        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Out", "Out", "Out", GH_ParamAccess.list);
        }



        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {

            OFBaseDomain DOM = null;



            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            if ((gobj.Value is OFBaseDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }



            string topoName = "patch";
            int mode = 0;
            List<GeometryBase> topo = new List<GeometryBase>();
            List<Point3d> points = new List<Point3d>();

            DA.GetDataList(1, topo);
            //DA.GetDataList(2, points);
            //DA.GetData(2, ref topoName);


            DA.GetData(2, ref mode);




            if (mode == 0)
            {

                List<Mesh> allTopo = new List<Mesh>();
                MeshingParameters mp = new MeshingParameters();

                foreach (GeometryBase b in topo)
                {

                    if (b.ObjectType == Rhino.DocObjects.ObjectType.Mesh)
                    {
                        Mesh obj = (Mesh)b;
                        allTopo.Add(obj);
                    }
                    else if (b.ObjectType == Rhino.DocObjects.ObjectType.Brep || b.ObjectType == Rhino.DocObjects.ObjectType.Extrusion || b.ObjectType == Rhino.DocObjects.ObjectType.Surface)
                    {
                        Brep obj = (Brep)b;
                        var m = Mesh.CreateFromBrep(obj, mp);
                        foreach (Mesh mm in m) allTopo.Add(mm);

                    }
                    
                    
                    for (int i = 0; i < allTopo.Count; i++)
                    {
                        string filePath = DOM.workingDirectory + @"constant\triSurface\" + topoName + i + ".stl";
                        STLExport.ExportBinary(filePath, allTopo[i]);
                    }


                }

                File.WriteAllText(Path.Combine(DOM.systemDirectory + "controlDict"), StringTemplates.controlDict(10000, 5, 20, allTopo));
                File.WriteAllText(Path.Combine(DOM.systemDirectory + "topoSetDict"), StringTemplates.topoSetDict(allTopo));

                string postProcessDirectory = DOM.workingDirectory + @"\postProcessing\";
                int counterTopo = 0;



                if (!Directory.Exists(postProcessDirectory))
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "There is no postProcessing directory.");
                }
                else
                {
                    counterTopo = topo.Count();

                    string[] dir = Directory.GetDirectories(postProcessDirectory);
                    string basePath = postProcessDirectory + @"\swakExpression_";

                    int counterIter = Directory.GetDirectories(dir[0]).Length;


                    string[] fullDir = new string[counterTopo];
                    string[] filePathResults = new string[counterTopo];
                    string[] dirLastIter = Directory.GetDirectories(dir[0]);


                    var item = dirLastIter[dirLastIter.Length - 1];

                    string lastIter = Path.GetFileName(item);



                    // Build filepath
                    for (int i = 0; i < counterTopo; i++)
                    {
                        fullDir[i] = basePath + topoName + i;
                    }


                    // Build get fileName

                    //filePathResults = Directory.GetFiles(fullDir[0]);
                    //string fileName = new String(Path.GetFileName(filePathResults[0]).Where(c => Char.IsLetter(c) | Char.IsPunctuation(c)).ToArray());;
                    //string fileName2 = Regex.Replace(filePathResults[0], @"[^A-Z]+", String.Empty);


                    List<String> fullPath = new List<String>();

                    for (int i = 0; i < counterTopo; i++)
                    {
                        fullPath.Add(fullDir[i] + @"\" + lastIter + @"\" + topoName + i);
                    }


                    double[] cpValues = new double[counterTopo];
                    var csv = new System.Text.StringBuilder();


                    for (int i = 0; i < counterTopo; i++)
                    {
                        var lastLine = File.ReadLines(fullPath[i]).Last();
                        cpValues[i] = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[1]);
                        //if (cpValues[i] > 100)
                        //{
                        //    cpValues[i] = cpValues[i - 1];
                        //};
                        var firstColumn = i.ToString();
                        var secondColumn = cpValues[i].ToString();
                        var newLine = string.Format("{0},{1}", firstColumn, secondColumn);
                        csv.AppendLine(newLine);
                    }

                    File.WriteAllText(postProcessDirectory + topoName +@".csv", csv.ToString());

                    DA.SetDataList(0,cpValues);
                }

               


            }

            if (mode == 1)
            {

                return;



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
            get { return new Guid("{16D5EB0A-F81C-4655-BB6C-5892C5448E0B}"); }
        }
    }
}

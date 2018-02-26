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
using System.Text.RegularExpressions;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace WindTunnel
{
    public class PostProcessingProbes : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public PostProcessingProbes()
          : base("Probes", "Probes","postProcessing","Eddy", "postProcessing")
        {
        }



        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Domain", "Domain", "Domain", GH_ParamAccess.item);            
            pManager.AddPointParameter("points", "points", "points", GH_ParamAccess.list);
            pManager.AddTextParameter("pointName", "pointName", "pointName", GH_ParamAccess.item);
            pManager.AddIntegerParameter("Mode", "Mode", "Mode", GH_ParamAccess.item, 0);

            Param_Integer param = pManager[3] as Param_Integer;
            param.AddNamedValue("cp_Probes", 0);
            param.AddNamedValue("U_Probes", 1);



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

            OFBaseDomain DOM = null;



            GH_ObjectWrapper gobj = null;
            if (!DA.GetData(0, ref gobj)) { }

            if ((gobj.Value is OFBaseDomain))
            {
                DOM = (OFBaseDomain)gobj.Value;
            }
            if (DOM == null) { AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Please pass a valid domain object"); return; }



            string pointName = "";
            int mode = 0;
            List<GeometryBase> topo = new List<GeometryBase>();
            List<Point3d> points = new List<Point3d>();

            
            DA.GetDataList(2, points);
            DA.GetData(3, ref pointName);
            DA.GetData(4, ref mode);






            if (mode == 0) // cp
            {



                string postProcessDirectory = DOM.workingDirectory + @"\postProcessing\";
                int counterPoints = 0;


                counterPoints = points.Count();


                string[] dir = Directory.GetDirectories(postProcessDirectory);

                //replace this with input
                string basePath = postProcessDirectory + @"\cpSamples";

                int counterIter = Directory.GetDirectories(basePath).Length;





                string[] filePathResults = new string[counterPoints];
                string[] directoriesBasePath = Directory.GetDirectories(dir[0]);




                var latestTimedirectoriesBasePath = directoriesBasePath[directoriesBasePath.Length - 1];




                string latestTime = Path.GetFileName(latestTimedirectoriesBasePath);





                // Build get fileName

                //filePathResults = Directory.GetFiles(fullDir[0]);
                //string fileName = new String(Path.GetFileName(filePathResults[0]).Where(c => Char.IsLetter(c) | Char.IsPunctuation(c)).ToArray());;
                //string fileName2 = Regex.Replace(filePathResults[0], @"[^A-Z]+", String.Empty);


                string fullPath = "";


                fullPath = basePath + @"\" + latestTime + @"\" + "total(p)_coeff";
                

                double[] cpValues = new double[counterPoints];
                var csv = new System.Text.StringBuilder();

                double[] values = new double[counterPoints];

                var lastLine = File.ReadLines(fullPath).Last();

                //double[] values = lastLine.Split(' ').Select(n => Convert.ToDouble(n)).ToArray();
                for (int i = 1; i < counterPoints; i++)
                {
                    values[i] = double.Parse(lastLine.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i]); //this workes
                                                                                                                           //var firstColumn = i.ToString();
                                                                                                                           //var secondColumn = cpValues[i].ToString();
                                                                                                                           //var newLine = string.Format("{0},{1}", firstColumn, secondColumn);
                                                                                                                           //csv.AppendLine(newLine);
                }
                
                File.WriteAllText(postProcessDirectory + pointName + ".csv", csv.ToString());





            }

            if (mode == 1) // U
            {



                string postProcessDirectory = DOM.workingDirectory + @"\postProcessing\";
                int counterPoints = 0;


                counterPoints = points.Count();


                string[] dir = Directory.GetDirectories(postProcessDirectory);

                //replace this with input
                string basePath = postProcessDirectory + @"\cpSamples";

                int counterIter = Directory.GetDirectories(basePath).Length;





                string[] filePathResults = new string[counterPoints];
                string[] directoriesBasePath = Directory.GetDirectories(dir[0]);




                var latestTimedirectoriesBasePath = directoriesBasePath[directoriesBasePath.Length - 1];




                string latestTime = Path.GetFileName(latestTimedirectoriesBasePath);





                // Build get fileName

                //filePathResults = Directory.GetFiles(fullDir[0]);
                //string fileName = new String(Path.GetFileName(filePathResults[0]).Where(c => Char.IsLetter(c) | Char.IsPunctuation(c)).ToArray());;
                //string fileName2 = Regex.Replace(filePathResults[0], @"[^A-Z]+", String.Empty);


                string fullPath = "";


                fullPath = basePath + @"\" + latestTime + @"\" + "U";




                var csv = new System.Text.StringBuilder();


                List<Vector3d> values = new List<Vector3d>();

                var lastLine = File.ReadLines(fullPath).Last();
                string replacedString = System.Text.RegularExpressions.Regex.Replace(lastLine, "[()]", "", RegexOptions.Compiled); 



                //double[] values = lastLine.Split(' ').Select(n => Convert.ToDouble(n)).ToArray();
                for (int i = 1; i < counterPoints; i += 3)
                {
                    values.Add(new Vector3d(double.Parse(replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i]), double.Parse(replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 1]), double.Parse(replacedString.Split(" ".ToCharArray(), StringSplitOptions.RemoveEmptyEntries)[i + 2])));
                }
                
                File.WriteAllText(postProcessDirectory + pointName+".csv", csv.ToString());







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
            get { return new Guid("{D39A60E1-7086-4C6F-BFF1-492D84910227}"); }
        }
    }
}

using EddyLib;
using EddyLib.Radiation;
using Grasshopper.Kernel;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.IO;

namespace Eddy.Components.Radiation
{
    public class SkyTemperature_Component : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the SkyTemperature_Component class.
        /// </summary>
        public SkyTemperature_Component()
          : base("Sky temperature", "SkyT", "Sky temperature " + EddyVersion.toString(), EddyVersion.Name, "X | Radiation")
        { }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddTextParameter("Weather", "W", "Weather filepath", GH_ParamAccess.item, DefaultDirectoriesAndPaths.DefaultWeather);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddNumberParameter("Weather", "W", "Weather filepath", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            string weatherPath = "";
            DA.GetData(0, ref weatherPath);



            if (!File.Exists(weatherPath))
            {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Weather file could not be found");
                return;
            }
            Weather weather = new Weather(weatherPath);
            var sky = new SkyTemperatureModel(weather.DewPointTemp, weather.DryBulbTemp, weather.TotalSkyCover, weather.RelativeHumidity, true, SkyTemperatureModel.CalculationType.DefaultClarkAllen);
            var SkyTemp = sky.Temp;
            DA.SetDataList(0, SkyTemp);


        }

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("e6291958-a327-4d45-860d-437e80083405"); }
        }
    }
}
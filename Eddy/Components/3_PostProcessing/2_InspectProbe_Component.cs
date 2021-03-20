using Eddy.Properties;
using EddyLib;
using EddyLib.Radiation;
using EddyLib.UI;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Attributes;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Eddy.Components.Radiation
{
    public class InspectProbe_Component : GH_Component
    {


        public string ProbeInspectorMode = "Sensor";

        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            writer.SetString("type", ProbeInspectorMode);
            return base.Write(writer);
        }

        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            ProbeInspectorMode = reader.GetString("type");
            //dd.Text = Type;
            return base.Read(reader);
        }


        public override void CreateAttributes()
        {
            m_attributes = new CustomAttributes(this);
        }

        public class CustomAttributes : GH_ComponentAttributes
        {


            public CustomAttributes(InspectProbe_Component owner) : base(owner) { }

            #region Custom layout logic
            private RectangleF isSensor { get; set; }
            private RectangleF isHour { get; set; }


            protected override void Layout()
            {
                base.Layout();

                //We'll extend the basic layout by adding three regions to the bottom of this component,
                isSensor = new RectangleF(Bounds.X, Bounds.Bottom, Bounds.Width, 20);
                isHour = new RectangleF(Bounds.X, Bounds.Bottom + 20, Bounds.Width, 20);

                Bounds = new RectangleF(Bounds.X, Bounds.Y, Bounds.Width, Bounds.Height + 40);
            }
            #endregion

            #region Custom Mouse handling


            public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
            {
                if (e.Button == System.Windows.Forms.MouseButtons.Left)
                {
                    InspectProbe_Component comp = Owner as InspectProbe_Component;

                    if (isSensor.Contains(e.CanvasLocation))
                    {
                        if (comp.ProbeInspectorMode == "Sensor") return GH_ObjectResponse.Handled;
                        comp.RecordUndoEvent("Sensor");
                        comp.ProbeInspectorMode = "Sensor";
                        comp.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }

                    if (isHour.Contains(e.CanvasLocation))
                    {
                        if (comp.ProbeInspectorMode == "Hour") return GH_ObjectResponse.Handled;
                        comp.RecordUndoEvent("Hour");
                        comp.ProbeInspectorMode = "Hour";
                        comp.ExpireSolution(true);
                        return GH_ObjectResponse.Handled;
                    }


                }
                return base.RespondToMouseDown(sender, e);
            }
            #endregion

            #region Custom Render logic
            protected override void Render(GH_Canvas canvas, System.Drawing.Graphics graphics, GH_CanvasChannel channel)
            {
                switch (channel)
                {
                    case GH_CanvasChannel.Objects:
                        //We need to draw everything outselves.
                        // base.RenderComponentCapsule(canvas, graphics, true, false, false, true, true, true);
                        base.RenderComponentCapsule(canvas, graphics, true, true, false, true, true, true);


                        InspectProbe_Component comp = Owner as InspectProbe_Component;

                        GH_Capsule buttonSensor = GH_Capsule.CreateTextCapsule(isSensor, isSensor, comp.ProbeInspectorMode == "Sensor" ? GH_Palette.Grey : GH_Palette.White, "Sensor", 2, 0);
                        buttonSensor.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                        buttonSensor.Dispose();

                        GH_Capsule buttonHour = GH_Capsule.CreateTextCapsule(isHour, isHour, comp.ProbeInspectorMode == "Hour" ? GH_Palette.Grey : GH_Palette.White, "Hour", 2, 0);
                        buttonHour.Render(graphics, this.Selected, Owner.Locked, Owner.Hidden);
                        buttonHour.Dispose();


                        break;
                    default:
                        base.Render(canvas, graphics, channel);
                        break;
                }
            }
            #endregion
        }
















        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.secondary; }
        }


        /// <summary>
        /// Initializes a new instance of the ThermalSystem_Component class.
        /// </summary>
        public InspectProbe_Component()
          : base("InspectSensor", "InSen", "Inspect sensor " + EddyVersion.toString(), EddyVersion.Name, "3 | PostProcessing")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddGenericParameter("Sensor", "Sen", "Simulation Sensor", GH_ParamAccess.list);

            pManager.AddIntegerParameter("Metric", "Met", "Metric", GH_ParamAccess.item, 0);
            var types = Enum.GetNames(typeof(RProbeMetric));
            Param_Integer param = pManager[1] as Param_Integer;
            for (int i = 0; i < types.Length; i++)
            {
                param.AddNamedValue(types[i], i);
            }
            pManager.AddIntegerParameter("Index", "i", "Hour or sensor index", GH_ParamAccess.item, 0);

        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddPointParameter("Point", "Pt", "Point", GH_ParamAccess.list);
            pManager.AddMeshParameter("Mesh", "M", "Mesh", GH_ParamAccess.list);
            pManager.AddNumberParameter("Data", "Data", "Data", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            int _metricI = 0;
            if (!DA.GetData(1, ref _metricI)) return;
            RProbeMetric metric = (RProbeMetric)_metricI;

            int h = 0;
            if (!DA.GetData(2, ref h)) return;



            List<IGH_Goo> gooProbeList = new List<IGH_Goo>();
            if (!DA.GetDataList(0, gooProbeList)) { }
            List<RProbe> rprobeList = new List<RProbe>();
            List<WProbe> wprobeList = new List<WProbe>();

            foreach (var gooProbe in gooProbeList)
            {
                if (gooProbe != null)
                {
                    RProbe rprobe = null;
                    WProbe wprobe = null;
                    if (gooProbe.CastTo<RProbe>(out rprobe)) { rprobeList.Add(rprobe); }
                    else if (gooProbe.CastTo<WProbe>(out wprobe)) { wprobeList.Add(wprobe); }
                    else
                    {
                        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Probe provided cannot be cast into the correct format. Are you sure you are passing the correct input?");
                        return;
                    }
                }

            }

            List<Point3d> points = new List<Point3d>();
            List<Mesh> meshes = new List<Mesh>();
            List<float> data = new List<float>();

            if (ProbeInspectorMode == "Hour")

                foreach (var rprobe in rprobeList)
                {
                    points.Add(rprobe.Point.Value);
                    if (rprobe.PreviewGeo != null) meshes.Add(rprobe.PreviewGeo.Value);
                    else meshes.Add(null);


                    if (metric == RProbeMetric.MRT)
                    {
                        data.Add(rprobe.LongWave_MRT[h] + rprobe.SolarGain_dMRT[h]);
                    }
                    else if (metric == RProbeMetric.UTCI)
                    {
                        data.Add(rprobe.UTCI[h]);
                    }
                    else if (metric == RProbeMetric.DirRad)
                    {
                        data.Add(rprobe.DirRad[h]);
                    }
                    else if (metric == RProbeMetric.TotalRad)
                    {
                        data.Add(rprobe.TotalRad[h]);
                    }
                    else if (metric == RProbeMetric.dMRT)
                    {
                        data.Add(rprobe.SolarGain_dMRT[h]);
                    }
                    else if (metric == RProbeMetric.lwMRT)
                    {
                        data.Add(rprobe.LongWave_MRT[h]);
                    }
                    else if (metric == RProbeMetric.WindSpeed)
                    {
                        data.Add(rprobe.WindSpeed[h]);
                    }
                }

            else
            {
                var rprobe = rprobeList[h];

                points.Add(rprobe.Point.Value);
                if (rprobe.PreviewGeo != null) meshes.Add(rprobe.PreviewGeo.Value);
                else meshes.Add(null);


                if (metric == RProbeMetric.MRT)
                {
                    data.AddRange( rprobe.LongWave_MRT.Zip(rprobe.SolarGain_dMRT, (a, b) => a + b) );
                }
                else if (metric == RProbeMetric.UTCI)
                {
                    data.AddRange(rprobe.UTCI);
                }
                else if (metric == RProbeMetric.DirRad)
                {
                    data.AddRange(rprobe.DirRad);
                }
                else if (metric == RProbeMetric.TotalRad)
                {
                    data.AddRange(rprobe.TotalRad);
                }
                else if (metric == RProbeMetric.dMRT)
                {
                    data.AddRange(rprobe.SolarGain_dMRT);
                }
                else if (metric == RProbeMetric.lwMRT)
                {
                    data.AddRange(rprobe.LongWave_MRT);
                }
                else if (metric == RProbeMetric.WindSpeed)
                {
                    data.AddRange(rprobe.WindSpeed);
                }

            }

            DA.SetDataList(0, points);
            DA.SetDataList(1, meshes);

            DA.SetDataList(2, data);
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
                return Resources.Eddy_Sensor_Inspect;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("{FF0BDCD5-D39E-4943-83E5-25C8B0D24034}"); }
        }

    }
}
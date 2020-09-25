using Eddy.Properties;
using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eddy.Components.Indoor.Params
{
    public class GH_IndoorBC_Inlet :  GH_Goo<IndoorBCs.Inlet>, IGH_PreviewData
    {
        #region drawing methods
        public BoundingBox ClippingBox
        {
            get { return this.Value.Geometry.GetBoundingBox(true); }
        }
        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            //No meshes are drawn.   
        }
        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Value == null) { return; }

            if (Value.Velocity != null && Value.Centroid!= null)
            {
                args.Pipeline.DrawArrow(new Line(Value.Centroid,Value.Velocity),args.Color);
            }

           
        }
        #endregion



        public GH_IndoorBC_Inlet()
        {
            this.Value = new IndoorBCs.Inlet();
        }

        // constructor with initial value
        public GH_IndoorBC_Inlet(IndoorBCs.Inlet indoorGeoValue)
        {
            this.Value = indoorGeoValue;
        }

        // copy constructor
        public GH_IndoorBC_Inlet(GH_IndoorBC_Inlet indoorGeoSource)
        {
            this.Value = indoorGeoSource.Value;
        }

        // duplication method
        public override IGH_Goo Duplicate()
        {
            return new GH_IndoorBC_Inlet(this);
        }

        // return validity of IndoorGeometry
        public override bool IsValid
        {
            // TODO...
            get { return true; }
        }

        // return a string with the name of this Type.
        public override string TypeName
        {
            get { return "IndoorBC Inlet"; }
        }

        // return a string describing what this Type is about.
        public override string TypeDescription
        {
            get { return "IndoorBC Inlet"; }
        }

        // return a string representation of the state (value) of this instance.
        public override string ToString()
        {
            // Camera name?
            string IndoorGeometry_name = "";
            if (Value != null && Value.Name.Length > 0)
            {
                IndoorGeometry_name += ":[" + Value.Name + "]";
            }

            // to string
            return "IndoorBC Inlet" + IndoorGeometry_name;
        }

        // serlialize
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            var json = JsonConvert.SerializeObject(this.Value, Formatting.None);
            writer.SetString("IndoorBC_Inlet", json);
 
            return true;
        }

        // deserialize
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            var json = reader.GetString("IndoorBC_Inlet");
            if (!String.IsNullOrWhiteSpace(json)) {
                this.Value = JsonConvert.DeserializeObject<IndoorBCs.Inlet>(json);  
            }
  
            return true;
        }






    }

    public class Param_IndoorBC_Inlet : GH_PersistentParam<GH_IndoorBC_Inlet>
    {
        // we need to supply a constructor without arguments that calls the base class constructor.
        public Param_IndoorBC_Inlet() :
          base(new GH_InstanceDescription("Inlet", "Inlet",
              "Inlet (Indoor Boundary Condition)", EddyVersion.Name, "7 | Indoor"))
        { }

        // unique id
        public override Guid ComponentGuid
        {
            get { return new Guid("{EB07F9FB-78B1-43B6-8B9C-718E37DF1D9E}"); }
        }

        // hidden parameter
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.obscure; }
        }

        // icon
        protected override Bitmap Icon
        {
            get
            {
                return Resources.Eddy_Indoor_Inlet;
            }
        }


        //We do not allow users to pick inlets, 
        //therefore the following 4 methods disable all this ui.
        protected override GH_GetterResult Prompt_Plural(ref List<GH_IndoorBC_Inlet> values)
        {
            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Singular(ref GH_IndoorBC_Inlet value)
        {
            return GH_GetterResult.cancel;
        }
        protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomSingleValueItem()
        {
            System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
            item.Text = "Not available";
            item.Visible = false;
            return item;
        }
        protected override System.Windows.Forms.ToolStripMenuItem Menu_CustomMultiValueItem()
        {
            System.Windows.Forms.ToolStripMenuItem item = new System.Windows.Forms.ToolStripMenuItem();
            item.Text = "Not available";
            item.Visible = false;
            return item;
        }


        #region preview methods
        public BoundingBox ClippingBox
        {
            get
            {
                return Preview_ComputeClippingBox();
            }
        }
        public void DrawViewportMeshes(IGH_PreviewArgs args)
        {
            //Meshes aren't drawn.
        }
        public void DrawViewportWires(IGH_PreviewArgs args)
        {
            //Use a standard method to draw gunk, you don't have to specifically implement this.
            Preview_DrawWires(args);
        }

        private bool m_hidden = false;
        public bool Hidden
        {
            get { return m_hidden; }
            set { m_hidden = value; }
        }
        public bool IsPreviewCapable
        {
            get { return true; }
        }
        #endregion

    }
}

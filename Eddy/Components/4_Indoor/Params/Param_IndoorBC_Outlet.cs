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

namespace Eddy.Components.Indoor.Params
{
    public class IndoorOutletGoo : GH_Goo<IndoorBC.Outlet>, IGH_PreviewData
    {
        #region constructors

        public IndoorOutletGoo()
        {
            this.Value = new IndoorBC.Outlet();
        }

        // constructor with initial value
        public IndoorOutletGoo(IndoorBC.Outlet indoorGeoValue)
        {
            this.Value = indoorGeoValue;
        }

        // copy constructor
        public IndoorOutletGoo(IndoorOutletGoo indoorGeoSource)
        {
            this.Value = indoorGeoSource.Value;
        }

        public override IGH_Goo Duplicate()
        {
            return new IndoorOutletGoo(Value == null ? new IndoorBC.Outlet() : Value.Duplicate());
        }

        #endregion constructors

        #region properties

        // return a string with the name of this Type.
        public override string TypeName
        {
            get { return "IndoorBC Outlet"; }
        }

        // return a string describing what this Type is about.
        public override string TypeDescription
        {
            get { return "IndoorBC Outlet"; }
        }

        // return a string representation of the state (value) of this instance.
        public override string ToString()
        {
            string s = "";
            if (Value != null)
            {
                // s = "Velocity [m/s]: " + Value.Velocity.Length;  //JsonConvert.SerializeObject(this.Value, Formatting.Indented);
                s = "Pressure outlet ";//
            }

            // to string
            return "[Outlet] " + s;
        }

        // serlialize
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            var json = JsonConvert.SerializeObject(this.Value, Formatting.None);
            writer.SetString("IndoorBC_Outlet", json);

            return true;
        }

        // deserialize
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            var json = reader.GetString("IndoorBC_Outlet");
            if (!String.IsNullOrWhiteSpace(json))
            {
                this.Value = JsonConvert.DeserializeObject<IndoorBC.Outlet>(json);
            }

            return true;
        }

        public override bool IsValid
        {
            get
            {
                if (Value == null) { return false; }
                if (Value.Geometry == null || !Value.Geometry.IsValid) { return false; }
                return true;
            }
        }

        public override string IsValidWhyNot
        {
            get
            {
                if (Value == null) { return "No internal instance"; }
                if (Value.Geometry == null || !Value.Geometry.IsValid) { return "No valid geometry mesh in internal instance"; }
                return string.Empty;
            }
        }

        #endregion properties

        #region drawing methods

        public BoundingBox ClippingBox
        {
            get { return this.Value.Geometry.GetBoundingBox(true); }
        }

        public void DrawViewportMeshes(GH_PreviewMeshArgs args)
        {
            if (Value == null) { return; }
            if (Value.Geometry != null)
            {
                args.Pipeline.DrawMeshShaded(Value.Geometry, args.Material);
            }
        }

        public void DrawViewportWires(GH_PreviewWireArgs args)
        {
            if (Value == null) { return; }

            // if (Value.Velocity != null && Value.Centroid != null)
            if (Value.Centroid != null)
            {
                //if (Value.Velocity.Length > 0)
                // {
                //args.Pipeline.DrawArrow(new Line(Value.Centroid, Value.Velocity * 10), args.Color);
                args.Pipeline.DrawArrow(new Line(Value.Centroid, Value.Normals[0] * 10), args.Color);

                //  }
            }
        }

        #endregion drawing methods
    }

    public class Param_IndoorBC_Outlet : GH_PersistentParam<IndoorOutletGoo>, IGH_PreviewObject
    {
        // we need to supply a constructor without arguments that calls the base class constructor.
        public Param_IndoorBC_Outlet() :
          base(new GH_InstanceDescription("Outlet", "Outlet",
              "Outlet (Indoor Boundary Condition)", EddyVersion.Name, "9 | Indoor"))
        { }

        // unique id
        public override Guid ComponentGuid
        {
            get { return new Guid("{B19EFC90-ECE2-4170-983D-AF339FCCBE80}"); }
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
                return Resources.Eddy_Indoor_Outlet_copy;
            }
        }

        //We do not allow users to pick Outlets,
        //therefore the following 4 methods disable all this ui.
        protected override GH_GetterResult Prompt_Plural(ref List<IndoorOutletGoo> values)
        {
            return GH_GetterResult.cancel;
        }

        protected override GH_GetterResult Prompt_Singular(ref IndoorOutletGoo value)
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
            Preview_DrawMeshes(args);
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

        #endregion preview methods
    }
}
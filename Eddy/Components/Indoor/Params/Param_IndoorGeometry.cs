using EddyLib;
using EddyLib.Indoor;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eddy.Components.Indoor.Params
{
    public class GH_OFIndoorGeometry : GH_Goo<OFIndoorGeometry>
    {
        public GH_OFIndoorGeometry()
        {
            this.Value = new OFIndoorGeometry();
        }

        // constructor with initial value
        public GH_OFIndoorGeometry(OFIndoorGeometry indoorGeoValue)
        {
            this.Value = indoorGeoValue;
        }

        // copy constructor
        public GH_OFIndoorGeometry(GH_OFIndoorGeometry indoorGeoSource)
        {
            this.Value = indoorGeoSource.Value;
        }

        // duplication method
        public override IGH_Goo Duplicate()
        {
            return new GH_OFIndoorGeometry(this);
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
            get { return "IndoorGeometry"; }
        }

        // return a string describing what this Type is about.
        public override string TypeDescription
        {
            get { return "IndoorGeometry"; }
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
            return "IndoorGeometry" + IndoorGeometry_name;
        }

        // serlialize
        public override bool Write(GH_IO.Serialization.GH_IWriter writer)
        {
            var json = JsonConvert.SerializeObject(this.Value, Formatting.None);
            writer.SetString("IndoorGeometry", json);

            //// serialize value as byte array
            //using (var stream = new MemoryStream())
            //{
            //    Serializer.Serialize(stream, this.Value);
            //    byte[] bytes = stream.ToArray();
            //    writer.SetByteArray("IndoorGeometry", bytes);
            //}
            return true;
        }

        // deserialize
        public override bool Read(GH_IO.Serialization.GH_IReader reader)
        {
            var json = reader.GetString("IndoorGeometry");
            if (!String.IsNullOrWhiteSpace(json)) {
                this.Value = JsonConvert.DeserializeObject<OFIndoorGeometry>(json);  
            }
 
            //// deserialize byte array to value
            //byte[] bytes = reader.GetByteArray("IndoorGeometry");
            //using (var stream = new MemoryStream(bytes))
            //{
            //    stream.Position = 0;
            //    this.Value = Serializer.Deserialize<OFIndoorGeometry>(stream);
            //}
            return true;
        }
    }

    public class Param_IndoorGeometry : GH_PersistentParam<GH_OFIndoorGeometry>
    {
        // we need to supply a constructor without arguments that calls the base class constructor.
        public Param_IndoorGeometry() :
          base(new GH_InstanceDescription("ObjectIndoor", "ObjectIndoor",
              "ObjectIndoor", EddyVersion.Name, "7 | Indoor"))
        { }

        // unique id
        public override Guid ComponentGuid
        {
            get { return new Guid("{EB07F9FB-78B1-43B6-8B9C-718E37DF1D9E}"); }
        }

        // hidden parameter
        public override GH_Exposure Exposure
        {
            get { return GH_Exposure.primary; }
        }

        // icon
        protected override Bitmap Icon
        {
            get
            {
                return null;
            }
        }

        // for persistent params, we also need the following two methods:
        protected override GH_GetterResult Prompt_Singular(ref GH_OFIndoorGeometry value)
        {
            return GH_GetterResult.cancel;
        }
        protected override GH_GetterResult Prompt_Plural(ref List<GH_OFIndoorGeometry> values)
        {
            return GH_GetterResult.cancel;
        }

    }
    }

using Eddy.Properties;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Eddy
{
    public class EddyInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "Eddy3D";
            }
        }

        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return Resources.Eddy3D;
            }
        }

        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "AIRFLOW AND MICROCLIMATE SIMULATIONS FOR RHINO AND GRASSHOPPER";
            }
        }

        public override Guid Id
        {
            get
            {
                return new Guid("{2E38BADA-FD78-4B1A-A943-DC06595AF3D5}");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "Patrick Kastner, Timur Dogan";
            }
        }

        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "www.eddy3d.com";
            }
        }
    }
}
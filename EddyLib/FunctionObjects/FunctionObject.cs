using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Geometry;

namespace EddyLib.FunctionObjects
{
    public class FunctionObject
    {
        public string FullExportString { get; set; }

        public string ID { get; set; }
        public string Name { get; set; }

        public Mesh Geometry { get; set; }

        public enum selectionMode
        {
            all,
            cellZone
        }

        public void ExportGeometry(List<FunctionObject> FO, string meshStlDir)

        {
            for (int i = 0; i < FO.Count; i++)
            {
                STLExport.ExportBinary(meshStlDir + FO[i].ID + ".stl", FO[i].Geometry);
            }
        }
    }
}
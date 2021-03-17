using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Radiation
{
    [DataContract]
    public class MRT_Simulation_Settings
    {
        public MRT_Simulation_Settings() { }

         
        [DataMember]
        public double CummulativeViewFactorCutoff { get; set; } = 0.1;

        [DataMember]
        public bool ComputeReflectionsAndDiffuseRadiation { get; set; } = true;

    }
}

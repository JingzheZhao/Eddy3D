using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    internal class ThermoPhysicalPropertiesDict : GenericDict
    {
        public ThermoPhysicalPropertiesDict
       (
       )
        {
            this.Name = "thermoPhysicalProperties";
            this.Location = DictLocation.constant;

            this.Header = GetHeader(this);

            this.FullDictString = @"FoamFile
{
    version         1912;
    format          ascii;
    class           dictionary;
    location        ""constant"";
    object          thermophysicalProperties;
}

thermoType
{
    type            heRhoThermo;
    mixture         pureMixture;
    transport       const;
    thermo          hConst;
    equationOfState perfectGas;
    specie          specie;
    energy          sensibleEnthalpy;
}

mixture
{
    specie
    {
        molWeight       28.9644;
        nMoles          1;
    }

    thermodynamics
    {
        Cp              1000;
        Hf              0;
    }

    transport
    {
        mu              1.8e-05;
        Pr              0.71;
    }
}

";
        }
    }
}
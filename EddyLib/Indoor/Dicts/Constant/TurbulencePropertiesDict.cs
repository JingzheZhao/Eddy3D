using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    internal class TurbulencePropertiesDict : GenericDict
    {
        public TurbulencePropertiesDict
       (
       )
        {
            this.DictionaryName = "turbulenceProperties";
            this.Location = DictLocation.constant;
            this.FC = FieldClass.dictionary;

            this.Header = GetHeader(this);

            this.FullDictString = @"FoamFile
{
    version         1912;
    format          ascii;
    class           dictionary;
    location        ""constant"";
    object          turbulenceProperties;
}

simulationType  RAS;

RAS
{
    RASModel        kOmegaSST;
    kOmegaSSTCoeffs
    {
        alphaK1         0.85;
        alphaK2         1.0;
        alphaOmega1     0.5;
        alphaOmega2     0.856;
        beta1           0.075;
        beta2           0.0828;
        betaStar        0.09;
        gamma1          0.555;
        gamma2          0.44;
        a1              0.31;
        b1              1.0;
        c1              10.0;
        F3              no;
    }

    turbulence      on;
    printCoeffs     on;
}";
        }
    }
}
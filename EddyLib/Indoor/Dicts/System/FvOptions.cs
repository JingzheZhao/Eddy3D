using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    public class FvOptions : GenericDict
    {

        //public List<String> InternalDict = new List<string>();

        public FvOptions(List<GenericDict> fv)
        {
            this.DictionaryName = "fvOptions";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;

            this.Header = GetHeader(this);

            //this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetddtSchemesDict(IndoorDom)));

            StringBuilder sb = new StringBuilder();

            foreach (var dic in fv) { sb.AppendLine(dic.FullDictString); }

            string[] parts = {
               this.Header, "\n",
         String.Join("\n", sb)
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }


        
    }
}



            //            this.FullDictString = @"FoamFile
            //{
            //    version         1912;
            //    format          ascii;
            //    class           dictionary;
            //    location        ""system"";
            //    object          fvSchemes;
            //}

//ddtSchemes
//{
//    default         steadyState;
//}

//gradSchemes
//{
//    default         Gauss linear;
//}

//divSchemes
//{
//    default         none;
//    div(phi,U)      bounded Gauss upwind;
//    div(phi,h)      bounded Gauss upwind;
//    div(phi,K)      bounded Gauss upwind;
//    div(((rho*nuEff)*dev2(T(grad(U))))) Gauss linear;
//    div(phi,k)      bounded Gauss upwind;
//    div(phi,omega)  bounded Gauss upwind;
//    div(phi,AoA)    bounded Gauss upwind;
//}

//laplacianSchemes
//{
//    default         Gauss linear corrected;
//    laplacian(DkEff,k) Gauss linear uncorrected;
//    laplacian(DomegaEff,omega) Gauss linear uncorrected;
//    laplacian(DAoA,AoA) Gauss linear limited 0.333;
//}

//interpolationSchemes
//{
//    default         linear;
//}

//snGradSchemes
//{
//    default         corrected;
//}

//fluxRequired
//{
//    default         no;
//}

//wallDist
//{
//    method          meshWave;
//}

//";
//        }
//    }

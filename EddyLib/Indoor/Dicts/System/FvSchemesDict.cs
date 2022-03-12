using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

namespace EddyLib.Indoor.Dicts
{
    public class FvSchemesDict : GenericDict
    {
        public List<String> InternalDict = new List<string>();

        public FvSchemesDict(IndoorDomain IndoorDom)
        {
            this.DictionaryName = "fvSchemes";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;

            this.Header = GetHeader(this);

            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetddtSchemesDict(IndoorDom)));
            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetGradSchemesDict(IndoorDom)));
            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetDivSchemesDict(IndoorDom)));
            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetLaplacianSchemesDict(IndoorDom)));
            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetInterpolationSchemesDict(IndoorDom)));
            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetSnGradSchemesDict(IndoorDom)));
            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetFluxRequiredDict(IndoorDom)));
            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetWallDistDict(IndoorDom)));

            string[] parts = {
               this.Header, "\n",
         String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }

        private static Dictionary<string, dynamic> GetddtSchemesDict(IndoorDomain IndoorDom)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            //Dictionary<string, dynamic> FunctionObjectlDict = new Dictionary<string, dynamic>();

            Dict.Add("ddtSchemes", InternalDict);

            InternalDict.Add("default", "steadyState");

            return Dict;
        }

        private static Dictionary<string, dynamic> GetGradSchemesDict(IndoorDomain IndoorDom)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("gradSchemes", InternalDict);

            InternalDict.Add("default", "Gauss linear");

            return Dict;
        }

        private static Dictionary<string, dynamic> GetDivSchemesDict(IndoorDomain IndoorDom)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("divSchemes", InternalDict);

            InternalDict.Add("default", "none");
            InternalDict.Add("div(phi,U)", "bounded Gauss upwind");
            InternalDict.Add("div(phi,h)", "bounded Gauss upwind");
            InternalDict.Add("div(phi,K)", "bounded Gauss upwind");
            InternalDict.Add("div(((rho*nuEff)*dev2(T(grad(U)))))", "Gauss linear");
            InternalDict.Add("div(phi,k)", "bounded Gauss upwind");
            InternalDict.Add("div(phi,omega)", "bounded Gauss upwind");
            InternalDict.Add("div(phi,aoa)", "bounded Gauss upwind");
            InternalDict.Add("div(phi,covid19)", "bounded Gauss upwind");

            return Dict;
        }

        private static Dictionary<string, dynamic> GetLaplacianSchemesDict(IndoorDomain IndoorDom)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("laplacianSchemes", InternalDict);

            InternalDict.Add("default", "Gauss linear corrected");
            InternalDict.Add("laplacian(DkEff,k)", "Gauss linear uncorrected");
            InternalDict.Add("laplacian(DomegaEff,omega)", "Gauss linear uncorrected");
            InternalDict.Add("laplacian(Daoa,aoa)", "Gauss linear limited 0.333");
            InternalDict.Add("laplacian(Dcovid19,covid19)", "Gauss linear limited 0.333");

            return Dict;
        }

        private static Dictionary<string, dynamic> GetInterpolationSchemesDict(IndoorDomain IndoorDom)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("interpolationSchemes", InternalDict);

            InternalDict.Add("default", "linear");

            return Dict;
        }

        private static Dictionary<string, dynamic> GetSnGradSchemesDict(IndoorDomain IndoorDom)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("snGradSchemes", InternalDict);

            InternalDict.Add("default", "corrected");

            return Dict;
        }

        private static Dictionary<string, dynamic> GetFluxRequiredDict(IndoorDomain IndoorDom)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("fluxRequired", InternalDict);

            InternalDict.Add("default", "no");

            return Dict;
        }

        private static Dictionary<string, dynamic> GetWallDistDict(IndoorDomain IndoorDom)
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("wallDist", InternalDict);

            InternalDict.Add("method", "meshWave");

            return Dict;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

#if DEBUG
[assembly: InternalsVisibleTo("UnitTest")]
#endif

namespace EddyLib.Indoor.Dicts
{
    public class FvSolutionDict : GenericDict
    {

        public List<String> InternalDict = new List<string>();

        public FvSolutionDict()
        {
            this.DictionaryName = "fvSolution";

            this.Location = DictLocation.system;
            this.FC = FieldClass.dictionary;
            this.Header = GetHeader(this);

            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetSolversDict()));

            string[] parts = {
               this.Header, "\n",
         String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }


        //private static List<Dictionary<string, dynamic>> GetSolversDict()
        //{
        //    List<Dictionary<string, dynamic>> OuterKey = new List<Dictionary<string, dynamic>>();

        //    OuterKey.Add(GetP_RghDict());

        //}

      public  static Dictionary<string, dynamic> MakeDict_p_rgh()
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("p_rgh", InternalDict);

            InternalDict.Add("solver", "PCG");
            InternalDict.Add("preconditioner", "DIC");
            InternalDict.Add("tolerance", "1e-8");
            InternalDict.Add("relTol", "0.01");

            return Dict;
        }

        static Dictionary<string, dynamic> MakeDict_U()
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("U", InternalDict);

            InternalDict.Add("solver", "PBiCGStab");
            InternalDict.Add("preconditioner", "DILU");
            InternalDict.Add("tolerance", "1e-5");
            InternalDict.Add("relTol", "0.1");

            return Dict;
        }

        static Dictionary<string, dynamic> MakeDict_h()
        {
            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();

            Dict.Add("h", InternalDict);

            InternalDict.Add("solver", "PBiCGStab");
            InternalDict.Add("preconditioner", "DILU");
            InternalDict.Add("tolerance", "1e-5");
            InternalDict.Add("relTol", "0.1");

            return Dict;
        }


        private static Dictionary<string, List<Dictionary<string, dynamic>>> GetSolversDict()
        {
            Dictionary<string, List<Dictionary<string, dynamic>>> OuterName = new Dictionary<string, List<Dictionary<string, dynamic>>>();

            List<Dictionary<string, dynamic>> OuterKey = new List<Dictionary<string, dynamic>>();

            OuterName.Add("solver", OuterKey);

            OuterKey.Add(MakeDict_p_rgh());
            OuterKey.Add(MakeDict_U());
            OuterKey.Add(MakeDict_h());

            return OuterName;
        }

    }
}

//            this.FullDictString = @"FoamFile
//{
//    version         1912;
//    format          ascii;
//    class           dictionary;
//    location        ""system"";
//    object          fvSolution;
//}

//solvers
//{
//    p_rgh
//    {
//        solver          PCG;
//        preconditioner  DIC;
//        tolerance       1e-8;
//        relTol          0.01;
//    }

//    U
//    {
//        solver          PBiCGStab;
//        preconditioner  DILU;
//        tolerance       1e-5;
//        relTol          0.1;
//    }

//    h
//    {
//        solver          PBiCGStab;
//        preconditioner  DILU;
//        tolerance       1e-5;
//        relTol          0.1;
//    }

//    k
//    {
//        solver          PBiCGStab;
//        preconditioner  DILU;
//        tolerance       1e-5;
//        relTol          0.1;
//    }

//    omega
//    {
//        solver          PBiCGStab;
//        preconditioner  DILU;
//        tolerance       1e-5;
//        relTol          0.1;
//    }

//    AoA
//    {
//        solver          GAMG;
//        tolerance       1e-7;
//        relTol          1e-8;
//        nPreSweeps      0;
//        nPostSweeps     2;
//        cacheAgglomeration true;
//        smoother        GaussSeidel;
//        agglomerator    faceAreaPair;
//        nCellsInCoarsestLevel 10;
//        mergeLevels     1;
//        maxIter         100;
//    }
//}

//SIMPLE
//{
//    residualControl
//    {
//        p_rgh           1e-4;
//        U               1e-3;
//        h               1e-3;
//    }

//    nNonOrthogonalCorrectors 0;
//    pRefCell        0;
//    pRefValue       0;
//}

//relaxationFactors
//{
//    fields
//    {
//        p_rgh           0.3;
//        AoA             0.5;
//    }

//    equations
//    {
//        U               0.3;
//        T               0.5;
//        h               0.3;
//        rho             0.3;
//        k               0.1;
//        omega           0.1;
//    }
//}

//";

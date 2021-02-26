using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EddyLib.Indoor.Dicts;

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



        private static List<Dictionary<string, dynamic>> GetSolversDict()
        {
            List<Dictionary<string, dynamic>> OuterKey = new List<Dictionary<string, dynamic>>();

            Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> InternalDict = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> InternalDict2 = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> InternalDict3 = new Dictionary<string, dynamic>();

            //Dictionary<string, dynamic> FunctionObjectlDict = new Dictionary<string, dynamic>();

            //string name = "solver";

            OuterKey.Add(Dict);
            Dict.Add("p_rgh", InternalDict);

            InternalDict.Add("solver", "PCG");
            InternalDict.Add("preconditioner", "DIC");
            InternalDict.Add("tolerance", "1e-8");
            InternalDict.Add("relTol", "0.01");

            OuterKey.Add(Dict2);
            Dict2.Add("U", InternalDict2);

            InternalDict2.Add("solver", "PBiCGStab");
            InternalDict2.Add("preconditioner", "DILU");
            InternalDict2.Add("tolerance", "1e-5");
            InternalDict2.Add("relTol", "0.1");

            OuterKey.Add(Dict3);
            Dict3.Add("h", InternalDict3);

            InternalDict3.Add("solver", "pbicgstab");
            InternalDict3.Add("preconditioner", "dilu");
            InternalDict3.Add("tolerance", "1e-5");
            InternalDict3.Add("reltol", "0.1");


            return OuterKey;
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

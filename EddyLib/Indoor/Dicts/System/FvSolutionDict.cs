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
            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetSimpleDict()));
            this.InternalDict.Add(CppMapSerializerDyn.Serialize(GetrelaxationFactorsDict()));

            string[] parts = {
               this.Header, "\n",
         String.Join("\n", this.InternalDict.ToArray())
            };

            this.FullDictString = parts.Aggregate((partialPhrase, word) => $"{partialPhrase} {word}");
        }


      public  static Dictionary<string, dynamic> MakeDict_p_rgh()
        {
            //Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            //Dict.Add("p_rgh", InternalDict);

            Dict3.Add("solver", "PCG");
            Dict3.Add("preconditioner", "DIC");
            Dict3.Add("tolerance", "1e-8");
            Dict3.Add("relTol", "0.01");

            return Dict3;
        }

        static Dictionary<string, dynamic> MakeDict_U()
        {
            //Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            //Dict.Add("U", InternalDict);

            Dict3.Add("solver", "PBiCGStab");
            Dict3.Add("preconditioner", "DILU");
            Dict3.Add("tolerance", "1e-5");
            Dict3.Add("relTol", "0.1");

            return Dict3;
        }

        static Dictionary<string, dynamic> MakeDict_h()
        {
            //Dictionary<string, dynamic> Dict = new Dictionary<string, dynamic>();
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            //Dict.Add("h", InternalDict);

            Dict3.Add("solver", "PBiCGStab");
            Dict3.Add("preconditioner", "DILU");
            Dict3.Add("tolerance", "1e-5");
            Dict3.Add("relTol", "0.1");

            return Dict3;
        }

        static Dictionary<string, dynamic> MakeDict_k()
        {
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dict3.Add("solver", "PBiCGStab");
            Dict3.Add("preconditioner", "DILU");
            Dict3.Add("tolerance", "1e-5");
            Dict3.Add("relTol", "0.1");

            return Dict3;
        }

        static Dictionary<string, dynamic> MakeDict_omega()
        {
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dict3.Add("solver", "PBiCGStab");
            Dict3.Add("preconditioner", "DILU");
            Dict3.Add("tolerance", "1e-5");
            Dict3.Add("relTol", "0.1");

            return Dict3;
        }

        static Dictionary<string, dynamic> MakeDict_AoA()
        {
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dict3.Add("solver", "GAMG");
            Dict3.Add("tolerance", "1e-7");
            Dict3.Add("relTol", "1e-8");
            Dict3.Add("nPreSweeps", "0");
            Dict3.Add("nPostSweeps", "2");
            Dict3.Add("cacheAgglomeration", "true");
            Dict3.Add("smoother", "GaussSeidel");
            Dict3.Add("agglomerator", "faceAreaPair");
            Dict3.Add("nCellsInCoarsestLevel", "10");
            Dict3.Add("mergeLevels", "1");
            Dict3.Add("maxIter", "100");


            return Dict3;
        }

        //Solvers - 3 Nested Dict
        private static Dictionary<string, dynamic> GetSolversDict()
        {
            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();

            Dict1.Add("solver", Dict2);

            Dict2.Add("p_rgh",MakeDict_p_rgh());
            Dict2.Add("U",MakeDict_U());
            Dict2.Add("h", MakeDict_h());
            Dict2.Add("k", MakeDict_k());
            Dict2.Add("omega", MakeDict_omega());
            Dict2.Add("AoA", MakeDict_AoA());

            return Dict1;
        }



        static Dictionary<string, dynamic> MakeDict_residualControl()
        {
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dict3.Add("p_rgh", "1e-4");
            Dict3.Add("U", "1e-3");
            Dict3.Add("h", "1e-3");

            return Dict3;
        }

        //SIMPLE - 1 Nested Dict Level3, 3 dict Level2
        private static Dictionary<string, dynamic> GetSimpleDict()
        {
            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();

            Dict1.Add("SIMPLE", Dict2);

            //nested - level 3 dict
            Dict2.Add("residualControl", MakeDict_residualControl());

            //not nested - level 2 dict
            Dict2.Add("nNonOrthogonalCorrectors", "0");
            Dict2.Add("pRefCell", "0");
            Dict2.Add("pRefValue", "0");

            return Dict1;
        }


        //relaxationFactors
        // Dict level 2 : 
        //    fields
        //    equations
        static Dictionary<string, dynamic> MakeDict_fields()
        {
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dict3.Add("p_rgh", "0.3");
            Dict3.Add("AoA", "0.5");

            return Dict3;
        }

        static Dictionary<string, dynamic> MakeDict_equations()
        {
            Dictionary<string, dynamic> Dict3 = new Dictionary<string, dynamic>();

            Dict3.Add("U", "0.3");
            Dict3.Add("T", "0.5");
            Dict3.Add("h", "0.3");
            Dict3.Add("rho", "0.3");
            Dict3.Add("k", "0.1");
            Dict3.Add("omega", "0.1");

            return Dict3;
        }

        //relaxationFactors - 2 Nested Dict, Level 3
        private static Dictionary<string, dynamic> GetrelaxationFactorsDict()
        {
            Dictionary<string, dynamic> Dict1 = new Dictionary<string, dynamic>();

            Dictionary<string, dynamic> Dict2 = new Dictionary<string, dynamic>();

            Dict1.Add("relaxationFactors", Dict2);

            //nested - level 3 dict
            Dict2.Add("fields", MakeDict_fields());
            Dict2.Add("equations", MakeDict_equations());


            return Dict1;
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

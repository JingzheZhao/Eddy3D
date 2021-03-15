using Grasshopper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib.Thermal
{
    class LongWaveInjectedIDFCode
    {
        private void GenerateLongWaveIDFCode(DataTree<double> vf, ref object A)
        {

            StringBuilder sb = new StringBuilder();


            for (int i = 0; i < vf.BranchCount; i++)
            {
                string for1patch = @"

        !-   ===========  SURFACE " + i + @" ===========

        Schedule:Constant,
        UNZ_" + i + @":f1_STSched,     !- Name
        Temperature,               !- Schedule Type Limits Name
        0;                         !- Hourly Value

        EnergyManagementSystem:Sensor,
        UNZ_" + i + @"_F1_ST, !Name
        UNZ_" + i + @":F1, ! Output:Variable Index Key Name
        Surface Outside Face Temperature; ! Output:Variable Name

        EnergyManagementSystem:Actuator,
        UNZ_" + i + @"_F1_STSCHED_Override,
        UNZ_" + i + @":F1_STSCHED,Schedule:Constant,Schedule Value;


        SurfaceProperty:LocalEnvironment,
        UNZ_" + i + @":f1_LocalEnv,                   !- Name
        UNZ_" + i + @":f1,                            !- Exterior Surface Name
        ,                                         !- External Shading Fraction Schedule Name
        UNZ_" + i + @":f1_ViewFac;                    !- Surrounding Surfaces Object Name

        SurfaceProperty:SurroundingSurfaces,
        UNZ_" + i + @":f1_ViewFac,              !- Name
        ,         		                      !- Sky View Factor
        ,                                   !- Sky Temperature Schedule Name
        ,                                   !- Ground View Factor
        ,                                   !- Ground Temperature Schedule Name
        ";

                var viewFacs = vf.Branch(vf.Paths[i]);

                for (int j = 0; j < viewFacs.Count; j++)
                {

                    if (viewFacs[j] == 0) continue;

                    string s = @"
          UNZ_" + j + @":f1_ViewFac,                   !- Surrounding Surface " + j + @" Name
          " + Math.Round(viewFacs[j], 4) + @",                                !- Surrounding Surface " + j + @" View Factor
          UNZ_" + j + @":f1_STSched,
          ";
                    for1patch += s;
                }

                for1patch = for1patch.Trim();
                for1patch = for1patch.Remove(for1patch.Length - 1, 1) + ";";



                sb.AppendLine(for1patch);
                sb.AppendLine("");

            }

            sb.AppendLine(@"

      EnergyManagementSystem:ProgramCallingManager,
      SurfTempScheduleOverride,
      BeginTimestepBeforePredictor,
      SurfTempScheduleOverrideProg;

      EnergyManagementSystem:Program,
      SurfTempScheduleOverrideProg,
      ");

            for (int i = 0; i < vf.BranchCount; i++)
            {
                if (i == vf.BranchCount - 1) sb.AppendLine("  Set UNZ_" + i + @"_F1_STSCHED_Override = UNZ_" + i + @"_F1_ST;");

                else sb.AppendLine("  Set UNZ_" + i + @"_F1_STSCHED_Override = UNZ_" + i + @"_F1_ST,");

            }



            A = sb;

        }
    }
}

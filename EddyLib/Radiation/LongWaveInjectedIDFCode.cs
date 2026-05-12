using Grasshopper;
using System;
using System.Text;

namespace EddyLib.Radiation
{
    internal class LongWaveInjectedIDFCode
    {
        private void GenerateLongWaveIDFCode(DataTree<double> vf, ref object A)
        {
            StringBuilder sb = new StringBuilder();
            int branchCount = vf.BranchCount;

            for (int i = 0; i < branchCount; i++)
            {
                int startIndex = sb.Length;

                sb.Append($@"!-   ===========  SURFACE {i} ===========

        Schedule:Constant,
        UNZ_{i}:f1_STSched,     !- Name
        Temperature,               !- Schedule Type Limits Name
        0;                         !- Hourly Value

        EnergyManagementSystem:Sensor,
        UNZ_{i}_F1_ST, !Name
        UNZ_{i}:F1, ! Output:Variable Index Key Name
        Surface Outside Face Temperature; ! Output:Variable Name

        EnergyManagementSystem:Actuator,
        UNZ_{i}_F1_STSCHED_Override,
        UNZ_{i}:F1_STSCHED,Schedule:Constant,Schedule Value;

        SurfaceProperty:LocalEnvironment,
        UNZ_{i}:f1_LocalEnv,                   !- Name
        UNZ_{i}:f1,                            !- Exterior Surface Name
        ,                                         !- External Shading Fraction Schedule Name
        UNZ_{i}:f1_ViewFac;                    !- Surrounding Surfaces Object Name

        SurfaceProperty:SurroundingSurfaces,
        UNZ_{i}:f1_ViewFac,              !- Name
        ,         		                      !- Sky View Factor
        ,                                   !- Sky Temperature Schedule Name
        ,                                   !- Ground View Factor
        ,                                   !- Ground Temperature Schedule Name
        ");

                var viewFacs = vf.Branch(vf.Paths[i]);
                int viewFacsCount = viewFacs.Count;

                for (int j = 0; j < viewFacsCount; j++)
                {
                    if (viewFacs[j] == 0) continue;

                    sb.Append($@"
          UNZ_{j}:f1_ViewFac,                   !- Surrounding Surface {j} Name
          {Math.Round(viewFacs[j], 4)},                                !- Surrounding Surface {j} View Factor
          UNZ_{j}:f1_STSched,
          ");
                }

                while (sb.Length > startIndex && char.IsWhiteSpace(sb[sb.Length - 1]))
                {
                    sb.Length--;
                }

                if (sb.Length > startIndex)
                {
                    sb.Length--;
                    sb.Append(';');
                }

                sb.AppendLine();
                sb.AppendLine();
            }

            sb.AppendLine(@"

      EnergyManagementSystem:ProgramCallingManager,
      SurfTempScheduleOverride,
      BeginTimestepBeforePredictor,
      SurfTempScheduleOverrideProg;

      EnergyManagementSystem:Program,
      SurfTempScheduleOverrideProg,
      ");

            for (int i = 0; i < branchCount; i++)
            {
                if (i == branchCount - 1) sb.AppendLine($@"  Set UNZ_{i}_F1_STSCHED_Override = UNZ_{i}_F1_ST;");
                else sb.AppendLine($@"  Set UNZ_{i}_F1_STSCHED_Override = UNZ_{i}_F1_ST,");
            }

            A = sb;
        }
    }
}
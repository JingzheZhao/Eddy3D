using EddyLib.BCs;
using EddyLib.OpenFOAM;
using System.IO;

namespace EddyLib
{
    /// <summary>
    /// Generates OpenFOAM simulation case files for each wind direction.
    /// </summary>
    public static partial class RunFoamSimulation
    {
        #region Boundary Condition Files

        private static void WriteBoundaryFiles(OFBaseDomain domain, int index, CasePaths paths)
        {
            if (domain is OFBoxDomain)
            {
                WriteBoxBoundaryFiles(domain, index, paths);
            }
            else if (domain is OFCylDomain cylDomain)
            {
                WriteCylBoundaryFiles(cylDomain, index, paths);
            }
        }

        private static void WriteBoxBoundaryFiles(OFBaseDomain domain, int index, CasePaths paths)
        {
            WriteVelocityAndAbl(domain, index, paths,
                Strings.BCDicts.UBoxABL(domain, index),
                Strings.BCDicts.UBoxConstU(domain, index));

            DictFileWriter.WriteBoundaryPair(paths, "p", Strings.BCDicts.P(domain));
            DictFileWriter.WriteBoundaryPair(paths, "omega", Strings.BCDicts.Omega(domain));
            DictFileWriter.WriteBoundaryPair(paths, "k", Strings.BCDicts.K(domain));
            DictFileWriter.WriteBoundaryPair(paths, "epsilon", Strings.BCDicts.Epsilon(domain));
            DictFileWriter.WriteBoundaryPair(paths, "nut", Strings.BCDicts.Nut(domain));
            DictFileWriter.WriteBoundaryPair(paths, "aoa", Strings.BCDicts.AOA());
            DictFileWriter.WriteBoundaryPair(paths, "initialConditions", Strings.BCDicts.InitialConditions(domain, index));
        }

        private static void WriteCylBoundaryFiles(OFCylDomain domain, int index, CasePaths paths)
        {
            WriteVelocityAndAbl(domain, index, paths,
                Strings.BCDicts.U_CylABL(domain, index),
                Strings.BCDicts.UCylConstU(domain, index));

            DictFileWriter.WriteBoundaryPair(paths, "p", Strings.BCDicts.P_Cyl(domain, index));
            DictFileWriter.WriteBoundaryPair(paths, "omega", Strings.BCDicts.Omega_Cyl(domain, index));
            DictFileWriter.WriteBoundaryPair(paths, "k", Strings.BCDicts.K_Cyl(domain, index));
            DictFileWriter.WriteBoundaryPair(paths, "epsilon", Strings.BCDicts.Epsilon_Cyl(domain, index));
            DictFileWriter.WriteBoundaryPair(paths, "nut", Strings.BCDicts.Nut_Cyl(domain, index));
            DictFileWriter.WriteBoundaryPair(paths, "aoa", Strings.BCDicts.AOA_Cyl(domain, index));
            DictFileWriter.WriteBoundaryPair(paths, "initialConditions", Strings.BCDicts.InitialConditions(domain, index));
        }

        private static void WriteVelocityAndAbl(OFBaseDomain domain, int index, CasePaths paths, string uAblContent, string uConstContent)
        {
            if (domain.BCond.BCs[index] is ABL abl)
            {
                DictFileWriter.WriteBoundaryPair(paths, "U", uAblContent);
                DictFileWriter.WriteBoundaryPair(paths, "ABLConditions", Strings.BCDicts.ABL(abl, index));
            }
            else if (domain.BCond.BCs[index] is ConstU)
            {
                var bcond = CreateFallbackAbl(domain, index);
                DictFileWriter.WriteBoundaryPair(paths, "U", uConstContent);
                DictFileWriter.WriteBoundaryPair(paths, "ABLConditions", Strings.BCDicts.ABL(bcond, index));
            }
        }

        private static ABL CreateFallbackAbl(OFBaseDomain domain, int index)
        {
            var bc = domain.BCond.BCs[index];
            return new ABL(domain.BCond.WindDirections[index], bc.URef, 10, bc.z0, 0);
        }

        #endregion
    }
}

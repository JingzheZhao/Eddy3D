using EddyLib;
using EddyLib.OpenFOAM;
using System;
using System.IO;
using System.Runtime.InteropServices;
using Xunit;

namespace RhinoPlugin.Test.Xunit
{
    public class Test_InstallationChecks
    {
        [Fact]
        public void TestCheckBlueCfd_MissingDir()
        {
            // Set BlueCfdDir to a non-existent path
            string originalPath = DefaultDirectoriesAndPaths.BlueCfdDir;
            string tempMissingPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

            try
            {
                DefaultDirectoriesAndPaths.BlueCfdDir = tempMissingPath;
                Assert.Throws<FileNotFoundException>(() => DefaultDirectoriesAndPaths.CheckBlueCfd());
            }
            finally
            {
                DefaultDirectoriesAndPaths.BlueCfdDir = originalPath;
            }
        }

        [Fact]
        public void TestCheckBlueCfd_MissingSetvars()
        {
            string originalPath = DefaultDirectoriesAndPaths.BlueCfdDir;
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                DefaultDirectoriesAndPaths.BlueCfdDir = tempDir;
                // Should throw because setvars_OF8.bat and README.TXT are missing
                Assert.Throws<FileNotFoundException>(() => DefaultDirectoriesAndPaths.CheckBlueCfd());
            }
            finally
            {
                DefaultDirectoriesAndPaths.BlueCfdDir = originalPath;
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void TestCheckBlueCfd_Valid()
        {
            string originalPath = DefaultDirectoriesAndPaths.BlueCfdDir;
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);
            File.WriteAllText(Path.Combine(tempDir, "setvars_OF8.bat"), "rem dummy");

            try
            {
                DefaultDirectoriesAndPaths.BlueCfdDir = tempDir;
                // Should NOT throw
                DefaultDirectoriesAndPaths.CheckBlueCfd();
            }
            finally
            {
                DefaultDirectoriesAndPaths.BlueCfdDir = originalPath;
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }
        [Fact]
        public void TestNormalizeEnginePath()
        {
            string originalPath = DefaultDirectoriesAndPaths.RadianceDir;
            string baseDir = @"C:\TestRadiance";
            string binDir = @"C:\TestRadiance\bin";

            try
            {
                // Test setting base dir
                DefaultDirectoriesAndPaths.RadianceDir = baseDir;
                Assert.Equal(baseDir, DefaultDirectoriesAndPaths.RadianceDir);

                // Test setting bin dir (should be normalized to parent)
                DefaultDirectoriesAndPaths.RadianceDir = binDir;
                Assert.Equal(baseDir, DefaultDirectoriesAndPaths.RadianceDir);

                // Test boolean strings (should revert to default)
                DefaultDirectoriesAndPaths.RadianceDir = "False";
                Assert.Contains("Radiance_012cb178_Windows", DefaultDirectoriesAndPaths.RadianceDir);

                DefaultDirectoriesAndPaths.RadianceDir = "True";
                Assert.Contains("Radiance_012cb178_Windows", DefaultDirectoriesAndPaths.RadianceDir);

                // Test whitespace/empty
                DefaultDirectoriesAndPaths.RadianceDir = " ";
                Assert.Contains("Radiance_012cb178_Windows", DefaultDirectoriesAndPaths.RadianceDir);
            }
            finally
            {
                DefaultDirectoriesAndPaths.RadianceDir = originalPath;
            }
        }

        [Fact]
        public void CasesDir_UsesExpectedPlatformLocation()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var expected = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Eddy3D",
                    "Cases");
                Assert.Equal(expected, DefaultDirectoriesAndPaths.CasesDir, ignoreCase: true);
            }
            else
            {
                var expected = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Eddy3D",
                    "Cases");
                Assert.Equal(expected, DefaultDirectoriesAndPaths.CasesDir);
            }
        }

        [Fact]
        public void ResolveWorkingDirectory_SimpleName_UsesCasesDir()
        {
            var resolved = DefaultDirectoriesAndPaths.ResolveWorkingDirectory("MyCase");
            var expected = Path.Combine(DefaultDirectoriesAndPaths.CasesDir, "MyCase");
            Assert.Equal(expected, resolved, ignoreCase: RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
        }

        [Fact]
        public void FoamCleaner_RemovesGeneratedFieldsFromZero_UsingReferenceFolder()
        {
            var caseRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(caseRoot, "system"));
            Directory.CreateDirectory(Path.Combine(caseRoot, "constant"));
            Directory.CreateDirectory(Path.Combine(caseRoot, "0"));
            Directory.CreateDirectory(Path.Combine(caseRoot, "0.org"));
            Directory.CreateDirectory(Path.Combine(caseRoot, "postProcessing"));
            Directory.CreateDirectory(Path.Combine(caseRoot, "100"));

            File.WriteAllText(Path.Combine(caseRoot, "0.org", "U"), "baseline U");
            File.WriteAllText(Path.Combine(caseRoot, "0.org", "p"), "baseline p");

            File.WriteAllText(Path.Combine(caseRoot, "0", "U"), "run U");
            File.WriteAllText(Path.Combine(caseRoot, "0", "p"), "run p");
            File.WriteAllText(Path.Combine(caseRoot, "0", "total(p)_coeff"), "generated");
            File.WriteAllText(Path.Combine(caseRoot, "0", "yPlus"), "generated");

            try
            {
                var ok = FoamCleaner.CleanCase(caseRoot);
                Assert.True(ok);

                Assert.True(File.Exists(Path.Combine(caseRoot, "0", "U")));
                Assert.True(File.Exists(Path.Combine(caseRoot, "0", "p")));
                Assert.False(File.Exists(Path.Combine(caseRoot, "0", "total(p)_coeff")));
                Assert.False(File.Exists(Path.Combine(caseRoot, "0", "yPlus")));

                Assert.False(Directory.Exists(Path.Combine(caseRoot, "postProcessing")));
                Assert.False(Directory.Exists(Path.Combine(caseRoot, "100")));
            }
            finally
            {
                if (Directory.Exists(caseRoot))
                {
                    Directory.Delete(caseRoot, true);
                }
            }
        }

        [Fact]
        public void FoamCleaner_RemovesKnownCoeffFieldWithoutReferenceFolder()
        {
            var caseRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(Path.Combine(caseRoot, "system"));
            Directory.CreateDirectory(Path.Combine(caseRoot, "constant"));
            Directory.CreateDirectory(Path.Combine(caseRoot, "0"));

            File.WriteAllText(Path.Combine(caseRoot, "0", "U"), "baseline U");
            File.WriteAllText(Path.Combine(caseRoot, "0", "total(p)_coeff"), "generated");

            try
            {
                var ok = FoamCleaner.CleanCase(caseRoot);
                Assert.True(ok);
                Assert.True(File.Exists(Path.Combine(caseRoot, "0", "U")));
                Assert.False(File.Exists(Path.Combine(caseRoot, "0", "total(p)_coeff")));
            }
            finally
            {
                if (Directory.Exists(caseRoot))
                {
                    Directory.Delete(caseRoot, true);
                }
            }
        }

        [Fact]
        public void AutoCpuCount_Is75PercentOfPhysicalCores()
        {
            int physical = Utilities.GetPhysicalCoreCount();
            int auto = Utilities.GetAutoCpuCount();

            int expected = Math.Max(1, (int)Math.Floor(physical * 0.75));

            Assert.True(physical >= 1);
            Assert.Equal(expected, auto);
            Assert.InRange(auto, 1, physical);
        }

        [Fact]
        public void CalculateOptimalCPUs_UsesAutoForMinusOne_AndClampsInvalid()
        {
            int auto = Utilities.GetAutoCpuCount();

            Assert.Equal(auto, OpenFOAMHelpers.CalculateOptimalCPUs("unused", -1));
            Assert.Equal(6, OpenFOAMHelpers.CalculateOptimalCPUs("unused", 6));
            Assert.Equal(1, OpenFOAMHelpers.CalculateOptimalCPUs("unused", 0));
            Assert.Equal(1, OpenFOAMHelpers.CalculateOptimalCPUs("unused", -5));
        }
    }
}

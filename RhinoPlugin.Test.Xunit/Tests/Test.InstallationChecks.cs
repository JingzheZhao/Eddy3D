using EddyLib;
using EddyLib.Helpers;
using EddyLib.OpenFOAM;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
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
                // Should throw because blueCFD-Core 2024 files are missing.
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
            Directory.CreateDirectory(Path.Combine(tempDir, "OpenFOAM-12"));
            File.WriteAllText(Path.Combine(tempDir, "setvars.bat"), "rem dummy");

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
        public void BuildBlueCfdBatch_UsesDetectedInstalledMpiPath()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string caseDir = Path.Combine(tempDir, "case");
            string mpiName = "MS-MPI-10.2.0";
            string mpiBin = Path.Combine(tempDir, "ThirdParty-12", "platforms", "mingw_w64Gcc122", mpiName, "bin");
            string olderMpiBin = Path.Combine(tempDir, "ThirdParty-12", "platforms", "mingw_w64Gcc122", "MS-MPI-10.1.2", "bin");
            string mpiLib = Path.Combine(tempDir, "OpenFOAM-12", "platforms", "mingw_w64Gcc122DPInt32Opt", "lib", mpiName);

            Directory.CreateDirectory(caseDir);
            Directory.CreateDirectory(mpiBin);
            Directory.CreateDirectory(olderMpiBin);
            Directory.CreateDirectory(mpiLib);
            File.WriteAllText(Path.Combine(tempDir, "setvars_OF12.bat"), "@echo off");
            File.WriteAllText(Path.Combine(mpiBin, "mpiexec.exe"), string.Empty);
            File.WriteAllText(Path.Combine(olderMpiBin, "mpiexec.exe"), string.Empty);
            File.WriteAllText(Path.Combine(mpiLib, "libPstream.dll"), string.Empty);

            try
            {
                string script = EddyLib.Strings.BatFiles.BlueCfdScriptBuilder.BuildBlueCfdBatch(
                    new[] { "mpiexec -np 2 foamRun -solver incompressibleFluid -parallel" },
                    caseDir,
                    EddyLib.Strings.RunMode.Canvas,
                    tempDir);

                Assert.Contains(Path.Combine(tempDir, "setvars_OF12.bat"), script);
                Assert.Contains(mpiBin, script);
                Assert.Contains(mpiLib, script);
                Assert.Contains($"FOAM_MPI={mpiName}", script);
                Assert.Equal(mpiName, DefaultDirectoriesAndPaths.GetBlueCfdMpiName(tempDir));
                Assert.Equal("10.2.0", DefaultDirectoriesAndPaths.GetBlueCfdMpiVersion(tempDir));
            }
            finally
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void FindResidualsDat_FindsNonZeroTimeDirectory()
        {
            string caseDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            string residualsPath = Path.Combine(caseDir, "postProcessing", "residuals", "1", "residuals.dat");

            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(residualsPath));
                File.WriteAllText(residualsPath, "# Residuals\n1 1e-3\n");

                string foundPath = EddyLib.Strings.PlotResiduals.FindResidualsDat(caseDir);
                string foundFolder = EddyLib.Strings.PlotResiduals.FindResidualsFolder(caseDir);

                Assert.Equal(residualsPath, foundPath);
                Assert.Equal(Path.GetDirectoryName(residualsPath), foundFolder);
            }
            finally
            {
                if (Directory.Exists(caseDir)) Directory.Delete(caseDir, true);
            }
        }

        [Fact]
        public void TestCheckBlueCfd_PathWithSpaces_Throws()
        {
            string originalPath = DefaultDirectoriesAndPaths.BlueCfdDir;
            string tempDir = Path.Combine(Path.GetTempPath(), "blueCFD space test " + Guid.NewGuid().ToString("N").Substring(0, 6));
            Directory.CreateDirectory(tempDir);
            Directory.CreateDirectory(Path.Combine(tempDir, "OpenFOAM-12"));
            File.WriteAllText(Path.Combine(tempDir, "setvars.bat"), "rem dummy");

            try
            {
                DefaultDirectoriesAndPaths.BlueCfdDir = tempDir;
                var ex = Assert.Throws<InvalidOperationException>(() => DefaultDirectoriesAndPaths.CheckBlueCfd());
                Assert.Contains("spaces", ex.Message);
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
            bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            string baseDir = isWindows ? @"C:\TestRadiance" : "/tmp/TestRadiance";
            string binDir = isWindows ? @"C:\TestRadiance\bin" : "/tmp/TestRadiance/bin";
            string expectedDefaultFragment = isWindows
                ? DefaultDirectoriesAndPaths.RadianceWindowsFolderName
                : RuntimeInformation.ProcessArchitecture == Architecture.Arm64
                    ? DefaultDirectoriesAndPaths.RadianceMacOSArm64FolderName
                    : DefaultDirectoriesAndPaths.RadianceMacOSFolderName;

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
                Assert.Contains(expectedDefaultFragment, DefaultDirectoriesAndPaths.RadianceDir);

                DefaultDirectoriesAndPaths.RadianceDir = "True";
                Assert.Contains(expectedDefaultFragment, DefaultDirectoriesAndPaths.RadianceDir);

                // Test whitespace/empty
                DefaultDirectoriesAndPaths.RadianceDir = " ";
                Assert.Contains(expectedDefaultFragment, DefaultDirectoriesAndPaths.RadianceDir);

                // Test foreign absolute path from another OS (should revert to default)
                DefaultDirectoriesAndPaths.RadianceDir = isWindows
                    ? "/Users/patrickkastner/Eddy3D/Radiance"
                    : @"C:\Program Files\Radiance";
                Assert.Contains(expectedDefaultFragment, DefaultDirectoriesAndPaths.RadianceDir);
            }
            finally
            {
                DefaultDirectoriesAndPaths.RadianceDir = originalPath;
            }
        }

        [Fact]
        public void Eddy3DInstallDir_UsesExpectedPlatformLocation()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var expected = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Eddy3D");
                Assert.Equal(expected, DefaultDirectoriesAndPaths.Eddy3DInstallDir, ignoreCase: true);
            }
            else
            {
                var expected = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "Eddy3D");
                Assert.Equal(expected, DefaultDirectoriesAndPaths.Eddy3DInstallDir);
            }
        }

        [Fact]
        public void CasesDir_UsesExpectedPlatformLocation()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var expected = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
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
        public void ResolveWorkingDirectory_EmptyInput_UsesAutoGeneratedCasePath()
        {
            var first = DefaultDirectoriesAndPaths.ResolveWorkingDirectory("");
            var second = DefaultDirectoriesAndPaths.ResolveWorkingDirectory("");
            AssertAutoGeneratedCasePath(first);
            Assert.Equal(first, second);
        }

        [Fact]
        public void ResolveWorkingDirectory_WindowsAbsolutePath_OnNonWindows_UsesAutoGeneratedLocalCasePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var input = @"C:\Users\pkastner\AppData\Local\Eddy3D\Cases\MyCase";
            var first = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            var second = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            AssertAutoGeneratedCasePath(first);
            Assert.Equal(first, second);
        }

        [Fact]
        public void ResolveWorkingDirectory_WindowsCasesRoot_OnNonWindows_UsesAutoGeneratedLocalCasePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var input = @"C:\Users\pkastner\AppData\Local\Eddy3D\Cases";
            var first = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            var second = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            AssertAutoGeneratedCasePath(first);
            Assert.Equal(first, second);
        }

        [Fact]
        public void ResolveWorkingDirectory_MalformedWindowsPath_OnNonWindows_UsesAutoGeneratedLocalCasePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var input = @"C/\Users\pkastner\AppData\Local\Eddy3D\Cases";
            var first = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            var second = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            AssertAutoGeneratedCasePath(first);
            Assert.Equal(first, second);
        }

        [Fact]
        public void ResolveWorkingDirectory_NestedMigratedWindowsPath_OnNonWindows_UsesAutoGeneratedLocalCasePath()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var input = Path.Combine(
                DefaultDirectoriesAndPaths.CasesDir,
                @"C:\Users\pkastner\AppData\Local\Eddy3D\Cases\CaseA");
            var first = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            var second = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            AssertAutoGeneratedCasePath(first);
            Assert.Equal(first, second);
        }

        [Fact]
        public void ResolveWorkingDirectory_UnixAbsolutePath_OnWindows_UsesAutoGeneratedLocalCasePath()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return;
            }

            var input = "/Users/patrickkastner/Eddy3D/Cases/MyCase";
            var first = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            var second = DefaultDirectoriesAndPaths.ResolveWorkingDirectory(input);
            AssertAutoGeneratedCasePath(first);
            Assert.Equal(first, second, ignoreCase: true);
        }

        private static void AssertAutoGeneratedCasePath(string resolvedPath)
        {
            Assert.False(string.IsNullOrWhiteSpace(resolvedPath));

            string fullPath = Path.GetFullPath(resolvedPath);
            string casesRoot = Path.GetFullPath(DefaultDirectoriesAndPaths.CasesDir)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string expectedPrefix = casesRoot + Path.DirectorySeparatorChar;
            StringComparison comparison = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            Assert.True(
                fullPath.StartsWith(expectedPrefix, comparison),
                $"Expected '{fullPath}' to be under '{casesRoot}'.");

            string leaf = Path.GetFileName(fullPath);
            Assert.True(
                Regex.IsMatch(leaf, @"^Case_\d{8}_\d{6}_[0-9a-f]{4}$"),
                $"Expected auto case name format 'Case_yyyyMMdd_HHmmss_xxxx' but got '{leaf}'.");
        }

        [Fact]
        public void FixDirectories_AcceptsEitherTrailingSeparatorWithoutMixing()
        {
            Assert.Equal(@"abc\", DirectoryHelpers.EnsureTrailingBackslash(@"abc\"));
            Assert.Equal("abc/", DirectoryHelpers.EnsureTrailingBackslash("abc/"));
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
        public void FoamCleaner_RemovesExtendedFeatureEdgeMeshFolders()
        {
            var caseRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var constantDir = Path.Combine(caseRoot, "constant");
            var rootFeatureDir = Path.Combine(caseRoot, "extendedFeatureEdgeMesh");
            var constantFeatureDir = Path.Combine(constantDir, "extendedFeatureEdgeMesh");
            var airRegionFeatureDir = Path.Combine(constantDir, "air", "extendedFeatureEdgeMesh");

            Directory.CreateDirectory(Path.Combine(caseRoot, "system"));
            Directory.CreateDirectory(constantDir);
            Directory.CreateDirectory(Path.Combine(caseRoot, "0"));
            Directory.CreateDirectory(rootFeatureDir);
            Directory.CreateDirectory(constantFeatureDir);
            Directory.CreateDirectory(airRegionFeatureDir);

            File.WriteAllText(Path.Combine(rootFeatureDir, "rootFeature.eMesh"), "dummy");
            File.WriteAllText(Path.Combine(constantFeatureDir, "mainFeature.eMesh"), "dummy");
            File.WriteAllText(Path.Combine(airRegionFeatureDir, "airFeature.eMesh"), "dummy");

            try
            {
                var ok = FoamCleaner.CleanCase(caseRoot);
                Assert.True(ok);

                Assert.False(Directory.Exists(rootFeatureDir));
                Assert.False(Directory.Exists(constantFeatureDir));
                Assert.False(Directory.Exists(airRegionFeatureDir));
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
        public void FoamCleaner_RemovesRootProbeCacheBinaries()
        {
            var workingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            var rootPostProcessing = Path.Combine(workingDir, "postProcessing");
            var nestedPostProcessing = Path.Combine(workingDir, "90", "postProcessing");

            Directory.CreateDirectory(rootPostProcessing);
            Directory.CreateDirectory(nestedPostProcessing);

            var rootCacheA = Path.Combine(rootPostProcessing, "90_probe_U.bin");
            var rootCacheB = Path.Combine(rootPostProcessing, "180_customProbe_p.bin");
            var rootNonCache = Path.Combine(rootPostProcessing, "keep.txt");
            var nestedBin = Path.Combine(nestedPostProcessing, "should-stay.bin");

            File.WriteAllText(rootCacheA, "cache");
            File.WriteAllText(rootCacheB, "cache");
            File.WriteAllText(rootNonCache, "keep");
            File.WriteAllText(nestedBin, "nested");

            try
            {
                var ok = FoamCleaner.CleanProbeCacheFiles(workingDir);
                Assert.True(ok);

                Assert.False(File.Exists(rootCacheA));
                Assert.False(File.Exists(rootCacheB));
                Assert.True(File.Exists(rootNonCache));
                Assert.True(File.Exists(nestedBin));
            }
            finally
            {
                if (Directory.Exists(workingDir))
                {
                    Directory.Delete(workingDir, true);
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

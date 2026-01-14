using EddyLib;
using System;
using System.IO;
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
    }
}

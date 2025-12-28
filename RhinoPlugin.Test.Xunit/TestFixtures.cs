using EddyLib;
using EddyLib.BCs;
using System;
using System.Collections.Generic;
using System.IO;

namespace RhinoPlugin.Test.Xunit
{
    /// <summary>
    /// Shared test fixtures for creating common test objects.
    /// </summary>
    public static class TestFixtures
    {
        /// <summary>
        /// Creates a default ABL boundary condition.
        /// </summary>
        public static ABL CreateDefaultABL(int windDirection = 0)
        {
            return new ABL(windDirection, TestConstants.DefaultUref, TestConstants.DefaultZref, TestConstants.DefaultZ0, 0);
        }

        /// <summary>
        /// Creates a BCCollection with a single default ABL.
        /// </summary>
        public static BCCollection CreateDefaultBCCollection(int windDirection = 0)
        {
            return new BCCollection(CreateDefaultABL(windDirection));
        }

        /// <summary>
        /// Creates a BCCollection with a list of default ABLs (e.g. for multiple wind directions).
        /// </summary>
        public static BCCollection CreateDefaultBCCollection(IEnumerable<int> windDirections)
        {
            var bcs = new List<BC>();
            foreach (var wd in windDirections)
            {
                bcs.Add(CreateDefaultABL(wd));
            }
            return new BCCollection(bcs);
        }

        /// <summary>
        /// Creates default mesh settings for OpenFOAM.
        /// </summary>
        public static OFMeshSettings CreateDefaultMeshSettings(string caseDir)
        {
            var settings = new OFMeshSettings
            {
                accBuildings = 3,
                accFeatures = 4,
                accGround = 3
            };
            settings.SetDirectories(Utilities.Directories.FixDirectories(caseDir));
            return settings;
        }

        /// <summary>
        /// Creates default run settings for OpenFOAM.
        /// </summary>
        public static OFRunSettings CreateDefaultRunSettings()
        {
            return new OFRunSettings
            {
                iter = TestConstants.DefaultIterations,
                CPUs = 6,
                relaxationFactors = RelaxationFactors.Robust,
                schemes = fvSchemes.Optimized,
                turbModel = TurbModel.kEpsilon
            };
        }

        /// <summary>
        /// Creates a unique temporary test directory.
        /// </summary>
        public static string CreateTestDirectory(string prefix = "testcase")
        {
            var path = Path.Combine(TestConstants.TestingDirectory, $"{prefix}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Cleans up the test directory if it exists.
        /// </summary>
        public static void CleanupTestDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception ex)
            {
                // Log or ignore cleanup errors (common in tests due to file locks)
                Console.WriteLine($"Cleanup warning: {ex.Message}");
            }
        }
    }
}

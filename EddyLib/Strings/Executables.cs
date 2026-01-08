namespace EddyLib.Strings
{
    /// <summary>
    /// OpenFOAM dictionary generation templates.
    /// Methods are split across partial files:
    /// - Executables.Control.cs - ControlDict
    /// - Executables.FunctionObjects.cs - FunctionObj*
    /// - Executables.Probes.cs - SampleProbes, TopoSetDict
    /// - Executables.Schemes.cs - FvSchemes*
    /// - Executables.Solvers.cs - FvSolution*, Relaxation
    /// - Executables.Properties.cs - Transport, Turbulence, Residuals, Decompose
    /// - Executables.Mesh.cs - BlockMesh, Snappy (existing)
    /// </summary>
    public partial class OFExecDicts
    {
        // All methods have been moved to partial files.
        // This file serves as the documentation hub for the class.
    }
}

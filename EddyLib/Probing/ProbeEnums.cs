namespace EddyLib
{
    /// <summary>
    /// Specifies whether a field is a vector or scalar.
    /// </summary>
    public enum fieldType
    {
        vector,
        scalar
    }

    /// <summary>
    /// OpenFOAM field types available for probing.
    /// </summary>
    public enum field
    {
        U, p, cp_coeff, epsilon, omega, k, nut, phi, aoa, covid19
    }
}

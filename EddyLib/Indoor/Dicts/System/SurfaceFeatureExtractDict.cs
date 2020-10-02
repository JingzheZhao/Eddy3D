namespace EddyLib.Indoor.Dicts
{
    internal class SurfaceFeatureExtractDict : GenericDict
    {
        public SurfaceFeatureExtractDict()
        {
            this.Name = "surfaceFeatureExtractDict";

            this.Header = GetHeader(this);
            this.Location = DictLocation.system;

            this.FullDictString = @"FoamFile
{
    version         1912;
    format          ascii;
    class           dictionary;
    location        ""system"";
    object          surfaceFeatureExtractDict;

	    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
        // - 180: selects all edges
        includedAngle   180;
        geometricTestOnly yes;
    }
}

Inlet*.stl
{
    extractionMethod extractFromSurface;
    includedAngle   180.00;
    geometricTestOnly yes;
    intersectionMethod none;
    writeObj        no;
	    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
        // - 180: selects all edges
        includedAngle   180;
        geometricTestOnly yes;
    }
}

Outlet*.stl
{
    extractionMethod extractFromSurface;
    includedAngle   180.00;
    geometricTestOnly yes;
    intersectionMethod none;
    writeObj        no;
	    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
        // - 180: selects all edges
        includedAngle   180;
        geometricTestOnly yes;
    }
}

Walls*.stl
{
    extractionMethod extractFromSurface;
    includedAngle   180.00;
    geometricTestOnly yes;
    intersectionMethod none;
    writeObj        no;
	    extractFromSurfaceCoeffs
    {
        // Mark edges whose adjacent surface normals are at an angle less than includedAngle as features
        // - 0 : selects no edges
        // - 180: selects all edges
        includedAngle   180;
        geometricTestOnly yes;
    }
}

";
        }
    }
}
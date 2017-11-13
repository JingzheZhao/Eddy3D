using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindTunnel
{
    public class StringTemplates
    {
        public static string blockMeshDict(OFDomainBuilder DOM)
        {
       return @"
/*--------------------------------*- C++ -*----------------------------------*\
|       o          |                                                          |
|    o     o       | HELYX-OS                                                  |
|   o   O   o      | Version: v2.2.0                                           |
|    o     o       | Web:     http://www.engys.com                            |
|       o          |                                                          |
\*---------------------------------------------------------------------------*/
FoamFile
{
        version 2.0;
        format ascii;
        class dictionary;
        location system;
        object blockMeshDict;
}
convertToMeters 1;
backgroundMesh
{
        xMin " + DOM.xMin + @";
        xMax " + DOM.xMax + @";
        yMin " + DOM.yMin + @";
        yMax " + DOM.yMax + @";
        zMin " + DOM.zMin + @";
        zMax " + DOM.zMax + @";
        xCells " + DOM.xCells + @";
        yCells " + DOM.yCells + @";
        zCells " + DOM.zCells + @";
}
vertices
(
                ($:backgroundMesh.xMin $:backgroundMesh.yMin $:backgroundMesh.zMin)
                ($:backgroundMesh.xMax $:backgroundMesh.yMin $:backgroundMesh.zMin)
                ($:backgroundMesh.xMax $:backgroundMesh.yMax $:backgroundMesh.zMin)
                ($:backgroundMesh.xMin $:backgroundMesh.yMax $:backgroundMesh.zMin)
                ($:backgroundMesh.xMin $:backgroundMesh.yMin $:backgroundMesh.zMax)
                ($:backgroundMesh.xMax $:backgroundMesh.yMin $:backgroundMesh.zMax)
                ($:backgroundMesh.xMax $:backgroundMesh.yMax $:backgroundMesh.zMax)
                ($:backgroundMesh.xMin $:backgroundMesh.yMax $:backgroundMesh.zMax)
);
blocks
(
        hex (0 1 2 3 4 5 6 7)
        (
                        $:backgroundMesh.xCells
                        $:backgroundMesh.yCells
                        $:backgroundMesh.zCells
        )
        simpleGrading (1 1 1)
);
edges
(
);
boundary
(
        inlet
{
        type patch;
        faces
        (
                (0 1 4 5)
        );
}
        outlet
{
        type patch;
        faces
        (
                (2 3 6 7)
        );
}
        ground
{
        type wall;
        faces
        (
                (0 1 2 3)
        );
}
        frontAndBack
{
        type symmetry;
        faces
        (
                        (1 2 5 6)
                        (0 3 4 7)
                        (4 5 6 7)
        );
}
);
        ";


    }


}
}

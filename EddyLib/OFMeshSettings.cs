using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib
{

    public enum SnappySetting
    {
        Blocks,
        BlocksSnapping,
        BlocksSnappingLayers
    }

    public class OFMeshSettings
    {
       public int accBuildings = 3;
       public int accFeatures = 3;
       public int accRefinement = 3;
       public int accGround = 3;
       public int nLayers = 3;
       public int mode = 2;

        public SnappySetting snappySetting = SnappySetting.Blocks;
        

        public override string ToString()
        {
            return String.Format(@"accBuilding = {0}
accFeatures = {1}
accRefinement = {2}
accGround = {3}
nLayers = {4}
mode = {5}
Snappy Settings = {6}", accBuildings, accFeatures, accRefinement, accGround, nLayers, mode, snappySetting);

}
    }}
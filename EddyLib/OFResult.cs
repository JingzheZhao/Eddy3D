using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EddyLib
{
 public   class OFResult
    {
        OFBaseDomain Domain;
        OFRunSettings RunSettings;
        OFMeshSettings MeshSettings;

        public OFResult(OFBaseDomain Domain, OFRunSettings RunSettings, OFMeshSettings MeshSettings) {
            this.Domain = Domain;
            this.RunSettings = RunSettings;
            this.MeshSettings = MeshSettings;
        }

    }
}

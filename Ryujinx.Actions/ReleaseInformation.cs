using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.Actions
{
    public class ReleaseInformation
    {
        public VersionCore Version { get; set; }
        public List<ArtifactInformation> Artifacts { get; set; }

        private ReleaseInformation()
        {

        }

        public ReleaseInformation(VersionCore version)
        {
            Version   = version;
            Artifacts = new List<ArtifactInformation>();
        }
    }
}

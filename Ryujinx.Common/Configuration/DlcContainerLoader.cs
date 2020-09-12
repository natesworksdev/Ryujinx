using Ryujinx.Common.IO.Abstractions;
using Ryujinx.Common.Utilities;
using System.Collections.Generic;
using System.Linq;

namespace Ryujinx.Common.Configuration
{
    public sealed class DlcContainerLoader
    {
        private readonly string _dlcJsonPath;
        private readonly ILocalStorageManagement _localStorageManagement;

        public DlcContainerLoader(string dlcJsonPath, ILocalStorageManagement localStorageManagement)
        {
            _dlcJsonPath = dlcJsonPath;
            _localStorageManagement = localStorageManagement;
        }

        public IEnumerable<DlcContainer> Load()
        {
            if (!_localStorageManagement.Exists(_dlcJsonPath))
            {
                return Enumerable.Empty<DlcContainer>();
            }                

            using var fileStream = _localStorageManagement.OpenRead(_dlcJsonPath);

            return JsonHelper.Deserialize<IEnumerable<DlcContainer>>(fileStream)
                .Where(c => c.DlcNcaList != null).ToList();
        }
    }
}

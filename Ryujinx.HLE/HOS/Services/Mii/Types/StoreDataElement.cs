using System.Runtime.InteropServices;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [StructLayout(LayoutKind.Sequential, Size = 0x48)]
    public struct StoreDataElement : IElement
    {
        public StoreData StoreData;
        public Source    Source;

        public void SetFromStoreData(StoreData storeData)
        {
            StoreData = storeData;
        }

        public void SetSource(Source source)
        {
            source = Source;
        }
    }
}

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    public interface IElement
    {
        void SetFromStoreData(StoreData storeData);

        void SetSource(Source source);
    }
}

namespace Ryujinx.Horizon.Sdk.Sf.Cmif
{
    abstract class DomainServiceObject : ServerDomainBase, IServiceObject
    {
        public abstract ServerDomainBase GetServerDomain();
    }
}

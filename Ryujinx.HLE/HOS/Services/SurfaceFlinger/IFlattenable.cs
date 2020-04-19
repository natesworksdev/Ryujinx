namespace Ryujinx.HLE.HOS.Services.SurfaceFlinger
{
    interface IFlattenable
    {
        public uint GetFlattenedSize();

        public uint GetFdCount();

        public void Flatten(Parcel parcel);

        public void Unflatten(Parcel parcel);
    }
}

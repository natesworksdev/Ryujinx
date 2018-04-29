using ChocolArm64.Memory;
using Ryujinx.Graphics.Gpu;
using System.Collections.Concurrent;

namespace Ryujinx.Core.OsHle.Services.Nv
{
    class NvMapIoctl
    {
        private NsGpuMemoryMgr Vmm;

        private static ConcurrentDictionary<Process, IdDictionary> NvMaps;

        private object NvMapLock;

        private const int FlagNotFreedYet = 1;

        private enum NvMapParam
        {
            Size  = 1,
            Align = 2,
            Base  = 3,
            Heap  = 4,
            Kind  = 5,
            Compr = 6
        }

        public NvMapIoctl()
        {
            NvMapLock = new object();
        }

        public int Create(ServiceCtx Context)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvMapCreate Args = AMemoryHelper.Read<NvMapCreate>(Context.Memory, InputPosition);

            if (Args.Size == 0)
            {
                return NvResult.InvalidInput;
            }

            Args.Handle = AddNvMap(Context, new NvMap(Args.Size));

            AMemoryHelper.Write(Context.Memory, OutputPosition, Args);

            return NvResult.Success;
        }

        public int IocFromId(ServiceCtx Context)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvMapFromId Args = AMemoryHelper.Read<NvMapFromId>(Context.Memory, InputPosition);

            lock (NvMapLock)
            {
                NvMap Map = GetNvMap(Context, Args.Id);

                if (Map == null)
                {
                    return NvResult.InvalidInput;
                }

                Map.IncrementRefCount();
            }

            AMemoryHelper.Write(Context.Memory, OutputPosition, Args);

            return NvResult.Success;
        }

        public int Alloc(ServiceCtx Context)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvMapAlloc Args = AMemoryHelper.Read<NvMapAlloc>(Context.Memory, InputPosition);

            NvMap Map = GetNvMap(Context, Args.Handle);

            if (Map == null)
            {
                return NvResult.InvalidInput;
            }

            if ((Args.Align & (Args.Align - 1)) != 0)
            {
                return NvResult.InvalidInput;
            }

            if ((uint)Args.Align < 0x1000)
            {
                Args.Align = 0x1000;
            }

            Map.Align = Args.Align;

            Map.Kind = (byte)Args.Kind;

            Args.Address = Vmm.Reserve(Args.Address, (uint)Map.Size, (uint)Map.Align);

            AMemoryHelper.Write(Context.Memory, OutputPosition, Args);

            return NvResult.Success;
        }

        public int Free(ServiceCtx Context)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvMapFree Args = AMemoryHelper.Read<NvMapFree>(Context.Memory, InputPosition);

            lock (NvMapLock)
            {
                NvMap Map = GetNvMap(Context, Args.Handle);

                if (Map == null)
                {
                    return NvResult.InvalidInput;
                }

                long RefCount = Map.DecrementRefCount();

                if (RefCount <= 0)
                {
                    DeleteNvMap(Context, Args.Handle);

                    Args.Flags = 0;
                }
                else
                {
                    Args.Flags = FlagNotFreedYet;
                }

                Args.RefCount = RefCount;
                Args.Size     = Map.Size;
            }

            AMemoryHelper.Write(Context.Memory, OutputPosition, Args);

            return NvResult.Success;
        }

        public int Param(ServiceCtx Context)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            Nv.NvMapParam Args = AMemoryHelper.Read<Nv.NvMapParam>(Context.Memory, InputPosition);

            NvMap Map = GetNvMap(Context, Args.Handle);

            if (Map == null)
            {
                return NvResult.InvalidInput;
            }

            switch ((NvMapParam)Args.Param)
            {
                case NvMapParam.Size:  Args.Result = Map.Size;   break;
                case NvMapParam.Align: Args.Result = Map.Align;  break;
                case NvMapParam.Heap:  Args.Result = 0x40000000; break;
                case NvMapParam.Kind:  Args.Result = Map.Kind;   break;
                case NvMapParam.Compr: Args.Result = 0;          break;

                //Note: Base is not supported and returns an error.
                //Any other value also returns an error.
                default: return NvResult.InvalidInput;
            }

            AMemoryHelper.Write(Context.Memory, OutputPosition, Args);

            return NvResult.Success;
        }

        public int GetId(ServiceCtx Context)
        {
            long InputPosition  = Context.Request.GetBufferType0x21Position();
            long OutputPosition = Context.Request.GetBufferType0x22Position();

            NvMapGetId Args = AMemoryHelper.Read<NvMapGetId>(Context.Memory, InputPosition);

            NvMap Map = GetNvMap(Context, Args.Handle);

            if (Map == null)
            {
                return NvResult.InvalidInput;
            }

            Args.Id = Args.Handle;

            AMemoryHelper.Write(Context.Memory, OutputPosition, Args);

            return NvResult.Success;
        }

        private int AddNvMap(ServiceCtx Context, NvMap Map)
        {
            IdDictionary Maps = NvMaps.GetOrAdd(Context.Process, (Key) => new IdDictionary());

            return Maps.Add(Map);
        }

        private bool DeleteNvMap(ServiceCtx Context, int Handle)
        {
            if (NvMaps.TryGetValue(Context.Process, out IdDictionary Maps))
            {
                return Maps.Delete(Handle) != null;
            }

            return false;
        }

        public NvMap GetNvMapWithFb(ServiceCtx Context, int Handle)
        {
            if (NvMaps.TryGetValue(Context.Process, out IdDictionary Maps))
            {
                return Maps.GetData<NvMap>(Handle);
            }

            return null;
        }

        public NvMap GetNvMap(ServiceCtx Context, int Handle)
        {
            if (Handle != 0 && NvMaps.TryGetValue(Context.Process, out IdDictionary Maps))
            {
                return Maps.GetData<NvMap>(Handle);
            }

            return null;
        }
    }
}
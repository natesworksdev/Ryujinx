using ARMeilleure.Translation.PTC;

namespace ARMeilleure.Translation.TTC
{
    class TtcInfo : PtcInfo
    {
        public ulong LastGuestAddress { get; set; }
        public ulong GuestSize { get; set; }

        public int HostSize { get; set; }

        public bool IsBusy { get; set; }

        public TranslatedFunction TranslatedFunc { get; set; }

        public TtcInfo() : base() { }
    }
}
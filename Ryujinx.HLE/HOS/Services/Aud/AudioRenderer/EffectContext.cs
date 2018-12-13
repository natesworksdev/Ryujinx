using System;
using System.Collections.Generic;
using System.Text;

namespace Ryujinx.HLE.HOS.Services.Aud.AudioRenderer
{
    class EffectContext
    {
        public EffectOut OutStatus;

        public EffectContext()
        {
            OutStatus.State = EffectState.None;
        }
    }
}

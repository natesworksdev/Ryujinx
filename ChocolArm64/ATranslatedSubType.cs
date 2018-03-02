using ChocolArm64.Memory;
using ChocolArm64.State;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;

namespace ChocolArm64
{
    enum ATranslatedSubType
    {
        SubBlock,
        SubTier0,
        SubTier1
    }
}
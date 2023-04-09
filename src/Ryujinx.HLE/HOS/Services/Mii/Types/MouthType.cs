using System.Diagnostics.CodeAnalysis;

namespace Ryujinx.HLE.HOS.Services.Mii.Types
{
    [SuppressMessage("Design", "CA1069:Enums values should not be duplicated")]
    enum MouthType : byte
    {
        Neutral,
        NeutralLips,
        Smile,
        SmileStroke,
        SmileTeeth,
        LipsSmall,
        LipsLarge,
        Wave,
        WaveAngrySmall,
        NeutralStrokeLarge,
        TeethSurprised,
        LipsExtraLarge,
        LipsUp,
        NeutralDown,
        Surprised,
        TeethMiddle,
        NeutralStroke,
        LipsExtraSmall,
        Malicious,
        LipsDual,
        NeutralComma,
        NeutralUp,
        TeethLarge,
        WaveAngry,
        LipsSexy,
        SmileInverted,
        LipsSexyOutline,
        SmileRounded,
        LipsTeeth,
        NeutralOpen,
        TeethRounded,
        WaveAngrySmallInverted,
        NeutralCommaInverted,
        TeethFull,
        SmileDownLine,
        Kiss,

        Min = 0,
        Max = 35
    }
}

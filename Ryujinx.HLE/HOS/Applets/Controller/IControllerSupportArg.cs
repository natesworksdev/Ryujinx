namespace Ryujinx.HLE.HOS.Applets
{
    interface IControllerSupportArg
    {
        string[] GetExplainTexts();
        uint[] GetIdentificationColors();
    }
}
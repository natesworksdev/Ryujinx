using System.Numerics;

namespace Ryujinx.Input;

public interface IHandheld
{
    Vector3 GetMotionData(MotionInputId gyroscope);
}

using Ryujinx.Input;
using SDL3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using static SDL3.SDL3;

namespace Ryujinx.SDL3;

public unsafe class SDL3MotionDriver : IHandheld, IDisposable
{
    private Dictionary<SDL_SensorType, SDL_Sensor> sensors;
    public SDL3MotionDriver()
    {
        SDL_Init(SDL_InitFlags.Sensor);
        sensors = SDL_GetSensors().ToArray().ToDictionary(SDL_GetSensorTypeForID, SDL_OpenSensor);
    }

    public void Dispose()
    {
        foreach (var sensor in sensors.Values)
        {
            SDL_CloseSensor(sensor);
        }
    }

    public Vector3 GetMotionData(MotionInputId gyroscope)
    {
        var data = stackalloc float[3];

        switch (gyroscope)
        {
            case MotionInputId.Gyroscope:
                SDL_GetSensorData(sensors[SDL_SensorType.Gyro], data, 3);
                return new Vector3(data[0], data[1], data[2]) * (180 / MathF.PI);
            case MotionInputId.Accelerometer:
                SDL_GetSensorData(sensors[SDL_SensorType.Accel], data, 3);
                return new Vector3(data[0], data[1], data[2]) / SDL_STANDARD_GRAVITY;
            default:
                return Vector3.Zero;
        }
    }
}

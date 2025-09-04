using System;
using UnityEngine;

namespace QuestMarkerTracking.Utilities
{
    public static class SystemUtils
    {
        // Android Camera2 CameraCharacteristic SENSOR_INFO_TIMESTAMP_SOURCE_UNKNOWN = 0
        // System uses nanoTime as timestampNs
        // ReSharper disable once MemberCanBeMadeStatic.Local because it uses a private member
        public static long GetNanoTime()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            AndroidJavaClass systemClass = new("java.lang.System");
            return systemClass.CallStatic<long>("nanoTime");
#else
            return DateTime.Now.Ticks * 100;
#endif
        }
    }
}
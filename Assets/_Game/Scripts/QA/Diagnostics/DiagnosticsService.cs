using System.Collections.Generic;
using UnityEngine;

namespace Game.QA
{
    using Game.Core;

    public class DiagnosticsService : IDiagnosticsService
    {
        private readonly List<float> _frameTimes = new();
        private float _maxFrameTime;

        public void RecordFrame(float deltaTime)
        {
            _frameTimes.Add(deltaTime * 1000f); // convert to ms
            if (deltaTime * 1000f > _maxFrameTime)
                _maxFrameTime = deltaTime * 1000f;

            // Keep rolling window of last 300 frames
            if (_frameTimes.Count > 300)
                _frameTimes.RemoveAt(0);
        }

        public void Reset()
        {
            _frameTimes.Clear();
            _maxFrameTime = 0f;
        }

        public DeviceDiagnostics GetSnapshot()
        {
            float avgFps = 0f;
            if (_frameTimes.Count > 0)
            {
                float sumMs = 0f;
                foreach (var t in _frameTimes) sumMs += t;
                float avgMs = sumMs / _frameTimes.Count;
                avgFps = avgMs > 0 ? 1000f / avgMs : 0f;
            }

            return new DeviceDiagnostics
            {
                FpsAvg = avgFps,
                MaxFrameTimeMs = _maxFrameTime,
                MemoryUsedBytes = (long)Profiling.Profiler.GetTotalAllocatedMemoryLong(),
                DeviceModel = SystemInfo.deviceModel,
                Platform = Application.platform.ToString(),
                Resolution = $"{Screen.width}x{Screen.height}"
            };
        }
    }
}

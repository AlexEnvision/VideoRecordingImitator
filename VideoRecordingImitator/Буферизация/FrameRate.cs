using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace VideoRecordingImitator
{
    /// <summary>
    /// Ограничение по частоте кадров
    /// </summary>
    public enum FrameRate
    {
        AUTO = 0,
        FPS_24 = 42,
        FPS_25 = 40,
        FPS_30 = 33,
        FPS_50 = 20,
    }
}

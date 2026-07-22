namespace DmdClock.Core.Scn;

public sealed record ScnStoryboard(
    ushort FirstFrameDelayMs,
    bool ClockAboveFirstFrame,
    bool BlankFirstFrame,
    ushort FrameDelayMs,
    bool ClockAboveFrames,
    ushort LastFrameDelayMs,
    bool ClockAboveLastFrame,
    bool BlankLastFrame,
    byte ClockStyle,
    byte CustomX,
    byte CustomY);

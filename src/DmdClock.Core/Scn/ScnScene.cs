namespace DmdClock.Core.Scn;

public sealed record ScnScene(
    ushort Version,
    IReadOnlyList<ScnStoryboard> Storyboards,
    IReadOnlyList<DmdFrame> Frames);


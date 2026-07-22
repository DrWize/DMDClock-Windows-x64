using DmdClock.Core.Playback;
using DmdClock.Core.Scn;

namespace DmdClock.Core.Tests.Playback;

public sealed class ScenePlaybackSessionTests
{
    [Fact]
    public void Advance_UsesFrameTimingAndCompletes()
    {
        using var data = TestScnFile.Create(frameCount: 3, frameDelayMs: 100);
        var scene = ScnReader.Read(data);
        var start = DateTimeOffset.UnixEpoch;
        var session = new ScenePlaybackSession(scene, start);

        Assert.False(session.Advance(start.AddMilliseconds(9)));
        Assert.True(session.Advance(start.AddMilliseconds(10)));
        Assert.Equal(1, session.CurrentFrameIndex);
        Assert.False(session.Advance(start.AddMilliseconds(109)));
        Assert.True(session.Advance(start.AddMilliseconds(110)));
        Assert.Equal(2, session.CurrentFrameIndex);
        Assert.True(session.Advance(start.AddMilliseconds(210)));
        Assert.Equal(2, session.CurrentFrameIndex);
        Assert.False(session.IsComplete);
        Assert.False(session.Advance(start.AddMilliseconds(230)));
        Assert.True(session.IsComplete);
    }

    [Fact]
    public void Pause_PreservesRemainingFrameTime()
    {
        using var data = TestScnFile.Create(frameCount: 2, frameDelayMs: 100);
        var session = new ScenePlaybackSession(ScnReader.Read(data), DateTimeOffset.UnixEpoch);

        session.Pause(DateTimeOffset.UnixEpoch.AddMilliseconds(5));
        Assert.False(session.Advance(DateTimeOffset.UnixEpoch.AddSeconds(5)));
        session.Resume(DateTimeOffset.UnixEpoch.AddSeconds(5));

        Assert.False(session.Advance(DateTimeOffset.UnixEpoch.AddSeconds(5).AddMilliseconds(4)));
        Assert.True(session.Advance(DateTimeOffset.UnixEpoch.AddSeconds(5).AddMilliseconds(5)));
    }

    [Fact]
    public void Constructor_CreatesBlankFirstAndLastSpecialSteps()
    {
        var frame = new DmdFrame(128, 32, Enumerable.Repeat((byte)5, 128 * 32).ToArray());
        var storyboard = new ScnStoryboard(50, false, true, 100, false, 75, true, true, 0, 0, 0);
        var scene = new ScnScene(1, [storyboard], [frame]);
        var start = DateTimeOffset.UnixEpoch;
        var session = new ScenePlaybackSession(scene, start);

        Assert.True(session.IsBlank);
        Assert.Equal(-1, session.CurrentFrameIndex);
        Assert.True(session.Advance(start.AddMilliseconds(50)));
        Assert.False(session.IsBlank);
        Assert.True(session.Advance(start.AddMilliseconds(150)));
        Assert.True(session.IsBlank);
        Assert.True(session.ClockAbove);
    }
}

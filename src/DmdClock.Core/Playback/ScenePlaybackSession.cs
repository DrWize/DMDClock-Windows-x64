using DmdClock.Core.Rendering;
using DmdClock.Core.Scn;

namespace DmdClock.Core.Playback;

public sealed class ScenePlaybackSession
{
    private readonly IReadOnlyList<PlaybackStep> _steps;
    private int _stepIndex;
    private DateTimeOffset _nextFrameAt;
    private TimeSpan _remainingWhenPaused;

    public ScenePlaybackSession(ScnScene scene, DateTimeOffset startedAt)
    {
        ArgumentNullException.ThrowIfNull(scene);
        if (scene.Frames.Count == 0)
            throw new ArgumentException("A scene must contain at least one frame.", nameof(scene));

        Scene = scene;
        Storyboard = scene.Storyboards.FirstOrDefault() ??
            new ScnStoryboard(0, false, false, 100, false, 0, false, false, 0, 0, 0);
        _steps = CreateSteps(scene, Storyboard);
        Restart(startedAt);
    }

    public ScnScene Scene { get; }
    public ScnStoryboard Storyboard { get; }
    public int CurrentFrameIndex => _steps[_stepIndex].FrameIndex;
    public DmdFrame CurrentFrame => _steps[_stepIndex].Frame;
    public bool ClockAbove => _steps[_stepIndex].ClockAbove;
    public bool IsBlank => _steps[_stepIndex].IsBlank;
    public bool IsPaused { get; private set; }
    public bool IsComplete { get; private set; }

    public bool Advance(DateTimeOffset now)
    {
        if (IsPaused || IsComplete || now < _nextFrameAt)
            return false;

        var changed = false;
        while (!IsComplete && now >= _nextFrameAt)
        {
            if (_stepIndex >= _steps.Count - 1)
            {
                IsComplete = true;
                break;
            }

            _stepIndex++;
            _nextFrameAt += _steps[_stepIndex].Duration;
            changed = true;
        }
        return changed;
    }

    public void Pause(DateTimeOffset now)
    {
        if (IsPaused || IsComplete) return;
        _remainingWhenPaused = _nextFrameAt > now ? _nextFrameAt - now : TimeSpan.Zero;
        IsPaused = true;
    }

    public void Resume(DateTimeOffset now)
    {
        if (!IsPaused || IsComplete) return;
        _nextFrameAt = now + _remainingWhenPaused;
        IsPaused = false;
    }

    public void MoveNext(DateTimeOffset now)
    {
        if (_stepIndex < _steps.Count - 1) _stepIndex++;
        IsComplete = false;
        ResetDeadline(now);
    }

    public void MovePrevious(DateTimeOffset now)
    {
        if (_stepIndex > 0) _stepIndex--;
        IsComplete = false;
        ResetDeadline(now);
    }

    public void Restart(DateTimeOffset now)
    {
        _stepIndex = 0;
        IsPaused = false;
        IsComplete = false;
        ResetDeadline(now);
    }

    private void ResetDeadline(DateTimeOffset now)
    {
        _remainingWhenPaused = _steps[_stepIndex].Duration;
        _nextFrameAt = now + _remainingWhenPaused;
    }

    private static IReadOnlyList<PlaybackStep> CreateSteps(ScnScene scene, ScnStoryboard storyboard)
    {
        var steps = new List<PlaybackStep>(scene.Frames.Count + 2);
        var regularDuration = Duration(storyboard.FrameDelayMs);
        var firstNormalFrame = 0;

        if (storyboard.FirstFrameDelayMs > 0)
        {
            var blank = storyboard.BlankFirstFrame;
            steps.Add(new PlaybackStep(
                blank ? DmdFrameCompositor.CreateBlank() : scene.Frames[0],
                blank ? -1 : 0,
                storyboard.ClockAboveFirstFrame,
                blank,
                Duration(storyboard.FirstFrameDelayMs)));
            if (!blank) firstNormalFrame = 1;
        }

        for (var index = firstNormalFrame; index < scene.Frames.Count; index++)
            steps.Add(new PlaybackStep(scene.Frames[index], index, storyboard.ClockAboveFrames, false, regularDuration));

        if (storyboard.LastFrameDelayMs > 0)
        {
            var blank = storyboard.BlankLastFrame;
            steps.Add(new PlaybackStep(
                blank ? DmdFrameCompositor.CreateBlank() : scene.Frames[^1],
                blank ? -1 : scene.Frames.Count - 1,
                storyboard.ClockAboveLastFrame,
                blank,
                Duration(storyboard.LastFrameDelayMs)));
        }

        return steps;
    }

    private static TimeSpan Duration(ushort milliseconds) => TimeSpan.FromMilliseconds(milliseconds == 0 ? 100 : milliseconds);

    private sealed record PlaybackStep(DmdFrame Frame, int FrameIndex, bool ClockAbove, bool IsBlank, TimeSpan Duration);
}


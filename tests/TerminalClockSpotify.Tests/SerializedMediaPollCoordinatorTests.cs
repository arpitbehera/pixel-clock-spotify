using TerminalClockSpotify.Media;
using Xunit;

namespace TerminalClockSpotify.Tests;

public sealed class SerializedMediaPollCoordinatorTests
{
    [Fact]
    public async Task RequestsDuringActivePollCoalesceIntoOneFollowUp()
    {
        var releases = new Queue<TaskCompletionSource>();
        var active = 0;
        var maxActive = 0;
        var calls = 0;
        var coordinator = new SerializedMediaPollCoordinator(async () =>
        {
            calls++;
            active++;
            maxActive = Math.Max(maxActive, active);
            var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            releases.Enqueue(release);
            await release.Task;
            active--;
        });

        var first = coordinator.RequestAsync();
        await WaitUntilAsync(() => calls == 1);

        var second = coordinator.RequestAsync();
        var third = coordinator.RequestAsync();
        var fourth = coordinator.RequestAsync();
        releases.Dequeue().SetResult();
        await WaitUntilAsync(() => calls == 2);
        releases.Dequeue().SetResult();

        await Task.WhenAll(first, second, third, fourth);

        Assert.Equal(2, calls);
        Assert.Equal(1, maxActive);
    }

    private static async Task WaitUntilAsync(Func<bool> condition)
    {
        for (var attempt = 0; attempt < 100 && !condition(); attempt++)
            await Task.Delay(5);

        Assert.True(condition());
    }
}

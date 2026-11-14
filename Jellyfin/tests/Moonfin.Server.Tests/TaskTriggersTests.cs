using MediaBrowser.Model.Tasks;
using Moonfin.Server.Helpers;
using Xunit;

namespace Moonfin.Server.Tests;

/// <summary>
/// Jellyfin 10.11 turned TaskTriggerInfo.Type from a string into an enum, so assigning it
/// directly stopped binding and every scheduled task silently lost its startup trigger.
/// These run against the Jellyfin 12.4 enum contract.
/// </summary>
public sealed class TaskTriggersTests
{
    [Fact]
    public void StartupTriggerCarriesTheStartupTypeAndNoTimeOfDay()
    {
        var trigger = TaskTriggers.Startup();

        Assert.Equal("StartupTrigger", trigger.Type.ToString());
        Assert.Null(trigger.TimeOfDayTicks);
    }

    [Fact]
    public void DailyTriggerCarriesTheDailyTypeAndTheRequestedTime()
    {
        var trigger = TaskTriggers.Daily(TimeSpan.FromHours(5));

        Assert.Equal("DailyTrigger", trigger.Type.ToString());
        Assert.Equal(TimeSpan.FromHours(5).Ticks, trigger.TimeOfDayTicks);
    }

    [Fact]
    public void TriggerNamesMatchWhatTheServerExpects()
    {
        Assert.Equal("StartupTrigger", TaskTriggers.Startup().Type.ToString());
        Assert.Equal("DailyTrigger", TaskTriggers.Daily(TimeSpan.Zero).Type.ToString());
    }
}

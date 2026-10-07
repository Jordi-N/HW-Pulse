using HwPulse.Sensors;

namespace HwPulse.Tests;

public class ServicePickTests
{
    [Fact]
    public void DefaultsPickServerServicesSortedByLabel()
    {
        string[] installed =
        [
            "Spooler",
            "JellyfinServer",
            "actions.runner.owner-repo.build-01",
            "w3svc",
            "actions.runner.owner.org-runner",
        ];

        Assert.Equal(
            [
                new ServiceSetting("actions.runner.owner-repo.build-01", "build-01"),
                new ServiceSetting("w3svc", "IIS"),
                new ServiceSetting("JellyfinServer", "Jellyfin"),
                new ServiceSetting("actions.runner.owner.org-runner", "org-runner"),
            ],
            ServicePick.Defaults(installed));
    }

    [Fact]
    public void DefaultsWithoutKnownServicesIsEmpty()
    {
        Assert.Empty(ServicePick.Defaults(["Spooler", "WinRM"]));
    }

    [Theory]
    [InlineData(@"""C:\actions runner\bin\RunnerService.exe""", @"C:\actions runner\bin\Runner.Worker.exe", true)]
    [InlineData(@"C:\runner\bin\RunnerService.exe", @"c:\RUNNER\bin.2.320.0\Runner.Worker.exe", true)]
    [InlineData(@"C:\runner\bin\RunnerService.exe", @"C:\runner2\bin\Runner.Worker.exe", false)]
    [InlineData(null, @"C:\runner\bin\Runner.Worker.exe", false)]
    public void IsRunnerBusyMatchesWorkerInsideRunnerFolder(string? imagePath, string worker, bool busy)
    {
        Assert.Equal(busy, ServicePick.IsRunnerBusy(imagePath, [worker]));
    }

    [Theory]
    [InlineData(false, false, false, ServiceState.Missing)]
    [InlineData(true, false, false, ServiceState.Stopped)]
    [InlineData(true, true, false, ServiceState.Running)]
    [InlineData(true, true, true, ServiceState.Busy)]
    public void StateCombinesInstalledRunningAndBusy(bool installed, bool running, bool busy, ServiceState expected)
    {
        Assert.Equal(expected, ServicePick.State(installed, running, busy));
    }
}

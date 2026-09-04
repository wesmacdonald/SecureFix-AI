namespace SecureFix.Api;

using SecureFix.Core.Services;

public sealed class ConfigurationKillSwitch : IKillSwitch
{
    public ConfigurationKillSwitch(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        IsEnabled = configuration.GetValue<bool>("Policy:KillSwitchEnabled")
            || string.Equals(
                Environment.GetEnvironmentVariable("POLICY_KILL_SWITCH_ENABLED"),
                "true",
                StringComparison.OrdinalIgnoreCase);
    }

    public bool IsEnabled { get; }
}

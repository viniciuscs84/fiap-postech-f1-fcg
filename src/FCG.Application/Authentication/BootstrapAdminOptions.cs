namespace FCG.Application.Authentication;

/// <summary>Configuration for optional initial administrator creation.</summary>
public sealed class BootstrapAdminOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "BootstrapAdmin";

    /// <summary>Indicates whether bootstrap creation is enabled.</summary>
    public bool Enabled { get; init; }

    /// <summary>Administrator name.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Administrator e-mail.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Administrator initial password.</summary>
    public string Password { get; init; } = string.Empty;
}

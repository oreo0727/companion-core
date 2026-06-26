namespace Companion.Core.Constants;

public static class AgentNames
{
    public const string ChiefOfStaff = "ChiefOfStaff";
    public const string Planner = "Planner";
    public const string Research = "Research";
    public const string Coder = "Coder";
    public const string Writer = "Writer";
    public const string Travel = "Travel";
    public const string Finance = "Finance";
    public const string Health = "Health";
    public const string Home = "Home";

    public static readonly IReadOnlyList<string> All =
    [
        ChiefOfStaff,
        Planner,
        Research,
        Coder,
        Writer,
        Travel,
        Finance,
        Health,
        Home
    ];
}

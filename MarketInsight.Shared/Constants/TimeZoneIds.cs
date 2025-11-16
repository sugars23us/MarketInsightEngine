
namespace MarketInsight.Shared.Constants
{
    /// <summary>
    /// Cross-platform friendly names for time zones.
    /// On Windows, use the standard Windows IDs; on Linux containers,
    /// use IANA. The DB filters we wrote use Windows ID "Eastern Standard Time".
    /// </summary>
    public static class TimeZoneIds
    {
        // Windows time zone id for New York (handles DST automatically).
        public const string EasternWindows = "Eastern Standard Time";

        // IANA id for New York (Linux/containers).
        public const string EasternIana = "America/New_York";

        // Switzerland
        public const string ZurichWindows = "W. Europe Standard Time";
        public const string ZurichIana = "Europe/Zurich";
    }
}

using System.Collections.Generic;

namespace Game.Core
{
    public interface IAnalytics
    {
        void Track(string eventName, Dictionary<string, object> props = null);
        void Flush();
    }

    public static class AnalyticsEvents
    {
        public const string AppStart       = "app_start";
        public const string LevelStart     = "level_start";
        public const string LevelEnd       = "level_end";
        public const string KingdomNodeView     = "kingdom_node_view";
        public const string KingdomNodeComplete = "kingdom_node_complete";
        public const string BoosterUse     = "booster_use";
        public const string CrashMarker    = "crash_marker";
    }
}

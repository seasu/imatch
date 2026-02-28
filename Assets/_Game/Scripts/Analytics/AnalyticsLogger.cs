using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Game.Analytics
{
    using Game.Core;

    public class AnalyticsLogger : IAnalytics
    {
        private readonly List<string> _buffer = new();
        private readonly string _logPath;

        public AnalyticsLogger()
        {
            _logPath = Path.Combine(Application.persistentDataPath, "analytics_log.jsonl");
        }

        public void Track(string eventName, Dictionary<string, object> props = null)
        {
            var entry = new Dictionary<string, object>
            {
                ["event"] = eventName,
                ["ts"] = DateTime.UtcNow.ToString("o")
            };
            if (props != null)
                foreach (var kv in props) entry[kv.Key] = kv.Value;

            // Simple manual JSON serialisation (no Newtonsoft dependency)
            var line = DictToJson(entry);
            _buffer.Add(line);
            Debug.Log($"[Analytics] {line}");
        }

        public void Flush()
        {
            if (_buffer.Count == 0) return;
            try
            {
                File.AppendAllLines(_logPath, _buffer);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Analytics] Flush failed: {e.Message}");
            }
            _buffer.Clear();
        }

        public string GetLogPath() => _logPath;

        public IReadOnlyList<string> GetBufferedEvents() => _buffer;

        private static string DictToJson(Dictionary<string, object> d)
        {
            var parts = new List<string>();
            foreach (var kv in d)
            {
                string val = kv.Value switch
                {
                    null       => "null",
                    bool b     => b ? "true" : "false",
                    string s   => $"\"{Escape(s)}\"",
                    int i      => i.ToString(),
                    float f    => f.ToString("G"),
                    double dv  => dv.ToString("G"),
                    _          => $"\"{Escape(kv.Value.ToString())}\""
                };
                parts.Add($"\"{Escape(kv.Key)}\":{val}");
            }
            return "{" + string.Join(",", parts) + "}";
        }

        private static string Escape(string s) =>
            s.Replace("\\", "\\\\").Replace("\"", "\\\"")
             .Replace("\n", "\\n").Replace("\r", "\\r");
    }
}

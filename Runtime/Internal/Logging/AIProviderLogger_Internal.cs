using UnityEngine;

namespace Weppy.AIProvider
{
    public static partial class AIProviderLogger
    {
        private static LogLevel _currentLogLevel = LogLevel.VERBOSE;

        private static void SetLogLevelInternal(LogLevel level_)
        {
            _currentLogLevel = level_;
        }

        public static void Log(object message_)
        {
            if (_currentLogLevel >= LogLevel.INFO)
                Debug.Log($"[AIProvider] {message_}");
        }

        public static void LogWarning(object message_)
        {
            if (_currentLogLevel >= LogLevel.WARNING)
                Debug.LogWarning($"[AIProvider] {message_}");
        }

        public static void LogError(object message_)
        {
            if (_currentLogLevel >= LogLevel.ERROR)
                Debug.LogError($"[AIProvider] {message_}");
        }

        public static void LogDebug(object message_)
        {
            if (_currentLogLevel >= LogLevel.DEBUG)
                Debug.Log($"[AIProvider][Debug] {message_}");
        }

        public static void LogVerbose(object message_)
        {
            if (_currentLogLevel >= LogLevel.VERBOSE)
                Debug.Log($"[AIProvider][Verbose] {message_}");
        }
    }
}

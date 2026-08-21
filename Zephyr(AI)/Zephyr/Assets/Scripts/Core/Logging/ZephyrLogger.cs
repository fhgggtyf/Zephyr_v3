/*
 * ZephyrLogger.cs
 * ---------------
 * Module:  Core / Logging
 * Purpose: Centralized singleton logger for the entire Zephyr project. Filters log output
 *          by level (Debug/Info/Warning/Error) and routes to UnityEngine.Debug. Provides
 *          static convenience methods (Debug, Info, Warning, Error) that internally call
 *          the Log() method. Resolves the Debug naming conflict with UnityEngine.Debug
 *          via the UnityDebug alias. Lives in the Persistent scene so it is always available.
 * Dependencies: UnityEngine (for MonoBehaviour, Debug via alias).
 * Scene:    Persistent (Ch.14.2 — infrastructure manager, never unloaded).
 * Ch.Ref:   Ch.1.2 Logging Guidelines.
 */
using UnityEngine;
using UnityDebug = UnityEngine.Debug;

namespace Zephyr.Core.Logging
{
    /// <summary>
    /// Centralized logger. Filters logs by level and routes to UnityEngine.Debug.
    /// Lives in Persistent scene (Ch.14.2).
    /// </summary>
    public sealed class ZephyrLogger : MonoBehaviour
    {
        public static ZephyrLogger Instance { get; private set; }

        [SerializeField] private LogLevel _minimumLevel = LogLevel.Info;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void SetMinimumLevel(LogLevel level)
        {
            _minimumLevel = level;
        }

        public void Log(LogLevel level, string message, Object context = null)
        {
            if (level < _minimumLevel) return;

            string tag = level.ToString().ToUpper();
            string formatted = $"[{tag}] {message}";

            switch (level)
            {
                case LogLevel.Debug:
                    UnityDebug.Log(formatted, context);
                    break;
                case LogLevel.Info:
                    UnityDebug.Log(formatted, context);
                    break;
                case LogLevel.Warning:
                    UnityDebug.LogWarning(formatted, context);
                    break;
                case LogLevel.Error:
                    UnityDebug.LogError(formatted, context);
                    break;
            }
        }

        public void Debug(string message, Object context = null) => Log(LogLevel.Debug, message, context);
        public void Info(string message, Object context = null) => Log(LogLevel.Info, message, context);
        public void Warning(string message, Object context = null) => Log(LogLevel.Warning, message, context);
        public void Error(string message, Object context = null) => Log(LogLevel.Error, message, context);
    }
}

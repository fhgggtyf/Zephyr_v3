/*
 * LogLevel.cs
 * -----------
 * Module:  Core / Logging
 * Purpose: Defines the severity levels used by ZephyrLogger for log filtering.
 *          Ordered from most verbose to least: Debug → Info → Warning → Error → None.
 *          Setting the minimum level to Info suppresses Debug messages, etc.
 * Dependencies: None (pure enum).
 * Scene:    N/A (compilation-only; configured at runtime via ZephyrLogger.SetMinimumLevel).
 * Ch.Ref:   Ch.1.2 Logging Guidelines.
 */
namespace Zephyr.Core.Logging
{
    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error,
        None
    }
}

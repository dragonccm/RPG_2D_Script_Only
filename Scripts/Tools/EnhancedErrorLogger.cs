using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Enhanced Error Logger - System logging thông minh v?i categorization và filtering
/// </summary>
public static class EnhancedErrorLogger
{
    private static Dictionary<string, int> errorCounts = new Dictionary<string, int>();
    private static Dictionary<string, float> lastErrorTimes = new Dictionary<string, float>();
    private static bool enableDebugLogging = true;
    private static bool enableWarningLogging = true;
    private static bool enableErrorLogging = true;
    
    // Error throttling settings
    private const float ERROR_THROTTLE_TIME = 1f; // Don't log same error more than once per second
    private const int MAX_SAME_ERROR_COUNT = 10; // Stop logging after 10 same errors
    
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warning = 2,
        Error = 3,
        Critical = 4
    }
    
    /// <summary>
    /// Log enhanced message v?i category và throttling
    /// </summary>
    public static void Log(string message, LogLevel level = LogLevel.Info, string category = "General", Object context = null)
    {
        if (!ShouldLog(level)) return;
        
        string key = $"{category}:{message}";
        
        // Throttling check
        if (lastErrorTimes.ContainsKey(key))
        {
            if (Time.time - lastErrorTimes[key] < ERROR_THROTTLE_TIME)
                return; // Too soon, skip
        }
        
        // Count check
        if (errorCounts.ContainsKey(key))
        {
            errorCounts[key]++;
            if (errorCounts[key] > MAX_SAME_ERROR_COUNT)
            {
                if (errorCounts[key] == MAX_SAME_ERROR_COUNT + 1)
                {
                    Debug.LogWarning($"[Enhanced Logger] Suppressing further '{category}' errors (>{MAX_SAME_ERROR_COUNT})");
                }
                return; // Too many, suppress
            }
        }
        else
        {
            errorCounts[key] = 1;
        }
        
        lastErrorTimes[key] = Time.time;
        
        // Format message
        string formattedMessage = FormatMessage(message, level, category, errorCounts[key]);
        
        // Log based on level
        switch (level)
        {
            case LogLevel.Debug:
                Debug.Log(formattedMessage, context);
                break;
            case LogLevel.Info:
                Debug.Log(formattedMessage, context);
                break;
            case LogLevel.Warning:
                Debug.LogWarning(formattedMessage, context);
                break;
            case LogLevel.Error:
                Debug.LogError(formattedMessage, context);
                break;
            case LogLevel.Critical:
                Debug.LogError($"?? CRITICAL: {formattedMessage}", context);
                break;
        }
    }
    
    /// <summary>
    /// Quick log methods for convenience
    /// </summary>
    public static void LogDebug(string message, string category = "Debug", Object context = null)
        => Log(message, LogLevel.Debug, category, context);
        
    public static void LogInfo(string message, string category = "Info", Object context = null)
        => Log(message, LogLevel.Info, category, context);
        
    public static void LogWarning(string message, string category = "Warning", Object context = null)
        => Log(message, LogLevel.Warning, category, context);
        
    public static void LogError(string message, string category = "Error", Object context = null)
        => Log(message, LogLevel.Error, category, context);
        
    public static void LogCritical(string message, string category = "Critical", Object context = null)
        => Log(message, LogLevel.Critical, category, context);
    
    /// <summary>
    /// Boss-specific logging methods
    /// </summary>
    public static void LogBossError(string bossName, string message, Object context = null)
        => LogError($"[{bossName}] {message}", "Boss", context);
        
    public static void LogBossWarning(string bossName, string message, Object context = null)
        => LogWarning($"[{bossName}] {message}", "Boss", context);
        
    public static void LogBossInfo(string bossName, string message, Object context = null)
        => LogInfo($"[{bossName}] {message}", "Boss", context);
    
    /// <summary>
    /// Skill system specific logging
    /// </summary>
    public static void LogSkillError(string skillName, string message, Object context = null)
        => LogError($"[Skill: {skillName}] {message}", "Skill", context);
        
    public static void LogSkillWarning(string skillName, string message, Object context = null)
        => LogWarning($"[Skill: {skillName}] {message}", "Skill", context);
    
    /// <summary>
    /// AI specific logging
    /// </summary>
    public static void LogAIError(string aiName, string message, Object context = null)
        => LogError($"[AI: {aiName}] {message}", "AI", context);
        
    public static void LogAIWarning(string aiName, string message, Object context = null)
        => LogWarning($"[AI: {aiName}] {message}", "AI", context);
    
    private static bool ShouldLog(LogLevel level)
    {
        return level switch
        {
            LogLevel.Debug => enableDebugLogging,
            LogLevel.Info => true, // Always log info
            LogLevel.Warning => enableWarningLogging,
            LogLevel.Error => enableErrorLogging,
            LogLevel.Critical => true, // Always log critical
            _ => true
        };
    }
    
    private static string FormatMessage(string message, LogLevel level, string category, int count)
    {
        string prefix = level switch
        {
            LogLevel.Debug => "??",
            LogLevel.Info => "??",
            LogLevel.Warning => "??",
            LogLevel.Error => "?",
            LogLevel.Critical => "??",
            _ => "??"
        };
        
        string countStr = count > 1 ? $" (x{count})" : "";
        return $"{prefix} [{category}] {message}{countStr}";
    }
    
    /// <summary>
    /// Get error statistics
    /// </summary>
    public static Dictionary<string, int> GetErrorStatistics()
    {
        return new Dictionary<string, int>(errorCounts);
    }
    
    /// <summary>
    /// Clear error statistics
    /// </summary>
    public static void ClearStatistics()
    {
        errorCounts.Clear();
        lastErrorTimes.Clear();
    }
    
    /// <summary>
    /// Configure logging levels
    /// </summary>
    public static void SetLoggingEnabled(bool debug, bool warning, bool error)
    {
        enableDebugLogging = debug;
        enableWarningLogging = warning;
        enableErrorLogging = error;
    }
    
    /// <summary>
    /// Print error summary
    /// </summary>
    public static void PrintErrorSummary()
    {
        if (errorCounts.Count == 0)
        {
            Debug.Log("?? [Enhanced Logger] No errors recorded!");
            return;
        }
        
        var summary = "?? [Enhanced Logger] Error Summary:\n";
        var sortedErrors = new List<KeyValuePair<string, int>>(errorCounts);
        sortedErrors.Sort((x, y) => y.Value.CompareTo(x.Value)); // Sort by count descending
        
        foreach (var kvp in sortedErrors)
        {
            summary += $"  • {kvp.Key}: {kvp.Value} times\n";
        }
        
        Debug.Log(summary);
    }
}
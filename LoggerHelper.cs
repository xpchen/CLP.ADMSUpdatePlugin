using System;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace CLP.ADMSUpdatePlugin
{
    /// <summary>
    /// Minimal static helper for NLog.
    /// Call <c>LoggerHelper.Configure&lt;T&gt;(path)</c> once, then
    /// use <c>LoggerHelper.Info("...")</c> etc. anywhere.
    /// </summary>
    public static class LoggerHelper
    {
        private static readonly object _configLock = new();
        private static bool _isConfigured;
        private static ILogger _logger = LogManager.GetLogger("Default");

        /// <summary>
        /// One‑time configuration entry point.
        /// </summary>
        /// <typeparam name="T">The type whose name will be used as the root logger.</typeparam>
        /// <param name="logFilePath">Absolute or relative path to the log file.</param>
        /// <param name="minConsoleLevel">Minimum level for console output (default = Info).</param>
        /// <param name="minFileLevel">Minimum level for file output (default = Debug).</param>
        public static void Configure<T>(string logFilePath,
                                        string logName= null,
                                        LogLevel? minConsoleLevel = null,
                                        LogLevel? minFileLevel = null)
        {
            if (_isConfigured) return;

            lock (_configLock)
            {
                if (_isConfigured) return;

                minConsoleLevel ??= LogLevel.Info;
                minFileLevel ??= LogLevel.Debug;

                var config = new LoggingConfiguration();

                // Console target
                var consoleTarget = new ColoredConsoleTarget("console")
                {
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
                };
                config.AddTarget(consoleTarget);
                config.AddRule(minConsoleLevel, LogLevel.Fatal, consoleTarget);

                // File target
                var fileTarget = new FileTarget("logfile")
                {
                    FileName = logFilePath,
                    Layout = "${longdate}|${level:uppercase=true}|${logger}|${message} ${exception:format=tostring}"
                };
                config.AddTarget(fileTarget);
                config.AddRule(minFileLevel, LogLevel.Fatal, fileTarget);

                LogManager.Configuration = config;
                if (String.IsNullOrWhiteSpace(logName))
                {
                    // Set default logger for subsequent static calls
                    _logger = LogManager.GetLogger(typeof(T).Name);
                }
                else {
                    _logger = LogManager.GetLogger(logName);
                }
               

                _isConfigured = true;
            }
        }

        #region Static logging helpers
        public static void Trace(string message) => _logger.Trace(message);
        public static void Trace(Exception ex, string message) => _logger.Trace(ex, message);

        public static void Debug(string message) => _logger.Debug(message);
        public static void Debug(Exception ex, string message) => _logger.Debug(ex, message);

        public static void Info(string message) => _logger.Info(message);
        public static void Info(Exception ex, string message) => _logger.Info(ex, message);

        public static void Warn(string message) => _logger.Warn(message);
        public static void Warn(Exception ex, string message) => _logger.Warn(ex, message);

        public static void Error(string message) => _logger.Error(message);
        public static void Error(Exception ex, string message) => _logger.Error(ex, message);

        public static void Fatal(string message) => _logger.Fatal(message);
        public static void Fatal(Exception ex, string message) => _logger.Fatal(ex, message);
        #endregion
    }
}
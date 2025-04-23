using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASFuelControl.Common
{
    public class Logger
    {
        private static bool creatingLogEntry = false;

        /// <summary>
        /// Convert underscore strings to CamelCase
        /// </summary>
        /// <param name="name">the string to be converted</param>
        /// <returns></returns>
        private static string UnderscoreToCamelCase(string name)
        {
            if (string.IsNullOrEmpty(name) || !name.Contains("_"))
            {
                return name;
            }
            string[] array = name.Split('_');
            for (int i = 0; i < array.Length; i++)
            {
                string s = array[i];
                string first = string.Empty;
                string rest = string.Empty;
                if (s.Length > 0)
                {
                    first = Char.ToUpperInvariant(s[0]).ToString();
                }
                if (s.Length > 1)
                {
                    rest = s.Substring(1).ToLowerInvariant();
                }
                array[i] = first + rest;
            }
            string newname = string.Join(" ", array);
            if (newname.Length == 0)
            {
                newname = name;
            }
            return newname;
        }

        #region NLog

        private static Logger instance = null;

        /// <summary>
        /// Singelton definition of logger
        /// </summary>
        public static Logger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Logger();
                return instance;
            }
        }

        public static NLog.LogLevel LevelFrom { set; get; }

        public static NLog.LogLevel LevelTo { set; get; }

        private static NLog.Logger logger = null;

        /// <summary>
        /// Defines if the Logger has a Debug Log File
        /// </summary>
        public bool DebugLog { set; get; }

        /// <summary>
        /// Defines if the Logger has a Failure Log File
        /// </summary>
        public bool FailureLog { set; get; }

        /// <summary>
        /// Defines if the Logger has a Main Log File
        /// </summary>
        public bool MainLog { set; get; }

        /// <summary>
        /// Defines if the Logger has an Email sending Target
        /// </summary>
        public bool SendEmails { set; get; }

        /// <summary>
        /// The path of the logging files
        /// </summary>
        public string MainPath { set; get; }

        /// <summary>
        /// Gets the calling method if the Stack.
        /// </summary>
        /// <param name="skipStack">Levels to be skipped</param>
        /// <returns>The method name</returns>
        public static string GetMethodName(int skipStack = 1)
        {
            var stack = new System.Diagnostics.StackTrace(skipStack);
            var frame = stack.GetFrame(0);
            var method = frame.GetMethod();

            return method.ReflectedType.FullName + "." + method.Name;
        }

        /// <summary>
        /// The current NLog logger
        /// </summary>
        public NLog.Logger CurrentLogger
        {
            get
            {
                if (logger == null)
                {
                    var config = new NLog.Config.LoggingConfiguration();
                    NLog.LogManager.ThrowExceptions = true;
                    AddLogger(config, LevelFrom, LevelTo, "logfile", this.MainPath + "\\MainLog.txt", this.MainPath + "\\MainLog.[format].txt");
                    //if (SendEmails)
                    //    AddMailLogger(config, NLog.LogLevel.Info, NLog.LogLevel.Fatal, "mailLog");
                    NLog.LogManager.Configuration = config;
                    logger = NLog.LogManager.GetCurrentClassLogger();
                }
                return logger;
            }
        }

        public void Trace(string message, string url = "", int methodLevel = 2)
        {
            try
            {
                string methodName = Logger.GetMethodName(methodLevel);
                this.CurrentLogger.Trace("" + methodName + "|" + url + "|" + message);
            }
            catch (Exception ex)
            {
                
            }
        }

        public void Trace<TArgument>(string message, TArgument argument, string url = "", int methodLevel = 2)
        {
            try
            {
                string methodName = Logger.GetMethodName(methodLevel);
                this.CurrentLogger.Trace("" + methodName + "|" + url + "|" + message, argument);
            }
            catch (Exception ex)
            {
            }
        }

        public void Trace<TArgument1, TArgument2>(string message, TArgument1 argument1, TArgument2 argument2, string url = "", int methodLevel = 2)
        {
            try
            {
                string methodName = Logger.GetMethodName(methodLevel);
                this.CurrentLogger.Trace("" + methodName + "|" + url + "|" + message, argument1, argument2);
            }
            catch (Exception ex)
            {
            }
        }

        public void Trace<TArgument1, TArgument2, TArgument3>(string message, TArgument1 argument1, TArgument2 argument2, TArgument3 argument3, string url = "", int methodLevel = 2)
        {
            try
            {
                string methodName = Logger.GetMethodName(methodLevel);
                this.CurrentLogger.Trace("" + methodName + "|" + url + "|" + message, argument1, argument2, argument3);
            }
            catch (Exception ex)
            {
            }
        }

        public void Debug(string message, string url = "", int methodLevel = 2)
        {
            try
            {
                string methodName = Logger.GetMethodName(methodLevel);
                this.CurrentLogger.Debug("" + methodName + "|" + url + "|" + message);
            }
            catch (Exception ex)
            {
            }
        }

        public void Debug<TArgument>(string message, TArgument argument, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            try
            {
                this.CurrentLogger.Debug("" + methodName + "|" + url + "|" + message, argument);
            }
            catch (Exception ex)
            {
            }
        }

        public void Debug<TArgument1, TArgument2>(string message, TArgument1 argument1, TArgument2 argument2, string url = "", int methodLevel = 2)
        {
            try
            {
                string methodName = Logger.GetMethodName(methodLevel);
                this.CurrentLogger.Debug("" + methodName + "|" + url + "|" + message, argument1, argument2);
            }
            catch (Exception ex)
            {
            }
        }

        public void Debug<TArgument1, TArgument2, TArgument3>(string message, TArgument1 argument1, TArgument2 argument2, TArgument3 argument3, string url = "", int methodLevel = 2)
        {
            try
            {
                string methodName = Logger.GetMethodName(methodLevel);
                this.CurrentLogger.Debug("" + methodName + "|" + url + "|" + message, argument1, argument2, argument3);
            }
            catch (Exception ex)
            {
            }
        }

        public void Info(string message, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Info("" + methodName + "|" + url + "|" + message);
        }

        public void Info<TArgument>(string message, TArgument argument, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Info("" + methodName + "|" + url + "|" + message, argument);
        }

        public void Info(string message, object argument1, object argument2, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Info("" + methodName + "|" + url + "|" + message, argument1, argument2);
        }

        public void Warn(string message, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Warn("" + methodName + "|" + url + "|" + message);
        }

        public void Warn<TArgument>(string message, TArgument argument, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Warn("" + methodName + "|" + url + "|" + message, argument);
        }

        public void Warn<TArgument1, TArgument2>(string message, TArgument1 argument1, TArgument2 argument2, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Warn("" + methodName + "|" + url + "|" + message, argument1, argument2);
        }

        public void Warn<TArgument1, TArgument2, TArgument3>(string message, TArgument1 argument1, TArgument2 argument2, TArgument3 argument3, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Warn("" + methodName + "|" + url + "|" + message, argument1, argument2, argument3);
        }

        public void Warn(string message, object argument1, object argument2, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Warn("" + methodName + "|" + url + "|" + message, argument1, argument2);
        }

        public void Error(Exception ex, string message, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Error(ex, "" + methodName + "|" + url + "|" + message);
        }

        public void Error<TArgument>(Exception ex, string message, TArgument argument, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Error(ex, "" + methodName + "|" + url + "|" + message, argument);
        }

        public void Error(string message, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Error("" + methodName + "|" + url + "|" + message);
        }

        public void Error<TArgument>(string message, TArgument argument, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Error("" + methodName + "|" + url + "|" + message, argument);
        }

        public void Error<TArgument1, TArgument2>(string message, TArgument1 argument1, TArgument2 argument2, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Error("" + methodName + "|" + url + "|" + message, argument1, argument2);
        }

        public void Error(Exception ex, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Error("" + methodName + "|" + "{value1}, Stack Trace: {value2}", ex.Message, ex.StackTrace);
        }

        public void Fatal(string message, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Fatal("" + methodName + "|" + url + "|" + message);
        }

        public void Fatal<TArgument>(string message, TArgument argument, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Fatal("" + methodName + "|" + url + "|" + message, argument);
        }

        public void Fatal(string message, string argument1, string argument2, string url = "", int methodLevel = 2)
        {
            string methodName = Logger.GetMethodName(methodLevel);
            this.CurrentLogger.Fatal("" + methodName + "|" + url + "|" + message, argument1, argument2);
        }

        /// <summary>
        /// Initializes the logger for the first time
        /// </summary>
        /// <param name="main">Logger has Main Logging File</param>
        /// <param name="debug">Logger has Debug Logging File</param>
        /// <param name="failure">Logger has Failure Logging File</param>
        /// <param name="sendEmail">Logger has Email sender target</param>
        /// <param name="logPath">Logger's Files Path</param>
        public static void InitializeLogger(string logPath)
        {
            if (instance != null)
                return;
            instance = new Logger();
            if (logPath.Last() == '\\')
                logPath = logPath.Substring(0, logPath.Length - 1);
            instance.MainPath = logPath;

            //NLog.LogEventInfo.MessageTemplateParameters.
        }

        /// <summary>
        /// Resets the Logger after settings change
        /// </summary>
        /// <param name="main">Logger has Main Logging File</param>
        /// <param name="debug">Logger has Debug Logging File</param>
        /// <param name="failure">Logger has Failure Logging File</param>
        /// <param name="sendEmail">Logger has Email sender target</param>
        /// <param name="logPath">Logger's Files Path</param>
        public static void ResetLogger(string logPath)
        {
            logger = null;
            instance = null;
            InitializeLogger(logPath);
        }

        /// <summary>
        /// Add Logger target
        /// </summary>
        /// <param name="config">The NLog logging configuration</param>
        /// <param name="minLevel">the minimum level of logging target</param>
        /// <param name="maxLevel">the maximum level of logging target</param>
        /// <param name="name">The name of the logging target</param>
        /// <param name="fileName">The file name of the logging target</param>
        /// <param name="archiveFileName">The filename of the archives</param>
        private static void AddLogger(NLog.Config.LoggingConfiguration config, NLog.LogLevel minLevel, NLog.LogLevel maxLevel, string name, string fileName, string archiveFileName)
        {
            var logfile = new NLog.Targets.FileTarget(name) { FileName = fileName };

            var asynTarget = new NLog.Targets.Wrappers.AsyncTargetWrapper();
            asynTarget.WrappedTarget = logfile;
            asynTarget.QueueLimit = 10000;
            asynTarget.OverflowAction = NLog.Targets.Wrappers.AsyncTargetWrapperOverflowAction.Discard;

            //logfile.Layout
            logfile.ArchiveEvery = NLog.Targets.FileArchivePeriod.Day;
            logfile.ArchiveDateFormat = "yyyyMMdd_HHmm";
            logfile.ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Date;
            logfile.ConcurrentWrites = true;
            logfile.CreateDirs = true;
            logfile.Layout = "${longdate}|${level:uppercase=true}|${message}";
            logfile.ForceMutexConcurrentWrites = true;
            logfile.KeepFileOpen = true;
            logfile.LineEnding = NLog.Targets.LineEndingMode.CRLF;
            logfile.OpenFileCacheTimeout = 60;
            logfile.ArchiveFileName = archiveFileName.Replace("[format]", "{#}");

            config.AddRule(minLevel, maxLevel, asynTarget);
        }

        #endregion
    }

    public enum LogTypeEnum
    {
        Alert,
        Exception,
        Executed,
        Created,
        Updated,
        Deleted,
        Moved,
        Success,
        Info,
        NotFound,
        Failed
    }

    public class LogProperty
    {
        public string PropertyName { set; get; }
        public string PropertyValue { set; get; }
    }
}

using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace VaultSystems.Errors
{   
        /// <summary>
    /// Do not use this system its not yet implemented! it can break the game if used!
    /// </summary>
    public static class VaultErrorLogFileWriter
    {
        private static readonly string logDir = Path.Combine(Application.persistentDataPath, "VaultLogs");
        private static string sessionFilePath;
        private static readonly object fileLock = new();

        static VaultErrorLogFileWriter()
        {
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            foreach (var file in Directory.GetFiles(logDir, "VaultErrorLog_*.txt"))
            {
                if (File.GetCreationTime(file) < DateTime.Now.AddDays(-7))
                    File.Delete(file);
            }

            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            sessionFilePath = Path.Combine(logDir, $"VaultErrorLog_{timestamp}.txt");
            WriteHeader();
        }

        private static void WriteHeader()
        {
            var header = new StringBuilder();
            header.AppendLine("==========================================");
            header.AppendLine($" Vault Error Log - Session {DateTime.Now}");
            header.AppendLine("==========================================");
            header.AppendLine($"Unity Version: {Application.unityVersion}");
            header.AppendLine($"Platform: {Application.platform}");
            header.AppendLine($"Data Path: {Application.persistentDataPath}");
            header.AppendLine("==========================================\n");

            lock (fileLock)
                File.WriteAllText(sessionFilePath, header.ToString());
        }

        public static void LogError(VaultError error)
        {
            if (error == null) return;

            Func<VaultError, string> formatLog = e =>
                $"\n--- ERROR #{e.errorID} [{e.errorType}] ---\n" +
                $"Timestamp: {DateTime.Now:HH:mm:ss.fff}\n" +
                $"Message: {e.message}\n" +
                $"Context: {e.context}\n" +
                $"Unique ID: {e.uniqueId}\n" +
                $"Trace:\n{e.trace}\n" +
                "-------------------------------------------";

            lock (fileLock)
                File.AppendAllText(sessionFilePath, formatLog(error));
        }

        public static void WriteSessionFooter()
        {
            var footer = $"\n=== Session Ended {DateTime.Now} ===\n\n";
            lock (fileLock)
                File.AppendAllText(sessionFilePath, footer);
        }

        public static string GetCurrentLogPath() => sessionFilePath;
    }
}
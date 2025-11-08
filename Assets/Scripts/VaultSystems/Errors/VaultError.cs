using System;
using System.Collections.Generic;
using UnityEngine;
using VaultSystems.Data;

namespace VaultSystems.Errors
{
            /// <summary>
    /// Do not use this system its not yet implemented! it can break the game if used!
    /// </summary>
    public enum VaultErrorType
    {
        Breakpoint,
        Assertion,
        Logic,
        Runtime,
        Data,
        Critical,
        Primitive
    }

    [Serializable]
    public class VaultError
    {
        public int errorID;
        public VaultErrorType errorType;
        public string message;
        public string trace;
        public UnityEngine.Object context;
        public string uniqueId;

        // Sims-2-style metadata
        public int frameIndex;
        public int objectInstanceId;
        public string objectName;
        public string systemName;
        public int methodHash;
        public int runtimeState;
        public Dictionary<string, string> contextData = new();
        private const int MAX_CONTEXT_DATA = 16;

        public VaultError(int id, VaultErrorType type, string msg, string trc, UnityEngine.Object ctx)
        {
            errorID = id;
            errorType = type;
            message = msg;
            trace = trc;
            context = ctx;

            frameIndex = Environment.TickCount & 0xFFFF;
            objectInstanceId = ctx ? ctx.GetInstanceID() : -1;
            objectName = ctx ? ctx.name : "None";
            systemName = ctx ? ctx.GetType().Name : "Unknown";
            methodHash = (msg + trc).GetHashCode() & 0xFFFF;
            runtimeState = 0;

            uniqueId = ctx != null && ctx is Component comp && comp.GetComponent<UniqueId>() != null
                ? comp.GetComponent<UniqueId>().GetID()
                : GenerateFallbackId(ctx);
        }

        private string GenerateFallbackId(UnityEngine.Object ctx)
        {
            if (ctx == null) return Guid.NewGuid().ToString("N");
            string input = $"{ctx.name}_{ctx.GetInstanceID()}";
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
                return $"0x{BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToLower()}";
            }
        }

        public string ToFormattedTrace()
        {
            Func<Dictionary<string, string>, string> formatContext = data =>
            {
                if (data.Count == 0) return "None";
                string result = "";
                foreach (var kv in data) result += $"{kv.Key}={kv.Value}, ";
                return result.TrimEnd(',', ' ');
            };

            return $@"Frame: {frameIndex}
Stack Object id: {objectInstanceId}
Stack Object name: {objectName}
System: {systemName}
Unique ID: {uniqueId}
Node: {methodHash}
Prim state: {runtimeState}
Params: {formatContext(contextData)}
Trace:
{trace}";
        }

        public void AddContextData(string key, string value)
        {
            if (contextData.Count >= MAX_CONTEXT_DATA) return;
            contextData[key] = value;
        }
    }
}
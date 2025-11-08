using UnityEngine;
using VaultSystems.Errors;
using System;
using System.Linq;
namespace VaultSystems.Errors
{   
        /// <summary>
    /// Do not use this system its not yet implemented! it can break the game if used!
    /// </summary>
    public static class VaultBreakpoint
    {
        public static void Check(bool condition, string message, UnityEngine.Object context = null)
        {
            if (!condition) Dispatch(VaultErrorType.Breakpoint, message, context);
        }

        public static void Assert(bool condition, string message, UnityEngine.Object context = null)
        {
            if (!condition) Dispatch(VaultErrorType.Assertion, message, context);
        }

        public static void RuntimeError(string message, UnityEngine.Object context = null)
        {
            Dispatch(VaultErrorType.Runtime, message, context);
        }

        public static void LogicError(string message, UnityEngine.Object context = null)
        {
            Dispatch(VaultErrorType.Logic, message, context);
        }

        public static void DataError(string message, UnityEngine.Object context = null)
        {
            Dispatch(VaultErrorType.Data, message, context);
        }

        public static void Critical(string message, UnityEngine.Object context = null)
        {
            Dispatch(VaultErrorType.Critical, message, context);
        }

        public static void Primitive(bool condition, string message, UnityEngine.Object context = null)
        {
            if (!condition) Dispatch(VaultErrorType.Primitive, message, context);
        }

        private static void Dispatch(VaultErrorType type, string message, UnityEngine.Object context)
        {
            Action dispatch = () => VaultErrorDispatcher.Dispatch(type, message, context);
            dispatch();
        }
    }
}
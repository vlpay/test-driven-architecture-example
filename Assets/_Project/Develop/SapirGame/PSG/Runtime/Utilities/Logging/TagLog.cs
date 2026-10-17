using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace SapirGame.PSG.Runtime.Utilities.Logging
{
    /// <summary>
    /// Provides categorized logging with tag prefixes and production-build optimization.
    /// Debug logging methods are completely removed in production builds via conditional compilation.
    /// </summary>
    public sealed class TagLog
    {
        // Cached tag strings to avoid allocations during logging
        private readonly string _tag;        // Format: "[Tag] "
        private readonly string _partTag;    // Format: "[Tag:" (for additional tags)

        /// <summary>
        /// Initializes a logger with a specific tag prefix.
        /// </summary>
        /// <param name="tag">Primary category tag for all messages from this logger.</param>
        public TagLog(string tag)
        {
            if (string.IsNullOrEmpty(tag)) {
                _tag = string.Empty;
                _partTag = "[";  // Base for additional tags
            }
            else {
                _tag = '[' + tag + "] ";
                _partTag = '[' + tag + ':';  // Prepared for concatenation
            }
        }

        // Debug-level logging - completely removed in production
#if SAPIRGAME_PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        [HideInCallstack]
        public void D(string msg)
        {
            Debug.unityLogger.Log(LogType.Log, _tag, msg);
        }

#if SAPIRGAME_PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        [HideInCallstack]
        public void D(string additionalTag, string msg)
        {
            Debug.unityLogger.Log(LogType.Log, GetFullTag(additionalTag), msg);
        }

        // Warning-level logging - always available
        [HideInCallstack]
        public void W(string msg)
        {
            Debug.unityLogger.Log(LogType.Warning, _tag, msg);
        }

        [HideInCallstack]
        public void W(string additionalTag, string msg)
        {
            Debug.unityLogger.Log(LogType.Warning, GetFullTag(additionalTag), msg);
        }

        // Error-level logging - always available
        [HideInCallstack]
        public void E(string msg)
        {
            Debug.unityLogger.Log(LogType.Error, _tag, msg);
        }

        [HideInCallstack]
        public void E(string additionalTag, string msg)
        {
            Debug.unityLogger.Log(LogType.Error, GetFullTag(additionalTag), msg);
        }

        // Exception logging with stack trace
        [HideInCallstack]
        public void E(Exception e)
        {
            Debug.unityLogger.Log(LogType.Exception, _tag, e.ToString());
        }

        [HideInCallstack]
        public void E(string additionalTag, Exception e)
        {
            Debug.unityLogger.Log(LogType.Exception, GetFullTag(additionalTag), e.ToString());
        }

        // Utility methods to throw exceptions with tagged messages
        [HideInCallstack]
        public void ThrowException(string msg)
        {
            throw new Exception(_tag + msg);
        }

        [HideInCallstack]
        public void ThrowException(string additionalTag, string msg)
        {
            throw new Exception(GetFullTag(additionalTag) + msg);
        }

        /// <summary>
        /// Constructs a full tag string by combining primary and additional tags.
        /// Optimized to avoid string concatenation at log time.
        /// </summary>
        private string GetFullTag(string additionalTag) => _partTag + additionalTag + "] ";
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace SapirGame.PSG.Runtime.Utilities.Logging
{
    /// <summary>
    /// Implements an object pool for LogPacker instances to reduce GC allocations
    /// during frequent logging operations. Thread-safe for single-threaded Unity usage.
    /// </summary>
    /// <remarks>
    /// Pattern: Object Pool pattern with fixed capacity. Prevents StringBuilder
    /// allocations during intensive logging by reusing instances.
    /// </remarks>
    public sealed class BuilderLogPool
    {
        // Stack provides LIFO behavior which improves cache locality
        private readonly Stack<LogPacker> _buildersPool = new();
        private readonly TagLog _log;
        private readonly int _buildersCount;

        /// <summary>
        /// Initializes the pool with pre-allocated LogPacker instances.
        /// </summary>
        /// <param name="log">The logger to which messages will be output.</param>
        /// <param name="buildersCount">Fixed pool size. Determines maximum concurrent usage.</param>
        public BuilderLogPool(TagLog log, int buildersCount)
        {
            _log = log;
            _buildersCount = buildersCount;

            // Pre-warm the pool to avoid runtime allocations
            for (var i = 0; i < buildersCount; i++)
                _buildersPool.Push(new LogPacker());
        }

        /// <summary>
        /// Retrieves a LogPacker from the pool for building log messages.
        /// The instance is cleared before returning for immediate reuse.
        /// </summary>
        /// <returns>A cleared LogPacker ready for message construction.</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when pool is exhausted. Indicates buildersCount was set too low.
        /// </exception>
        public LogPacker Rent()
        {
            // TryPeek is used instead of Pop to keep pool size consistent
            // and detect over-usage through the same instance
            if (_buildersPool.TryPeek(out LogPacker sb)) {
                sb.Clear();
                return sb;
            }

            throw new ArgumentOutOfRangeException(nameof(_buildersCount),
                "You are trying to Peek more string builder then you define");
        }

        /// <summary>
        /// Returns a LogPacker to the pool and outputs its content as a log message.
        /// </summary>
        /// <param name="sb">The LogPacker instance to return.</param>
        /// <param name="additionalTag">Optional secondary tag for categorized logging.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when attempting to return more instances than pool capacity.
        /// </exception>
        public void ReturnAndLog(LogPacker sb, string additionalTag = null)
        {
            // Safety check: prevent returning foreign instances
            if (_buildersPool.Count + 1 < _buildersCount) {
                throw new ArgumentOutOfRangeException(nameof(_buildersCount),
                    "You are trying to Return more string builder then you define");
            }

            // Output the built message
            if (string.IsNullOrEmpty(additionalTag))
                _log.D(sb.ToString());
            else
                _log.D(additionalTag, sb.ToString());

            // Reset and return to pool
            sb.Clear();
            _buildersPool.Push(sb);
        }
    }

    /// <summary>
    /// Wrapper around StringBuilder with conditional compilation for production builds.
    /// All Append* methods are stripped in SAPIRGAME_PROD to eliminate log formatting overhead.
    /// </summary>
    public sealed class LogPacker
    {
        private readonly StringBuilder _sb = new();

        // Conditional compilation: In production builds, all Append methods become no-ops
        // This eliminates string formatting overhead while keeping the same API
#if SAPIRGAME_PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        public void AppendFormat(string format, object arg0)
        {
            _sb.AppendFormat(format, arg0);
        }

#if SAPIRGAME_PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        public void AppendFormat(string format, object arg0, object arg1)
        {
            _sb.AppendFormat(format, arg0, arg1);
        }

#if SAPIRGAME_PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        public void AppendFormat(string format,
            object arg0,
            object arg1,
            object arg2)
        {
            _sb.AppendFormat(format, arg0, arg1, arg2);
        }

#if SAPIRGAME_PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        public void AppendFormat(string format, params object[] args)
        {
            _sb.AppendFormat(format, args);
        }

#if SAPIRGAME_PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        public void AppendLine()
        {
            _sb.AppendLine();
        }

#if SAPIRGAME_PROD
        [System.Diagnostics.Conditional("DUMMY_UNUSED_DEFINE")]
#endif
        public void AppendLine(string value)
        {
            _sb.AppendLine(value);
        }

        /// <summary>
        /// Clears the internal buffer. Always available for pool reuse.
        /// </summary>
        public void Clear()
        {
            _sb.Clear();
        }

        // Expose useful StringBuilder properties for advanced scenarios
        public int EnsureCapacity(int capacity) => _sb.EnsureCapacity(capacity);
        public bool Equals(LogPacker sb) => _sb.Equals(sb._sb);
        public override string ToString() => _sb.ToString();

        // Property accessors for StringBuilder internals
        public int Capacity
        {
            get => _sb.Capacity;
            set => _sb.Capacity = value;
        }

        public char this[int index]
        {
            get => _sb[index];
            set => _sb[index] = value;
        }

        public int Length
        {
            get => _sb.Length;
            set => _sb.Length = value;
        }

        public int MaxCapacity => _sb.MaxCapacity;
    }
}

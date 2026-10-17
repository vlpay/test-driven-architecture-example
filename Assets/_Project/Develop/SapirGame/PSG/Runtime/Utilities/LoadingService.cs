using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using SapirGame.PSG.Runtime.Utilities.Logging;
using Cysharp.Threading.Tasks;

namespace SapirGame.PSG.Runtime.Utilities
{
    /// <summary>
    /// Represents a load unit that requires disposal after use.
    /// </summary>
    public interface IDisposableLoadUnit : ILoadUnit, IDisposable { }

    /// <summary>
    /// Represents a unit of work that can be loaded asynchronously.
    /// </summary>
    public interface ILoadUnit
    {
        /// <summary>
        /// Performs the loading operation asynchronously.
        /// </summary>
        UniTask Load();
    }

    /// <summary>
    /// Represents a parameterized load unit that can be loaded asynchronously.
    /// </summary>
    /// <typeparam name="T">Type of the loading parameter.</typeparam>
    public interface ILoadUnit<in T>
    {
        /// <summary>
        /// Performs the loading operation with the specified parameter.
        /// </summary>
        /// <param name="param">Parameter required for loading.</param>
        UniTask Load(T param);
    }

    /// <summary>
    /// Represents a parameterized load unit that requires disposal after use.
    /// </summary>
    /// <typeparam name="T">Type of the loading parameter.</typeparam>
    public interface IDisposableLoadUnit<in T> : ILoadUnit<T>, IDisposable { }

    /// <summary>
    /// Service for managing asynchronous loading operations with timing, logging, and resource disposal.
    /// Provides thread synchronization to main thread upon completion when needed.
    /// </summary>
    public sealed class LoadingService
    {
        // Performance monitoring is disabled in production to avoid overhead
        private readonly Stopwatch _watch =
#if !SAPIRGAME_PROD
            new();
#else
            null;
#endif

        /// <summary>
        /// Collection of disposable resources that will be cleaned up when the service is disposed.
        /// </summary>
        public readonly List<IDisposable> Disposables = new();

        /// <summary>
        /// Initiates timing and logging when a loading operation begins.
        /// </summary>
        /// <param name="unit">The load unit being processed.</param>
        private void OnLoadingBegin(object unit)
        {
            _watch?.Restart();
            Log.Loading.D($"{unit.GetType().Name} loading is started");
        }

        /// <summary>
        /// Finalizes timing, logs results, and ensures execution returns to main thread if needed.
        /// Thread switching is required for Unity API calls that are main-thread only.
        /// </summary>
        /// <param name="unit">The load unit that was processed.</param>
        /// <param name="isError">Indicates whether the operation failed.</param>
        private async UniTask OnLoadingFinish(object unit, bool isError)
        {
            _watch?.Stop();
            Log.Loading.D($"{unit.GetType().Name} is {(isError ? "NOT " : "")}loaded with time {_watch?.ElapsedMilliseconds}ms");

            int currentThreadId = Thread.CurrentThread.ManagedThreadId;
            int mainThreadId = PlayerLoopHelper.MainThreadId;

            // Switch to main thread if we're not already there (required for Unity operations)
            if (mainThreadId != currentThreadId) {
                _watch?.Restart();
                Log.Loading.D("THREADING",$"start switching from '{currentThreadId}' thread to main thread '{mainThreadId}'");
                await UniTask.SwitchToMainThread();
                _watch?.Stop();
                Log.Loading.D("THREADING",$"switch finished with time {_watch?.ElapsedMilliseconds}");
            }
        }

        /// <summary>
        /// Executes a single loading unit with exception handling and performance tracking.
        /// </summary>
        /// <param name="loadUnit">The unit to load.</param>
        /// <param name="skipExceptionThrow">When true, suppresses exception propagation.</param>
        public async UniTask BeginLoading(ILoadUnit loadUnit, bool skipExceptionThrow = false)
        {
            var isError = true;

            try {
                OnLoadingBegin(loadUnit);
                await loadUnit.Load();
                isError = false;
            }
            catch (Exception e) {
                Log.Loading.E(e);

                if (!skipExceptionThrow)
                    throw;
            }
            finally {
                await OnLoadingFinish(loadUnit, isError);
            }
        }

        /// <summary>
        /// Executes a disposable loading unit and registers it for cleanup.
        /// </summary>
        /// <param name="unit">The disposable unit to load.</param>
        /// <param name="skipExceptionThrow">When true, suppresses exception propagation.</param>
        public async UniTask BeginLoading(IDisposableLoadUnit unit, bool skipExceptionThrow = false)
        {
            Disposables.Add(unit);
            await BeginLoading((ILoadUnit)unit, skipExceptionThrow);
        }

        /// <summary>
        /// Executes a parameterized loading unit with exception handling.
        /// </summary>
        /// <typeparam name="T">Type of the loading parameter.</typeparam>
        /// <param name="loadUnit">The parameterized unit to load.</param>
        /// <param name="param">Parameter to pass to the load operation.</param>
        /// <param name="skipExceptionThrow">When true, suppresses exception propagation.</param>
        public async UniTask BeginLoading<T>(ILoadUnit<T> loadUnit, T param, bool skipExceptionThrow = false)
        {
            var isError = true;

            try {
                OnLoadingBegin(loadUnit);
                await loadUnit.Load(param);
                isError = false;
            }
            catch (Exception e) {
                Log.Loading.E(e);

                if (!skipExceptionThrow)
                    throw;
            }
            finally {
                await OnLoadingFinish(loadUnit, isError);
            }
        }

        /// <summary>
        /// Executes a parameterized disposable loading unit and registers it for cleanup.
        /// </summary>
        /// <typeparam name="T">Type of the loading parameter.</typeparam>
        /// <param name="unit">The disposable parameterized unit to load.</param>
        /// <param name="param">Parameter to pass to the load operation.</param>
        /// <param name="skipExceptionThrow">When true, suppresses exception propagation.</param>
        public async UniTask BeginLoading<T>(IDisposableLoadUnit<T> unit, T param, bool skipExceptionThrow = false)
        {
            Disposables.Add(unit);
            await BeginLoading((ILoadUnit<T>)unit, param, skipExceptionThrow);
        }

        /// <summary>
        /// Executes multiple loading units sequentially.
        /// </summary>
        /// <param name="skipExceptionThrow">When true, suppresses exception propagation for all units.</param>
        /// <param name="units">Array of units to load in order.</param>
        public async UniTask BeginLoading(bool skipExceptionThrow = false, params ILoadUnit[] units)
        {
            foreach (ILoadUnit loadUnit in units)
                await BeginLoading(loadUnit, skipExceptionThrow);
        }

        /// <summary>
        /// Executes multiple loading units in parallel with aggregated timing.
        /// Useful for independent I/O operations to reduce total loading time.
        /// </summary>
        /// <param name="logName">Identifier used for logging the parallel operation.</param>
        /// <param name="skipExceptionThrow">When true, suppresses exception propagation.</param>
        /// <param name="units">Array of units to load concurrently.</param>
        public async UniTask BeginLoadingParallel(string logName, bool skipExceptionThrow = false, params ILoadUnit[] units)
        {
            var isError = true;

            try {
                OnLoadingBegin(logName);
                var t = UniTask.WhenAll(units.Select(e => e.Load()));
                await t;
                isError = false;
            }
            catch (Exception e) {
                Log.Loading.E(e);

                if (!skipExceptionThrow)
                    throw;
            }
            finally {
                await OnLoadingFinish(logName, isError);
            }
        }
    }
}

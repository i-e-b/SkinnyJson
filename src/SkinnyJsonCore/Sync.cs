using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace SkinnyJson
{
    /// <summary>
    /// Helper class to properly wait for async tasks
    /// </summary>
    internal static class Sync  
    {
        private static readonly TaskFactory _taskFactory = new(CancellationToken.None,
                TaskCreationOptions.None,
                TaskContinuationOptions.None,
                TaskScheduler.Default);

        /// <summary>
        /// Run an async function synchronously and return the result
        /// </summary>
        public static TResult Run<TResult>([InstantHandle]Func<Task<TResult>>? func)
        {
            if (_taskFactory is null) throw new Exception("Static init failed");
            if (func is null) throw new ArgumentNullException(nameof(func));

            var rawTask = _taskFactory.StartNew(func).Unwrap();
            if (rawTask is null) throw new Exception("Invalid task");

            return rawTask.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Run an async function synchronously
        /// </summary>
        public static void Run([InstantHandle]Func<Task> func)
        {
            if (_taskFactory is null) throw new Exception("Static init failed");
            if (func is null) throw new ArgumentNullException(nameof(func));

            var rawTask = _taskFactory.StartNew(func).Unwrap();
            if (rawTask is null) throw new Exception("Invalid task");

            rawTask.GetAwaiter().GetResult();
        }
    }
}
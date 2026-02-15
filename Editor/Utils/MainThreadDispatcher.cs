using System;
using System.Threading;
using UnityEditor;

namespace Weppy.AIProvider.Chat.Editor
{
    /// <summary>
    /// Utility class for dispatching actions to Unity's main thread.
    /// Useful for ensuring thread-safe operations when working with Unity APIs.
    /// </summary>
    public static class MainThreadDispatcher
    {
        private static int _mainThreadId;
        private static bool _initialized;

        /// <summary>
        /// Initializes the main thread dispatcher with the current thread ID.
        /// This should be called from the main thread during initialization.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            _mainThreadId = Thread.CurrentThread.ManagedThreadId;
            _initialized = true;
        }

        /// <summary>
        /// Checks if the current thread is the Unity main thread.
        /// </summary>
        /// <returns>True if on main thread, false otherwise</returns>
        public static bool IsMainThread()
        {
            if (!_initialized)
            {
                Initialize();
            }

            return Thread.CurrentThread.ManagedThreadId == _mainThreadId;
        }

        /// <summary>
        /// Executes an action on the main thread.
        /// If already on the main thread, executes immediately.
        /// Otherwise, schedules execution via EditorApplication.delayCall.
        /// </summary>
        /// <param name="action_">The action to execute</param>
        public static void Dispatch(Action action_)
        {
            if (action_ == null)
                return;

            if (IsMainThread())
            {
                action_();
            }
            else
            {
                EditorApplication.delayCall += () => action_();
            }
        }
    }
}

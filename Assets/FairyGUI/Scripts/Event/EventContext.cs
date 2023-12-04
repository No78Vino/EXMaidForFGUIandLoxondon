using System.Collections.Generic;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class EventContext
    {
        private static readonly Stack<EventContext> pool = new();

        internal bool _defaultPrevented;
        internal bool _stopsPropagation;
        internal bool _touchCapture;

        internal List<EventBridge> callChain = new();

        /// <summary>
        /// </summary>
        public object data;

        /// <summary>
        /// </summary>
        public string type;

        /// <summary>
        /// </summary>
        public EventDispatcher sender { get; internal set; }

        /// <summary>
        ///     /
        /// </summary>
        public object initiator { get; internal set; }

        /// <summary>
        ///     /
        /// </summary>
        public InputEvent inputEvent { get; internal set; }

        /// <summary>
        /// </summary>
        public bool isDefaultPrevented => _defaultPrevented;

        /// <summary>
        /// </summary>
        public void StopPropagation()
        {
            _stopsPropagation = true;
        }

        /// <summary>
        /// </summary>
        public void PreventDefault()
        {
            _defaultPrevented = true;
        }

        /// <summary>
        /// </summary>
        public void CaptureTouch()
        {
            _touchCapture = true;
        }

        internal static EventContext Get()
        {
            if (pool.Count > 0)
            {
                var context = pool.Pop();
                context._stopsPropagation = false;
                context._defaultPrevented = false;
                context._touchCapture = false;
                return context;
            }

            return new EventContext();
        }

        internal static void Return(EventContext value)
        {
            pool.Push(value);
        }
    }
}
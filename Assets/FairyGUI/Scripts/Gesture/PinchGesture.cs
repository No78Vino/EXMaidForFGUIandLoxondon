using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    ///     两个指头捏或者放的手势。
    /// </summary>
    public class PinchGesture : EventDispatcher
    {
        private float _lastScale;

        private float _startDistance;
        private bool _started;
        private bool _touchBegan;
        private readonly int[] _touches;

        /// <summary>
        ///     从上次通知后的改变量。
        /// </summary>
        public float delta;

        /// <summary>
        ///     总共缩放的量。
        /// </summary>
        public float scale;

        public PinchGesture(GObject host)
        {
            this.host = host;
            Enable(true);

            _touches = new int[2];

            onBegin = new EventListener(this, "onPinchBegin");
            onEnd = new EventListener(this, "onPinchEnd");
            onAction = new EventListener(this, "onPinchAction");
        }

        /// <summary>
        /// </summary>
        public GObject host { get; private set; }

        /// <summary>
        ///     当两个手指开始呈捏手势时派发该事件。
        /// </summary>
        public EventListener onBegin { get; }

        /// <summary>
        ///     当其中一个手指离开屏幕时派发该事件。
        /// </summary>
        public EventListener onEnd { get; }

        /// <summary>
        ///     当手势动作时派发该事件。
        /// </summary>
        public EventListener onAction { get; }

        public void Dispose()
        {
            Enable(false);
            host = null;
        }

        public void Enable(bool value)
        {
            if (value)
            {
                if (host == GRoot.inst)
                {
                    Stage.inst.onTouchBegin.Add(__touchBegin);
                    Stage.inst.onTouchMove.Add(__touchMove);
                    Stage.inst.onTouchEnd.Add(__touchEnd);
                }
                else
                {
                    host.onTouchBegin.Add(__touchBegin);
                    host.onTouchMove.Add(__touchMove);
                    host.onTouchEnd.Add(__touchEnd);
                }
            }
            else
            {
                _started = false;
                _touchBegan = false;
                if (host == GRoot.inst)
                {
                    Stage.inst.onTouchBegin.Remove(__touchBegin);
                    Stage.inst.onTouchMove.Remove(__touchMove);
                    Stage.inst.onTouchEnd.Remove(__touchEnd);
                }
                else
                {
                    host.onTouchBegin.Remove(__touchBegin);
                    host.onTouchMove.Remove(__touchMove);
                    host.onTouchEnd.Remove(__touchEnd);
                }
            }
        }

        private void __touchBegin(EventContext context)
        {
            if (Stage.inst.touchCount == 2)
                if (!_started && !_touchBegan)
                {
                    _touchBegan = true;
                    Stage.inst.GetAllTouch(_touches);
                    var pt1 = host.GlobalToLocal(Stage.inst.GetTouchPosition(_touches[0]));
                    var pt2 = host.GlobalToLocal(Stage.inst.GetTouchPosition(_touches[1]));
                    _startDistance = Vector2.Distance(pt1, pt2);

                    context.CaptureTouch();
                }
        }

        private void __touchMove(EventContext context)
        {
            if (!_touchBegan || Stage.inst.touchCount != 2)
                return;

            var evt = context.inputEvent;
            var pt1 = host.GlobalToLocal(Stage.inst.GetTouchPosition(_touches[0]));
            var pt2 = host.GlobalToLocal(Stage.inst.GetTouchPosition(_touches[1]));
            var dist = Vector2.Distance(pt1, pt2);

            if (!_started && Mathf.Abs(dist - _startDistance) > UIConfig.touchDragSensitivity)
            {
                _started = true;
                scale = 1;
                _lastScale = 1;

                onBegin.Call(evt);
            }

            if (_started)
            {
                var ss = dist / _startDistance;
                delta = ss - _lastScale;
                _lastScale = ss;
                scale += delta;
                onAction.Call(evt);
            }
        }

        private void __touchEnd(EventContext context)
        {
            _touchBegan = false;
            if (_started)
            {
                _started = false;
                onEnd.Call(context.inputEvent);
            }
        }
    }
}
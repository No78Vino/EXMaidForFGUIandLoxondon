using System;
using UnityEngine;
using Random = UnityEngine.Random;
#if FAIRYGUI_TOLUA
using LuaInterface;
#endif

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public delegate void GTweenCallback();

    /// <summary>
    /// </summary>
    /// <param name="tweener"></param>
    public delegate void GTweenCallback1(GTweener tweener);

    /// <summary>
    /// </summary>
    public interface ITweenListener
    {
        /// <summary>
        /// </summary>
        /// <param name="tweener"></param>
        void OnTweenStart(GTweener tweener);

        /// <summary>
        /// </summary>
        /// <param name="tweener"></param>
        void OnTweenUpdate(GTweener tweener);

        /// <summary>
        /// </summary>
        /// <param name="tweener"></param>
        void OnTweenComplete(GTweener tweener);
    }

    /// <summary>
    /// </summary>
    public class GTweener
    {
        private float _breakpoint;
        private CustomEase _customEase;

        private float _easeOvershootOrAmplitude;
        private float _easePeriod;
        private EaseType _easeType;
        private float _elapsedTime;
        private int _ended;
        private bool _ignoreEngineTimeScale;
        internal bool _killed;
        private ITweenListener _listener;
        private GTweenCallback _onComplete;
        private GTweenCallback1 _onComplete1;
        private GTweenCallback _onStart;
        private GTweenCallback1 _onStart1;

        private GTweenCallback _onUpdate;
        private GTweenCallback1 _onUpdate1;
        private GPath _path;
        internal bool _paused;
        internal TweenPropType _propType;
        private int _smoothStart;
        private bool _snapping;

        private bool _started;

        internal object _target;
        private float _timeScale;
        private int _valueSize;
        private bool _yoyo;

        public GTweener()
        {
            startValue = new TweenValue();
            endValue = new TweenValue();
            value = new TweenValue();
            deltaValue = new TweenValue();
        }

        /// <summary>
        /// </summary>
        public float delay { get; private set; }

        /// <summary>
        /// </summary>
        public float duration { get; private set; }

        /// <summary>
        /// </summary>
        public int repeat { get; private set; }

        /// <summary>
        /// </summary>
        public object target => _target;

        /// <summary>
        /// </summary>
        public object userData { get; private set; }

        /// <summary>
        /// </summary>
        public TweenValue startValue { get; }

        /// <summary>
        /// </summary>
        public TweenValue endValue { get; }

        /// <summary>
        /// </summary>
        public TweenValue value { get; }

        /// <summary>
        /// </summary>
        public TweenValue deltaValue { get; }

        /// <summary>
        /// </summary>
        public float normalizedTime { get; private set; }

        /// <summary>
        /// </summary>
        public bool completed => _ended != 0;

        /// <summary>
        /// </summary>
        public bool allCompleted => _ended == 1;

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetDelay(float value)
        {
            delay = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetDuration(float value)
        {
            duration = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetBreakpoint(float value)
        {
            _breakpoint = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetEase(EaseType value)
        {
            _easeType = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="customEase"></param>
        /// <returns></returns>
        public GTweener SetEase(EaseType value, CustomEase customEase)
        {
            _easeType = value;
            _customEase = customEase;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetEasePeriod(float value)
        {
            _easePeriod = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetEaseOvershootOrAmplitude(float value)
        {
            _easeOvershootOrAmplitude = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="times"></param>
        /// <param name="yoyo"></param>
        /// <returns></returns>
        public GTweener SetRepeat(int times, bool yoyo = false)
        {
            repeat = times;
            _yoyo = yoyo;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetTimeScale(float value)
        {
            _timeScale = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetIgnoreEngineTimeScale(bool value)
        {
            _ignoreEngineTimeScale = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetSnapping(bool value)
        {
            _snapping = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetPath(GPath value)
        {
            _path = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetTarget(object value)
        {
            _target = value;
            _propType = TweenPropType.None;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="propType"></param>
        /// <returns></returns>
        public GTweener SetTarget(object value, TweenPropType propType)
        {
            _target = value;
            _propType = propType;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetUserData(object value)
        {
            userData = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
#if FAIRYGUI_TOLUA
        [NoToLua]
#endif
        public GTweener OnUpdate(GTweenCallback callback)
        {
            _onUpdate = callback;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
#if FAIRYGUI_TOLUA
        [NoToLua]
#endif
        public GTweener OnStart(GTweenCallback callback)
        {
            _onStart = callback;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
#if FAIRYGUI_TOLUA
        [NoToLua]
#endif
        public GTweener OnComplete(GTweenCallback callback)
        {
            _onComplete = callback;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public GTweener OnUpdate(GTweenCallback1 callback)
        {
            _onUpdate1 = callback;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public GTweener OnStart(GTweenCallback1 callback)
        {
            _onStart1 = callback;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        public GTweener OnComplete(GTweenCallback1 callback)
        {
            _onComplete1 = callback;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public GTweener SetListener(ITweenListener value)
        {
            _listener = value;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="paused"></param>
        /// <returns></returns>
        public GTweener SetPaused(bool paused)
        {
            _paused = paused;
            if (_paused)
                _smoothStart = 0;
            return this;
        }

        /// <summary>
        /// </summary>
        /// <param name="time"></param>
        public void Seek(float time)
        {
            if (_killed)
                return;

            _elapsedTime = time;
            if (_elapsedTime < delay)
            {
                if (_started)
                    _elapsedTime = delay;
                else
                    return;
            }

            Update();
        }

        /// <summary>
        /// </summary>
        /// <param name="complete"></param>
        public void Kill(bool complete = false)
        {
            if (_killed)
                return;

            if (complete)
            {
                if (_ended == 0)
                {
                    if (_breakpoint >= 0)
                        _elapsedTime = delay + _breakpoint;
                    else if (repeat >= 0)
                        _elapsedTime = delay + duration * (repeat + 1);
                    else
                        _elapsedTime = delay + duration * 2;
                    Update();
                }

                CallCompleteCallback();
            }

            _killed = true;
        }

        internal GTweener _To(float start, float end, float duration)
        {
            _valueSize = 1;
            startValue.x = start;
            endValue.x = end;
            value.x = start;
            this.duration = duration;
            return this;
        }

        internal GTweener _To(Vector2 start, Vector2 end, float duration)
        {
            _valueSize = 2;
            startValue.vec2 = start;
            endValue.vec2 = end;
            value.vec2 = start;
            this.duration = duration;
            return this;
        }

        internal GTweener _To(Vector3 start, Vector3 end, float duration)
        {
            _valueSize = 3;
            startValue.vec3 = start;
            endValue.vec3 = end;
            value.vec3 = start;
            this.duration = duration;
            return this;
        }

        internal GTweener _To(Vector4 start, Vector4 end, float duration)
        {
            _valueSize = 4;
            startValue.vec4 = start;
            endValue.vec4 = end;
            value.vec4 = start;
            this.duration = duration;
            return this;
        }

        internal GTweener _To(Color start, Color end, float duration)
        {
            _valueSize = 4;
            startValue.color = start;
            endValue.color = end;
            value.color = start;
            this.duration = duration;
            return this;
        }

        internal GTweener _To(double start, double end, float duration)
        {
            _valueSize = 5;
            startValue.d = start;
            endValue.d = end;
            value.d = start;
            this.duration = duration;
            return this;
        }

        internal GTweener _Shake(Vector3 start, float amplitude, float duration)
        {
            _valueSize = 6;
            startValue.vec3 = start;
            startValue.w = amplitude;
            this.duration = duration;
            _easeType = EaseType.Linear;
            return this;
        }

        internal void _Init()
        {
            delay = 0;
            duration = 0;
            _breakpoint = -1;
            _easeType = EaseType.QuadOut;
            _timeScale = 1;
            _ignoreEngineTimeScale = false;
            _easePeriod = 0;
            _easeOvershootOrAmplitude = 1.70158f;
            _snapping = false;
            repeat = 0;
            _yoyo = false;
            _valueSize = 0;
            _started = false;
            _paused = false;
            _killed = false;
            _elapsedTime = 0;
            normalizedTime = 0;
            _ended = 0;
            _path = null;
            _customEase = null;
            _smoothStart = Time.frameCount == 1 ? 3 : 1; //刚启动时会有多帧的超时
        }

        internal void _Reset()
        {
            _target = null;
            _listener = null;
            userData = null;
            _onStart = _onUpdate = _onComplete = null;
            _onStart1 = _onUpdate1 = _onComplete1 = null;
        }

        internal void _Update()
        {
            if (_ended != 0) //Maybe completed by seek
            {
                CallCompleteCallback();
                _killed = true;
                return;
            }

            float dt;
            if (_smoothStart > 0)
            {
                _smoothStart--;
                dt = Mathf.Clamp(Time.unscaledDeltaTime, 0,
                    Application.targetFrameRate > 0 ? 1.0f / Application.targetFrameRate : 0.016f);
                if (!_ignoreEngineTimeScale)
                    dt *= Time.timeScale;
            }
            else if (_ignoreEngineTimeScale)
            {
                dt = Time.unscaledDeltaTime;
            }
            else
            {
                dt = Time.deltaTime;
            }

            if (_timeScale != 1)
                dt *= _timeScale;
            if (dt == 0)
                return;

            _elapsedTime += dt;
            Update();

            if (_ended != 0)
                if (!_killed)
                {
                    CallCompleteCallback();
                    _killed = true;
                }
        }

        private void Update()
        {
            _ended = 0;

            if (_valueSize == 0) //DelayedCall
            {
                if (_elapsedTime >= delay + duration)
                    _ended = 1;

                return;
            }

            if (!_started)
            {
                if (_elapsedTime < delay)
                    return;

                _started = true;
                CallStartCallback();
                if (_killed)
                    return;
            }

            var reversed = false;
            var tt = _elapsedTime - delay;
            if (_breakpoint >= 0 && tt >= _breakpoint)
            {
                tt = _breakpoint;
                _ended = 2;
            }

            if (repeat != 0)
            {
                var round = Mathf.FloorToInt(tt / duration);
                tt -= duration * round;
                if (_yoyo)
                    reversed = round % 2 == 1;

                if (repeat > 0 && repeat - round < 0)
                {
                    if (_yoyo)
                        reversed = repeat % 2 == 1;
                    tt = duration;
                    _ended = 1;
                }
            }
            else if (tt >= duration)
            {
                tt = duration;
                _ended = 1;
            }

            normalizedTime = EaseManager.Evaluate(_easeType, reversed ? duration - tt : tt, duration,
                _easeOvershootOrAmplitude, _easePeriod, _customEase);

            value.SetZero();
            deltaValue.SetZero();

            if (_valueSize == 5)
            {
                var d = startValue.d + (endValue.d - startValue.d) * normalizedTime;
                if (_snapping)
                    d = Math.Round(d);
                deltaValue.d = d - value.d;
                value.d = d;
                value.x = (float)d;
            }
            else if (_valueSize == 6)
            {
                if (_ended == 0)
                {
                    var r = Random.insideUnitSphere;
                    r.x = r.x > 0 ? 1 : -1;
                    r.y = r.y > 0 ? 1 : -1;
                    r.z = r.z > 0 ? 1 : -1;
                    r *= startValue.w * (1 - normalizedTime);

                    deltaValue.vec3 = r;
                    value.vec3 = startValue.vec3 + r;
                }
                else
                {
                    value.vec3 = startValue.vec3;
                }
            }
            else if (_path != null)
            {
                var vec3 = _path.GetPointAt(normalizedTime);
                if (_snapping)
                {
                    vec3.x = Mathf.Round(vec3.x);
                    vec3.y = Mathf.Round(vec3.y);
                    vec3.z = Mathf.Round(vec3.z);
                }

                deltaValue.vec3 = vec3 - value.vec3;
                value.vec3 = vec3;
            }
            else
            {
                for (var i = 0; i < _valueSize; i++)
                {
                    var n1 = startValue[i];
                    var n2 = endValue[i];
                    var f = n1 + (n2 - n1) * normalizedTime;
                    if (_snapping)
                        f = Mathf.Round(f);
                    deltaValue[i] = f - value[i];
                    value[i] = f;
                }

                value.d = value.x;
            }

            if (_target != null && _propType != TweenPropType.None)
                TweenPropTypeUtils.SetProps(_target, _propType, value);

            CallUpdateCallback();
        }

        private void CallStartCallback()
        {
            if (GTween.catchCallbackExceptions)
            {
                try
                {
                    if (_onStart1 != null)
                        _onStart1(this);
                    if (_onStart != null)
                        _onStart();
                    if (_listener != null)
                        _listener.OnTweenStart(this);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("FairyGUI: error in start callback > " + e.Message);
                }
            }
            else
            {
                if (_onStart1 != null)
                    _onStart1(this);
                if (_onStart != null)
                    _onStart();
                if (_listener != null)
                    _listener.OnTweenStart(this);
            }
        }

        private void CallUpdateCallback()
        {
            if (GTween.catchCallbackExceptions)
            {
                try
                {
                    if (_onUpdate1 != null)
                        _onUpdate1(this);
                    if (_onUpdate != null)
                        _onUpdate();
                    if (_listener != null)
                        _listener.OnTweenUpdate(this);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("FairyGUI: error in update callback > " + e.Message);
                }
            }
            else
            {
                if (_onUpdate1 != null)
                    _onUpdate1(this);
                if (_onUpdate != null)
                    _onUpdate();
                if (_listener != null)
                    _listener.OnTweenUpdate(this);
            }
        }

        private void CallCompleteCallback()
        {
            if (GTween.catchCallbackExceptions)
            {
                try
                {
                    if (_onComplete1 != null)
                        _onComplete1(this);
                    if (_onComplete != null)
                        _onComplete();
                    if (_listener != null)
                        _listener.OnTweenComplete(this);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("FairyGUI: error in complete callback > " + e.Message);
                }
            }
            else
            {
                if (_onComplete1 != null)
                    _onComplete1(this);
                if (_onComplete != null)
                    _onComplete();
                if (_listener != null)
                    _listener.OnTweenComplete(this);
            }
        }
    }
}
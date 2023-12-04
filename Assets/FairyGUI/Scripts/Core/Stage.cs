using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;
#if UNITY_5_3_OR_NEWER
using UnityEngine.SceneManagement;
#endif

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class Stage : Container
    {
        private static bool _touchScreen;
        internal static int _clickTestThreshold;
#pragma warning disable 0649
        private static IKeyboard _keyboard;
#pragma warning restore 0649

        private static Stage _inst;


        private static List<DisplayObject> sTempList1;
        private static List<int> sTempList2;
        private static Dictionary<uint, int> sTempDict;
        private AudioSource _audio;
        private readonly Dictionary<string, CursorDef> _cursors;
        private bool _customInput;
        private bool _customInputButtonDown;
        private Vector2 _customInputPos;
        private DisplayObject _focused;
        private readonly List<Container> _focusHistory;
        private readonly List<DisplayObject> _focusInChain;
        private readonly List<DisplayObject> _focusOutChain;
        private int _frameGotHitTarget;
        private int _frameGotTouchPosition;
        private bool _IMEComposite;
        private InputTextField _lastInput;
        private Container _nextFocus;
        private EventListener _onStageResized;
        private readonly List<DisplayObject> _rollOutChain;
        private readonly List<DisplayObject> _rollOverChain;
        private readonly List<NTexture> _toCollectTextures = new();
        private readonly TouchInfo[] _touches;
        private Vector2 _touchPosition;

        private DisplayObject _touchTarget;
        private readonly UpdateContext _updateContext;

        /// <summary>
        /// </summary>
        public Stage()
        {
            _inst = this;
            soundVolume = 1;

            _updateContext = new UpdateContext();
            _frameGotHitTarget = -1;

            _touches = new TouchInfo[5];
            for (var i = 0; i < _touches.Length; i++)
                _touches[i] = new TouchInfo();

            var isOSX = Application.platform == RuntimePlatform.OSXPlayer
                        || Application.platform == RuntimePlatform.OSXEditor;
            if (Application.platform == RuntimePlatform.WindowsPlayer
                || Application.platform == RuntimePlatform.WindowsEditor
                || isOSX)
                touchScreen = false;
            else
                touchScreen = Input.touchSupported && SystemInfo.deviceType != DeviceType.Desktop;
            //在PC上，是否retina屏对输入法位置，鼠标滚轮速度都有影响，但现在没发现Unity有获得的方式。仅判断是否Mac可能不够（外接显示器的情况）。所以最好自行设置。
            devicePixelRatio = isOSX && Screen.dpi > 96 ? 2 : 1;

            _rollOutChain = new List<DisplayObject>();
            _rollOverChain = new List<DisplayObject>();
            _focusOutChain = new List<DisplayObject>();
            _focusInChain = new List<DisplayObject>();
            _focusHistory = new List<Container>();
            _cursors = new Dictionary<string, CursorDef>();

            SetSize(Screen.width, Screen.height);
            cachedTransform.localScale = new Vector3(StageCamera.DefaultUnitsPerPixel, StageCamera.DefaultUnitsPerPixel,
                StageCamera.DefaultUnitsPerPixel);

            var engine = Object.FindObjectOfType<StageEngine>();
            if (engine != null)
                Object.Destroy(engine.gameObject);

            gameObject.name = "Stage";
            gameObject.layer = LayerMask.NameToLayer(StageCamera.LayerName);
            gameObject.AddComponent<StageEngine>();
            gameObject.AddComponent<UIContentScaler>();
            gameObject.SetActive(true);
            Object.DontDestroyOnLoad(gameObject);

            EnableSound();

            Timers.inst.Add(5, 0, RunTextureCollector);

#if UNITY_5_4_OR_NEWER
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
#endif
        }

        [Obsolete("Use size.y")] public int stageHeight => (int)_contentRect.height;

        [Obsolete("Use size.x")] public int stageWidth => (int)_contentRect.width;

        /// <summary>
        /// </summary>
        public float soundVolume { get; set; }

        /// <summary>
        /// </summary>
        public static Stage inst
        {
            get
            {
                if (_inst == null)
                    Instantiate();

                return _inst;
            }
        }

        /// <summary>
        ///     如果是true，表示触摸输入，将使用Input.GetTouch接口读取触摸屏输入。
        ///     如果是false，表示使用鼠标输入，将使用Input.GetMouseButtonXXX接口读取鼠标输入。
        ///     一般来说，不需要设置，底层会自动根据系统环境设置正确的值。
        /// </summary>
        public static bool touchScreen
        {
            get { return _touchScreen; }
            set
            {
                _touchScreen = value;
                if (_touchScreen)
                {
#if !(UNITY_WEBPLAYER || UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || UNITY_EDITOR)
                    _keyboard = new FairyGUI.TouchScreenKeyboard();
                    keyboardInput = true;
#endif
                    _clickTestThreshold = 50;
                }
                else
                {
                    _keyboard = null;
                    keyboardInput = false;
                    inst.ResetInputState();
                    _clickTestThreshold = 10;
                }
            }
        }

        /// <summary>
        ///     如果是true，表示使用屏幕上弹出的键盘输入文字。常见于移动设备。
        ///     如果是false，表示是接受按键消息输入文字。常见于PC。
        ///     一般来说，不需要设置，底层会自动根据系统环境设置正确的值。
        /// </summary>
        public static bool keyboardInput { get; set; }

        /// <summary>
        /// </summary>
        public static bool isTouchOnUI => _inst != null && _inst.touchTarget != null;

        /// <summary>
        ///     As unity does not provide ways to detect this, you should set it by yourself.
        ///     This will effect:
        ///     1. compoistion cursor pos.
        ///     2. mouse wheel speed.
        /// </summary>
        public static float devicePixelRatio { get; set; }

        /// <summary>
        /// </summary>
        public EventListener onStageResized =>
            _onStageResized ?? (_onStageResized = new EventListener(this, "onStageResized"));

        /// <summary>
        /// </summary>
        public DisplayObject touchTarget
        {
            get
            {
                if (_frameGotHitTarget != Time.frameCount)
                    GetHitTarget();

                if (_touchTarget == this)
                    return null;
                return _touchTarget;
            }
        }

        /// <summary>
        /// </summary>
        public DisplayObject focus
        {
            get
            {
                if (_focused != null && _focused.isDisposed)
                    _focused = null;
                return _focused;
            }
            set => SetFocus(value);
        }

        /// <summary>
        /// </summary>
        public Vector2 touchPosition
        {
            get
            {
                UpdateTouchPosition();
                return _touchPosition;
            }
        }


        /// <summary>
        /// </summary>
        public int touchCount { get; private set; }

        /// <summary>
        /// </summary>
        public IKeyboard keyboard
        {
            get => _keyboard;
            set => _keyboard = value;
        }

        /// <summary>
        /// </summary>
        /// <value></value>
        public string activeCursor { get; private set; }

        public event Action beforeUpdate;
        public event Action afterUpdate;

        /// <summary>
        /// </summary>
        public static void Instantiate()
        {
            if (_inst == null)
            {
                _inst = new Stage();
                GRoot._inst = new GRoot();
                GRoot._inst.ApplyContentScaleFactor();
                _inst.AddChild(GRoot._inst.displayObject);

                StageCamera.CheckMainCamera();
            }
        }

#if UNITY_5_4_OR_NEWER
        private void SceneManager_sceneLoaded(Scene scene, LoadSceneMode mode)
        {
            StageCamera.CheckMainCamera();
        }
#endif

        public override void Dispose()
        {
            base.Dispose();

            Timers.inst.Remove(RunTextureCollector);

#if UNITY_5_4_OR_NEWER
            SceneManager.sceneLoaded -= SceneManager_sceneLoaded;
#endif
        }

        public void SetFocus(DisplayObject newFocus, bool byKey = false)
        {
            if (newFocus == this)
                newFocus = null;

            _nextFocus = null;

            if (_focused == newFocus)
                return;

            Container navRoot = null;
            var element = newFocus;
            while (element != null)
            {
                if (!element.focusable)
                    return;
                if (element is Container && ((Container)element).tabStopChildren)
                    if (navRoot == null)
                        navRoot = element as Container;

                element = element.parent;
            }

            var oldFocus = _focused;
            _focused = newFocus;

            if (navRoot != null)
            {
                navRoot._lastFocus = _focused;
                var pos = _focusHistory.IndexOf(navRoot);
                if (pos != -1)
                {
                    if (pos < _focusHistory.Count - 1)
                        _focusHistory.RemoveRange(pos + 1, _focusHistory.Count - pos - 1);
                }
                else
                {
                    _focusHistory.Add(navRoot);
                    if (_focusHistory.Count > 10)
                        _focusHistory.RemoveAt(0);
                }
            }

            _focusInChain.Clear();
            _focusOutChain.Clear();

            element = oldFocus;
            while (element != null)
            {
                if (element.focusable)
                    _focusOutChain.Add(element);
                element = element.parent;
            }

            element = _focused;
            int i;
            while (element != null)
            {
                i = _focusOutChain.IndexOf(element);
                if (i != -1)
                {
                    _focusOutChain.RemoveRange(i, _focusOutChain.Count - i);
                    break;
                }

                if (element.focusable)
                    _focusInChain.Add(element);

                element = element.parent;
            }

            var cnt = _focusOutChain.Count;
            if (cnt > 0)
            {
                for (i = 0; i < cnt; i++)
                {
                    element = _focusOutChain[i];
                    if (element.stage != null)
                    {
                        element.DispatchEvent("onFocusOut", null);
                        if (_focused != newFocus) //focus changed in event
                            return;
                    }
                }

                _focusOutChain.Clear();
            }

            cnt = _focusInChain.Count;
            if (cnt > 0)
            {
                for (i = 0; i < cnt; i++)
                {
                    element = _focusInChain[i];
                    if (element.stage != null)
                    {
                        element.DispatchEvent("onFocusIn", byKey ? "key" : null);
                        if (_focused != newFocus) //focus changed in event
                            return;
                    }
                }

                _focusInChain.Clear();
            }

            if (_focused is InputTextField)
                _lastInput = (InputTextField)_focused;
        }

        internal void _OnFocusRemoving(Container sender)
        {
            _nextFocus = sender;
            if (_focusHistory.Count > 0)
            {
                var i = _focusHistory.Count - 1;
                DisplayObject test = _focusHistory[i];
                var element = _focused;
                while (element != null && element != sender)
                {
                    if (element is Container && ((Container)element).tabStopChildren && element == test)
                    {
                        i--;
                        if (i < 0)
                            break;

                        test = _focusHistory[i];
                    }

                    element = element.parent;
                }

                if (i != _focusHistory.Count - 1)
                {
                    _focusHistory.RemoveRange(i + 1, _focusHistory.Count - i - 1);
                    if (_focusHistory.Count > 0)
                        _nextFocus = _focusHistory[_focusHistory.Count - 1];
                }
            }

            if (_focused is InputTextField)
                _lastInput = null;
            _focused = null;
        }

        /// <summary>
        /// </summary>
        /// <param name="backward"></param>
        public void DoKeyNavigate(bool backward)
        {
            Container navBase = null;
            var element = _focused;
            while (element != null)
            {
                if (element is Container && ((Container)element).tabStopChildren)
                {
                    navBase = element as Container;
                    break;
                }

                element = element.parent;
            }

            if (navBase == null)
                navBase = this;

            var it = navBase.GetDescendants(backward);
            var started = _focused == null;
            DisplayObject test2 = _focused != null ? _focused.parent : null;

            while (it.MoveNext())
            {
                var dobj = it.Current;
                if (started)
                {
                    if (dobj == test2)
                        test2 = test2.parent;
                    else if (dobj._AcceptTab())
                        return;
                }
                else if (dobj == _focused)
                {
                    started = true;
                }
            }

            if (started)
            {
                it.Reset();
                while (it.MoveNext())
                {
                    var dobj = it.Current;
                    if (dobj == _focused)
                        break;

                    if (dobj == test2)
                        test2 = test2.parent;
                    else if (dobj._AcceptTab())
                        return;
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="touchId"></param>
        /// <returns></returns>
        public Vector2 GetTouchPosition(int touchId)
        {
            UpdateTouchPosition();

            if (touchId < 0)
                return _touchPosition;

            for (var j = 0; j < 5; j++)
            {
                var touch = _touches[j];
                if (touch.touchId == touchId)
                    return new Vector2(touch.x, touch.y);
            }

            return _touchPosition;
        }

        /// <summary>
        /// </summary>
        /// <param name="touchId"></param>
        /// <returns></returns>
        public DisplayObject GetTouchTarget(int touchId)
        {
            if (_frameGotHitTarget != Time.frameCount)
                GetHitTarget();

            for (var j = 0; j < 5; j++)
            {
                var touch = _touches[j];
                if (touch.touchId == touchId)
                    return touch.target != this ? touch.target : null;
            }

            return null;
        }

        public int[] GetAllTouch(int[] result)
        {
            if (result == null)
                result = new int[touchCount];
            var i = 0;
            for (var j = 0; j < 5; j++)
            {
                var touch = _touches[j];
                if (touch.touchId != -1)
                {
                    result[i++] = touch.touchId;
                    if (i >= result.Length)
                        break;
                }
            }

            return result;
        }

        /// <summary>
        /// </summary>
        public void ResetInputState()
        {
            for (var j = 0; j < 5; j++)
                _touches[j].Reset();

            if (!touchScreen)
                _touches[0].touchId = 0;

            touchCount = 0;
        }


        /// <summary>
        /// </summary>
        /// <param name="touchId"></param>
        public void CancelClick(int touchId)
        {
            for (var j = 0; j < 5; j++)
            {
                var touch = _touches[j];
                if (touch.touchId == touchId)
                    touch.clickCancelled = true;
            }
        }

        /// <summary>
        /// </summary>
        public void EnableSound()
        {
            if (_audio == null)
            {
                _audio = gameObject.AddComponent<AudioSource>();
                _audio.bypassEffects = true;
            }
        }

        /// <summary>
        /// </summary>
        public void DisableSound()
        {
            if (_audio != null)
            {
                Object.Destroy(_audio);
                _audio = null;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="clip"></param>
        /// <param name="volumeScale"></param>
        public void PlayOneShotSound(AudioClip clip, float volumeScale)
        {
            if (_audio != null && soundVolume > 0)
                _audio.PlayOneShot(clip, volumeScale * soundVolume);
        }

        /// <summary>
        /// </summary>
        /// <param name="clip"></param>
        public void PlayOneShotSound(AudioClip clip)
        {
            if (_audio != null && soundVolume > 0)
                _audio.PlayOneShot(clip, soundVolume);
        }

        /// <summary>
        /// </summary>
        /// <param name="text"></param>
        /// <param name="autocorrection"></param>
        /// <param name="multiline"></param>
        /// <param name="secure"></param>
        /// <param name="alert"></param>
        /// <param name="textPlaceholder"></param>
        /// <param name="keyboardType"></param>
        /// <param name="hideInput"></param>
        public void OpenKeyboard(string text, bool autocorrection, bool multiline, bool secure,
            bool alert, string textPlaceholder, int keyboardType, bool hideInput)
        {
            if (_keyboard != null)
                _keyboard.Open(text, autocorrection, multiline, secure, alert, textPlaceholder, keyboardType,
                    hideInput);
        }

        /// <summary>
        /// </summary>
        public void CloseKeyboard()
        {
            if (_keyboard != null) _keyboard.Close();
        }

        /// <summary>
        ///     输入字符到当前光标位置
        /// </summary>
        /// <param name="value"></param>
        public void InputString(string value)
        {
            if (_lastInput != null)
                _lastInput.ReplaceSelection(value);
        }

        /// <summary>
        /// </summary>
        /// <param name="screenPos"></param>
        /// <param name="buttonDown"></param>
        public void SetCustomInput(Vector2 screenPos, bool buttonDown)
        {
            _customInput = true;
            _customInputButtonDown = buttonDown;
            _customInputPos = screenPos;
            _frameGotHitTarget = 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="screenPos"></param>
        /// <param name="buttonDown"></param>
        /// <param name="buttonUp"></param>
        public void SetCustomInput(Vector2 screenPos, bool buttonDown, bool buttonUp)
        {
            _customInput = true;
            if (buttonDown)
                _customInputButtonDown = true;
            else if (buttonUp)
                _customInputButtonDown = false;
            _customInputPos = screenPos;
            _frameGotHitTarget = 0;
        }

        /// <summary>
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="buttonDown"></param>
        public void SetCustomInput(ref RaycastHit hit, bool buttonDown)
        {
            Vector2 screenPos = HitTestContext.cachedMainCamera.WorldToScreenPoint(hit.point);
            HitTestContext.CacheRaycastHit(HitTestContext.cachedMainCamera, ref hit);
            SetCustomInput(screenPos, buttonDown);
        }

        /// <summary>
        /// </summary>
        /// <param name="hit"></param>
        /// <param name="buttonDown"></param>
        /// <param name="buttonUp"></param>
        public void SetCustomInput(ref RaycastHit hit, bool buttonDown, bool buttonUp)
        {
            Vector2 screenPos = HitTestContext.cachedMainCamera.WorldToScreenPoint(hit.point);
            HitTestContext.CacheRaycastHit(HitTestContext.cachedMainCamera, ref hit);
            SetCustomInput(screenPos, buttonDown, buttonUp);
        }

        public void ForceUpdate()
        {
            _updateContext.Begin();
            Update(_updateContext);
            _updateContext.End();
        }

        internal void InternalUpdate()
        {
            HandleEvents();

            if (_nextFocus != null)
            {
                if (_nextFocus.stage != null)
                {
                    if (_nextFocus.tabStopChildren)
                    {
                        if (_nextFocus._lastFocus != null && _nextFocus.IsAncestorOf(_nextFocus._lastFocus))
                            SetFocus(_nextFocus._lastFocus);
                        else
                            SetFocus(_nextFocus);
                    }
                    else
                    {
                        SetFocus(_nextFocus);
                    }
                }

                _nextFocus = null;
            }

            if (beforeUpdate != null)
                beforeUpdate();

            _updateContext.Begin();
            Update(_updateContext);
            _updateContext.End();

            if (BaseFont.textRebuildFlag)
            {
                //字体贴图更改了，重新渲染一遍，防止本帧文字显示错误
                _updateContext.Begin();
                Update(_updateContext);
                _updateContext.End();

                BaseFont.textRebuildFlag = false;
            }

            if (afterUpdate != null)
                afterUpdate();
        }

        private void GetHitTarget()
        {
            if (_frameGotHitTarget == Time.frameCount)
                return;

            _frameGotHitTarget = Time.frameCount;

            if (_customInput)
            {
                var pos = _customInputPos;
                pos.y = _contentRect.height - pos.y;

                var touch = _touches[0];
                _touchTarget = HitTest(pos, true);
                touch.target = _touchTarget;
            }
            else if (touchScreen)
            {
                _touchTarget = null;
                for (var i = 0; i < Input.touchCount; ++i)
                {
                    var uTouch = Input.GetTouch(i);

                    var pos = uTouch.position;
                    pos.y = _contentRect.height - pos.y;

                    TouchInfo touch = null;
                    TouchInfo free = null;
                    for (var j = 0; j < 5; j++)
                    {
                        if (_touches[j].touchId == uTouch.fingerId)
                        {
                            touch = _touches[j];
                            break;
                        }

                        if (_touches[j].touchId == -1)
                            free = _touches[j];
                    }

                    if (touch == null)
                    {
                        touch = free;
                        if (touch == null || uTouch.phase != TouchPhase.Began)
                            continue;

                        touch.touchId = uTouch.fingerId;
                    }

                    if (uTouch.phase == TouchPhase.Stationary)
                    {
                        _touchTarget = touch.target;
                    }
                    else
                    {
                        _touchTarget = HitTest(pos, true);
                        touch.target = _touchTarget;
                    }
                }
            }
            else
            {
                Vector2 pos = Input.mousePosition;
                pos.y = Screen.height - pos.y;
                var touch = _touches[0];
                if (pos.x < 0 || pos.y < 0) //outside of the window
                    _touchTarget = this;
                else
                    _touchTarget = HitTest(pos, true);
                touch.target = _touchTarget;
            }

            HitTestContext.ClearRaycastHitCache();
        }

        internal void HandleScreenSizeChanged(int screenWidth, int screenHeight, float unitsPerPixel)
        {
            SetSize(screenWidth, screenHeight);
            cachedTransform.localScale = new Vector3(unitsPerPixel, unitsPerPixel, unitsPerPixel);

            if (!DispatchEvent("onStageResized", null))
            {
                var scaler = gameObject.GetComponent<UIContentScaler>();
                scaler.ApplyChange();
                GRoot.inst.ApplyContentScaleFactor();
            }
        }

        internal void HandleGUIEvents(Event evt)
        {
            if (evt.rawType == EventType.KeyDown)
            {
                if (_IMEComposite && Input.compositionString.Length == 0)
                {
                    _IMEComposite = false;
                    //eat one key on IME closing
                    if (evt.keyCode != KeyCode.None)
                        return;
                }

                var touch = _touches[0];
                touch.keyCode = evt.keyCode;
                touch.modifiers = evt.modifiers;
                touch.character = evt.character;

                touch.UpdateEvent();
                var f = focus;
                if (f != null)
                    f.BubbleEvent("onKeyDown", touch.evt);
                else
                    DispatchEvent("onKeyDown", touch.evt);
            }
            else if (evt.rawType == EventType.KeyUp)
            {
                var touch = _touches[0];
                touch.keyCode = evt.keyCode;
                touch.modifiers = evt.modifiers;
                touch.character = evt.character;

                touch.UpdateEvent();
                var f = focus;
                if (f != null)
                    f.BubbleEvent("onKeyUp", touch.evt);
                else
                    DispatchEvent("onKeyUp", touch.evt);
            }
#if UNITY_2017_1_OR_NEWER
            else if (evt.type == EventType.ScrollWheel)
#else
            else if (evt.type == EventType.scrollWheel)
#endif
            {
                if (_touchTarget != null)
                {
                    var touch = _touches[0];
                    touch.mouseWheelDelta = evt.delta.y;
                    touch.UpdateEvent();
                    _touchTarget.BubbleEvent("onMouseWheel", touch.evt);
                    touch.mouseWheelDelta = 0;
                }
            }
        }

        private void HandleEvents()
        {
            GetHitTarget();

            UpdateTouchPosition();

            if (_customInput)
            {
                HandleCustomInput();
                _customInput = false;
            }
            else if (touchScreen)
            {
                HandleTouchEvents();
            }
            else
            {
                HandleMouseEvents();
            }

            if (_focused is InputTextField)
                HandleTextInput();
        }

        private void UpdateTouchPosition()
        {
            if (_frameGotTouchPosition != Time.frameCount)
            {
                _frameGotTouchPosition = Time.frameCount;
                if (_customInput)
                {
                    _touchPosition = _customInputPos;
                    _touchPosition.y = _contentRect.height - _touchPosition.y;
                }
                else if (touchScreen)
                {
                    for (var i = 0; i < Input.touchCount; ++i)
                    {
                        var uTouch = Input.GetTouch(i);
                        _touchPosition = uTouch.position;
                        _touchPosition.y = _contentRect.height - _touchPosition.y;
                    }
                }
                else
                {
                    Vector2 pos = Input.mousePosition;
                    if (pos.x >= 0 && pos.y >= 0) //编辑器环境下坐标有时是负
                    {
                        pos.y = _contentRect.height - pos.y;
                        _touchPosition = pos;
                    }
                }
            }
        }

        private void HandleTextInput()
        {
            _IMEComposite = Input.compositionString.Length > 0;

            var textField = (InputTextField)_focused;
            if (!textField.editable)
                return;

            if (keyboardInput)
            {
                if (textField.keyboardInput && _keyboard != null)
                {
                    var s = _keyboard.GetInput();
                    if (s != null)
                    {
                        if (_keyboard.supportsCaret)
                            textField.ReplaceSelection(s);
                        else
                            textField.ReplaceText(s);
                    }

                    if (_keyboard.done)
                        SetFocus(null);
                }
            }
            else
            {
                textField.CheckComposition();
            }
        }

        private void HandleCustomInput()
        {
            var pos = _customInputPos;
            pos.y = _contentRect.height - pos.y;
            var touch = _touches[0];

            if (touch.x != pos.x || touch.y != pos.y)
            {
                touch.x = pos.x;
                touch.y = pos.y;
                touch.Move();
            }

            if (touch.lastRollOver != touch.target)
                HandleRollOver(touch);

            if (_customInputButtonDown)
            {
                if (!touch.began)
                {
                    touchCount = 1;
                    touch.Begin();
                    touch.button = 0;
                    touch.touchId = 0;
                    SetFocus(touch.target);

                    touch.UpdateEvent();
                    touch.target.BubbleEvent("onTouchBegin", touch.evt);
                }
            }
            else if (touch.began)
            {
                touchCount = 0;
                touch.End();

                var clickTarget = touch.ClickTest();
                if (clickTarget != null)
                {
                    touch.UpdateEvent();
                    clickTarget.BubbleEvent("onClick", touch.evt);
                }

                touch.button = -1;
            }
        }

        private void HandleMouseEvents()
        {
            var touch = _touches[0];
            if (touch.x != _touchPosition.x || touch.y != _touchPosition.y)
            {
                touch.x = _touchPosition.x;
                touch.y = _touchPosition.y;
                touch.Move();
            }

            if (touch.lastRollOver != touch.target)
                HandleRollOver(touch);

            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                if (!touch.began)
                {
                    touchCount = 1;
                    touch.Begin();
                    touch.button = Input.GetMouseButtonDown(2) ? 2 : Input.GetMouseButtonDown(1) ? 1 : 0;
                    SetFocus(touch.target);

                    touch.UpdateEvent();
                    touch.target.BubbleEvent("onTouchBegin", touch.evt);
                }

            if (Input.GetMouseButtonUp(0) || Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
                if (touch.began)
                {
                    touchCount = 0;
                    touch.End();

                    var clickTarget = touch.ClickTest();
                    if (clickTarget != null)
                    {
                        touch.UpdateEvent();

                        if (Input.GetMouseButtonUp(1) || Input.GetMouseButtonUp(2))
                            clickTarget.BubbleEvent("onRightClick", touch.evt);
                        else
                            clickTarget.BubbleEvent("onClick", touch.evt);
                    }

                    touch.button = -1;
                }

            //We have to do this, coz the cursor will auto change back after a click or dragging
            if (Input.GetMouseButtonUp(0) && activeCursor != null)
                _ChangeCursor(activeCursor);
        }

        private void HandleTouchEvents()
        {
            var tc = Input.touchCount;
            for (var i = 0; i < tc; ++i)
            {
                var uTouch = Input.GetTouch(i);

                if (uTouch.phase == TouchPhase.Stationary)
                    continue;

                var pos = uTouch.position;
                pos.y = _contentRect.height - pos.y;

                TouchInfo touch = null;
                for (var j = 0; j < 5; j++)
                    if (_touches[j].touchId == uTouch.fingerId)
                    {
                        touch = _touches[j];
                        break;
                    }

                if (touch == null)
                    continue;

                if (touch.x != pos.x || touch.y != pos.y)
                {
                    touch.x = pos.x;
                    touch.y = pos.y;
                    if (touch.began)
                        touch.Move();
                }

                if (touch.lastRollOver != touch.target)
                    HandleRollOver(touch);

                if (uTouch.phase == TouchPhase.Began)
                {
                    if (!touch.began)
                    {
                        touchCount++;
                        touch.Begin();
                        touch.button = 0;
                        SetFocus(touch.target);

                        touch.UpdateEvent();
                        touch.target.BubbleEvent("onTouchBegin", touch.evt);
                    }
                }
                else if (uTouch.phase == TouchPhase.Canceled || uTouch.phase == TouchPhase.Ended)
                {
                    if (touch.began)
                    {
                        touchCount--;
                        touch.End();

                        if (uTouch.phase != TouchPhase.Canceled)
                        {
                            var clickTarget = touch.ClickTest();
                            if (clickTarget != null)
                            {
                                touch.clickCount = uTouch.tapCount;
                                touch.UpdateEvent();
                                clickTarget.BubbleEvent("onClick", touch.evt);
                            }
                        }

                        touch.target = null;
                        HandleRollOver(touch);

                        touch.touchId = -1;
                    }
                }
            }
        }

        private void HandleRollOver(TouchInfo touch)
        {
            DisplayObject element;
            _rollOverChain.Clear();
            _rollOutChain.Clear();

            element = touch.lastRollOver;
            while (element != null)
            {
                _rollOutChain.Add(element);
                element = element.parent;
            }

            touch.lastRollOver = touch.target;

            var cursor = this.cursor;
            if (cursor == null)
            {
                element = touch.target;
                while (element != null)
                {
                    if (element.cursor != null && cursor == null)
                        cursor = element.cursor;
                    element = element.parent;
                }
            }

            element = touch.target;
            int i;
            while (element != null)
            {
                i = _rollOutChain.IndexOf(element);
                if (i != -1)
                {
                    _rollOutChain.RemoveRange(i, _rollOutChain.Count - i);
                    break;
                }

                _rollOverChain.Add(element);

                element = element.parent;
            }

            var cnt = _rollOutChain.Count;
            if (cnt > 0)
            {
                for (i = 0; i < cnt; i++)
                {
                    element = _rollOutChain[i];
                    if (element.stage != null)
                        element.DispatchEvent("onRollOut", null);
                }

                _rollOutChain.Clear();
            }

            cnt = _rollOverChain.Count;
            if (cnt > 0)
            {
                for (i = 0; i < cnt; i++)
                {
                    element = _rollOverChain[i];
                    if (element.stage != null)
                        element.DispatchEvent("onRollOver", null);
                }

                _rollOverChain.Clear();
            }

            if (cursor != activeCursor)
                _ChangeCursor(cursor);
        }

        /// <summary>
        ///     设置UIPanel/UIPainter等的渲染层次，由UIPanel等内部调用。开发者不需要调用。
        /// </summary>
        /// <param name="target"></param>
        /// <param name="value"></param>
        public void ApplyPanelOrder(Container target)
        {
            var sortingOrder = target._panelOrder;
            var numChildren = this.numChildren;
            var i = 0;
            int j;
            var curIndex = -1;
            for (; i < numChildren; i++)
            {
                var obj = GetChildAt(i);
                if (obj == target)
                {
                    curIndex = i;
                    continue;
                }

                if (obj == GRoot.inst.displayObject)
                    j = 1000;
                else if (obj is Container)
                    j = ((Container)obj)._panelOrder;
                else
                    continue;

                if (sortingOrder <= j)
                {
                    if (curIndex != -1)
                        AddChildAt(target, i - 1);
                    else
                        AddChildAt(target, i);
                    break;
                }
            }

            if (i == numChildren)
                AddChild(target);
        }

        /// <summary>
        ///     Adjust display order of all UIPanels rendering in worldspace by their z order.
        /// </summary>
        /// <param name="panelSortingOrder">Only UIPanel.sortingOrder equals to this value will be involve in this sorting</param>
        public void SortWorldSpacePanelsByZOrder(int panelSortingOrder)
        {
            if (sTempList1 == null)
            {
                sTempList1 = new List<DisplayObject>();
                sTempList2 = new List<int>();
                sTempDict = new Dictionary<uint, int>();
            }

            var numChildren = this.numChildren;
            for (var i = 0; i < numChildren; i++)
            {
                var obj = GetChildAt(i) as Container;
                if (obj == null || obj.renderMode != RenderMode.WorldSpace || obj._panelOrder != panelSortingOrder)
                    continue;

                sTempDict[obj.id] = i;

                sTempList1.Add(obj);
                sTempList2.Add(i);
            }

            sTempList1.Sort((c1, c2) =>
            {
                var ret = c2.cachedTransform.position.z.CompareTo(c1.cachedTransform.position.z);
                if (ret == 0)
                    //如果大家z值一样，使用原来的顺序，防止不停交换顺序（闪烁）
                    return sTempDict[c1.id].CompareTo(sTempDict[c2.id]);
                return ret;
            });

            ChangeChildrenOrder(sTempList2, sTempList1);

            sTempList1.Clear();
            sTempList2.Clear();
            sTempDict.Clear();
        }

        /// <summary>
        /// </summary>
        /// <param name="texture"></param>
        public void MonitorTexture(NTexture texture)
        {
            if (_toCollectTextures.IndexOf(texture) == -1)
                _toCollectTextures.Add(texture);
        }

        private void RunTextureCollector(object param)
        {
            var cnt = _toCollectTextures.Count;
            var curTime = Time.time;
            var i = 0;
            while (i < cnt)
            {
                var texture = _toCollectTextures[i];
                if (texture.disposed)
                {
                    _toCollectTextures.RemoveAt(i);
                    cnt--;
                }
                else if (curTime - texture.lastActive > 5)
                {
                    texture.Dispose();
                    _toCollectTextures.RemoveAt(i);
                    cnt--;
                }
                else
                {
                    i++;
                }
            }
        }

        public void AddTouchMonitor(int touchId, EventDispatcher target)
        {
            TouchInfo touch = null;
            for (var j = 0; j < 5; j++)
            {
                touch = _touches[j];
                if ((touchId == -1 && touch.touchId != -1)
                    || (touchId != -1 && touch.touchId == touchId))
                    break;
            }

            if (touch.touchMonitors.IndexOf(target) == -1)
                touch.touchMonitors.Add(target);
        }

        public void RemoveTouchMonitor(EventDispatcher target)
        {
            for (var j = 0; j < 5; j++)
            {
                var touch = _touches[j];
                var i = touch.touchMonitors.IndexOf(target);
                if (i != -1)
                    touch.touchMonitors[i] = null;
            }
        }

        public bool IsTouchMonitoring(EventDispatcher target)
        {
            for (var j = 0; j < 5; j++)
            {
                var touch = _touches[j];
                var i = touch.touchMonitors.IndexOf(target);
                if (i != -1)
                    return true;
            }

            return false;
        }

        internal Transform CreatePoolManager(string name)
        {
            var go = new GameObject("[" + name + "]");
            go.SetActive(false);

            var t = go.transform;
            t.SetParent(cachedTransform, false);

            return t;
        }

        /// <summary>
        /// </summary>
        /// <param name="cursorName"></param>
        /// <param name="texture"></param>
        /// <param name="hotspot"></param>
        public void RegisterCursor(string cursorName, Texture2D texture, Vector2 hotspot)
        {
            _cursors[cursorName] = new CursorDef { texture = texture, hotspot = hotspot };
        }

        /// <summary>
        /// </summary>
        /// <param name="cursorName"></param>
        internal void _ChangeCursor(string cursorName)
        {
            CursorDef cursorDef;
            if (cursorName != null && _cursors.TryGetValue(cursorName, out cursorDef))
            {
                if (activeCursor == cursorName)
                    Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                activeCursor = cursorName;
                Cursor.SetCursor(cursorDef.texture, cursorDef.hotspot, CursorMode.Auto);
            }
            else
            {
                activeCursor = null;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        private class CursorDef
        {
            public Vector2 hotspot;
            public Texture2D texture;
        }
    }

    internal class TouchInfo
    {
        private static readonly List<EventBridge> sHelperChain = new();
        public bool began;
        public int button;
        public char character;
        public bool clickCancelled;
        public int clickCount;
        public int downFrame;
        public List<DisplayObject> downTargets;
        public float downTime;

        public float downX;
        public float downY;

        public InputEvent evt;
        public float holdTime;
        public KeyCode keyCode;
        public int lastClickButton;
        public float lastClickTime;
        public float lastClickX;
        public float lastClickY;
        public DisplayObject lastRollOver;
        public EventModifiers modifiers;
        public float mouseWheelDelta;
        public DisplayObject target;
        public int touchId;
        public List<EventDispatcher> touchMonitors;
        public float x;
        public float y;

        public TouchInfo()
        {
            evt = new InputEvent();
            downTargets = new List<DisplayObject>();
            touchMonitors = new List<EventDispatcher>();
            Reset();
        }

        public void Reset()
        {
            touchId = -1;
            x = 0;
            y = 0;
            clickCount = 0;
            button = -1;
            keyCode = KeyCode.None;
            character = '\0';
            modifiers = 0;
            mouseWheelDelta = 0;
            lastClickTime = 0;
            began = false;
            target = null;
            downTargets.Clear();
            lastRollOver = null;
            clickCancelled = false;
            touchMonitors.Clear();
        }

        public void UpdateEvent()
        {
            evt.touchId = touchId;
            evt.x = x;
            evt.y = y;
            evt.clickCount = clickCount;
            evt.keyCode = keyCode;
            evt.character = character;
            evt.modifiers = modifiers;
            evt.mouseWheelDelta = mouseWheelDelta;
            evt.button = button;
            evt.holdTime = holdTime;
        }

        public void Begin()
        {
            began = true;
            clickCancelled = false;
            downX = x;
            downY = y;
            downTime = Time.unscaledTime;
            downFrame = Time.frameCount;
            holdTime = 0;

            downTargets.Clear();
            if (target != null)
            {
                downTargets.Add(target);
                var obj = target;
                while (obj != null)
                {
                    downTargets.Add(obj);
                    obj = obj.parent;
                }
            }
        }

        public void Move()
        {
            if (began)
                holdTime = Time.frameCount - downFrame == 1
                    ? 1f / Application.targetFrameRate
                    : Time.unscaledTime - downTime;

            UpdateEvent();

            if (Mathf.Abs(x - downX) > 50 || Mathf.Abs(y - downY) > 50) clickCancelled = true;

            if (touchMonitors.Count > 0)
            {
                var len = touchMonitors.Count;
                for (var i = 0; i < len; i++)
                {
                    var e = touchMonitors[i];
                    if (e != null)
                    {
                        if (e is DisplayObject && ((DisplayObject)e).stage == null)
                            continue;
                        if (e is GObject && !((GObject)e).onStage)
                            continue;
                        e.GetChainBridges("onTouchMove", sHelperChain, false);
                    }
                }

                Stage.inst.BubbleEvent("onTouchMove", evt, sHelperChain);
                sHelperChain.Clear();
            }
            else
            {
                Stage.inst.DispatchEvent("onTouchMove", evt);
            }
        }

        public void End()
        {
            began = false;

            if (downTargets.Count == 0
                || clickCancelled
                || Mathf.Abs(x - downX) > Stage._clickTestThreshold
                || Mathf.Abs(y - downY) > Stage._clickTestThreshold)
            {
                clickCancelled = true;
                lastClickTime = 0;
                clickCount = 1;
            }
            else
            {
                if (Time.unscaledTime - lastClickTime < 0.35f
                    && Mathf.Abs(x - lastClickX) < Stage._clickTestThreshold
                    && Mathf.Abs(y - lastClickY) < Stage._clickTestThreshold
                    && lastClickButton == button)
                {
                    if (clickCount == 2)
                        clickCount = 1;
                    else
                        clickCount++;
                }
                else
                {
                    clickCount = 1;
                }

                lastClickTime = Time.unscaledTime;
                lastClickX = x;
                lastClickY = y;
                lastClickButton = button;
            }

            //当间隔一帧时，使用帧率计算时间，避免掉帧因素
            holdTime = Time.frameCount - downFrame == 1
                ? 1f / Application.targetFrameRate
                : Time.unscaledTime - downTime;
            UpdateEvent();

            if (touchMonitors.Count > 0)
            {
                var len = touchMonitors.Count;
                for (var i = 0; i < len; i++)
                {
                    var e = touchMonitors[i];
                    if (e != null)
                        e.GetChainBridges("onTouchEnd", sHelperChain, false);
                }

                target.BubbleEvent("onTouchEnd", evt, sHelperChain);

                touchMonitors.Clear();
                sHelperChain.Clear();
            }
            else
            {
                target.BubbleEvent("onTouchEnd", evt);
            }
        }

        public DisplayObject ClickTest()
        {
            if (clickCancelled)
            {
                downTargets.Clear();
                return null;
            }

            var obj = downTargets[0];
            if (obj.stage != null) //依然派发到原来的downTarget，虽然可能它已经偏离当前位置，主要是为了正确处理点击缩放的效果
            {
                downTargets.Clear();
                return obj;
            }

            obj = target;
            while (obj != null)
            {
                var i = downTargets.IndexOf(obj);
                if (i != -1 && obj.stage != null)
                    break;

                obj = obj.parent;
            }

            downTargets.Clear();

            return obj;
        }
    }
}
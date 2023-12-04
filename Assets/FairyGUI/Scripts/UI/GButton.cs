using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    ///     GButton class.
    /// </summary>
    public class GButton : GComponent, IColorGear
    {
        public const string UP = "up";
        public const string DOWN = "down";
        public const string OVER = "over";
        public const string SELECTED_OVER = "selectedOver";
        public const string DISABLED = "disabled";
        public const string SELECTED_DISABLED = "selectedDisabled";
        private Controller _buttonController;

        private bool _down;
        private int _downEffect;
        private float _downEffectValue;
        private bool _downScaled;
        private string _icon;
        protected GObject _iconObject;

        private ButtonMode _mode;

        private EventListener _onChanged;
        private bool _over;
        protected Controller _relatedController;
        protected string _relatedPageId;
        private bool _selected;
        private string _selectedIcon;
        private string _selectedTitle;
        private string _title;

        protected GObject _titleObject;

        /// <summary>
        ///     For radio or checkbox. if false, the button will not change selected status on click. Default is true.
        ///     如果为true，对于单选和多选按钮，当玩家点击时，按钮会自动切换状态。设置为false，则不会。默认为true。
        /// </summary>
        public bool changeStateOnClick;

        /// <summary>
        ///     Show a popup on click.
        ///     可以为按钮设置一个关联的组件，当按钮被点击时，此组件被自动弹出。
        /// </summary>
        public GObject linkedPopup;

        /// <summary>
        ///     Play sound when button is clicked.
        /// </summary>
        public NAudioClip sound;

        /// <summary>
        ///     Volume of the click sound. (0-1)
        /// </summary>
        public float soundVolumeScale;

        public GButton()
        {
            sound = UIConfig.buttonSound;
            soundVolumeScale = UIConfig.buttonSoundVolumeScale;
            changeStateOnClick = true;
            _downEffectValue = 0.8f;
            _title = string.Empty;
        }

        /// <summary>
        ///     Dispatched when the button status was changed.
        ///     如果为单选或多选按钮，当按钮的选中状态发生改变时，此事件触发。
        /// </summary>
        public EventListener onChanged => _onChanged ?? (_onChanged = new EventListener(this, "onChanged"));

        /// <summary>
        ///     Icon of the button.
        /// </summary>
        public override string icon
        {
            get => _icon;
            set
            {
                _icon = value;
                value = _selected && _selectedIcon != null ? _selectedIcon : _icon;
                if (_iconObject != null)
                    _iconObject.icon = value;
                UpdateGear(7);
            }
        }

        /// <summary>
        ///     Title of the button
        /// </summary>
        public string title
        {
            get => _title;
            set
            {
                _title = value;
                if (_titleObject != null)
                    _titleObject.text = _selected && _selectedTitle != null ? _selectedTitle : _title;
                UpdateGear(6);
            }
        }

        /// <summary>
        ///     Same of the title.
        /// </summary>
        public override string text
        {
            get => title;
            set => title = value;
        }

        /// <summary>
        ///     Icon value on selected status.
        /// </summary>
        public string selectedIcon
        {
            get => _selectedIcon;
            set
            {
                _selectedIcon = value;
                value = _selected && _selectedIcon != null ? _selectedIcon : _icon;
                if (_iconObject != null)
                    _iconObject.icon = value;
            }
        }

        /// <summary>
        ///     Title value on selected status.
        /// </summary>
        public string selectedTitle
        {
            get => _selectedTitle;
            set
            {
                _selectedTitle = value;
                if (_titleObject != null)
                    _titleObject.text = _selected && _selectedTitle != null ? _selectedTitle : _title;
            }
        }

        /// <summary>
        ///     Title color.
        /// </summary>
        public Color titleColor
        {
            get
            {
                var tf = GetTextField();
                if (tf != null)
                    return tf.color;
                return Color.black;
            }
            set
            {
                var tf = GetTextField();
                if (tf != null)
                {
                    tf.color = value;
                    UpdateGear(4);
                }
            }
        }

        /// <summary>
        /// </summary>
        public int titleFontSize
        {
            get
            {
                var tf = GetTextField();
                if (tf != null)
                    return tf.textFormat.size;
                return 0;
            }
            set
            {
                var tf = GetTextField();
                if (tf != null)
                {
                    var format = tf.textFormat;
                    format.size = value;
                    tf.textFormat = format;
                }
            }
        }

        /// <summary>
        ///     If the button is in selected status.
        /// </summary>
        public bool selected
        {
            get => _selected;

            set
            {
                if (_mode == ButtonMode.Common)
                    return;

                if (_selected != value)
                {
                    _selected = value;
                    SetCurrentState();
                    if (_selectedTitle != null && _titleObject != null)
                        _titleObject.text = _selected ? _selectedTitle : _title;
                    if (_selectedIcon != null)
                    {
                        var str = _selected ? _selectedIcon : _icon;
                        if (_iconObject != null)
                            _iconObject.icon = str;
                    }

                    if (_relatedController != null
                        && parent != null
                        && !parent._buildingDisplayList)
                    {
                        if (_selected)
                        {
                            _relatedController.selectedPageId = _relatedPageId;
                            if (_relatedController.autoRadioGroupDepth)
                                parent.AdjustRadioGroupDepth(this, _relatedController);
                        }
                        else if (_mode == ButtonMode.Check && _relatedController.selectedPageId == _relatedPageId)
                        {
                            _relatedController.oppositePageId = _relatedPageId;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Button mode.
        /// </summary>
        /// <seealso cref="ButtonMode" />
        public ButtonMode mode
        {
            get => _mode;
            set
            {
                if (_mode != value)
                {
                    if (value == ButtonMode.Common)
                        selected = false;
                    _mode = value;
                }
            }
        }

        /// <summary>
        ///     A controller is connected to this button, the activate page of this controller will change while the button status
        ///     changed.
        ///     对应编辑器中的单选控制器。
        /// </summary>
        public Controller relatedController
        {
            get => _relatedController;
            set
            {
                if (value != _relatedController)
                {
                    _relatedController = value;
                    _relatedPageId = null;
                }
            }
        }

        /// <summary>
        /// </summary>
        public string relatedPageId
        {
            get => _relatedPageId;
            set => _relatedPageId = value;
        }

        /// <summary>
        /// </summary>
        public Color color
        {
            get => titleColor;
            set => titleColor = value;
        }

        /// <summary>
        ///     Simulates a click on this button.
        ///     模拟点击这个按钮。
        /// </summary>
        /// <param name="downEffect">If the down effect will simulate too.</param>
        public void FireClick(bool downEffect, bool clickCall = false)
        {
            if (downEffect && _mode == ButtonMode.Common)
            {
                SetState(OVER);

                Timers.inst.Add(0.1f, 1, param => { SetState(DOWN); });
                Timers.inst.Add(0.2f, 1,
                    param =>
                    {
                        SetState(UP);
                        if (clickCall) onClick.Call();
                    }
                );
            }
            else
            {
                if (clickCall) onClick.Call();
            }

            __click();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public GTextField GetTextField()
        {
            if (_titleObject is GTextField)
                return (GTextField)_titleObject;
            if (_titleObject is GLabel)
                return ((GLabel)_titleObject).GetTextField();
            if (_titleObject is GButton)
                return ((GButton)_titleObject).GetTextField();
            return null;
        }

        protected void SetState(string val)
        {
            if (_buttonController != null)
                _buttonController.selectedPage = val;

            if (_downEffect == 1)
            {
                var cnt = numChildren;
                if (val == DOWN || val == SELECTED_OVER || val == SELECTED_DISABLED)
                {
                    var color = new Color(_downEffectValue, _downEffectValue, _downEffectValue);
                    for (var i = 0; i < cnt; i++)
                    {
                        var obj = GetChildAt(i);
                        if (obj is IColorGear && !(obj is GTextField))
                            ((IColorGear)obj).color = color;
                    }
                }
                else
                {
                    for (var i = 0; i < cnt; i++)
                    {
                        var obj = GetChildAt(i);
                        if (obj is IColorGear && !(obj is GTextField))
                            ((IColorGear)obj).color = Color.white;
                    }
                }
            }
            else if (_downEffect == 2)
            {
                if (val == DOWN || val == SELECTED_OVER || val == SELECTED_DISABLED)
                {
                    if (!_downScaled)
                    {
                        _downScaled = true;
                        SetScale(scaleX * _downEffectValue, scaleY * _downEffectValue);
                    }
                }
                else
                {
                    if (_downScaled)
                    {
                        _downScaled = false;
                        SetScale(scaleX / _downEffectValue, scaleY / _downEffectValue);
                    }
                }
            }
        }

        protected void SetCurrentState()
        {
            if (grayed && _buttonController != null && _buttonController.HasPage(DISABLED))
            {
                if (_selected)
                    SetState(SELECTED_DISABLED);
                else
                    SetState(DISABLED);
            }
            else
            {
                if (_selected)
                    SetState(_over ? SELECTED_OVER : DOWN);
                else
                    SetState(_over ? OVER : UP);
            }
        }

        public override void HandleControllerChanged(Controller c)
        {
            base.HandleControllerChanged(c);

            if (_relatedController == c)
                selected = _relatedPageId == c.selectedPageId;
        }

        protected override void HandleGrayedChanged()
        {
            if (_buttonController != null && _buttonController.HasPage(DISABLED))
            {
                if (grayed)
                {
                    if (_selected)
                        SetState(SELECTED_DISABLED);
                    else
                        SetState(DISABLED);
                }
                else
                {
                    if (_selected)
                        SetState(DOWN);
                    else
                        SetState(UP);
                }
            }
            else
            {
                base.HandleGrayedChanged();
            }
        }

        protected override void ConstructExtension(ByteBuffer buffer)
        {
            buffer.Seek(0, 6);

            _mode = (ButtonMode)buffer.ReadByte();
            var str = buffer.ReadS();
            if (str != null)
                sound = UIPackage.GetItemAssetByURL(str) as NAudioClip;
            soundVolumeScale = buffer.ReadFloat();
            _downEffect = buffer.ReadByte();
            _downEffectValue = buffer.ReadFloat();
            if (_downEffect == 2)
                SetPivot(0.5f, 0.5f, pivotAsAnchor);

            _buttonController = GetController("button");
            _titleObject = GetChild("title");
            _iconObject = GetChild("icon");
            if (_titleObject != null)
                _title = _titleObject.text;
            if (_iconObject != null)
                _icon = _iconObject.icon;

            if (_mode == ButtonMode.Common)
                SetState(UP);

            displayObject.onRollOver.Add(__rollover);
            displayObject.onRollOut.Add(__rollout);
            displayObject.onTouchBegin.Add(__touchBegin);
            displayObject.onTouchEnd.Add(__touchEnd);
            displayObject.onRemovedFromStage.Add(__removedFromStage);
            displayObject.onClick.Add(__click);
        }

        public override void Setup_AfterAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_AfterAdd(buffer, beginPos);

            if (!buffer.Seek(beginPos, 6))
                return;

            if ((ObjectType)buffer.ReadByte() != packageItem.objectType)
                return;

            string str;

            str = buffer.ReadS();
            if (str != null)
                title = str;
            str = buffer.ReadS();
            if (str != null)
                selectedTitle = str;
            str = buffer.ReadS();
            if (str != null)
                icon = str;
            str = buffer.ReadS();
            if (str != null)
                selectedIcon = str;
            if (buffer.ReadBool())
                titleColor = buffer.ReadColor();
            var iv = buffer.ReadInt();
            if (iv != 0)
                titleFontSize = iv;
            iv = buffer.ReadShort();
            if (iv >= 0)
                _relatedController = parent.GetControllerAt(iv);
            _relatedPageId = buffer.ReadS();

            str = buffer.ReadS();
            if (str != null)
                sound = UIPackage.GetItemAssetByURL(str) as NAudioClip;
            if (buffer.ReadBool())
                soundVolumeScale = buffer.ReadFloat();

            selected = buffer.ReadBool();
        }

        private void __rollover()
        {
            if (_buttonController == null || !_buttonController.HasPage(OVER))
                return;

            _over = true;
            if (_down)
                return;

            if (grayed && _buttonController.HasPage(DISABLED))
                return;

            SetState(_selected ? SELECTED_OVER : OVER);
        }

        private void __rollout()
        {
            if (_buttonController == null || !_buttonController.HasPage(OVER))
                return;

            _over = false;
            if (_down)
                return;

            if (grayed && _buttonController.HasPage(DISABLED))
                return;

            SetState(_selected ? DOWN : UP);
        }

        private void __touchBegin(EventContext context)
        {
            if (context.inputEvent.button != 0)
                return;

            _down = true;
            context.CaptureTouch();

            if (_mode == ButtonMode.Common)
            {
                if (grayed && _buttonController != null && _buttonController.HasPage(DISABLED))
                    SetState(SELECTED_DISABLED);
                else
                    SetState(DOWN);
            }

            if (linkedPopup != null)
            {
                if (linkedPopup is Window)
                    ((Window)linkedPopup).ToggleStatus();
                else
                    root.TogglePopup(linkedPopup, this);
            }
        }

        private void __touchEnd()
        {
            if (_down)
            {
                _down = false;
                if (_mode == ButtonMode.Common)
                {
                    if (grayed && _buttonController != null && _buttonController.HasPage(DISABLED))
                        SetState(DISABLED);
                    else if (_over)
                        SetState(OVER);
                    else
                        SetState(UP);
                }
                else
                {
                    if (!_over
                        && _buttonController != null
                        && (_buttonController.selectedPage == OVER || _buttonController.selectedPage == SELECTED_OVER))
                        SetCurrentState();
                }
            }
        }

        private void __removedFromStage()
        {
            if (_over)
                __rollout();
        }

        private void __click()
        {
            if (sound != null && sound.nativeClip != null)
                Stage.inst.PlayOneShotSound(sound.nativeClip, soundVolumeScale);

            if (_mode == ButtonMode.Check)
            {
                if (changeStateOnClick)
                {
                    selected = !_selected;
                    DispatchEvent("onChanged", null);
                }
            }
            else if (_mode == ButtonMode.Radio)
            {
                if (changeStateOnClick && !_selected)
                {
                    selected = true;
                    DispatchEvent("onChanged", null);
                }
            }
            else
            {
                if (_relatedController != null)
                    _relatedController.selectedPageId = _relatedPageId;
            }
        }
    }
}
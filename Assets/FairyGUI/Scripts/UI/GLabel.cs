using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    ///     GLabel class.
    /// </summary>
    public class GLabel : GComponent, IColorGear
    {
        protected GObject _iconObject;
        protected GObject _titleObject;

        /// <summary>
        ///     Icon of the label.
        /// </summary>
        public override string icon
        {
            get
            {
                if (_iconObject != null)
                    return _iconObject.icon;
                return null;
            }

            set
            {
                if (_iconObject != null)
                    _iconObject.icon = value;
                UpdateGear(7);
            }
        }

        /// <summary>
        ///     Title of the label.
        /// </summary>
        public string title
        {
            get
            {
                if (_titleObject != null)
                    return _titleObject.text;
                return null;
            }
            set
            {
                if (_titleObject != null)
                    _titleObject.text = value;
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
        ///     If title is input text.
        /// </summary>
        public bool editable
        {
            get
            {
                if (_titleObject is GTextInput)
                    return _titleObject.asTextInput.editable;
                return false;
            }

            set
            {
                if (_titleObject is GTextInput)
                    _titleObject.asTextInput.editable = value;
            }
        }

        /// <summary>
        ///     Title color of the label
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
        /// </summary>
        public Color color
        {
            get => titleColor;
            set => titleColor = value;
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

        protected override void ConstructExtension(ByteBuffer buffer)
        {
            _titleObject = GetChild("title");
            _iconObject = GetChild("icon");
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
                icon = str;
            if (buffer.ReadBool())
                titleColor = buffer.ReadColor();
            var iv = buffer.ReadInt();
            if (iv != 0)
                titleFontSize = iv;

            if (buffer.ReadBool())
            {
                var input = GetTextField() as GTextInput;
                if (input != null)
                {
                    str = buffer.ReadS();
                    if (str != null)
                        input.promptText = str;

                    str = buffer.ReadS();
                    if (str != null)
                        input.restrict = str;

                    iv = buffer.ReadInt();
                    if (iv != 0)
                        input.maxLength = iv;
                    iv = buffer.ReadInt();
                    if (iv != 0)
                        input.keyboardType = iv;
                    if (buffer.ReadBool())
                        input.displayAsPassword = true;
                }
                else
                {
                    buffer.Skip(13);
                }
            }

            if (buffer.version >= 5)
            {
                var sound = buffer.ReadS();
                if (!string.IsNullOrEmpty(sound))
                {
                    var volumeScale = buffer.ReadFloat();
                    displayObject.onClick.Add(() =>
                    {
                        var audioClip = UIPackage.GetItemAssetByURL(sound) as NAudioClip;
                        if (audioClip != null && audioClip.nativeClip != null)
                            Stage.inst.PlayOneShotSound(audioClip.nativeClip, volumeScale);
                    });
                }
                else
                {
                    buffer.Skip(4);
                }
            }
        }
    }
}
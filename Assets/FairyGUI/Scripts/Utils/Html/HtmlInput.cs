using UnityEngine;

namespace FairyGUI.Utils
{
    /// <summary>
    /// </summary>
    public class HtmlInput : IHtmlObject
    {
        public static int defaultBorderSize = 2;
        public static Color defaultBorderColor = ToolSet.ColorFromRGB(0xA9A9A9);
        public static Color defaultBackgroundColor = Color.clear;
        private bool _hidden;

        private RichTextField _owner;

        public HtmlInput()
        {
            textInput = (GTextInput)UIObjectFactory.NewObject(ObjectType.InputText);
            textInput.gameObjectName = "HtmlInput";
            textInput.verticalAlign = VertAlignType.Middle;
        }

        public GTextInput textInput { get; }

        public DisplayObject displayObject => textInput.displayObject;

        public HtmlElement element { get; private set; }

        public float width => _hidden ? 0 : textInput.width;

        public float height => _hidden ? 0 : textInput.height;

        public void Create(RichTextField owner, HtmlElement element)
        {
            _owner = owner;
            this.element = element;

            var type = element.GetString("type");
            if (type != null)
                type = type.ToLower();

            _hidden = type == "hidden";
            if (!_hidden)
            {
                var width = element.GetInt("width", 0);
                var height = element.GetInt("height", 0);
                var borderSize = element.GetInt("border", defaultBorderSize);
                var borderColor = element.GetColor("border-color", defaultBorderColor);
                var backgroundColor = element.GetColor("background-color", defaultBackgroundColor);

                if (width == 0)
                {
                    width = element.space;
                    if (width > _owner.width / 2 || width < 100)
                        width = (int)_owner.width / 2;
                }

                if (height == 0)
                    height = element.format.size + 10;

                textInput.textFormat = element.format;
                textInput.displayAsPassword = type == "password";
                textInput.maxLength = element.GetInt("maxlength", int.MaxValue);
                textInput.border = borderSize;
                textInput.borderColor = borderColor;
                textInput.backgroundColor = backgroundColor;
                textInput.SetSize(width, height);
            }

            textInput.text = element.GetString("value");
        }

        public void SetPosition(float x, float y)
        {
            if (!_hidden)
                textInput.SetXY(x, y);
        }

        public void Add()
        {
            if (!_hidden)
                _owner.AddChild(textInput.displayObject);
        }

        public void Remove()
        {
            if (!_hidden && textInput.displayObject.parent != null)
                _owner.RemoveChild(textInput.displayObject);
        }

        public void Release()
        {
            textInput.RemoveEventListeners();
            textInput.text = null;

            _owner = null;
            element = null;
        }

        public void Dispose()
        {
            textInput.Dispose();
        }
    }
}
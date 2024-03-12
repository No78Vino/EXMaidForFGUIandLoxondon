using UnityEngine;

namespace FairyGUI.Utils
{
    /// <summary>
    /// </summary>
    public class HtmlSelect : IHtmlObject
    {
        public const string CHANGED_EVENT = "OnHtmlSelectChanged";

        public static string resource;
        private readonly EventCallback0 _changeHandler;

        private RichTextField _owner;

        public HtmlSelect()
        {
            if (resource != null)
            {
                comboBox = UIPackage.CreateObjectFromURL(resource).asComboBox;
                _changeHandler = () => { _owner.DispatchEvent(CHANGED_EVENT, null, this); };
            }
            else
            {
                Debug.LogWarning("FairyGUI: Set HtmlSelect.resource first");
            }
        }

        public GComboBox comboBox { get; }

        public DisplayObject displayObject => comboBox.displayObject;

        public HtmlElement element { get; private set; }

        public float width => comboBox != null ? comboBox.width : 0;

        public float height => comboBox != null ? comboBox.height : 0;

        public void Create(RichTextField owner, HtmlElement element)
        {
            _owner = owner;
            this.element = element;

            if (comboBox == null)
                return;

            comboBox.onChanged.Add(_changeHandler);

            var width = element.GetInt("width", comboBox.sourceWidth);
            var height = element.GetInt("height", comboBox.sourceHeight);
            comboBox.SetSize(width, height);
            comboBox.items = (string[])element.Get("items");
            comboBox.values = (string[])element.Get("values");
            comboBox.value = element.GetString("value");
        }

        public void SetPosition(float x, float y)
        {
            if (comboBox != null)
                comboBox.SetXY(x, y);
        }

        public void Add()
        {
            if (comboBox != null)
                _owner.AddChild(comboBox.displayObject);
        }

        public void Remove()
        {
            if (comboBox != null && comboBox.displayObject.parent != null)
                _owner.RemoveChild(comboBox.displayObject);
        }

        public void Release()
        {
            if (comboBox != null)
                comboBox.RemoveEventListeners();

            _owner = null;
            element = null;
        }

        public void Dispose()
        {
            if (comboBox != null)
                comboBox.Dispose();
        }
    }
}
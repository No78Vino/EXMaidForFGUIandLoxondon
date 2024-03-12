namespace FairyGUI.Utils
{
    /// <summary>
    /// </summary>
    public class HtmlLink : IHtmlObject
    {
        private readonly EventCallback1 _clickHandler;
        private RichTextField _owner;
        private readonly EventCallback0 _rolloutHandler;
        private readonly EventCallback1 _rolloverHandler;
        private SelectionShape _shape;

        public HtmlLink()
        {
            _shape = new SelectionShape();
            _shape.gameObject.name = "HtmlLink";
            _shape.cursor = "text-link";

            _clickHandler = context => { _owner.BubbleEvent("onClickLink", element.GetString("href")); };
            _rolloverHandler = context =>
            {
                if (_owner.htmlParseOptions.linkHoverBgColor.a > 0)
                    _shape.color = _owner.htmlParseOptions.linkHoverBgColor;
            };
            _rolloutHandler = () =>
            {
                if (_owner.htmlParseOptions.linkHoverBgColor.a > 0)
                    _shape.color = _owner.htmlParseOptions.linkBgColor;
            };
        }

        public DisplayObject displayObject => _shape;

        public HtmlElement element { get; private set; }

        public float width => 0;

        public float height => 0;

        public void Create(RichTextField owner, HtmlElement element)
        {
            _owner = owner;
            this.element = element;
            _shape.onClick.Add(_clickHandler);
            _shape.onRollOver.Add(_rolloverHandler);
            _shape.onRollOut.Add(_rolloutHandler);
            _shape.color = _owner.htmlParseOptions.linkBgColor;
        }

        public void SetPosition(float x, float y)
        {
            _shape.SetXY(x, y);
        }

        public void Add()
        {
            //add below _shape
            _owner.AddChildAt(_shape, 0);
        }

        public void Remove()
        {
            if (_shape.parent != null)
                _owner.RemoveChild(_shape);
        }

        public void Release()
        {
            _shape.RemoveEventListeners();

            _owner = null;
            element = null;
        }

        public void Dispose()
        {
            _shape.Dispose();
            _shape = null;
        }

        public void SetArea(int startLine, float startCharX, int endLine, float endCharX)
        {
            if (startLine == endLine && startCharX > endCharX)
            {
                var tmp = startCharX;
                startCharX = endCharX;
                endCharX = tmp;
            }

            _shape.rects.Clear();
            _owner.textField.GetLinesShape(startLine, startCharX, endLine, endCharX, true, _shape.rects);
            _shape.Refresh();
        }
    }
}
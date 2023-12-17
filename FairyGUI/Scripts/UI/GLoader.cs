using System;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    ///     GLoader class
    /// </summary>
    public class GLoader : GObject, IAnimationGear, IColorGear
    {
        /// <summary>
        ///     Display an error sign if the loader fails to load the content.
        ///     UIConfig.loaderErrorSign muse be set.
        /// </summary>
        public bool showErrorSign;

        private string _url;
        private AlignType _align;
        private VertAlignType _verticalAlign;
        private bool _autoSize;
        private FillType _fill;
        private bool _shrinkOnly;
        private bool _updatingLayout;
        private PackageItem _contentItem;
        private readonly Action<NTexture> _reloadDelegate;

        private GObject _errorSign;

#if FAIRYGUI_PUERTS
        public Action __loadExternal;
        public Action<NTexture> __freeExternal;
#endif

        public GLoader()
        {
            _url = string.Empty;
            _align = AlignType.Left;
            _verticalAlign = VertAlignType.Top;
            showErrorSign = true;
            _reloadDelegate = OnExternalReload;
        }

        protected override void CreateDisplayObject()
        {
            displayObject = new Container("GLoader");
            displayObject.gOwner = this;
            movieClip = new MovieClip();
            ((Container)displayObject).AddChild(movieClip);
            ((Container)displayObject).opaque = true;
        }

        public override void Dispose()
        {
            if (_disposed) return;

            if (movieClip.texture != null)
                if (_contentItem == null)
                {
                    movieClip.texture.onSizeChanged -= _reloadDelegate;
                    try
                    {
                        FreeExternal(movieClip.texture);
                    }
                    catch (Exception err)
                    {
                        Debug.LogWarning(err);
                    }
                }

            if (_errorSign != null)
                _errorSign.Dispose();
            if (component != null)
                component.Dispose();
            movieClip.Dispose();

            base.Dispose();
        }

        /// <summary>
        /// </summary>
        public string url
        {
            get => _url;
            set
            {
                if (_url == value)
                    return;

                ClearContent();
                _url = value;
                LoadContent();
                UpdateGear(7);
            }
        }

        public override string icon
        {
            get => _url;
            set => url = value;
        }

        /// <summary>
        /// </summary>
        public AlignType align
        {
            get => _align;
            set
            {
                if (_align != value)
                {
                    _align = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// </summary>
        public VertAlignType verticalAlign
        {
            get => _verticalAlign;
            set
            {
                if (_verticalAlign != value)
                {
                    _verticalAlign = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// </summary>
        public FillType fill
        {
            get => _fill;
            set
            {
                if (_fill != value)
                {
                    _fill = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// </summary>
        public bool shrinkOnly
        {
            get => _shrinkOnly;
            set
            {
                if (_shrinkOnly != value)
                {
                    _shrinkOnly = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// </summary>
        public bool autoSize
        {
            get => _autoSize;
            set
            {
                if (_autoSize != value)
                {
                    _autoSize = value;
                    UpdateLayout();
                }
            }
        }

        /// <summary>
        /// </summary>
        public bool playing
        {
            get => movieClip.playing;
            set
            {
                movieClip.playing = value;
                UpdateGear(5);
            }
        }

        /// <summary>
        /// </summary>
        public int frame
        {
            get => movieClip.frame;
            set
            {
                movieClip.frame = value;
                UpdateGear(5);
            }
        }

        /// <summary>
        /// </summary>
        public float timeScale
        {
            get => movieClip.timeScale;
            set => movieClip.timeScale = value;
        }

        /// <summary>
        /// </summary>
        public bool ignoreEngineTimeScale
        {
            get => movieClip.ignoreEngineTimeScale;
            set => movieClip.ignoreEngineTimeScale = value;
        }

        /// <summary>
        /// </summary>
        /// <param name="time"></param>
        public void Advance(float time)
        {
            movieClip.Advance(time);
        }

        /// <summary>
        /// </summary>
        public Material material
        {
            get => movieClip.material;
            set => movieClip.material = value;
        }

        /// <summary>
        /// </summary>
        public string shader
        {
            get => movieClip.shader;
            set => movieClip.shader = value;
        }

        /// <summary>
        /// </summary>
        public Color color
        {
            get => movieClip.color;
            set
            {
                if (movieClip.color != value)
                {
                    movieClip.color = value;
                    UpdateGear(4);
                }
            }
        }

        /// <summary>
        /// </summary>
        public FillMethod fillMethod
        {
            get => movieClip.fillMethod;
            set => movieClip.fillMethod = value;
        }

        /// <summary>
        /// </summary>
        public int fillOrigin
        {
            get => movieClip.fillOrigin;
            set => movieClip.fillOrigin = value;
        }

        /// <summary>
        /// </summary>
        public bool fillClockwise
        {
            get => movieClip.fillClockwise;
            set => movieClip.fillClockwise = value;
        }

        /// <summary>
        /// </summary>
        public float fillAmount
        {
            get => movieClip.fillAmount;
            set => movieClip.fillAmount = value;
        }

        /// <summary>
        /// </summary>
        public Image image => movieClip;

        /// <summary>
        /// </summary>
        public MovieClip movieClip { get; private set; }

        /// <summary>
        /// </summary>
        public GComponent component { get; private set; }

        /// <summary>
        /// </summary>
        public NTexture texture
        {
            get => movieClip.texture;

            set
            {
                url = null;

                movieClip.texture = value;
                if (value != null)
                {
                    sourceWidth = value.width;
                    sourceHeight = value.height;
                }
                else
                {
                    sourceWidth = sourceHeight = 0;
                }

                UpdateLayout();
            }
        }

        public override IFilter filter
        {
            get => movieClip.filter;
            set => movieClip.filter = value;
        }

        public override BlendMode blendMode
        {
            get => movieClip.blendMode;
            set => movieClip.blendMode = value;
        }

        /// <summary>
        /// </summary>
        protected void LoadContent()
        {
            ClearContent();

            if (string.IsNullOrEmpty(_url))
                return;

            if (_url.StartsWith(UIPackage.URL_PREFIX))
                LoadFromPackage(_url);
            else
                LoadExternal();
        }

        protected void LoadFromPackage(string itemURL)
        {
            _contentItem = UIPackage.GetItemByURL(itemURL);

            if (_contentItem != null)
            {
                _contentItem = _contentItem.getBranch();
                sourceWidth = _contentItem.width;
                sourceHeight = _contentItem.height;
                _contentItem = _contentItem.getHighResolution();
                _contentItem.Load();

                if (_contentItem.type == PackageItemType.Image)
                {
                    movieClip.texture = _contentItem.texture;
                    movieClip.textureScale = new Vector2(_contentItem.width / (float)sourceWidth,
                        _contentItem.height / (float)sourceHeight);
                    movieClip.scale9Grid = _contentItem.scale9Grid;
                    movieClip.scaleByTile = _contentItem.scaleByTile;
                    movieClip.tileGridIndice = _contentItem.tileGridIndice;

                    UpdateLayout();
                }
                else if (_contentItem.type == PackageItemType.MovieClip)
                {
                    movieClip.interval = _contentItem.interval;
                    movieClip.swing = _contentItem.swing;
                    movieClip.repeatDelay = _contentItem.repeatDelay;
                    movieClip.frames = _contentItem.frames;

                    UpdateLayout();
                }
                else if (_contentItem.type == PackageItemType.Component)
                {
                    var obj = UIPackage.CreateObjectFromURL(itemURL);
                    if (obj == null)
                    {
                        SetErrorState();
                    }
                    else if (!(obj is GComponent))
                    {
                        obj.Dispose();
                        SetErrorState();
                    }
                    else
                    {
                        component = (GComponent)obj;
                        ((Container)displayObject).AddChild(component.displayObject);
                        UpdateLayout();
                    }
                }
                else
                {
                    if (_autoSize)
                        SetSize(_contentItem.width, _contentItem.height);

                    SetErrorState();

                    Debug.LogWarning("Unsupported type of GLoader: " + _contentItem.type);
                }
            }
            else
            {
                SetErrorState();
            }
        }

        protected virtual void LoadExternal()
        {
#if FAIRYGUI_PUERTS
            if (__loadExternal != null) {
                __loadExternal();
                return;
            }
#endif
            var tex = (Texture2D)Resources.Load(_url, typeof(Texture2D));
            if (tex != null)
                onExternalLoadSuccess(new NTexture(tex));
            else
                onExternalLoadFailed();
        }

        protected virtual void FreeExternal(NTexture texture)
        {
#if FAIRYGUI_PUERTS
            if (__freeExternal != null) {
                __freeExternal(texture);
                return;
            }
#endif
        }

        public void onExternalLoadSuccess(NTexture texture)
        {
            movieClip.texture = texture;
            sourceWidth = texture.width;
            sourceHeight = texture.height;
            movieClip.scale9Grid = null;
            movieClip.scaleByTile = false;
            texture.onSizeChanged += _reloadDelegate;
            UpdateLayout();
        }

        public void onExternalLoadFailed()
        {
            SetErrorState();
        }

        private void OnExternalReload(NTexture texture)
        {
            sourceWidth = texture.width;
            sourceHeight = texture.height;
            UpdateLayout();
        }

        private void SetErrorState()
        {
            if (!showErrorSign || !Application.isPlaying)
                return;

            if (_errorSign == null)
            {
                if (UIConfig.loaderErrorSign != null)
                    _errorSign = UIPackage.CreateObjectFromURL(UIConfig.loaderErrorSign);
                else
                    return;
            }

            if (_errorSign != null)
            {
                _errorSign.SetSize(width, height);
                ((Container)displayObject).AddChild(_errorSign.displayObject);
            }
        }

        protected void ClearErrorState()
        {
            if (_errorSign != null && _errorSign.displayObject.parent != null)
                ((Container)displayObject).RemoveChild(_errorSign.displayObject);
        }

        protected void UpdateLayout()
        {
            if (component == null && movieClip.texture == null && movieClip.frames == null)
            {
                if (_autoSize)
                {
                    _updatingLayout = true;
                    SetSize(50, 30);
                    _updatingLayout = false;
                }

                return;
            }

            float contentWidth = sourceWidth;
            float contentHeight = sourceHeight;

            if (_autoSize)
            {
                _updatingLayout = true;
                if (contentWidth == 0)
                    contentWidth = 50;
                if (contentHeight == 0)
                    contentHeight = 30;
                SetSize(contentWidth, contentHeight);

                _updatingLayout = false;

                if (_width == contentWidth && _height == contentHeight)
                {
                    if (component != null)
                    {
                        component.SetXY(0, 0);
                        component.SetScale(1, 1);
                    }
                    else
                    {
                        movieClip.SetXY(0, 0);
                        movieClip.SetSize(contentWidth, contentHeight);
                    }

                    InvalidateBatchingState();
                    return;
                }
                //如果不相等，可能是由于大小限制造成的，要后续处理
            }

            float sx = 1, sy = 1;
            if (_fill != FillType.None)
            {
                sx = width / sourceWidth;
                sy = height / sourceHeight;

                if (sx != 1 || sy != 1)
                {
                    if (_fill == FillType.ScaleMatchHeight)
                    {
                        sx = sy;
                    }
                    else if (_fill == FillType.ScaleMatchWidth)
                    {
                        sy = sx;
                    }
                    else if (_fill == FillType.Scale)
                    {
                        if (sx > sy)
                            sx = sy;
                        else
                            sy = sx;
                    }
                    else if (_fill == FillType.ScaleNoBorder)
                    {
                        if (sx > sy)
                            sy = sx;
                        else
                            sx = sy;
                    }

                    if (_shrinkOnly)
                    {
                        if (sx > 1)
                            sx = 1;
                        if (sy > 1)
                            sy = 1;
                    }

                    contentWidth = sourceWidth * sx;
                    contentHeight = sourceHeight * sy;
                }
            }

            if (component != null)
                component.SetScale(sx, sy);
            else
                movieClip.size = new Vector2(contentWidth, contentHeight);

            float nx;
            float ny;
            if (_align == AlignType.Center)
                nx = (width - contentWidth) / 2;
            else if (_align == AlignType.Right)
                nx = width - contentWidth;
            else
                nx = 0;
            if (_verticalAlign == VertAlignType.Middle)
                ny = (height - contentHeight) / 2;
            else if (_verticalAlign == VertAlignType.Bottom)
                ny = height - contentHeight;
            else
                ny = 0;
            if (component != null)
                component.SetXY(nx, ny);
            else
                movieClip.SetXY(nx, ny);

            InvalidateBatchingState();
        }

        private void ClearContent()
        {
            ClearErrorState();

            if (movieClip.texture != null)
            {
                if (_contentItem == null)
                {
                    movieClip.texture.onSizeChanged -= _reloadDelegate;
                    FreeExternal(movieClip.texture);
                }

                movieClip.texture = null;
            }

            movieClip.frames = null;

            if (component != null)
            {
                component.Dispose();
                component = null;
            }

            _contentItem = null;
        }

        protected override void HandleSizeChanged()
        {
            base.HandleSizeChanged();

            if (!_updatingLayout)
                UpdateLayout();
        }

        public override void Setup_BeforeAdd(ByteBuffer buffer, int beginPos)
        {
            base.Setup_BeforeAdd(buffer, beginPos);

            buffer.Seek(beginPos, 5);

            _url = buffer.ReadS();
            _align = (AlignType)buffer.ReadByte();
            _verticalAlign = (VertAlignType)buffer.ReadByte();
            _fill = (FillType)buffer.ReadByte();
            _shrinkOnly = buffer.ReadBool();
            _autoSize = buffer.ReadBool();
            showErrorSign = buffer.ReadBool();
            movieClip.playing = buffer.ReadBool();
            movieClip.frame = buffer.ReadInt();

            if (buffer.ReadBool())
                movieClip.color = buffer.ReadColor();
            movieClip.fillMethod = (FillMethod)buffer.ReadByte();
            if (movieClip.fillMethod != FillMethod.None)
            {
                movieClip.fillOrigin = buffer.ReadByte();
                movieClip.fillClockwise = buffer.ReadBool();
                movieClip.fillAmount = buffer.ReadFloat();
            }

            if (!string.IsNullOrEmpty(_url))
                LoadContent();
        }
    }
}
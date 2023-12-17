using FairyGUI.Utils;

namespace FairyGUI
{
    /// <summary>
    ///     Gear is a connection between object and controller.
    /// </summary>
    public abstract class GearBase
    {
        public static bool disableAllTweenEffect = false;
        protected Controller _controller;

        protected GObject _owner;
        protected GearTweenConfig _tweenConfig;

        public GearBase(GObject owner)
        {
            _owner = owner;
        }

        /// <summary>
        ///     Controller object.
        /// </summary>
        public Controller controller
        {
            get => _controller;

            set
            {
                if (value != _controller)
                {
                    _controller = value;
                    if (_controller != null)
                        Init();
                }
            }
        }

        public GearTweenConfig tweenConfig
        {
            get
            {
                if (_tweenConfig == null)
                    _tweenConfig = new GearTweenConfig();
                return _tweenConfig;
            }
        }

        public void Dispose()
        {
            if (_tweenConfig != null && _tweenConfig._tweener != null)
            {
                _tweenConfig._tweener.Kill();
                _tweenConfig._tweener = null;
            }
        }

        public void Setup(ByteBuffer buffer)
        {
            _controller = _owner.parent.GetControllerAt(buffer.ReadShort());
            Init();

            int cnt = buffer.ReadShort();
            if (this is GearDisplay)
            {
                ((GearDisplay)this).pages = buffer.ReadSArray(cnt);
            }
            else if (this is GearDisplay2)
            {
                ((GearDisplay2)this).pages = buffer.ReadSArray(cnt);
            }
            else
            {
                for (var i = 0; i < cnt; i++)
                {
                    var page = buffer.ReadS();
                    if (page == null)
                        continue;

                    AddStatus(page, buffer);
                }

                if (buffer.ReadBool())
                    AddStatus(null, buffer);
            }

            if (buffer.ReadBool())
            {
                _tweenConfig = new GearTweenConfig();
                _tweenConfig.easeType = (EaseType)buffer.ReadByte();
                _tweenConfig.duration = buffer.ReadFloat();
                _tweenConfig.delay = buffer.ReadFloat();
            }

            if (buffer.version >= 2)
            {
                if (this is GearXY)
                {
                    if (buffer.ReadBool())
                    {
                        ((GearXY)this).positionsInPercent = true;
                        for (var i = 0; i < cnt; i++)
                        {
                            var page = buffer.ReadS();
                            if (page == null)
                                continue;

                            ((GearXY)this).AddExtStatus(page, buffer);
                        }

                        if (buffer.ReadBool())
                            ((GearXY)this).AddExtStatus(null, buffer);
                    }
                }
                else if (this is GearDisplay2)
                {
                    ((GearDisplay2)this).condition = buffer.ReadByte();
                }
            }

            if (buffer.version >= 4 && _tweenConfig != null && _tweenConfig.easeType == EaseType.Custom)
            {
                _tweenConfig.customEase = new CustomEase();
                _tweenConfig.customEase.Create(buffer.ReadPath());
            }

            if (buffer.version >= 6)
                if (this is GearAnimation)
                {
                    for (var i = 0; i < cnt; i++)
                    {
                        var page = buffer.ReadS();
                        if (page == null)
                            continue;

                        ((GearAnimation)this).AddExtStatus(page, buffer);
                    }

                    if (buffer.ReadBool())
                        ((GearAnimation)this).AddExtStatus(null, buffer);
                }
        }

        public virtual void UpdateFromRelations(float dx, float dy)
        {
        }

        protected abstract void AddStatus(string pageId, ByteBuffer buffer);
        protected abstract void Init();

        /// <summary>
        ///     Call when controller active page changed.
        /// </summary>
        public abstract void Apply();

        /// <summary>
        ///     Call when object's properties changed.
        /// </summary>
        public abstract void UpdateState();
    }

    public class GearTweenConfig
    {
        internal uint _displayLockToken;
        internal GTweener _tweener;

        /// <summary>
        /// </summary>
        public CustomEase customEase;

        /// <summary>
        ///     Tween delay in seconds.
        /// </summary>
        public float delay;

        /// <summary>
        ///     Tween duration in seconds.
        /// </summary>
        public float duration;

        /// <summary>
        ///     Ease type.
        /// </summary>
        public EaseType easeType;

        /// <summary>
        ///     Use tween to apply change.
        /// </summary>
        public bool tween;

        public GearTweenConfig()
        {
            tween = true;
            easeType = EaseType.QuadOut;
            duration = 0.3f;
            delay = 0;
        }
    }
}
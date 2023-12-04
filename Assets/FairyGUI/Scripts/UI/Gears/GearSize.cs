using System.Collections.Generic;
using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    internal class GearSizeValue
    {
        public float height;
        public float scaleX;
        public float scaleY;
        public float width;

        public GearSizeValue(float width, float height, float scaleX, float scaleY)
        {
            this.width = width;
            this.height = height;
            this.scaleX = scaleX;
            this.scaleY = scaleY;
        }
    }

    /// <summary>
    ///     Gear is a connection between object and controller.
    /// </summary>
    public class GearSize : GearBase, ITweenListener
    {
        private GearSizeValue _default;
        private Dictionary<string, GearSizeValue> _storage;

        public GearSize(GObject owner)
            : base(owner)
        {
        }

        public void OnTweenStart(GTweener tweener)
        {
        }

        public void OnTweenUpdate(GTweener tweener)
        {
            _owner._gearLocked = true;
            var flag = (int)tweener.userData;
            if ((flag & 1) != 0)
                _owner.SetSize(tweener.value.x, tweener.value.y, _owner.CheckGearController(1, _controller));
            if ((flag & 2) != 0)
                _owner.SetScale(tweener.value.z, tweener.value.w);
            _owner._gearLocked = false;

            _owner.InvalidateBatchingState();
        }

        public void OnTweenComplete(GTweener tweener)
        {
            _tweenConfig._tweener = null;
            if (_tweenConfig._displayLockToken != 0)
            {
                _owner.ReleaseDisplayLock(_tweenConfig._displayLockToken);
                _tweenConfig._displayLockToken = 0;
            }

            _owner.DispatchEvent("onGearStop", this);
        }

        protected override void Init()
        {
            _default = new GearSizeValue(_owner.width, _owner.height, _owner.scaleX, _owner.scaleY);
            _storage = new Dictionary<string, GearSizeValue>();
        }

        protected override void AddStatus(string pageId, ByteBuffer buffer)
        {
            GearSizeValue gv;
            if (pageId == null)
            {
                gv = _default;
            }
            else
            {
                gv = new GearSizeValue(0, 0, 1, 1);
                _storage[pageId] = gv;
            }

            gv.width = buffer.ReadInt();
            gv.height = buffer.ReadInt();
            gv.scaleX = buffer.ReadFloat();
            gv.scaleY = buffer.ReadFloat();
        }

        public override void Apply()
        {
            GearSizeValue gv;
            if (!_storage.TryGetValue(_controller.selectedPageId, out gv))
                gv = _default;

            if (_tweenConfig != null && _tweenConfig.tween && UIPackage._constructing == 0 && !disableAllTweenEffect)
            {
                if (_tweenConfig._tweener != null)
                {
                    if (_tweenConfig._tweener.endValue.x != gv.width || _tweenConfig._tweener.endValue.y != gv.height
                                                                     || _tweenConfig._tweener.endValue.z != gv.scaleX ||
                                                                     _tweenConfig._tweener.endValue.w != gv.scaleY)
                    {
                        _tweenConfig._tweener.Kill(true);
                        _tweenConfig._tweener = null;
                    }
                    else
                    {
                        return;
                    }
                }

                var a = gv.width != _owner.width || gv.height != _owner.height;
                var b = gv.scaleX != _owner.scaleX || gv.scaleY != _owner.scaleY;
                if (a || b)
                {
                    if (_owner.CheckGearController(0, _controller))
                        _tweenConfig._displayLockToken = _owner.AddDisplayLock();

                    _tweenConfig._tweener = GTween.To(
                            new Vector4(_owner.width, _owner.height, _owner.scaleX, _owner.scaleY),
                            new Vector4(gv.width, gv.height, gv.scaleX, gv.scaleY), _tweenConfig.duration)
                        .SetDelay(_tweenConfig.delay)
                        .SetEase(_tweenConfig.easeType, _tweenConfig.customEase)
                        .SetUserData((a ? 1 : 0) + (b ? 2 : 0))
                        .SetTarget(this)
                        .SetListener(this);
                }
            }
            else
            {
                _owner._gearLocked = true;
                _owner.SetSize(gv.width, gv.height, _owner.CheckGearController(1, _controller));
                _owner.SetScale(gv.scaleX, gv.scaleY);
                _owner._gearLocked = false;
            }
        }

        public override void UpdateState()
        {
            GearSizeValue gv;
            if (!_storage.TryGetValue(_controller.selectedPageId, out gv))
            {
                _storage[_controller.selectedPageId] =
                    new GearSizeValue(_owner.width, _owner.height, _owner.scaleX, _owner.scaleY);
            }
            else
            {
                gv.width = _owner.width;
                gv.height = _owner.height;
                gv.scaleX = _owner.scaleX;
                gv.scaleY = _owner.scaleY;
            }
        }

        public override void UpdateFromRelations(float dx, float dy)
        {
            if (_controller != null && _storage != null)
            {
                foreach (var gv in _storage.Values)
                {
                    gv.width += dx;
                    gv.height += dy;
                }

                _default.width += dx;
                _default.height += dy;

                UpdateState();
            }
        }
    }
}
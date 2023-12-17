using System.Collections.Generic;
using FairyGUI.Utils;

namespace FairyGUI
{
    internal class GearAnimationValue
    {
        public string animationName;
        public int frame;
        public bool playing;
        public string skinName;

        public GearAnimationValue(bool playing, int frame)
        {
            this.playing = playing;
            this.frame = frame;
        }
    }

    /// <summary>
    ///     Gear is a connection between object and controller.
    /// </summary>
    public class GearAnimation : GearBase
    {
        private GearAnimationValue _default;
        private Dictionary<string, GearAnimationValue> _storage;

        public GearAnimation(GObject owner)
            : base(owner)
        {
        }

        protected override void Init()
        {
            _default = new GearAnimationValue(((IAnimationGear)_owner).playing, ((IAnimationGear)_owner).frame);
            if (_owner is GLoader3D)
            {
                _default.animationName = ((GLoader3D)_owner).animationName;
                _default.skinName = ((GLoader3D)_owner).skinName;
            }

            _storage = new Dictionary<string, GearAnimationValue>();
        }

        protected override void AddStatus(string pageId, ByteBuffer buffer)
        {
            GearAnimationValue gv;
            if (pageId == null)
            {
                gv = _default;
            }
            else
            {
                gv = new GearAnimationValue(false, 0);
                _storage[pageId] = gv;
            }

            gv.playing = buffer.ReadBool();
            gv.frame = buffer.ReadInt();
        }

        public void AddExtStatus(string pageId, ByteBuffer buffer)
        {
            GearAnimationValue gv;
            if (pageId == null)
                gv = _default;
            else
                gv = _storage[pageId];
            gv.animationName = buffer.ReadS();
            gv.skinName = buffer.ReadS();
        }

        public override void Apply()
        {
            _owner._gearLocked = true;

            GearAnimationValue gv;
            if (!_storage.TryGetValue(_controller.selectedPageId, out gv))
                gv = _default;

            var mc = (IAnimationGear)_owner;
            mc.frame = gv.frame;
            mc.playing = gv.playing;
            if (_owner is GLoader3D)
            {
                ((GLoader3D)_owner).animationName = gv.animationName;
                ((GLoader3D)_owner).skinName = gv.skinName;
            }

            _owner._gearLocked = false;
        }

        public override void UpdateState()
        {
            var mc = (IAnimationGear)_owner;
            GearAnimationValue gv;
            if (!_storage.TryGetValue(_controller.selectedPageId, out gv))
            {
                _storage[_controller.selectedPageId] = gv = new GearAnimationValue(mc.playing, mc.frame);
            }
            else
            {
                gv.playing = mc.playing;
                gv.frame = mc.frame;
            }

            if (_owner is GLoader3D)
            {
                gv.animationName = ((GLoader3D)_owner).animationName;
                gv.skinName = ((GLoader3D)_owner).skinName;
            }
        }
    }
}
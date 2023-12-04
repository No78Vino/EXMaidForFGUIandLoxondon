using System.Collections.Generic;
using FairyGUI.Utils;

namespace FairyGUI
{
    /// <summary>
    ///     Gear is a connection between object and controller.
    /// </summary>
    public class GearFontSize : GearBase
    {
        private int _default;
        private Dictionary<string, int> _storage;

        public GearFontSize(GObject owner)
            : base(owner)
        {
        }

        protected override void Init()
        {
            _default = ((GTextField)_owner).textFormat.size;
            _storage = new Dictionary<string, int>();
        }

        protected override void AddStatus(string pageId, ByteBuffer buffer)
        {
            if (pageId == null)
                _default = buffer.ReadInt();
            else
                _storage[pageId] = buffer.ReadInt();
        }

        public override void Apply()
        {
            _owner._gearLocked = true;

            int cv;
            if (!_storage.TryGetValue(_controller.selectedPageId, out cv))
                cv = _default;

            var tf = ((GTextField)_owner).textFormat;
            tf.size = cv;
            ((GTextField)_owner).textFormat = tf;

            _owner._gearLocked = false;
        }

        public override void UpdateState()
        {
            _storage[_controller.selectedPageId] = ((GTextField)_owner).textFormat.size;
        }
    }
}
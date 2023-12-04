using System.Collections.Generic;
using FairyGUI.Utils;

namespace FairyGUI
{
    /// <summary>
    ///     Gear is a connection between object and controller.
    /// </summary>
    public class GearIcon : GearBase
    {
        private string _default;
        private Dictionary<string, string> _storage;

        public GearIcon(GObject owner)
            : base(owner)
        {
        }

        protected override void Init()
        {
            _default = _owner.icon;
            _storage = new Dictionary<string, string>();
        }

        protected override void AddStatus(string pageId, ByteBuffer buffer)
        {
            if (pageId == null)
                _default = buffer.ReadS();
            else
                _storage[pageId] = buffer.ReadS();
        }

        public override void Apply()
        {
            _owner._gearLocked = true;

            string cv;
            if (!_storage.TryGetValue(_controller.selectedPageId, out cv))
                cv = _default;

            _owner.icon = cv;

            _owner._gearLocked = false;
        }

        public override void UpdateState()
        {
            _storage[_controller.selectedPageId] = _owner.icon;
        }
    }
}
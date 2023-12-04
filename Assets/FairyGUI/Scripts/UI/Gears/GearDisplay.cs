using System;
using FairyGUI.Utils;

namespace FairyGUI
{
    /// <summary>
    ///     Gear is a connection between object and controller.
    /// </summary>
    public class GearDisplay : GearBase
    {
        private uint _displayLockToken;

        private int _visible;

        public GearDisplay(GObject owner)
            : base(owner)
        {
            _displayLockToken = 1;
        }

        /// <summary>
        ///     Pages involed in this gear.
        /// </summary>
        public string[] pages { get; set; }

        public bool connected => _controller == null || _visible > 0;

        protected override void AddStatus(string pageId, ByteBuffer buffer)
        {
        }

        protected override void Init()
        {
            pages = null;
        }

        public override void Apply()
        {
            _displayLockToken++;
            if (_displayLockToken == 0)
                _displayLockToken = 1;

            if (pages == null || pages.Length == 0
                              || Array.IndexOf(pages, _controller.selectedPageId) != -1)
                _visible = 1;
            else
                _visible = 0;
        }

        public override void UpdateState()
        {
        }

        public uint AddLock()
        {
            _visible++;
            return _displayLockToken;
        }

        public void ReleaseLock(uint token)
        {
            if (token == _displayLockToken)
                _visible--;
        }
    }
}
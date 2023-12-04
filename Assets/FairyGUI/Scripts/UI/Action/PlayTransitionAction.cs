using FairyGUI.Utils;

namespace FairyGUI
{
    public class PlayTransitionAction : ControllerAction
    {
        private Transition _currentTransition;
        public float delay;
        public int playTimes;
        public bool stopOnExit;
        public string transitionName;

        public PlayTransitionAction()
        {
            playTimes = 1;
            delay = 0;
        }

        protected override void Enter(Controller controller)
        {
            var trans = controller.parent.GetTransition(transitionName);
            if (trans != null)
            {
                if (_currentTransition != null && _currentTransition.playing)
                    trans.ChangePlayTimes(playTimes);
                else
                    trans.Play(playTimes, delay, null);
                _currentTransition = trans;
            }
        }

        protected override void Leave(Controller controller)
        {
            if (stopOnExit && _currentTransition != null)
            {
                _currentTransition.Stop();
                _currentTransition = null;
            }
        }

        public override void Setup(ByteBuffer buffer)
        {
            base.Setup(buffer);

            transitionName = buffer.ReadS();
            playTimes = buffer.ReadInt();
            delay = buffer.ReadFloat();
            stopOnExit = buffer.ReadBool();
        }
    }
}
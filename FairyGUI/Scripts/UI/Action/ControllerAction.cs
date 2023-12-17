using System;
using FairyGUI.Utils;

namespace FairyGUI
{
    public class ControllerAction
    {
        public enum ActionType
        {
            PlayTransition,
            ChangePage
        }

        public string[] fromPage;
        public string[] toPage;

        public static ControllerAction CreateAction(ActionType type)
        {
            switch (type)
            {
                case ActionType.PlayTransition:
                    return new PlayTransitionAction();

                case ActionType.ChangePage:
                    return new ChangePageAction();
            }

            return null;
        }

        public void Run(Controller controller, string prevPage, string curPage)
        {
            if ((fromPage == null || fromPage.Length == 0 || Array.IndexOf(fromPage, prevPage) != -1)
                && (toPage == null || toPage.Length == 0 || Array.IndexOf(toPage, curPage) != -1))
                Enter(controller);
            else
                Leave(controller);
        }

        protected virtual void Enter(Controller controller)
        {
        }

        protected virtual void Leave(Controller controller)
        {
        }

        public virtual void Setup(ByteBuffer buffer)
        {
            int cnt;

            cnt = buffer.ReadShort();
            fromPage = new string[cnt];
            for (var i = 0; i < cnt; i++)
                fromPage[i] = buffer.ReadS();

            cnt = buffer.ReadShort();
            toPage = new string[cnt];
            for (var i = 0; i < cnt; i++)
                toPage[i] = buffer.ReadS();
        }
    }
}
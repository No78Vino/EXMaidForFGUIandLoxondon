using FairyGUI.Utils;

namespace FairyGUI
{
    public class ChangePageAction : ControllerAction
    {
        public string controllerName;
        public string objectId;
        public string targetPage;

        protected override void Enter(Controller controller)
        {
            if (string.IsNullOrEmpty(controllerName))
                return;

            GComponent gcom;
            if (!string.IsNullOrEmpty(objectId))
                gcom = controller.parent.GetChildById(objectId) as GComponent;
            else
                gcom = controller.parent;
            if (gcom != null)
            {
                var cc = gcom.GetController(controllerName);
                if (cc != null && cc != controller && !cc.changing)
                {
                    if (targetPage == "~1")
                    {
                        if (controller.selectedIndex < cc.pageCount)
                            cc.selectedIndex = controller.selectedIndex;
                    }
                    else if (targetPage == "~2")
                    {
                        cc.selectedPage = controller.selectedPage;
                    }
                    else
                    {
                        cc.selectedPageId = targetPage;
                    }
                }
            }
        }

        public override void Setup(ByteBuffer buffer)
        {
            base.Setup(buffer);

            objectId = buffer.ReadS();
            controllerName = buffer.ReadS();
            targetPage = buffer.ReadS();
        }
    }
}
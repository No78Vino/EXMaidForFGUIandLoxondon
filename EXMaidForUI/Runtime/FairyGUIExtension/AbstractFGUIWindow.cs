using EXMaidForUI.Runtime.FairyGUIExtension;
using Loxodon.Framework.Binding.Contexts;
using Loxodon.Framework.Interactivity;
using UnityEngine;

namespace FairyGUI.Extension
{
    public abstract class AbstractFGUIWindow : Window
    {
        private IBindingContext _bindingContext;

        /// <summary>
        ///     是否全屏
        /// </summary>
        protected bool _isFullScreen;

        protected bool _isModal;

        private GButton _modalButton;

        protected string _pkgName;
        protected ViewModelCommon _vm;
        protected string _windowPathName;

        public ViewModelCommon VM => _vm;

        protected IBindingContext bindingContext
        {
            get { return _bindingContext ??= contentPane.displayObject.gameObject.BindingContext(); }
        }

        protected void CreateContentPane(ViewModelCommon vm, string pkgName, string windowName, bool isFullScreen)
        {
            _vm = vm;
            _pkgName = pkgName;
            _windowPathName = windowName;
            _isFullScreen = isFullScreen;
            FairyGUIPackageExtension.LoadPackage(_pkgName);
            contentPane = UIPackage.CreateObject(_pkgName, _windowPathName).asCom;
            if (_isFullScreen) MakeFullScreen();
            bindingContext.DataContext = _vm;

            // 点击不显示顺序影响
            bringToFontOnClick = false;
        }

        protected GObject _ui(string path)
        {
            var arr = path.Split('.');
            var cnt = arr.Length;
            var gcom = contentPane;
            GObject obj = null;
            for (var i = 0; i < cnt; ++i)
            {
                if (arr[i].EndsWith("]"))
                {
                    var listName = arr[i].Substring(0, arr[i].IndexOf('['));
                    obj = gcom.GetChild(listName);
                    if (obj is GList list)
                    {
                        var index = arr[i].Substring(arr[i].IndexOf('[') + 1, arr[i].Length - 2 - listName.Length);
                        if (index == "last")
                        {
                            var actualIdx = list.ItemIndexToChildIndex(list.numItems - 1); // 如果是GList,注意元素索引和子项索引的转换关系
                            obj = actualIdx >= 0 ? list.GetChildAt(actualIdx) : null;
                        }
                        else
                        {
                            if (int.TryParse(index, out var idx))
                            {
                                var actualIdx = list.ItemIndexToChildIndex(idx); // 如果是GList,注意元素索引和子项索引的转换关系
                                obj = actualIdx >= 0 ? list.GetChildAt(actualIdx) : null;
                            }
                            else
                            {
                                obj = null;
                            }
                        }
                    }
                    else
                    {
                        obj = null;
                    }
                }
                else
                {
                    obj = gcom.GetChild(arr[i]);
                }

                if (obj == null) break;
                if (i == cnt - 1) continue;
                if (!(obj is GComponent))
                {
                    obj = null;
                    break;
                }

                gcom = (GComponent)obj;
            }


            if (obj == null)
                Debug.LogError($"[FairyGUI] No Component Path:{path} In WindowComponent:{_windowPathName}.");
            return obj;
        }

        public GObject GetUI(string path)
        {
            return _ui(path);
        }

        protected virtual void Msg_Common(object sender, InteractionEventArgs args)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (args.Context != null)
                Debug.Log($"{GetType()} Msg_Common args.Context = {args.Context}");
#endif
        }

        protected virtual void Msg_Transition(object sender, InteractionEventArgs args)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (args.Context != null)
                Debug.Log($"{GetType()} Msg_Transition args.Context = {args.Context}");
#endif
        }

        protected override void OnShown()
        {
            base.OnShown();
            _vm.OnOpen();
        }

        public virtual void OnDispose()
        {
        }
    }
}
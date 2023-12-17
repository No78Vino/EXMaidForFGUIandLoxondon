using System;
using System.Collections.Generic;
using System.Linq;
using FairyGUI;
using FairyGUI.Extension;
using Framework.Utilities;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Contexts;
using UnityEngine;

namespace EXTool.EXMaid.UI
{
    public interface IEXMaidUI
    {
        void LaunchBindingService(FGUIPackageExtension.OnLoadResource onLoadResourceHandler);

        T LoadWindow<T>() where T : AbstractFGUIWindow;

        void UnloadWindow<T>() where T : AbstractFGUIWindow;

        T OpenWindow<T>() where T : AbstractFGUIWindow;
        
        T VM<T>() where T : ViewModelCommon;
        
        AbstractFGUIWindow Windows(Type type);
        
        AbstractFGUIWindow WindowsWithoutLoad(Type type);

        void AddWorldSpaceUI(GObject obj);
        void RefreshSceneUICanvas(float cameraSize);
    }

    public sealed class EXMaidUI : IEXMaidUI
    {
        private float _secondCount;
        private Dictionary<Type, ViewModelCommon> _vms;
        private Dictionary<Type, AbstractFGUIWindow> _windows;
        private GComponent _worldSpaceUICanvas;
        private Window _worldSpaceUIWindow;

        public void LaunchBindingService(FGUIPackageExtension.OnLoadResource onLoadResourceHandler = null)
        {
            var context = Context.GetApplicationContext();
            var container = context.GetContainer();
            /* Initialize the data binding service */
            var bundle = new BindingServiceBundle(container);
            bundle.Start();
            //初始化支持FairyGUI的数据绑定相关组件，请在BindingServiceBundle启动后执行
            var fairyGUIBindingServiceBundle = new FairyGUIBindingServiceBundle(container);
            fairyGUIBindingServiceBundle.Start();
            
            FGUIPackageExtension.RegisterOnLoadResourceHandler(onLoadResourceHandler);
        }

        public T LoadWindow<T>() where T : AbstractFGUIWindow
        {
            var t = typeof(T);
            var w = Windows(t);
            return w as T;
        }

        public void UnloadWindow<T>() where T : AbstractFGUIWindow
        {
            var w = typeof(T);
            if (!_windows.ContainsKey(w)) return;
            _windows[w].VM.OnUnload();
            _vms.Remove(_windows[w].VM.GetType());

            _windows[w].OnDispose();
            _windows[w].Dispose();
            _windows.Remove(w);
        }

        public T OpenWindow<T>() where T : AbstractFGUIWindow
        {
            var w = LoadWindow<T>();
            w.Show();
            return w;
        }

        public T VM<T>() where T : ViewModelCommon
        {
            var t = typeof(T);
            if (!_vms.ContainsKey(t))
            {
                EXLog.Warning($"[UI] View Model:{t} has not been loaded! Please LOAD it before CALLING.");
                return null;
            }

            var vm = _vms[t];
            return vm as T;
        }

        public AbstractFGUIWindow Windows(Type type)
        {
            if (!_windows.ContainsKey(type))
            {
                _windows.Add(type, Activator.CreateInstance(type) as AbstractFGUIWindow);
                var vm = _windows[type].VM;
                _vms.Add(vm.GetType(), vm);
                vm.OnLoaded();
            }

            return _windows[type];
        }

        public AbstractFGUIWindow WindowsWithoutLoad(Type type)
        {
            if (!_windows.ContainsKey(type)) return null;
            return _windows[type];
        }

        public void AddWorldSpaceUI(GObject obj)
        {
            _worldSpaceUICanvas.AddChild(obj);
        }

        /// <summary>
        ///     世界UI画布位置更新
        /// </summary>
        public void RefreshSceneUICanvas(float cameraSize)
        {
            //缩放
            var uiScale = cameraSize / Camera.main.orthographicSize;
            _worldSpaceUICanvas.SetScale(uiScale, uiScale);
            // 移动
            var gRoot = GRoot.inst;
            var size = gRoot.viewHeight * 0.5f / Camera.main.orthographicSize;
            var localPosition = Camera.main.transform.localPosition;
            _worldSpaceUICanvas.x = gRoot.viewWidth * 0.5f - localPosition.x * size;
            _worldSpaceUICanvas.y = gRoot.viewHeight * 0.5f + localPosition.y * size;
        }

        public void OnServiceStart()
        {
            _windows = new Dictionary<Type, AbstractFGUIWindow>();
            _vms = new Dictionary<Type, ViewModelCommon>();
            CreateWorldSpaceUICanvas();
        }

        public void OnServiceUpdate()
        {
            _secondCount += Time.deltaTime;
            var isSecondUpdate = _secondCount > 1;
            if (_secondCount > 1) _secondCount = 0;
            foreach (var w in _windows.Values)
                if (w.isShowing)
                {
                    w.VM.Update_f();
                    if (isSecondUpdate) w.VM.Update_s();
                }
        }

        public void OnServiceStop()
        {
            UnloadAllWindow();
            _worldSpaceUIWindow.Dispose();
            //UIPackage.RemoveAllPackages();
        }

        ///创建地图UI画布
        private void CreateWorldSpaceUICanvas()
        {
            _worldSpaceUIWindow = new Window();
            _worldSpaceUIWindow.contentPane = new GComponent();
            _worldSpaceUIWindow.contentPane.touchable = true;
            _worldSpaceUIWindow.bringToFontOnClick = false;
            _worldSpaceUIWindow.MakeFullScreen();
            _worldSpaceUIWindow.Show();

            _worldSpaceUICanvas = new GComponent();
            _worldSpaceUICanvas.touchable = true;
            _worldSpaceUICanvas.name = "MapCanvas";
            _worldSpaceUIWindow.contentPane.AddChild(_worldSpaceUICanvas);
        }

        ///世界坐标系 -> 地图UI画布坐标系
        public static Vector3 WorldSpaceToUISpacePosition(Vector3 worldPos, float cameraSize)
        {
            var gRoot = GRoot.inst;
            var size = gRoot.viewHeight * 0.5f / cameraSize;
            return new Vector3(worldPos.x * size + gRoot.x, -worldPos.y * size - gRoot.y, 0);
        }

        private void UnloadAllWindow()
        {
            var listCopy = _windows.Values.ToList();
            foreach (var win in listCopy)
            {
                var w = win.GetType();
                if (!_windows.ContainsKey(w)) return;
                _windows[w].VM.OnUnload();
                _vms.Remove(_windows[w].VM.GetType());

                _windows[w].OnDispose();
                _windows[w].Dispose();
                _windows.Remove(w);
            }
        }
    }
}
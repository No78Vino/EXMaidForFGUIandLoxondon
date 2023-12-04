using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using FairyGUI;
using Loxodon.Framework.Binding;
using Loxodon.Framework.Contexts;
using Loxodon.Framework.Services;
using UnityEngine;
using Object = UnityEngine.Object;

namespace EXTool.EXMaid.UI
{
    public interface IEXMaidUI
    {
        void LaunchBindingService();
        /// <summary>
        ///     加载窗口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T LoadWindow<T>() where T : AbstractFGUIWindow;

        /// <summary>
        ///     卸载窗口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        void UnloadWindow<T>() where T : AbstractFGUIWindow;

        /// <summary>
        ///     打开窗口
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        T OpenWindow<T>() where T : AbstractFGUIWindow;

        /// <summary>
        ///     View Model获取接口
        /// </summary>
        /// <typeparam name="T"> View Model 类</typeparam>
        /// <returns></returns>
        T VM<T>() where T : ViewModelCommon;
        
        /// <summary>
        /// 获取FGUI窗口实例
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        AbstractFGUIWindow Windows(Type type);

        /// <summary>
        /// 获取FGUI窗口实例(不需要加载)
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        AbstractFGUIWindow WindowsWithoutLoad(Type type);
        
        void AddWorldSpaceUI(GObject obj);
        void RefreshSceneUICanvas(float cameraSize);
    }

    public sealed class EXMaidUI : IEXMaidUI
    {
        private float _secondCount;
        private Dictionary<Type, ViewModelCommon> _vms;
        private Dictionary<Type, AbstractFGUIWindow> _windows;
        private Window _worldSpaceUIWindow;
        private GComponent _worldSpaceUICanvas;

        public void LaunchBindingService()
        {
            var context = Context.GetApplicationContext();
            var container = context.GetContainer();
            /* Initialize the data binding service */
            var bundle = new BindingServiceBundle(container);
            bundle.Start();
            //初始化支持FairyGUI的数据绑定相关组件，请在BindingServiceBundle启动后执行
            var fairyGUIBindingServiceBundle = new FairyGUIBindingServiceBundle(container);
            fairyGUIBindingServiceBundle.Start();
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
                Debug.LogWarning($"[UI] View Model:{t} has not been loaded! Please LOAD it before CALLING.");
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
                _windows[type].OnLoaded();
                var vm = _windows[type].VM;
                _vms.Add(vm.GetType(), vm);
                vm.OnLoaded();
            }

            return _windows[type];
        }

        public AbstractFGUIWindow WindowsWithoutLoad(Type type)
        {
            if (!_windows.ContainsKey(type))
            {
                return null;
            }
            return _windows[type];
        }

        public void OnServiceStart()
        {
            _windows = new Dictionary<Type, AbstractFGUIWindow>();
            _vms = new Dictionary<Type, ViewModelCommon>();
            CreateWorldSpaceUICanvas();
        }

        public void OnServiceUpdate()
        {
            _secondCount += UnityEngine.Time.deltaTime;
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
        void CreateWorldSpaceUICanvas()
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
        public static Vector3 WorldSpaceToUISpacePosition(Vector3 worldPos,float cameraSize)
        {
            var gRoot = GRoot.inst;
            float size = (gRoot.viewHeight * 0.5f) / cameraSize;
            return new Vector3(worldPos.x * size + gRoot.x, -worldPos.y * size - gRoot.y, 0);
        }

        public void AddWorldSpaceUI(GObject obj)
        {
            _worldSpaceUICanvas.AddChild(obj);
        }

        /// <summary>
        /// 世界UI画布位置更新
        /// </summary>
        public void RefreshSceneUICanvas(float cameraSize)
        {
            //缩放
            var uiScale = cameraSize / Camera.main.orthographicSize;
            _worldSpaceUICanvas.SetScale(uiScale, uiScale);
            // 移动
            var gRoot = GRoot.inst;
            var size = (gRoot.viewHeight * 0.5f) / Camera.main.orthographicSize;
            var localPosition = Camera.main.transform.localPosition;
            _worldSpaceUICanvas.x = (gRoot.viewWidth * 0.5f - localPosition.x * size);
            _worldSpaceUICanvas.y = (gRoot.viewHeight * 0.5f + localPosition.y * size);
        }
        
         void UnloadAllWindow()
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
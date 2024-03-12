﻿#if FAIRYGUI_TOLUA
using LuaInterface;
#endif

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class EventListener
    {
        private readonly EventBridge _bridge;

        public EventListener(EventDispatcher owner, string type)
        {
            _bridge = owner.GetEventBridge(type);
            this.type = type;
        }

        /// <summary>
        /// </summary>
        public string type { get; }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        public void AddCapture(EventCallback1 callback)
        {
            _bridge.AddCapture(callback);
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveCapture(EventCallback1 callback)
        {
            _bridge.RemoveCapture(callback);
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        public void Add(EventCallback1 callback)
        {
            _bridge.Add(callback);
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        public void Remove(EventCallback1 callback)
        {
            _bridge.Remove(callback);
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
#if FAIRYGUI_TOLUA
        [NoToLua]
#endif
        public void Add(EventCallback0 callback)
        {
            _bridge.Add(callback);
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
#if FAIRYGUI_TOLUA
        [NoToLua]
#endif
        public void Remove(EventCallback0 callback)
        {
            _bridge.Remove(callback);
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
        public void Set(EventCallback1 callback)
        {
            _bridge.Clear();
            if (callback != null)
                _bridge.Add(callback);
        }

        /// <summary>
        /// </summary>
        /// <param name="callback"></param>
#if FAIRYGUI_TOLUA
        [NoToLua]
#endif
        public void Set(EventCallback0 callback)
        {
            _bridge.Clear();
            if (callback != null)
                _bridge.Add(callback);
        }

#if FAIRYGUI_TOLUA
        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <param name="self"></param>
        public void Add(LuaFunction func, LuaTable self)
        {
            _bridge.Add(func, self);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <param name="self"></param>
        public void Add(LuaFunction func, GComponent self)
        {
            _bridge.Add(func, self);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <param name="self"></param>
        public void Remove(LuaFunction func, LuaTable self)
        {
            _bridge.Remove(func, self);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <param name="self"></param>
        public void Remove(LuaFunction func, GComponent self)
        {
            _bridge.Remove(func, self);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <param name="self"></param>
        public void Set(LuaFunction func, LuaTable self)
        {
            _bridge.Clear();
            if (func != null)
                Add(func, self);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="func"></param>
        /// <param name="self"></param>
        public void Set(LuaFunction func, GComponent self)
        {
            _bridge.Clear();
            if (func != null)
                Add(func, self);
        }
#endif

        /// <summary>
        /// </summary>
        public bool isEmpty => !_bridge.owner.hasEventListeners(type);

        /// <summary>
        /// </summary>
        public bool isDispatching => _bridge.owner.isDispatching(type);

        /// <summary>
        /// </summary>
        public void Clear()
        {
            _bridge.Clear();
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool Call()
        {
            return _bridge.owner.InternalDispatchEvent(type, _bridge, null, null);
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool Call(object data)
        {
            return _bridge.owner.InternalDispatchEvent(type, _bridge, data, null);
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool BubbleCall(object data)
        {
            return _bridge.owner.BubbleEvent(type, data);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool BubbleCall()
        {
            return _bridge.owner.BubbleEvent(type, null);
        }

        /// <summary>
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public bool BroadcastCall(object data)
        {
            return _bridge.owner.BroadcastEvent(type, data);
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public bool BroadcastCall()
        {
            return _bridge.owner.BroadcastEvent(type, null);
        }
    }
}
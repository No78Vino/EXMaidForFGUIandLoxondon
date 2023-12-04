using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI.Utils
{
    /// <summary>
    /// </summary>
    public class HtmlPageContext : IHtmlPageContext
    {
        public static HtmlPageContext inst = new();

        private static Transform _poolManager;
        private readonly Stack<IHtmlObject> _buttonPool;
        private readonly Stack<IHtmlObject> _imagePool;
        private readonly Stack<IHtmlObject> _inputPool;
        private readonly Stack<IHtmlObject> _linkPool;
        private readonly Stack<IHtmlObject> _selectPool;

        public HtmlPageContext()
        {
            _imagePool = new Stack<IHtmlObject>();
            _inputPool = new Stack<IHtmlObject>();
            _buttonPool = new Stack<IHtmlObject>();
            _selectPool = new Stack<IHtmlObject>();
            _linkPool = new Stack<IHtmlObject>();

            if (Application.isPlaying && _poolManager == null)
                _poolManager = Stage.inst.CreatePoolManager("HtmlObjectPool");
        }

        public virtual IHtmlObject CreateObject(RichTextField owner, HtmlElement element)
        {
            IHtmlObject ret = null;
            var fromPool = false;
            if (element.type == HtmlElementType.Image)
            {
                if (_imagePool.Count > 0 && _poolManager != null)
                {
                    ret = _imagePool.Pop();
                    fromPool = true;
                }
                else
                {
                    ret = new HtmlImage();
                }
            }
            else if (element.type == HtmlElementType.Link)
            {
                if (_linkPool.Count > 0 && _poolManager != null)
                {
                    ret = _linkPool.Pop();
                    fromPool = true;
                }
                else
                {
                    ret = new HtmlLink();
                }
            }
            else if (element.type == HtmlElementType.Input)
            {
                var type = element.GetString("type");
                if (type != null)
                    type = type.ToLower();
                if (type == "button" || type == "submit")
                {
                    if (_buttonPool.Count > 0 && _poolManager != null)
                    {
                        ret = _buttonPool.Pop();
                        fromPool = true;
                    }
                    else
                    {
                        ret = new HtmlButton();
                    }
                }
                else
                {
                    if (_inputPool.Count > 0 && _poolManager != null)
                    {
                        ret = _inputPool.Pop();
                        fromPool = true;
                    }
                    else
                    {
                        ret = new HtmlInput();
                    }
                }
            }
            else if (element.type == HtmlElementType.Select)
            {
                if (_selectPool.Count > 0 && _poolManager != null)
                {
                    ret = _selectPool.Pop();
                    fromPool = true;
                }
                else
                {
                    ret = new HtmlSelect();
                }
            }

            //Debug.Log("from=" + fromPool);
            if (ret != null)
            {
                //可能已经被GameObject tree deleted了，不再使用
                if (fromPool && ret.displayObject != null && ret.displayObject.isDisposed)
                {
                    ret.Dispose();
                    return CreateObject(owner, element);
                }

                ret.Create(owner, element);
                if (ret.displayObject != null)
                    ret.displayObject.home = owner.cachedTransform;
            }

            return ret;
        }

        public virtual void FreeObject(IHtmlObject obj)
        {
            if (_poolManager == null)
            {
                obj.Dispose();
                return;
            }

            //可能已经被GameObject tree deleted了，不再回收
            if (obj.displayObject != null && obj.displayObject.isDisposed)
            {
                obj.Dispose();
                return;
            }

            obj.Release();
            if (obj is HtmlImage)
                _imagePool.Push(obj);
            else if (obj is HtmlInput)
                _inputPool.Push(obj);
            else if (obj is HtmlButton)
                _buttonPool.Push(obj);
            else if (obj is HtmlLink)
                _linkPool.Push(obj);

            if (obj.displayObject != null)
                obj.displayObject.cachedTransform.SetParent(_poolManager, false);
        }

        public virtual NTexture GetImageTexture(HtmlImage image)
        {
            return null;
        }

        public virtual void FreeImageTexture(HtmlImage image, NTexture texture)
        {
        }
    }
}
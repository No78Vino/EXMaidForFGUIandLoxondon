using System.Collections.Generic;
using UnityEngine;

namespace FairyGUI
{
    internal static class TweenManager
    {
        private static GTweener[] _activeTweens = new GTweener[30];
        private static readonly List<GTweener> _tweenerPool = new(30);
        private static int _totalActiveTweens;
        private static bool _inited;

        internal static GTweener CreateTween()
        {
            if (!_inited)
                Init();

            GTweener tweener;
            var cnt = _tweenerPool.Count;
            if (cnt > 0)
            {
                tweener = _tweenerPool[cnt - 1];
                _tweenerPool.RemoveAt(cnt - 1);
            }
            else
            {
                tweener = new GTweener();
            }

            tweener._Init();
            _activeTweens[_totalActiveTweens++] = tweener;

            if (_totalActiveTweens == _activeTweens.Length)
            {
                var newArray = new GTweener[_activeTweens.Length + Mathf.CeilToInt(_activeTweens.Length * 0.5f)];
                _activeTweens.CopyTo(newArray, 0);
                _activeTweens = newArray;
            }

            return tweener;
        }

        internal static bool IsTweening(object target, TweenPropType propType)
        {
            if (target == null)
                return false;

            var anyType = propType == TweenPropType.None;
            for (var i = 0; i < _totalActiveTweens; i++)
            {
                var tweener = _activeTweens[i];
                if (tweener != null && tweener.target == target && !tweener._killed
                    && (anyType || tweener._propType == propType))
                    return true;
            }

            return false;
        }

        internal static bool KillTweens(object target, TweenPropType propType, bool completed)
        {
            if (target == null)
                return false;

            var flag = false;
            var cnt = _totalActiveTweens;
            var anyType = propType == TweenPropType.None;
            for (var i = 0; i < cnt; i++)
            {
                var tweener = _activeTweens[i];
                if (tweener != null && tweener.target == target && !tweener._killed
                    && (anyType || tweener._propType == propType))
                {
                    tweener.Kill(completed);
                    flag = true;
                }
            }

            return flag;
        }

        internal static GTweener GetTween(object target, TweenPropType propType)
        {
            if (target == null)
                return null;

            var cnt = _totalActiveTweens;
            var anyType = propType == TweenPropType.None;
            for (var i = 0; i < cnt; i++)
            {
                var tweener = _activeTweens[i];
                if (tweener != null && tweener.target == target && !tweener._killed
                    && (anyType || tweener._propType == propType))
                    return tweener;
            }

            return null;
        }

        internal static void Update()
        {
            var cnt = _totalActiveTweens;
            var freePosStart = -1;
            for (var i = 0; i < cnt; i++)
            {
                var tweener = _activeTweens[i];
                if (tweener == null)
                {
                    if (freePosStart == -1)
                        freePosStart = i;
                }
                else if (tweener._killed)
                {
                    tweener._Reset();
                    _tweenerPool.Add(tweener);
                    _activeTweens[i] = null;

                    if (freePosStart == -1)
                        freePosStart = i;
                }
                else
                {
                    if (tweener._target is GObject && ((GObject)tweener._target)._disposed)
                        tweener._killed = true;
                    else if (!tweener._paused)
                        tweener._Update();

                    if (freePosStart != -1)
                    {
                        _activeTweens[freePosStart] = tweener;
                        _activeTweens[i] = null;
                        freePosStart++;
                    }
                }
            }

            if (freePosStart >= 0)
            {
                if (_totalActiveTweens != cnt) //new tweens added
                {
                    var j = cnt;
                    cnt = _totalActiveTweens - cnt;
                    for (var i = 0; i < cnt; i++)
                    {
                        _activeTweens[freePosStart++] = _activeTweens[j];
                        _activeTweens[j] = null;
                        j++;
                    }
                }

                _totalActiveTweens = freePosStart;
            }
        }

        internal static void Clean()
        {
            _tweenerPool.Clear();
        }

        private static void Init()
        {
            _inited = true;
            if (Application.isPlaying)
            {
                var gameObject = new GameObject("[FairyGUI.TweenManager]");
                gameObject.hideFlags = HideFlags.HideInHierarchy;
                gameObject.SetActive(true);
                Object.DontDestroyOnLoad(gameObject);

                gameObject.AddComponent<TweenEngine>();
            }
        }

        private class TweenEngine : MonoBehaviour
        {
            private void Update()
            {
                TweenManager.Update();
            }
        }
    }
}
using System.Collections;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    ///     文字打字效果。先调用Start，然后Print。
    /// </summary>
    public class TypingEffect
    {
        protected Vector3[] _backupVerts;
        protected int _mainLayerStart;
        protected int _mainLayerVertCount;

        protected int _printIndex;
        protected bool _shadow;

        protected bool _started;

        protected bool _stroke;
        protected int _strokeDrawDirs;
        protected int _strokeLayerStart;
        protected TextField _textField;
        protected Vector3[] _vertices;
        protected int _vertIndex;

        /// <summary>
        /// </summary>
        /// <param name="textField"></param>
        public TypingEffect(TextField textField)
        {
            _textField = textField;
            _textField.EnableCharPositionSupport();
        }

        /// <summary>
        /// </summary>
        /// <param name="textField"></param>
        public TypingEffect(GTextField textField)
        {
            if (textField is GRichTextField)
                _textField = ((RichTextField)textField.displayObject).textField;
            else
                _textField = (TextField)textField.displayObject;
            _textField.EnableCharPositionSupport();
        }

        /// <summary>
        ///     总输出次数
        /// </summary>
        public int totalTimes
        {
            get
            {
                var times = 0;
                var charPositions = _textField.charPositions;
                for (var i = 0; i < charPositions.Count - 1; i++)
                    if (charPositions[i].imgIndex > 0) //这是一个图片
                        times++;
                    else if (!char.IsWhiteSpace(_textField.parsedText[i]))
                        times++;
                return times;
            }
        }

        /// <summary>
        ///     开始打字效果。可以重复调用重复启动。
        /// </summary>
        public void Start()
        {
            _textField.graphics.meshModifier -= OnMeshModified;
            _textField.Redraw();
            _textField.graphics.meshModifier += OnMeshModified;

            _stroke = false;
            _shadow = false;
            _strokeDrawDirs = 4;
            _mainLayerStart = 0;
            _mainLayerVertCount = 0;
            _printIndex = 0;
            _vertIndex = 0;
            _started = true;

            var vertCount = _textField.graphics.mesh.vertexCount;
            _backupVerts = _textField.graphics.mesh.vertices;
            if (_vertices == null || _vertices.Length != vertCount)
                _vertices = new Vector3[vertCount];
            var zero = Vector3.zero;
            for (var i = 0; i < vertCount; i++)
                _vertices[i] = zero;
            _textField.graphics.mesh.vertices = _vertices;

            //隐藏所有混排的对象
            if (_textField.richTextField != null)
            {
                var ec = _textField.richTextField.htmlElementCount;
                for (var i = 0; i < ec; i++)
                    _textField.richTextField.ShowHtmlObject(i, false);
            }

            var charCount = _textField.charPositions.Count;
            for (var i = 0; i < charCount; i++)
            {
                var cp = _textField.charPositions[i];
                _mainLayerVertCount += cp.vertCount;
            }

            if (_mainLayerVertCount < vertCount) //说明有描边或者阴影
            {
                var repeat = vertCount / _mainLayerVertCount;
                _stroke = repeat > 2;
                _shadow = repeat % 2 == 0;
                _mainLayerStart = vertCount - vertCount / repeat;
                _strokeLayerStart = _shadow ? vertCount / repeat : 0;
                _strokeDrawDirs = repeat > 8 ? 8 : 4;
            }
        }

        /// <summary>
        ///     输出一个字符。如果已经没有剩余的字符，返回false。
        /// </summary>
        /// <returns></returns>
        public bool Print()
        {
            if (!_started)
                return false;

            TextField.CharPosition cp;
            var charPositions = _textField.charPositions;
            var listCnt = charPositions.Count;

            while (_printIndex < listCnt - 1) //最后一个是占位的，无效的，所以-1
            {
                cp = charPositions[_printIndex++];
                if (cp.vertCount > 0)
                    output(cp.vertCount);
                if (cp.imgIndex > 0) //这是一个图片
                {
                    _textField.richTextField.ShowHtmlObject(cp.imgIndex - 1, true);
                    return true;
                }

                if (!char.IsWhiteSpace(_textField.parsedText[_printIndex - 1]))
                {
                    return true;
                }
            }

            Cancel();
            return false;
        }

        private void output(int vertCount)
        {
            int start, end;

            start = _mainLayerStart + _vertIndex;
            end = start + vertCount;
            for (var i = start; i < end; i++)
                _vertices[i] = _backupVerts[i];

            if (_stroke)
            {
                start = _strokeLayerStart + _vertIndex;
                end = start + vertCount;
                for (var i = start; i < end; i++)
                for (var j = 0; j < _strokeDrawDirs; j++)
                {
                    var k = i + _mainLayerVertCount * j;
                    _vertices[k] = _backupVerts[k];
                }
            }

            if (_shadow)
            {
                start = _vertIndex;
                end = start + vertCount;
                for (var i = start; i < end; i++) _vertices[i] = _backupVerts[i];
            }

            _textField.graphics.mesh.vertices = _vertices;

            _vertIndex += vertCount;
        }

        /// <summary>
        ///     打印的协程。
        /// </summary>
        /// <param name="interval">每个字符输出的时间间隔</param>
        /// <returns></returns>
        public IEnumerator Print(float interval)
        {
            while (Print())
                yield return new WaitForSeconds(interval);
        }

        /// <summary>
        ///     使用固定时间间隔完成整个打印过程。
        /// </summary>
        /// <param name="interval"></param>
        public void PrintAll(float interval)
        {
            Timers.inst.StartCoroutine(Print(interval));
        }

        public void Cancel()
        {
            if (!_started)
                return;

            _started = false;
            _textField.graphics.meshModifier -= OnMeshModified;
            _textField.graphics.SetMeshDirty();
        }

        /// <summary>
        ///     当打字过程中，文本可能会由于字体纹理更改而发生字体重建，要处理这种情况。
        ///     图片对象不需要处理，因为HtmlElement.status里设定的隐藏标志不会因为Mesh更新而被冲掉。
        /// </summary>
        private void OnMeshModified()
        {
            if (_textField.graphics.mesh.vertexCount != _backupVerts.Length) //可能文字都改了
            {
                Cancel();
                return;
            }

            _backupVerts = _textField.graphics.mesh.vertices;

            var vertCount = _vertices.Length;
            var zero = Vector3.zero;
            for (var i = 0; i < vertCount; i++)
                if (_vertices[i] != zero)
                    _vertices[i] = _backupVerts[i];

            _textField.graphics.mesh.vertices = _vertices;
        }
    }
}
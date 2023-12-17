using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FairyGUI
{
    [Flags]
    public enum MaterialFlags
    {
        Clipped = 1,
        SoftClipped = 2,
        StencilTest = 4,
        AlphaMask = 8,
        Grayed = 16,
        ColorFilter = 32
    }

    /// <summary>
    ///     Every texture-shader combination has a MaterialManager.
    /// </summary>
    public class MaterialManager
    {
        private const int internalKeywordsCount = 6;

        private static readonly string[] internalKeywords =
            { "CLIPPED", "SOFT_CLIPPED", null, "ALPHA_MASK", "GRAYED", "COLOR_FILTER" };

        private List<string> _addKeywords;
        private bool _combineTexture;
        private readonly Dictionary<int, List<MaterialRef>> _materials;
        private readonly Shader _shader;

        private readonly NTexture _texture;

        public bool firstMaterialInFrame;

        /// <summary>
        /// </summary>
        /// <param name="texture"></param>
        /// <param name="shader"></param>
        internal MaterialManager(NTexture texture, Shader shader)
        {
            _texture = texture;
            _shader = shader;
            _materials = new Dictionary<int, List<MaterialRef>>();
            _combineTexture = texture.alphaTexture != null;
        }

        public event Action<Material> onCreateNewMaterial;

        /// <summary>
        /// </summary>
        /// <param name="keywords"></param>
        /// <returns></returns>
        public int GetFlagsByKeywords(IList<string> keywords)
        {
            if (_addKeywords == null)
                _addKeywords = new List<string>();

            var flags = 0;
            for (var i = 0; i < keywords.Count; i++)
            {
                var s = keywords[i];
                if (string.IsNullOrEmpty(s))
                    continue;
                var j = _addKeywords.IndexOf(s);
                if (j == -1)
                {
                    j = _addKeywords.Count;
                    _addKeywords.Add(s);
                }

                flags += 1 << (j + internalKeywordsCount);
            }

            return flags;
        }

        /// <summary>
        /// </summary>
        /// <param name="flags"></param>
        /// <param name="blendMode"></param>
        /// <param name="group"></param>
        /// <returns></returns>
        public Material GetMaterial(int flags, BlendMode blendMode, uint group)
        {
            if (blendMode != BlendMode.Normal && BlendModeUtils.Factors[(int)blendMode].pma)
                flags |= (int)MaterialFlags.ColorFilter;

            List<MaterialRef> items;
            if (!_materials.TryGetValue(flags, out items))
            {
                items = new List<MaterialRef>();
                _materials[flags] = items;
            }

            var frameId = Time.frameCount;
            var cnt = items.Count;
            MaterialRef result = null;
            for (var i = 0; i < cnt; i++)
            {
                var item = items[i];

                if (item.group == group && item.blendMode == blendMode)
                {
                    if (item.frame != frameId)
                    {
                        firstMaterialInFrame = true;
                        item.frame = frameId;
                    }
                    else
                    {
                        firstMaterialInFrame = false;
                    }

                    if (_combineTexture)
                        item.material.SetTexture(ShaderConfig.ID_AlphaTex, _texture.alphaTexture);

                    return item.material;
                }

                if (result == null &&
                    (item.frame > frameId ||
                     item.frame < frameId - 1)) //collect materials if it is unused in last frame
                {
                    result = item;
                }
            }

            if (result == null)
            {
                result = new MaterialRef { material = CreateMaterial(flags) };
                items.Add(result);
            }
            else if (_combineTexture)
            {
                result.material.SetTexture(ShaderConfig.ID_AlphaTex, _texture.alphaTexture);
            }

            if (result.blendMode != blendMode)
            {
                BlendModeUtils.Apply(result.material, blendMode);
                result.blendMode = blendMode;
            }

            result.group = group;
            result.frame = frameId;
            firstMaterialInFrame = true;
            return result.material;
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        private Material CreateMaterial(int flags)
        {
            var mat = new Material(_shader);

            mat.mainTexture = _texture.nativeTexture;
            if (_texture.alphaTexture != null)
            {
                mat.EnableKeyword("COMBINED");
                mat.SetTexture(ShaderConfig.ID_AlphaTex, _texture.alphaTexture);
            }

            for (var i = 0; i < internalKeywordsCount; i++)
                if ((flags & (1 << i)) != 0)
                {
                    var s = internalKeywords[i];
                    if (s != null)
                        mat.EnableKeyword(s);
                }

            if (_addKeywords != null)
            {
                var keywordCnt = _addKeywords.Count;
                for (var i = 0; i < keywordCnt; i++)
                    if ((flags & (1 << (i + internalKeywordsCount))) != 0)
                        mat.EnableKeyword(_addKeywords[i]);
            }

            mat.hideFlags = DisplayObject.hideFlags;
            if (onCreateNewMaterial != null)
                onCreateNewMaterial(mat);

            return mat;
        }

        /// <summary>
        /// </summary>
        public void DestroyMaterials()
        {
            var iter = _materials.GetEnumerator();
            while (iter.MoveNext())
            {
                var items = iter.Current.Value;
                if (Application.isPlaying)
                {
                    var cnt = items.Count;
                    for (var j = 0; j < cnt; j++)
                        Object.Destroy(items[j].material);
                }
                else
                {
                    var cnt = items.Count;
                    for (var j = 0; j < cnt; j++)
                        Object.DestroyImmediate(items[j].material);
                }

                items.Clear();
            }

            iter.Dispose();
        }

        /// <summary>
        /// </summary>
        public void RefreshMaterials()
        {
            _combineTexture = _texture.alphaTexture != null;
            var iter = _materials.GetEnumerator();
            while (iter.MoveNext())
            {
                var items = iter.Current.Value;
                var cnt = items.Count;
                for (var j = 0; j < cnt; j++)
                {
                    var mat = items[j].material;
                    mat.mainTexture = _texture.nativeTexture;
                    if (_combineTexture)
                    {
                        mat.EnableKeyword("COMBINED");
                        mat.SetTexture(ShaderConfig.ID_AlphaTex, _texture.alphaTexture);
                    }
                }
            }

            iter.Dispose();
        }

        private class MaterialRef
        {
            public BlendMode blendMode;
            public int frame;
            public uint group;
            public Material material;
        }
    }
}
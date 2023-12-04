using FairyGUI.Utils;
using UnityEngine;

namespace FairyGUI
{
    /// <summary>
    /// </summary>
    public class PackageItem
    {
        //sound
        public NAudioClip audioClip;

        //font
        public BitmapFont bitmapFont;
        public string[] branches;
        public bool exported;
        public UIObjectFactory.GComponentCreator extensionCreator;
        public string file;
        public MovieClip.Frame[] frames;
        public int height;
        public string[] highResolution;

        public string id;

        //movieclip
        public float interval;
        public string name;
        public ObjectType objectType;
        public UIPackage owner;
        public PixelHitTestData pixelHitTestData;
        public ByteBuffer rawData;
        public float repeatDelay;

        //image
        public Rect? scale9Grid;
        public bool scaleByTile;

        //spine/dragonbones
        public Vector2 skeletonAnchor;
        public object skeletonAsset;
        public bool swing;
        public NTexture texture;
        public int tileGridIndice;

        //component
        public bool translated;

        public PackageItemType type;
        public int width;

        public object Load()
        {
            return owner.GetItemAsset(this);
        }

        public PackageItem getBranch()
        {
            if (branches != null && owner._branchIndex != -1)
            {
                var itemId = branches[owner._branchIndex];
                if (itemId != null)
                    return owner.GetItem(itemId);
            }

            return this;
        }

        public PackageItem getHighResolution()
        {
            if (highResolution != null && GRoot.contentScaleLevel > 0)
            {
                var i = GRoot.contentScaleLevel - 1;
                if (i >= highResolution.Length)
                    i = highResolution.Length - 1;
                var itemId = highResolution[i];
                if (itemId != null)
                    return owner.GetItem(itemId);
            }

            return this;
        }
    }
}
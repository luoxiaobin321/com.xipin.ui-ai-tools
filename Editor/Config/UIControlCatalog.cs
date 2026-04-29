using System;
using System.Collections.Generic;
using UnityEngine;

namespace Xipin.UIAITools
{
    [CreateAssetMenu(menuName = "Xipin/UI AI Tools/Control Catalog")]
    public class UIControlCatalog : ScriptableObject
    {
        public List<string> dynamicImageComponents = new List<string> { "ImagePlus", "RawImagePlus", "ResView", "ItemView", "TopResView" };
        public List<string> textComponents = new List<string> { "TextPlus", "TMP_Text" };
        public List<string> buttonComponents = new List<string> { "Button", "ButtonPlus", "UIActionButton", "TogglePlus" };
        public List<string> scrollComponents = new List<string> { "ScrollViewPlus", "ScrollRectPlus" };
        public List<string> panelComponents = new List<string> { "UIPanel", "CanvasPanelBase", "CanvasPanelItemBase" };
        public List<string> functionalEmptyImageComponents = new List<string> { "BackgroundPlus", "GuideMask", "HighlightStencil", "SpriteAtlasAnimator", "Mask", "CanvasGroup" };

        public bool HasRole(string typeName, UIControlRole role)
        {
            var list = role == UIControlRole.DynamicImage ? dynamicImageComponents :
                role == UIControlRole.Text ? textComponents :
                role == UIControlRole.Button ? buttonComponents :
                role == UIControlRole.Scroll ? scrollComponents :
                role == UIControlRole.Panel ? panelComponents :
                functionalEmptyImageComponents;
            return list.Exists(x => string.Equals(x, typeName, StringComparison.OrdinalIgnoreCase));
        }
    }

    public enum UIControlRole
    {
        DynamicImage,
        Text,
        Button,
        Scroll,
        Panel,
        FunctionalEmptyImage
    }
}

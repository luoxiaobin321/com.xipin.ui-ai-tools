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

        public static void ValidateContract()
        {
            var catalog = CreateInstance<UIControlCatalog>();
            ExpectRole(catalog, "ImagePlus", UIControlRole.DynamicImage);
            ExpectRole(catalog, "tmp_text", UIControlRole.Text);
            ExpectRole(catalog, "buttonplus", UIControlRole.Button);
            ExpectRole(catalog, "ScrollRectPlus", UIControlRole.Scroll);
            ExpectRole(catalog, "CanvasPanelBase", UIControlRole.Panel);
            ExpectRole(catalog, "SpriteAtlasAnimator", UIControlRole.FunctionalEmptyImage);
            if (catalog.HasRole("ImagePlus", UIControlRole.Button))
                throw new Exception("UI control catalog role contract failed: ImagePlus/Button");
            Debug.Log("UI control catalog contract validation passed.");
        }

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

        static void ExpectRole(UIControlCatalog catalog, string typeName, UIControlRole role)
        {
            if (!catalog.HasRole(typeName, role))
                throw new Exception($"UI control catalog role contract failed: {typeName}/{role}");
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

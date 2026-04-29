using System.Collections.Generic;
using UnityEngine;

namespace Xipin.UIAITools
{
    [CreateAssetMenu(menuName = "Xipin/UI AI Tools/Profile")]
    public class UIAIToolsProfile : ScriptableObject
    {
        public string prefabRoot = "Assets/Bundle/Prefab";
        public string artUIRoot = "Assets/Art/UI";
        public string uiAtlasRoot = "Assets/Bundle/UIAtlas";
        public string uiTextureRoot = "Assets/Bundle/UITexture";
        public string mapTextureRoot = "Assets/Bundle/MapTexture";
        public string logRoot = "Logs";
        public string yooAssetAddressRule = "AddressByFileName";
        public List<string> textSearchRoots = new List<string> { "Assets/Scripts", "Assets/Bundle/Config", "Assets/Bundle/Setting" };
        public bool excludeMapFromUITriage = true;
    }
}

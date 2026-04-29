namespace Xipin.UIAITools
{
    public static class UIImageSearchService
    {
        public static void SearchReuseByImage()
        {
            UIAssetScanService.SearchReuseByImage();
        }

        public static void SearchReuseByImage(UIAIToolsProfile profile)
        {
            UIAssetScanService.SearchReuseByImage(profile);
        }

        public static void SearchReuseByImage(UIAIToolsProfile profile, UIControlCatalog catalog)
        {
            UIAssetScanService.SearchReuseByImage(profile, catalog);
        }

        public static void SearchReuseByImageBatch()
        {
            UIAssetScanService.SearchReuseByImageBatch();
        }

        public static void SearchReuseByImageBatch(UIAIToolsProfile profile)
        {
            UIAssetScanService.SearchReuseByImageBatch(profile);
        }

        public static void SearchReuseByImageBatch(UIAIToolsProfile profile, UIControlCatalog catalog)
        {
            UIAssetScanService.SearchReuseByImageBatch(profile, catalog);
        }
    }
}

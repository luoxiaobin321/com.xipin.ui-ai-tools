namespace Xipin.UIAITools
{
    public interface IUIAIGenerationProvider
    {
        string Name { get; }
        UIRedesignDraft CreateDraft(UIRedesignRequest request);
    }
}

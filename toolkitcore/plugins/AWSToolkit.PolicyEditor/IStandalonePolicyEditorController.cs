using Amazon.AWSToolkit.PolicyEditor.Model;

namespace Amazon.AWSToolkit.PolicyEditor
{
    public interface IStandalonePolicyEditorController
    {
        string Title { get; }
        PolicyModel.PolicyModelMode PolicyMode { get; }

        string GetPolicyDocument();
        void SavePolicyDocument(string document);
    }
}

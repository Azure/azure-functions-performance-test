
namespace SampleUsages
{
    public enum Platorm
    {
        AzureAnyAmazon,
        AzureOnly,
        AmazonOnly
    }

    public enum Language
    {
        NodeJs
    }

    public enum TriggerType
    {
        Queue,
        Blob,
        Http
    }

    public enum FunctionType
    {
        CPUIntensive,
        HighMemory,
        LowUsage
    }

    public enum LoadType
    {
        Steady,
        Various,
        Peek
    }
}

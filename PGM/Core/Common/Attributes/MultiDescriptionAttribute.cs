namespace PGM.Core.Common.Attributes;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
public sealed class MultiDescriptionAttribute : Attribute
{
    public string Description { get; }
    public string Category { get; }

    public MultiDescriptionAttribute(string description, string category = "Default")
    {
        Description = description;
        Category = category;
    }
}

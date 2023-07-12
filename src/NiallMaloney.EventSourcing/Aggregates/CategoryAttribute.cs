namespace NiallMaloney.EventSourcing.Aggregates;

[AttributeUsage(AttributeTargets.Class)]
public class CategoryAttribute : Attribute
{
    public string Category { get; }

    public CategoryAttribute(string category) => Category = category;
}

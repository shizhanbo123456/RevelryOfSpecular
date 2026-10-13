using System;

[AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class GmLabelAttribute:Attribute
{
    public string label;
    public GmLabelAttribute(string label)
    {
        this.label = label;
    }
}
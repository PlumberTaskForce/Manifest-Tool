using System;

namespace JohnCena.ManifestTool.DataAttributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class StructureOrderAttribute : Attribute
    {
        public int Order { get; private set; }

        public StructureOrderAttribute(int order)
        {
            this.Order = order;
        }
    }
}

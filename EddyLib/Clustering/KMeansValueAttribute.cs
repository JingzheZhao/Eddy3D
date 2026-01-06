using System;

namespace EddyLib
{
    /// <summary>
    /// Defines a property or field as an attribute to use for the k-means clustering
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class KMeansValueAttribute : Attribute
    { }
}

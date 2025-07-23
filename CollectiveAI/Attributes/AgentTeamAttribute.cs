using System;

namespace CollectiveAI.Attributes
{
    /// <summary>
    /// Attribute to mark a class as representing an agent team and specify its name
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public class TeamAttribute(string name) : Attribute
    {
        public string Name { get; } = name ?? throw new ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Attribute to mark a method as an agent creation method
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AgentAttribute : Attribute;
}
namespace CollectiveAI.Interfaces
{
    /// <summary>
    /// Marker interface for agent team class
    /// Classes implementing this interface and marked with [Team] attribute 
    /// will be automatically discovered and their agents created
    /// </summary>
    public interface IAgentTeam
    {
        // This can remain empty as a marker interface, or you can add common methods if needed
        // For example, you might add initialization or cleanup methods:
        // Task InitializeAsync();
        // Task CleanupAsync();
    }
}
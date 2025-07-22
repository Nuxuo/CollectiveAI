namespace CollectiveAI.Models;

public class AgentConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Instructions { get; set; } = string.Empty;
    public List<string> Plugins { get; set; } = [];
}
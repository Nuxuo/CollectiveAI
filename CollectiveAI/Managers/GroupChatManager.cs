using System.Text.Json;
using Microsoft.SemanticKernel.Agents.Orchestration.GroupChat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

namespace CollectiveAI.Managers;

public sealed class AiGroupChatManager(string topic, IChatCompletionService chatCompletion) : GroupChatManager
{
    public override ValueTask<GroupChatManagerResult<string>> FilterResults(ChatHistory history,
        CancellationToken cancellationToken = default)
    {
        return GetResponseAsync<string>(history, Prompts.Filter(topic), cancellationToken);
    }

    public override ValueTask<GroupChatManagerResult<string>> SelectNextAgent(ChatHistory history, GroupChatTeam team,
        CancellationToken cancellationToken = default)
    {
        return GetResponseAsync<string>(history, Prompts.Selection(topic, team.FormatList()), cancellationToken);
    }

    public override ValueTask<GroupChatManagerResult<bool>> ShouldRequestUserInput(ChatHistory history,
        CancellationToken cancellationToken = default)
    {
        return ValueTask.FromResult(new GroupChatManagerResult<bool>(false)
            { Reason = "The AI group chat manager does not request user input." });
    }

    public override async ValueTask<GroupChatManagerResult<bool>> ShouldTerminate(ChatHistory history,
        CancellationToken cancellationToken = default)
    {
        var result = await base.ShouldTerminate(history, cancellationToken);
        if (!result.Value)
            result = await GetResponseAsync<bool>(history, Prompts.Termination(topic), cancellationToken);
        return result;
    }

    private async ValueTask<GroupChatManagerResult<TValue>> GetResponseAsync<TValue>(ChatHistory history, string prompt,
        CancellationToken cancellationToken = default)
    {
        var executionSettings = new OpenAIPromptExecutionSettings
            { ResponseFormat = typeof(GroupChatManagerResult<TValue>) };
        var request = new ChatHistory(history) { new(AuthorRole.System, prompt) };
        var response =
            await chatCompletion.GetChatMessageContentAsync(request, executionSettings, null, cancellationToken);
        var responseText = response.ToString();

        return JsonSerializer.Deserialize<GroupChatManagerResult<TValue>>(responseText) ??
               throw new InvalidOperationException($"Failed to parse response: {responseText}");
    }

    private static class Prompts
    {
        public static string Termination(string topic)
        {
            return $"""
                        You are the CEO overseeing a trading strategy discussion about '{topic}'.
                        Your goal is to ensure the team has thoroughly analyzed all angles and reached an actionable consensus.
                        
                        Evaluate if:
                        - Risk parameters have been clearly defined and accepted
                        - Execution strategy is realistic and agreed upon
                        - Compliance has signed off on the approach
                        - All team members have contributed their expertise
                        - Specific action items with ownership are established
                        - Expected returns and risk metrics are quantified
                        
                        If the team has reached a unified, executable trading strategy with clear next steps, respond with True.
                        If critical perspectives are missing or consensus hasn't been achieved, respond with False.
                    """;
        }

        public static string Selection(string topic, string participants)
        {
            return $"""
                        You are the CEO facilitating a trading strategy discussion about '{topic}'.
                        You need to orchestrate productive collaboration by selecting who speaks next.
                        
                        Consider:
                        - Who can best challenge or validate the previous speaker's analysis
                        - Which expertise is missing from the current discussion
                        - Whether we need execution input at this stage
                        - Who can synthesize different viewpoints into actionable ideas
                        
                        Team members and their roles:
                        {participants}
                        
                        Select the team member whose input would most advance our decision-making process.
                        Prioritize building on previous insights rather than starting new threads.
                        Respond with only the name of the participant.
                        
                        You MUST respond with EXACTLY one of the agent names listed above.
                    """;
        }

        public static string Filter(string topic)
        {
            return $"""
                        You are the CEO providing a direct response about '{topic}'.
                        
                        Give a clear, conversational answer as if someone just asked you this question directly.
                        
                        Your response should be natural and to-the-point, like you're briefing someone who needs the bottom line.
                        Include the key facts, decisions, or recommendations that matter, but keep it conversational rather than formal.
                        
                        If there are specific actions needed, mention who's handling what and when, but weave it into your natural response.
                        If there are risks or concerns, bring them up naturally as part of your answer.
                        
                        Only structure it as a formal strategic review if the topic specifically calls for strategic planning or analysis.
                        
                        Focus on giving the person what they actually need to know, the way you'd explain it in person.
                    """;
        }
    }
}
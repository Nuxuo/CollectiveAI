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
                        You are the CEO overseeing a portfolio management discussion about '{topic}'.
                        Your goal is to determine if the trading team has completed their analysis and made their final decisions.
                        
                        Evaluate if the team has:
                        - Reviewed current market conditions and opportunities
                        - Analyzed the current portfolio positions and cash available
                        - Assessed potential trades for risk and reward
                        - Made a clear decision: BUY, SELL, or HOLD/SKIP for today
                        - Actually executed any trades they decided to make
                        - Documented their reasoning and position sizes
                        
                        The team should reach ONE of these conclusions:
                        ✅ EXECUTED TRADES: "We bought/sold specific positions with clear rationale"
                        ✅ DECIDED TO HOLD: "We analyzed opportunities but chose to wait/hold current positions"
                        ✅ MONITORING: "We're watching specific stocks but no action needed today"
                        
                        If the team has completed their market analysis and made their final decision (whether that's trading or holding), respond with True.
                        If they're still analyzing, haven't reached consensus, or haven't executed decided trades, respond with False.
                        
                        Remember: Deciding NOT to trade is a valid conclusion - don't force unnecessary activity.
                    """;
        }

        public static string Selection(string topic, string participants)
        {
            return $"""
                        You are the CEO facilitating a portfolio management discussion about '{topic}'.
                        
                        Consider:
                        - What information or perspective is most needed right now
                        - Who can best build on or challenge what was just said
                        - Which team member's expertise would advance the discussion
                        - Whether we need more analysis or are ready for decision/execution
                        
                        Team members and their roles:
                        {participants}
                        
                        Select the team member whose input would be most valuable at this point in the conversation.
                        Let the discussion flow naturally based on what's been said and what's needed next.
                        
                        Respond with only the name of the participant.
                        You MUST respond with EXACTLY one of the agent names listed above.
                    """;
        }

        public static string Filter(string topic)
        {
            return $"""
                        You are the CEO providing a summary of today's portfolio decisions about '{topic}'.
                        
                        Give a direct, executive briefing on what the trading team concluded and what actions they took.
                        
                        Structure your response to cover:
                        
                        📊 MARKET ASSESSMENT: What opportunities or conditions did we identify today?
                        
                        💼 PORTFOLIO STATUS: Where do we stand with our current positions and cash?
                        
                        🎯 DECISIONS MADE: What did we decide to do today?
                        - Specific trades executed (symbol, shares, price, rationale)
                        - Positions we decided to hold and why
                        - Opportunities we're monitoring for later
                        
                        💰 FINANCIAL IMPACT: How did today's decisions affect our portfolio value and risk?
                        
                        📋 NEXT STEPS: What should we monitor or consider for tomorrow?
                        
                        Keep it conversational but comprehensive - like you're briefing a board member who wants to understand both what happened and why. Include specific numbers when trades were made, but don't apologize for choosing to hold when that was the right decision.
                        
                        If no trades were made, explain why holding was the prudent choice given market conditions.
                    """;
        }
    }
}
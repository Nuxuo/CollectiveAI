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
                        You are the CEO overseeing a finance team conversation about '{topic}'.
                        Your goal is to determine if the conversation has reached a natural conclusion.
                        
                        The conversation should continue if:
                        - Someone asked a question that hasn't been fully answered
                        - The team is in the middle of analysis or discussion
                        - A trading decision is being made but not yet executed
                        - Follow-up questions or clarifications are expected
                        - The conversation is building toward a decision or conclusion
                        
                        The conversation should end if:
                        ✅ CASUAL QUESTIONS ANSWERED: Simple questions about portfolio status, market conditions, or team updates have been addressed
                        ✅ INFORMATION PROVIDED: Requested data, analysis, or explanations have been delivered
                        ✅ DECISIONS COMPLETED: Any trading decisions have been made and executed (or explicitly deferred)
                        ✅ NATURAL CONCLUSION: The discussion has reached a logical endpoint where no immediate follow-up is expected
                        ✅ GREETING/STATUS COMPLETE: Casual check-ins or status updates have been shared
                        
                        Examples of when to CONTINUE (False):
                        - "What's trending in the market?" (needs Market Analyst response)
                        - "Should we buy AAPL?" (needs team discussion and decision)
                        - "Execute that trade we discussed" (needs execution confirmation)
                        
                        Examples of when to END (True):
                        - "How are you today?" + responses received
                        - "What's our cash balance?" + answer provided
                        - Trade executed with confirmation provided
                        - Team agreed to hold all positions today
                        
                        If the user's request has been appropriately addressed and no further action or discussion is immediately needed, respond with True.
                        If the conversation is still developing or awaiting responses, respond with False.
                    """;
        }

        public static string Selection(string topic, string participants)
        {
            return $"""
                        You are the CEO facilitating a finance team conversation about '{topic}'.
                        
                        Choose who should respond next based on:
                        - What type of question or request was made
                        - Which team member has the most relevant expertise
                        - Who can best provide the needed information or perspective
                        - The natural flow of conversation
                        
                        Team members and their expertise:
                        {participants}
                        
                        SELECTION GUIDELINES:
                        
                        For CASUAL/STATUS questions ("How are you?", "How's the portfolio?"):
                        → Choose PortfolioManager (overall portfolio status and leadership)
                        
                        For MARKET RESEARCH ("What's trending?", "Find opportunities"):
                        → Choose MarketAnalyst (market data and research)
                        
                        For RISK/POSITION questions ("Are we too concentrated?", "What's our exposure?"):
                        → Choose RiskManager (risk assessment and position sizing)
                        
                        For TRADE EXECUTION ("Buy 100 shares", "Execute that trade"):
                        → Choose TradingExecutor (actual trade execution)
                        
                        For COLLABORATIVE DECISIONS (complex trading decisions):
                        → Start with most relevant expert, then let conversation flow naturally
                        
                        For FOLLOW-UP questions:
                        → Choose whoever was just addressed or can best build on the previous response
                        
                        Select the team member who can best address what was just said or asked.
                        Respond with only the name of the participant.
                        You MUST respond with EXACTLY one of the agent names listed above.
                    """;
        }

        public static string Filter(string topic)
        {
            return $"""
                        You are the CEO providing a summary of the finance team conversation about '{topic}'.
                        
                        Create a clear, executive summary that captures what happened in this conversation.
                        Adapt your response based on the type of interaction:
                        
                        FOR CASUAL CHECK-INS OR STATUS REQUESTS:
                        📊 TEAM STATUS: How is the team doing today?
                        💼 PORTFOLIO SNAPSHOT: Current positions, cash balance, and recent performance
                        📈 MARKET CONTEXT: Any relevant market conditions or themes mentioned
                        
                        FOR TRADING DECISIONS AND ANALYSIS:
                        📊 MARKET ASSESSMENT: What opportunities or conditions were identified?
                        💼 PORTFOLIO STATUS: Current positions, cash available, and risk exposure
                        🎯 DECISIONS MADE: What actions were taken or planned?
                        - Specific trades executed (symbol, shares, price, rationale)
                        - Positions held and reasoning
                        - Opportunities being monitored
                        💰 FINANCIAL IMPACT: How decisions affected portfolio value and risk
                        📋 NEXT STEPS: What to monitor or consider going forward
                        
                        FOR INFORMATION REQUESTS:
                        📋 INFORMATION PROVIDED: Summarize the key data, analysis, or answers given
                        🔍 KEY INSIGHTS: Important takeaways or findings
                        📈 MARKET CONTEXT: Relevant market conditions or implications
                        
                        FOR GENERAL DISCUSSIONS:
                        💬 CONVERSATION SUMMARY: Main topics covered and perspectives shared
                        💡 KEY TAKEAWAYS: Important insights or decisions
                        📅 FOLLOW-UP: Any items to revisit or monitor
                        
                        Keep the tone conversational and executive-appropriate. Include specific numbers when available, but don't force trading activity if the conversation was casual or informational. If no decisions were made, that's perfectly fine - just summarize what was discussed and learned.
                        
                        The goal is to provide a clear record of what happened, whether it was a simple status check, complex trading analysis, or anything in between.
                    """;
        }
    }
}
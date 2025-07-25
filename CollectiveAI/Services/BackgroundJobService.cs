using Hangfire;
namespace CollectiveAI.Services;

/// <summary>
/// Simple background job service for daily market discussion
/// </summary>
public interface IBackgroundJobService
{
    string ScheduleDailyMarketDiscussion();
    string ScheduleCustomMarketDiscussion(string message, DateTime scheduledTime, TimeZoneInfo? timeZone = null);
}

public class BackgroundJobService(ILogger<BackgroundJobService> logger) : IBackgroundJobService
{
    public string ScheduleDailyMarketDiscussion()
    {
        var jobId = "daily-market-discussion";
        RecurringJob.AddOrUpdate(
            jobId,
            () => RunDailyMarketDiscussionAsync(),
            "0 9 * *1-5",
            new RecurringJobOptions
            {
                TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time"),
                MisfireHandling = MisfireHandlingMode.Ignorable
            });
        logger.LogInformation("Scheduled daily market discussion job: {JobId}", jobId);
        return jobId;
    }

    public string ScheduleCustomMarketDiscussion(string message, DateTime scheduledTime, TimeZoneInfo? timeZone = null)
    {
        var targetTimeZone = timeZone ?? TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        var scheduledTimeOffset = new DateTimeOffset(scheduledTime, targetTimeZone.GetUtcOffset(scheduledTime));

        var jobId = BackgroundJob.Schedule(
            () => RunMarketDiscussionAsync(message),
            scheduledTimeOffset);

        logger.LogInformation("Scheduled custom market discussion job: {JobId} for {ScheduledTime} ({TimeZone}) with message: {Message}",
            jobId, scheduledTime, targetTimeZone.DisplayName, message);
        return jobId;
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 900 })]
    public async Task RunDailyMarketDiscussionAsync()
    {
        var message = """
            Good morning Finance team. Time for our daily portfolio review and trading decisions.
            Here's what I need from you today: Start by checking what's trending in the markets right now and identify any significant overnight developments or news that could impact our positions. 
            Review our current portfolio status - what positions we're holding, our cash balance, and how our existing investments performed yesterday.
            
            I want you to actively search for new opportunities across all sectors. Don't just look at the obvious names - dig into what's moving, what's showing unusual volume, and what themes are emerging. 
            Consider both individual stock picks and any portfolio adjustments we might need.
            Assess our risk exposure and position sizing. Are we properly diversified? 
            
            Do we have any concentrations that need addressing? Are there any positions we should consider trimming or adding to based on recent performance or changing fundamentals?
            Most importantly, I need you to reach a clear decision by the end of this discussion: Are we executing any trades today, or are we holding our current positions? If you find compelling opportunities, 
            I want specific recommendations with position sizes and rationale. If market conditions suggest we should stay patient, that's equally valuable - just tell me why and what we're watching for.
            Remember, we have real money at stake here. Make sure any trades you decide to execute are actually carried out, and document your reasoning. 
            
            We're not just talking strategy - we're making actual investment decisions that impact our portfolio performance.
            Time to get to work. What does the market look like today, and how should we position ourselves?
            """;
        await RunMarketDiscussionAsync(message);
    }

    [AutomaticRetry(Attempts = 2, DelaysInSeconds = new[] { 300, 900 })]
    public async Task RunMarketDiscussionAsync(string message)
    {
        logger.LogInformation("Starting market discussion with message: {Message}", message);
        try
        {
            var agentService = ServiceLocator.GetRequiredService<IAgentService>();
            await agentService.DiscussAsync(message, maxRounds: 15);
            logger.LogInformation("Market discussion completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to run market discussion");
            throw;
        }
    }
}
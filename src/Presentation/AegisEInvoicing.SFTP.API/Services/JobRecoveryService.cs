//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.Extensions.Hosting;
//using Microsoft.Extensions.Logging;
//using Quartz;

//namespace AegisEInvoicing.BackgroundService.Services;

///// <summary>
///// Service to monitor and recover blocked Quartz jobs
///// </summary>
//public class JobRecoveryService : BackgroundService
//{
//    private readonly IServiceProvider _serviceProvider;
//    private readonly ILogger<JobRecoveryService> _logger;
//    private DateTime _lastJobActivity = DateTime.UtcNow;

//    public JobRecoveryService(
//        IServiceProvider serviceProvider,
//        ILogger<JobRecoveryService> logger)
//    {
//        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
//        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//    }

//    public void NotifyJobActivity()
//    {
//        _lastJobActivity = DateTime.UtcNow;
//        _logger.LogDebug("[JOB RECOVERY] Job activity recorded at {Timestamp}", _lastJobActivity);
//    }

//    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//    {
//        _logger.LogInformation("[JOB RECOVERY] Job recovery service started");

//        // Wait for initial startup
//        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

//        while (!stoppingToken.IsCancellationRequested)
//        {
//            try
//            {
//                await CheckForBlockedJobs(stoppingToken);
//                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
//            }
//            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
//            {
//                _logger.LogInformation("[JOB RECOVERY] Job recovery service is stopping");
//                break;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "[JOB RECOVERY] Error in job recovery service: {Message}", ex.Message);
//                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
//            }
//        }
//    }

//    private async Task CheckForBlockedJobs(CancellationToken cancellationToken)
//    {
//        try
//        {
//            using var scope = _serviceProvider.CreateScope();
//            var scheduler = scope.ServiceProvider.GetService<IScheduler>();
            
//            if (scheduler == null || !scheduler.IsStarted)
//            {
//                _logger.LogWarning("[JOB RECOVERY] Scheduler is not available or not started");
//                return;
//            }

//            var timeSinceLastActivity = DateTime.UtcNow - _lastJobActivity;
            
//            // If no job activity for more than 2 minutes, check for blocked jobs
//            if (timeSinceLastActivity > TimeSpan.FromMinutes(2))
//            {
//                _logger.LogWarning("[JOB RECOVERY] No job activity for {TimeSince}. Checking for blocked jobs...", timeSinceLastActivity);

//                var jobKeys = await scheduler.GetJobKeys(Quartz.Impl.Matchers.GroupMatcher<JobKey>.AnyGroup());
                
//                foreach (var jobKey in jobKeys)
//                {
//                    var triggers = await scheduler.GetTriggersOfJob(jobKey);
                    
//                    foreach (var trigger in triggers)
//                    {
//                        var triggerState = await scheduler.GetTriggerState(trigger.Key);
                        
//                        _logger.LogInformation("[JOB RECOVERY] Job: {JobKey}, Trigger: {TriggerKey}, State: {State}", 
//                            jobKey, trigger.Key, triggerState);

//                        if (triggerState == TriggerState.Blocked)
//                        {
//                            _logger.LogWarning("[JOB RECOVERY] Detected blocked trigger: {TriggerKey}. Attempting recovery...", trigger.Key);
                            
//                            try
//                            {
//                                // Try to interrupt the job
//                                await scheduler.Interrupt(jobKey);
//                                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                                
//                                // Resume the trigger
//                                await scheduler.ResumeTrigger(trigger.Key);
                                
//                                _logger.LogInformation("[JOB RECOVERY] Successfully recovered blocked trigger: {TriggerKey}", trigger.Key);
                                
//                                // Reset activity time since we just performed recovery
//                                NotifyJobActivity();
//                            }
//                            catch (Exception recoveryEx)
//                            {
//                                _logger.LogError(recoveryEx, "[JOB RECOVERY] Failed to recover blocked trigger {TriggerKey}: {Error}", 
//                                    trigger.Key, recoveryEx.Message);
//                            }
//                        }
//                        else if (triggerState == TriggerState.Normal)
//                        {
//                            // Check if the trigger should have fired recently but didn't
//                            var nextFireTime = trigger.GetNextFireTimeUtc();
//                            var prevFireTime = trigger.GetPreviousFireTimeUtc();
                            
//                            if (prevFireTime.HasValue)
//                            {
//                                var timeSinceLastFire = DateTimeOffset.UtcNow - prevFireTime.Value;
                                
//                                // If it's been more than 3 minutes since last fire and should have fired, something is wrong
//                                if (timeSinceLastFire > TimeSpan.FromMinutes(3))
//                                {
//                                    _logger.LogWarning("[JOB RECOVERY] Trigger {TriggerKey} hasn't fired in {TimeSince}. Last fire: {LastFire}", 
//                                        trigger.Key, timeSinceLastFire, prevFireTime);
                                    
//                                    // Try triggering it manually
//                                    try
//                                    {
//                                        await scheduler.TriggerJob(jobKey);
//                                        _logger.LogInformation("[JOB RECOVERY] Manually triggered job: {JobKey}", jobKey);
//                                        NotifyJobActivity();
//                                    }
//                                    catch (Exception triggerEx)
//                                    {
//                                        _logger.LogError(triggerEx, "[JOB RECOVERY] Failed to manually trigger job {JobKey}: {Error}", 
//                                            jobKey, triggerEx.Message);
//                                    }
//                                }
//                            }
//                        }
//                    }
//                }
//            }
//            else
//            {
//                _logger.LogDebug("[JOB RECOVERY] Jobs are running normally. Last activity: {TimeSince} ago", timeSinceLastActivity);
//            }
//        }
//        catch (Exception ex)
//        {
//            _logger.LogError(ex, "[JOB RECOVERY] Error checking for blocked jobs: {Message}", ex.Message);
//        }
//    }
//}
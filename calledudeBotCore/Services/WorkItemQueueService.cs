using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IWorkItemQueueService : IHostedService
{
	void EnqueueItem(Task task);
}

public class WorkItemQueueService : BackgroundService, IWorkItemQueueService
{
	private readonly BlockingCollection<Task> _queue;
	private readonly ILogger<WorkItemQueueService> _logger;

	public WorkItemQueueService(ILogger<WorkItemQueueService> logger)
	{
		_queue = [];
		_logger = logger;
	}

	public void EnqueueItem(Task task)
	{
		_queue.Add(task);
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				_logger.LogTrace("Waiting for work.");
				var task = _queue.Take(stoppingToken);

				_logger.LogTrace("Work dequeued. Executing.");
				await task;

				_logger.LogTrace("Executed work.");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "An error occured with an enqueued work item.");
			}
		}
	}
}
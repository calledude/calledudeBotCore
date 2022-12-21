using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace calledudeBot.Services;

public interface IProcessMonitorService
{
	Task WaitForProcessesToQuit();
	Task WaitForProcessToStart(params string[] processNames);
}

public class ProcessMonitorService : IProcessMonitorService
{
	private readonly ILogger<ProcessMonitorService> _logger;
	private string[]? _processes;

	public ProcessMonitorService(ILogger<ProcessMonitorService> logger)
	{
		_logger = logger;
	}

	public async Task WaitForProcessToStart(params string[] processNames)
	{
		_processes = processNames;

		while (!GetProcesses().Any())
		{
			await Task.Delay(2000);
		}
	}

	public async Task WaitForProcessesToQuit()
	{
		if (_processes is null)
			throw new InvalidOperationException("No processes registered to watch.");

		while (GetProcesses().Any(x => !x.HasExited))
		{
			await Task.Delay(50);
		}
	}

	private IEnumerable<Process> GetProcesses()
		=> _processes!.SelectMany(Process.GetProcessesByName);
}

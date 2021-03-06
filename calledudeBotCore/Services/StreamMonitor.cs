using calledudeBot.Bots;
using calledudeBot.Config;
using calledudeBot.Models;
using Discord;
using Discord.WebSocket;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Enum;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;

namespace calledudeBot.Services
{
    public interface IStreamMonitor
    {
        bool IsStreaming { get; }
        DateTime StreamStarted { get; }

        Task Connect();
        void Dispose();
        Task Handle(ReadyNotification notification, CancellationToken cancellationToken);
    }

    public sealed class StreamMonitor : INotificationHandler<ReadyNotification>, IDisposable, IStreamMonitor
    {
        private IGuildUser? _streamer;
        private ITextChannel? _announceChannel;

        private readonly OBSWebsocket _obs;
        private readonly System.Timers.Timer _streamStatusTimer;
        private readonly SemaphoreSlim _exitSem;
        private readonly ILogger<StreamMonitor> _logger;
        private readonly DiscordSocketClient _client;
        private readonly ulong _announceChannelID;
        private readonly ulong _streamerID;

        public bool IsStreaming { get; private set; }
        public DateTime StreamStarted { get; private set; }

        public StreamMonitor(ILogger<StreamMonitor> logger, IOptions<BotConfig> options, DiscordSocketClient client)
        {
            var config = options?.Value ?? throw new ArgumentNullException(nameof(options));

            _exitSem = new SemaphoreSlim(1);

            _logger = logger;
            _client = client;

            _obs = new OBSWebsocket
            {
                WSTimeout = TimeSpan.FromSeconds(5)
            };

            _obs.StreamStatus += CheckLiveStatus;
            _obs.StreamingStateChanged += StreamingStateChanged;
            _obs.Disconnected += OnObsExit;
            _obs.Connected += OnConnected;

            _streamStatusTimer = new System.Timers.Timer(2000);
            _streamStatusTimer.Elapsed += CheckDiscordStatus;

            _announceChannelID = config.AnnounceChannelId!;
            _streamerID = config.StreamerId!;
        }

        public Task Handle(ReadyNotification notification, CancellationToken cancellationToken)
        {
            if (notification.Bot is DiscordBot)
            {
                _ = Connect();
            }

            return Task.CompletedTask;
        }

        public async Task Connect()
        {
            _announceChannel = _client.GetChannel(_announceChannelID) as ITextChannel;
            if (_announceChannel == null)
            {
                _logger.LogWarning("Invalid channel. Will not announce when stream goes live.");
                return;
            }

            _streamer = await _announceChannel.Guild.GetUserAsync(_streamerID);
            if (_streamer == null)
            {
                _logger.LogWarning("Invalid StreamerID. Could not find user.");
                return;
            }

            await WaitForObsProcess();
            await TryConnect();
        }

        private void StreamingStateChanged(OBSWebsocket sender, OutputState state)
        {
            if (state == OutputState.Started)
            {
                _logger.LogInformation("OBS has gone live. Checking discord status for user {0}#{1}..", _streamer!.Username, _streamer.Discriminator);
                _streamStatusTimer.Start();
            }
            else if (state == OutputState.Stopped)
            {
                IsStreaming = false;
                _streamStatusTimer.Stop();
                _logger.LogInformation("Stream stopped.");
            }
        }

        private void OnConnected(object? sender, EventArgs e)
            => _logger.LogInformation("Connected to OBS. Start streaming!");

        private async Task TryConnect()
        {
            //Trying 5 times just in case.
            if (await Enumerable.Range(1, 5)
                .ToAsyncEnumerable()
                .SelectAwait(async _ => await _obs.Connect("ws://localhost:4444"))
                .AllAsync(x => !x))
            {
                _logger.LogWarning("You need to install the obs-websocket plugin for OBS and configure it to run on port 4444.");
                _logger.LogWarning("Go to this URL to download it: {0}", "https://github.com/Palakis/obs-websocket/releases");

                await Task.Delay(10000);
            }
        }

        private async Task WaitForObsProcess()
        {
            _logger.LogInformation("Waiting for OBS to start.");
            while (!GetObsProcesses().Any())
            {
                await Task.Delay(2000);
            }
        }

        private async void CheckDiscordStatus(object sender, ElapsedEventArgs e)
        {
            if (_streamer?.Activity is not StreamingGame sg)
                return;

            _streamStatusTimer.Stop();
            IsStreaming = true;

            var messages = await _announceChannel!
                .GetMessagesAsync()
                .FlattenAsync();

            var twitchUsername = sg.Url.Split('/').Last();
            var msg = $"🔴 **{twitchUsername}** is now **LIVE**\n- Title: **{sg.Details}**\n- Watch at: {sg.Url}";

            //StreamStarted returns the _true_ time that the stream started
            //If any announcement message exists within 3 minutes of that, don't send a new announcement
            //In that case we assume that the bot has been restarted (for whatever reason)
            if (!messages.Any(m =>
                m.Author.Id == _client.CurrentUser.Id
                && m.Content.Equals(msg)
                && StreamStarted - m.Timestamp < TimeSpan.FromMinutes(3)))
            {
                _logger.LogInformation("Streamer went live. Sending announcement to #{0}", _announceChannel.Name);
                await _announceChannel.SendMessageAsync(msg);
            }
        }

        private async void OnObsExit(object? sender, EventArgs e)
        {
            if (!await _exitSem.WaitAsync(150))
                return;

            IsStreaming = false;
            _streamStatusTimer.Stop();
            _obs.Disconnect();

            while (GetObsProcesses().Any(x => !x.HasExited))
            {
                await Task.Delay(50);
            }

            await Connect();

            _exitSem.Release();
        }

        private static IEnumerable<Process> GetObsProcesses()
        {
            string[] procs = { "obs64", "obs32" };

            return procs.SelectMany(Process.GetProcessesByName);
        }

        private void CheckLiveStatus(OBSWebsocket sender, StreamStatus status)
        {
            if (status.Streaming == IsStreaming)
                return;

            if (status.Streaming)
            {
                StreamStarted = DateTime.Now - status.TotalStreamTime;
                _streamStatusTimer.Start();
            }
        }

        public void Dispose()
        {
            _streamStatusTimer.Dispose();
            _obs.Disconnect();
        }
    }
}

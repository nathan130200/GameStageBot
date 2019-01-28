using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Lavalink;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.CommandsNext.Attributes;

namespace GameStage
{
    public class GameStageBot
    {
        public const string STREAMING_URL = "https://www.twitch.tv/fogoamigo100";
        public const ulong GAMESTAGE = 498338969323307018;

        internal IServiceProvider _services;
        internal DiscordClient _discord;
        internal CommandsNextExtension _commandsnext;
        internal InteractivityExtension _interactivity;
        internal LavalinkExtension _lavalink;

        internal Timer _activity_timer;
        internal volatile int _activity_reset = 0;

        public static string GetUnicodeEmoji(string name, bool named = true)
        {
            if (named && !name.StartsWith(":"))
                name = ":" + name;

            if (named && !name.EndsWith(":"))
                name += ":";

            var emojis = (Dictionary<string, string>)typeof(DiscordEmoji)
                .GetProperty("UnicodeEmojis", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);

            return emojis[name];
        }

        public GameStageBot()
        {
            _services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(RandomNumberGenerator.Create())
                .BuildServiceProvider();

            _discord = new DiscordClient(new DiscordConfiguration
            {
                Token = Environment.GetEnvironmentVariable("GAMESTAGE_BOT_TOKEN"),
                TokenType = TokenType.Bot,
                AutoReconnect = true,
                AutomaticGuildSync = true,
                GatewayCompressionLevel = GatewayCompressionLevel.Stream,
                LargeThreshold = 512,
                MessageCacheSize = 512,
                HttpTimeout = 1.Minutes(),
                LogLevel = LogLevel.Debug,
                ReconnectIndefinitely = true,
                UseInternalLogHandler = false
            });

            _lavalink = _discord.UseLavalink();

            _interactivity = _discord.UseInteractivity(new InteractivityConfiguration
            {
                PaginationBehavior = TimeoutBehaviour.DeleteMessage,
                PaginationTimeout = 10.Minutes(),
                Timeout = 5.Minutes()
            });

            _commandsnext = _discord.UseCommandsNext(new CommandsNextConfiguration
            {
                Services = _services,
                EnableDms = false,
                DmHelp = false,
                EnableMentionPrefix = false,
                StringPrefixes = new[] { "gst3!", "gst3 " },
            });

            _commandsnext.RegisterCommands(typeof(GameStageBot).Assembly);
            _commandsnext.CommandExecuted += this.OnCommandExecuted;
            _commandsnext.CommandErrored += this.OnCommandErrored;

            _activity_timer = new Timer(ActivityTimerCallback, null, -1, -1);

            _discord.DebugLogger.LogMessageReceived += this.OnLog;
            _discord.Ready += this.OnReady;
            _discord.ClientErrored += this.OnClientError;
        }

        public async Task StartAsync()
        {
            await _discord.ConnectAsync(status: UserStatus.DoNotDisturb);
        }

        public async Task StopAsync()
        {
            await _discord.UpdateStatusAsync(userStatus: UserStatus.Invisible);
            await _discord.DisconnectAsync();
        }

        void OnLog(object sender, DebugLogMessageEventArgs e)
        {
            var cb = null as Action<string>;

            switch (e.Level)
            {
                case LogLevel.Info:
                case LogLevel.Debug:
                    cb = Log.Info;
                    break;

                case LogLevel.Critical:
                case LogLevel.Error:
                    cb = Log.Error;
                    break;

                case LogLevel.Warning:
                    cb = Log.Warn;
                    break;
            }

            cb?.Invoke($"[{e.Application}]: {e.Message}");
        }

        async Task OnReady(ReadyEventArgs e)
        {
            await _discord.UpdateStatusAsync(new DiscordActivity
            {
                Name = $"{GetUnicodeEmoji(":sleeping:")} Acabei de acordar!",
                ActivityType = ActivityType.Streaming,
                StreamUrl = STREAMING_URL
            });

            _activity_timer.Change(10.Seconds(), 30.Seconds());
        }

        #region << Activity Timer >>

        public async Task UpdateCreatorActivityAsync(int? size = null)
        {
            _activity_reset = size ?? 10;

            if (size == null || size <= 0)
                _activity_reset = 0;

            var mbrf = new Func<Task<DiscordMember>>(async () => {
                return await _discord.GetGuildAsync(GAMESTAGE).ContinueWith(task =>
                {
                    return task.Result.Members.Where(xm => xm.Id == 143466929615667201)
                        .FirstOrDefault();
                });
            });

            var creator = await mbrf();

            if (creator == null)
                _activity_reset = 0;

            if (creator.Presence != null && creator.Presence.Activity != null)
            {
                if (creator.Presence.Activity.ActivityType == ActivityType.Playing && !string.IsNullOrEmpty(creator.Presence.Activity.Name))
                {
                    await _discord.UpdateStatusAsync(new DiscordActivity
                    {
                        ActivityType = ActivityType.Watching,
                        Name = $"{GetUnicodeEmoji(":heart_eyes:")} meu criador {creator.Username}#{creator.Discriminator} jogar {creator.Presence.Activity.Name}.",
                    });
                }
                else if (creator.Presence.Activity.ActivityType == ActivityType.ListeningTo)
                {
                    if (_activity_reset > 0)
                        return;
                    else
                    {
                        _ = Task.Run(async () =>
                        {
                            do
                            {
                                creator = await mbrf();

                                var msc = creator.Presence.Activity.RichPresence.Details + " (" + creator.Presence.Activity.RichPresence.State + ")";

                                await _discord.UpdateStatusAsync(new DiscordActivity
                                {
                                    ActivityType = ActivityType.ListeningTo,
                                    Name = $"{GetUnicodeEmoji(":heart_eyes:")} música boa com meu criador {creator.Username}#{creator.Discriminator} - {msc}.",
                                });

                                await Task.Delay(1000);
                                _activity_reset--;
                            }
                            while (_activity_reset > 0);
                        });
                    }
                }
                else
                    goto def;
            }
            else
                goto def;

        def:
            {
                await _discord.UpdateStatusAsync(new DiscordActivity
                {
                    ActivityType = ActivityType.Watching,
                    Name = $"{GetUnicodeEmoji(":heart_eyes:")} meu criador {creator.Username}#{creator.Discriminator} programar.",
                });

                return;
            }
        }

        async void ActivityTimerCallback(object state)
        {
            if (_activity_reset > 0)
                return;

            var offset = new Random(Environment.TickCount).Next(1, 10);

            switch (offset)
            {
                case 1:
                {
                    var mbr = await _discord.GetGuildAsync(GAMESTAGE).ContinueWith(task => task.Result.MemberCount);

                    await _discord.UpdateStatusAsync(new DiscordActivity
                    {
                        Name = $"vocês! {GetUnicodeEmoji(":grinning:")} Já somos {mbr} membros na comunidade!",
                        ActivityType = ActivityType.Watching,
                    });
                }
                break;

                case 2:
                {
                    await _discord.UpdateStatusAsync(new DiscordActivity
                    {
                        Name = $"{GetUnicodeEmoji(":ping_pong:")} minha latência de {_discord.Ping}ms",
                        ActivityType = ActivityType.Streaming,
                        StreamUrl = STREAMING_URL
                    });
                }
                break;

                case 3:
                {
                    var online = await _discord.GetGuildAsync(GAMESTAGE)
                    .ContinueWith(task =>
                    {
                        return task.Result.Members.Where(xm =>
                            xm.Presence != null && (xm.Presence.Status != UserStatus.Invisible || xm.Presence.Status != UserStatus.Offline))
                                .Count();
                    });

                    var term = "membros online!";

                    if (online >= 2)
                        term = "membros online!";
                    else if (online == 1)
                        term = "membro online!";
                    else if (online <= 0)
                        term = "ninguém online!";

                    if (online >= 1)
                        term = online + " " + term;

                    await _discord.UpdateStatusAsync(new DiscordActivity
                    {
                        Name = $"{GetUnicodeEmoji(":ok_hand:")} Temos {term}",
                        ActivityType = ActivityType.ListeningTo,
                    });
                }
                break;

                case 4:
                {
                    var online = await _discord.GetGuildAsync(GAMESTAGE).ContinueWith(task => 
                    {
                        return task.Result.Members.Where(xm =>
                            xm.Roles.Any(xr =>
                            {
                                return xr.Id == 498665163222941696 ||
                                    xr.Id == 500200650010001419 ||
                                    xr.Id == 500876183110418442 ||
                                    xr.Id == 500876457602580480 ||
                                    xr.Id == 500876744685912085 ||
                                    xr.Id == 510972108835258378 ||
                                    xr.Id == 511352404596359208 ||
                                    xr.Id == 513862435350904872   
                                ;
                            })
                        );
                    })
                            
                    .ContinueWith(task =>
                    {
                        return task.Result.Where(xm => xm.Presence != null &&
                            (xm.Presence.Status != UserStatus.Offline || xm.Presence.Status != UserStatus.Invisible))
                            .Count();
                    });

                    await _discord.UpdateStatusAsync(new DiscordActivity
                    {
                        Name = $"{(online > 0 ? $"{GetUnicodeEmoji(":sunglasses:")} {online} staff{(online > 1 ? "s" : "")} online": $"{GetUnicodeEmoji(":scream:")} nenhum staff online.")}",
                        ActivityType = ActivityType.Watching
                    });
                }
                break;

                case 5:
                {
                    var online = await _discord.GetGuildAsync(GAMESTAGE).ContinueWith(task =>
                    {
                        return task.Result.Members.Where(xm => xm.IsBot)
                            .Where(xm => xm.Presence != null && (xm.Presence.Status != UserStatus.Offline || xm.Presence.Status != UserStatus.Invisible))
                            .Count();
                    });

                    await _discord.UpdateStatusAsync(new DiscordActivity
                    {
                        ActivityType = ActivityType.Playing,
                        Name = $"{GetUnicodeEmoji(":robot:")} com meu{(online > 2 ? "s" : "")} {online} amigo{(online > 2 ? "s" : "")} robô{(online > 2 ? "s" : "")}"
                    });

                }
                break;

                case 6:
                {
                    var mbr = await _discord.GetGuildAsync(GAMESTAGE)
                        .ContinueWith(task =>
                        {
                            return task.Result.Members.Where(xm => xm.Id == 143466929615667201)
                                .FirstOrDefault();
                        });

                    if (mbr.Presence != null && mbr.Presence.Activity != null)
                    {
                        if (mbr.Presence.Activity.ActivityType == ActivityType.Playing && !string.IsNullOrEmpty(mbr.Presence.Activity.Name))
                        {
                            await _discord.UpdateStatusAsync(new DiscordActivity
                            {
                                ActivityType = ActivityType.Watching,
                                Name = $"{GetUnicodeEmoji(":heart_eyes:")} meu criador {mbr.Username}#{mbr.Discriminator} jogar {mbr.Presence.Activity.Name}.",
                            });
                        }
                        else if(mbr.Presence.Activity.ActivityType == ActivityType.ListeningTo)
                        {
                            await _discord.UpdateStatusAsync(new DiscordActivity
                            {
                                ActivityType = ActivityType.ListeningTo,
                                Name = $"{GetUnicodeEmoji(":heart_eyes:")} música boa com meu criador {mbr.Username}#{mbr.Discriminator}.",
                            });
                        }
                    }
                    else
                    {

                        await _discord.UpdateStatusAsync(new DiscordActivity
                        {
                            ActivityType = ActivityType.Watching,
                            Name = $"{GetUnicodeEmoji(":heart_eyes:")} meu criador {mbr.Username}#{mbr.Discriminator} programar.",
                        });
                    }

                    await Task.Delay(6000);
                }
                break;

                case 7:
                {
                    await _discord.UpdateStatusAsync(new DiscordActivity
                    {
                        ActivityType = ActivityType.Streaming,
                        StreamUrl = STREAMING_URL,
                        Name = $"{GetUnicodeEmoji(":rage:")} | Não quebre as regras da comunidade!"
                    });
                }
                break;

                case 8:
                {
                    await _discord.UpdateStatusAsync(new DiscordActivity
                    {
                        Name = $"{GetUnicodeEmoji(":ghost:")} | Não seja um fantasma! Venha interagir com a gente.",
                        ActivityType = ActivityType.Watching
                    });
                }
                break;

                case 9:
                {
                    await _discord.UpdateStatusAsync(new DiscordActivity
                    {
                        Name = $"{GetUnicodeEmoji(":smiling_imp:")} | To te vendo kkkkk",
                        ActivityType = ActivityType.Watching
                    });
                }
                break;

                case 10:
                {
                    await UpdateCreatorActivityAsync();
                }
                break;
            }
        }

        #endregion

        Task OnClientError(ClientErrorEventArgs e)
        {
            Log.Error("[DSharpPlus] Client error in event {0}\n{1}", e.EventName, e.Exception);
            return Task.CompletedTask;
        }

        string GetContextFormat(CommandContext ctx)
        {
            return $"{ctx.Guild.Name} ({ctx.Guild.Id}): #{ctx.Channel.Name} ({ctx.Channel.Id}) -> " +
                $"{ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id}): {ctx.Message.Content}";
        }

        Task OnCommandExecuted(CommandExecutionEventArgs e)
        {
            Log.Info("[DSharpPlus/CommandsNext] Command executed: {0}", GetContextFormat(e.Context));
            return Task.CompletedTask;
        }

        async Task OnCommandErrored(CommandErrorEventArgs e)
        {
            var ctx = e.Context;
            var ex = e.Exception;

            while (ex is AggregateException)
                ex = ex.InnerException;

            if (ex is CommandNotFoundException)
                return;

            if(ex is ChecksFailedException cfe)
            {
                var rpa = (RequirePermissionsAttribute)cfe.FailedChecks.FirstOrDefault(xf => xf is RequirePermissionsAttribute);
                if(rpa != null)
                {
                    await ctx.RespondAsync($"{ctx.User.Mention} :x: Você precisa de permissões para executar isso: `{rpa.Permissions.ToPermissionString()}`");
                    goto log;
                }

                var roa = (RequireOwnerAttribute)cfe.FailedChecks.FirstOrDefault(xf => xf is RequireOwnerAttribute);
                if(roa != null)
                {
                    await ctx.RespondAsync($"{ctx.User.Mention} :x: Você precisa ser meu desenvolvedor para executar isso.");
                    goto log;
                }

                var rbpa = (RequireBotPermissionsAttribute)cfe.FailedChecks.FirstOrDefault(xf => xf is RequireBotPermissionsAttribute);
                if(rbpa != null)
                {
                    await ctx.RespondAsync($"{ctx.User.Mention} :x: Eu preciso de permissões para executar isso: `{rpa.Permissions.ToPermissionString()}`");
                    goto log;
                }

            }

            if(ex is NotImplementedException)
            {
                await ctx.RespondAsync($"{ctx.User.Mention} :x: Não implementado ainda!");
                goto log;
            }

            if (ex is GameStageCommandException gsce)
            {
                await gsce.ThrowAsync(ctx);
                goto log;
            }

            log:
            {
                Log.Error("[DSharpPlus/CommandsNext] Command errored: {0}\n{1}", GetContextFormat(ctx), ex);
                return;
            }
        }
    }
}

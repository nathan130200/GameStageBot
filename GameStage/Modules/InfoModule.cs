using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace GameStage.Modules
{
    public class InfoModule : BaseCommandModule
    {
        [Command, RequireOwner]
        public async Task CreatorAsync(CommandContext ctx, int? size = 15)
        {
            await Program.GetInstance()
                .GetBot()
                .UpdateCreatorActivityAsync(size);

            await ctx.RespondAsync($"{ctx.User.Mention} :white_check_mark: Feito!");
        }

        [Command]
        public async Task PingAsync(CommandContext ctx)
        {
            var deb = new DiscordEmbedBuilder()
                .AddField(":satellite_orbital: Socket", $"{ctx.Client.Ping}ms")
                .AddField(":zap: API", "\u200b")
                .AddField(":leaves: Média", "\u200b");

            var watch = Stopwatch.StartNew();
            await ctx.TriggerTypingAsync();
            watch.Stop();

            var msg = await ctx.RespondAsync(ctx.User.Mention, embed: deb);

            deb.Fields[1].Value = $"{watch.ElapsedMilliseconds}ms";
            deb.Fields[2].Value = $"{(watch.ElapsedMilliseconds + ctx.Client.Ping) / 2}ms";
            await msg.ModifyAsync(ctx.User.Mention, embed: deb.Build());
        }
    }
}

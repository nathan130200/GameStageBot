using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace GameStage
{
    public class GameStageCommandException : Exception
    {
        public string Content { get; }
        public DiscordEmbed Embed { get; }

        public GameStageCommandException(string content, DiscordEmbedBuilder embed = null) : base(Formatter.Strip(content))
        {
            this.Content = content;
            this.Embed = embed;
        }

        public async Task ThrowAsync(CommandContext ctx)
        {
            try
            {
                await ctx.RespondAsync(this.Content, embed: this.Embed);
            }
            catch
            {

            }
        }
    }
}

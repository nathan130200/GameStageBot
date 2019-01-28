using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameStage.Modules
{
    [Group, Description("Comandos referentes ao unturned da comunidade GameStag3.")]
    public class UnturnedModule : BaseCommandModule
    {
        [Command]
        public async Task RegistrarAsync(CommandContext ctx,

            [Description("Identificador da steam para cadastro.")]
            ulong? id = null)
        {
            if (id == null)
                throw new GameStageCommandException($"{ctx.User.Mention} :x: Identificador inválido!");


        }

        [Command]
        public async Task BuscarAsync(CommandContext ctx, [RemainingText] string pesquisa)
        {
            if (pesquisa.StartsWith("/") || pesquisa.StartsWith("\\"))
                pesquisa = pesquisa.Substring(1);

            if (pesquisa.EndsWith("/") || pesquisa.EndsWith("\\"))
                pesquisa.Substring(0, pesquisa.Length - 1);

            var profile = await GetSteamProfileAsync(pesquisa);
            if (profile == null)
                await ctx.RespondAsync($"{ctx.User.Mention} Perfil não encontrado!");
            else
            {
                await ctx.RespondAsync(ctx.User.Mention, embed: new DiscordEmbedBuilder()
                    .WithTitle(profile?.ToString()));
            }
        }

        struct SteamProfileResponse
        {
            public ulong Id;
            public string CustomUrl;
            public string ProfileState;
            public string CreatedAt;
            public string Name;
            public string RealName;
            public string Location;
            public string Status;
            public string Url;

            public override string ToString()
            {
                var str = "";
                var fields = this.GetType().GetFields();

                foreach (var field in fields)
                {
                    str += field.Name + ": " + field.GetValue(this) + ",\n";
                }

                if (str.EndsWith(",\n"))
                    str.Substring(0, str.Length - 2);

                return str;
            }
        }

        async Task<SteamProfileResponse?> GetSteamProfileAsync(string id)
        {
            try
            {
                var html = new HtmlDocument();

                using (var client = new HttpClient())
                using (var response = await client.GetAsync($"https://steamid.io/lookup/{id}"))
                {
                    await response.Content.ReadAsStringAsync().ContinueWith(t => html.LoadHtml(t.Result));

                    var result = new SteamProfileResponse();

                    result.Id = ulong.Parse(html.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/dl/dd[3]/a").InnerText);
                    result.CustomUrl = html.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/dl/dd[4]/a").InnerText;
                    result.ProfileState = html.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/dl/dd[5]/span").InnerText;
                    result.CreatedAt = html.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/dl/dd[6]").InnerText;
                    result.Name = html.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/dl/dd[7]").InnerText;
                    result.RealName = html.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/dl/dd[8]").InnerText;
                    result.Location = html.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/dl/dd[9]/a").InnerText;
                    result.Status = html.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/dl/dd[10]/span").InnerText;
                    result.Url = html.DocumentNode.SelectSingleNode("//*[@id=\"content\"]/dl/dd[11]/a").InnerText;

                    return result;
                }
            }
            catch (Exception ex)
            {
                Log.Error(nameof(UnturnedModule) + " GetSteamProfile(): [{0}]\n{1}", id, ex);
                return null;
            }
        }
    }
}
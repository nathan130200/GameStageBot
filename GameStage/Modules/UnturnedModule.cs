using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GameStage.Modules
{
    [Group, Aliases("u")]
    public class UnturnedModule : BaseCommandModule
    {
        [Command]
        public async Task RegistrarAsync(CommandContext ctx, ulong? id = null)
        {
            if (id == null)
                throw new GameStageCommandException($"{ctx.User.Mention} :x: Você precisa fornecer um identificador válido para steam.");

            throw new NotImplementedException();
        }

        [Group, ModuleLifespan(ModuleLifespan.Singleton)]
        public class TestModule : BaseCommandModule
        {
            volatile bool _alldone;
            FontCollection _fonts;
            HttpClient _client;

            public TestModule()
            {
                _fonts = new FontCollection();
                _fonts.Install(@".\Resources\Fonts\Bombardier.ttf");

                _client = new HttpClient();
                _alldone = true;
            }

            [Command]
            public async Task RegistrarAsync(CommandContext ctx, bool state = true)
            {
                var msg = await ctx.RespondAsync($"{ctx.User.Mention} verificando...");

                if (!_alldone)
                    msg = await msg.ModifyAsync($"{ctx.User.Mention} na fila...");


                while (!_alldone)
                    await Task.Delay(100);


                _alldone = false;

                msg = await msg.ModifyAsync($"{ctx.User.Mention} preparando...");


                var avatarstream = await _client.GetStreamAsync(ctx.User.GetAvatarUrl(ImageFormat.Png));
                var avatar = Image.Load<Rgba32>(avatarstream);
                avatar.Mutate(x =>
                {
                    x.Resize(64, 64);
                });


                var img = new Image<Rgba32>(480, 320);

                var text = new Image<Rgba32>(480, 320);
                text.Mutate(x =>
                {

                    var fnt = _fonts.Families.ElementAt(0).CreateFont(32f);
                    x.DrawText($"{ctx.User.Username}#{ctx.User.Discriminator}", fnt, Brushes.Solid(Rgba32.White), Pens.Solid(Rgba32.Black, 1f), new PointF(83, 166));
                });

                var statetext = new Image<Rgba32>(480, 320);
                statetext.Mutate(x =>
                {
                    var fnt = _fonts.Families.ElementAt(0).CreateFont(24f);

                    var pn = Pens.Solid(Rgba32.DarkGreen, 1f);
                    var bs = Brushes.Solid(Rgba32.Green);

                    var r = "Aprovado";

                    if (!state)
                    {
                        r = "Reprovado";
                        pn = Pens.Solid(Rgba32.DarkRed, 1f);
                        bs = Brushes.Solid(Rgba32.Red);
                    }

                    x.DrawText(r, fnt, bs, pn, new PointF(82, 189));
                });

                var timestamp = new Image<Rgba32>(480, 320);
                timestamp.Mutate(x =>
                {
                    var fnt = _fonts.Families.ElementAt(0).CreateFont(24f);

                    x.DrawText(DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss"), fnt, Brushes.Solid(Rgba32.White), Pens.Solid(Rgba32.Black, 0.8f), new PointF(82, 213));
                });

                img.Mutate(x =>
                {
                    x.DrawImage(avatar, new Point(10, 165), 1f);
                    x.DrawImage(text, 1f);
                    x.DrawImage(statetext, 1f);
                    x.DrawImage(timestamp, 1f);
                });

                var ms = new MemoryStream();
                img.SaveAsPng(ms);

                ms.Position = 0;

                msg = await msg.ModifyAsync($"{ctx.User.Mention} finalizando...");
                await ctx.RespondWithFileAsync($"u_test_registrar_{ctx.User.Id}.png", ms, ctx.User.Mention);
                await msg.DeleteAsync();

                ms.Dispose();
                ms = null;

                timestamp.Dispose();
                timestamp = null;

                statetext.Dispose();
                statetext = null;

                avatar.Dispose();
                avatar = null;

                img.Dispose();
                img = null;

                avatarstream.Dispose();
                avatarstream = null;

                GC.Collect();
                var notify = GC.WaitForFullGCComplete(-1);
                await ctx.RespondAsync($"{ctx.User.Mention} GC::WaitForFullGCComplete(): {notify}");

                _alldone = true;
            }
        }
    }
}

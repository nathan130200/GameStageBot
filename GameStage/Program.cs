using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameStage
{
    public class Program
    {
        internal static CancellationTokenSource _cts = new CancellationTokenSource(); 
        internal static Program _instance;
        internal GameStageBot _bot;

        public Program()
        {
            _instance = this;
            _bot = new GameStageBot();
        }

        public Task StartAsync()
            => _bot.StartAsync();

        public Task StopAsync()
            => _bot.StopAsync();

        static void Main(string[] args)
        {
            try
            {
                MainAsync().GetAwaiter().GetResult();
            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }

        static async Task MainAsync()
        {
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                _cts.Cancel();
            };

            var program = new Program();
            await program.StartAsync();

            while (!_cts.IsCancellationRequested)
                await Task.Delay(100);

            await program.StopAsync();
        }

        public static Program GetInstance()
            => _instance;

        public GameStageBot GetBot()
            => _bot;
    }
}

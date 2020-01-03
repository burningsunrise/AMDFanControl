using System;
using System.Threading;
using System.Threading.Tasks;

namespace Fancontrol
{
    internal static class Program
    {
        private static readonly System.Timers.Timer Timer = new System.Timers.Timer();
        private static readonly SetSingleSpeed FanController = new SetSingleSpeed();

        private static async Task Main(string[] args)
        {
            if (args.Length == 4)
            {
                try
                {
                    await FanController.SetFanSingleFanSpeed(FanController.DirectoryNames.Find(card => card.Contains("card" + args[1])), Convert.ToInt32(args[3]));
                    await Console.Out.WriteLineAsync($"Changed GPU {args[1]} fan to {args[3]}%");
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync($"Couldn't change single speed: {ex}");
                }
                
            }
            else if (args.Length == 2)
            {
                await FanController.SetAllFanSpeed(Convert.ToInt32(args[1]));
                Console.WriteLine($"All fans set to: {args[1]}%");
            }
            else if (args.Length <= 0)
            {
                Console.WriteLine($"Fancontrol Started: {DateTime.Now}");
                Timer.Elapsed += TimerElapsed;
                Timer.Interval = 5000;
                Timer.Enabled = true;
                TimerElapsed(Timer, null);
                Thread.Sleep(Timeout.Infinite);
            }
            else
            {
                Console.WriteLine("Please try again with proper arguments ex (-card 1 -speed 0): -c 1 -s 0");
            }
        }

        private static async void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await FanController.GetTemps();
            await FanController.AutomaticFanControl();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading;

namespace Fancontrol
{
    class Program
    {
        private static readonly System.Timers.Timer timer = new System.Timers.Timer();
        private static readonly SetSingleSpeed fanController = new SetSingleSpeed();

        static void Main(string[] args)
        {
            Console.WriteLine($"Fancontrol Started: {DateTime.Now}");
            timer.Elapsed += TimerElapsed;
            timer.Interval = 5000;
            timer.Enabled = true;
            TimerElapsed(timer, null);
            Thread.Sleep(Timeout.Infinite);

        }

        static async void TimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            await fanController.GetTemps();
            await fanController.AutomaticFanControl();
        }
    }
}

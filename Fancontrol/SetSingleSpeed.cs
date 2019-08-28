using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Fancontrol
{
    class SetSingleSpeed
    {
        public SetSingleSpeed()
        {
            GetInformation();
        }

        public List<string> DirectoryNames { get; set; } = new List<string>();
        public List<string> CardDirectoryNames { get; set; }
        public Dictionary<string, int> Cards { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, DateTime> OverHeatingCards { get; set; } = new Dictionary<string, DateTime>();
        public int FanMax { get; set; }
        public int FanSpeed { get; set; }
        public double PercentageRange { get; set; } = 5.0;

        private void GetInformation()
        {
            try
            {
                CardDirectoryNames = new List<string>(Directory.GetDirectories($"/sys/class/drm/", "card?"));
            }
            catch (DirectoryNotFoundException dirEx)
            {
                Console.WriteLine($"Couldn't find card directory: {dirEx}");
            }
            foreach (var cardDir in CardDirectoryNames)
            {
                try
                {
                    DirectoryNames.Add(Directory.GetDirectories($"{cardDir}/device/hwmon/", "hwmon?")[0]);
                }
                catch (DirectoryNotFoundException)
                {
                    continue;
                }
            }
        }

        public async Task GetTemps()
        {
            foreach (var directory in DirectoryNames)
            {
                try
                {
                    using (StreamReader reader = File.OpenText(directory + "/temp1_input"))
                    {
                        // if you want output to console you need to declare a variable like int temp = reader.ReadLine();
                        Cards[directory] = Convert.ToInt32(await reader.ReadLineAsync()) / 1000;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Couldn't get temp: {ex}");
                }
            }
        }

        public async Task AutomaticFanControl()
        {
            foreach (KeyValuePair<string, int> cardInformation in Cards)
            {
                if (OverHeatingCards.TryGetValue(cardInformation.Key, out DateTime dateTime))
                {
                    if (OverHeatingCards[cardInformation.Key] > DateTime.Now)
                        await SetFanSingleFanSpeed(cardInformation.Key, 100);
                    else if (OverHeatingCards[cardInformation.Key] <= DateTime.Now)
                        OverHeatingCards.Remove(cardInformation.Key);

                }
                else
                {
                    if (cardInformation.Value >= 80)
                    {
                        await SetFanSingleFanSpeed(cardInformation.Key, 100);
                        OverHeatingCards[cardInformation.Key] = DateTime.Now.AddHours(1);
                    }
                    else if (cardInformation.Value >= 75)
                        await SetFanSingleFanSpeed(cardInformation.Key, 100);
                    else if (cardInformation.Value >= 70)
                        await SetFanSingleFanSpeed(cardInformation.Key, 95);
                    else if (cardInformation.Value >= 65)
                        await SetFanSingleFanSpeed(cardInformation.Key, 85);
                    else if (cardInformation.Value >= 55)
                        await SetFanSingleFanSpeed(cardInformation.Key, 80);
                    else if (cardInformation.Value >= 50)
                        await SetFanSingleFanSpeed(cardInformation.Key, 75);
                    else if (cardInformation.Value >= 40)
                        await SetFanSingleFanSpeed(cardInformation.Key, 70);
                    else if (cardInformation.Value >= 35)
                        await SetFanSingleFanSpeed(cardInformation.Key, 55);
                    else if (cardInformation.Value >= 30)
                        await SetFanSingleFanSpeed(cardInformation.Key, 20);
                    else
                        await SetFanSingleFanSpeed(cardInformation.Key, 100);
                }

            }
        }

        private async Task SetFanSingleFanSpeed(string card, int speed)
        {
            try
            {
                using (StreamReader reader = File.OpenText(card + "/pwm1_max"))
                {
                    FanMax = Convert.ToInt32(await reader.ReadLineAsync());
                }
                FanSpeed = (Convert.ToInt32(FanMax * speed)) / 100;

                using (StreamReader reader = File.OpenText(card + "/pwm1_enable"))
                {
                    if (await reader.ReadLineAsync() != "1")
                    {
                        using (StreamWriter writer = File.CreateText(card + "/pwm1_enable"))
                        {
                            await writer.WriteAsync("1");
                        }
                    }
                }
                using (StreamReader reader = File.OpenText(card + "/pwm1"))
                {
                    int fileFanSpeed = Convert.ToInt32(await reader.ReadLineAsync());
                    if (!WithinPercent(FanSpeed, fileFanSpeed, speed) && fileFanSpeed != FanSpeed)
                    {
                        using (StreamWriter writer = File.CreateText(card + "/pwm1"))
                        {
                            char[] charFanSpeed = FanSpeed.ToString().ToCharArray();
                            await writer.WriteAsync(charFanSpeed, 0, charFanSpeed.Length);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an error setting the single fan speed: {ex}");
            }
        }

        private bool WithinPercent(int num1, int num2, int speed)
        {
            double firstPercentage = ((double)num1 / 100) * PercentageRange;
            double secondPercentage = ((double)num2 / 100) * PercentageRange;
            double final = ((Math.Abs(firstPercentage - secondPercentage)) / firstPercentage) * 100;
            if (speed >= 85)
                return false;
            if (final <= PercentageRange)
                return true;
            else
                return false;
        }
    }
}

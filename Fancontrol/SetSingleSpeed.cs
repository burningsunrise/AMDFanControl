using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Fancontrol
{
    internal class SetSingleSpeed
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
                    
                    if (Directory.Exists($"{cardDir}/device/hwmon/"))
                    {
                        DirectoryNames.Add(Directory.GetDirectories($"{cardDir}/device/hwmon/", "hwmon?")[0]);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Probably picked up integrated gpu: {ex}");
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
            foreach (var (key, value) in Cards)
            {
                if (OverHeatingCards.TryGetValue(key, out var dateTime))
                {
                    if (OverHeatingCards[key] > DateTime.Now)
                        await SetFanSingleFanSpeed(key, 100);
                    else if (OverHeatingCards[key] <= DateTime.Now)
                        OverHeatingCards.Remove(key);

                }
                else
                {
                    if (value >= 80)
                    {
                        await SetFanSingleFanSpeed(key, 100);
                        OverHeatingCards[key] = DateTime.Now.AddHours(1);
                    }
                    else if (value >= 75)
                        await SetFanSingleFanSpeed(key, 100);
                    else if (value >= 70)
                        await SetFanSingleFanSpeed(key, 95);
                    else if (value >= 65)
                        await SetFanSingleFanSpeed(key, 85);
                    else if (value >= 55)
                        await SetFanSingleFanSpeed(key, 80);
                    else if (value >= 50)
                        await SetFanSingleFanSpeed(key, 75);
                    else if (value >= 40)
                        await SetFanSingleFanSpeed(key, 70);
                    else if (value >= 35)
                        await SetFanSingleFanSpeed(key, 55);
                    else if (value >= 30)
                        await SetFanSingleFanSpeed(key, 20);
                    else
                        await SetFanSingleFanSpeed(key, 100);
                }

            }
        }

        public async Task SetFanSingleFanSpeed(string card, int speed)
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
                    try
                    {
                        var fileFanSpeed = Convert.ToInt32(await reader.ReadLineAsync());
                        if (!WithinPercent(FanSpeed, fileFanSpeed, speed) && fileFanSpeed != FanSpeed)
                        {
                            using (StreamWriter writer = File.CreateText(card + "/pwm1"))
                            {
                                char[] charFanSpeed = FanSpeed.ToString().ToCharArray();
                                await writer.WriteAsync(charFanSpeed, 0, charFanSpeed.Length);
                            }
                        }
                        
                    }
                    catch (Exception)
                    {
                        // Catching no device errors from reader.ReadLineAsync() when a fan is not present
                        // pwm1 doesn't play well with no fans present
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"There was an error setting the single fan speed: {ex}");
            }
        }

        public async Task SetAllFanSpeed(int speed)
        {
            foreach (var card in DirectoryNames)
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
                        try
                        {
                            var fileFanSpeed = Convert.ToInt32(await reader.ReadLineAsync());
                            if (!WithinPercent(FanSpeed, fileFanSpeed, speed) && fileFanSpeed != FanSpeed)
                            {
                                using (StreamWriter writer = File.CreateText(card + "/pwm1"))
                                {
                                    char[] charFanSpeed = FanSpeed.ToString().ToCharArray();
                                    await writer.WriteAsync(charFanSpeed, 0, charFanSpeed.Length);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Catching no device errors from reader.ReadLineAsync() when a fan is not present
                            // pwm1 doesn't play well with no fans present
                        }
                    }
                }

                catch (Exception ex)
                {
                    Console.WriteLine($"There was an error setting all fan speeds: {ex}");
                }
            }
        }

        private bool WithinPercent(int num1, int num2, int speed)
        {
            var firstPercentage = ((double)num1 / 100) * PercentageRange;
            var secondPercentage = ((double)num2 / 100) * PercentageRange;
            var final = ((Math.Abs(firstPercentage - secondPercentage)) / firstPercentage) * 100;
            if (speed >= 85)
                return false;
            return final <= PercentageRange;
        }
    }
}

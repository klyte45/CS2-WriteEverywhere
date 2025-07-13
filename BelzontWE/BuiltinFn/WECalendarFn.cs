using Game.Simulation;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE.Builtin
{
    public static class WECalendarFn
    {
        private static TimeSystem timeSystem;
        public static string GetTimeStringWeLocale(Entity reference, Dictionary<string, string> vars)
        {
            timeSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimeSystem>();
            var time = timeSystem.normalizedTime * 24;
            var formatter = WEModData.InstanceWE.FormatCulture.DateTimeFormat;
            if (formatter.ShortTimePattern.Contains("tt"))
            {
                var isPM = time >= 12;
                var timeOfDayIndicator = isPM ? vars.TryGetValue("pm", out var pm) ? pm : formatter.PMDesignator : vars.TryGetValue("am", out var am) ? am : formatter.AMDesignator;
                return $"{Math.Floor(((time + 23) % 12) + 1):#0}:{math.floor(time * 60 % 60):00}{timeOfDayIndicator}";
            }
            else
            {
                return $"{Math.Floor(time):00}:{math.floor(time * 60 % 60):00}";
            }
        }
        public static string GetFormattedDateWeLocale(Entity reference, Dictionary<string, string> vars)
        {
            timeSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimeSystem>();
            var time = timeSystem.GetCurrentDateTime();
            time.AddMonths(time.Day - time.Month);
            var formatter = WEModData.InstanceWE.FormatCulture.DateTimeFormat;

            var format = vars.TryGetValue("dateFormat", out var dateFormat) ? dateFormat : formatter.YearMonthPattern;

            return time.ToString(format, formatter);

        }
    }
}
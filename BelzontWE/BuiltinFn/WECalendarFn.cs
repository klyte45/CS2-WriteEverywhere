using Game.Simulation;
using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Entities;
using Unity.Mathematics;

namespace BelzontWE.Builtin
{
    [WEBuiltinFunction("Calendar")]
    public static class WECalendarFn
    {
        private static TimeSystem timeSystem;

        public static Func<float> GetNormalizedTime_binding = () =>
        {
            timeSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimeSystem>();
            return timeSystem.normalizedTime;
        };

        public static Func<DateTime> GetCurrentDateTime_binding = () =>
        {
            timeSystem ??= World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<TimeSystem>();
            return timeSystem.GetCurrentDateTime();
        };

        public static Func<DateTimeFormatInfo> GetDateTimeFormatter_binding = () =>
            WEModData.InstanceWE.FormatCulture.DateTimeFormat;

        [WEFormula(typeof(string))]
        public static string GetTimeStringWeLocale(Entity reference, Dictionary<string, string> vars)
        {
            var time = GetNormalizedTime_binding() * 24;
            var formatter = GetDateTimeFormatter_binding();
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
        [WEFormula(typeof(string))]
        public static string GetFormattedDateWeLocale(Entity reference, Dictionary<string, string> vars)
        {
            var time = GetCurrentDateTime_binding();
            time = time.AddMonths(time.Day - time.Month);
            var formatter = GetDateTimeFormatter_binding();

            var format = vars.TryGetValue("dateFormat", out var dateFormat) ? dateFormat : $"MMM{formatter.DateSeparator}yyyy";

            return time.ToString(format, formatter);

        }
    }
}
﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Assembly_Bot
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
        {
            int diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static DateTime EndOfWeek(this DateTime dt, DayOfWeek startOfWeek) => StartOfWeek(dt, startOfWeek).AddDays(6);
    }
}
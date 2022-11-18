﻿namespace Tweey.Support;

readonly struct CustomDateTime : IAdditionOperators<CustomDateTime, CustomDateTime, TimeSpan>, ISubtractionOperators<CustomDateTime, CustomDateTime, TimeSpan>,
    IAdditionOperators<CustomDateTime, TimeSpan, CustomDateTime>, ISubtractionOperators<CustomDateTime, TimeSpan, CustomDateTime>,
    IComparisonOperators<CustomDateTime, CustomDateTime, bool>
{
    readonly TimeSpan timeSpan;

    public static readonly CustomDateTime Invalid = new(TimeSpan.FromTicks(-100000));

    public CustomDateTime(TimeSpan timeSpan) =>
        this.timeSpan = timeSpan;

    const int SecondsPerMinute = 60;
    const int MinutesPerHour = 60;
    const int HoursPerDay = 24;
    const int DaysPerMonth = 30;
    const int MonthsPerYear = 12;

    void GetComponents(out int year, out int month, out int day,
        out int hour, out int minute, out int second)
    {
        var wt = timeSpan.TotalSeconds;
        second = (int)(wt % SecondsPerMinute); wt /= SecondsPerMinute;
        minute = (int)(wt % MinutesPerHour); wt /= MinutesPerHour;
        hour = (int)(wt % HoursPerDay); wt /= HoursPerDay;
        day = (int)(wt % DaysPerMonth); wt /= DaysPerMonth;
        month = (int)(wt % MonthsPerYear); wt /= MonthsPerYear;
        year = (int)(wt);
    }

    public TimeSpan TimeOfDay
    {
        get
        {
            GetComponents(out _, out _, out _, out var hour, out var minute, out var second);
            return new(hour, minute, second);
        }
    }

    public override string ToString()
    {
        GetComponents(out var year, out var month, out var day, out var hour, out var minute, out _);
        return $"{year + 1:00}-{month + 1:00}-{day + 1:00} {hour:00}:{minute:00}";
    }

    public static TimeSpan operator +(CustomDateTime left, CustomDateTime right) =>
        left.timeSpan + right.timeSpan;

    public static TimeSpan operator -(CustomDateTime left, CustomDateTime right) =>
        left.timeSpan - right.timeSpan;

    public static bool operator >(CustomDateTime left, CustomDateTime right) => left.timeSpan > right.timeSpan;
    public static bool operator >=(CustomDateTime left, CustomDateTime right) => left.timeSpan >= right.timeSpan;
    public static bool operator <(CustomDateTime left, CustomDateTime right) => left.timeSpan < right.timeSpan;
    public static bool operator <=(CustomDateTime left, CustomDateTime right) => left.timeSpan <= right.timeSpan;
    public static bool operator ==(CustomDateTime left, CustomDateTime right) => left.timeSpan == right.timeSpan;
    public static bool operator !=(CustomDateTime left, CustomDateTime right) => left.timeSpan != right.timeSpan;

    public static CustomDateTime operator +(CustomDateTime left, TimeSpan right) => new(left.timeSpan + right);
    public static CustomDateTime operator -(CustomDateTime left, TimeSpan right) => new(left.timeSpan - right);
}
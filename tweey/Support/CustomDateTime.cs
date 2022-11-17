namespace Tweey.Support;

readonly struct CustomDateTime
{
    readonly TimeSpan timeSpan;

    public CustomDateTime(TimeSpan timeSpan) =>
        this.timeSpan = timeSpan;

    void GetComponents(out int year, out int month, out int day,
        out int hour, out int minute, out int second)
    {
        var wt = timeSpan.TotalSeconds;
        second = (int)(wt % 60); wt /= 60;
        minute = (int)(wt % 60); wt /= 60;
        hour = (int)(wt % 24); wt /= 24;
        day = (int)(wt % 30 + 1); wt /= 30;
        month = (int)(wt % 12 + 1); wt /= 12;
        year = (int)(wt + 1);
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
        return $"{year:00}-{month:00}-{day:00} {hour:00}:{minute:00}";
    }
}

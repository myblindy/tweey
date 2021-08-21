namespace Tweey.Support
{
    public static class ThreadSafeRandom
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        [ThreadStatic] private static Random Local;     // this field will always be initialized when used
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

        public static Random ThisThreadsRandom =>
            Local ??= new Random(unchecked(Environment.TickCount * 31 + Environment.CurrentManagedThreadId));
    }
}

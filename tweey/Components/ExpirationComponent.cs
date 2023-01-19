namespace Tweey.Components;

[EcsComponent]
struct ExpirationComponent
{
    public CustomDateTime Date { get; set; }
    public Func<bool>? IsExpired { get; set; }

    public ExpirationComponent(CustomDateTime expirationDate) => Date = expirationDate;
    public ExpirationComponent(Func<bool> isExpiredFunc) => (Date, IsExpired) = (CustomDateTime.MaxValue, isExpiredFunc);
}
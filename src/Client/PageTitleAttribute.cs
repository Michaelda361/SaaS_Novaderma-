namespace TalentManagement.Client;

/// <summary>
/// Título mostrado en la barra superior (<see cref="Layout.MainLayout"/>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class PageTitleAttribute : Attribute
{
    public string Title { get; }

    public PageTitleAttribute(string title) =>
        Title = string.IsNullOrWhiteSpace(title) ? throw new ArgumentException("Title is required.", nameof(title)) : title;
}

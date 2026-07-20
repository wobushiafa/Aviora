namespace Aviora.Demo.ViewModels;

public abstract class DemoPageViewModel(string title) : ViewModelBase
{
    public string Title { get; } = title;
}

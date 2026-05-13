namespace WorkChat;

using System.Collections.Specialized;

using WorkChat.ViewModels;

public partial class MainPage : ContentPage
{
    private readonly MainPageViewModel viewModel;

    public MainPage(MainPageViewModel viewModel)
    {
        InitializeComponent();

        this.viewModel = viewModel;
        BindingContext = viewModel;

        viewModel.Messages.CollectionChanged += OnMessagesChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        viewModel.Messages.CollectionChanged -= OnMessagesChanged;
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add)
        {
            return;
        }

        if (viewModel.Messages.Count == 0)
        {
            return;
        }

        Dispatcher.Dispatch(() =>
            MessagesView.ScrollTo(viewModel.Messages.Count - 1, position: ScrollToPosition.End, animate: true));
    }
}

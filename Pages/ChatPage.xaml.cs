// Файл: Pages/ChatPage.xaml.cs
using YandexMessengerMAUI.ViewModels;

namespace YandexMessengerMAUI.Pages;

public partial class ChatPage : ContentPage
{
    private readonly ChatViewModel _vm;
    private readonly string _recipientUsername;

    public ChatPage(string recipientUsername)
    {
        InitializeComponent();
        _recipientUsername = recipientUsername;
        _vm = new ChatViewModel(App.Api, App.Auth, App.SignalR);
        BindingContext = _vm;

        // Прокручиваем список вниз при новом сообщении
        _vm.ScrollToBottom += () =>
        {
            if (_vm.Messages.Count > 0)
                MessagesList.ScrollTo(_vm.Messages[^1], animate: true);
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync(_recipientUsername);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Cleanup();
    }
}

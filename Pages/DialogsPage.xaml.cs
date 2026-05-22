// Файл: Pages/DialogsPage.xaml.cs
using YandexMessengerMAUI.Models;
using YandexMessengerMAUI.ViewModels;

namespace YandexMessengerMAUI.Pages;

public partial class DialogsPage : ContentPage
{
    private readonly DialogsViewModel _vm;

    public DialogsPage()
    {
        InitializeComponent();
        _vm = new DialogsViewModel(App.Api, App.Auth, App.SignalR);
        BindingContext = _vm;

        _vm.OpenChat += async username =>
            await Navigation.PushAsync(new ChatPage(username));

        _vm.OpenNewChat += async () =>
            await Navigation.PushAsync(new NewChatPage());

        _vm.LoggedOut += async () =>
        {
            await Navigation.PushAsync(new LoginPage(), animated: false);
            // Убираем всё из стека кроме LoginPage
            var pages = Navigation.NavigationStack.ToList();
            foreach (var page in pages.SkipLast(1))
                Navigation.RemovePage(page);
        };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _vm.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Cleanup();
    }

    private void OnDialogTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Frame frame && frame.BindingContext is Dialog dialog)
            _vm.OpenChatWith(dialog.Username);
    }
}

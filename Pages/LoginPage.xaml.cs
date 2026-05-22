// Файл: Pages/LoginPage.xaml.cs

using YandexMessengerMAUI.ViewModels;

namespace YandexMessengerMAUI.Pages;

public partial class LoginPage : ContentPage
{
    private readonly LoginViewModel _vm;

    public LoginPage()
    {
        InitializeComponent();
        _vm = new LoginViewModel(App.Api, App.Auth);
        BindingContext = _vm;

        // После успешного входа — переходим на список чатов
        _vm.LoginSucceeded += async () =>
        {
            await Navigation.PushAsync(new DialogsPage(), animated: true);
            // Убираем экран входа из стека (нельзя вернуться назад)
            Navigation.RemovePage(this);
        };

        // Новый пользователь — нужно выбрать юзернейм
        _vm.NeedsUsername += async (_) =>
        {
            await Navigation.PushAsync(new SetUsernamePage(), animated: true);
        };
    }

    /// <summary>
    /// Вызывается из App.xaml.cs когда получен deep link от Яндекса.
    /// </summary>
    public async Task HandleCallbackAsync(Uri callbackUri)
    {
        await _vm.HandleOAuthCallbackAsync(callbackUri);
    }
}

// Файл: Pages/SplashPage.xaml.cs
// При старте проверяет сохранённую сессию и перенаправляет
// либо на LoginPage, либо на DialogsPage.

using YandexMessengerMAUI.Services;

namespace YandexMessengerMAUI.Pages;

public partial class SplashPage : ContentPage
{
    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Небольшая пауза для показа сплэша
        await Task.Delay(800);

        // Пробуем восстановить сессию из SecureStorage
        var hasSession = await App.Auth.TryRestoreSessionAsync();

        if (hasSession)
        {
            // Токен валиден — сразу открываем список чатов
            await Navigation.PushAsync(new DialogsPage(), animated: false);
        }
        else
        {
            // Нет сессии — экран входа
            await Navigation.PushAsync(new LoginPage(), animated: false);
        }

        // Убираем SplashPage из стека навигации
        Navigation.RemovePage(this);
    }
}

// Файл: App.xaml.cs
// Точка входа приложения.
// Здесь: DI, первоначальная навигация, обработка deep link от Яндекс OAuth.

using YandexMessengerMAUI.Pages;
using YandexMessengerMAUI.Services;
using YandexMessengerMAUI.ViewModels;
using Microsoft.Maui.Controls;

namespace YandexMessengerMAUI;

public partial class App : Application
{
    // Сервисы — создаём один раз, передаём везде (простой DI без контейнера)
    public static ApiService Api { get; } = new();
    public static AuthService Auth { get; } = new(Api);
    public static SignalRService SignalR { get; } = new();

    public App()
    {
        InitializeComponent();
        MainPage = new NavigationPage(new SplashPage());
    }

    /// <summary>
    /// Перехват deep link.
    /// Яндекс после OAuth редиректит на:
    ///   yandexmessenger://callback?token=JWT&username=alice&needs_username=false
    ///
    /// На Android нужно зарегистрировать схему в AndroidManifest.xml.
    /// На Windows — через Protocol в Package.appxmanifest.
    /// </summary>
    protected override async void OnAppLinkRequestReceived(Uri uri)
    {
        base.OnAppLinkRequestReceived(uri);

        if (uri.Scheme != "yandexmessenger") return;
        if (uri.Host != "callback") return;

        // Находим LoginPage в стеке навигации и передаём ему callback
        if (MainPage is NavigationPage navPage)
        {
            foreach (var page in navPage.Navigation.NavigationStack)
            {
                if (page is LoginPage loginPage)
                {
                    await loginPage.HandleCallbackAsync(uri);
                    return;
                }
            }
        }
    }
}

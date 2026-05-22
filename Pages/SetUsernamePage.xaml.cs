// Файл: Pages/SetUsernamePage.xaml.cs
using YandexMessengerMAUI.ViewModels;

namespace YandexMessengerMAUI.Pages;

public partial class SetUsernamePage : ContentPage
{
    public SetUsernamePage()
    {
        InitializeComponent();
        var vm = new SetUsernameViewModel(App.Api, App.Auth);
        BindingContext = vm;

        vm.UsernameSet += async () =>
        {
            // Юзернейм выбран — переходим к чатам, очищаем стек
            await Navigation.PushAsync(new DialogsPage(), animated: true);
            // Убираем LoginPage и SetUsernamePage из стека
            var pages = Navigation.NavigationStack.ToList();
            foreach (var page in pages.SkipLast(1))
                Navigation.RemovePage(page);
        };
    }
}

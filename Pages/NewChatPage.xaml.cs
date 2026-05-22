// Файл: Pages/NewChatPage.xaml.cs
using YandexMessengerMAUI.ViewModels;

namespace YandexMessengerMAUI.Pages;

public partial class NewChatPage : ContentPage
{
    public NewChatPage()
    {
        InitializeComponent();
        var vm = new NewChatViewModel(App.Api);
        BindingContext = vm;

        vm.UserFound += async username =>
        {
            // Открываем чат и убираем экран поиска из стека
            await Navigation.PushAsync(new ChatPage(username));
            Navigation.RemovePage(this);
        };
    }
}

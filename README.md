# 📱 Yandex Messenger — MAUI Клиент

Клиентское приложение на .NET MAUI для Android и Windows.
Общается с бэкендом через HTTP REST + SignalR WebSocket.

---

## 🗂 Структура проекта

```
YandexMessengerMAUI/
│
├── AppConstants.cs              ← URL сервера (МЕНЯЙТЕ ЗДЕСЬ)
├── App.xaml / App.xaml.cs       ← Глобальные ресурсы, DI, deep link
├── MauiProgram.cs               ← Точка входа MAUI
│
├── Models/
│   └── Models.cs                ← Все модели данных (AuthResponse, ChatMessage, Dialog…)
│
├── Services/
│   ├── ApiService.cs            ← Все HTTP-запросы к бэкенду
│   ├── SignalRService.cs        ← WebSocket соединение (SignalR)
│   └── AuthService.cs          ← Управление JWT сессией (SecureStorage)
│
├── ViewModels/
│   ├── BaseViewModel.cs         ← INotifyPropertyChanged, RelayCommand
│   ├── LoginViewModel.cs        ← Логика экрана входа + OAuth callback
│   ├── SetUsernameViewModel.cs  ← Выбор юзернейма при первом входе
│   ├── DialogsViewModel.cs      ← Список чатов + SignalR события
│   ├── ChatViewModel.cs         ← Переписка + отправка/получение
│   └── NewChatViewModel.cs     ← Поиск пользователя
│
├── Pages/
│   ├── SplashPage.xaml/.cs      ← Загрузка: проверка сессии → роутинг
│   ├── LoginPage.xaml/.cs       ← Кнопка «Войти через Яндекс»
│   ├── SetUsernamePage.xaml/.cs ← Выбор @username (только новые)
│   ├── DialogsPage.xaml/.cs     ← Список чатов (главный экран)
│   ├── ChatPage.xaml/.cs        ← Переписка с пользователем
│   └── NewChatPage.xaml/.cs    ← Поиск нового собеседника
│
├── Converters/
│   └── Converters.cs            ← Конвертеры для XAML биндингов
│
└── Platforms/
    ├── Android/AndroidManifest.xml   ← Deep link схема yandexmessenger://
    └── Windows/Package.appxmanifest  ← Deep link протокол для Windows
```

---

## 🔗 Взаимодействие клиента с сервером

### HTTP REST (ApiService.cs)

| Действие | Метод | URL |
|----------|-------|-----|
| Получить ссылку OAuth | GET | `/api/auth/login` |
| Проверить JWT | GET | `/api/auth/me` |
| Установить юзернейм | POST | `/api/auth/set-username` |
| Отправить сообщение | POST | `/api/messages` |
| История чата | GET | `/api/messages/history?withUsername=alice` |
| Список диалогов | GET | `/api/messages/dialogs` |
| Найти пользователя | GET | `/api/messages/find-user?username=alice` |

### WebSocket / SignalR (SignalRService.cs)

- Подключение: `ws://localhost:5000/chatHub?access_token=JWT`
- **Сервер → клиент:** `ReceiveMessage` — новое сообщение
- **Сервер → клиент:** `MessagesRead` — собеседник прочитал
- **Клиент → сервер:** `MarkAsRead(username)` — отмечаем прочитанным
- **Клиент → сервер:** `Ping` — проверка соединения

---

## ⚙️ Шаг 1: Что нужно установить

### .NET 8 SDK + MAUI workload

```powershell
# Проверить текущие SDK
dotnet --list-sdks

# Если .NET 8 не установлен:
# Скачать с https://dotnet.microsoft.com/download/dotnet/8.0

# Установить MAUI workload (в PowerShell от администратора)
dotnet workload install maui
dotnet workload install maui-android
dotnet workload install maui-windows
```

### Visual Studio 2022

При установке включить компоненты:
- ✅ «.NET Multi-platform App UI development» (MAUI)
- ✅ «ASP.NET and web development»
- ✅ Android SDK (включается автоматически с MAUI)

### Android SDK (если нужен Android)

Visual Studio установит его автоматически. Либо через Android Studio.
Нужна версия Android API 21+ (Android 5.0+).

---

## ⚙️ Шаг 2: Настройка

### 2.1 URL сервера

Откройте `AppConstants.cs` и проверьте константу:

```csharp
// Для запуска на Android-эмуляторе (10.0.2.2 = localhost хост-машины):
#if ANDROID
    public const string ServerBaseUrl = "http://10.0.2.2:5000";
#else
// Для Windows:
    public const string ServerBaseUrl = "http://localhost:5000";
#endif
```

Если сервер на другой машине — замените на её IP:
```csharp
public const string ServerBaseUrl = "http://192.168.1.100:5000";
```

### 2.2 Deep link redirect в бэкенде

Бэкенд должен редиректить после OAuth не на `/api/auth/callback` в браузере,
а на deep link приложения. Добавьте в `AuthController.cs` (на бэкенде) в конец метода Callback:

```csharp
// Вместо return Ok(...) — редирект на deep link:
var deepLink = $"yandexmessenger://callback?token={jwt}&username={user.Username}" +
               $"&email={Uri.EscapeDataString(email)}&needs_username={isNew.ToString().ToLower()}";
return Redirect(deepLink);
```

### 2.3 В Яндекс OAuth Console

Добавьте дополнительный Redirect URI:
```
yandexmessenger://callback
```
(рядом с уже существующим `http://localhost:5000/api/auth/callback`)

---

## 🚀 Шаг 3: Запуск

### Открыть проект в Visual Studio 2022

1. File → Open → Project/Solution
2. Выберите `YandexMessengerMAUI.csproj`

### Восстановить пакеты

```bash
dotnet restore
```

### Запуск на Windows

В Visual Studio выберите целевую платформу `Windows Machine` и нажмите ▶.

Или через CLI:
```bash
dotnet build -t:Run -f net8.0-windows10.0.19041.0
```

### Запуск на Android-эмуляторе

1. В Visual Studio: Tools → Android → Android Device Manager
2. Создайте эмулятор (например Pixel 5, API 33)
3. Запустите эмулятор
4. Выберите его в списке устройств и нажмите ▶

Или через CLI:
```bash
dotnet build -t:Run -f net8.0-android
```

### Запуск на физическом Android-устройстве

1. Включите режим разработчика на телефоне
2. Включите USB-отладку
3. Подключите кабелем
4. Устройство появится в Visual Studio — выберите и запустите

---

## 📲 Экраны приложения

```
SplashPage (загрузка, 0.8 сек)
    ↓ есть JWT → проверяем на сервере
    ├─ токен валиден → DialogsPage (главный экран)
    └─ токен истёк / нет → LoginPage

LoginPage
    ↓ «Войти через Яндекс» → открывает браузер
    ↓ после OAuth браузер делает deep link → приложение перехватывает
    ├─ новый пользователь → SetUsernamePage
    └─ уже зарегистрирован → DialogsPage

SetUsernamePage
    ↓ выбрал @username → сохранили → DialogsPage

DialogsPage (главный экран)
    ├─ [список диалогов с аватарами, временем, счётчиком непрочитанных]
    ├─ нажал на диалог → ChatPage
    ├─ нажал ✏️ → NewChatPage
    └─ новые сообщения приходят через SignalR автоматически

ChatPage
    ├─ [пузыри сообщений: мои справа зелёные, чужие слева синие]
    ├─ [поле ввода + кнопка отправки]
    └─ новые сообщения через SignalR добавляются сразу

NewChatPage
    └─ поиск по @username → нашли → открыть ChatPage
```

---

## ❌ Типичные ошибки

### «MAUI workload not installed»
```bash
dotnet workload install maui
```

### Android-эмулятор не видит сервер на localhost
Используйте `10.0.2.2` вместо `localhost` (в AppConstants.cs это уже сделано для Android).

### Deep link не работает
- Убедитесь, что в AndroidManifest.xml прописана схема `yandexmessenger`
- На Windows: проект должен быть запакован (MSIX), чтобы Protocol registration работал
- Альтернатива для тестирования: вставить JWT токен вручную (взять из Swagger `/api/auth/callback`)

### «Connection refused» при подключении SignalR
- Проверьте, что бэкенд запущен: http://localhost:5000
- На Android проверьте AppConstants.cs — должно быть `10.0.2.2:5000`

### «Unauthorized» при запросах
- Токен не установлен или истёк
- Зайдите заново через OAuth

### Ошибка сборки: «SDK 8.0 not found»
Установите .NET 8 SDK: https://dotnet.microsoft.com/download/dotnet/8.0
```bash
dotnet --list-sdks   # должна быть строка 8.0.xxx
```

---

## 🔄 Сборка Release APK (Android)

```bash
dotnet publish -f net8.0-android -c Release
```
APK будет в `bin/Release/net8.0-android/publish/`

## 🔄 Сборка Windows MSIX (для установки)

```bash
dotnet publish -f net8.0-windows10.0.19041.0 -c Release
```

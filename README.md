# PSG-template

Шаблон проекта для Unity с настроенной архитектурой, папочной структурой, сервисами загрузки и логирования. Основан на VContainer и UniTask.

## 📁 Структура проекта

```
Assets/_Project/
├── Art/                    # Отдел художников: 3D-модели, спрайты, анимации, террейны, шейдеры
├── Develop/                # Отдел разработки
│   ├── Configs/            # Все ScriptableObject конфиги
│   └── CompanyName/        # Замените на название вашей компании
│       └── ProjectName/    # Замените на название вашего проекта
│           ├── Editor/     # Editor-скрипты (не попадают в билд)
│           ├── Runtime/    # Production-код
│           ├── Shared/     # Код для клиента и бэкенда (опционально)
│           └── Backend/    # Транспортный слой, без ссылок на Unity (опционально)
├── Tests/                  # Тесты
│   ├── EditMode/           # EditMode тесты (опционально)
│   └── PlayMode/           # PlayMode тесты (опционально)
```

## 🎮 Предопределённая структура сцен

| Индекс | Сцена | Назначение |
|--------|-------|------------|
| 0 | `0.Bootstrap` | Настройка проекта и регистрация зависимостей |
| 1 | `1.Loading` | GDPR, авторизация, логин, загрузка контента |
| 2 | `2.Meta` | Мета-часть: UI, магазин, карта саги, достижения, прокачка героев |
| 3 | `3.Core` | Основная игровая механика (переиспользуемая часть) |
| 4 | `4.Empty` | Очистка ресурсов при переходе между сценами |

## 🚀 Быстрый старт

### Установленные пакеты

- **SceneSwitcher** — быстрое переключение сцен возле кнопки PlayMode.
- **Startup Scene Definer** — запуск PlayMode всегда со сцены `0.Bootstrap` (игнорирует суффикс `Test` в названии). Включён в Project Settings по умолчанию.
- **IngameDebugConsole** — доступна в Bootstrap сцене. **Перед релизом обязательно выключить!**

### Запуск

1. Откройте сцену `0.Bootstrap`.
2. Нажмите Play.
3. Проект автоматически пройдёт Bootstrap → Loading → Meta → Core

## 🧩 Архитектура и DI (VContainer)

### Контейнеры

```
BootstrapScope (LifetimeScope)
├── MetaScope
├── CoreScope
└── LoadingScope
```

- **BootstrapScope** — корневой контейнер, живёт всё время приложения. Регистрирует persistent-сервисы.
- **SceneScope** — создаётся при входе на сцену, уничтожается при выходе. Регистрирует scoped-сервисы.

### Предопределённые сервисы в BootstrapScope

```csharp
builder.Register<LoadingService>(Lifetime.Scoped);
builder.Register<SceneManager>(Lifetime.Singleton);

builder.Register<AudioMixerService>(Lifetime.Singleton).As<IAudioMixerService>().As<ILoadUnit>();
builder.RegisterInstance(_audioMixer);

builder.Register<AudioPlayer>(Lifetime.Singleton).As<IAudioPlayer>().As<ILoadUnit>();
builder.RegisterInstance(_audioLibrary);

builder.RegisterEntryPoint<BootstrapFlow>();
```

### 💡 Доступные сервисы (можно получить в любом месте через DI)

#### `LoadingService`

Сервис для асинхронной загрузки `ILoadUnit` с замером времени, логированием и автоматическим переключением на главный поток.

```csharp
await _loadingService.BeginLoading(myLoadUnit);
await _loadingService.BeginLoadingParallel("Configs and Data", false, unit1, unit2);
```

**Особенности:**
- Замер времени через `Stopwatch` (отключается в продакшене директивой `SAPIRGAME_PROD`)
- Автоматическое переключение на главный поток для Unity API
- Сбор `IDisposable` для последующей очистки
- Поддержка последовательной и параллельной загрузки

#### `SceneManager`

Вспомогательный класс для переключения сцен с очисткой через `4.Empty`.

```csharp
await _sceneManager.LoadScene(RuntimeConstants.Scenes.Core);
```

**Логика:**
- При переходе на `Meta` / `Core` / `Loading` сначала грузит `Empty`, затем целевую сцену.
- Пропускает `Empty` при загрузке `Bootstrap` или `Loading`.

#### `IAudioMixerService` / `IAudioPlayer`

Сервисы для работы с аудиомикшером и воспроизведением звуков. Реализуют `ILoadUnit` — загружаются асинхронно при старте.

#### `AssetService`

Статический сервис для загрузки ресурсов через `Resources`.

```csharp
var config = AssetService.R.Load<MyConfig>("Configs/MyConfig");
```

### 📄 `RuntimeConstants`

Все константы (индексы сцен, настройки frame rate) хранятся здесь:

```csharp
RuntimeConstants.Scenes.Bootstrap   // индекс сцены 0.Bootstrap
RuntimeConstants.Scenes.Loading     // индекс сцены 1.Loading
RuntimeConstants.FrameRate.Default  // 60
```

## 📦 Работа с конфигами (ConfigContainer)

**Все ScriptableObject конфиги хранятся в:** `Assets/_Project/Develop/Configs/`

### Как это работает

1. В `BootstrapFlow` создаётся и загружается `ConfigContainer` — сервис, который содержит ссылки на все конфиги проекта.
2. ConfigContainer регистрируется в VContainer как `ConfigContainer` (через `builder.RegisterComponent` или `builder.RegisterInstance`).
3. Любой сервис в любом скоупе получает доступ ко всем конфигам через DI.

### Пример создания конфига

```csharp
// Файл: Assets/_Project/Develop/Configs/Battle/WaterConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "WaterConfig", menuName = "Game/Battle/WaterConfig")]
public class WaterConfig : ScriptableObject
{
    [SerializeField] private float _waterLevel = 0f;
    [SerializeField] private float _waterSpeedModificator = 0.5f;
    
    public float WaterLevel => _waterLevel;
    public float WaterSpeedModificator => _waterSpeedModificator;
}
```

### Пример структуры ConfigContainer, если договорились использовать json

```csharp
using System;
using Cysharp.Threading.Tasks;
using DataSakura.Runtime.Utilities;
using Newtonsoft.Json;
using UnityEngine;

namespace DataSakura.Runtime
{
    public sealed class ConfigContainer : ILoadUnit
    {
        public BattleConfigContainer Battle;
        
        public UniTask Load()
        {
            var asset = AssetService.R.Load<TextAsset>(RuntimeConstants.Configs.ConfigFileName);
            JsonConvert.PopulateObject(asset.text, this);
            return UniTask.CompletedTask;
        }
    }
    
    [Serializable]
    public class BattleConfigContainer
    {
        public PlaneConfig PlaneConfig;
        public BotPlaneConfig BotPlaneConfig;
        public JoystickConfig JoystickConfig;
        public WaterConfig WaterConfig;
    }

    [Serializable]
    public class PlaneConfig
    {
        public float Responsiveness;
        public BulletConfig Bullet;
    }

    [Serializable]
    public class BotPlaneConfig : PlaneConfig
    {
        public float ShootInterval;
        public float DistanceToShoot;
        public float DefaultSpeedModificator = .5f;
        /// <summary> How sensitively the AI applies the roll controls</summary>
        public float RollSensitivity = .5f;      
        /// <summary> How sensitively the AI applies the pitch controls</summary>
        public float PitchSensitivity = .5f;     
        /// <summary> The amount that the plane can wander by when heading for a target</summary>
        public float LateralWanderDistance = 2;  
        /// <summary> The speed at which the plane will wander laterally</summary>
        public float LateralWanderSpeed = 0.11f; 
        /// <summary> The maximum angle that the AI will attempt to make plane can climb at</summary>
        public float MaxClimbAngle = 45;         
        /// <summary> The maximum angle that the AI will attempt to u</summary>
        public float MaxRollAngle = 70;          
        /// <summary> This increases the effect of the controls based on the plane's speed.</summary>
        public float SpeedEffect = 0.01f;        
    }

    [Serializable]
    public class BulletConfig
    {
        public float Speed;
        public float LifeTime;
    }

    [Serializable]
    public sealed class JoystickConfig
    {
        public float HandleOffsetActivation;
        public float JoystickJitter;
    }

    [Serializable]
    public sealed class WaterConfig
    {
        public float WaterLevel;
        public float WaterSpeedModificator;
    }
}
```

### Регистрация в BootstrapScope

Текстовые json конфиги загружаются через `ILoadUnit` и инжектятся:

```csharp
// ConfigLoader.cs — реализует ILoadUnit
public class ConfigLoader : ILoadUnit
{
    private readonly ConfigContainer _configContainer;
    
    [Inject]
    public ConfigLoader(ConfigContainer configContainer)
    {
        _configContainer = configContainer;
    }
    
    public UniTask Load()
    {
        // Конфиги уже загружены через инспектор или Resources
        return UniTask.CompletedTask;
    }
}
```

В `BootstrapFlow`:
```csharp
public class BootstrapFlow : IStartable
{
    private readonly LoadingService _loadingService;
    private readonly ConfigContainer _configContainer;
    
    public async void Start()
    {
        // ConfigContainer уже имеет ссылки на все конфиги (назначены через инспектор)
        await _loadingService.BeginLoading(_configContainer as ILoadUnit);
    }
}
```

### Использование текстового конфига в любом сервисе

```csharp
public class WaterBuffService : ILoadUnit, IFixedTickable
{
    private readonly ConfigContainer _configContainer;
    private WaterConfig _waterConfig;
    
    public WaterBuffService(ConfigContainer configContainer)
    {
        _configContainer = configContainer;
        _waterConfig = _configContainer.WaterConfig; // доступ через контейнер
    }
    
    void IFixedTickable.FixedTick()
    {
        float waterLevel = _waterConfig.WaterLevel;
        float speedMod = _waterConfig.WaterSpeedModificator;
        // логика...
    }
}
```

## 📝 Система логирования

### Использование

```csharp
using SapirGame.PSG.Runtime.Utilities.Logging;

// Базовое логирование с тегом
Log.Default.D("TAG", "Сообщение");
Log.Default.W("TAG", "Предупреждение");
Log.Default.E("TAG", "Ошибка");

// Логирование загрузки
Log.Loading.D("Загрузка конфигов начата");
Log.Loading.E(exception);
```

### `TagLog`

Класс для категоризированного логирования с префиксами.

```csharp
private static readonly TagLog Log = new TagLog("MY_SYSTEM");
Log.D("Initialized");
Log.D("DETAIL", "Детальное сообщение");
```

### Удаление из продакшена

**Методы `D()` (Debug) полностью удаляются из билда** при определении директивы компиляции `SAPIRGAME_PROD`.

Методы `W()`, `E()`, `ThrowException()` остаются всегда.

## 🔄 `ILoadUnit` и `LoadingService`

### Интерфейсы

| Интерфейс | Назначение |
|-----------|------------|
| `ILoadUnit` | Единица загрузки с методом `UniTask Load()` |
| `IDisposableLoadUnit` | `ILoadUnit` + `IDisposable` |
| `ILoadUnit<T>` | Параметризованная единица загрузки |
| `IDisposableLoadUnit<T>` | Параметризованная + `IDisposable` |

### Пример реализации `ILoadUnit`

```csharp
public class MyService : ILoadUnit
{
    private readonly ConfigContainer _configContainer;
    
    public MyService(ConfigContainer configContainer)
    {
        _configContainer = configContainer;
    }
    
    public async UniTask Load()
    {
        // Инициализация сервиса после загрузки конфигов
        await UniTask.CompletedTask;
    }
}
```

## 🏁 `Flow` — инициализация скоупа

Суффикс `Flow` означает класс инициализации скоупа (старт). Реализует `IStartable` от VContainer.

### Пример: `BootstrapFlow`

```csharp
using SapirGame.PSG.Runtime.Bootstrap.Units;
using SapirGame.PSG.Runtime.Utilities;
using SapirGame.PSG.Runtime.AudioMixerSystem;
using SapirGame.PSG.Runtime.AudioPlayerSystem;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

namespace SapirGame.PSG.Runtime.Bootstrap
{
    public class BootstrapFlow : IStartable
    {
        private readonly LoadingService _loadingService;
        private readonly SceneManager _sceneManager;
        private readonly IAudioMixerService _audioMixerService;
        private readonly IAudioPlayer _audioPlayer;

        public BootstrapFlow(
            LoadingService loadingService,
            SceneManager sceneManager,
            IAudioMixerService audioMixerService,
            IAudioPlayer audioPlayer)
        {
            _loadingService = loadingService;
            _sceneManager = sceneManager;
            _audioMixerService = audioMixerService;
            _audioPlayer = audioPlayer;
        }

        public async void Start()
        {
            // Установка целевого FPS
            Application.targetFrameRate = RuntimeConstants.FrameRate.Default;

            // Последовательная загрузка аудиосервисов
            await _loadingService.BeginLoading(_audioMixerService as ILoadUnit);
            await _loadingService.BeginLoading(_audioPlayer as ILoadUnit);

            // Переход на сцену загрузки (без ожидания)
            _sceneManager.LoadScene(RuntimeConstants.Scenes.Loading).Forget();
        }
    }
}
```

**Что происходит:**
1. Настройка `targetFrameRate = 60`
2. Асинхронная загрузка `AudioMixerService` (через `ILoadUnit.Load()`)
3. Асинхронная загрузка `AudioPlayer`
4. Запуск перехода на сцену `1.Loading`

## 🧪 Пример сервиса: `WaterBuffService`

```csharp
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DataSakura.Runtime.Battle.Airplane;
using DataSakura.Runtime.Utilities;
using VContainer.Unity;

namespace DataSakura.Runtime.Battle
{
    public class WaterBuffService : ILoadUnit, IFixedTickable
    {
        private readonly BattleController _battleController;
        private readonly ConfigContainer _configContainer;
        private WaterConfig _waterConfig;
        private List<UnderwaterPlane> _follow;
        private bool _isInitialized;

        public WaterBuffService(BattleController battleController, ConfigContainer configContainer)
        {
            _battleController = battleController;
            _configContainer = configContainer;
        }

        UniTask ILoadUnit.Load()
        {
            _waterConfig = _configContainer.WaterConfig; // получение конфига из контейнера
            _follow = new List<UnderwaterPlane>(_battleController.Bots.Count + 1) { new(_battleController.Player) };
            
            for (var i = 0; i < _battleController.Bots.Count; i++)
                _follow.Add(new UnderwaterPlane(_battleController.Bots[i]));
            
            _isInitialized = true;
            return UniTask.CompletedTask;
        }

        void IFixedTickable.FixedTick()
        {
            if (!_isInitialized) return;
            
            for (var i = 0; i < _follow.Count; i++)
            {
                UnderwaterPlane plane = _follow[i];
                if (plane.View.IsDead.Value) continue;

                if (plane.View.transform.position.y > _waterConfig.WaterLevel && plane.IsUnderwater)
                {
                    plane.View.ResetMaxSpeed();
                    plane.IsUnderwater = false;
                    continue;
                }
                
                if (plane.View.transform.position.y < _waterConfig.WaterLevel && !plane.IsUnderwater)
                {
                    plane.View.BuffMaxSpeed(_waterConfig.WaterSpeedModificator);
                    plane.IsUnderwater = true;
                }
            }
        }
        
        private sealed class UnderwaterPlane
        {
            public bool IsUnderwater;
            public readonly PlaneView View;
            public UnderwaterPlane(PlaneView view) => View = view;
        }
    }
}
```

**Где регистрируется:** В `CoreScope` (основная игровая сцена).

## 👨‍🎨 Руководство для геймдизайнеров

### 📂 Работа с конфигами

**Все конфиги (ScriptableObject) хранятся в:** `Assets/_Project/Develop/Configs/`

### Создание нового конфига

1. В папке `Configs` создайте подпапку по фиче: `Configs/Battle/`
2. Создайте C# скрипт конфига:
```csharp
// WaterConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "WaterConfig", menuName = "Game/Battle/WaterConfig")]
public class WaterConfig : ScriptableObject
{
    [Header("Water Level")]
    [SerializeField] private float _waterLevel = 0f;
    
    [Header("Speed Modifier")]
    [SerializeField] [Range(0f, 1f)] private float _waterSpeedModificator = 0.5f;
    
    public float WaterLevel => _waterLevel;
    public float WaterSpeedModificator => _waterSpeedModificator;
}
```

### Структура конфигов по фичам

```
Configs/
├── Battle/
│   ├── WaterConfig.asset
│   ├── DamageConfig.asset
│   └── SpawnConfig.asset
├── Meta/
│   ├── ShopConfig.asset
│   └── ProgressionConfig.asset
└── Audio/
    └── AudioMixerConfig.asset
```

# Unity Test Workshop

[English](#english) · [Русский](#русский)

## English

**Quick navigation:** [Project map](#project-map-en) · [Run tests](#run-tests-en) · [EditMode patterns](#editmode-patterns-en) · [PlayMode examples](#playmode-examples-en)

### Project map (EN)

The workshop code is in `Assets/_Project/Develop/SapirGame/PSG`:

- [Wallet.cs](Assets/_Project/Develop/SapirGame/PSG/Runtime/Wallet.cs) — plain C# wallet rules.
- [CoinPickup.cs](Assets/_Project/Develop/SapirGame/PSG/Runtime/CoinPickup.cs) — Unity component that credits a wallet and destroys its GameObject.
- [WalletTests.cs](Assets/_Project/Develop/SapirGame/PSG/Tests/EditMode/WalletTests.cs) — EditMode test attributes and data sources.
- [WalletFixtureTests.cs](Assets/_Project/Develop/SapirGame/PSG/Tests/EditMode/WalletFixtureTests.cs) — parameterized fixtures and setup lifecycle.
- [CoinPickupTests.cs](Assets/_Project/Develop/SapirGame/PSG/Tests/PlayMode/CoinPickupTests.cs) — PlayMode behavior and Unity lifecycle.
- [EditMode assembly](Assets/_Project/Develop/SapirGame/PSG/Tests/EditMode/PSG.Tests.EditMode.asmdef) and [PlayMode assembly](Assets/_Project/Develop/SapirGame/PSG/Tests/PlayMode/PSG.Tests.PlayMode.asmdef) — test assembly references and platform settings.

The EditMode assembly is Editor-only. Both test assemblies reference `PSG.Runtime`. The project uses Unity **6000.2.6f2**.

### Run tests (EN)

1. Open the repository in Unity 6000.2.6f2 and wait for compilation.
2. Open **Window → General → Test Runner**.
3. Run the **EditMode** or **PlayMode** suite. If Test Runner is missing, install or enable Unity Test Framework in Package Manager.

The current NUnit data defines **14 EditMode cases** and **7 PlayMode cases**. Treat those as source-derived counts; the Test Runner shows the actual result for the Editor and package versions in use.

### EditMode patterns (EN)

| Pattern | Example in this project | What it demonstrates |
| --- | --- | --- |
| `[Test]` | Spend the exact balance | One named check with no parameters |
| `[TestCase]` | Spend 3 or overspend 11; add 1 or 5 | Reuse one method with explicit inputs and expected values |
| `[Range]` | Spend 9, 10, and 11 | Generate cases around a boundary; each case gets a fresh wallet from `[SetUp]` |
| `[TestCaseSource]` | Spend 0 or -1 | Keep a readable list of named or complex inputs outside the test method |
| `[TestFixture]` | Start with 10 or 100 coins | Pass parameters to the whole test class; NUnit creates a fixture for each parameter set |

Most methods follow **Arrange → Act → Assert**: prepare the wallet, call the operation, then check its result. In TDD, first write the expected behavior and see the test fail, then implement the behavior and rerun it.

`[SetUp]` and `[TearDown]` run before and after **each test case**. `[OneTimeSetUp]` and `[OneTimeTearDown]` run once for **each fixture configuration**. In `WalletFixtureTests`, the fixture baseline is created once and each case gets its own wallet, so changes do not leak between cases. A parameterized fixture does not create a separate fixture for every test method.

NUnit does not reset `static` fields between cases or fixture configurations. Tests that touch shared static state should reset it explicitly and restore it during cleanup; avoid parallel execution when cases share mutable static state.

### PlayMode examples (EN)

`CoinPickupTests` creates a GameObject in code, adds the component, and initializes it with a wallet. The main coroutine collects twice in one frame, checks that the reward is credited once, yields a frame, then checks that Unity processed the deferred destruction. Other cases cover missing or invalid initialization and a custom reward amount. `[UnityTearDown]` removes any object left behind, including after a failed assertion. No scene or prefab setup is needed.

EditMode/PlayMode describe **where** a test runs. Unit/integration describe **what scope** it checks: the wallet tests check a rule in isolation; the coin tests check the wallet, component, and Unity object lifetime together.

## Русский

**Быстрая навигация:** [Структура проекта](#project-map-ru) · [Запуск тестов](#run-tests-ru) · [Шаблоны EditMode](#editmode-patterns-ru) · [Примеры PlayMode](#playmode-examples-ru)

### Структура проекта (RU)

Код воркшопа находится в `Assets/_Project/Develop/SapirGame/PSG`:

- [Wallet.cs](Assets/_Project/Develop/SapirGame/PSG/Runtime/Wallet.cs) — правила кошелька на обычном C#.
- [CoinPickup.cs](Assets/_Project/Develop/SapirGame/PSG/Runtime/CoinPickup.cs) — Unity-компонент, который начисляет монеты и уничтожает свой GameObject.
- [WalletTests.cs](Assets/_Project/Develop/SapirGame/PSG/Tests/EditMode/WalletTests.cs) — атрибуты EditMode-тестов и источники данных.
- [WalletFixtureTests.cs](Assets/_Project/Develop/SapirGame/PSG/Tests/EditMode/WalletFixtureTests.cs) — параметризованные фикстуры и этапы подготовки.
- [CoinPickupTests.cs](Assets/_Project/Develop/SapirGame/PSG/Tests/PlayMode/CoinPickupTests.cs) — поведение в PlayMode и жизненный цикл Unity.
- [EditMode assembly](Assets/_Project/Develop/SapirGame/PSG/Tests/EditMode/PSG.Tests.EditMode.asmdef) и [PlayMode assembly](Assets/_Project/Develop/SapirGame/PSG/Tests/PlayMode/PSG.Tests.PlayMode.asmdef) — ссылки сборок и настройки платформы.

Сборка EditMode запускается только в Editor. Обе тестовые сборки ссылаются на `PSG.Runtime`. Версия проекта — **Unity 6000.2.6f2**.

### Запуск тестов (RU)

1. Откройте репозиторий в Unity 6000.2.6f2 и дождитесь компиляции.
2. Откройте **Window → General → Test Runner**.
3. Запустите набор **EditMode** или **PlayMode**. Если Test Runner не отображается, установите или включите Unity Test Framework в Package Manager.

В исходниках задано **14 кейсов EditMode** и **7 кейсов PlayMode**. Это количество следует из NUnit-атрибутов и источников данных; фактический результат для установленных версий Unity и пакетов покажет Test Runner.

### Шаблоны EditMode (RU)

| Шаблон | Пример в проекте | Что показывает |
| --- | --- | --- |
| `[Test]` | Списание всего баланса | Один именованный кейс без параметров |
| `[TestCase]` | Списание 3 монет или попытка потратить 11; добавление 1 или 5 | Повторное использование метода с явными входами и ожиданиями |
| `[Range]` | Списание 9, 10 и 11 монет | Генерацию кейсов вокруг границы; для каждого `[SetUp]` создаёт новый кошелёк |
| `[TestCaseSource]` | Попытка потратить 0 или −1 | Отдельный список именованных или сложных наборов данных |
| `[TestFixture]` | Начальный баланс 10 или 100 монет | Передачу параметров всему классу; NUnit создаёт фикстуру для каждого набора параметров |

Большинство методов следует схеме **Arrange → Act → Assert**: подготовить кошелёк, выполнить действие, проверить результат. В TDD сначала описывают ожидаемое поведение и видят падение теста, затем реализуют поведение и запускают тест повторно.

`[SetUp]` и `[TearDown]` выполняются перед и после **каждого кейса**. `[OneTimeSetUp]` и `[OneTimeTearDown]` — один раз для **каждой конфигурации фикстуры**. В `WalletFixtureTests` исходный кошелёк фикстуры создаётся один раз, а для каждого кейса готовится отдельный кошелёк: изменения не перетекают между кейсами. Параметризованная фикстура не создаёт отдельный экземпляр класса для каждого тестового метода.

NUnit не сбрасывает `static`-поля между кейсами и конфигурациями фикстуры. Тестам, которые используют общее статическое состояние, нужно явно сбрасывать его и восстанавливать при очистке. Кейсы с изменяемым общим состоянием не следует запускать параллельно.

### Примеры PlayMode (RU)

`CoinPickupTests` создаёт GameObject кодом, добавляет компонент и связывает его с кошельком. Основная корутина подбирает монету дважды в одном кадре, проверяет однократное начисление, ждёт кадр и затем проверяет, что Unity выполнила отложенное уничтожение объекта. Остальные кейсы проверяют отсутствие или некорректность инициализации и пользовательский размер награды. `[UnityTearDown]` удаляет оставшийся объект, в том числе после упавшей проверки. Сцена и префаб не нужны.

EditMode/PlayMode описывают, **где** запускается тест. Unit/integration описывают, **что и в каком объёме** он проверяет: тесты кошелька изолированно проверяют правило, тесты монеты — совместную работу кошелька, компонента и времени жизни Unity-объекта.

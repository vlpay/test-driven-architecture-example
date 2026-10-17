# Unity Testing Workshop

A Unity sample for learning unit tests, Test-Driven Development (TDD), and EditMode/PlayMode testing through a player wallet and a collectible coin.

Start with a rule: **a player with 10 coins can spend all 10 coins**. Write down the expected result, run a test, find the mistake, and fix it.

## Repository structure

```text
.
├── Starter/
│   └── Assets/                  # Example with an intentional bug
├── Solution/
│   └── Assets/                  # The same example with the bug fixed
├── Exercises/
│   └── WalletExtraTests.cs.txt  # Optional exercise solution
└── README.md
```

Both variants use the same structure inside `Assets`:

```text
Assets/
└── _Project/
    └── Develop/
        └── SapirGame/
            └── PSG/
                ├── Runtime/
                │   ├── PSG.Runtime.asmdef
                │   ├── Wallet.cs
                │   └── CoinPickup.cs
                └── Tests/
                    ├── EditMode/
                    │   ├── PSG.Tests.EditMode.asmdef
                    │   └── WalletTests.cs
                    └── PlayMode/
                        ├── PSG.Tests.PlayMode.asmdef
                        └── CoinPickupTests.cs
```

| Folder | Purpose |
| --- | --- |
| `Runtime` | Code used by the game: the wallet and coin component. |
| `Tests/EditMode` | Tests of wallet rules that do not need gameplay frames. |
| `Tests/PlayMode` | Tests involving the coin component and Unity's game loop. |

The project convention also allows these folders when needed:

- `Editor`: tools that run only in the Unity Editor.
- `Shared`: code shared by the client and backend.
- `Backend`: the client's transport layer, without Unity Engine references.

These optional folders are not included in this sample. External SDKs stay in their own folders outside `_Project`; top-level `Assets/Plugins` and `Assets/Resources` are reserved for external assets in this convention.

Each `.asmdef` defines a group of C# scripts called an assembly. Both test assemblies reference `Runtime`, allowing tests to use the game classes. `Runtime` does not reference the tests. The supplied files already configure the test assemblies.

## Getting started

This repository contains source files to import into a Unity project, rather than a complete project with `Packages` and `ProjectSettings`.

1. Create or open a separate Unity project for the workshop.
2. In Package Manager, install **Test Framework** (`com.unity.test-framework`) if it is missing. Use a version compatible with your Editor.
3. Copy the contents of `Starter/Assets` into your project's `Assets` folder.
4. Wait for compilation and resolve any Console errors.
5. Open **Window > General > Test Runner**.
6. Run `WalletTests` on the **EditMode** tab, then `CoinPickupTests` on the **PlayMode** tab.

**Import only one variant at a time.** Starter and Solution contain the same class and assembly names. To switch to Solution, replace the existing `Wallet.cs` with the Solution version, or apply the fix below.

No scene or prefab setup is required: the PlayMode test creates its own objects.

## Example 1: testing the wallet

`Wallet.TrySpend(amount)` returns `true` and subtracts the amount when enough coins are available. Otherwise, it returns `false` and keeps the balance unchanged.

| Starting balance | Amount | Expected result | Remaining balance |
| --- | --- | --- | --- |
| 10 | 3 | `true` | 7 |
| 10 | 10 | `true` | 0 |
| 10 | 11 | `false` | 10 |
| 0 | 1 | `false` | 0 |

Starter intentionally rejects the second case. The fix changes one comparison:

```csharp
if (amount >= Balance) return false;

if (amount > Balance) return false;
```

Run the test before the fix to see the failure, then run it again after the change. Each test follows three steps: **prepare the wallet, perform the purchase, check the result**. This structure is commonly called Arrange, Act, Assert (AAA).

## Example 2: testing a coin in PlayMode

The coin test creates a `GameObject`, adds `CoinPickup`, and connects it to a wallet with a reward of 5 coins.

It calls `TryCollect()` twice in the same frame and checks that the reward is added only once. Then `yield return null` lets Unity advance to the next frame so the test can check that the coin was destroyed. Cleanup removes any remaining test object.

The wallet tests check one rule in isolation. The coin test checks how the wallet, component, and Unity lifecycle work together. **EditMode and PlayMode describe the execution environment; unit and integration describe the scope of a test.**

## Expected results

| Variant | EditMode | PlayMode |
| --- | --- | --- |
| Starter | 11 pass, 1 intentional failure | 1 pass |
| Solution | 12 pass | 1 pass |

These are the expected baseline results. Confirm them in your Unity Editor.

## Optional exercise

Write a test for this sequence:

1. Create a wallet with 10 coins.
2. Try to spend 11: expect `false` and a balance of 10.
3. Try to spend 10: expect `true` and a balance of 0.

This checks that a rejected purchase does not break the next valid purchase. After trying it yourself, compare your test with `Exercises/WalletExtraTests.cs.txt`. Copy it into your project's `Tests/EditMode` folder and rename it to `WalletExtraTests.cs` to run it. Solution should then have 13 passing EditMode cases.

## Where TDD fits

For new behavior, TDD starts with a test: describe the expected result, see the test fail, implement enough code to make it pass, then simplify the code if needed.

Starter demonstrates a test for an existing bug. Keeping that test after the fix helps catch the same mistake if it returns.

## Scope

The coin test calls `TryCollect()` directly. It does not check collision triggers, input, UI buttons, or prefab setup. Those behaviors need their own checks.

## Documentation

- [Unity Test Framework](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/index.html)
- [Creating test assemblies](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/workflow-create-test-assembly.html)
- [UnityTest and frame-based tests](https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/reference-attribute-unitytest.html)

Use the documentation version that matches the package installed in your project.

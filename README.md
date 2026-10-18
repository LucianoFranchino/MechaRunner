# Mecha Runner

MechaRunner is a 2D endless runner: the player controls a robot who, tired of being a slave in the factory where he works, tries to escape from its security guards.

Made in **Unity 6 (6000.0.59f2)** with C#, URP 2D Renderer and the Input System. Target: **Android, landscape**.

## Gameplay

The robot runs in one direction while obstacles and enemies come towards it. Over time they move faster and spawn more often.

- **Swipe up:** jump. Swipe up again in the air for a double jump.
- **Swipe down:** drop fast.
- **FIRE button:** shoot the arm cannon. Ground enemies take 2 hits.
- **Power-ups:**
  - **Double Coins:** every coin counts x2.
  - **Super Shot:** one shot destroys any enemy.
  - **Double Score:** points count x2.

Coins and the best score are saved between sessions.

## Project structure

| Folder | Contents |
|---|---|
| `Assets/Scenes` | `MainMenu` → `Tutorial` → `Game` (build order) |
| `Assets/Scripts/Managers` | `GameManager` (Playing / Paused / GameOver states), score, coins (`CurrencyService`), audio, difficulty, pools, power-ups |
| `Assets/Scripts/Spawners` | Object pools (referenced by prefab) and weighted spawners |
| `Assets/Scripts/UI` | HUD, pause, Game Over sequence, menu views |
| `Assets/Scripts/Environment` | Camera framing, parallax |
| `Assets/Test` | Play Mode regression tests (`MechaRunner.Tests` assembly) |

Game code lives in the `MechaRunner` assembly (`Assets/Scripts/MechaRunner.asmdef`). Tests are only compiled when the Test Runner needs them.

## Tests

Open **Window → General → Test Runner → PlayMode** and run all. The tests load the real `Game` scene and restore the saved PlayerPrefs when they finish.

## Builds

Builds go to the `Builds/` folder at the project root. That folder is ignored by Git.

# 2D Game Project: Cthulhu Memory Match

## Overview
Unity 2D memory matching game. Match cards → vaporize cards → drop loot → tentacles eat loot → score increase → summon Cthulhu at score 10.

## Project Structure
- `Assets/Scripts/`: Core C# logic.
  - `GameManager.cs`: Main loop. Grid generation, shuffle logic, score tracking, Cthulhu summon event. Singleton.
  - `CardManager.cs`: Card UI logic. Flip animation, vaporize effect, loot drop trigger. Handles click events.
  - `TentacleController.cs`: Inverse Kinematics (IK) for tentacle movement. Random wander or follow mouse.
  - `TentacleEater.cs`: Trigger/physics logic for tentacle mouth. Snatch loot tagged `Loot`. Consume animation + score increment.
  - `CameraShake.cs`: Add trauma for shake effects (vaporize, swallow, summon).
  - `ParallaxManager.cs` / `Restart.cs` / `Extensions.cs`: Utility/visual helpers.

## Key Mechanics

### 1. Grid & Shuffle
`GameManager.cs` spawn cards based on `difficulty`. Card layout uses `cellPrefab` and `cardPrefab`. Start routine animate pop-in, drop fake loot, hide, then slide-shuffle.

### 2. Match Logic
`GameManager.CheckGuess(GameObject)` track 2 flipped cards.
- Match = `CardManager.Vaporize()`. Drop real loot.
- No Match = `CardManager.Reset()`. Flip back.

### 3. Tentacle System
- **Controller**: IK target move towards random point or mouse. Constrained by `maxReach`.
- **Eater**: Detect `Loot` trigger. Grab food (disable physics/drag). Pull to mouth -> Swallow (shrink animation) -> Destroy -> Score++.

### 4. Game End
Score reach 10 → `GameManager.SummonCthulhu()`. Trigger `OnSummonCthulhu` event. Add high camera trauma.

## Important Notes
- UI/World Space mix. Loot drops use world space (`GetWorldPosForLoot`), Cards use UI/Screen Space.
- Events: `OnGameStarted` enable tentacle hunt. `OnSummonCthulhu` trigger end state.
- Object pooling needed. Loot/cells use `Instantiate`/`Destroy`.

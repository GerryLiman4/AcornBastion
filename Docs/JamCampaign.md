# Five-level jam campaign

The campaign uses `Assets/Resources/Campaign/JamCampaign.json` as the authority for tower costs and combat values, enemy stats, ore rewards, spell settings, unlocks, and all 15 encounters. Towers receive this configuration when placed; enemies receive it before their first frame. Prefab values do not override campaign balance.

Start from MainMenu to begin a fresh campaign. Playing Level.unity directly also starts Level 1. Retry restarts the current level; Next Level reloads the same battlefield with a fresh budget and full base health. Returning to the menu and pressing Start begins at Level 1 again. Progress is session-only.

## Controls

- Preparation: select an unlocked tower card, then click an empty highlighted tile. Right click or Escape cancels selection.
- Mine an ore node with two clicks. Blue ore also grants mana. Buy Miners before mining to receive their bonuses.
- Start begins the next wave and discards remaining ore. Building and mining are unavailable during waves.
- During waves, press **1** or select **Damage Trap**, then click a road tile. Press **2** or select **Area Burst**, then click a point inside the battlefield. Burst unlocks in Level 3.
- The targeting preview is cyan when valid and red when placement, mana, cooldown, or active-trap limits prevent casting. Right click, Escape, or pressing the selected skill again cancels without spending mana.
- Mana is collected automatically from blue ore and marked Grunt combat kills. Base arrivals and cleanup give no rewards.

## Campaign

| Level | Starting gold | New content | Enemy totals by round | Mining nodes by preparation |
|---|---:|---|---|---|
| First Defense | 120 | Miner, Archer, Grunt, Damage Trap | 6 / 9 / 12 | 4 / 3 / 3 |
| Heavy Footsteps | 150 | Knight, Brute | 9 / 13 / 17 | 4 / 4 / 4 |
| Dark Ritual | 175 | Mage, Elder, Area Burst | 12 / 16 / 20 | 5 / 4 / 4 |
| Crossfire | 200 | Gunner | 15 / 21 / 27 | 5 / 5 / 4 |
| Last Stand | 225 | Full-roster finale | 18 / 25 / 33 | 6 / 5 / 5 |

Each level has 20 base health. Gold, mana, placed towers, and base health carry between rounds. Preparation has no timer. Round clears award 25 gold after the first two rounds. New levels reset those resources. There are no tower upgrades, selling, or permanent stat bonuses.

The new HUD extends the existing Canvas at runtime. Existing cards, map, base, and Start button remain in use; no extra scene setup is required. The existing Farmer prefab and UnitId remain unchanged internally and are presented as Miner.

## Repeatable validation

- **Tools > FUR > Validate Campaign Definitions** checks all 15 authored encounters over 100 seeds each, exact marked quotas, first-half marks, opening Grunts, Elder escorts, required prefabs, opening budgets, and total mana income. It writes `Temp/CampaignValidation/Definitions.txt`.
- In Play Mode, `CampaignPlaytest.Begin("mixed")`, `CampaignPlaytest.Begin("archer")`, or `CampaignPlaytest.Begin("max-miners")` runs an accelerated campaign using normal purchases, finite mining, actual tower combat, and normally funded spells. The Archer strategy buys no Miners. Results go to `Temp/CampaignValidation/Playtest-<strategy>.txt`. These scripted strategies are smoke tests, not a substitute for novice playtesting.
- `CampaignValidation.BuildWindows()` produces a development build at `Temp/CampaignValidation/Player/FUR.exe` and records its result in `Build.txt`.

Adjust enemy health and spawn intervals first when tuning difficulty. Preserve the readable economy until real playtests establish a reason to change it. The 15–20 minute campaign length and first-attempt novice success remain playtest targets.

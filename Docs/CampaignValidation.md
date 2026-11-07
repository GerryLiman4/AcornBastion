# Campaign validation — 3 November 2026

Validated with the project's Unity **6000.0.75f1** Editor and a Windows x64 development player.

| Check | Result |
|---|---|
| Unity C# compilation | Passed; no compiler errors |
| Authored encounter validation | 1,500 seeded scenarios passed across all 15 rounds |
| Live opening and economy checks | 28 checks passed |
| Targeting and death-resolution regression checks | 7 checks passed |
| Live burst, rewards, leak, cleanup, and trap-boundary checks | 15 checks passed |
| Mixed towers, one Miner per level | All 5 levels cleared; final base health 11/20 |
| Archers, no Miners | All 5 levels cleared; final base health 17/20 |
| Maximum Miners with mixed towers | All 5 levels cleared; final base health 20/20 |
| Windows x64 development build | Succeeded; 0 errors, 0 warnings |
| Standalone initialization | Player launched and initialized graphics, input, and runtime assemblies without startup exceptions |

The three campaign runs exercised **45 actual rounds and 759 authored enemies**. The scripted player used ordinary purchases, finite mining nodes, tower attacks, and earned mana. It received no extra gold, health, or mana. Runs used accelerated simulation, so these are gameplay integration checks rather than measurements of normal-speed difficulty or playtime.

The 50 targeted live checks used controlled test state independently of those campaign runs. They covered the exact tutorial opening, locked and unaffordable towers, Miner limits, two-click mining, repeated mining rejection, preparation/wave restrictions, mana cap and overspending, invalid casts, cooldowns, cancellation, burst damage and marked rewards, leaks, cleanup, duplicate death resolution, trap boundaries, and trap re-entry.

An initial mixed run exposed an existing alternate-target branch that could select the player's base. The branch now validates faction and retains a valid enemy target. Isolated regression checks and the subsequent complete campaign runs passed. An initial build attempt was canceled because Unity displayed a scene-save dialog; a temporary scene copy was compared with the saved scene and found identical. The scene was saved and the subsequent build passed. Build validation now refuses dirty scenes before invoking the build pipeline.

Detailed reports and local build:

- `Temp/CampaignValidation/Definitions.txt`
- `Temp/CampaignValidation/RuntimeRules.txt`
- `Temp/CampaignValidation/Playtest-mixed.txt`
- `Temp/CampaignValidation/Playtest-archer.txt`
- `Temp/CampaignValidation/Playtest-max-miners.txt`
- `Temp/CampaignValidation/Build.txt`
- `Temp/CampaignValidation/Player/FUR.exe`
- `Temp/CampaignValidation/PlayerSmoke.log`

No Inspector setup remains. The initial Editor scene and background-play setting are restored after validation. Human novice testing, normal-speed campaign duration, and final balance tuning remain unverified; the 15–20 minute target is not established by accelerated runs.

# Audio routing and gameplay sounds

`Assets/Game/Audio/GameAudio.mixer` contains BGM, SFX, and Ambience buses under Master. Music in Level and WeatherPreview is routed to BGM.

`Assets/Resources/Sound/SoundEffectLibrary.asset` assigns the SFX bus, the existing clip pairs, and playback volumes:

| Action | Clips | Volume |
|---|---|---:|
| Gunner firing | machinegun1, machinegun2 | 0.20 |
| Knight slash | slash1, slash2 | 0.55 |
| Each accepted mineral click | weed pulled1, Weed pulled2 | 0.55 |

Both variants are used without repeating the previous selection. Knight audio is triggered by the same animation event that creates the slash. Mining audio plays on both clicks, including collection, and finishes independently of the removed mineral.

The machine-gun recordings contain sustained firing. Each audible Gunner reserves one manager-owned voice, restarting a recording only when the previous one finishes while shots continue. Playback stops after a shot gap, during reload, on wave completion, or when the tower is disabled. Build previews do not play attack sounds.

AudioManager starts independently before the first scene and persists across levels. GameManager contains no sound playback, setup, or cleanup logic. AudioManager loads the library once and owns three fixed pools of five voices: machine-gun fire, Knight slash, and mineral click. Both clip variants count toward the same five-instance category limit. Excess requests are ignored without queuing, interrupting current sounds, or affecting attacks and mining. A slot becomes available when its sound ends; Gunner slots also release when firing stops or their owner is disabled or destroyed. Different categories can play simultaneously, with at most 15 SFX voices total. All voices output to SFX. AudioManager stops effects automatically when the active scene changes. No Inspector wiring is required on individual towers or minerals.

Adjust the category volumes in SoundEffectLibrary or use the SFX mixer fader for all effects together. BGM and Ambience remain separate.

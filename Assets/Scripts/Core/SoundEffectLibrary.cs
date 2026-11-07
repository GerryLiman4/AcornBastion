using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(fileName = "SoundEffectLibrary", menuName = "Audio/Sound Effect Library")]
public sealed class SoundEffectLibrary : ScriptableObject
{
    public AudioMixerGroup outputGroup;
    public AudioClip[] machineGunClips;
    public AudioClip[] knightSlashClips;
    public AudioClip[] mineralClickClips;
    public AudioClip[] selectCardClips;
    public AudioClip[] archerShotClips;
    [Range(0f, 1f)] public float machineGunVolume = 0.2f;
    [Range(0f, 1f)] public float knightSlashVolume = 0.55f;
    [Range(0f, 1f)] public float mineralClickVolume = 0.55f;
    [Range(0f, 1f)] public float selectCardVolume = 0.55f;
    [Range(0f, 1f)] public float archerShotVolume = 0.55f;
    [Range(0f, 1f)] public float buttonVolume = 1f;
}

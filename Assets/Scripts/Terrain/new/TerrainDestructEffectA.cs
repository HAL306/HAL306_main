using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地形破壊時のエフェクト・サウンドを扱う
/// </summary>
[RequireComponent(typeof(TerrainContextA))]
public class TerrainDestructEffectA : MonoBehaviour
{
    // サウンドタイプ
    private enum SoundEffectType
    {
        SMALL_CRACK,
        BIG_CRACK,
    };

    private TerrainContextA _terrainContext;

    private static DestructEffectManager _destructEffectManager;

    private static AudioSource _sfxSource;
    private static readonly Dictionary<SoundEffectType, float> _lastPlayTime = new Dictionary<SoundEffectType, float>();


    public void OnDestruct(List<Vector2[]> destructPaths, float destructArea)
    {
        if (destructPaths == null || destructPaths.Count == 0)
            return;

        // エフェクトの生成
        EmitDestructEffect(destructPaths, destructArea);

        // サウンドの再生
        PlayDestructSound(destructArea);
    }


    private void Awake()
    {
        if (_terrainContext == null)
            _terrainContext = GetComponent<TerrainContextA>();
    }

    private void EmitDestructEffect(List<Vector2[]> destructPaths, float destructArea)
    {
        TerrainParameterA parameter = _terrainContext.TerrainParameter;
        if (parameter == null)
            return;

        if (_destructEffectManager == null)
        {
            _destructEffectManager = FindAnyObjectByType<DestructEffectManager>();
            if (_destructEffectManager == null)
                return;
        }
        _destructEffectManager.Emit(destructPaths, destructArea, parameter, transform);
    }

    private void PlayDestructSound(float destructArea)
    {
        TerrainSettingsA settings = _terrainContext.TerrainSettings;
        TerrainParameterA parameter = _terrainContext.TerrainParameter;
        if (!parameter.IsSoundEnabled)
            return;

        SoundEffectType soundType = destructArea >= settings.BigCrackAreaThreshold ?
            SoundEffectType.BIG_CRACK : SoundEffectType.SMALL_CRACK;

        if (settings.SoundInterval > 0.0f)
        {
            if (_lastPlayTime.TryGetValue(soundType , out float last) &&
                Time.time - last < settings.SoundInterval)
                return;
            _lastPlayTime[soundType] = Time.time;
        }

        AudioClip[] clipPool = soundType == SoundEffectType.BIG_CRACK ?
            settings.BigCrackSounds : settings.SmallCrackSounds;
        if (clipPool == null || clipPool.Length == 0)
            return;

        AudioClip clip = clipPool[UnityEngine.Random.Range(0, clipPool.Length)];
        if (clip == null)
            return;

        if (_sfxSource == null)
        {
            GameObject go = new GameObject("TerrainSFX");
            _sfxSource = go.AddComponent<AudioSource>();
        }

        _sfxSource.pitch = UnityEngine.Random.Range(settings.PitchMin, settings.PitchMax);
        _sfxSource.PlayOneShot(clip, UnityEngine.Random.Range(settings.VolumeMin, settings.VolumeMax));
    }
}

using System;
using UnityEngine;
using UnityEngine.AddressableAssets;

[Serializable]
public class AudioData
{
    [Header("Audio Settings")]
    [SerializeField] private EAudioType _audioType;
    [SerializeField] private AudioClip _audioClip;
    [SerializeField] private AssetReferenceT<AudioClip> _audioClipReference;

    public EAudioType AudioType => _audioType;
    public AudioClip AudioClip => _audioClip;
    public AssetReferenceT<AudioClip> AudioClipReference => _audioClipReference;
}

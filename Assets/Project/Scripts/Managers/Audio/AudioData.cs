using System;
using UnityEngine;

/// <summary>
/// 사운드 종류와 AudioClip을 연결하는 데이터입니다.
/// </summary>
[Serializable]
public class AudioData
{
    [Header("Audio Settings")]
    [SerializeField] private EAudioType _audioType;
    [SerializeField] private AudioClip _audioClip;

    public EAudioType AudioType => _audioType;
    public AudioClip AudioClip => _audioClip;
}


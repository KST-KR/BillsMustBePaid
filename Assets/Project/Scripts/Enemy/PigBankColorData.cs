using System;
using UnityEngine;

/// <summary>
/// 돼지 저금통 종류와 색깔을 매칭하는 데이터
/// </summary>
[Serializable]
public class PigBankColorData 
{
    [Header("PigBank Settings")]
    [SerializeField] private Material _pigBodyMaterial;
    [SerializeField] private Material _pigEyeMaterial;

    public Material PigBodyMaterial => _pigBodyMaterial;
    public Material PigEyeMaterial => _pigEyeMaterial;
}

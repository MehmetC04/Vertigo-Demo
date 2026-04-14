using UnityEngine;


public enum AttachmentCategory
{
    Sight,
    Mag,
    Barrel,
    Tactical,
    Stock
}


public enum StatType
{
    Power,
    Damage,
    Accuracy,
    Range,
    FireRate,
    Speed,
    ClipSize,
    ReloadSpeed
}


[System.Serializable]
public struct StatModifier
{
    public StatType statType;
    public float value; 
}
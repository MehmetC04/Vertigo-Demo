using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New_AttachmentData", menuName = "VertigoDemo/Attachment Data")]
public class AttachmentData : ScriptableObject
{
    [Header("UI Information")]
    public string attachmentName; 
    [TextArea]
    public string description;    
    public Sprite attachmentIcon;
    [Range(1, 5)]
    public int attachmentLvl;

    [Header("Category")]
    public AttachmentCategory category;

    [Header("3D Model References")]
    [Tooltip("Hiyerarþide aktif edilecek objelerin tam adlarý (Örn: sk_primary_dash_att_02_sight_2_LOD0)")]
 
    public List<string> targetMeshNames;

    [Header("Stat Modifications")]
    [Tooltip("Bu eklentinin silaha saðladýðý stat artýþlarý veya azalýþlarý")]
    public List<StatModifier> modifiers;
}
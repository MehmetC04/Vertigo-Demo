using System.Collections.Generic;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    public static WeaponManager Instance { get; private set; }

    public Transform weaponRoot;

    private Dictionary<string, GameObject> allMeshes = new Dictionary<string, GameObject>();
    private Dictionary<AttachmentCategory, List<string>> activeMeshes = new Dictionary<AttachmentCategory, List<string>>();
    private Dictionary<AttachmentCategory, AttachmentData> equippedData = new Dictionary<AttachmentCategory, AttachmentData>();
    private AttachmentData currentPreviewData;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeMeshes();
    }

    private void InitializeMeshes()
    {
        Transform[] children = weaponRoot.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child != weaponRoot)
            {
                allMeshes[child.name] = child.gameObject;
            }
        }
    }

    public void EquipWithoutPreview(AttachmentData data)
    {
        if (data == null) return;
        equippedData[data.category] = data;
        ApplyVisuals(data);
    }

    public void PreviewAttachment(AttachmentData data)
    {
        if (data == null) return;
        currentPreviewData = data;
        ApplyVisuals(data);
    }

    public void EquipAttachment()
    {
        if (currentPreviewData != null)
        {
            equippedData[currentPreviewData.category] = currentPreviewData;
            currentPreviewData = null;
        }
    }

    public void CancelPreview(AttachmentCategory category)
    {
        if (equippedData.TryGetValue(category, out AttachmentData eqData))
        {
            ApplyVisuals(eqData);
        }
        else
        {
            ClearCategoryVisuals(category);
        }
        currentPreviewData = null;
    }

    public bool IsEquipped(AttachmentData data)
    {
        if (data == null) return false;
        if (equippedData.TryGetValue(data.category, out AttachmentData currentEqData))
        {
            return currentEqData == data;
        }
        return false;
    }

    private void ApplyVisuals(AttachmentData data)
    {
        ClearCategoryVisuals(data.category);

        List<string> newlyActivated = new List<string>();
        foreach (string name in data.targetMeshNames)
        {
            if (allMeshes.TryGetValue(name, out GameObject obj))
            {
                obj.SetActive(true);
                newlyActivated.Add(name);
            }
        }
        activeMeshes[data.category] = newlyActivated;
    }

    private void ClearCategoryVisuals(AttachmentCategory category)
    {
        if (activeMeshes.ContainsKey(category))
        {
            foreach (string name in activeMeshes[category])
            {
                if (allMeshes.TryGetValue(name, out GameObject obj))
                {
                    obj.SetActive(false);
                }
            }
            activeMeshes[category].Clear();
        }
    }

    public void UpdateCameraFocus(AttachmentData data)
    {
        if (CameraController.Instance == null || data == null) return;

        Bounds bounds = CalculateBounds(data.targetMeshNames);
        if (bounds.size != Vector3.zero)
        {
            CameraController.Instance.FocusOnBounds(bounds);
        }
    }

    private Bounds CalculateBounds(List<string> meshNames)
    {
        Bounds bounds = new Bounds(weaponRoot.position, Vector3.zero);
        bool hasBounds = false;

        foreach (string name in meshNames)
        {
            if (allMeshes.TryGetValue(name, out GameObject obj))
            {
                Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers)
                {
                    if (!hasBounds)
                    {
                        bounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }
            }
        }
        return bounds;
    }
    public Dictionary<AttachmentCategory, AttachmentData> GetEquippedData()
    {
        return equippedData;
    }
}
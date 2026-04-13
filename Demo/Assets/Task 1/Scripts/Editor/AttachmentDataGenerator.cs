using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public class AttachmentDataGenerator : EditorWindow
{
    [MenuItem("Vertigo Demo/Generate Attachments From CSV")]
    public static void GenerateAttachments()
    {
        string filePath = EditorUtility.OpenFilePanel("Select AttachmentDatabase.csv", Application.dataPath, "csv");

        if (string.IsNullOrEmpty(filePath))
        {
            Debug.Log("Ýþlem iptal edildi, dosya seçilmedi.");
            return;
        }

        string saveFolder = "Assets/Data/Attachments";
        if (!AssetDatabase.IsValidFolder("Assets/Data")) AssetDatabase.CreateFolder("Assets", "Data");
        if (!AssetDatabase.IsValidFolder(saveFolder)) AssetDatabase.CreateFolder("Assets/Data", "Attachments");

        string[] lines = File.ReadAllLines(filePath);
        int generatedCount = 0;

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] columns = line.Split(',');

            
            AttachmentData newData = ScriptableObject.CreateInstance<AttachmentData>();

            int id = int.Parse(columns[0].Trim());
            string idFormatted = id.ToString("D2");

            newData.attachmentName = columns[1].Trim();
            newData.category = (AttachmentCategory)System.Enum.Parse(typeof(AttachmentCategory), columns[2].Trim());

            string[] meshNames = columns[4].Split('|');
            newData.targetMeshNames = new List<string>(meshNames);

            newData.modifiers = new List<StatModifier>();

            if (columns.Length > 6 && !string.IsNullOrWhiteSpace(columns[5]))
            {
                StatModifier mod1 = new StatModifier();
                mod1.statType = (StatType)System.Enum.Parse(typeof(StatType), columns[5].Trim());
                mod1.value = float.Parse(columns[6].Trim());
                newData.modifiers.Add(mod1);
            }

            if (columns.Length > 8 && !string.IsNullOrWhiteSpace(columns[7]))
            {
                StatModifier mod2 = new StatModifier();
                mod2.statType = (StatType)System.Enum.Parse(typeof(StatType), columns[7].Trim());
                mod2.value = float.Parse(columns[8].Trim());
                newData.modifiers.Add(mod2);
            }

            string iconName = columns[3].Trim();
            string[] foundAssets = AssetDatabase.FindAssets(iconName + " t:Sprite");

            if (foundAssets.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(foundAssets[0]);
                newData.attachmentIcon = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            }
            else
            {
                Debug.LogWarning($"Ýkon bulunamadý: {iconName}. P.");
            }

           
            string fileName = $"{idFormatted}_{newData.category}_{newData.attachmentName.Replace(" ", "")}.asset";
            string fullSavePath = saveFolder + "/" + fileName;

            AssetDatabase.CreateAsset(newData, fullSavePath);
            generatedCount++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"<color=green>Baþarýlý!</color> Toplam {generatedCount} adet Attachment Data oluþturuldu.");
    }
}
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using BasicPuzzle.Data;
using BasicPuzzle.Core;

namespace BasicPuzzle.Editor
{
    public class LevelAssetGenerator
    {
        [MenuItem("Tools/BasicPuzzle/Generate 100 Level Assets")]
        public static void GenerateLevels()
        {
            // levels.txt dosyasının yolu
            string txtPath = "Assets/Resources/levels.txt";
            if (!File.Exists(txtPath))
            {
                Debug.LogError($"[LevelAssetGenerator] levels.txt bulunamadı! Lütfen şu yolda olduğundan emin olun: {txtPath}");
                return;
            }

            // Hedef klasör yolu
            string targetFolder = "Assets/Levels";
            if (!AssetDatabase.IsValidFolder(targetFolder))
            {
                AssetDatabase.CreateFolder("Assets", "Levels");
            }

            string[] lines = File.ReadAllLines(txtPath);
            List<LevelData> createdLevels = new List<LevelData>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                string[] parts = trimmed.Split('|');
                if (parts.Length < 5) continue;

                try
                {
                    int levelNo = int.Parse(parts[0].Trim());
                    string sentence = parts[1].Trim();
                    float showDuration = float.Parse(parts[2].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    int shuffleCount = int.Parse(parts[3].Trim());
                    float swapDuration = float.Parse(parts[4].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    
                    float arcHeight = 150f;
                    if (parts.Length >= 6 && !string.IsNullOrEmpty(parts[5].Trim()))
                    {
                        arcHeight = float.Parse(parts[5].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    }

                    int trapCount = 0;
                    if (parts.Length >= 7 && !string.IsNullOrEmpty(parts[6].Trim()))
                    {
                        trapCount = int.Parse(parts[6].Trim());
                    }

                    bool randomize = false;
                    if (parts.Length >= 8 && !string.IsNullOrEmpty(parts[7].Trim()))
                    {
                        randomize = parts[7].Trim() == "1";
                    }

                    EnvelopeLayoutType layoutType = EnvelopeLayoutType.Grid;
                    if (parts.Length >= 9 && !string.IsNullOrEmpty(parts[8].Trim()))
                    {
                        string layoutStr = parts[8].Trim();
                        if (System.Enum.TryParse(layoutStr, true, out EnvelopeLayoutType parsedLayout))
                        {
                            layoutType = parsedLayout;
                        }
                    }

                    string assetPath = $"{targetFolder}/Level_{levelNo}.asset";
                    
                    // Eğer dosya zaten varsa yükle ve güncelle, yoksa yeni oluştur
                    LevelData levelData = AssetDatabase.LoadAssetAtPath<LevelData>(assetPath);

                    bool isNew = false;
                    if (levelData == null)
                    {
                        levelData = ScriptableObject.CreateInstance<LevelData>();
                        isNew = true;
                    }

                    // Değerleri ata
                    levelData.Initialize(sentence, showDuration, shuffleCount, swapDuration, arcHeight, trapCount, randomize, layoutType);

                    if (isNew)
                    {
                        AssetDatabase.CreateAsset(levelData, assetPath);
                    }
                    else
                    {
                        EditorUtility.SetDirty(levelData);
                    }

                    createdLevels.Add(levelData);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LevelAssetGenerator] Hata '{line}' ayrıştırılırken: {e.Message}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[LevelAssetGenerator] {createdLevels.Count} adet LevelData asset dosyası '{targetFolder}' klasöründe başarıyla oluşturuldu!");

            // Sahnedeki GameManager'a otomatik ata
            GameManager gameManager = Object.FindObjectOfType<GameManager>();
            if (gameManager != null)
            {
                // Seviye numaralarına göre sırala
                createdLevels.Sort((a, b) => {
                    int numA = ExtractNumber(a.name);
                    int numB = ExtractNumber(b.name);
                    return numA.CompareTo(numB);
                });

                // Undo desteği ve kalıcılık için SerializedObject kullanıyoruz
                SerializedObject so = new SerializedObject(gameManager);
                SerializedProperty levelsProp = so.FindProperty("levels");
                if (levelsProp != null)
                {
                    levelsProp.ClearArray();
                    levelsProp.arraySize = createdLevels.Count;
                    for (int i = 0; i < createdLevels.Count; i++)
                    {
                        levelsProp.GetArrayElementAtIndex(i).objectReferenceValue = createdLevels[i];
                    }
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(gameManager);
                    Debug.Log("[LevelAssetGenerator] Sahnede aktif olan GameManager nesnesinin 'levels' dizisi başarıyla dolduruldu!");
                }
            }
            else
            {
                Debug.LogWarning("[LevelAssetGenerator] Sahnede GameManager bulunamadı. Lütfen GameManager üzerindeki levels dizisine oluşturulan assetleri elle sürükleyin.");
            }
        }

        private static int ExtractNumber(string name)
        {
            string numberString = "";
            foreach (char c in name)
            {
                if (char.IsDigit(c))
                {
                    numberString += c;
                }
            }
            int result = 0;
            int.TryParse(numberString, out result);
            return result;
        }
    }
}

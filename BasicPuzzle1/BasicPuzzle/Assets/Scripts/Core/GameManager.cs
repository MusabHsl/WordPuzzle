using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BasicPuzzle.Data;
using BasicPuzzle.Gameplay;

namespace BasicPuzzle.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private ObjectPooler objectPooler;
        [SerializeField] private ShuffleController shuffleController;

        [Header("Level Configuration")]
        [Tooltip("Oynanacak seviyelerin listesi")]
        [SerializeField] private LevelData[] levels;
        
        [Tooltip("Başlangıç Seviye İndeksi (Sıfır tabanlı)")]
        [SerializeField] private int currentLevelIndex = 0;

        [Header("Grid Layout Settings")]
        [Tooltip("Zarfların Hücre Boyutu")]
        [SerializeField] private Vector2 cellSize = new Vector2(325f, 200f);
        
        [Tooltip("Zarflar Arasındaki Boşluk")]
        [SerializeField] private Vector2 cellSpacing = new Vector2(40f, 40f);
        
        [Tooltip("Satır Başına Maksimum Zarf Sayısı")]
        [SerializeField] private int maxColumns = 3;

        // Oyun Durum Takibi
        private GameState currentState = GameState.Loading;
        private LevelData currentLevelData;
        private List<Envelope> activeEnvelopes = new List<Envelope>();
        private int currentTargetIndex = 0;
        private string[] levelWords;

        // Yetenek (Skill) Durum Değişkenleri
        private bool isSlowMotionActive = false;
        private float slowMotionMultiplier = 2.0f;
        private bool isHintUsedInCurrentLevel = false;
        private bool isSlowMoUsedInCurrentLevel = false;
        private bool isShieldUsedInCurrentLevel = false;
        private bool preserveSlowMotionOnRestart = false;

        // Event'ler (UI ve diğer sınıfların dinlemesi için)
        public event Action<GameState> OnStateChanged;
        public event Action<LevelData> OnLevelLoaded;
        public event Action<int, string> OnWordSolved; // (çözülen kelime indeksi, çözülen kelime)
        public event Action OnLevelWin;
        public event Action OnLevelFail;
        public event Action OnSkillsUpdated;

        // Getters
        public GameState CurrentState => currentState;
        public int CurrentLevelIndex => currentLevelIndex;
        public int CurrentTargetIndex => currentTargetIndex;
        public string[] LevelWords => levelWords;
        public int TotalLevelCount => levels != null ? levels.Length : 0;
        public bool IsSlowMotionActive => isSlowMotionActive;
        public float SlowMotionMultiplier => slowMotionMultiplier;
        public bool IsHintUsedInCurrentLevel => isHintUsedInCurrentLevel;
        public bool IsSlowMoUsedInCurrentLevel => isSlowMoUsedInCurrentLevel;
        public bool IsShieldUsedInCurrentLevel => isShieldUsedInCurrentLevel;

        private void Awake()
        {
            // Mobil cihazlarda 60 FPS akıcılık sağlamak için hedef kare hızını ayarla
            Application.targetFrameRate = 60;

            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            // Eğer inspector üzerinden el ile veya otomatik araçla seviye atanmamışsa metin dosyasından yükle
            if (levels == null || levels.Length == 0)
            {
                LoadLevelsFromTextFile();
            }
        }

        /// <summary>
        /// Resources klasöründen 'levels.txt' dosyasını yükler ve seviye veritabanını oluşturur.
        /// </summary>
        private void LoadLevelsFromTextFile()
        {
            TextAsset textAsset = Resources.Load<TextAsset>("levels");
            if (textAsset == null)
            {
                Debug.Log("[GameManager] 'levels.txt' Resources klasöründe bulunamadı, Inspector üzerindeki seviyeler kullanılacak.");
                return;
            }

            List<LevelData> dynamicLevels = new List<LevelData>();
            string[] lines = textAsset.text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                string trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                    continue;

                // Format: SeviyeNo | Cümle | BeklemeSüresi | KarıştırmaSayısı | Hız (SwapDuration) | [Opsiyonel] ArcHeight | [Opsiyonel] TrapCount | [Opsiyonel] RandomizeInitialPositions
                string[] parts = trimmed.Split('|');
                if (parts.Length < 5)
                {
                    Debug.LogWarning($"[GameManager] Hatalı seviye satır formatı: {line}");
                    continue;
                }

                try
                {
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
                        if (Enum.TryParse(layoutStr, true, out EnvelopeLayoutType parsedLayout))
                        {
                            layoutType = parsedLayout;
                        }
                    }

                    LevelData levelData = ScriptableObject.CreateInstance<LevelData>();
                    levelData.Initialize(sentence, showDuration, shuffleCount, swapDuration, arcHeight, trapCount, randomize, layoutType);
                    dynamicLevels.Add(levelData);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[GameManager] Seviye satırı ayrıştırılırken hata: '{line}'. Hata mesajı: {e.Message}");
                }
            }

            if (dynamicLevels.Count > 0)
            {
                levels = dynamicLevels.ToArray();
                Debug.Log($"[GameManager] {levels.Length} adet seviye 'levels.txt' dosyasından başarıyla yüklendi!");
            }
        }

        private void Start()
        {
            // ShuffleController event abonesi
            if (shuffleController != null)
            {
                shuffleController.OnShuffleComplete += OnShuffleFinished;
            }

            StartLevel(currentLevelIndex);
        }

        private void OnDestroy()
        {
            if (shuffleController != null)
            {
                shuffleController.OnShuffleComplete -= OnShuffleFinished;
            }
        }

        /// <summary>
        /// Belirtilen indeksli seviyeyi yükler.
        /// </summary>
        public void StartLevel(int levelIndex)
        {
            if (levels == null || levels.Length == 0)
            {
                Debug.LogError("[GameManager] Seviye listesi boş!");
                return;
            }

            // Seviye dizinini sınırlandır (loop yapabiliriz veya son seviyede kalabilir)
            currentLevelIndex = levelIndex % levels.Length;
            currentLevelData = levels[currentLevelIndex];

            // Yetenek durumlarını sıfırla veya koru
            if (!preserveSlowMotionOnRestart)
            {
                isSlowMotionActive = false;
                isSlowMoUsedInCurrentLevel = false;
            }
            else
            {
                isSlowMotionActive = true;
                isSlowMoUsedInCurrentLevel = true;
                preserveSlowMotionOnRestart = false; // Sıfırla
            }

            isHintUsedInCurrentLevel = false;
            isShieldUsedInCurrentLevel = false;

            OnSkillsUpdated?.Invoke();

            StartCoroutine(LoadLevelRoutine());
        }

        /// <summary>
        /// Seviye yükleme akışını yöneten Coroutine.
        /// </summary>
        private IEnumerator LoadLevelRoutine()
        {
            ChangeState(GameState.Loading);

            // Önceki seviyeyi temizle
            objectPooler.ReturnAllToPool();
            activeEnvelopes.Clear();
            currentTargetIndex = 0;

            // Seviyenin kelimelerini al
            levelWords = currentLevelData.GetWords();
            int wordCount = levelWords.Length;
            int trapCount = currentLevelData.TrapCount;
            int totalCount = wordCount + trapCount;

            if (wordCount == 0)
            {
                Debug.LogError("[GameManager] Seviyedeki kelime sayısı 0!");
                yield break;
            }

            // Pozisyon indekslerini hazırlayalım
            List<int> gridIndices = new List<int>();
            for (int i = 0; i < totalCount; i++)
            {
                gridIndices.Add(i);
            }

            // Eğer seviyede başlangıç pozisyonları karıştırılmak isteniyorsa
            if (currentLevelData.RandomizeInitialPositions)
            {
                for (int i = gridIndices.Count - 1; i > 0; i--)
                {
                    int rnd = UnityEngine.Random.Range(0, i + 1);
                    int temp = gridIndices[i];
                    gridIndices[i] = gridIndices[rnd];
                    gridIndices[rnd] = temp;
                }
            }

            // Zarfları konumlandır ve oluştur
            Vector2 finalCellSize;
            Vector2[] gridPositions = CalculateGridPositions(totalCount, out finalCellSize);
            for (int i = 0; i < totalCount; i++)
            {
                Envelope envelope = objectPooler.GetPooledObject();
                if (envelope != null)
                {
                    int gridIndex = gridIndices[i];
                    if (i < wordCount)
                    {
                        envelope.Initialize(levelWords[i], i, gridIndex);
                    }
                    else
                    {
                        // Tuzak zarf: Kelimesi boş "", orijinal kelime sırası -1
                        envelope.Initialize("", -1, gridIndex);
                    }
                    
                    // Boyutu sığacak şekilde ayarla
                    envelope.RectTransform.sizeDelta = finalCellSize;
                    
                    // Pozisyonu ata
                    envelope.RectTransform.anchoredPosition = gridPositions[gridIndex];
                    activeEnvelopes.Add(envelope);
                }
            }

            // UI'ı bilgilendir
            OnLevelLoaded?.Invoke(currentLevelData);

            // Oyuncuya kelimeleri göster (parıldayan transparan önizleme modu)
            ChangeState(GameState.ShowingWords);
            foreach (var envelope in activeEnvelopes)
            {
                envelope.StartPreviewState(0.5f);
            }

            // Gösterme süresi kadar bekle
            yield return new WaitForSeconds(currentLevelData.InitialShowDuration);

            // Karıştırma aşamasına geç
            StartShufflePhase();
        }

        /// <summary>
        /// Karıştırma fazını tetikler.
        /// </summary>
        private void StartShufflePhase()
        {
            ChangeState(GameState.Shuffling);

            // Kelimeleri gizle ve tıklamaları engelle
            foreach (var envelope in activeEnvelopes)
            {
                envelope.RevealWord(false);
                envelope.SetInteractable(false);
            }

            // Karıştırmayı başlat
            shuffleController.StartShuffle(
                activeEnvelopes, 
                currentLevelData.ShuffleCount, 
                currentLevelData.SwapDuration, 
                currentLevelData.ArcHeight
            );
        }

        /// <summary>
        /// Karıştırma bittiğinde çağrılır.
        /// </summary>
        private void OnShuffleFinished()
        {
            ChangeState(GameState.Gameplay);

            // Zarfları tıklanabilir yap
            foreach (var envelope in activeEnvelopes)
            {
                envelope.SetInteractable(true);
            }
        }

        /// <summary>
        /// Oyuncu bir zarfa tıkladığında doğrulama yapar.
        /// </summary>
        public void OnEnvelopeClicked(Envelope clickedEnvelope)
        {
            if (currentState != GameState.Gameplay) return;

            // Doğru kelime tıklandı mı? (Cümledeki yinelenen kelimelerin her iki zarfla da çözülebilmesi için büyük/küçük harf duyarsız metin karşılaştırması yapıyoruz)
            if (!clickedEnvelope.IsSolved && 
                string.Equals(clickedEnvelope.Word, levelWords[currentTargetIndex], StringComparison.OrdinalIgnoreCase))
            {
                // Doğru tıklama geri bildirimi
                clickedEnvelope.PlayCorrectFeedback(0.4f, () => {
                    // Kelime açıldı
                    clickedEnvelope.RevealWord(true);
                });

                string solvedWord = levelWords[currentTargetIndex];
                OnWordSolved?.Invoke(currentTargetIndex, solvedWord);
                
                currentTargetIndex++;

                // Cümle tamamlandı mı?
                if (currentTargetIndex >= levelWords.Length)
                {
                    StartCoroutine(LevelWinRoutine());
                }
            }
            else
            {
                // Yanlış tıklama geri bildirimi (Kırmızı sarsıntı)
                clickedEnvelope.PlayIncorrectFeedback(0.5f);
            }
        }

        /// <summary>
        /// Seviye kazanıldığında çalışır.
        /// </summary>
        private IEnumerator LevelWinRoutine()
        {
            ChangeState(GameState.LevelWin);
            
            // Tüm zarfları etkileşime kapat
            foreach (var envelope in activeEnvelopes)
            {
                envelope.SetInteractable(false);
            }

            yield return new WaitForSeconds(1.3f);

            // Bölüm kazanıldığında tüm zarfları ekrandan gizle
            foreach (var envelope in activeEnvelopes)
            {
                if (envelope != null)
                {
                    envelope.gameObject.SetActive(false);
                }
            }
            
            OnLevelWin?.Invoke();
        }

        /// <summary>
        /// Bir sonraki seviyeye geçişi tetikler.
        /// </summary>
        public void LoadNextLevel()
        {
            StartLevel(currentLevelIndex + 1);
        }

        /// <summary>
        /// Mevcut seviyeyi yeniden başlatır.
        /// </summary>
        public void RestartLevel()
        {
            StartLevel(currentLevelIndex);
        }

        /// <summary>
        /// Oyun durumunu değiştirir ve dinleyicileri uyarır.
        /// </summary>
        private void ChangeState(GameState newState)
        {
            currentState = newState;
            OnStateChanged?.Invoke(currentState);
        }

        /// <summary>
        /// Kelimelerin UI üzerinde ortalanmış geometrik (Grid, Pyramid, InversePyramid, Diamond, Circle) pozisyonlarını ve sığacak hücre boyutunu hesaplar.
        /// </summary>
        private Vector2[] CalculateGridPositions(int itemCount, out Vector2 finalCellSize)
        {
            Vector2[] positions = new Vector2[itemCount];
            if (itemCount <= 0)
            {
                finalCellSize = cellSize;
                return positions;
            }
            if (itemCount == 1)
            {
                positions[0] = Vector2.zero;
                finalCellSize = cellSize;
                return positions;
            }

            // Get layout type from current level data
            EnvelopeLayoutType layout = EnvelopeLayoutType.Grid;
            if (currentLevelData != null)
            {
                layout = currentLevelData.LayoutType;
            }

            // 1. Zarf kabının (Container) ekran genişliğini belirle
            float containerWidth = 1080f; // Mobil varsayılan dikey çözünürlük
            if (objectPooler != null && objectPooler.Container != null)
            {
                RectTransform containerRect = objectPooler.Container.GetComponent<RectTransform>();
                if (containerRect != null)
                {
                    containerWidth = containerRect.rect.width;
                    if (containerWidth <= 0) containerWidth = 1080f;
                }
            }

            // 2. Zarf açıldığında çakışmayı önlemek için minimum yatay boşluk hesabı (1.35x açılma ölçeği baz alınır)
            float minSpacingX = cellSize.x * 0.38f;
            float targetSpacingX = Mathf.Max(cellSpacing.x, minSpacingX);
            float targetSpacingY = cellSpacing.y;
            float maxAllowedWidth = containerWidth * 0.92f; // %8 emniyet marjı

            if (layout == EnvelopeLayoutType.Circle)
            {
                // Circumference and Radius logic for Circle layout
                // We want distance between adjacent items on the circle to be at least targetDistance
                float targetDistance = cellSize.x + targetSpacingX;
                float angleStep = (2f * Mathf.PI) / itemCount;
                float sinHalf = Mathf.Sin(angleStep / 2f);
                float radius = targetDistance / (2f * sinHalf);

                float gridWidth = 2f * radius + cellSize.x;
                float scaleFactor = 1f;
                if (gridWidth > maxAllowedWidth)
                {
                    scaleFactor = maxAllowedWidth / gridWidth;
                }

                finalCellSize = cellSize * scaleFactor;
                float activeSpacingX = targetSpacingX * scaleFactor;
                float activeDistance = finalCellSize.x + activeSpacingX;
                float activeRadius = activeDistance / (2f * Mathf.Sin(Mathf.PI / itemCount));

                // Position elements clockwise starting from 12 o'clock (90 degrees)
                for (int i = 0; i < itemCount; i++)
                {
                    float angle = (Mathf.PI / 2f) - i * angleStep;
                    float posX = activeRadius * Mathf.Cos(angle);
                    float posY = activeRadius * Mathf.Sin(angle);
                    positions[i] = new Vector2(posX, posY);
                }
            }
            else
            {
                // Row-based layouts (Grid, Pyramid, InversePyramid, Diamond)
                List<int> rowSizes = GetRowSizesForLayout(layout, itemCount);
                int rows = rowSizes.Count;

                // Find max column count in any row for scaling calculations
                int maxColsInRow = 1;
                foreach (int size in rowSizes)
                {
                    if (size > maxColsInRow) maxColsInRow = size;
                }

                float gridWidth = maxColsInRow * cellSize.x + (maxColsInRow - 1) * targetSpacingX;
                float scaleFactor = 1f;

                if (gridWidth > maxAllowedWidth)
                {
                    scaleFactor = maxAllowedWidth / gridWidth;
                }

                // Ölçeklenmiş nihai boyutlar
                finalCellSize = cellSize * scaleFactor;
                float activeCellSizeX = finalCellSize.x;
                float activeCellSizeY = finalCellSize.y;
                
                float activeSpacingX = targetSpacingX * scaleFactor;
                float activeSpacingY = targetSpacingY * scaleFactor;

                // Toplam yükseklik (Merkezleme için)
                float totalHeight = (rows - 1) * (activeCellSizeY + activeSpacingY);
                float startY = totalHeight * 0.5f;

                int itemIndex = 0;
                for (int r = 0; r < rows; r++)
                {
                    int rowItemsCount = rowSizes[r];
                    float rowWidth = (rowItemsCount - 1) * (activeCellSizeX + activeSpacingX);
                    float startX = -rowWidth * 0.5f;
                    float posY = startY - r * (activeCellSizeY + activeSpacingY);

                    for (int c = 0; c < rowItemsCount; c++)
                    {
                        if (itemIndex >= itemCount) break;

                        float posX = startX + c * (activeCellSizeX + activeSpacingX);
                        positions[itemIndex] = new Vector2(posX, posY);
                        itemIndex++;
                    }
                }
            }

            return positions;
        }

        /// <summary>
        /// Belirtilen yerleşim türü ve eleman sayısına göre satır genişliklerini (sütun sayılarını) döner.
        /// </summary>
        private List<int> GetRowSizesForLayout(EnvelopeLayoutType layout, int itemCount)
        {
            List<int> rowSizes = new List<int>();

            if (layout == EnvelopeLayoutType.Grid)
            {
                int cols = Mathf.Min(itemCount, maxColumns);
                int rows = Mathf.CeilToInt((float)itemCount / cols);
                for (int r = 0; r < rows; r++)
                {
                    if (r == rows - 1)
                    {
                        rowSizes.Add(itemCount - r * cols);
                    }
                    else
                    {
                        rowSizes.Add(cols);
                    }
                }
                return rowSizes;
            }

            if (layout == EnvelopeLayoutType.Pyramid)
            {
                switch (itemCount)
                {
                    case 1: rowSizes.AddRange(new[] { 1 }); break;
                    case 2: rowSizes.AddRange(new[] { 1, 1 }); break;
                    case 3: rowSizes.AddRange(new[] { 1, 2 }); break;
                    case 4: rowSizes.AddRange(new[] { 1, 3 }); break;
                    case 5: rowSizes.AddRange(new[] { 1, 2, 2 }); break;
                    case 6: rowSizes.AddRange(new[] { 1, 2, 3 }); break;
                    case 7: rowSizes.AddRange(new[] { 1, 2, 4 }); break;
                    case 8: rowSizes.AddRange(new[] { 1, 2, 5 }); break;
                    case 9: rowSizes.AddRange(new[] { 1, 3, 5 }); break;
                    case 10: rowSizes.AddRange(new[] { 1, 2, 3, 4 }); break;
                    case 11: rowSizes.AddRange(new[] { 1, 2, 3, 5 }); break;
                    case 12: rowSizes.AddRange(new[] { 1, 2, 3, 6 }); break;
                    default:
                        int remaining = itemCount;
                        int cur = 1;
                        while (remaining > 0)
                        {
                            if (remaining >= cur)
                            {
                                rowSizes.Add(cur);
                                remaining -= cur;
                                cur++;
                            }
                            else
                            {
                                rowSizes[rowSizes.Count - 1] += remaining;
                                remaining = 0;
                            }
                        }
                        break;
                }
                return rowSizes;
            }

            if (layout == EnvelopeLayoutType.InversePyramid)
            {
                List<int> normalPyramid = GetRowSizesForLayout(EnvelopeLayoutType.Pyramid, itemCount);
                normalPyramid.Reverse();
                return normalPyramid;
            }

            if (layout == EnvelopeLayoutType.Diamond)
            {
                switch (itemCount)
                {
                    case 1: rowSizes.AddRange(new[] { 1 }); break;
                    case 2: rowSizes.AddRange(new[] { 1, 1 }); break;
                    case 3: rowSizes.AddRange(new[] { 1, 1, 1 }); break;
                    case 4: rowSizes.AddRange(new[] { 1, 2, 1 }); break;
                    case 5: rowSizes.AddRange(new[] { 1, 3, 1 }); break;
                    case 6: rowSizes.AddRange(new[] { 1, 2, 2, 1 }); break;
                    case 7: rowSizes.AddRange(new[] { 2, 3, 2 }); break;
                    case 8: rowSizes.AddRange(new[] { 1, 3, 3, 1 }); break;
                    case 9: rowSizes.AddRange(new[] { 1, 2, 3, 2, 1 }); break;
                    case 10: rowSizes.AddRange(new[] { 1, 2, 4, 2, 1 }); break;
                    case 11: rowSizes.AddRange(new[] { 1, 3, 3, 3, 1 }); break;
                    case 12: rowSizes.AddRange(new[] { 1, 3, 4, 3, 1 }); break;
                    default:
                        int rem = itemCount;
                        List<int> topHalf = new List<int>();
                        int currentWidth = 1;
                        while (rem >= currentWidth * 2)
                        {
                            topHalf.Add(currentWidth);
                            rem -= currentWidth * 2;
                            currentWidth++;
                        }
                        if (rem > 0)
                        {
                            topHalf.Add(rem);
                        }
                        rowSizes.AddRange(topHalf);
                        for (int i = topHalf.Count - 2; i >= 0; i--)
                        {
                            rowSizes.Add(topHalf[i]);
                        }
                        break;
                }
                return rowSizes;
            }

            return rowSizes;
        }

        #region Skill Systems
        public bool UseSlowMotionSkill()
        {
            // Seviye kilidi kontrolü (Seviye 5 ve üzeri)
            if (currentLevelIndex < 5) return false;
            // Zaten kullanıldı mı kontrolü
            if (isSlowMoUsedInCurrentLevel) return false;

            isSlowMoUsedInCurrentLevel = true;
            isSlowMotionActive = true;

            // Eğer oyuncu guessing (Gameplay) fazındaysa, bölümü yavaş çekimde baştan başlat
            if (currentState == GameState.Gameplay)
            {
                preserveSlowMotionOnRestart = true;
                RestartLevel();
            }

            OnSkillsUpdated?.Invoke();
            return true;
        }

        public bool UseHintSkill()
        {
            // Seviye kilidi kontrolü (Seviye 12 ve üzeri)
            if (currentLevelIndex < 12) return false;
            // Zaten kullanıldı mı kontrolü
            if (isHintUsedInCurrentLevel) return false;
            // Doğru oyun durumunda mıyız kontrolü
            if (currentState != GameState.Gameplay) return false;

            // Mevcut hedef kelimeyi içeren zarfı bul
            Envelope targetEnvelope = activeEnvelopes.Find(e => e.WordIndex == currentTargetIndex);
            if (targetEnvelope != null)
            {
                isHintUsedInCurrentLevel = true;
                targetEnvelope.PlayHintAnimation(1.5f);
                OnSkillsUpdated?.Invoke();
                return true;
            }
            return false;
        }

        public bool UseTrapShieldSkill()
        {
            // Seviye kilidi kontrolü (Seviye 18 ve üzeri)
            if (currentLevelIndex < 18) return false;
            // Zaten kullanıldı mı kontrolü
            if (isShieldUsedInCurrentLevel) return false;
            // Doğru oyun durumunda mıyız kontrolü
            if (currentState != GameState.Gameplay) return false;

            // Çözülmemiş ilk tuzak zarfı bul (-1 indeksli)
            Envelope trapEnvelope = activeEnvelopes.Find(e => e.WordIndex == -1 && !e.IsSolved);
            if (trapEnvelope != null)
            {
                isShieldUsedInCurrentLevel = true;
                trapEnvelope.DisableTrap();
                OnSkillsUpdated?.Invoke();
                return true;
            }
            else
            {
                Debug.LogWarning("[GameManager] Deaktif edilecek tuzak zarf bulunamadı!");
                return false;
            }
        }
        #endregion

        #region Editor Helper Functions
        [ContextMenu("Skip Level")]
        private void SkipLevelTest()
        {
            if (Application.isPlaying)
            {
                LoadNextLevel();
            }
        }

        [ContextMenu("Restart Level")]
        private void RestartLevelTest()
        {
            if (Application.isPlaying)
            {
                RestartLevel();
            }
        }
        #endregion
    }
}

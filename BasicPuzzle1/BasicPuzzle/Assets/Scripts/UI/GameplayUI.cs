using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BasicPuzzle.Core;
using BasicPuzzle.Data;

namespace BasicPuzzle.UI
{
    public class GameplayUI : MonoBehaviour
    {
        [Header("Text Components")]
        [Tooltip("Cümlenin gösterileceği TMP alanı")]
        [SerializeField] private TextMeshProUGUI sentenceText;

        [Tooltip("Oyuncuya yönlendirme metnini gösteren TMP alanı")]
        [SerializeField] private TextMeshProUGUI statusText;

        [Tooltip("Mevcut Seviyeyi gösteren TMP alanı (Örn: Seviye 12 / 100)")]
        [SerializeField] private TextMeshProUGUI levelText;

        [Header("Panels")]
        [Tooltip("Kazanma ekranı paneli")]
        [SerializeField] private GameObject winPanel;

        [Tooltip("Yetenek (Skill) butonları paneli")]
        [SerializeField] private GameObject skillPanel;

        [Header("Buttons")]
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button inGameRestartButton;

        private void Start()
        {
            // Panel durumunu başlangıçta pasifleştir
            if (winPanel != null)
            {
                winPanel.SetActive(false);
            }

            // Buton dinleyicilerini tanımla
            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(OnNextLevelClicked);

            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (inGameRestartButton != null)
                inGameRestartButton.onClick.AddListener(OnRestartClicked);

            // GameManager event'lerine abone ol
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelLoaded += HandleLevelLoaded;
                GameManager.Instance.OnStateChanged += HandleStateChanged;
                GameManager.Instance.OnWordSolved += HandleWordSolved;
                GameManager.Instance.OnLevelWin += HandleLevelWin;

                // İlk açılışta verileri elle güncelle (Script Execution Order yarışını önlemek için)
                RefreshLevelDisplay();
                RefreshSentenceDisplay();
            }
        }

        private void OnDestroy()
        {
            // Event aboneliklerini temizle
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnLevelLoaded -= HandleLevelLoaded;
                GameManager.Instance.OnStateChanged -= HandleStateChanged;
                GameManager.Instance.OnWordSolved -= HandleWordSolved;
                GameManager.Instance.OnLevelWin -= HandleLevelWin;
            }
        }

        /// <summary>
        /// Seviye yüklendiğinde metinleri ve panelleri sıfırlar.
        /// </summary>
        private void HandleLevelLoaded(LevelData levelData)
        {
            if (winPanel != null)
            {
                winPanel.SetActive(false);
            }

            RefreshSentenceDisplay();
            RefreshLevelDisplay();
        }

        /// <summary>
        /// Oyun durumu değiştiğinde başlık yönergesini günceller.
        /// </summary>
        private void HandleStateChanged(GameState state)
        {
            string message = "";
            switch (state)
            {
                case GameState.Loading:
                    message = "Yeni Seviye Yükleniyor...";
                    break;
                case GameState.ShowingWords:
                    message = "<color=#FFFF00>KELİMELERİ AKLINDA TUT!</color>";
                    break;
                case GameState.Shuffling:
                    message = "<color=#FFA500>ZARFLARI DİKKATLE TAKİP ET!</color>";
                    break;
                case GameState.Gameplay:
                    string targetWord = GetCurrentTargetWord();
                    message = $"SIRADAKİ KELİMEYİ BUL: <color=#00FF00><b>\"{targetWord}\"</b></color>";
                    break;
                case GameState.LevelWin:
                    message = "<color=#00FF00>TEBRİKLER! BÖLÜMÜ TAMAMLADIN!</color>";
                    break;
                case GameState.LevelFail:
                    message = "<color=#FF0000>BAŞARISIZ! TEKRAR DENE.</color>";
                    break;
            }

            if (statusText != null)
            {
                statusText.text = message;
                
                // Mikro-animasyon: Yönerge değiştiğinde hafifçe büyüyüp küçülür
                statusText.transform.DOKill();
                statusText.transform.localScale = Vector3.one;
                statusText.transform.DOPunchScale(Vector3.one * 0.12f, 0.3f, 8, 1f);
            }

            // Seviye başarıyla tamamlandığında üstteki metinleri, oyun içi restart butonunu ve yetenek panelini gizle
            if (state == GameState.LevelWin)
            {
                if (levelText != null) levelText.gameObject.SetActive(false);
                if (sentenceText != null) sentenceText.gameObject.SetActive(false);
                if (inGameRestartButton != null) inGameRestartButton.gameObject.SetActive(false);
                if (skillPanel != null) skillPanel.SetActive(false);
            }
            else
            {
                if (levelText != null) levelText.gameObject.SetActive(true);
                if (sentenceText != null) sentenceText.gameObject.SetActive(true);
                if (inGameRestartButton != null) inGameRestartButton.gameObject.SetActive(true);
                if (skillPanel != null) skillPanel.SetActive(true);
            }
        }

        /// <summary>
        /// Her kelime doğru çözüldüğünde cümleyi ve yönergeyi günceller.
        /// </summary>
        private void HandleWordSolved(int solvedIndex, string solvedWord)
        {
            RefreshSentenceDisplay();
            
            // Eğer oyun devam ediyorsa sonraki hedef kelimeyi göster
            if (statusText != null && GameManager.Instance.CurrentState == GameState.Gameplay)
            {
                string nextTargetWord = GetCurrentTargetWord();
                statusText.text = $"SIRADAKİ KELİMEYİ BUL: <color=#00FF00><b>\"{nextTargetWord}\"</b></color>";
                
                statusText.transform.DOKill();
                statusText.transform.localScale = Vector3.one;
                statusText.transform.DOPunchScale(Vector3.one * 0.1f, 0.25f, 5, 1f);
            }
        }

        /// <summary>
        /// Bölüm kazanıldığında kazanma panelini DOTween ile açar.
        /// </summary>
        private void HandleLevelWin()
        {
            if (winPanel != null)
            {
                winPanel.transform.DOKill();
                winPanel.transform.localScale = Vector3.zero;
                winPanel.SetActive(true);
                
                // Panel arkadan öne fırlama animasyonuyla açılır (OutBack)
                winPanel.transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
            }
        }

        /// <summary>
        /// Mevcut seviyeyi ve toplam seviye sayısını günceller.
        /// </summary>
        private void RefreshLevelDisplay()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[GameplayUI] GameManager.Instance is null!");
                return;
            }
            if (levelText == null)
            {
                Debug.LogWarning("[GameplayUI] levelText bileşen referansı boş! Lütfen Inspector penceresinden sürükleyip bırakın.");
                return;
            }

            int levelIndex = GameManager.Instance.CurrentLevelIndex;
            if (levelIndex == 0)
            {
                levelText.text = "TUTORIAL";
            }
            else
            {
                levelText.text = $"LEVEL {levelIndex}";
            }
            
            Debug.Log($"[GameplayUI] Seviye metni başarıyla güncellendi: {levelText.text}");
        }

        /// <summary>
        /// Cümledeki kelimelerin renk ve durumlarını günceller (Zengin Metin).
        /// </summary>
        private void RefreshSentenceDisplay()
        {
            if (sentenceText == null || GameManager.Instance == null) return;

            string[] words = GameManager.Instance.LevelWords;
            int currentTarget = GameManager.Instance.CurrentTargetIndex;

            if (words == null || words.Length == 0) return;

            string formattedSentence = "";

            for (int i = 0; i < words.Length; i++)
            {
                if (i < currentTarget)
                {
                    // Çözülmüş kelimeler (Yeşil)
                    formattedSentence += $"<color=#00FF00>{words[i]}</color>";
                }
                else if (i == currentTarget)
                {
                    // Şuan aranılan hedef kelime (Kalın, altı çizgili Sarı)
                    formattedSentence += $"<color=#FFFF00><u><b>{words[i]}</b></u></color>";
                }
                else
                {
                    // Henüz çözülmemiş kelimeler (Gri)
                    formattedSentence += $"<color=#888888>{words[i]}</color>";
                }

                if (i < words.Length - 1)
                {
                    formattedSentence += " "; // Kelimeler arasına boşluk koy
                }
            }

            sentenceText.text = formattedSentence;
        }

        /// <summary>
        /// GameManager'dan aranılan aktif hedef kelimeyi çeker.
        /// </summary>
        private string GetCurrentTargetWord()
        {
            if (GameManager.Instance == null) return string.Empty;

            string[] words = GameManager.Instance.LevelWords;
            int targetIndex = GameManager.Instance.CurrentTargetIndex;

            if (words != null && targetIndex >= 0 && targetIndex < words.Length)
            {
                return words[targetIndex];
            }

            return string.Empty;
        }

        #region Button Callbacks
        private void OnNextLevelClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadNextLevel();
            }
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartLevel();
            }
        }
        #endregion
    }
}

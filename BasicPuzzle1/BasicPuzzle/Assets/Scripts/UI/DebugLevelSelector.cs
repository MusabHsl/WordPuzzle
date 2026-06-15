using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using BasicPuzzle.Core;

namespace BasicPuzzle.UI
{
    public class DebugLevelSelector : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Butonların ekleneceği UI Panel (Örn: Grid Layout Group barındıran bir panel)")]
        [SerializeField] private Transform container;

        [Tooltip("Seviye butonu Prefab'ı (Üzerinde Button ve TextMeshProUGUI bileşeni veya alt nesnesinde TMP olmalı)")]
        [SerializeField] private GameObject buttonPrefab;

        [Header("UI Toggle")]
        [Tooltip("Panelin açılıp kapanmasını sağlayan ana panel (Opsiyonel)")]
        [SerializeField] private GameObject mainPanel;

        private void Start()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[DebugLevelSelector] GameManager bulunamadı!");
                return;
            }

            // GameManager'daki seviye sayısına göre butonları üret
            CreateLevelButtons();
        }

        private void OnDestroy()
        {
            if (mainPanel != null)
            {
                mainPanel.transform.DOKill();
            }
        }

        /// <summary>
        /// GameManager'daki seviye sayısına göre dinamik olarak butonları üretir.
        /// </summary>
        private void CreateLevelButtons()
        {
            if (container == null || buttonPrefab == null)
            {
                Debug.LogError("[DebugLevelSelector] Gerekli referanslar (container veya buttonPrefab) eksik!");
                return;
            }

            // Önce konteyner içindeki eski test butonlarını temizle
            foreach (Transform child in container)
            {
                Destroy(child.gameObject);
            }

            int levelCount = GameManager.Instance.TotalLevelCount;

            for (int i = 0; i < levelCount; i++)
            {
                // Butonu oluştur
                GameObject btnObj = Instantiate(buttonPrefab, container);
                btnObj.SetActive(true); // Şablon kapalı olsa bile üretilen butonları aktif et ki görünsünler!
                
                // Butonun yazısını ayarla (İlk seviye Eğitim, diğerleri 1-100 arası seviye)
                TextMeshProUGUI btnText = btnObj.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null)
                {
                    if (i == 0)
                    {
                        btnText.text = "Tutorial";
                    }
                    else
                    {
                        btnText.text = i.ToString();
                    }
                }

                // Buton tıklama olayını bağla
                Button btn = btnObj.GetComponent<Button>();
                if (btn != null)
                {
                    int levelIndex = i; // C# Lambda Closure (kapsam) sorunu yaşamamak için lokal kopya
                    btn.onClick.AddListener(() =>
                    {
                        GameManager.Instance.StartLevel(levelIndex);
                        
                        // İsteğe bağlı: Seviye seçilince debug panelini otomatik kapat
                        if (mainPanel != null)
                        {
                            TogglePanel(); // Animasyonlu kapat
                        }
                    });
                }
            }
        }

        /// <summary>
        /// Debug panelini buton yardımıyla açıp kapatmak için kullanılacak toggle metodu (DOTween Animasyonlu).
        /// </summary>
        public void TogglePanel()
        {
            if (mainPanel == null) return;

            mainPanel.transform.DOKill();

            if (!mainPanel.activeSelf)
            {
                // Açılma Animasyonu: Paneli aktif et, ölçeği sıfırla ve fırlayarak büyüt (OutBack)
                mainPanel.SetActive(true);
                mainPanel.transform.localScale = Vector3.zero;
                mainPanel.transform.DOScale(Vector3.one, 0.4f).SetEase(Ease.OutBack);
            }
            else
            {
                // Kapanma Animasyonu: Paneli geri küçült (InBack), bitince tamamen deaktif et
                mainPanel.transform.DOScale(Vector3.zero, 0.3f)
                    .SetEase(Ease.InBack)
                    .OnComplete(() => mainPanel.SetActive(false));
            }
        }
    }
}

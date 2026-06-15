using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace BasicPuzzle.UI
{
    [RequireComponent(typeof(Image))]
    [RequireComponent(typeof(RectTransform))]
    public class BackgroundEffect : MonoBehaviour
    {
        [Header("Renk Dalgalanması (Color Shift)")]
        [Tooltip("Renk geçişinin tamamlanma süresi (saniye)")]
        [SerializeField] private float colorTransitionDuration = 12f;

        [Tooltip("Arka planın yavaşça kayacağı hedef renk (Örn: Hafif mor/pembe bir ton)")]
        [SerializeField] private Color targetColor = new Color(0.85f, 0.75f, 1f, 1f);

        [Header("Nefes Alma (Scale Pulse)")]
        [Tooltip("Yavaşça büyüme/küçülme süresi (saniye)")]
        [SerializeField] private float scalePulseDuration = 18f;

        [Tooltip("Maksimum büyüme oranı (Örn: 1.03 = %3 daha büyük)")]
        [SerializeField] private float maxScaleMultiplier = 1.03f;

        [Header("Yavaş Kayma (Drift)")]
        [Tooltip("Arka planın çok hafif yatay/dikey kayma mesafesi")]
        [SerializeField] private Vector2 driftOffset = new Vector2(15f, 15f);

        [Tooltip("Kayma hareketinin süresi (saniye)")]
        [SerializeField] private float driftDuration = 15f;

        private Image backgroundImage;
        private RectTransform rectTransform;
        
        private Color originalColor;
        private Vector3 originalScale;
        private Vector2 originalPosition;

        private void Awake()
        {
            backgroundImage = GetComponent<Image>();
            rectTransform = GetComponent<RectTransform>();

            originalColor = backgroundImage.color;
            originalScale = transform.localScale;
            originalPosition = rectTransform.anchoredPosition;
        }

        private void Start()
        {
            StartEffects();
        }

        private void StartEffects()
        {
            // Önceki animasyonları temizle
            transform.DOKill();
            backgroundImage.DOKill();
            rectTransform.DOKill();

            // 1. Renk Dalgalanması: Orijinal renkten hedef renge yavaşça geçip geri döner (Yoyo)
            backgroundImage.DOColor(targetColor, colorTransitionDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            // 2. Nefes Alma: Çok yavaşça büyüyüp küçülür (Yoyo)
            transform.DOScale(originalScale * maxScaleMultiplier, scalePulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            // 3. Hafif Kayma (Drift): Görseli döndürmeden çok yavaşça el kamerası gibi sallar (Yoyo)
            rectTransform.DOAnchorPos(originalPosition + driftOffset, driftDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void OnDestroy()
        {
            // Temizlik
            if (backgroundImage != null) backgroundImage.DOKill();
            transform.DOKill();
            if (rectTransform != null) rectTransform.DOKill();
        }
    }
}

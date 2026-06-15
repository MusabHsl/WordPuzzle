using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using BasicPuzzle.Core;

namespace BasicPuzzle.Gameplay
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(Image))]
    public class Envelope : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField] private TextMeshProUGUI wordText;
        [SerializeField] private RectTransform paperRect;
        
        [Header("Visual Resources")]
        [Tooltip("Zarfın kapalı görseli")]
        [SerializeField] private Sprite closedSprite;
        
        [Tooltip("Zarfın açık görseli")]
        [SerializeField] private Sprite openSprite;

        [Header("Color Settings")]
        [SerializeField] private Color normalColor = new Color(0.15f, 0.16f, 0.22f, 1f); // Koyu Gece Mavisi / Antrasit (Açık arka plana göre maksimum kontrast sağlar)
        [SerializeField] private Color targetColor = new Color(0.0f, 0.85f, 0.7f, 1f); // Neon Cam Göbeği (Aşırı parlak ve premium)
        [SerializeField] private Color incorrectColor = new Color(1f, 0.25f, 0.35f, 1f); // Neon Mercan Kırmızı
        [SerializeField] private Color solvedColor = new Color(0.2f, 0.22f, 0.28f, 0.9f); // Koyu Füme (Yazının net okunabilmesi için)

        [Tooltip("Zarf açıldığında yazının normal rengi")]
        [SerializeField] private Color normalTextColor = Color.white;

        [Tooltip("Zarf doğru çözüldüğünde yazının rengi")]
        [SerializeField] private Color solvedTextColor = new Color(1f, 0.88f, 0.1f, 0.95f); // Altın Sarısı

        [Header("Scale Settings")]
        [Tooltip("Zarf açıldığında yatayda (X) en-boy farkını telafi etme oranı")]
        [SerializeField] private float openScaleXMultiplier = 1.35f;

        [Tooltip("Zarf açıldığında dikeyde (Y) en-boy farkını telafi etme oranı")]
        [SerializeField] private float openScaleYMultiplier = 1.1f;

        [Header("Z-Layering Overlays")]
        [Tooltip("Zarfın ön cebi ve mührü içeren üst katman objesi (kağıdın önünde durması için)")]
        [SerializeField] private GameObject envelopeFrontObj;

        // Private variables
        private RectTransform rectTransform;
        private Image envelopeImage;
        private Image paperImage;
        
        private string word;
        private int wordIndex;
        private int currentGridIndex;
        private bool isOpened = false;
        private bool isSolved = false;
        private bool isInteractable = false;

        private Vector3 originalScale;
        private Color originalImageColor;
        private Color originalTextColor = Color.white;

        // Getters
        public string Word => word;
        public int WordIndex => wordIndex;
        public int CurrentGridIndex { get => currentGridIndex; set => currentGridIndex = value; }
        public bool IsOpened => isOpened;
        public bool IsSolved => isSolved;
        public RectTransform RectTransform => rectTransform;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            envelopeImage = GetComponent<Image>();
            originalScale = transform.localScale;
            originalImageColor = envelopeImage.color;
            
            // Eğer paperRect Inspector'dan atanmadıysa Paper isimli alt nesneyi büyük/küçük harf duyarsız bulmaya çalış
            if (paperRect == null)
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name.Equals("paper", StringComparison.OrdinalIgnoreCase))
                    {
                        paperRect = child.GetComponent<RectTransform>();
                        break;
                    }
                }
            }

            if (paperRect != null)
            {
                paperRect.transform.localPosition = new Vector3(paperRect.transform.localPosition.x, paperRect.transform.localPosition.y, 0f);
                
                // Siyah kağıdın kendi Image bileşenini bul (PaperMask altındaysa onu, yoksa doğrudan kendininkini al)
                Transform paperChild = paperRect.Find("Paper");
                paperImage = paperChild != null ? paperChild.GetComponent<Image>() : paperRect.GetComponent<Image>();
            }

            // Unity Inspector'daki eski serileştirilmiş değerlerin kodu ezmesini engellemek için renkleri runtime'da zorla atıyoruz (Force Override)
            normalTextColor = Color.white; // Açık mektuplarda temiz beyaz
            solvedTextColor = new Color(1f, 0.85f, 0.15f, 1f); // Siyah kağıt üzerinde parlak altın sarısı

            if (wordText != null)
            {
                wordText.transform.localPosition = new Vector3(wordText.transform.localPosition.x, wordText.transform.localPosition.y, 0f);
                wordText.transform.SetAsLastSibling(); // Çizim sırasında en önde olmasını garanti et (siyah kağıdın arkasına düşmesin)
                originalTextColor = normalTextColor;
                wordText.color = normalTextColor;
                wordText.fontStyle = FontStyles.Bold;
            }

            // Eğer envelopeFrontObj Inspector'dan atanmadıysa EnvelopeFront isimli alt nesneyi bul
            if (envelopeFrontObj == null)
            {
                Transform frontChild = transform.Find("EnvelopeFront");
                if (frontChild != null)
                {
                    envelopeFrontObj = frontChild.gameObject;
                }
            }

            normalColor = Color.white; // Standart oynanışta beyaz zarf rengi
            targetColor = new Color(0.0f, 0.85f, 0.7f, 1f); 
            incorrectColor = new Color(1f, 0.25f, 0.35f, 1f); 
            solvedColor = new Color(0.2f, 0.22f, 0.28f, 0.9f);
        }

        /// <summary>
        /// Zarfı yeni kelime ve sıra bilgileriyle hazırlar.
        /// </summary>
        public void Initialize(string newWord, int originalIndex, int gridIndex, Sprite customClosed = null, Sprite customOpen = null)
        {
            word = newWord;
            wordIndex = originalIndex;
            currentGridIndex = gridIndex;

            if (customClosed != null) closedSprite = customClosed;
            if (customOpen != null) openSprite = customOpen;

            // Görsel sıfırlama
            envelopeImage.sprite = closedSprite;
            envelopeImage.color = normalColor;
            
            if (paperRect != null)
            {
                paperRect.DOKill();
                paperRect.anchoredPosition = Vector2.zero;
                paperRect.transform.localPosition = new Vector3(paperRect.transform.localPosition.x, paperRect.transform.localPosition.y, 0f);
                paperRect.localScale = Vector3.one;
                paperRect.localRotation = Quaternion.identity;
                if (paperImage != null)
                {
                    paperImage.enabled = false;
                }
                paperRect.gameObject.SetActive(false);
            }

            if (wordText != null)
            {
                wordText.text = word;
                wordText.gameObject.SetActive(false); // Başlangıçta yazıyı gizle
            }

            if (envelopeFrontObj != null)
            {
                envelopeFrontObj.SetActive(false);
            }

            isOpened = false;
            isSolved = false;
            isInteractable = false;
            transform.localScale = originalScale;
            
            // DOTween animasyonlarını sıfırla
            transform.DOKill();
            envelopeImage.DOKill();
            if (wordText != null) wordText.DOKill();
        }

        /// <summary>
        /// Zarfın tıklanabilir olup olmadığını ayarlar.
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            isInteractable = interactable;
        }

        /// <summary>
        /// Zarfın içindeki kelimeyi gösterir veya gizler. Animasyonları, renkleri ve ölçeği sıfırlar/günceller.
        /// </summary>
        public void RevealWord(bool reveal)
        {
            isOpened = reveal;
            
            // Önizleme veya pulse animasyonlarını durdur ve varsayılan renge döndür (Burada normale yani beyaza döner)
            envelopeImage.DOKill();
            envelopeImage.color = normalColor;
            
            if (paperRect != null)
            {
                paperRect.DOKill();
                paperRect.gameObject.SetActive(reveal);
                paperRect.anchoredPosition = Vector2.zero;
                paperRect.localScale = Vector3.one;
                paperRect.localRotation = Quaternion.identity;
                if (paperImage != null)
                {
                    paperImage.enabled = reveal;
                }
            }

            if (wordText != null)
            {
                wordText.DOKill();
                wordText.gameObject.SetActive(reveal);
                wordText.color = isSolved ? solvedTextColor : originalTextColor; // Çözüldüyse altın sarısı, değilse beyaz
                if (reveal)
                {
                    wordText.alpha = 1f;
                }
            }
            envelopeImage.sprite = reveal ? openSprite : closedSprite;

            if (envelopeFrontObj != null)
            {
                envelopeFrontObj.SetActive(reveal);
            }

            // Zarf açıldığında en-boy oranı farkını yatay ve dikeyde ayrı telafi etmek için ölçeğini güncelliyoruz
            if (reveal)
            {
                transform.localScale = new Vector3(
                    originalScale.x * openScaleXMultiplier,
                    originalScale.y * openScaleYMultiplier,
                    originalScale.z
                );
            }
            else
            {
                transform.localScale = originalScale;
            }
        }

        /// <summary>
        /// Karıştırma başlamadan önce zarfı ve kelimeyi hafif transparan ve parıldayan (pulse) önizleme moduna sokar.
        /// </summary>
        public void StartPreviewState(float fadeDuration)
        {
            isOpened = true;
            envelopeImage.sprite = closedSprite;
            
            // Zarfın gövdesini makul bir gri tona karartıyoruz (aşırı siyah olmaması ve detayların görünmesi için)
            envelopeImage.color = new Color(0.45f, 0.45f, 0.5f, 1f);

            // Zarf üzerinde nefes alma/pırpır efekti (Opaklık 0.5f - 1.0f arası)
            envelopeImage.DOFade(0.5f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);

            if (paperRect != null)
            {
                paperRect.DOKill();
                paperRect.gameObject.SetActive(true);
                paperRect.anchoredPosition = Vector2.zero;
                paperRect.localScale = Vector3.one;
                paperRect.localRotation = Quaternion.identity;
                if (paperImage != null)
                {
                    paperImage.enabled = false; // Kapalı zarfta siyah kağıt görünmesin
                }
            }

            if (wordText != null)
            {
                wordText.gameObject.SetActive(true);
                wordText.alpha = 0f;
                
                // Yazı rengini orijinal açık/beyaz haline geri getiriyoruz
                wordText.color = originalTextColor;
                
                // Yazının belirgin bir şekilde pırpır yapması efekti (0.8f - 1.0f)
                wordText.DOFade(0.8f, fadeDuration)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        wordText.DOFade(1.0f, 0.6f)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine);
                    });
            }
        }

        /// <summary>
        /// Zarfın kapalı görselden açık görsele DOTween kart çevirme animasyonu ile geçmesini sağlar.
        /// </summary>
        public void FlipOpen(float duration, Action onComplete = null)
        {
            if (isOpened)
            {
                onComplete?.Invoke();
                return;
            }

            isOpened = true;
            
            // Zarfı X ekseninde daraltıp (kartın arkası) sprite'ı değiştirerek geri açma (kartın önü)
            transform.DOScaleX(0f, duration * 0.5f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    envelopeImage.sprite = openSprite;
                    if (paperRect != null)
                    {
                        paperRect.gameObject.SetActive(true);
                        paperRect.anchoredPosition = Vector2.zero;
                        paperRect.localScale = Vector3.one;
                        paperRect.localRotation = Quaternion.identity;
                        if (paperImage != null)
                        {
                            paperImage.enabled = true; // Açık zarfta siyah kağıt görünsün
                        }
                    }
                    if (envelopeFrontObj != null)
                    {
                        envelopeFrontObj.SetActive(true);
                    }
                    if (wordText != null)
                    {
                        wordText.gameObject.SetActive(true);
                        wordText.alpha = 0f;
                        wordText.DOFade(1f, duration * 0.5f);
                    }
                    
                    // Açık haldeki ölçeğe (openScaleXMultiplier/openScaleYMultiplier) hem X hem de Y ekseninde büyüterek geri açıyoruz
                    Vector3 targetOpenScale = new Vector3(
                        originalScale.x * openScaleXMultiplier,
                        originalScale.y * openScaleYMultiplier,
                        originalScale.z
                    );
                    transform.DOScale(targetOpenScale, duration * 0.5f)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() => onComplete?.Invoke());
                });
        }

        /// <summary>
        /// Oyuncu yanlış tıkladığında kırmızı renk parlaması ve sarsıntı (shake) efekti uygular.
        /// </summary>
        public void PlayIncorrectFeedback(float duration = 0.5f)
        {
            isInteractable = false; // Efekt sırasında tıklamayı engelle
            
            // Sarsıntı efekti (Anchored position ile UI dostu sarsıntı)
            rectTransform.DOShakeAnchorPos(duration, 15f, 15, 90, false, true);

            // Renk uyarısı (Kırmızıya dönüp geri normal rengine gelme)
            envelopeImage.DOColor(incorrectColor, duration * 0.3f)
                .SetLoops(2, LoopType.Yoyo)
                .OnComplete(() =>
                {
                    envelopeImage.color = isOpened ? normalColor : normalColor;
                    isInteractable = true;
                });
        }

        /// <summary>
        /// Oyuncu doğru tıkladığında yeşil parlayarak kelimenin çözüldüğünü belirtir.
        /// Ayrıca içindeki siyah kağıdı havalandırarak (float) sihirli bir yıldız patlaması efekti oynatır.
        /// </summary>
        public void PlayCorrectFeedback(float duration = 0.5f, Action onComplete = null)
        {
            isSolved = true;
            isInteractable = false;

            // Zarf açılınca hedeflenen nihai boyut çarpanları
            Vector3 targetOpenScale = new Vector3(
                originalScale.x * openScaleXMultiplier,
                originalScale.y * openScaleYMultiplier,
                originalScale.z
            );

            // Zarf görselini açık yap ve hafif büyüterek tatlı bir bounce ile açılış animasyonu ver
            envelopeImage.sprite = openSprite;
            transform.DOScale(targetOpenScale * 1.1f, duration * 0.4f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    transform.DOScale(targetOpenScale, duration * 0.6f).SetEase(Ease.OutQuad);
                });

            // Sihirli yıldız patlaması (Sparkle particle burst) efekti tetikle
            SpawnMagicSparkles();

            // Siyah kağıdın (Paper) havalanması ve süzülmesi (Floating) animasyonu
            if (paperRect != null)
            {
                paperRect.DOKill();
                paperRect.gameObject.SetActive(true);
                paperRect.anchoredPosition = Vector2.zero;
                paperRect.transform.localPosition = new Vector3(paperRect.transform.localPosition.x, paperRect.transform.localPosition.y, 0f);
                paperRect.localScale = Vector3.one * 0.7f; // Zarfın içinden çıkarken hafif küçük başlar
                if (paperImage != null)
                {
                    paperImage.enabled = true; // Siyah kağıt görselini görünür yap
                }

                if (wordText != null)
                {
                    wordText.DOKill();
                    wordText.gameObject.SetActive(true);
                    wordText.transform.localPosition = new Vector3(wordText.transform.localPosition.x, wordText.transform.localPosition.y, 0f);
                    wordText.transform.SetAsLastSibling(); // Siyah kağıdın önünde kalmasını garantile
                    wordText.alpha = 1f;
                    wordText.color = solvedTextColor; // Rengini anında parlak altın sarısı yap
                }
                
                // Kağıdı dikeyde yukarı çıkart ve elastik bir hisle yerine oturt (Bounce)
                paperRect.DOAnchorPosY(115f, duration * 1.3f).SetEase(Ease.OutBack);
                paperRect.DOScale(Vector3.one * 1.15f, duration * 1.3f).SetEase(Ease.OutBack)
                    .OnComplete(() =>
                    {
                        // Havaya çıktıktan sonra sonsuz bir "süzülme/havada kalma" (float) döngüsü başlat
                        paperRect.DOAnchorPosY(123f, 1.5f)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine);

                        // Hafif beşik gibi sallanma döngüsü (Yalpalama)
                        paperRect.DOLocalRotate(new Vector3(0f, 0f, 1.8f), 1.7f)
                            .SetLoops(-1, LoopType.Yoyo)
                            .SetEase(Ease.InOutSine);
                    });
            }

            envelopeImage.DOColor(targetColor, duration * 0.5f)
                .OnComplete(() =>
                {
                    // Çözüldükten sonra zarfı karartıp yazıyı belirgin tutuyoruz (okunabilirliği artırmak için)
                    envelopeImage.DOColor(solvedColor, duration * 0.5f);
                    if (wordText != null)
                    {
                        wordText.DOColor(solvedTextColor, duration * 0.5f);
                    }
                    onComplete?.Invoke();
                });
        }

        /// <summary>
        /// Zarfın hedef kelime olduğunu belirtmek için yeşil vurgulama yapar.
        /// </summary>
        public void SetAsTarget(bool isTarget)
        {
            if (isSolved) return;

            if (isTarget)
            {
                // Hedef kelime olduğunda yeşil renkle vurgulanır ve hafifçe pırpır yapar (pulse)
                envelopeImage.color = targetColor;
                transform.DOScale(originalScale * 1.05f, 0.4f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine);
            }
            else
            {
                transform.DOKill();
                transform.localScale = originalScale;
                envelopeImage.color = normalColor;
            }
        }

        /// <summary>
        /// Zarfın kapalı görselden açık görsele DOTween kart çevirme animasyonu ile geçmesini sağlar.
        /// </summary>
        public void FlipClose(float duration, Action onComplete = null)
        {
            if (!isOpened)
            {
                onComplete?.Invoke();
                return;
            }

            isOpened = false;

            // Zarfı X ekseninde daraltıp (kartın önü) sprite'ı kapatarak geri açma (kartın arkası)
            transform.DOScaleX(0f, duration * 0.5f)
                .SetEase(Ease.InQuad)
                .OnComplete(() =>
                {
                    envelopeImage.sprite = closedSprite;
                    envelopeImage.color = normalColor;
                    if (envelopeFrontObj != null)
                    {
                        envelopeFrontObj.SetActive(false);
                    }
                    if (paperRect != null)
                    {
                        if (paperImage != null)
                        {
                            paperImage.enabled = false; // Kapatırken siyah kağıt görselini gizle
                        }
                        paperRect.gameObject.SetActive(false);
                    }
                    if (wordText != null)
                    {
                        wordText.gameObject.SetActive(false);
                    }

                    // Orijinal ölçeğine (normal boyuta) geri büyüterek kartı kapatıyoruz
                    transform.DOScale(originalScale, duration * 0.5f)
                        .SetEase(Ease.OutQuad)
                        .OnComplete(() => onComplete?.Invoke());
                });
        }

        /// <summary>
        /// Hedef gösterme (Hint) yeteneği tetiklendiğinde zarfı geçici olarak açıp kapatır.
        /// </summary>
        public void PlayHintAnimation(float showDuration)
        {
            bool wasInteractable = isInteractable;
            isInteractable = false;

            // Zarfı kart çevirme efektiyle aç
            FlipOpen(0.4f, () =>
            {
                // Belirlenen süre kadar açık beklet, sonra geri kapat
                DOVirtual.DelayedCall(showDuration, () =>
                {
                    FlipClose(0.4f, () =>
                    {
                        isInteractable = wasInteractable;
                    });
                });
            });
        }

        /// <summary>
        /// Tuzak Eleyici yeteneği tetiklendiğinde tuzağı karartıp kalıcı olarak deaktif eder.
        /// </summary>
        public void DisableTrap()
        {
            isSolved = true; // Tıklanmasını önlemek için çözüldü olarak işaretle
            isInteractable = false;

            // Zarfı grileştirip yarı saydam yaparak devre dışı bırakıyoruz
            envelopeImage.DOColor(new Color(0.25f, 0.25f, 0.3f, 0.6f), 0.5f);

            if (envelopeFrontObj != null)
            {
                envelopeFrontObj.SetActive(false);
            }

            if (paperRect != null)
            {
                if (paperImage != null)
                {
                    paperImage.enabled = false;
                }
                paperRect.gameObject.SetActive(false);
            }

            if (wordText != null)
            {
                wordText.text = string.Empty;
                wordText.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Havuza iade edilmeden önce zarfı tamamen sıfırlayan metot.
        /// </summary>
        public void ResetEnvelope()
        {
            transform.DOKill();
            envelopeImage.DOKill();
            
            if (envelopeFrontObj != null)
            {
                envelopeFrontObj.SetActive(false);
            }

            if (paperRect != null)
            {
                paperRect.DOKill();
                paperRect.anchoredPosition = Vector2.zero;
                paperRect.localScale = Vector3.one;
                paperRect.localRotation = Quaternion.identity;
                if (paperImage != null)
                {
                    paperImage.enabled = false; // Sıfırlarken siyah kağıt görselini gizle
                }
                paperRect.gameObject.SetActive(false);
            }

            if (wordText != null)
            {
                wordText.DOKill();
                wordText.text = string.Empty;
                wordText.color = Color.white; // varsayılan yazı rengi
            }
            envelopeImage.color = originalImageColor;
            transform.localScale = originalScale;
            isOpened = false;
            isSolved = false;
            isInteractable = false;
        }

        /// <summary>
        /// UI Tıklama algılayıcı interface uygulaması.
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isInteractable || isSolved) return;

            // GameManager üzerinden tıklamayı işle
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnEnvelopeClicked(this);
            }
        }

        /// <summary>
        /// Sihirli yıldız patlaması (sparkle burst) efekti oluşturur.
        /// TextMeshPro vektör karakterlerini kullanarak sıfır dosya bağımlılığıyla pürüzsüz ve performanslı parçacık efekti verir.
        /// </summary>
        private void SpawnMagicSparkles()
        {
            int particleCount = 8;
            for (int i = 0; i < particleCount; i++)
            {
                GameObject particleObj = new GameObject("MagicSparkleParticle");
                particleObj.transform.SetParent(this.transform, false);

                TextMeshProUGUI sparkleText = particleObj.AddComponent<TextMeshProUGUI>();
                sparkleText.text = UnityEngine.Random.Range(0, 2) == 0 ? "✦" : "★";
                sparkleText.fontSize = UnityEngine.Random.Range(20f, 32f);
                sparkleText.fontStyle = FontStyles.Bold;
                
                // Altın Sarısı veya Parlak Cam Göbeği (Magical cyan/gold) renk tonları
                sparkleText.color = UnityEngine.Random.Range(0, 2) == 0 
                    ? new Color(1f, 0.85f, 0.15f, 1f) 
                    : new Color(0f, 0.95f, 1f, 1f);
                    
                sparkleText.alignment = TextAlignmentOptions.Center;

                RectTransform pRect = particleObj.GetComponent<RectTransform>();
                // Zarfın ağız hizasından patlat
                pRect.anchoredPosition = new Vector2(0f, 30f);
                pRect.localScale = Vector3.one * 0.1f;

                // Dairesel fırlatma yönü hesabı
                float angle = i * (360f / particleCount) + UnityEngine.Random.Range(-15f, 15f);
                float radius = UnityEngine.Random.Range(100f, 175f);
                Vector2 targetPos = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad) * radius,
                    Mathf.Sin(angle * Mathf.Deg2Rad) * radius + 60f
                );

                float particleDuration = UnityEngine.Random.Range(0.6f, 0.9f);

                // DOTween ile fırlatma, büyüme, dönme ve yok olma animasyonları
                pRect.DOAnchorPos(targetPos, particleDuration).SetEase(Ease.OutCubic);
                pRect.DOScale(Vector3.one, particleDuration * 0.3f).SetEase(Ease.OutQuad).OnComplete(() =>
                {
                    pRect.DOScale(Vector3.zero, particleDuration * 0.7f).SetEase(Ease.InQuad);
                });
                
                pRect.DOLocalRotate(new Vector3(0f, 0f, UnityEngine.Random.Range(-240f, 240f)), particleDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad);
                sparkleText.DOFade(0f, particleDuration).SetEase(Ease.InCubic)
                    .OnComplete(() => Destroy(particleObj));
            }
        }
    }
}

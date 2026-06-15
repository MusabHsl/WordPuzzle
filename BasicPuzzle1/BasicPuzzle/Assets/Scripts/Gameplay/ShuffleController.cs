using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using BasicPuzzle.Core;

namespace BasicPuzzle.Gameplay
{
    public class ShuffleController : MonoBehaviour
    {
        /// <summary>
        /// Karıştırma işlemi tamamlandığında tetiklenecek event.
        /// </summary>
        public event Action OnShuffleComplete;

        private List<Envelope> activeEnvelopes;
        private int totalSwaps;
        private float swapDuration;
        private float arcHeight;
        private bool isShuffling = false;

        /// <summary>
        /// Karıştırma işlemini başlatır.
        /// </summary>
        public void StartShuffle(List<Envelope> envelopes, int shuffleCount, float duration, float height)
        {
            if (isShuffling) return;

            activeEnvelopes = envelopes;
            totalSwaps = shuffleCount;
            swapDuration = duration;
            arcHeight = height;

            if (activeEnvelopes == null || activeEnvelopes.Count < 2)
            {
                Debug.LogWarning("[ShuffleController] Karıştırmak için en az 2 zarf gereklidir!");
                OnShuffleComplete?.Invoke();
                return;
            }

            isShuffling = true;
            StartCoroutine(ShuffleRoutine());
        }

        /// <summary>
        /// Ardışık yer değiştirme hareketlerini yöneten Coroutine.
        /// </summary>
        private IEnumerator ShuffleRoutine()
        {
            int swapCount = 0;
            int lastIndexA = -1;
            int lastIndexB = -1;

            while (swapCount < totalSwaps)
            {
                // Rastgele iki zarf seç (Bir önceki yer değiştirme ile birebir aynı olmaması için kontrol)
                int indexA = UnityEngine.Random.Range(0, activeEnvelopes.Count);
                int indexB = UnityEngine.Random.Range(0, activeEnvelopes.Count);

                int attempts = 0;
                while (indexA == indexB || (activeEnvelopes.Count > 2 && ((indexA == lastIndexA && indexB == lastIndexB) || (indexA == lastIndexB && indexB == lastIndexA))))
                {
                    indexB = UnityEngine.Random.Range(0, activeEnvelopes.Count);
                    attempts++;
                    if (attempts > 50) break; // Sonsuz döngü önleyici güvenlik kilidi
                }

                lastIndexA = indexA;
                lastIndexB = indexB;

                Envelope envelopeA = activeEnvelopes[indexA];
                Envelope envelopeB = activeEnvelopes[indexB];

                // İkisinin de animasyonunun bitmesini bekleyeceğimiz bayrak
                bool swapCompleted = false;

                ExecuteSwap(envelopeA, envelopeB, () => {
                    swapCompleted = true;
                });

                // Mevcut swap hareketi bitene kadar bekle
                yield return new WaitUntil(() => swapCompleted);
                
                // Bir sonraki swap başlamadan önce gözü yormayan minik bir sönümlenme/dinlenme payı (bekleme süresiyle orantılı)
                // Bu sayede ani yön değişimlerindeki keskinlik kırılır ve hareketler daha yumuşak hissedilir.
                float currentDuration = swapDuration;
                if (GameManager.Instance != null && GameManager.Instance.IsSlowMotionActive)
                {
                    currentDuration = swapDuration * GameManager.Instance.SlowMotionMultiplier;
                }
                
                float restDuration = currentDuration * 0.12f; // %12 oranında dinlenme payı
                if (restDuration > 0.01f)
                {
                    yield return new WaitForSeconds(restDuration);
                }
                
                swapCount++;
            }

            isShuffling = false;
            OnShuffleComplete?.Invoke();
        }

        /// <summary>
        /// İki zarfı kavisli (yay) şeklinde yer değiştirir.
        /// </summary>
        private void ExecuteSwap(Envelope a, Envelope b, Action onComplete)
        {
            RectTransform rectA = a.RectTransform;
            RectTransform rectB = b.RectTransform;

            Vector2 posA = rectA.anchoredPosition;
            Vector2 posB = rectB.anchoredPosition;

            // Yavaş çekim (slow motion) aktif ise swap süresini çarp
            float currentDuration = swapDuration;
            if (GameManager.Instance != null && GameManager.Instance.IsSlowMotionActive)
            {
                currentDuration = swapDuration * GameManager.Instance.SlowMotionMultiplier;
            }

            // Karıştırma hızı çok yüksekken zarfların yukarı-aşağı çok sert ve keskin zıplamaması için 
            // kavis yüksekliğini swap süresine göre dinamik olarak ölçekliyoruz (odunsu hissi engeller).
            float scaledArcHeight = arcHeight * Mathf.Clamp(currentDuration / 0.7f, 0.25f, 1.0f);

            // Izgara (Grid) indekslerini yer değiştir
            int tempGridIndex = a.CurrentGridIndex;
            a.CurrentGridIndex = b.CurrentGridIndex;
            b.CurrentGridIndex = tempGridIndex;

            // Mesafe hesabı
            float diffX = Mathf.Abs(posA.x - posB.x);
            float diffY = Mathf.Abs(posA.y - posB.y);

            // DOTween Sequence oluşturuyoruz
            Sequence seqA = DOTween.Sequence();
            Sequence seqB = DOTween.Sequence();

            // Yumuşak ve keskin olmayan akıcı hareketler için Sine ease modellerini kullanıyoruz
            // Eğer zarflar yatayda daha uzaksa dikey kavis (Y ekseninde) yapalım
            if (diffX >= diffY)
            {
                // Zarf A için kavis: X ekseni doğrusal/yumuşak hareket ederken, Y ekseni tepe noktasına ulaşıp hedefe iner.
                seqA.Join(rectA.DOAnchorPosX(posB.x, currentDuration).SetEase(Ease.InOutSine));
                
                float midY_A = (posA.y + posB.y) * 0.5f + scaledArcHeight;
                seqA.Join(rectA.DOAnchorPosY(midY_A, currentDuration * 0.5f).SetEase(Ease.OutSine));
                seqA.Append(rectA.DOAnchorPosY(posB.y, currentDuration * 0.5f).SetEase(Ease.InSine));

                // Zarf B için ters kavis: X doğrusal/yumuşak, Y dip noktasına ulaşıp hedefe çıkar.
                seqB.Join(rectB.DOAnchorPosX(posA.x, currentDuration).SetEase(Ease.InOutSine));
                
                float midY_B = (posA.y + posB.y) * 0.5f - scaledArcHeight;
                seqB.Join(rectB.DOAnchorPosY(midY_B, currentDuration * 0.5f).SetEase(Ease.OutSine));
                seqB.Append(rectB.DOAnchorPosY(posA.y, currentDuration * 0.5f).SetEase(Ease.InSine));
            }
            else
            {
                // Eğer dikeyde daha uzaklarsa yatay kavis (X ekseninde) yapalım
                seqA.Join(rectA.DOAnchorPosY(posB.y, currentDuration).SetEase(Ease.InOutSine));
                
                float midX_A = (posA.x + posB.x) * 0.5f + scaledArcHeight;
                seqA.Join(rectA.DOAnchorPosX(midX_A, currentDuration * 0.5f).SetEase(Ease.OutSine));
                seqA.Append(rectA.DOAnchorPosX(posB.x, currentDuration * 0.5f).SetEase(Ease.InSine));

                seqB.Join(rectB.DOAnchorPosY(posA.y, currentDuration).SetEase(Ease.InOutSine));
                
                float midX_B = (posA.x + posB.x) * 0.5f - scaledArcHeight;
                seqB.Join(rectB.DOAnchorPosX(midX_B, currentDuration * 0.5f).SetEase(Ease.OutSine));
                seqB.Append(rectB.DOAnchorPosX(posA.x, currentDuration * 0.5f).SetEase(Ease.InSine));
            }

            // Animasyonlar tamamlandığında callback'i çalıştır
            seqA.Play();
            seqB.Play().OnComplete(() => onComplete?.Invoke());
        }
    }
}

using UnityEngine;
using System;
using System.Linq;

namespace BasicPuzzle.Data
{
    public enum EnvelopeLayoutType
    {
        Grid,
        Pyramid,
        InversePyramid,
        Diamond,
        Circle
    }

    [CreateAssetMenu(fileName = "NewLevelData", menuName = "BasicPuzzle/LevelData")]
    public class LevelData : ScriptableObject
    {
        [Header("Sentence Settings")]
        [Tooltip("Bölümde kullanılacak cümle (Örn: 'Bugün hava çok güzel')")]
        [TextArea(3, 5)]
        [SerializeField] private string sentence;

        [Header("Shuffle Settings")]
        [Tooltip("Bölüm başında kelimelerin oyuncuya açık gösterileceği süre (saniye)")]
        [SerializeField] private float initialShowDuration = 2f;

        [Tooltip("Zarfların kaç kez yer değiştireceği")]
        [SerializeField] private int shuffleCount = 10;

        [Tooltip("Tek bir yer değiştirme (swap) hareketinin süresi")]
        [SerializeField] private float swapDuration = 0.4f;

        [Tooltip("Zarfların kavis yüksekliği (Shell game kavis derinliği)")]
        [SerializeField] private float arcHeight = 150f;

        [Header("Trap Settings")]
        [Tooltip("Bölümde kullanılacak tuzak zarf sayısı (Aşama 3 ve sonrasında oyuncuyu şaşırtmak için kullanılır)")]
        [SerializeField] private int trapCount = 0;

        [Header("Difficulty Modifiers")]
        [Tooltip("Gösterme aşamasında kelimelerin soldan sağa sırayla değil, rastgele karışık dizilmesini sağlar.")]
        [SerializeField] private bool randomizeInitialPositions = false;

        [Tooltip("Zarfların diziliş geometrisi (Grid, Pyramid, InversePyramid, Diamond, Circle)")]
        [SerializeField] private EnvelopeLayoutType layoutType = EnvelopeLayoutType.Grid;

        [Header("Custom Layout Settings")]
        [Tooltip("Eğer el yapımı özel bir dizilim istiyorsanız buraya koordinat girebilirsiniz (Boş bırakırsanız otomatik Grid şeklinde dizilir)")]
        [SerializeField] private Vector2[] customPositions;

        // Getters
        public string Sentence => sentence;
        public float InitialShowDuration => initialShowDuration;
        public int ShuffleCount => shuffleCount;
        public float SwapDuration => swapDuration;
        public float ArcHeight => arcHeight;
        public int TrapCount => trapCount;
        public bool RandomizeInitialPositions => randomizeInitialPositions;
        public EnvelopeLayoutType LayoutType => layoutType;
        public Vector2[] CustomPositions => customPositions;

        /// <summary>
        /// Çalışma zamanında seviye verilerini dinamik doldurmak için kullanılan ilklendirme metodu.
        /// </summary>
        public void Initialize(string sentence, float initialShowDuration, int shuffleCount, float swapDuration, float arcHeight = 150f, int trapCount = 0, bool randomizeInitialPositions = false, EnvelopeLayoutType layoutType = EnvelopeLayoutType.Grid)
        {
            this.sentence = sentence;
            this.initialShowDuration = initialShowDuration;
            this.shuffleCount = shuffleCount;
            this.swapDuration = swapDuration;
            this.arcHeight = arcHeight;
            this.trapCount = trapCount;
            this.randomizeInitialPositions = randomizeInitialPositions;
            this.layoutType = layoutType;
            this.customPositions = null;
        }

        /// <summary>
        /// Cümleyi kelimelere böler ve noktalama işaretlerinden temizler.
        /// </summary>
        public string[] GetWords()
        {
            if (string.IsNullOrEmpty(sentence))
                return Array.Empty<string>();

            // Cümleyi boşluklardan ayırıyoruz
            string[] rawWords = sentence.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Her kelimenin sonundaki/başındaki noktalama işaretlerini temizliyoruz
            char[] charsToTrim = { '.', ',', '!', '?', ':', ';', '"', '(', ')' };
            return rawWords.Select(word => word.Trim(charsToTrim)).ToArray();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using BasicPuzzle.Gameplay;

namespace BasicPuzzle.Core
{
    public class ObjectPooler : MonoBehaviour
    {
        [Header("Pool Settings")]
        [Tooltip("Havuzlanacak Zarf Prefab'ı")]
        [SerializeField] private GameObject envelopePrefab;

        [Tooltip("Başlangıçta oluşturulacak nesne sayısı")]
        [SerializeField] private int initialPoolSize = 6;

        [Tooltip("Zarfların ekleneceği UI Panel (Canvas/Grid)")]
        [SerializeField] private Transform container;

        public Transform Container => container;

        private List<Envelope> pooledEnvelopes = new List<Envelope>();

        private void Awake()
        {
            InitializePool();
        }

        /// <summary>
        /// Belirlenen başlangıç boyutu kadar nesneyi oluşturur ve pasif olarak havuza ekler.
        /// </summary>
        private void InitializePool()
        {
            if (envelopePrefab == null)
            {
                Debug.LogError("[ObjectPooler] Zarf prefab'ı atanmadı!");
                return;
            }

            if (container == null)
            {
                container = this.transform;
            }

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewEnvelopeInstance();
            }
        }

        /// <summary>
        /// Yeni bir zarf örneği oluşturur, pasifleştirir ve havuz listesine ekler.
        /// </summary>
        private Envelope CreateNewEnvelopeInstance()
        {
            GameObject obj = Instantiate(envelopePrefab, container);
            obj.SetActive(false);
            
            Envelope envelope = obj.GetComponent<Envelope>();
            if (envelope == null)
            {
                Debug.LogError("[ObjectPooler] Prefab üzerinde 'Envelope' componenti bulunamadı!");
                Destroy(obj);
                return null;
            }

            pooledEnvelopes.Add(envelope);
            return envelope;
        }

        /// <summary>
        /// Havuzdan aktif olmayan bir zarf döner. Eğer hepsi aktifse yeni bir tane oluşturup döner.
        /// </summary>
        public Envelope GetPooledObject()
        {
            // Pasif olan ilk zarfı bul
            for (int i = 0; i < pooledEnvelopes.Count; i++)
            {
                if (!pooledEnvelopes[i].gameObject.activeInHierarchy)
                {
                    pooledEnvelopes[i].gameObject.SetActive(true);
                    return pooledEnvelopes[i];
                }
            }

            // Eğer hepsi aktifse, havuzu dinamik olarak büyüt
            Envelope newEnvelope = CreateNewEnvelopeInstance();
            if (newEnvelope != null)
            {
                newEnvelope.gameObject.SetActive(true);
            }
            return newEnvelope;
        }

        /// <summary>
        /// Sahnedeki tüm zarfları pasif hale getirip havuza geri döndürür.
        /// </summary>
        public void ReturnAllToPool()
        {
            foreach (var envelope in pooledEnvelopes)
            {
                if (envelope != null)
                {
                    envelope.ResetEnvelope();
                    envelope.gameObject.SetActive(false);
                }
            }
        }
    }
}

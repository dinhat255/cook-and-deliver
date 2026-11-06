using UnityEngine;
using UnityEngine.Pool;

namespace CookAndDeliver.Cooking.Core.Services
{
    // Implementation of IPoolService using Unity's built-in ObjectPool<T>
    public class PoolService : MonoBehaviour, IPoolService
    {
        [Header("Prefabs")]
        public GameObject flyPrefab;
        public GameObject toastPrefab;
        public GameObject puffPrefab;
        public GameObject sparkPrefab;

        ObjectPool<GameObject> flyPool;
        ObjectPool<GameObject> toastPool;
        ObjectPool<GameObject> puffPool;
        ObjectPool<GameObject> sparkPool;

        void Awake()
        {
            CookingServiceLocator.Register<IPoolService>(this);
            InitializePools();
        }

        void OnDestroy()
        {
            CookingServiceLocator.Unregister<IPoolService>();
            DisposePools();
        }

        void InitializePools()
        {
            flyPool = CreatePool(flyPrefab, "FlyArc_Pooled", 10, 30);
            toastPool = CreatePool(toastPrefab, "Toast_Pooled", 5, 20);
            puffPool = CreatePool(puffPrefab, "Puff_Pooled", 15, 50);
            sparkPool = CreatePool(sparkPrefab, "Spark_Pooled", 20, 80);
        }

        void DisposePools()
        {
            flyPool?.Clear();
            toastPool?.Clear();
            puffPool?.Clear();
            sparkPool?.Clear();
        }

        ObjectPool<GameObject> CreatePool(GameObject prefab, string defaultName, int defaultCapacity, int maxSize)
        {
            return new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var instance = Instantiate(prefab, transform);
                    instance.name = defaultName;
                    instance.SetActive(false);
                    return instance;
                },
                actionOnGet: go =>
                {
                    if (go != null) go.SetActive(true);
                },
                actionOnRelease: go =>
                {
                    if (go != null)
                    {
                        go.SetActive(false);
                        go.transform.SetParent(transform, false);
                    }
                },
                actionOnDestroy: go =>
                {
                    if (go != null) Destroy(go);
                },
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize
            );
        }

        public GameObject SpawnFlyArc(Transform parent)
        {
            var go = flyPool.Get();
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public void ReturnFlyArc(GameObject go)
        {
            if (go != null) flyPool.Release(go);
        }

        public GameObject SpawnToast(Transform parent)
        {
            var go = toastPool.Get();
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public void ReturnToast(GameObject go)
        {
            if (go != null) toastPool.Release(go);
        }

        public GameObject SpawnPuff(Transform parent)
        {
            var go = puffPool.Get();
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public void ReturnPuff(GameObject go)
        {
            if (go != null) puffPool.Release(go);
        }

        public GameObject SpawnSpark(Transform parent)
        {
            var go = sparkPool.Get();
            if (parent != null) go.transform.SetParent(parent, false);
            return go;
        }

        public void ReturnSpark(GameObject go)
        {
            if (go != null) sparkPool.Release(go);
        }
    }
}

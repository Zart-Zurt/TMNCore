using System.Collections.Generic;
using Sirenix.OdinInspector;
using TMNLibrary.Singleton;
using UnityEngine;

namespace Core.Pooling
{
    [DefaultExecutionOrder(-90)]
    [AddComponentMenu("Core/Pool Manager")]
    public class PoolManager : Singleton<PoolManager>
    {
        private readonly Dictionary<int, int> _instanceToPoolKey = new();
        private readonly Dictionary<int, Queue<GameObject>> _poolDictionary = new();
        private readonly Dictionary<int, GameObject> _prefabLookup = new();
        private readonly Dictionary<int, IPoolable[]> _poolableCache = new();

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null || count <= 0)
            {
                return;
            }

            var poolKey = prefab.GetInstanceID();
            if (!_poolDictionary.TryGetValue(poolKey, out var queue))
            {
                queue = new Queue<GameObject>(count);
                _poolDictionary[poolKey] = queue;
                _prefabLookup[poolKey] = prefab;
            }

            while (queue.Count < count)
            {
                var instance = Instantiate(prefab, transform);
                instance.SetActive(false);

                var instanceId = instance.GetInstanceID();
                _instanceToPoolKey[instanceId] = poolKey;
                _poolableCache[instanceId] = instance.GetComponentsInChildren<IPoolable>(true);

                queue.Enqueue(instance);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            if (prefab == null)
            {
                return null;
            }

            var poolKey = prefab.GetInstanceID();

            if (!_poolDictionary.TryGetValue(poolKey, out var queue))
            {
                queue = new Queue<GameObject>();
                _poolDictionary[poolKey] = queue;
                _prefabLookup[poolKey] = prefab;
            }

            GameObject instance = null;

            while (queue.Count > 0 && instance == null)
            {
                instance = queue.Dequeue();
            }

            int instanceId;
            if (instance == null)
            {
                instance = Instantiate(prefab);
                instanceId = instance.GetInstanceID();
                _instanceToPoolKey[instanceId] = poolKey;
                _poolableCache[instanceId] = instance.GetComponentsInChildren<IPoolable>(true);
            }
            else
            {
                instanceId = instance.GetInstanceID();
                _instanceToPoolKey[instanceId] = poolKey;
                if (!_poolableCache.ContainsKey(instanceId))
                {
                    _poolableCache[instanceId] = instance.GetComponentsInChildren<IPoolable>(true);
                }
            }

            instance.transform.SetPositionAndRotation(position, rotation);
            instance.transform.SetParent(parent);
            instance.SetActive(true);

            if (_poolableCache.TryGetValue(instanceId, out var poolables))
            {
                for (var i = 0; i < poolables.Length; i++)
                {
                    poolables[i]?.OnSpawnFromPool();
                }
            }

            return instance;
        }

        public void Despawn(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            var instanceId = instance.GetInstanceID();
            if (_instanceToPoolKey.TryGetValue(instanceId, out var poolKey))
            {
                if (!_poolDictionary.TryGetValue(poolKey, out var queue))
                {
                    queue = new Queue<GameObject>();
                    _poolDictionary[poolKey] = queue;
                }

                if (!_poolableCache.TryGetValue(instanceId, out var poolables))
                {
                    poolables = instance.GetComponentsInChildren<IPoolable>(true);
                    _poolableCache[instanceId] = poolables;
                }

                for (var i = 0; i < poolables.Length; i++)
                {
                    poolables[i]?.OnReturnToPool();
                }

                instance.SetActive(false);
                instance.transform.SetParent(transform);
                queue.Enqueue(instance);
            }
            else
            {
                Destroy(instance);
            }
        }

        public void Despawn(GameObject instance, GameObject prefab)
        {
            if (instance == null)
            {
                return;
            }

            if (prefab != null)
            {
                var poolKey = prefab.GetInstanceID();
                var instanceId = instance.GetInstanceID();
                _instanceToPoolKey[instanceId] = poolKey;

                if (!_poolDictionary.ContainsKey(poolKey))
                {
                    _poolDictionary[poolKey] = new Queue<GameObject>();
                    _prefabLookup[poolKey] = prefab;
                }

                if (!_poolableCache.ContainsKey(instanceId))
                {
                    _poolableCache[instanceId] = instance.GetComponentsInChildren<IPoolable>(true);
                }
            }

            Despawn(instance);
        }
    }
}


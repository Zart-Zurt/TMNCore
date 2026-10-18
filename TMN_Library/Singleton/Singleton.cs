using UnityEngine;

namespace TMNLibrary.Singleton
{
    public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static readonly object _lock = new();

        public static bool HasInstance => _instance != null;

        public static T Instance
        {
            get
            {
                if (_instance != null)
                {
                    return _instance;
                }

                lock (_lock)
                {
                    if (_instance != null)
                    {
                        return _instance;
                    }

                    _instance = FindAnyObjectByType<T>();
                    if (_instance != null && Application.isPlaying)
                    {
                        _instance.transform.SetParent(null);
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }

                return _instance;
            }
        }

        protected virtual void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this as T;

            if (Application.isPlaying)
            {
                transform.SetParent(null);
                DontDestroyOnLoad(gameObject);

                const string suffix = " (DontDestroyOnLoad)";
                if (!name.EndsWith(suffix))
                {
                    name += suffix;
                }
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
    }
}
using UnityEngine;

namespace Utility.Singletons
{
    public class Singleton<T> : MonoBehaviour where T : Component
    {
        protected static T instance;

        public static T TryGetInstance() => instance;

        public static T Instance
        {
            get
            {
                if (instance) return instance;

                instance = FindAnyObjectByType(typeof(T)) as T;
                if (instance) return instance;

                var gameObject = new GameObject(typeof(T).Name + "Auto Generated.");

                return instance = gameObject.AddComponent<T>();
            }
        }

        /// <summary>
        /// Make sure you call Base.Awake().
        /// </summary>
        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying) return;

            instance = this as T;
        }
    }

    public class PersistentSingleton<T> : MonoBehaviour where T : Component
    {
        //Don't destroy on load only works on root objects.
        public bool autoUnparentOnAwake = true;

        protected static T instance;
        public static T TryGetInstance() => instance;

        public static T Instance
        {
            get
            {
                if (instance) return instance;

                instance = FindAnyObjectByType(typeof(T)) as T;
                if (instance) return instance;

                var gameObject = new GameObject(typeof(T).Name + "Auto Generated.");
                instance = gameObject.AddComponent<T>();
                return instance;
            }
        }

        /// <summary>
        /// Make sure you call Base.Awake().
        /// </summary>
        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying) return;

            if (autoUnparentOnAwake)
            {
                transform.SetParent(null);
            }

            if (instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this as T;
            DontDestroyOnLoad(gameObject);
        }
    }

    public class RegulatingSingleton<T> : MonoBehaviour where T : Component
    {
        protected static T instance;

        public static bool hasInstance = instance != null;

        private float InitializedTime { get; set; } = 0.0f;

        public static T Instance
        {
            get
            {
                if (instance != null) return instance;

                instance = FindAnyObjectByType(typeof(T)) as T;
                if (instance != null) return instance;

                var gameObject = new GameObject(typeof(T).Name + "Auto Generated.")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                instance = gameObject.AddComponent<T>();
                return instance;
            }
        }

        /// <summary>
        /// Make sure you call Base.Awake().
        /// </summary>
        protected virtual void Awake()
        {
            InitializeSingleton();
        }

        protected virtual void InitializeSingleton()
        {
            if (!Application.isPlaying) return;

            InitializedTime = Time.time;
            DontDestroyOnLoad(gameObject);

            var oldObjects = FindObjectsByType<T>(FindObjectsSortMode.None);

            foreach (var obj in oldObjects)
            {
                if (obj.GetComponent<RegulatingSingleton<T>>().InitializedTime < InitializedTime)
                {
                    Destroy(obj.gameObject);
                }
            }

            if (instance == null)
            {
                instance = this as T;
            }
        }
    }
}
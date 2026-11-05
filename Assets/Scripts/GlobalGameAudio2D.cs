using UnityEngine;
using UnityEngine.SceneManagement;

namespace YearEnd
{
         public sealed class GlobalGameAudio2D : MonoBehaviour
    {
        private static GlobalGameAudio2D instance;

        [Range(0f, 1f)] public float bgmVolume = 0.22f;
        [Range(0f, 1f)] public float footstepVolume = 0.5f;

        [Range(0.001f, 0.05f)] public float minimumMovementPerFrame = 0.004f;

        private AudioSource bgmSource;
        private AudioSource footstepSource;
        private AudioClip footstepClip;
        private PlayerMovement2D player;
        private Vector3 previousPosition;
        private float timeUntilNextFootstep;
        private bool wasWalking;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CreateOnce()
        {
            if (instance != null) return;
            GameObject obj = new GameObject("GlobalGameAudio2D [persistent]");
            obj.AddComponent<GlobalGameAudio2D>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            bgmSource = Create2DSource("Background Music", true, bgmVolume);
            footstepSource = Create2DSource("Footsteps", false, footstepVolume);
            SceneManager.sceneLoaded += OnSceneLoaded;

            AudioClip bgm = Resources.Load<AudioClip>("Audio/BGM/ExploreTheme");
            footstepClip = Resources.Load<AudioClip>("Audio/SFX/Footstep");

            if (bgm != null)
            {
                bgmSource.clip = bgm;
                bgmSource.Play();
            }
            else Debug.LogWarning("找不到 BGM：請放到 Assets/Resources/Audio/BGM/ExploreTheme.wav 或 .ogg");

            if (footstepClip == null)
                Debug.LogWarning("找不到腳步音效：請放到 Assets/Resources/Audio/SFX/Footstep.wav");
        }

        private AudioSource Create2DSource(string sourceName, bool shouldLoop, float volume)
        {
            GameObject child = new GameObject(sourceName);
            child.transform.SetParent(transform, false);
            AudioSource source = child.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = shouldLoop;
            source.spatialBlend = 0f;  
            source.volume = volume;
            return source;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            player = FindFirstObjectByType<PlayerMovement2D>();
            if (player != null) previousPosition = player.transform.position;
            StopFootsteps();
        }

        private void FixedUpdate()
        {
            if (bgmSource != null) bgmSource.volume = bgmVolume;
            if (footstepSource != null) footstepSource.volume = footstepVolume;

            if (player == null)
            {
                player = FindFirstObjectByType<PlayerMovement2D>();
                if (player != null) previousPosition = player.transform.position;
                StopFootsteps();
                return;
            }

            Vector3 position = player.transform.position;
            float movedDistance = Vector2.Distance(position, previousPosition);
            previousPosition = position;

            GameSession2D session = GameSession2D.Instance;
            bool gameActive = session != null && session.IsWorldActive;
            bool keyHeld = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A)
                || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D)
                || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow)
                || Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow);

            bool walking = gameActive && keyHeld && movedDistance >= minimumMovementPerFrame;
            if (!walking || footstepClip == null)
            {
                StopFootsteps();
                return;
            }

            float fps = Mathf.Max(1f, player.walkFramesPerSecond);
            float secondsPerStep = 2f / fps;  
            if (!wasWalking) timeUntilNextFootstep = 0f;
            wasWalking = true;

            timeUntilNextFootstep -= Time.fixedDeltaTime;
            if (timeUntilNextFootstep <= 0f)
            {
                footstepSource.PlayOneShot(footstepClip);
                timeUntilNextFootstep += secondsPerStep;
                if (timeUntilNextFootstep < 0f)
                    timeUntilNextFootstep = secondsPerStep;
            }
        }

        private void StopFootsteps()
        {
            if (wasWalking && footstepSource != null) footstepSource.Stop();
            wasWalking = false;
            timeUntilNextFootstep = 0f;
        }

        private void OnDestroy()
        {
            if (instance != this) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }
}

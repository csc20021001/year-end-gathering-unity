using UnityEngine;

namespace YearEnd
{
    public sealed class PlayerMovement2D : MonoBehaviour
    {
        public float moveSpeed = 3.0f;

        public Sprite front, back, left, right;

        public Sprite idleDown, idleUp, idleLeft, idleRight;

        public Sprite[] walkDown = new Sprite[0];
        public Sprite[] walkUp = new Sprite[0];
        public Sprite[] walkLeft = new Sprite[0];
        public Sprite[] walkRight = new Sprite[0];

        [Range(1f, 16f)]
        public float walkFramesPerSecond = 7f;

        private enum Facing { Down, Up, Left, Right }
        private Facing facing = Facing.Down;
        private Rigidbody2D rb;
        private SpriteRenderer spriteRenderer;
        private Vector2 movement;
        private float frameElapsed;
        private int frameIndex;
        private bool wasMoving;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void Start()
        {
            ShowFrame(false);
        }

        private void Update()
        {
            GameSession2D session = GameSession2D.Instance;
            if (session == null || !session.IsWorldActive)
            {
                movement = Vector2.zero;
                frameElapsed = 0f;
                frameIndex = 0;
                wasMoving = false;
                ShowFrame(false);
                return;
            }

            movement = new Vector2(
                (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
                (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow) ? 1 : 0)
                - (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow) ? 1 : 0)
            ).normalized;

            bool moving = movement.sqrMagnitude > 0.01f;
            if (moving)
            {
                Facing nextFacing;
                if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
                    nextFacing = movement.x < 0 ? Facing.Left : Facing.Right;
                else
                    nextFacing = movement.y > 0 ? Facing.Up : Facing.Down;

                if (!wasMoving || nextFacing != facing)
                {
                    facing = nextFacing;
                    frameIndex = 0;
                    frameElapsed = 0f;
                }
                else
                {
                    frameElapsed += Time.deltaTime;
                    float frameSeconds = 1f / Mathf.Max(1f, walkFramesPerSecond);
                    if (frameElapsed >= frameSeconds)
                    {
                        int stepCount = Mathf.FloorToInt(frameElapsed / frameSeconds);
                        frameIndex += stepCount;
                        frameElapsed %= frameSeconds;
                    }
                }
            }
            else
            {
                frameElapsed = 0f;
                frameIndex = 0;
            }

            wasMoving = moving;
            ShowFrame(moving);
        }

        private Sprite IdleSprite()
        {
            switch (facing)
            {
                case Facing.Up: return idleUp != null ? idleUp : back;
                case Facing.Left: return idleLeft != null ? idleLeft : left;
                case Facing.Right: return idleRight != null ? idleRight : right;
                default: return idleDown != null ? idleDown : front;
            }
        }

        private Sprite[] WalkFrames()
        {
            switch (facing)
            {
                case Facing.Up: return walkUp;
                case Facing.Left: return walkLeft;
                case Facing.Right: return walkRight;
                default: return walkDown;
            }
        }

        private void ShowFrame(bool moving)
        {
            if (spriteRenderer == null) return;
            Sprite selected = null;
            if (moving)
            {
                Sprite[] frames = WalkFrames();
                if (frames != null && frames.Length > 0)
                    selected = frames[frameIndex % frames.Length];
            }

            if (selected == null) selected = IdleSprite();
            if (selected != null) spriteRenderer.sprite = selected;
            spriteRenderer.flipX = false;  
        }

        private void FixedUpdate()
        {
            if (rb != null)
                rb.linearVelocity = movement * moveSpeed;
        }

        private void OnDisable()
        {
            movement = Vector2.zero;
            wasMoving = false;
            frameElapsed = 0f;
            frameIndex = 0;
            if (rb != null) rb.linearVelocity = Vector2.zero;
            ShowFrame(false);
        }
    }
}

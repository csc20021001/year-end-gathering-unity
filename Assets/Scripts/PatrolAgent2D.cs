using UnityEngine;

namespace YearEnd
{
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public sealed class PatrolAgent2D : MonoBehaviour
    {
        [Min(0.1f)] public float speed = 1.2f;
        [Min(0.2f)] public float minTurnSeconds = 1.5f;
        [Min(0.2f)] public float maxTurnSeconds = 3.5f;

        public LayerMask obstacleLayers;
        [Min(0.05f)] public float probeRadius = 0.26f;
        [Min(0.1f)] public float lookAhead = 0.65f;

        [Min(0.1f)] public float hitRadius = 0.65f;

        private Rigidbody2D rb;
        private Vector2 direction;
        private float nextTurnTime;
        private Vector2 lastPosition;
        private float stuckSeconds;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            CircleCollider2D hitbox = GetComponent<CircleCollider2D>();
            hitbox.isTrigger = false;

            lastPosition = rb.position;
            ChooseDirection();
        }

        private bool WorldIsActive()
        {
            GameSession2D s = GameSession2D.Instance;
            return s != null && s.IsWorldActive;
        }

        private bool IsBlocked(Vector2 dir, float distance)
        {
            if (obstacleLayers.value == 0) return false;
            RaycastHit2D hit = Physics2D.CircleCast(
                rb.position, probeRadius, dir, distance, obstacleLayers);
            return hit.collider != null;
        }

        private void ChooseDirection()
        {
            if (rb == null) return;

            for (int i = 0; i < 16; i++)
            {
                float angle = Random.Range(0f, Mathf.PI * 2f);
                Vector2 candidate = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                if (!IsBlocked(candidate, lookAhead + 0.15f))
                {
                    direction = candidate;
                    nextTurnTime = Time.time + Random.Range(
                        minTurnSeconds, Mathf.Max(minTurnSeconds, maxTurnSeconds));
                    stuckSeconds = 0f;
                    return;
                }
            }

            direction = -direction;
            nextTurnTime = Time.time + 0.3f;
        }

        private void FixedUpdate()
        {
            if (!WorldIsActive())
            {
                rb.linearVelocity = Vector2.zero;
                lastPosition = rb.position;
                stuckSeconds = 0f;
                return;
            }

            if (Time.time >= nextTurnTime ||
                IsBlocked(direction, lookAhead + speed * Time.fixedDeltaTime))
            {
                ChooseDirection();
            }

            if (Vector2.Distance(rb.position, lastPosition) < 0.008f)
                stuckSeconds += Time.fixedDeltaTime;
            else
                stuckSeconds = 0f;

            if (stuckSeconds > 0.6f)
                ChooseDirection();

            rb.linearVelocity = direction * speed;
            lastPosition = rb.position;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            int otherLayer = collision.gameObject.layer;
            if ((obstacleLayers.value & (1 << otherLayer)) != 0)
                ChooseDirection();
        }


        private void OnCollisionStay2D(Collision2D collision)
        {
            GameSession2D s = GameSession2D.Instance;
            SceneWorld2D world = SceneWorld2D.Active;

            if (s == null || !s.IsWorldActive ||
                world == null || world.Player == null)
            {
                return;
            }

            Transform other = collision.transform;

            if (other != world.Player &&
                !other.IsChildOf(world.Player))
            {
                return;
            }

            if (s.State.Damage())
            {
                s.Notify(
                    "碰到巡邏人物，生命值 -20！",
                    3f
                );

                Debug.Log("玩家受到敵人傷害，目前 HP：" + s.State.Hp);
            }
        }


        private void OnDisable()
        {
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
    }
}

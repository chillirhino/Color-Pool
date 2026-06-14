using UnityEngine;

namespace ColorPool
{
    /// <summary>
    /// A ball on the table. The cue ball uses <see cref="colorIndex"/> = -1; colored
    /// balls use 0..9. Movement is constrained to the XZ plane and decays each
    /// FixedUpdate to match the prototype's per-frame friction.
    /// </summary>
    [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
    public class Ball : MonoBehaviour
    {
        public int colorIndex = -1;                       // -1 = cue
        public float radius = Tuning.DefaultBallRadius;

        Rigidbody rb;

        public Vector3 PrevPos { get; private set; }
        public Rigidbody Body => rb;
        public float Speed => rb != null ? rb.linearVelocity.magnitude : 0f;
        public bool IsStopped => Speed < GameSettings.I.stopSpeed;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearDamping = 0f;
            rb.angularDamping = 0.05f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Crisp, robust contacts so balls rebound off rails even when smashed by a much
            // heavier cue: more solver iterations, never sleep (always collidable), and a
            // capped depenetration speed so deep overlaps don't fling balls violently.
            rb.sleepThreshold = 0f;
            rb.solverIterations = 20;
            rb.solverVelocityIterations = 12;
            rb.maxDepenetrationVelocity = 3f;
            // Stay on the plane; freeze vertical spin (rolling about X/Z stays free).
            rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationY;
            ApplyRadius();
            PrevPos = transform.position;
        }

        /// <summary>Apply <see cref="radius"/> to scale, collider, mass, and rest height.</summary>
        public void ApplyRadius()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            transform.localScale = Vector3.one * (radius * 2f); // unit sphere has radius 0.5
            // Equal mass for every ball: a much heavier cue would trap light balls against the
            // rails (it can't recoil to give them room to rebound). Hit strength comes from the
            // cue's speed plus the impact boost in OnCollisionEnter, not from cue weight.
            rb.mass = 1f;
            Vector3 p = transform.position;
            p.y = radius;
            transform.position = p;
        }

        // The cue gives colored balls an extra speed kick on impact ("hit power").
        void OnCollisionEnter(Collision c)
        {
            if (colorIndex >= 0) return;                 // only the cue
            var otherRb = c.rigidbody;
            if (otherRb == null) return;
            var other = otherRb.GetComponent<Ball>();
            if (other == null || other.colorIndex < 0) return;
            float boost = GameSettings.I.cueHitPower;
            if (boost <= 1f) return;
            Vector3 v = otherRb.linearVelocity; v.y = 0f;
            v *= boost;
            float cap = GameSettings.I.launchPower;      // never faster than a full cue launch
            if (v.magnitude > cap) v = v.normalized * cap;
            otherRb.linearVelocity = v;
        }

        /// <summary>Launch in a horizontal direction at the given world speed (m/s).</summary>
        public void Launch(Vector3 dir, float speed)
        {
            dir.y = 0f;
            rb.linearVelocity = dir.normalized * speed;
        }

        void FixedUpdate()
        {
            var s = GameSettings.I;
            Vector3 v = rb.linearVelocity;
            v.y = 0f;
            v *= Mathf.Pow(s.friction, Time.fixedDeltaTime * 60f);
            if (v.magnitude < s.stopSpeed) v = Vector3.zero;
            rb.linearVelocity = v;

            // Colored balls paint the matching region they roll over (Regular mode).
            if (colorIndex >= 0 && v.sqrMagnitude > 0f && PaintSurface.Instance != null)
                PaintSurface.Instance.TryPaintAt(colorIndex, transform.position);

            PrevPos = transform.position;
        }
    }
}

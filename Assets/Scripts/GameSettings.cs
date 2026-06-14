using UnityEngine;

namespace ColorPool
{
    /// <summary>
    /// All the live-tunable gameplay knobs in one asset. Edit the GameSettings asset
    /// (Assets/Resources/GameSettings.asset) in the Inspector; values are read at runtime so
    /// most changes apply on the next Play (sizes/bounce rebuild with the level; friction,
    /// launch power and hit power apply immediately).
    /// </summary>
    [CreateAssetMenu(menuName = "Color Pool/Game Settings", fileName = "GameSettings")]
    public class GameSettings : ScriptableObject
    {
        static GameSettings _i;

        /// <summary>The active settings, loaded from Resources (falls back to defaults).</summary>
        public static GameSettings I
        {
            get
            {
                if (_i == null) _i = Resources.Load<GameSettings>("GameSettings");
                if (_i == null) _i = CreateInstance<GameSettings>();
                return _i;
            }
        }

        [Header("Ball sizes (world radius, metres)")]
        [Range(0.12f, 0.5f)] public float whiteBallRadius = 0.30f;
        [Range(0.12f, 0.5f)] public float coloredBallRadius = 0.26f;

        [Header("White ball launch")]
        [Tooltip("Max launch speed of the white ball (m/s). Higher = harder shots.")]
        public float launchPower = 20f;
        [Tooltip("Extra speed colored balls gain when the white ball hits them (1 = plain hit). Capped at launch power. Note: this is NOT cue weight — all balls are equal mass so they can be knocked off the rails.")]
        [Range(1f, 2.5f)] public float cueHitPower = 1.35f;

        [Header("Friction & stopping")]
        [Tooltip("Fraction of speed kept each 1/60 s. Higher = LESS friction / longer rolls. 1 = frictionless.")]
        [Range(0.95f, 0.999f)] public float friction = 0.992f;
        [Tooltip("Speed (m/s) below which a ball is snapped to a stop so you can shoot again.")]
        public float stopSpeed = 0.35f;

        [Header("Bounce (restitution 0..1)")]
        [Tooltip("How strongly the rails bounce balls back.")]
        [Range(0f, 1f)] public float wallBounce = 0.96f;
        [Tooltip("How bouncy balls are against each other.")]
        [Range(0f, 1f)] public float ballBounce = 0.95f;
    }
}

namespace ColorPool
{
    /// <summary>
    /// Central tuning constants for Color Pool, ported from the 2D prototype
    /// (the source of truth for feel). Prototype values are in pixels/frame @60fps;
    /// here they are converted to world metres using <see cref="UnitsPerPixel"/>.
    /// </summary>
    public static class Tuning
    {
        // 960x600 px canvas -> 9.6 x 6.0 m of play area.
        public const float UnitsPerPixel = 0.01f;

        // Table (XZ plane), oriented VERTICAL for portrait: long axis runs up the screen (Z).
        // Same 1.6:1 play ratio as the prototype, rotated 90deg. The long axis (Z, 9.6) maps to
        // the prototype's 960-pixel axis; the short axis (X, 6.0) maps to its 600-pixel axis.
        public const float TableWidth = 6.0f;   // X (across the screen)
        public const float TableDepth = 9.6f;   // Z (up the screen, long axis)

        // Rails.
        public const float RailHeight = 0.35f;
        public const float RailThickness = 0.30f;

        // Ball.
        public const float DefaultBallRadius = 0.26f; // 26 px
        public const float BaseMass = 1f;             // mass of a default-radius ball

        // Physics feel. Prototype is px/frame @60fps; m/s = (px/frame) * 60 * UnitsPerPixel = px/frame * 0.6.
        public const float FrictionPerFrame = 0.988f;  // v *= 0.988 each 1/60 s
        public const float RailRestitution = 0.85f;
        public const float BallRestitution = 0.95f;
        // Snappier than the prototype's crawl: kill movement once balls get slow so the
        // player can shoot again quickly instead of waiting out the long friction tail.
        public const float StopSpeed = 0.35f;          // m/s; below this a ball is zeroed
        public const float MaxShotSpeed = 14.4f;       // 24 px/frame

        // Aiming: drag anywhere on screen. Power is the drag length as a fraction of screen
        // height (resolution independent); a drag of this fraction = full power.
        public const float DragFullPowerFraction = 0.42f;
        public const float MinDragFraction = 0.012f;   // ignore tiny taps
        public const float PreviewMaxLength = 3.2f;    // max world length of the aim line

        /// <summary>Mass scales with volume so a big cue convincingly scatters small balls.</summary>
        public static float MassForRadius(float radius)
        {
            float k = radius / DefaultBallRadius;
            return BaseMass * k * k * k;
        }

        /// <summary>Per-FixedUpdate damping factor reproducing the prototype's 0.988/frame exactly.</summary>
        public static float DampingFactor(float fixedDeltaTime)
        {
            return UnityEngine.Mathf.Pow(FrictionPerFrame, fixedDeltaTime * 60f);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace ColorPool
{
    /// <summary>
    /// Builds and runs a level: rasterizes the current picture into the PaintSurface, resets
    /// the cue, and spawns colored balls for every colour the picture uses (so it's always
    /// winnable). When the picture is fully painted it cycles to the next picture.
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        public static LevelManager Instance { get; private set; }

        public int ballsPerColor = 2;
        public float winDelay = 1.3f;

        Ball cue;
        PaintSurface paint;
        readonly List<Ball> colored = new List<Ball>();
        Material[] colorMats = new Material[10];
        Shader litShader;
        PhysicsMaterial ballPhys;
        PhysicsMaterial railPhys;

        int pictureIndex;
        int levelCounter;
        bool won;
        float winTimer;

        void Awake() { Instance = this; }

        void Start()
        {
            litShader = Shader.Find("Universal Render Pipeline/Lit");
            paint = PaintSurface.Instance;
            cue = FindCue();
            if (cue != null) ballPhys = cue.GetComponent<SphereCollider>().sharedMaterial;
            var railGO = GameObject.Find("Rail_Near");
            if (railGO != null) railPhys = railGO.GetComponent<BoxCollider>().sharedMaterial;
            ApplyBounce();
            NewLevel(0);
        }

        // Rail bounce = wallBounce and ball-ball = ballBounce, solved for the Average combine.
        void ApplyBounce()
        {
            var s = GameSettings.I;
            if (ballPhys != null)
            {
                ballPhys.bounciness = s.ballBounce;
                ballPhys.bounceCombine = PhysicsMaterialCombine.Average;
                ballPhys.frictionCombine = PhysicsMaterialCombine.Minimum;
                ballPhys.dynamicFriction = 0f; ballPhys.staticFriction = 0f;
            }
            if (railPhys != null)
            {
                railPhys.bounciness = Mathf.Clamp01(2f * s.wallBounce - s.ballBounce);
                railPhys.bounceCombine = PhysicsMaterialCombine.Average;
                railPhys.frictionCombine = PhysicsMaterialCombine.Minimum;
                railPhys.dynamicFriction = 0f; railPhys.staticFriction = 0f;
            }
        }

        static Ball FindCue()
        {
            foreach (var b in FindObjectsByType<Ball>(FindObjectsSortMode.None))
                if (b.colorIndex < 0) return b;
            return null;
        }

        public void NewLevel(int index)
        {
            pictureIndex = index;
            levelCounter++;
            won = false;
            winTimer = 0f;

            paint.Build(PictureLibrary.Get(index), levelCounter * 7919 + index);

            ResetCue();
            SpawnColoredBalls();

            var all = new List<Ball>(colored);
            if (cue != null) all.Add(cue);
            GameManager.Instance.SetBalls(all);
            GameManager.Instance.ResetShots();
            GameManager.Instance.ResumeAiming();
        }

        void ResetCue()
        {
            if (cue == null) return;
            cue.radius = GameSettings.I.whiteBallRadius;
            cue.ApplyRadius();
            var rb = cue.Body;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            cue.transform.position = new Vector3(0f, cue.radius, -3.8f);
        }

        void SpawnColoredBalls()
        {
            foreach (var b in colored) if (b != null) Destroy(b.gameObject);
            colored.Clear();

            Random.InitState(levelCounter * 104729 + pictureIndex);
            bool[] present = paint.PresentColors();
            float r = GameSettings.I.coloredBallRadius;
            float xLim = Tuning.TableWidth * 0.5f - r - 0.15f;
            float zMin = -2.2f, zMax = Tuning.TableDepth * 0.5f - r - 0.2f;
            var placed = new List<Vector3>();
            if (cue != null) placed.Add(cue.transform.position);

            for (int c = 0; c < 10; c++)
            {
                if (!present[c]) continue;
                for (int n = 0; n < ballsPerColor; n++)
                {
                    Vector3 pos = Vector3.zero;
                    for (int tries = 0; tries < 200; tries++)
                    {
                        pos = new Vector3(Random.Range(-xLim, xLim), r, Random.Range(zMin, zMax));
                        bool ok = true;
                        foreach (var q in placed)
                            if ((q - pos).sqrMagnitude < (r * 2f * 1.2f) * (r * 2f * 1.2f)) { ok = false; break; }
                        if (ok) break;
                    }
                    placed.Add(pos);
                    colored.Add(CreateColoredBall(c, pos));
                }
            }
        }

        Ball CreateColoredBall(int color, Vector3 pos)
        {
            float r = GameSettings.I.coloredBallRadius;
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "Ball_" + color;
            go.transform.localScale = Vector3.one * (r * 2f);
            go.transform.position = new Vector3(pos.x, r, pos.z);
            go.GetComponent<MeshRenderer>().sharedMaterial = ColorMat(color);
            go.GetComponent<SphereCollider>().sharedMaterial = ballPhys;
            go.AddComponent<Rigidbody>();
            var ball = go.AddComponent<Ball>();
            ball.colorIndex = color;
            ball.radius = r;
            ball.ApplyRadius(); // recompute mass now that colorIndex is set (no cue boost)
            return ball;
        }

        Material ColorMat(int color)
        {
            if (colorMats[color] == null)
            {
                var m = new Material(litShader);
                Color c = Palette.ColorOf(color);
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.8f);
                colorMats[color] = m;
            }
            return colorMats[color];
        }

        void Update()
        {
            if (paint == null) return;
            if (!won && paint.IsComplete)
            {
                won = true;
                winTimer = 0f;
                GameManager.Instance.SetWin();
            }
            if (won)
            {
                winTimer += Time.deltaTime;
                if (winTimer >= winDelay) NewLevel(pictureIndex + 1);
            }
        }
    }
}

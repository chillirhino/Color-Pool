using UnityEngine;
using UnityEngine.InputSystem;

namespace ColorPool
{
    /// <summary>
    /// Slingshot aiming. Press and drag ANYWHERE on screen, then release to fling the cue
    /// ball: the drag vector sets the shot (pull back to launch the opposite way), and the
    /// drag length sets power (as a fraction of screen height, so it feels the same on any
    /// resolution). A dotted line from the cue previews direction and power. Input is locked
    /// unless the GameManager is Aiming. Uses the new Input System pointer (mouse + touch).
    /// </summary>
    public class CueAimController : MonoBehaviour
    {
        public Ball cueBall;
        public Camera cam;
        public LineRenderer aimLine;   // dotted shot direction/power, from the cue
        public LineRenderer dragLine;  // faint line showing the drag gesture

        Plane tablePlane;
        bool dragging;
        Vector2 startScreen;
        Vector3 startWorld;

        void Start()
        {
            if (cam == null) cam = Camera.main;
            tablePlane = new Plane(Vector3.up, new Vector3(0f, cueBall.radius, 0f));
            ShowLine(aimLine, false);
            ShowLine(dragLine, false);
        }

        void Update()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null || cueBall == null) return;

            bool pressed = pointer.press.isPressed;
            Vector2 screen = pointer.position.ReadValue();

            if (!dragging)
            {
                bool canStart = pressed && GameManager.Instance != null && GameManager.Instance.CanAim;
                if (canStart && RaycastTable(screen, out Vector3 w))
                {
                    dragging = true;
                    startScreen = screen;
                    startWorld = w;
                }
            }
            else if (pressed)
            {
                UpdatePreview(screen);
            }
            else
            {
                Release(screen);
            }
        }

        bool RaycastTable(Vector2 screenPos, out Vector3 world)
        {
            Ray ray = cam.ScreenPointToRay(screenPos);
            if (tablePlane.Raycast(ray, out float enter)) { world = ray.GetPoint(enter); return true; }
            world = Vector3.zero;
            return false;
        }

        // Returns the shot: direction the cue will travel and power as a 0..1 fraction.
        bool ShotFrom(Vector2 currentScreen, out Vector3 dir, out float powerFrac)
        {
            dir = Vector3.zero;
            powerFrac = 0f;

            float dragFrac = (startScreen - currentScreen).magnitude / Mathf.Max(1, Screen.height);
            if (dragFrac < Tuning.MinDragFraction) return false;
            powerFrac = Mathf.Clamp01(dragFrac / Tuning.DragFullPowerFraction);

            // World direction: pull back (start -> current) flings the cue the opposite way.
            if (!RaycastTable(currentScreen, out Vector3 currentWorld)) return false;
            Vector3 back = startWorld - currentWorld;
            back.y = 0f;
            if (back.sqrMagnitude < 1e-6f) return false;
            dir = back.normalized;
            return true;
        }

        void UpdatePreview(Vector2 currentScreen)
        {
            if (!ShotFrom(currentScreen, out Vector3 dir, out float powerFrac))
            {
                ShowLine(aimLine, false);
                ShowLine(dragLine, false);
                return;
            }
            Vector3 cuePos = cueBall.transform.position + Vector3.up * 0.02f;
            ShowLine(aimLine, true);
            aimLine.SetPosition(0, cuePos);
            aimLine.SetPosition(1, cuePos + dir * (Tuning.PreviewMaxLength * powerFrac));

            if (RaycastTable(currentScreen, out Vector3 cw))
            {
                ShowLine(dragLine, true);
                dragLine.SetPosition(0, startWorld + Vector3.up * 0.02f);
                dragLine.SetPosition(1, cw + Vector3.up * 0.02f);
            }
        }

        void Release(Vector2 currentScreen)
        {
            dragging = false;
            ShowLine(aimLine, false);
            ShowLine(dragLine, false);
            if (GameManager.Instance != null && GameManager.Instance.CanAim &&
                ShotFrom(currentScreen, out Vector3 dir, out float powerFrac))
            {
                cueBall.Launch(dir, GameSettings.I.launchPower * powerFrac);
                GameManager.Instance.NotifyShotFired();
            }
        }

        static void ShowLine(LineRenderer lr, bool on)
        {
            if (lr != null && lr.enabled != on) lr.enabled = on;
        }
    }
}

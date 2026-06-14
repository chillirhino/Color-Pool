using System.Collections.Generic;
using UnityEngine;

namespace ColorPool
{
    public enum GameState { Aiming, Simulating, Win }

    /// <summary>
    /// Owns the shot loop: input is allowed only while Aiming. A fired shot -> Simulating;
    /// when every ball stops -> back to Aiming. LevelManager drives Win on completion.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Aiming;
        public int Shots { get; private set; }
        public bool CanAim => State == GameState.Aiming;

        readonly List<Ball> balls = new List<Ball>();

        void Awake() { Instance = this; }

        public void SetBalls(IEnumerable<Ball> newBalls)
        {
            balls.Clear();
            balls.AddRange(newBalls);
        }

        public void ResetShots() { Shots = 0; }

        public void NotifyShotFired()
        {
            Shots++;
            if (State == GameState.Aiming) State = GameState.Simulating;
        }

        public void SetWin() { State = GameState.Win; }
        public void ResumeAiming() { State = GameState.Aiming; }

        public bool AllBallsStopped()
        {
            foreach (Ball b in balls)
                if (b != null && !b.IsStopped) return false;
            return true;
        }

        void Update()
        {
            if (State == GameState.Simulating && AllBallsStopped())
                State = GameState.Aiming;
        }
    }
}

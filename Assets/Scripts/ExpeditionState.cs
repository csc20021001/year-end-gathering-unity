using System;

namespace YearEnd
{
    public enum Relic { Plank, Leaf, Rice, Wine }
    public enum Outcome { Ready, Playing, Won, Lost }
    // Pure game rules, independent of rendering and scene loading.
    [Serializable]
    public sealed class ExpeditionState
    {
        public const float Duration = 180f;
        public const float Invulnerability = 1.25f;
        public float Remaining { get; private set; } = Duration;
        public int Hp { get; private set; } = 100;
        public int Mask { get; private set; }
        public bool Paused { get; private set; }
        public float Immunity { get; private set; }
        public Outcome Status { get; private set; } = Outcome.Ready;
        public int Count { get { int c = 0; for (int i = 0; i < 4; i++) if ((Mask & (1 << i)) != 0) c++; return c; } }
        public int Score => Count * 100 + (int)Math.Floor(Remaining) * 10 + Hp * 5;
        public bool Has(Relic r) => (Mask & (1 << (int)r)) != 0;
        public bool Active => Status == Outcome.Playing && !Paused;
        public void Start() { Remaining = Duration; Hp = 100; Mask = 0; Paused = false; Immunity = 0; Status = Outcome.Playing; }
        public void SetPaused(bool value) { if (Status == Outcome.Playing) Paused = value; }
        public void Tick(float dt)
        {
            if (!Active || dt <= 0 || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            Immunity = Math.Max(0, Immunity - dt);
            Remaining = Math.Max(0, Remaining - dt);
            if (Remaining <= 0) Status = Outcome.Lost;
        }
        public bool Collect(Relic r)
        {
            if (!Active || Has(r)) return false;
            Mask |= 1 << (int)r; return true;
        }
        public bool Damage()
        {
            if (!Active || Immunity > 0) return false;
            Hp = Math.Max(0, Hp - 20); Immunity = Invulnerability;
            if (Hp == 0) Status = Outcome.Lost;
            return true;
        }
        public bool Finish()
        {
            if (!Active || Count != 4 || Hp <= 0 || Remaining <= 0) return false;
            Status = Outcome.Won; return true;
        }
    }
}

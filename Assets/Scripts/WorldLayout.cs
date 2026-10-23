using UnityEngine;

namespace YearEnd
{
    public static class WorldLayout
    {
        // Coordinates use a 100 x 64 map, origin at bottom left.
        public static readonly string[] Scenes = { "ResearchRoom", "Outdoors", "Exhibition" };
        public static readonly Vector2[] ItemPositions = { new Vector2(29, 40), new Vector2(19, 44), new Vector2(78, 43), new Vector2(29, 16) };
        public static readonly Rect[][] Obstacles = {
            new [] { new Rect(0,51,100,13), new Rect(0,14,19,29), new Rect(6,45,19,12), new Rect(85,16,15,36), new Rect(0,0,43,12), new Rect(57,0,43,12) },
            new [] { new Rect(0,49,30,15), new Rect(65,49,35,15), new Rect(35,27,29,16), new Rect(0,4,22,20), new Rect(73,4,27,20), new Rect(0,0,43,9), new Rect(57,0,43,9) },
            new [] { new Rect(0,51,100,13), new Rect(8,32,22,13), new Rect(70,32,24,13), new Rect(6,16,22,13), new Rect(73,16,21,13), new Rect(37,43,26,8), new Rect(0,0,43,10), new Rect(57,0,43,10) }
        };
        public static readonly Vector2[][] PatrolRoutes = {
            new [] { new Vector2(21,31), new Vector2(31,31), new Vector2(31,46), new Vector2(21,46) },
            new [] { new Vector2(69,26), new Vector2(80,26), new Vector2(80,46), new Vector2(69,46) },
            new [] { new Vector2(26,12), new Vector2(70,12) }
        };
        public static bool Blocked(int room, Vector2 point)
        {
            if (point.x < 3 || point.x > 97 || point.y < 3 || point.y > 60) return true;
            foreach (Rect r in Obstacles[room])
                if (new Rect(r.x - 1.3f, r.y - .6f, r.width + 2.6f, r.height + 1.2f).Contains(point)) return true;
            return false;
        }
        public static Vector2 Move(int room, Vector2 from, Vector2 delta)
        {
            // Substeps prevent tunnelling after frame stalls.
            int steps = Mathf.Max(1, Mathf.CeilToInt(delta.magnitude / .5f));
            Vector2 step = delta / steps;
            for (int i = 0; i < steps; i++)
            {
                Vector2 p = from + new Vector2(step.x, 0); if (!Blocked(room, p)) from = p;
                p = from + new Vector2(0, step.y); if (!Blocked(room, p)) from = p;
            }
            return from;
        }
    }
}

using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>Level 04 story pass: Signal Box 4 with Ignatz's logbook (optional side area) and memory fragments.</summary>
    public static partial class Level04Builder
    {
        static void BuildSignalBox(Transform root, Transform shared, Transform logic)
        {
            // Off the depot's east wall (door at z 22), x 14.25..22, z 17..27.
            var g = Group("SignalBox", shared);
            Floor("BoxFloor", g, new Vector3(18.1f, 0f, 22f), new Vector2(7.7f, 10f), "wood_dark");
            Wall("BoxS", g, new Vector3(14.25f, 0f, 17f), new Vector3(22f, 0f, 17f), 3.6f, 0.3f, "brick_warm");
            Wall("BoxN", g, new Vector3(14.25f, 0f, 27f), new Vector3(22f, 0f, 27f), 3.6f, 0.3f, "brick_warm");
            Wall("BoxE", g, new Vector3(22f, 0f, 17f), new Vector3(22f, 0f, 27f), 3.6f, 0.3f, "brick_warm");
            Box("BoxCeiling", g, new Vector3(18.1f, 3.75f, 22f), new Vector3(8f, 0.3f, 10.3f), "wood_dark");
            Sign(g, new Vector3(13.7f, 3.2f, 22f), 90f, "SIGNAL BOX 4", 0.4f, new Color(1f, 0.85f, 0.55f), "wood_dark");

            // Lever frame along the east wall.
            Box("LeverFrame", g, new Vector3(21.2f, 0.4f, 22f), new Vector3(0.8f, 0.8f, 6f), "metal_dark");
            string[] names = { "DEPOT", "XING 1", "XING 2", "XING 3", "PLATFORM", "DEEP LIFT", "SPARE", "SPARE" };
            for (int i = 0; i < names.Length; i++)
            {
                float z = 19.4f + i * 0.75f;
                var lever = Box("Lever", g, new Vector3(21f, 1.2f, z), new Vector3(0.08f, 0.9f, 0.08f), i == 5 ? "warning_red" : "rail", false);
                lever.transform.rotation = Quaternion.Euler(0f, 0f, i == 5 ? -25f : 15f);
                Label("LeverTag", g, new Vector3(20.75f, 0.9f, z), 90f, (i + 1) + "\n" + names[i], 0.11f, new Color(1f, 0.92f, 0.75f));
            }
            Box("LineDiagram", g, new Vector3(21.82f, 2.4f, 22f), new Vector3(0.05f, 1f, 5f), "paper", false);
            Label("DiagramText", g, new Vector3(21.75f, 2.4f, 22f), 90f, "DEPOT == XING (3) == STATION == DEEP LIFT\nSIX CARS HELD AT DEEP LIFT - FOUNDERS' ORDER", 0.15f, new Color(0.2f, 0.15f, 0.1f));
            Box("Desk", g, new Vector3(16.5f, 0.4f, 25.6f), new Vector3(2f, 0.8f, 0.9f), "wood");
            var log = Clue("IgnatzLogbook", g, new Vector3(16.5f, 0.85f, 25.6f), new Vector3(0.5f, 0.08f, 0.45f), "fabric_red", "Read Ignatz's logbook", "L04.logbook", "L04.logbook",
                "Conductor's logbook - I. Marrow",
                "Day -9. Order under Founder Halden's seal: hold six cars at the deep lift every night. No passengers. 'Practice.'\n\nDay -6. Season passes printed for 214. Children's fares mostly. Wren asked me why. I said I don't ask.\n\nDay -2. Ilse came down from the tower, still with that carved zero in her pocket. Made me promise: whatever happens, keep the deep lift powered.\n\nDay -1. Cars full by the first bell. I drove them down. I didn't look at the faces. I wish I had.", true, TimeMask.All);
            log.lines.Add(L("Arin", "He drove the evacuation trains and never knew what they were for."));
            Box("Kettle", g, new Vector3(15.2f, 0.95f, 25.6f), new Vector3(0.25f, 0.3f, 0.25f), "copper_verdigris", false);

            // Past: warm and tidy. Present: rust and water. Future: crystal and holo timetable.
            var past = StateGroup("SignalBoxPast", root, TimeMask.Past, false);
            Sphere("Lamp", past, new Vector3(18f, 3.3f, 22f), 0.2f, "lamp_warm", false);
            PointLight("BoxLamp", past, new Vector3(18f, 3f, 22f), Warm, 2f, 9f);
            Box("Calendar", past, new Vector3(14.42f, 2f, 25f), new Vector3(0.03f, 0.6f, 0.45f), "paper", false);
            Label("CalendarText", past, new Vector3(14.45f, 2f, 25f), -90f, "FESTIVAL\nWEEK\n(0)", 0.14f, new Color(0.5f, 0.1f, 0.1f));
            var pres = StateGroup("SignalBoxPresent", root, TimeMask.Present, false);
            Box("Puddle", pres, new Vector3(18f, 0.02f, 20f), new Vector3(3f, 0.02f, 2f), "water", false);
            Box("RustStreak", pres, new Vector3(21.8f, 1.6f, 19f), new Vector3(0.03f, 2.5f, 0.6f), "iron_rust", false);
            var fut = StateGroup("SignalBoxFuture", root, TimeMask.Future, false);
            var rng = new System.Random(4407);
            Crystal(fut, new Vector3(15.5f, 0f, 18.2f), 1.6f, rng, false);
            Crystal(fut, new Vector3(20.8f, 0f, 26f), 1.2f, rng, false);
            Hologram("TimetableGhost", fut, new Vector3(18f, 2f, 22f), new Vector3(1.6f, 0.9f, 0.02f), PrimitiveType.Cube, "hologram_amber", true, false);
            Label("TimetableGhostText", fut, new Vector3(18f, 2f, 21.95f), 0f, "CARS 1-6 : DEEP LIFT\nDEPART : FIRST BELL", 0.2f, new Color(1f, 0.8f, 0.5f));
            Ambient("BoxClock", g, new Vector3(18f, 2f, 22f), AmbientKind.Machine, 0.1f, TimeMask.Past, 1f, 8f);
        }

        static void BuildFragments(Transform root)
        {
            var frags = Group("MemoryFragments", root);
            Fragment(frags, new Vector3(15.3f, 1.2f, 18f), "L04.1", "The signalman's tea",
                "PAST. A signalman, to Ignatz: 'Six cars held at the deep lift again. You ever think the Founders know something we don't?' Ignatz: 'I think the Founders know everything we don't. Pour the tea.'");
            Fragment(frags, new Vector3(-12.2f, 1.2f, 56f), "L04.2", "Season pass",
                "PAST. Wren, counting: 'Two hundred and fourteen. Halden, Oskel, Varo, Lund... all the passes go to the same streets. Lantern Row gets twelve. Our street gets none.'");
            Fragment(frags, new Vector3(-13f, 1.2f, 29f), "L04.3", "Last car",
                "PRESENT. A depot worker, old now: 'I saw the last car go down full of children. Every one of them holding a ticket. I was told it was a festival outing. I was told a lot of things.'");
        }
    }
}

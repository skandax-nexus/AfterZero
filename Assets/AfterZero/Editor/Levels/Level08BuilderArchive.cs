using UnityEngine;
using static AfterZero.EditorTools.AZKit;
using static AfterZero.EditorTools.Level07BuilderKit;

namespace AfterZero.EditorTools
{
    public static partial class Level08Builder
    {
        // =====================================================================
        // Optional side area: the archive alcove of every previous cycle's letter
        // + the blue cup on the altar + memory fragments
        // =====================================================================

        static void BuildArchive(Transform shared, Transform logic)
        {
            var a = Group("Archive of Cycles", shared);
            Wall("ArchiveW", a, new Vector3(-15f, 0f, -41f), new Vector3(-15f, 0f, -31f), 5f, 0.5f, "obsidian");
            Wall("ArchiveN", a, new Vector3(-15f, 0f, -31f), new Vector3(-5f, 0f, -31f), 5f, 0.5f, "obsidian");
            Wall("ArchiveS", a, new Vector3(-15f, 0f, -41f), new Vector3(-5f, 0f, -41f), 5f, 0.5f, "obsidian");
            Box("ArchiveCeiling", a, new Vector3(-10f, 5.2f, -36f), new Vector3(10.4f, 0.4f, 10.4f), "obsidian");
            Label("ArchiveSign", a, new Vector3(-4.65f, 3.5f, -36f), -90f, "ARCHIVE OF CYCLES", 0.4f, new Color(1f, 0.85f, 0.6f));

            // Shelving: ten units of letters, numbered by cycle.
            var units = new (Vector3 pos, float yaw)[]
            {
                (new Vector3(-14.5f, 0f, -39.3f), 90f), (new Vector3(-14.5f, 0f, -37.1f), 90f), (new Vector3(-14.5f, 0f, -34.9f), 90f), (new Vector3(-14.5f, 0f, -32.7f), 90f),
                (new Vector3(-12.6f, 0f, -31.5f), 180f), (new Vector3(-10.2f, 0f, -31.5f), 180f), (new Vector3(-7.8f, 0f, -31.5f), 180f),
                (new Vector3(-12.6f, 0f, -40.5f), 0f), (new Vector3(-10.2f, 0f, -40.5f), 0f), (new Vector3(-7.8f, 0f, -40.5f), 0f),
            };
            for (int i = 0; i < units.Length; i++)
            {
                var u = Group("LetterShelf", a);
                u.position = units[i].pos;
                u.rotation = Quaternion.Euler(0f, units[i].yaw, 0f);
                GameObject B(string n, Vector3 local, Vector3 size, string mat, bool col = false)
                {
                    var go = Box(n, u, u.TransformPoint(local), size, mat, col);
                    go.transform.rotation = u.rotation;
                    return go;
                }
                B("Frame", new Vector3(0f, 1.6f, 0f), new Vector3(2f, 3.2f, 0.5f), "wood_dark", true);
                for (int k = 0; k < 4; k++)
                    NoShadow(B("Letters", new Vector3(0f, 0.45f + k * 0.75f, 0.27f), new Vector3(1.8f, 0.4f, 0.06f), "paper"));
                int from = i * 40 + 1, to = i * 40 + 40;
                // Front face is local +z (toward the room); text must be readable from the room.
                var lp = u.TransformPoint(new Vector3(0f, 3.4f, 0.3f));
                Label("ShelfNumber", u, lp, FaceYaw(lp, u.TransformPoint(new Vector3(0f, 1.6f, 3f))), string.Format("CYCLES {0:0000}-{1:0000}", from, to), 0.22f, new Color(1f, 0.9f, 0.7f));
            }
            // The desk: the newest letter, unsent.
            Box("Desk", a, new Vector3(-10f, 0.45f, -36f), new Vector3(2.2f, 0.9f, 1.1f), "wood");
            Cylinder("DeskLampPost", a, new Vector3(-10.8f, 1.2f, -36.3f), 0.04f, 0.6f, "brass", false);
            NoShadow(Sphere("DeskLampHead", a, new Vector3(-10.8f, 1.55f, -36.3f), 0.12f, "lamp_warm", false));
            PointLight("DeskLight", a, new Vector3(-10.5f, 2.2f, -36f), new Color(1f, 0.78f, 0.5f), 2.5f, 8f);
            for (int i = 0; i < 9; i++)
                Tilted("StackedLetter", a, new Vector3(-9.2f, 0.92f + i * 0.012f, -35.8f), new Vector3(0.32f, 0.01f, 0.22f), "paper", new Vector3(0f, i * 7f, 0f), false);
            Label("DeskNumber", a, new Vector3(-10f, 0.6f, -35.43f), 0f, "CYCLES 0401-0412", 0.18f, new Color(0.2f, 0.12f, 0.05f));
            Clue("NewestLetter", a, new Vector3(-9.9f, 0.93f, -36.2f), new Vector3(0.45f, 0.05f, 0.34f), "paper", "Read the unsent letter, cycle 0413", "L08.letterRead", "L08.letter",
                "Letter, Cycle 0413 (unsent)",
                "To Arin. Dated tomorrow.\n\nYou will arrive on the last train. You will not remember me. Take the blue cup before it falls. Follow the zero.\n\nIn the margins, a page of practice zeros -- dozens of them, each one closer to a single stroke with a small hook where it closes. Your stroke.\n\n-- M.",
                true);

            // Founder Aurel Halden's recording.
            Box("RecordTable", a, new Vector3(-13.6f, 0.4f, -36f), new Vector3(0.9f, 0.8f, 0.9f), "wood_dark");
            Cylinder("Phonograph", a, new Vector3(-13.6f, 0.85f, -36f), 0.3f, 0.1f, "brass", false);
            var horn = Box("Horn", a, new Vector3(-13.6f, 1.25f, -35.8f), new Vector3(0.35f, 0.35f, 0.6f), "brass", false);
            horn.transform.rotation = Quaternion.Euler(-30f, 0f, 0f);
            var rec = Clue("HaldenRecording", a, new Vector3(-13.6f, 0.95f, -36f), new Vector3(0.5f, 0.06f, 0.5f), "wood_dark", "Play Founder Halden's recording", "L08.haldenHeard", "L08.halden",
                "Recording: Founder Aurel Halden",
                "Halden, for the Founders: the three states will fall into one. The children move first, nine days early, names struck from the record. The bell rings once as the warning; twice means failure. 'I chose silence. If you are hearing this, silence was not enough.'",
                false);
            rec.lines.Add(L("Founder Aurel Halden", "This is Aurel Halden, for the Founders -- for whoever finds the Zero Chamber after us.", 4.5f));
            rec.lines.Add(L("Founder Aurel Halden", "The three states are grinding together. Our numbers say they will fall into one. Every era. Every person.", 5f));
            rec.lines.Add(L("Founder Aurel Halden", "We move the children first, nine days early, to the pocket between the states. Their names come off the record, so the city does not panic.", 5.5f));
            rec.lines.Add(L("Founder Aurel Halden", "On the night the bell rings once and the square empties. If it rings twice... we have failed.", 4.5f));
            rec.lines.Add(L("Founder Aurel Halden", "I chose silence. If you are hearing this, silence was not enough.", 4f));
            rec.lines.Add(L("Founder Aurel Halden", "Forgive us, or don't. But finish it.", 3.5f));
            Label("RecordTag", a, new Vector3(-13.6f, 1.9f, -36f), -90f, "A. HALDEN", 0.25f, new Color(1f, 0.9f, 0.7f));
            Ambient("ArchiveHush", a, new Vector3(-10f, 2f, -36f), AmbientKind.Nursery, 0.12f, TimeMask.All, 1f, 8f);
            ReflectionProbe(a, new Vector3(-10f, 2.5f, -36f), new Vector3(10f, 5f, 10f));

            // The blue cup, intact on the altar.
            var cup = Use("BlueCup", shared, new Vector3(1.4f, 0.5f, -1.2f), new Vector3(0.4f, 0.2f, 0.4f), "cup_blue", "Look at the blue cup", "L08.cupSeen", TimeMask.All, PrimitiveType.Cylinder);
            cup.lines.Add(L("Arin", "The blue cup. Intact. The first thing I saved, the first morning."));
            cup.lines.Add(L("Arin", "Every cycle it falls and breaks. Here, it never has."));

            // Memory fragments, off the main path.
            var f = Group("Memory Fragments", shared);
            Fragment(f, new Vector3(-14f, 1.3f, -40.2f), "L08.1", "Letter 0001",
                "The first letter in the archive, in a child's careful capitals: 'TO WHOEVER COMES NEXT. THE BELL RANG TWICE. I AM STILL HERE. I WILL WRITE AGAIN TOMORROW. -- MAREN, AGE 9.'");
            Fragment(f, new Vector3(-27.6f, 1.3f, -4.2f), "L08.2", "The Clapper Block",
                "PAST. Ilse Marrow's workbook: 'Cut the old clapper block into a zero this morning. Halden says the engine needs a key nobody would think to steal. Who steals a toy?'");
            Fragment(f, new Vector3(27.6f, 1.3f, -4.2f), "L08.3", "Branch Weather",
                "FUTURE. A survey slate from beyond the wall: 'Region 4: rain falls upward on Tuesdays. Region 7: the waterfall stopped mid-air in cycle 12 and has not moved since. Region 9: green sun, no shadows. Recommend: do not open the wall yet.'");
        }
    }
}

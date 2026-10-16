using UnityEngine;
using static AfterZero.EditorTools.AZKit;

namespace AfterZero.EditorTools
{
    /// <summary>Level 03 story pass: Odile's restricted stacks (optional side area), memory fragments, The Witness.</summary>
    public static partial class Level03Builder
    {
        static void BuildRestrictedStacks(Transform root, Transform shared, Transform logic)
        {
            // Side room behind the east-wall door (x 18..26, z 33..45).
            var g = Group("RestrictedStacks", shared);
            Floor("StacksFloor", g, new Vector3(22.3f, 0f, 39f), new Vector2(8.6f, 12f), "wood_dark");
            Wall("StacksS", g, new Vector3(18.3f, 0f, 33f), new Vector3(26.6f, 0f, 33f), 4.2f, 0.4f, "stone_warm");
            Wall("StacksN", g, new Vector3(18.3f, 0f, 45f), new Vector3(26.6f, 0f, 45f), 4.2f, 0.4f, "stone_warm");
            Wall("StacksE", g, new Vector3(26.6f, 0f, 33f), new Vector3(26.6f, 0f, 45f), 4.2f, 0.4f, "stone_warm");
            Sign(g, new Vector3(17.65f, 3.5f, 39f), 90f, "RESTRICTED STACKS - O. FENWICK", 0.35f, new Color(1f, 0.85f, 0.6f), "wood_dark");

            // Odile's desk and the sealed stacks.
            Box("OdileDesk", g, new Vector3(24.5f, 0.4f, 41.5f), new Vector3(1.6f, 0.8f, 1f), "wood");
            Box("DeskChair", g, new Vector3(25.6f, 0.25f, 41.5f), new Vector3(0.5f, 0.5f, 0.5f), "wood_dark");
            for (int i = 0; i < 3; i++)
            {
                Box("SealedStack", g, new Vector3(20f + i * 2.4f, 1.6f, 33.7f), new Vector3(1.8f, 3.2f, 0.8f), "wood_dark");
                Box("Cage", g, new Vector3(20f + i * 2.4f, 1.6f, 34.15f), new Vector3(1.9f, 3.2f, 0.04f), "iron_rust", false);
                Label("StackTag", g, new Vector3(20f + i * 2.4f, 2.9f, 34.25f), 180f, i == 1 ? "NURSERY REGISTER\nSEALED BY FOUNDERS' ORDER" : "TRANSFERS " + (i == 0 ? "A-L" : "M-Z"), 0.18f, new Color(1f, 0.85f, 0.6f));
            }
            Box("Crate", g, new Vector3(21f, 0.4f, 43.8f), new Vector3(1.4f, 0.8f, 1f), "wood");
            Label("CrateText", g, new Vector3(21f, 0.6f, 43.25f), 0f, "LINE 4 - CHILD FARES - 214", 0.2f, new Color(0.9f, 0.85f, 0.7f));

            var note = Clue("OdileNote", g, new Vector3(24.5f, 0.85f, 41.5f), new Vector3(0.5f, 0.04f, 0.4f), "paper", "Read the note on Odile's desk", "L03.odileNote", "L03.odile",
                "Odile Fenwick's desk note",
                "They want her filed twice. Living, then transferred to a register that has no address. Then dead, so no one will look.\n\nI have kept the certificate. I could not burn it; it has a countersignature I recognise from the visitors' book, the same careful hand every festival morning: ARIN.\n\nI have put it where only water will find it - the survey basin. If the rain-courses ever run again, someone will read it.\n\n- O.F.", true, TimeMask.Present | TimeMask.Future);
            note.lines.Add(L("Arin", "She hid it on purpose. And she knew my handwriting before I did."));
            var forged = Clue("ForgedCopy", g, new Vector3(23.4f, 0.85f, 41.5f), new Vector3(0.45f, 0.04f, 0.4f), "paper", "Take Odile's forged copy", "L03.forgedCopy", "L03.forged",
                "Odile's forged copy",
                "A second certificate for resident 0417, perfect in every line - except the countersignature is a careful imitation, and the paper is new.\n\nOdile's pencil note: 'For the Keepers, when they come. Let them burn this one.'", true, TimeMask.Present | TimeMask.Future);
            forged.lines.Add(L("Arin", "A decoy. Odile planned for the Keepers. I'll keep it close."));
            Box("ChildDrawing", g, new Vector3(26.35f, 1.6f, 38f), new Vector3(0.03f, 0.6f, 0.8f), "paper", false);
            Label("DrawingText", g, new Vector3(26.3f, 1.6f, 38f), 90f, "( 0 )\nme and the\nnice lady\n- M.", 0.16f, new Color(0.3f, 0.2f, 0.15f));

            // Past: the room is locked.
            var past = StateGroup("StacksPastDoor", root, TimeMask.Past, true);
            Box("LockedDoor", past, new Vector3(18f, 1.5f, 39f), new Vector3(0.2f, 3f, 2f), "wood");
            Label("LockedText", past, new Vector3(17.85f, 1.8f, 39f), 90f, "LOCKED\nkey with O. Fenwick", 0.22f, new Color(1f, 0.9f, 0.7f));
            Box("PastCeiling", past, new Vector3(22.3f, 4.4f, 39f), new Vector3(8.8f, 0.4f, 12.4f), "plaster_warm");
            // Present: Bram's lamp left burning, damp.
            var pres = StateGroup("StacksPresent", root, TimeMask.Present, false);
            Box("PresentCeiling", pres, new Vector3(22.3f, 4.4f, 39f), new Vector3(8.8f, 0.4f, 12.4f), "plaster_cold");
            Cylinder("OilLamp", pres, new Vector3(24f, 0.95f, 41.2f), 0.08f, 0.2f, "lamp_warm", false);
            PointLight("BramsLamp", pres, new Vector3(24f, 1.4f, 41.2f), Warm, 1.6f, 7f);
            Box("Puddle", pres, new Vector3(21f, 0.02f, 38f), new Vector3(2.4f, 0.02f, 2f), "water", false);
            Ambient("StacksDrip", pres, new Vector3(22f, 2f, 39f), AmbientKind.Water, 0.15f, TimeMask.Present, 1f, 8f);
            // Future: open to the sky, crystals in the cages, a hologram of the sealed register.
            var fut = StateGroup("StacksFuture", root, TimeMask.Future, false);
            var rng = new System.Random(3303);
            for (int i = 0; i < 3; i++) Crystal(fut, new Vector3(20f + i * 2.4f, 0f, 34.7f), 1.4f, rng);
            Hologram("RegisterGhost", fut, new Vector3(22.5f, 2.2f, 39f), new Vector3(1.4f, 1f, 0.02f), PrimitiveType.Cube, "hologram_magenta", true, false);
            Label("RegisterGhostText", fut, new Vector3(22.5f, 2.2f, 38.95f), 0f, "NURSERY REGISTER\nADDRESS: (none)", 0.2f, new Color(1f, 0.7f, 0.95f));
            PointLight("StacksNeon", fut, new Vector3(22.5f, 3f, 39f), new Color(1f, 0.35f, 0.85f), 2f, 9f);
            ReflectionProbe(g, new Vector3(22.3f, 2f, 39f), new Vector3(8.6f, 4.2f, 12f));
        }

        static void BuildStoryExtras(Transform root, Transform logic)
        {
            var w = Zone("WitnessHall", logic, new Vector3(0f, 1.5f, 2.5f), new Vector3(6f, 3f, 2f), "L03.witnessHall");
            w.lines.Add(L("The Witness", "A name written twice is a name someone wanted kept."));

            var frags = Group("MemoryFragments", root);
            Fragment(frags, new Vector3(25.6f, 1.3f, 35.2f), "L03.1", "The stamp clerk",
                "PAST. Tobin, stamping: 'Two hundred and fourteen child fares, all on the Founders' account. I asked Mistress Fenwick if there was a school trip. She took the box away and locked her door.'");
            Fragment(frags, new Vector3(-16.4f, 1.2f, 2.2f), "L03.2", "A mother in the queue",
                "PAST. Tamsin Oskel, holding a small hand: 'They say the Nursery is just for the festival week. Maren, don't hum in the Hall. The lady with the cards is kind, but she listens to everything.'");
            Fragment(frags, new Vector3(16.6f, 1.2f, 46.6f), "L03.3", "The night of the second bell",
                "PRESENT. Bram, forty years ago: 'The bell rang twice and the archive shook. In the morning the Register of the Dead had grown by a hundred children. Not one family came to claim a grave.'");
        }
    }
}

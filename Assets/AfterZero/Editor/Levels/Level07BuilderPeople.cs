using System.Collections.Generic;
using UnityEngine;
using static AfterZero.EditorTools.AZKit;
using static AfterZero.EditorTools.Level07BuilderKit;

namespace AfterZero.EditorTools
{
    public static partial class Level07Builder
    {
        // =====================================================================
        // Evacuation: three groups, named NPCs, warden, statues, sheltering crowd
        // =====================================================================

        // =====================================================================
        // Optional side area: the Founders' Hall and the evacuation planning table
        // =====================================================================

        static void BuildFoundersHall(Transform g, Transform past, Transform present, Transform future, Transform lit)
        {
            var h = Group("Founders' Hall", g);
            const float x0 = -32f, x1 = -7.4f, z0 = 28f, z1 = 46f;
            WallWithDoor("HallFront", h, new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0), 14f, 0.6f, "stone_warm", 2.6f, 3.2f, 0.285f);
            Wall("HallWest", h, new Vector3(x0, 0f, z0), new Vector3(x0, 0f, z1), 14f, 0.6f, "stone_warm");
            Wall("HallNorth", h, new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1), 14f, 0.6f, "stone_warm");
            Wall("HallEast", h, new Vector3(x1, 0f, z0), new Vector3(x1, 0f, z1), 14f, 0.6f, "stone_warm");
            Box("HallRoof", h, new Vector3((x0 + x1) * 0.5f, 14.2f, (z0 + z1) * 0.5f), new Vector3(x1 - x0 + 0.6f, 0.4f, z1 - z0 + 0.6f), "stone_warm");
            Box("HallCornice", h, new Vector3((x0 + x1) * 0.5f, 14.5f, z0 - 0.2f), new Vector3(x1 - x0 + 0.8f, 0.4f, 0.6f), "brass", false);
            Box("InnerCeiling", h, new Vector3((x0 + x1) * 0.5f, 7.2f, (z0 + z1) * 0.5f), new Vector3(x1 - x0, 0.3f, z1 - z0), "wood_dark");
            float doorX = x0 + (x1 - x0) * 0.285f;
            Label("HallName", h, new Vector3(doorX, 4.2f, z0 - 0.35f), 0f, "FOUNDERS' HALL", 0.7f, new Color(1f, 0.85f, 0.6f));
            Box("DoorArch", h, new Vector3(doorX, 3.4f, z0 - 0.35f), new Vector3(3.2f, 0.3f, 0.2f), "brass", false);
            var wg = Group("Hall Windows", lit);
            for (int f = 1; f < 4; f++)
            for (int c = 0; c < 7; c++)
            {
                if ((c + f) % 3 == 0) continue;
                NoShadow(Box("Window", wg, new Vector3(x0 + 2f + c * 3.4f, 2.2f + f * 3.4f, z0 - 0.34f), new Vector3(1.1f, 1.7f, 0.08f), "window_warm", false));
            }
            var fg = Group("Hall Neon", future);
            NoShadow(Box("Trim", fg, new Vector3((x0 + x1) * 0.5f, 13.5f, z0 - 0.35f), new Vector3(x1 - x0, 0.15f, 0.1f), "neon_violet", false));

            // Interior: the evacuation planning table.
            var t = Group("Planning Room", h);
            Vector3 tc = new Vector3(-19.5f, 0f, 37f);
            Box("Table", t, tc + Vector3.up * 0.45f, new Vector3(6f, 0.9f, 3f), "wood_dark");
            NoShadow(Box("Map", t, tc + Vector3.up * 0.91f, new Vector3(5.6f, 0.02f, 2.6f), "paper", false));
            // The square in miniature: tower, tram stairs, arcade, market, with route lines.
            NoShadow(Box("MapTower", t, tc + new Vector3(0f, 1.0f, 0.9f), new Vector3(0.4f, 0.2f, 0.4f), "stone_warm", false));
            NoShadow(Box("MapTram", t, tc + new Vector3(-1.6f, 0.95f, -1f), new Vector3(0.5f, 0.06f, 0.2f), "neon_amber", false));
            NoShadow(Box("MapRoute1", t, tc + new Vector3(-0.2f, 0.93f, -0.3f), new Vector3(2.8f, 0.01f, 0.05f), "fabric_red", false));
            NoShadow(Box("MapRoute2", t, tc + new Vector3(-1.2f, 0.93f, 0.2f), new Vector3(0.05f, 0.01f, 1.8f), "fabric_red", false));
            for (int i = 0; i < 6; i++)
                Cylinder("Marker", t, tc + new Vector3(-2.2f + i * 0.8f, 1.0f, -0.6f + (i % 2) * 0.9f), 0.06f, 0.18f, i < 3 ? "fabric_teal" : "fabric_red", false);
            Label("MapTitle", t, tc + new Vector3(0f, 0.95f, -1.2f), 0f, "EVACUATION -- PHASE ONE: NINE DAYS BEFORE", 0.18f, new Color(0.2f, 0.12f, 0.05f)).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            Label("MapTram", t, tc + new Vector3(-1.6f, 0.97f, -0.8f), 0f, "TRAM STAIRS", 0.14f, new Color(0.2f, 0.12f, 0.05f)).transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 8; i++)
            {
                float side = i < 4 ? -1f : 1f;
                Box("Chair", t, tc + new Vector3(-2.3f + (i % 4) * 1.5f, 0.5f, side * 2.1f), new Vector3(0.6f, 1f, 0.6f), "wood");
            }
            Clue("EvacuationPlan", t, tc + new Vector3(2.2f, 0.96f, 0.6f), new Vector3(0.6f, 0.04f, 0.45f), "paper", "Read the Founders' evacuation plan", "L07.planRead", "L07.plan",
                "The Founders' Evacuation Plan",
                "PHASE ONE (nine days before): the children of the listed families, quietly, by the tram stairs to the place with no address. Strike their names from the public record. No panic.\n\nPHASE TWO (the night): the square. The bell rings ONCE as the warning.\n\nIf the bell rings TWICE, the rescue has failed. Seal the tower. Call it an earthquake.\n\n-- A. Halden, for the Founders",
                true);
            // Wall board: the struck families.
            Box("ListBoard", t, new Vector3(-19.5f, 3.2f, 45.6f), new Vector3(7f, 3.6f, 0.1f), "wood_dark", false);
            NoShadow(Box("ListPaper", t, new Vector3(-19.5f, 3.2f, 45.52f), new Vector3(6.4f, 3.2f, 0.02f), "paper", false));
            Label("ListText", t, new Vector3(-19.5f, 3.2f, 45.48f), 0f,
                "LISTED FAMILIES\nOSKEL (Lane) .......... STRUCK\nVEYRA (Row) ........... STRUCK\nFENWICK ............... STRUCK\nARCADE ROW ............ STRUCK\nMARROW ...... remains (clockkeeper)",
                0.22f, new Color(0.15f, 0.1f, 0.05f));
            // Founders' portraits.
            string[] portraits = { "SELA VEYRA", "AUREL HALDEN", "OREN VEYRA" };
            for (int i = 0; i < 3; i++)
            {
                Box("Portrait", t, new Vector3(-31.6f, 3.6f, 32f + i * 5f), new Vector3(0.1f, 2.2f, 1.6f), i == 1 ? "brass" : "wood", false);
                NoShadow(Box("Canvas", t, new Vector3(-31.53f, 3.6f, 32f + i * 5f), new Vector3(0.02f, 1.9f, 1.3f), i == 1 ? "coat_burgundy" : "coat_navy", false));
                Label("PortraitName", t, new Vector3(-31.5f, 2.2f, 32f + i * 5f), -90f, portraits[i], 0.22f, new Color(1f, 0.9f, 0.7f));
            }
            PointLight("Chandelier", t, tc + Vector3.up * 5.5f, new Color(1f, 0.78f, 0.5f), 3f, 14f);

            // Per-state layers.
            var ph = Group("Hall (Past)", past);
            Crowd("Founders' Council", ph, tc + new Vector3(0f, 0f, -2.6f), new Vector2(5f, 1f), 2, 7501,
                new[] { "Nine days. It has to be nine days.", "If the names stay on the record, the city will panic.", "And if the bell rings twice?" }, "paper");
            NoShadow(Box("CouncilBanner", ph, new Vector3(-19.5f, 6f, 45.5f), new Vector3(9f, 1.2f, 0.05f), "fabric_red", false));
            var prh = Group("Hall (Present)", present);
            var rng = new System.Random(4242);
            for (int i = 0; i < 10; i++)
            {
                var pp = tc + new Vector3(((float)rng.NextDouble() - 0.5f) * 10f, 0.02f, ((float)rng.NextDouble() - 0.5f) * 7f);
                Tilted("StrewnPage", prh, pp, new Vector3(0.3f, 0.01f, 0.4f), "paper", new Vector3(0f, rng.Next(360), 0f), false);
            }
            Tilted("ToppledChair", prh, tc + new Vector3(3.6f, 0.3f, -2.4f), new Vector3(0.6f, 1f, 0.6f), "wood", new Vector3(80f, 20f, 0f), false);
            var fh = Group("Hall (Future)", future);
            Hologram("HoloMap", fh, tc + Vector3.up * 1.8f, new Vector3(5f, 0.03f, 2.4f), PrimitiveType.Cube, "hologram", true, false);
            Label("HoloMapText", fh, tc + new Vector3(0f, 2.4f, 0f), 0f, "PHASE ONE: COMPLETE\nPHASE TWO: FAILED  x  CYCLES", 0.35f, new Color(0.6f, 0.95f, 1f));
            ReflectionProbe(h, tc + Vector3.up * 3f, new Vector3(24f, 7f, 18f));
            Ambient("CouncilMurmur", h, tc + Vector3.up, AmbientKind.Crowd, 0.25f, TimeMask.Past, 1f, 14f);
        }

    }
}

using Parcial2.Core;
using Parcial2.Wolves;
using UnityEngine;

namespace Parcial2.Editor.Levels
{
    internal static class VariantALayout
    {
        public const float NodeHeight = 0.5f;
        public const float AgentHeight = 1f;

        public static readonly AreaSpec[] Areas =
        {
            new("Start", 0f, 13f, 6f, 19f, ZoneType.Start),
            new("Start Corridor", 6f, 14.5f, 8f, 17.5f),
            new("West Ring", 8f, 0f, 11f, 32f),
            new("North Ring", 11f, 29f, 41f, 32f),
            new("South Ring", 11f, 0f, 41f, 3f),
            new("East Ring", 41f, 0f, 44f, 32f),
            new("West Axis", 11f, 14.5f, 19f, 17.5f),
            new("Hunting Ground", 19f, 11f, 33f, 21f, ZoneType.HuntingGround),
            new("East Axis", 33f, 14.5f, 41f, 17.5f),
            new("Exit Corridor", 44f, 14.5f, 46f, 17.5f),
            new("Exit", 46f, 13f, 52f, 19f, ZoneType.Exit),
            new("SW Safe Alley", 13.5f, 10f, 16.5f, 14.5f),
            new("SW Safe", 12.5f, 5f, 17.5f, 10f, ZoneType.Safe),
            new("NE Safe Alley", 35.5f, 17.5f, 38.5f, 22f),
            new("NE Safe", 34.5f, 22f, 39.5f, 27f, ZoneType.Safe),
            new("North Room", 20f, 22.5f, 32f, 26.5f),
            new("North Room West Door", 20.5f, 26.5f, 23.5f, 29f),
            new("North Room East Door", 28.5f, 26.5f, 31.5f, 29f),
            new("South Room", 20f, 5.5f, 32f, 9.5f),
            new("South Room West Door", 20.5f, 3f, 23.5f, 5.5f),
            new("South Room East Door", 28.5f, 3f, 31.5f, 5.5f),
        };

        public static readonly NodeSpec[] Nodes =
        {
            new("Node 00 Start", 3f, 16f),
            new("Node 01 Start Door", 7f, 16f),
            new("Node 02 West Ring South Corner", 9.5f, 1.5f),
            new("Node 03 West Ring South", 9.5f, 9f),
            new("Node 04 West Ring Cross", 9.5f, 16f),
            new("Node 05 West Ring North", 9.5f, 23f),
            new("Node 06 West Ring North Corner", 9.5f, 30.5f),
            new("Node 07 North Ring West", 16f, 30.5f),
            new("Node 08 North Ring West Door", 22f, 30.5f),
            new("Node 09 North Ring East Door", 30f, 30.5f),
            new("Node 10 North Ring East", 36f, 30.5f),
            new("Node 11 East Ring North Corner", 42.5f, 30.5f),
            new("Node 12 East Ring North", 42.5f, 23f),
            new("Node 13 East Ring Cross", 42.5f, 16f),
            new("Node 14 East Ring South", 42.5f, 9f),
            new("Node 15 East Ring South Corner", 42.5f, 1.5f),
            new("Node 16 South Ring East", 36f, 1.5f),
            new("Node 17 South Ring East Door", 30f, 1.5f),
            new("Node 18 South Ring West Door", 22f, 1.5f),
            new("Node 19 South Ring West", 16f, 1.5f),
            new("Node 20 North Room West", 22f, 24.5f),
            new("Node 21 North Room Center", 26f, 24.5f),
            new("Node 22 North Room East", 30f, 24.5f),
            new("Node 23 South Room West", 22f, 7.5f),
            new("Node 24 South Room Center", 26f, 7.5f),
            new("Node 25 South Room East", 30f, 7.5f),
            new("Node 26 SW Safe Junction", 15f, 16f),
            new("Node 27 SW Safe", 15f, 7.5f, isSafeZone: true),
            new("Node 28 NE Safe Junction", 37f, 16f),
            new("Node 29 NE Safe", 37f, 24.5f, isSafeZone: true),
            new("Node 30 Hunting SW", 21f, 13f),
            new("Node 31 Hunting S", 26f, 13f),
            new("Node 32 Hunting SE", 31f, 13f),
            new("Node 33 Hunting W", 21f, 16f),
            new("Node 34 Hunting Center", 26f, 16f),
            new("Node 35 Hunting E", 31f, 16f),
            new("Node 36 Hunting NW", 21f, 19f),
            new("Node 37 Hunting N", 26f, 19f),
            new("Node 38 Hunting NE", 31f, 19f),
            new("Node 39 Exit Door", 45f, 16f),
            new("Node 40 Exit", 49f, 16f),
        };

        public static readonly (int a, int b)[] Links =
        {
            (0, 1), (1, 4), (4, 26), (26, 33), (35, 28), (28, 13), (13, 39), (39, 40),
            (26, 27), (28, 29),
            (2, 3), (3, 4), (4, 5), (5, 6),
            (6, 7), (7, 8), (8, 9), (9, 10), (10, 11),
            (11, 12), (12, 13), (13, 14), (14, 15),
            (15, 16), (16, 17), (17, 18), (18, 19), (19, 2),
            (8, 20), (9, 22), (20, 21), (21, 22),
            (18, 23), (17, 25), (23, 24), (24, 25),
            (30, 31), (31, 32), (33, 34), (34, 35), (36, 37), (37, 38),
            (30, 33), (33, 36), (31, 34), (34, 37), (32, 35), (35, 38),
        };

        public static readonly RouteSpec[] Routes =
        {
            new("Wolf L1 West Ring", PatrolMode.PingPong, 6, 5, 4, 3, 2),
            new("Wolf L2 North Room", PatrolMode.Loop, 8, 9, 22, 21, 20),
            new("Wolf L3 East Ring", PatrolMode.PingPong, 15, 14, 13, 12, 11),
            new("Wolf L4 South Room", PatrolMode.Loop, 17, 18, 23, 24, 25),
            new("Wolf L5 North Ring", PatrolMode.PingPong, 7, 8),
            new("Wolf L6 South Ring", PatrolMode.PingPong, 16, 17),
        };

        public static readonly Vector2 HunterStart = new(3f, 16f);

        public static readonly Vector2[] BoidSpawns =
        {
            new(24f, 15f), new(26f, 15f), new(28f, 15f),
            new(24f, 17f), new(26f, 17f), new(28f, 17f),
        };

        public static readonly Vector2 CameraCenter = new(26f, 16f);
        public const float CameraOrthographicSize = 18f;
    }
}

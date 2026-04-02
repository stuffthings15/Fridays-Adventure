using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Fridays_Adventure.Data
{
    public static class ContentLoader
    {
        private static readonly string DataDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Data");

        public static IslandDefinition LoadIsland(string id)
        {
            string path = Path.Combine(DataDir, id + ".json");
            if (File.Exists(path))
            {
                try { return Deserialize(path); } catch { }
            }
            return GetBuiltIn(id);
        }

        public static void SaveIsland(IslandDefinition def)
        {
            try
            {
                Directory.CreateDirectory(DataDir);
                string path = Path.Combine(DataDir, def.Id + ".json");
                var ser = new DataContractJsonSerializer(typeof(IslandDefinition));
                using (var fs = new FileStream(path, FileMode.Create))
                    ser.WriteObject(fs, def);
            }
            catch { }
        }

        public static IReadOnlyList<string> BuiltInIds() =>
            new[] { "dino", "wano", "sky", "harbor", "coral", "tundra",
                    "sunken_gate", "kelp", "boiling_vent", "abyss" };

        private static IslandDefinition Deserialize(string path)
        {
            var ser = new DataContractJsonSerializer(typeof(IslandDefinition));
            using (var fs = File.OpenRead(path))
                return (IslandDefinition)ser.ReadObject(fs);
        }

        private static IslandDefinition GetBuiltIn(string id)
        {
            switch (id)
            {
                case "dino":   return BuildDino();
                case "wano":   return BuildWano();
                case "sky":    return BuildSky();
                case "harbor":       return BuildHarbor();
                case "coral":        return BuildCoral();
                case "tundra":       return BuildTundra();
                case "sunken_gate":  return BuildSunkenGate();
                case "kelp":         return BuildKelpLabyrinth();
                case "boiling_vent": return BuildBoilingVentRuins();
                case "abyss":        return BuildAbyssEngine();
                default:             return new IslandDefinition { Id = id, Name = id };
            }
        }

        private static IslandDefinition BuildDino() => new IslandDefinition
        {
            Id = "dino", Name = "Dinosaur Island",
            BackgroundSprite = "bg_dinoIsland.png", Music = "music_island1.mp3",
            LevelWidth = 2800, GroundY = 440, BountyReward = 500,
            CompletionFlag = NarrativeFlags.DinoComplete,
            Platforms = new List<PlatformDef>
            {
                new PlatformDef { X=0,    Y=440, W=700,  H=160 },
                new PlatformDef { X=800,  Y=440, W=560,  H=160 },
                new PlatformDef { X=1440, Y=440, W=600,  H=160 },
                new PlatformDef { X=2120, Y=440, W=680,  H=160 },
                new PlatformDef { X=350,  Y=330, W=200,  H=20  },
                new PlatformDef { X=1050, Y=350, W=220,  H=20  },
                new PlatformDef { X=1700, Y=320, W=180,  H=20  },
                new PlatformDef { X=2300, Y=300, W=260,  H=20  },
            },
            Hazards = new List<HazardDef>
            {
                new HazardDef { HazardType="WaterPit",    X=700,  Y=440, W=100, H=160 },
                new HazardDef { HazardType="WaterPit",    X=1360, Y=440, W=80,  H=160 },
                new HazardDef { HazardType="WaterPit",    X=2040, Y=440, W=80,  H=160 },
                new HazardDef { HazardType="SeaStoneZone",X=1500, Y=390, W=220, H=50  },
                new HazardDef { HazardType="FireSource",  X=1780, Y=392, W=40,  H=48  },
                new HazardDef { HazardType="FireSource",  X=2250, Y=392, W=40,  H=48  },
            },
            Enemies = new List<EnemyDef>
            {
                new EnemyDef { X=200,  Y=398, Difficulty=0.6f },
                new EnemyDef { X=550,  Y=398, Difficulty=0.6f },
                new EnemyDef { X=950,  Y=398, Difficulty=0.85f },
                new EnemyDef { X=1200, Y=398, Difficulty=0.85f },
                new EnemyDef { X=1650, Y=398, Difficulty=1.0f },
                new EnemyDef { X=2200, Y=398, Difficulty=1.0f, IsBoss=true, Hp=120 },
            },
            ExitX = 2720
        };

        private static IslandDefinition BuildWano() => new IslandDefinition
        {
            Id = "wano", Name = "Blade Nation",
            BackgroundSprite = "bg_bladenation.png", Music = "music_island1.mp3",
            LevelWidth = 2600, GroundY = 440, BountyReward = 500,
            CompletionFlag = NarrativeFlags.WanoComplete,
            Platforms = new List<PlatformDef>
            {
                new PlatformDef { X=0,    Y=440, W=500,  H=160 },
                new PlatformDef { X=560,  Y=440, W=480,  H=160 },
                new PlatformDef { X=1100, Y=440, W=520,  H=160 },
                new PlatformDef { X=1680, Y=440, W=920,  H=160 },
                new PlatformDef { X=300,  Y=310, W=180,  H=20  },
                new PlatformDef { X=800,  Y=330, W=200,  H=20  },
                new PlatformDef { X=1350, Y=310, W=160,  H=20  },
                new PlatformDef { X=2000, Y=290, W=240,  H=20  },
            },
            Hazards = new List<HazardDef>
            {
                new HazardDef { HazardType="SeaStoneZone",X=1680, Y=380, W=280, H=60 },
                new HazardDef { HazardType="SeaStoneZone",X=2100, Y=380, W=200, H=60 },
                new HazardDef { HazardType="FireSource",  X=100,  Y=392, W=36,  H=48 },
                new HazardDef { HazardType="FireSource",  X=450,  Y=392, W=36,  H=48 },
                new HazardDef { HazardType="FireSource",  X=800,  Y=392, W=36,  H=48 },
                new HazardDef { HazardType="FireSource",  X=1150, Y=392, W=36,  H=48 },
            },
            Enemies = new List<EnemyDef>
            {
                new EnemyDef { X=250,  Y=392, Difficulty=1.0f, Hp=70  },
                new EnemyDef { X=700,  Y=392, Difficulty=1.0f, Hp=70  },
                new EnemyDef { X=1200, Y=392, Difficulty=1.2f, Hp=90  },
                new EnemyDef { X=1500, Y=392, Difficulty=1.2f, Hp=90  },
                new EnemyDef { X=1900, Y=392, Difficulty=1.4f, Hp=110 },
                new EnemyDef { X=2300, Y=392, Difficulty=1.5f, Hp=150, IsBoss=true },
            },
            ExitX = 2520
        };

        private static IslandDefinition BuildSky() => new IslandDefinition
        {
            Id = "sky", Name = "Sky Island",
            BackgroundSprite = "bg_skyisland.png", Music = "music_island1.mp3",
            LevelWidth = 900, GroundY = 3140, BountyReward = 600,
            CompletionFlag = NarrativeFlags.SkyComplete,
            Platforms = new List<PlatformDef>(),
            Hazards   = new List<HazardDef>(),
            Enemies   = new List<EnemyDef>(),
            ExitX = 450
        };

        // ── Sequel islands ────────────────────────────────────────────────

        private static IslandDefinition BuildHarbor() => new IslandDefinition
        {
            Id = "harbor", Name = "Harbor Town",
            BackgroundSprite = "bg_island.png", Music = "music_hub1.mp3",
            LevelWidth = 2200, GroundY = 440, BountyReward = 600,
            CompletionFlag = NarrativeFlags.HarborComplete,
            Platforms = new List<PlatformDef>
            {
                new PlatformDef { X=0,    Y=440, W=480,  H=160 },
                new PlatformDef { X=560,  Y=440, W=440,  H=160 },
                new PlatformDef { X=1080, Y=440, W=500,  H=160 },
                new PlatformDef { X=1660, Y=440, W=540,  H=160 },
                new PlatformDef { X=280,  Y=340, W=180,  H=20  },
                new PlatformDef { X=750,  Y=320, W=200,  H=20  },
                new PlatformDef { X=1300, Y=300, W=180,  H=20  },
            },
            Hazards = new List<HazardDef>
            {
                new HazardDef { HazardType="WaterPit",    X=480,  Y=440, W=80,  H=160 },
                new HazardDef { HazardType="WaterPit",    X=1000, Y=440, W=80,  H=160 },
                new HazardDef { HazardType="WaterPit",    X=1580, Y=440, W=80,  H=160 },
                new HazardDef { HazardType="SeaStoneZone",X=900,  Y=390, W=160, H=50  },
                new HazardDef { HazardType="SeaStoneZone",X=1400, Y=390, W=140, H=50  },
            },
            Enemies = new List<EnemyDef>
            {
                new EnemyDef { X=180,  Y=398, Difficulty=0.7f  },
                new EnemyDef { X=450,  Y=398, Difficulty=0.7f  },
                new EnemyDef { X=850,  Y=398, Difficulty=0.9f  },
                new EnemyDef { X=1150, Y=398, Difficulty=1.0f  },
                new EnemyDef { X=1500, Y=398, Difficulty=1.1f  },
                new EnemyDef { X=1850, Y=398, Difficulty=1.2f, IsBoss=true, Hp=140 },
            },
            ExitX = 2100
        };

        private static IslandDefinition BuildCoral() => new IslandDefinition
        {
            Id = "coral", Name = "Coral Reef",
            BackgroundSprite = "bg_island.png", Music = "music_exploration1.mp3",
            LevelWidth = 3000, GroundY = 440, BountyReward = 750,
            CompletionFlag = NarrativeFlags.CoralComplete,
            Platforms = new List<PlatformDef>
            {
                new PlatformDef { X=0,    Y=440, W=500,  H=160 },
                new PlatformDef { X=600,  Y=440, W=460,  H=160 },
                new PlatformDef { X=1160, Y=440, W=480,  H=160 },
                new PlatformDef { X=1740, Y=440, W=520,  H=160 },
                new PlatformDef { X=2360, Y=440, W=640,  H=160 },
                new PlatformDef { X=320,  Y=340, W=180,  H=20  },
                new PlatformDef { X=880,  Y=320, W=200,  H=20  },
                new PlatformDef { X=1460, Y=300, W=180,  H=20  },
                new PlatformDef { X=2050, Y=280, W=200,  H=20  },
            },
            Hazards = new List<HazardDef>
            {
                new HazardDef { HazardType="WaterPit",    X=500,  Y=440, W=100, H=160 },
                new HazardDef { HazardType="WaterPit",    X=1060, Y=440, W=100, H=160 },
                new HazardDef { HazardType="WaterPit",    X=1640, Y=440, W=100, H=160 },
                new HazardDef { HazardType="WaterPit",    X=2260, Y=440, W=100, H=160 },
                new HazardDef { HazardType="SeaStoneZone",X=620,  Y=385, W=240, H=55  },
                new HazardDef { HazardType="SeaStoneZone",X=1180, Y=385, W=220, H=55  },
                new HazardDef { HazardType="SeaStoneZone",X=1760, Y=385, W=260, H=55  },
                new HazardDef { HazardType="SeaStoneZone",X=2380, Y=385, W=200, H=55  },
                new HazardDef { HazardType="FireSource",  X=1000, Y=392, W=40,  H=48  },
                new HazardDef { HazardType="FireSource",  X=2100, Y=392, W=40,  H=48  },
            },
            Enemies = new List<EnemyDef>
            {
                new EnemyDef { X=200,  Y=398, Difficulty=1.0f, Hp=70  },
                new EnemyDef { X=480,  Y=398, Difficulty=1.0f, Hp=70  },
                new EnemyDef { X=900,  Y=398, Difficulty=1.2f, Hp=90  },
                new EnemyDef { X=1250, Y=398, Difficulty=1.2f, Hp=90  },
                new EnemyDef { X=1600, Y=398, Difficulty=1.4f, Hp=110 },
                new EnemyDef { X=1950, Y=398, Difficulty=1.4f, Hp=110 },
                new EnemyDef { X=2550, Y=398, Difficulty=1.6f, Hp=160, IsBoss=true },
            },
            ExitX = 2900
        };

        private static IslandDefinition BuildTundra() => new IslandDefinition
        {
            Id = "tundra", Name = "Tundra Peak",
            BackgroundSprite = "bg_skyisland.png", Music = "music_exploration2.mp3",
            LevelWidth = 2600, GroundY = 440, BountyReward = 800,
            CompletionFlag = NarrativeFlags.TundraComplete,
            Platforms = new List<PlatformDef>
            {
                new PlatformDef { X=0,    Y=440, W=460,  H=160 },
                new PlatformDef { X=540,  Y=440, W=500,  H=160 },
                new PlatformDef { X=1120, Y=440, W=480,  H=160 },
                new PlatformDef { X=1700, Y=440, W=900,  H=160 },
                new PlatformDef { X=260,  Y=300, W=180,  H=20  },
                new PlatformDef { X=760,  Y=280, W=200,  H=20  },
                new PlatformDef { X=1320, Y=260, W=160,  H=20  },
                new PlatformDef { X=1900, Y=240, W=260,  H=20  },
            },
            Hazards = new List<HazardDef>
            {
                new HazardDef { HazardType="WaterPit",    X=460,  Y=440, W=80,  H=160 },
                new HazardDef { HazardType="WaterPit",    X=1040, Y=440, W=80,  H=160 },
                new HazardDef { HazardType="SeaStoneZone",X=1720, Y=380, W=240, H=60  },
                new HazardDef { HazardType="SeaStoneZone",X=2100, Y=380, W=200, H=60  },
                new HazardDef { HazardType="FireSource",  X=120,  Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=500,  Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=900,  Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=1250, Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=1800, Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=2200, Y=392, W=36,  H=48  },
            },
            Enemies = new List<EnemyDef>
            {
                new EnemyDef { X=220,  Y=392, Difficulty=1.2f, Hp=90  },
                new EnemyDef { X=600,  Y=392, Difficulty=1.2f, Hp=90  },
                new EnemyDef { X=1000, Y=392, Difficulty=1.4f, Hp=110 },
                new EnemyDef { X=1300, Y=392, Difficulty=1.5f, Hp=120 },
                new EnemyDef { X=1850, Y=392, Difficulty=1.6f, Hp=130 },
                new EnemyDef { X=2300, Y=392, Difficulty=1.8f, Hp=180, IsBoss=true },
            },
            ExitX = 2520
        };

        // ── Underwater chapter (Tide of the Lost) ────────────────────────────────

        private static IslandDefinition BuildSunkenGate() => new IslandDefinition
        {
            Id = "sunken_gate", Name = "Sunken Gate",
            BackgroundSprite = "bg_island.png", Music = "music_exploration1.mp3",
            LevelWidth = 1800, GroundY = 440, BountyReward = 500,
            CompletionFlag = NarrativeFlags.SunkenGateComplete,
            Platforms = new List<PlatformDef>
            {
                new PlatformDef { X=0,    Y=440, W=500,  H=160 },
                new PlatformDef { X=580,  Y=440, W=460,  H=160 },
                new PlatformDef { X=1120, Y=440, W=680,  H=160 },
                new PlatformDef { X=300,  Y=330, W=160,  H=20  },
                new PlatformDef { X=800,  Y=310, W=180,  H=20  },
            },
            Hazards = new List<HazardDef>
            {
                new HazardDef { HazardType="WaterPit",    X=500,  Y=440, W=80,  H=160 },
                new HazardDef { HazardType="WaterPit",    X=1040, Y=440, W=80,  H=160 },
                new HazardDef { HazardType="SeaStoneZone",X=700,  Y=385, W=180, H=55  },
            },
            Enemies = new List<EnemyDef>
            {
                new EnemyDef { X=200,  Y=398, Difficulty=0.6f },
                new EnemyDef { X=480,  Y=398, Difficulty=0.7f },
                new EnemyDef { X=850,  Y=398, Difficulty=0.8f },
                new EnemyDef { X=1200, Y=398, Difficulty=0.9f },
                new EnemyDef { X=1500, Y=398, Difficulty=1.0f, IsBoss=true, Hp=100 },
            },
            ExitX = 1720
        };

        private static IslandDefinition BuildKelpLabyrinth() => new IslandDefinition
        {
            Id = "kelp", Name = "Kelp Labyrinth",
            BackgroundSprite = "bg_island.png", Music = "music_exploration1.mp3",
            LevelWidth = 2800, GroundY = 440, BountyReward = 650,
            CompletionFlag = NarrativeFlags.KelpComplete,
            Platforms = new List<PlatformDef>
            {
                new PlatformDef { X=0,    Y=440, W=480,  H=160 },
                new PlatformDef { X=560,  Y=440, W=440,  H=160 },
                new PlatformDef { X=1080, Y=440, W=500,  H=160 },
                new PlatformDef { X=1660, Y=440, W=500,  H=160 },
                new PlatformDef { X=2240, Y=440, W=560,  H=160 },
                new PlatformDef { X=280,  Y=320, W=160,  H=20  },
                new PlatformDef { X=780,  Y=300, W=180,  H=20  },
                new PlatformDef { X=1360, Y=280, W=160,  H=20  },
                new PlatformDef { X=1940, Y=260, W=200,  H=20  },
            },
            Hazards = new List<HazardDef>
            {
                new HazardDef { HazardType="WaterPit",    X=480,  Y=440, W=80,  H=160 },
                new HazardDef { HazardType="WaterPit",    X=1000, Y=440, W=80,  H=160 },
                new HazardDef { HazardType="WaterPit",    X=1580, Y=440, W=80,  H=160 },
                new HazardDef { HazardType="SeaStoneZone",X=560,  Y=385, W=200, H=55  },
                new HazardDef { HazardType="SeaStoneZone",X=1080, Y=385, W=220, H=55  },
                new HazardDef { HazardType="SeaStoneZone",X=1660, Y=385, W=200, H=55  },
                new HazardDef { HazardType="SeaStoneZone",X=2240, Y=385, W=180, H=55  },
                new HazardDef { HazardType="FireSource",  X=900,  Y=392, W=36,  H=48  },
            },
            Enemies = new List<EnemyDef>
            {
                new EnemyDef { X=200,  Y=398, Difficulty=0.9f, Hp=65  },
                new EnemyDef { X=480,  Y=398, Difficulty=0.9f, Hp=65  },
                new EnemyDef { X=900,  Y=398, Difficulty=1.1f, Hp=80  },
                new EnemyDef { X=1200, Y=398, Difficulty=1.1f, Hp=80  },
                new EnemyDef { X=1700, Y=398, Difficulty=1.3f, Hp=100 },
                new EnemyDef { X=2400, Y=398, Difficulty=1.4f, Hp=130, IsBoss=true },
            },
            ExitX = 2720
        };

        private static IslandDefinition BuildBoilingVentRuins() => new IslandDefinition
        {
            Id = "boiling_vent", Name = "Boiling Vent Ruins",
            BackgroundSprite = "bg_skyisland.png", Music = "music_exploration2.mp3",
            LevelWidth = 2600, GroundY = 440, BountyReward = 700,
            CompletionFlag = NarrativeFlags.BoilingVentComplete,
            Platforms = new List<PlatformDef>
            {
                new PlatformDef { X=0,    Y=440, W=460,  H=160 },
                new PlatformDef { X=540,  Y=440, W=480,  H=160 },
                new PlatformDef { X=1100, Y=440, W=520,  H=160 },
                new PlatformDef { X=1700, Y=440, W=900,  H=160 },
                new PlatformDef { X=260,  Y=310, W=160,  H=20  },
                new PlatformDef { X=760,  Y=290, W=180,  H=20  },
                new PlatformDef { X=1340, Y=270, W=160,  H=20  },
                new PlatformDef { X=1980, Y=250, W=220,  H=20  },
            },
            Hazards = new List<HazardDef>
            {
                new HazardDef { HazardType="WaterPit",    X=460,  Y=440, W=80,  H=160 },
                new HazardDef { HazardType="WaterPit",    X=1020, Y=440, W=80,  H=160 },
                new HazardDef { HazardType="SeaStoneZone",X=1700, Y=380, W=240, H=60  },
                new HazardDef { HazardType="SeaStoneZone",X=2160, Y=380, W=200, H=60  },
                new HazardDef { HazardType="FireSource",  X=80,   Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=420,  Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=820,  Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=1200, Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=1820, Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=2260, Y=392, W=36,  H=48  },
            },
            Enemies = new List<EnemyDef>
            {
                new EnemyDef { X=220,  Y=398, Difficulty=1.1f, Hp=80  },
                new EnemyDef { X=580,  Y=398, Difficulty=1.2f, Hp=90  },
                new EnemyDef { X=980,  Y=398, Difficulty=1.3f, Hp=100 },
                new EnemyDef { X=1300, Y=398, Difficulty=1.4f, Hp=110 },
                new EnemyDef { X=1860, Y=398, Difficulty=1.5f, Hp=120 },
                new EnemyDef { X=2350, Y=398, Difficulty=1.6f, Hp=150, IsBoss=true },
            },
            ExitX = 2520
        };

        private static IslandDefinition BuildAbyssEngine() => new IslandDefinition
        {
            Id = "abyss", Name = "Abyss Engine",
            BackgroundSprite = "bg_island.png", Music = "music_finale1.mp3",
            LevelWidth = 3200, GroundY = 440, BountyReward = 900,
            CompletionFlag = NarrativeFlags.AbyssComplete,
            Platforms = new List<PlatformDef>
            {
                new PlatformDef { X=0,    Y=440, W=480,  H=160 },
                new PlatformDef { X=580,  Y=440, W=440,  H=160 },
                new PlatformDef { X=1100, Y=440, W=480,  H=160 },
                new PlatformDef { X=1660, Y=440, W=480,  H=160 },
                new PlatformDef { X=2220, Y=440, W=980,  H=160 },
                new PlatformDef { X=280,  Y=310, W=160,  H=20  },
                new PlatformDef { X=800,  Y=290, W=180,  H=20  },
                new PlatformDef { X=1380, Y=270, W=160,  H=20  },
                new PlatformDef { X=1960, Y=250, W=180,  H=20  },
                new PlatformDef { X=2540, Y=230, W=200,  H=20  },
            },
            Hazards = new List<HazardDef>
            {
                new HazardDef { HazardType="WaterPit",    X=480,  Y=440, W=100, H=160 },
                new HazardDef { HazardType="WaterPit",    X=1000, Y=440, W=100, H=160 },
                new HazardDef { HazardType="WaterPit",    X=1560, Y=440, W=100, H=160 },
                new HazardDef { HazardType="WaterPit",    X=2120, Y=440, W=100, H=160 },
                new HazardDef { HazardType="SeaStoneZone",X=600,  Y=380, W=220, H=60  },
                new HazardDef { HazardType="SeaStoneZone",X=1120, Y=380, W=240, H=60  },
                new HazardDef { HazardType="SeaStoneZone",X=1680, Y=380, W=220, H=60  },
                new HazardDef { HazardType="SeaStoneZone",X=2240, Y=380, W=200, H=60  },
                new HazardDef { HazardType="FireSource",  X=100,  Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=760,  Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=1300, Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=1860, Y=392, W=36,  H=48  },
                new HazardDef { HazardType="FireSource",  X=2420, Y=392, W=36,  H=48  },
            },
            Enemies = new List<EnemyDef>
            {
                new EnemyDef { X=200,  Y=398, Difficulty=1.3f, Hp=100 },
                new EnemyDef { X=500,  Y=398, Difficulty=1.4f, Hp=110 },
                new EnemyDef { X=900,  Y=398, Difficulty=1.5f, Hp=120 },
                new EnemyDef { X=1250, Y=398, Difficulty=1.5f, Hp=120 },
                new EnemyDef { X=1700, Y=398, Difficulty=1.7f, Hp=140 },
                new EnemyDef { X=2050, Y=398, Difficulty=1.7f, Hp=140 },
                new EnemyDef { X=2500, Y=398, Difficulty=2.0f, Hp=200, IsBoss=true },
            },
            ExitX = 3100
        };
    }
}

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
            new[] { "dino", "wano", "sky" };

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
                case "dino": return BuildDino();
                case "wano": return BuildWano();
                case "sky":  return BuildSky();
                default:     return new IslandDefinition { Id = id, Name = id };
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
    }
}

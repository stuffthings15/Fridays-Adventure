using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Fridays_Adventure.Data
{
    [DataContract]
    public class PlatformDef
    {
        [DataMember] public int X { get; set; }
        [DataMember] public int Y { get; set; }
        [DataMember] public int W { get; set; }
        [DataMember] public int H { get; set; }
    }

    [DataContract]
    public class HazardDef
    {
        [DataMember] public string HazardType { get; set; }  // WaterPit | SeaStoneZone | FireSource
        [DataMember] public int X { get; set; }
        [DataMember] public int Y { get; set; }
        [DataMember] public int W { get; set; }
        [DataMember] public int H { get; set; }
    }

    [DataContract]
    public class EnemyDef
    {
        [DataMember] public int   X          { get; set; }
        [DataMember] public int   Y          { get; set; }
        [DataMember] public float Difficulty { get; set; } = 1f;
        [DataMember] public bool  IsBoss     { get; set; }
        [DataMember] public int   Hp         { get; set; }
    }

    [DataContract]
    public class IslandDefinition
    {
        [DataMember] public string             Id               { get; set; }
        [DataMember] public string             Name             { get; set; }
        [DataMember] public string             BackgroundSprite { get; set; }
        [DataMember] public string             Music            { get; set; }
        [DataMember] public int                LevelWidth       { get; set; } = 2800;
        [DataMember] public int                GroundY          { get; set; } = 440;
        [DataMember] public List<PlatformDef>  Platforms        { get; set; } = new List<PlatformDef>();
        [DataMember] public List<HazardDef>    Hazards          { get; set; } = new List<HazardDef>();
        [DataMember] public List<EnemyDef>     Enemies          { get; set; } = new List<EnemyDef>();
        [DataMember] public int                ExitX            { get; set; }
        [DataMember] public int                BountyReward     { get; set; } = 500;
        [DataMember] public string             DialogueId       { get; set; }
        [DataMember] public string             CompletionFlag   { get; set; }
    }
}

namespace Fridays_Adventure.Data
{
    public enum WarlordType { FireLord, SeaStoneLord, StormLord }

    public sealed class WarlordConfig
    {
        public string      Name        { get; }
        public WarlordType Type        { get; }
        public int         MaxHp       { get; }
        public int         Phase2Hp    { get; }
        public int         BaseDamage  { get; }
        public float       MoveSpeed   { get; }
        public string      TauntP1     { get; }
        public string      TauntP2     { get; }
        public string      Epithet     { get; }

        public WarlordConfig(string name, WarlordType type, int maxHp,
                             int baseDmg, float speed,
                             string tauntP1, string tauntP2, string epithet)
        {
            Name       = name;
            Type       = type;
            MaxHp      = maxHp;
            Phase2Hp   = maxHp / 2;
            BaseDamage = baseDmg;
            MoveSpeed  = speed;
            TauntP1    = tauntP1;
            TauntP2    = tauntP2;
            Epithet    = epithet;
        }

        // Pre-authored Warlords
        public static WarlordConfig FireLordSudo() => new WarlordConfig(
            "Lord Sudo",        WarlordType.FireLord,
            maxHp:   250,       baseDmg: 20, speed: 140f,
            tauntP1: "SUDO: \"Your ice is nothing before my flames!\"",
            tauntP2: "SUDO: \"BURN — everything you've built!\"",
            epithet: "The Ash Warlord");

        public static WarlordConfig SeaStoneLordVex() => new WarlordConfig(
            "Lord Vex",         WarlordType.SeaStoneLord,
            maxHp:   220,       baseDmg: 18, speed: 110f,
            tauntP1: "VEX: \"Your Devil Fruit is worthless here, pirate.\"",
            tauntP2: "VEX: \"Drown in SeaStone — helpless as always!\"",
            epithet: "The Suppressor");
    }
}

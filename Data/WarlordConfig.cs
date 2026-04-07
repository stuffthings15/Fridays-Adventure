namespace Fridays_Adventure.Data
{
    public enum WarlordType { FireLord, SeaStoneLord, StormLord, CentipedeLord }

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

        /// <summary>
        /// Bounty reward (in berries) granted when this warlord is defeated.
        /// Scaled to 15× MaxHp so harder warlords yield higher rewards.
        /// </summary>
        public int BountyReward => MaxHp * 15;

        /// <summary>
        /// Save-flag key recorded when this warlord is defeated.
        /// Derived from the warlord's name for uniqueness across saves.
        /// </summary>
        public string DefeatFlag => Name.ToLower().Replace(' ', '_') + "_defeated";

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

        public static WarlordConfig StormLordVanta() => new WarlordConfig(
            "Lord Vanta",       WarlordType.StormLord,
            maxHp:   300,       baseDmg: 22, speed: 160f,
            tauntP1: "VANTA: \"The storm bends to my will. You cannot outrun lightning!\"",
            tauntP2: "VANTA: \"I AM THE STORM — and you are already lost!\"",
            epithet: "The Tempest Warlord");

        public static WarlordConfig CentipedeOfTheDeep() => new WarlordConfig(
            "The Centipede",    WarlordType.CentipedeLord,
            maxHp:   350,       baseDmg: 22, speed: 160f,
            tauntP1: "CENTIPEDE: \"I am built from every foe you have defeated. You cannot break what cannot die!\"",
            tauntP2: "CENTIPEDE: \"ALL FORMS, UNITE — crush her into the deep!\"",
            epithet: "The Deep Amalgam");
    }
}

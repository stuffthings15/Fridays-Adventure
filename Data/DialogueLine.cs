using System;
using System.Collections.Generic;

namespace Fridays_Adventure.Data
{
    /// <summary>
    /// A single line of dialogue spoken by a character.
    /// Optionally includes a portrait image filename.
    /// </summary>
    public sealed class DialogueLine
    {
        /// <summary>Name of the speaker shown above the text box.</summary>
        public string Speaker  { get; }

        /// <summary>Dialogue text displayed with a typing effect.</summary>
        public string Text     { get; }

        /// <summary>
        /// Optional filename of a portrait image (e.g. "portrait_friday.png").
        /// When null, no portrait is rendered for this line.
        /// </summary>
        public string Portrait { get; }

        /// <summary>Creates a dialogue line with an optional portrait.</summary>
        public DialogueLine(string speaker, string text, string portrait = null)
        {
            Speaker  = speaker;
            Text     = text;
            Portrait = portrait;
        }
    }

    public sealed class DialogueChoice
    {
        public string Text       { get; }
        public int    BondChange { get; }
        public string FlagToSet  { get; }
        public DialogueChoice(string text, int bondChange = 0, string flagToSet = null)
        { Text = text; BondChange = bondChange; FlagToSet = flagToSet; }
    }

    public sealed class DialogueSequence
    {
        public List<DialogueLine>   Lines   { get; } = new List<DialogueLine>();
        public List<DialogueChoice> Choices { get; } = new List<DialogueChoice>();
        public Action<int>          OnDone  { get; set; }   // called with chosen index (-1 if no choice)
    }

    // ── Pre-authored sequences ───────────────────────────────────────────────

    public static class Dialogues
    {
        /// <summary>
        /// Returns the display name for the currently selected playable character,
        /// ensuring all dialogue sequences reference the active crew member.
        /// </summary>
        private static string PlayerName
        {
            get
            {
                switch (Fridays_Adventure.Engine.Game.Instance.SelectedCharacter)
                {
                    case Fridays_Adventure.Engine.PlayableCharacter.Orca: return "ORCA";
                    case Fridays_Adventure.Engine.PlayableCharacter.Swan: return "SWAN";
                    default: return "MISS FRIDAY";
                }
            }
        }

        public static DialogueSequence MeetFinn() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("FINN",        "Hey — you made it off that island alive. Most people don't."),
                new DialogueLine(PlayerName,     "..."),
                new DialogueLine("FINN",        "I'm Finn. You don't have to talk. But you're on our ship now."),
                new DialogueLine(PlayerName,     "I didn't ask to be."),
                new DialogueLine("FINN",        "No. But you're here. That counts for something.")
            },
            Choices = {
                new DialogueChoice("\"...Fine. I'll stay for now.\"",        bondChange: 2, flagToSet: "met_finn"),
                new DialogueChoice("\"Don't expect me to trust anyone.\"",   bondChange: 0, flagToSet: "met_finn_cold")
            }
        };

        public static DialogueSequence MeetAmelia() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("CAPTAIN AMELIA", "You survived Dinosaur Island with an Ice Devil Fruit? Impressive."),
                new DialogueLine(PlayerName,        "It was luck."),
                new DialogueLine("CAPTAIN AMELIA", "Luck runs out. Skills don't. I want you on this crew."),
                new DialogueLine(PlayerName,        "Why?"),
                new DialogueLine("CAPTAIN AMELIA", "Because you know what it means to have nothing. So do we.")
            },
            Choices = {
                new DialogueChoice("\"What's in it for me?\"",              bondChange: 1, flagToSet: "met_amelia"),
                new DialogueChoice("\"...I'll think about it.\"",           bondChange: 2, flagToSet: "met_amelia_warm")
            }
        };

        public static DialogueSequence MarineEncounter() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("MARINE CAPTAIN", "Halt! Sea Serpent pirates — by order of the World Government!"),
                new DialogueLine("CAPTAIN AMELIA", PlayerName + ". We need the path clear. Can you hold them?"),
                new DialogueLine(PlayerName,        "I'll freeze everything in reach."),
                new DialogueLine("FINN",           "Watch the water — don't let them corner you near the edge!")
            },
            Choices = {
                new DialogueChoice("\"I fight alone. Stand back.\"",        bondChange: 0),
                new DialogueChoice("\"Finn, cover my left flank.\"",        bondChange: 2, flagToSet: "used_combo")
            }
        };

        public static DialogueSequence BladeSamuriGate() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("BLADE GUARDIAN", "Outsiders are not welcome in Blade Nation. State your purpose."),
                new DialogueLine("CAPTAIN AMELIA", "We seek passage. Nothing more."),
                new DialogueLine("BLADE GUARDIAN", "Prove your worth in the trial of steel. Or turn back."),
                new DialogueLine(PlayerName,        "I'll handle this."),
                new DialogueLine("FINN",           PlayerName + " — there's SeaStone in the trial arena. Be careful.")
            },
            Choices = {
                new DialogueChoice("Accept the trial",                       bondChange: 1, flagToSet: "blade_trial"),
                new DialogueChoice("Find another way around",                bondChange: 0, flagToSet: "blade_bypass")
            }
        };

        public static DialogueSequence ZaraRescue() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("ZARA",         "Got you — don't you dare sink on my watch."),
                new DialogueLine(PlayerName,      "You... pulled me out."),
                new DialogueLine("ZARA",         "That's what crew does. Even for people who hate crew."),
                new DialogueLine(PlayerName,      "I don't hate you."),
                new DialogueLine("ZARA",         "High praise. I'll take it.")
            },
            Choices = {
                new DialogueChoice("\"Thank you, Zara.\"",                   bondChange: 3, flagToSet: "zara_bond"),
                new DialogueChoice("[Say nothing, but remember it]",         bondChange: 1, flagToSet: "zara_quiet")
            }
        };

        // ── Sequel sequences ─────────────────────────────────────────────────

        public static DialogueSequence MeetOrca() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("ORCA",        "You're the one they call " + PlayerName + "? You're smaller than I expected."),
                new DialogueLine(PlayerName,     "..."),
                new DialogueLine("ORCA",        "Relax. I meant it as a compliment. Small fighters are hard to hit."),
                new DialogueLine(PlayerName,     "Why are you following us?"),
                new DialogueLine("ORCA",        "Same reason as you, I'd guess. Nowhere else worth going.")
            },
            Choices = {
                new DialogueChoice("\"You can keep up if you want.\"",       bondChange: 2, flagToSet: NarrativeFlags.MetOrcaWarm),
                new DialogueChoice("\"We don't need the extra weight.\"",    bondChange: 0, flagToSet: NarrativeFlags.MetOrca)
            }
        };

        public static DialogueSequence OrcaJoinsCrew() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("CAPTAIN AMELIA", "Orca. You've proven yourself out there. The offer stands."),
                new DialogueLine("ORCA",           "I don't do crews. Never have."),
                new DialogueLine(PlayerName,        "Neither did I."),
                new DialogueLine("ORCA",           "...Fair point."),
                new DialogueLine("FINN",           "Welcome aboard. Try not to sink the ship.")
            },
            Choices = {
                new DialogueChoice("\"Glad you stayed.\"",                   bondChange: 3, flagToSet: NarrativeFlags.OrcaJoinedCrew),
                new DialogueChoice("[Nod once and walk away]",               bondChange: 1, flagToSet: NarrativeFlags.OrcaJoinedCrew)
            }
        };

        public static DialogueSequence MeetSwan() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("SWAN",        "You have an Ice Devil Fruit. I've never seen one in person."),
                new DialogueLine(PlayerName,     "It's not as impressive as it sounds."),
                new DialogueLine("SWAN",        "You froze three Marines in under a second. It's exactly as impressive as it sounds."),
                new DialogueLine(PlayerName,     "...What do you want?"),
                new DialogueLine("SWAN",        "To travel with someone who knows what they're doing, for once.")
            },
            Choices = {
                new DialogueChoice("\"You can handle yourself. You're in.\"", bondChange: 3, flagToSet: NarrativeFlags.MetSwanWarm),
                new DialogueChoice("\"Prove it first.\"",                     bondChange: 1, flagToSet: NarrativeFlags.MetSwan)
            }
        };

        public static DialogueSequence SwanJoinsCrew() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("SWAN",           "I've been thinking about what you said. About trust."),
                new DialogueLine(PlayerName,        "I said a lot of things."),
                new DialogueLine("SWAN",           "You said it doesn't come free. That you have to earn it every day."),
                new DialogueLine(PlayerName,        "I remember."),
                new DialogueLine("SWAN",           "Then I want to keep earning it. With your crew.")
            },
            Choices = {
                new DialogueChoice("\"Then you're one of us.\"",              bondChange: 4, flagToSet: NarrativeFlags.SwanJoinedCrew),
                new DialogueChoice("[Place a hand on her shoulder]",          bondChange: 3, flagToSet: NarrativeFlags.SwanJoinedCrew)
            }
        };
    }
}

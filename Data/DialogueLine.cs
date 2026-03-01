using System;
using System.Collections.Generic;

namespace Fridays_Adventure.Data
{
    public sealed class DialogueLine
    {
        public string Speaker { get; }
        public string Text    { get; }
        public DialogueLine(string speaker, string text) { Speaker = speaker; Text = text; }
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
        public static DialogueSequence MeetFinn() => new DialogueSequence
        {
            Lines = {
                new DialogueLine("FINN",        "Hey — you made it off that island alive. Most people don't."),
                new DialogueLine("MISS FRIDAY",  "..."),
                new DialogueLine("FINN",        "I'm Finn. You don't have to talk. But you're on our ship now."),
                new DialogueLine("MISS FRIDAY",  "I didn't ask to be."),
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
                new DialogueLine("MISS FRIDAY",    "It was luck."),
                new DialogueLine("CAPTAIN AMELIA", "Luck runs out. Skills don't. I want you on this crew."),
                new DialogueLine("MISS FRIDAY",    "Why?"),
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
                new DialogueLine("CAPTAIN AMELIA", "Friday. We need the path clear. Can you hold them?"),
                new DialogueLine("MISS FRIDAY",    "I'll freeze everything in reach."),
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
                new DialogueLine("MISS FRIDAY",    "I'll handle this."),
                new DialogueLine("FINN",           "Friday — there's SeaStone in the trial arena. Be careful.")
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
                new DialogueLine("MISS FRIDAY",   "You... pulled me out."),
                new DialogueLine("ZARA",         "That's what crew does. Even for people who hate crew."),
                new DialogueLine("MISS FRIDAY",   "I don't hate you."),
                new DialogueLine("ZARA",         "High praise. I'll take it.")
            },
            Choices = {
                new DialogueChoice("\"Thank you, Zara.\"",                   bondChange: 3, flagToSet: "zara_bond"),
                new DialogueChoice("[Say nothing, but remember it]",         bondChange: 1, flagToSet: "zara_quiet")
            }
        };
    }
}

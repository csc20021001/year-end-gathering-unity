using System;
using UnityEngine;

namespace YearEnd
{
    public enum DialogueSpeaker2D { Protagonist, Grandma, Brewer, RiceMaster }

    [Serializable]
    public sealed class DialogueLine2D
    {
        public DialogueSpeaker2D speaker;
        [TextArea(2, 12)] public string text;
    }

    [Serializable]
    public sealed class DialogueSequence2D
    {
        public string id;
        public DialogueLine2D[] lines = Array.Empty<DialogueLine2D>();
    }

    [CreateAssetMenu(fileName = "DialogueScriptV1", menuName = "歲末拾遺/劇情對話劇本")]
    public sealed class DialogueScript2D : ScriptableObject
    {
        public Sprite protagonistPortrait;
        public Sprite grandmaPortrait;
        public Sprite brewerPortrait;
        public Sprite riceMasterPortrait;
        [Range(5f, 80f)] public float charactersPerSecond = 28f;
        [Range(0f, 0.4f)] public float commaPause = .10f;
        [Range(0f, 0.7f)] public float sentencePause = .22f;

        public AudioClip typingSound;

        [Range(0f, 1f)]
        public float typingSoundVolume = 0.2f;

        [Range(1, 5)]
        public int typingSoundEveryNCharacters = 2;

        public DialogueSequence2D[] conversations = Array.Empty<DialogueSequence2D>();

        public DialogueSequence2D Find(string key)
        {
            if (conversations == null) return null;
            foreach (var entry in conversations)
                if (entry != null && entry.id == key) return entry;
            return null;
        }

        public Sprite Portrait(DialogueSpeaker2D speaker)
        {
            switch (speaker)
            {
                case DialogueSpeaker2D.Grandma: return grandmaPortrait;
                case DialogueSpeaker2D.Brewer: return brewerPortrait;
                case DialogueSpeaker2D.RiceMaster: return riceMasterPortrait;
                default: return protagonistPortrait;
            }
        }
        public string Name(DialogueSpeaker2D speaker)
        {
            switch (speaker)
            {
                case DialogueSpeaker2D.Grandma: return "婆婆";
                case DialogueSpeaker2D.Brewer: return "釀酒師";
                case DialogueSpeaker2D.RiceMaster: return "年糕店家";
                default: return "主角";
            }
        }
    }
}

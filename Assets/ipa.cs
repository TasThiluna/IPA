using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;
using System.Text.RegularExpressions;

public class ipa : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable playButton;
    public KMSelectable[] buttons;
    public TextMesh[] buttonTexts;
    public AudioClip[] sounds;
    public Color red;
    public Renderer playRenderer;

    private int solution;
    private int soundPresent;

    private static readonly string[] symbols = new string[] { "p", "b", "t", "d", "c", "ɟ", "k", "g", "q", "ɢ", "ʔ", "m", "n", "ɲ", "ŋ", "ʙ", "r", "ʀ", "ⱱ", "ɾ", "f", "v", "θ", "ð", "s", "z", "ʂ", "ʐ", "x", "ɣ", "h", "ɦ", "ʋ", "ɻ", "j", "ɭ", "ʎ", "ǀ", "ɓ", "ɗ", "ʛ", "pʼ", "tʼ", "kʼ", "ʈ", "ɖ", "ɱ", "ɳ", "ɴ", "ɽ", "ɸ", "β", "ç", "ʝ", "χ", "ʁ", "ħ", "ʕ", "ɬ", "ɮ", "ɹ", "ɰ", "l", "ʟ", "ʘ", "ǃ", "ǂ", "ǁ", "ʄ", "ɠ", "sʼ" };
    private static readonly string[] positionNames = new string[9] { "top-left", "top-middle", "top-right", "middle-left", "middle-middle", "middle-right", "bottom-left", "bottom-middle", "bottom-right" };
    private ipaSettings settings = new ipaSettings();
    private int cap;
    private bool cantPlay = true;
    private bool cantInteract = true;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    private void Awake()
    {
        ModConfig<ipaSettings> modConfig = new ModConfig<ipaSettings>("IpaSettings");
        settings = modConfig.Settings;
        var missionDesc = KTMissionGetter.Mission.Description;
        if (missionDesc != null)
        {
            var regex = new Regex(@"\[IPA\] (true|false)");
            var match = regex.Match(missionDesc);
            if (match.Success)
            {
                string[] options = match.Value.Replace("[IPA] ", "").Split(',');
                bool[] values = new bool[options.Length];
                for (int i = 0; i < options.Length; i++)
                    values[i] = options[i] == "true" ? true : false;
                settings.hardMode = values[0];
            }
        }
        modConfig.Settings = settings;
        moduleId = moduleIdCounter++;
        playButton.OnInteract += delegate () { PressButton(); return false; };
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { PressButton(button); return false; };
        module.OnActivate += delegate () { StartCoroutine(ShowText()); };
    }

    private void Start()
    {
        foreach (TextMesh t in buttonTexts)
            t.text = "";
        cap = !settings.hardMode ? 44 : 71;
        if (settings.hardMode)
        {
            Debug.LogFormat("[IPA #{0}] Hard mode is active!", moduleId);
            playRenderer.material.color = red;
        }
        GenerateAnswer();
    }

    private void GenerateAnswer()
    {
        solution = rnd.Range(0, 9);
        soundPresent = rnd.Range(0, cap);
        Debug.LogFormat("[IPA #{0}] The sound being played corresponds to the symbol {1}. This symbol is on the {2} button.", moduleId, symbols[soundPresent], positionNames[solution]);
    }

    private void PressButton()
    {
        playButton.AddInteractionPunch(.2f);
        if (cantPlay)
            return;
        StartCoroutine(PlayButton());
    }

    private IEnumerator PlayButton()
    {
        cantPlay = true;
        audio.PlaySoundAtTransform(sounds[soundPresent].name, playButton.transform);
        yield return new WaitForSeconds(sounds[soundPresent].length + .1f);
        cantPlay = false;
    }

    private void PressButton(KMSelectable button)
    {
        button.AddInteractionPunch(.2f);
        audio.PlaySoundAtTransform("button", button.transform);
        if (moduleSolved || cantInteract)
            return;
        var ix = Array.IndexOf(buttons, button);
        Debug.LogFormat("[IPA #{0}] You pressed the {1} button, which has the symbol {2}.", moduleId, positionNames[ix], buttonTexts[ix].text);
        if (ix != solution)
        {
            module.HandleStrike();
            Debug.LogFormat("[IPA #{0}] That was incorrect. Strike! Resetting...", moduleId);
            GenerateAnswer();
            StartCoroutine(HideText(false));
        }
        else
        {
            module.HandlePass();
            Debug.LogFormat("[IPA #{0}] That was correct. Module solved!", moduleId);
            moduleSolved = true;
            StartCoroutine(HideText(true));
        }
    }

    private IEnumerator ShowText()
    {
        var buttonNumbers = new int[] { 0, 1, 2, 5, 4, 3, 6, 7, 8 };
        var decoyCount = 0;
    regenerateDecoys:
        var decoys = Enumerable.Range(0, cap).ToList().Shuffle().Take(8).ToArray();
        if (decoys.Any(x => x == soundPresent))
            goto regenerateDecoys;
        for (int i = 0; i < 9; i++)
        {
            buttonTexts[buttonNumbers[i]].text = (buttonNumbers[i] == solution) ? symbols[soundPresent] : symbols[decoys[decoyCount]];
            switch (buttonTexts[buttonNumbers[i]].text)
            {
                case "sʼ":
                case "pʼ":
                case "tʼ":
                case "kʼ":
                    buttonTexts[buttonNumbers[i]].transform.localScale = new Vector3(.0003f, .0003f, .0003f);
                    break;
                default:
                    buttonTexts[buttonNumbers[i]].transform.localScale = new Vector3(.0005f, .0005f, .0005f);
                    break;
            }
            audio.PlaySoundAtTransform("tick", buttons[buttonNumbers[i]].transform);
            if (buttonNumbers[i] != solution)
                decoyCount++;
            yield return new WaitForSeconds(.15f);
        }
        cantPlay = false;
        cantInteract = false;
    }

    private IEnumerator HideText(bool becauseSolve)
    {
        cantPlay = true;
        cantInteract = true;
        var buttonNumbers = new int[] { 0, 1, 2, 5, 4, 3, 6, 7, 8 };
        for (int i = 0; i < 9; i++)
        {
            buttonTexts[buttonNumbers[i]].text = "";
            audio.PlaySoundAtTransform("tick", buttons[buttonNumbers[i]].transform);
            yield return new WaitForSeconds(.15f);
        }
        if (!becauseSolve)
        {
            yield return new WaitForSeconds(.25f);
            StartCoroutine(ShowText());
        }
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} <tl/1> [Presses the button in that direction, or in that position in reading order.] !{0} play [Presses the play button.]";
#pragma warning restore 414

    private KMSelectable[] ProcessTwitchCommand(string command)
    {
        var btns = new List<KMSelectable>();
        foreach (var cmd in command.ToLowerInvariant().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
        {
            switch (cmd.Replace("center", "middle").Replace("centre", "middle"))
            {
                case "tl": case "lt": case "topleft": case "lefttop": case "1": btns.Add(buttons[0]); break;
                case "tm": case "tc": case "mt": case "ct": case "topmiddle": case "middletop": case "2": btns.Add(buttons[1]); break;
                case "tr": case "rt": case "topright": case "righttop": case "3": btns.Add(buttons[2]); break;

                case "ml": case "cl": case "lm": case "lc": case "middleleft": case "leftmiddle": case "4": btns.Add(buttons[3]); break;
                case "mm": case "cm": case "mc": case "cc": case "middle": case "middlemiddle": case "5": btns.Add(buttons[4]); break;
                case "mr": case "cr": case "rm": case "rc": case "middleright": case "rightmiddle": case "6": btns.Add(buttons[5]); break;

                case "bl": case "lb": case "bottomleft": case "leftbottom": case "7": btns.Add(buttons[6]); break;
                case "bm": case "bc": case "mb": case "cb": case "bottommiddle": case "middlebottom": case "8": btns.Add(buttons[7]); break;
                case "br": case "rb": case "bottomright": case "rightbottom": case "9": btns.Add(buttons[8]); break;

                case "play": case "0": btns.Add(playButton); break;

                default: return null;
            }
        }
        return btns.ToArray();
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (cantInteract)
            yield return null;
        buttons[solution].OnInteract();
    }

    class ipaSettings
    {
        public bool hardMode = false;
    }

#pragma warning disable 414
    private static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
    {
        new Dictionary<string, object>
        {
            { "Filename", "IpaSettings.json" },
            { "Name", "IPA" },
            {
                "Listing", new List<Dictionary<string, object>>
                {
                    new Dictionary<string, object>
                    {
                        { "Key", "Hard mode" },
                        { "Text", "Allows symbols to appear that may be harder for a native English speaker to identify." }
                    }
                }
            }
        }
    };
#pragma warning restore 414

}

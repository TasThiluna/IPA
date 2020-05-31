using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using rnd = UnityEngine.Random;

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

    private static readonly string[] symbols = new string[] { "p", "b", "t", "d", "c", "ɟ", "k", "g", "q", "ɢ", "ʔ", "m", "n", "ɲ", "ŋ", "ʙ", "r", "ʀ", "ⱱ", "ɾ", "f", "v", "θ", "ð", "s", "z", "ʂ", "ʐ", "x", "ɣ", "h", "ɦ", "ʋ", "ɻ", "j", "ɭ", "ʎ", "ǀ", "ɓ", "ɗ", "ʛ", "pʼ", "tʼ", "kʼ", "ʈ", "ɖ", "ɱ", "ɳ", "ɴ", "ɽ", "ɸ", "β", "ç", "ʝ", "χ", "ʁ", "ħ",  "ʕ", "ɬ", "ɮ", "ɹ", "ɰ", "l", "ʟ", "ʘ", "ǃ", "ǂ", "ǁ", "ʄ", "ɠ", "sʼ" };
    private static readonly string[] positionNames = new string[] { "top-left", "top-middle", "top-right", "middle-left", "middle-middle", "middle-right", "bottom-left", "bottom-middle", "bottom-right" };
    private IpaSettings settings = new IpaSettings();
    private int cap;
    private bool cantPlay = true;
    private bool cantInteract = true;

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        ModConfig<IpaSettings> modConfig = new ModConfig<IpaSettings>("IpaSettings");
        settings = modConfig.Settings;
        modConfig.Settings = settings;
        moduleId = moduleIdCounter++;
        playButton.OnInteract += delegate () { PressButton(); return false; };
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { PressButton(button); return false; };
        module.OnActivate += delegate () { StartCoroutine(ShowText()); };
    }

    void Start()
    {
        foreach (TextMesh t in buttonTexts)
            t.text = "";
        cap = !settings.hardMode ? 44 : 71;
        if (settings.hardMode)
        {
            Debug.LogFormat("[I.P.A. #{0}] Hard mode is active!", moduleId);
            playRenderer.material.color = red;
        }
        GenerateAnswer();
    }

    void GenerateAnswer()
    {
        solution = rnd.Range(0, 9);
        soundPresent = rnd.Range(0, cap);
        Debug.LogFormat("[I.P.A. #{0}] The sound being played corresponds to the symbol {1}. This symbol is on the {2} button.", moduleId, symbols[soundPresent], positionNames[solution]);
    }

    void PressButton()
    {
        playButton.AddInteractionPunch(.2f);
        if (cantPlay)
            return;
        StartCoroutine(PlayButton());
    }

    IEnumerator PlayButton()
    {
        cantPlay = true;
        audio.PlaySoundAtTransform(sounds[soundPresent].name, playButton.transform);
        yield return new WaitForSeconds(sounds[soundPresent].length + .1f);
        cantPlay = false;
    }

    void PressButton(KMSelectable button)
    {
        button.AddInteractionPunch(.2f);
        audio.PlaySoundAtTransform("button", button.transform);
        if (moduleSolved || cantInteract)
            return;
        var ix = Array.IndexOf(buttons, button);
        Debug.LogFormat("[I.P.A. #{0}] You pressed the {1} button, which has the symbol {2}.", moduleId, positionNames[ix], buttonTexts[ix].text);
        if (ix != solution)
        {
            module.HandleStrike();
            Debug.LogFormat("[I.P.A. #{0}] That was incorrect. Strike! Resetting...", moduleId);
            GenerateAnswer();
            StartCoroutine(HideText(false));
        }
        else
        {
            module.HandlePass();
            Debug.LogFormat("[I.P.A. #{0}] That was correct. Module solved!", moduleId);
            moduleSolved = true;
            StartCoroutine(HideText(true));
        }
    }

    IEnumerator ShowText()
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
            if (new string[] { "sʼ", "pʼ", "tʼ", "kʼ" }.Contains(buttonTexts[buttonNumbers[i]].text))
                buttonTexts[i].transform.localScale = new Vector3(.0003f, .0003f, .0003f);
            else
                buttonTexts[i].transform.localScale = new Vector3(.0005f, .0005f, .0005f);
            audio.PlaySoundAtTransform("tick", buttons[buttonNumbers[i]].transform);
            if (buttonNumbers[i] != solution)
                decoyCount++;
            yield return new WaitForSeconds(.15f);
        }
        cantPlay = false;
        cantInteract = false;
    }

    IEnumerator HideText(bool becauseSolve)
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
    private readonly string TwitchHelpMessage = "!{0} play [Plays the sound] | !{0} press <pos> [Presses the button in that position. Valid positions are TL, TM, TR, ML, MM, MR, BL, BM, and BR.]";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        var inputs = new string[] { "tl", "tm", "tr", "ml", "mm", "mr", "bl", "bm", "br" };
        var cmd = input.ToLowerInvariant();
        var cmdAr = cmd.Split(' ').ToArray();
        if (cmd == "play")
        {
            if (cantPlay)
            {
                yield return "sendtochaterror The play button cannot be pressed right now.";
                yield break;
            }
            yield return null;
            playButton.OnInteract();
        }
        else if (cmdAr.Length == 2)
        {
            if (cmdAr[0] != "press" && !inputs.Any(x => x == cmdAr[1]))
                yield break;
            if (cantInteract)
            {
                yield return "sendtochaterror A button cannot be pressed right now.";
                yield break;
            }
            yield return null;
            buttons[Array.IndexOf(inputs, cmdAr[1])].OnInteract();
        }
        else
            yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return null;
        while (cantInteract)
            yield return null;
        buttons[solution].OnInteract();
    }

    class IpaSettings
    {
        public bool hardMode = false;
    }

    private static Dictionary<string, object>[] TweaksEditorSettings = new Dictionary<string, object>[]
    {
        new Dictionary<string, object>
        {
            { "Filename", "IpaSettings.json" },
            { "Name", "I.P.A." },
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

}

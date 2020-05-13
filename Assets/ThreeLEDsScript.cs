using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using System;

public class ThreeLEDsScript : MonoBehaviour {

    public KMAudio audio;
    public KMBombInfo bomb;
    public KMColorblindMode Colorblind;

    public KMSelectable[] buttons;
    public Material[] roofMats;
    public Material[] stemMats;
    public Renderer[] roofRends;
    public Renderer[] stemRends;
    public Light[] lights;
    public Color[] lightCols;
    public TextMesh[] cbTexts;

    private string[] colorNames = new string[] { "white", "red", "blue", "green", "yellow" };
    private int[] chosenLEDColors = new int[] { -1, -1, -1 };
    private bool[] initialStates = new bool[] { false, false, false };
    private bool[] currentStates = new bool[] { false, false, false };
    private bool[] correctStates = new bool[] { false, false, false };
    private bool[][] stateTable = { new bool[] { true, false, true }, new bool[] { true, false, false }, new bool[] { false, true, false }, new bool[] { false, true, true }, new bool[] { false, false, true }, new bool[] { true, true, true } };
    private int indexInTable = -1;
    private bool colorblindActive = false;

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        moduleSolved = false;
        foreach (KMSelectable obj in buttons)
        {
            KMSelectable pressed = obj;
            pressed.OnInteract += delegate () { PressButton(pressed); return false; };
        }
        GetComponent<KMBombModule>().OnActivate += OnActivate;
    }

    void Start () {
        if (Colorblind.ColorblindModeActive)
            colorblindActive = true;
        for (int i = 0; i < 3; i++)
        {
            chosenLEDColors[i] = UnityEngine.Random.Range(0, lightCols.Length);
            stemRends[i].material = stemMats[chosenLEDColors[i]];
            stemRends[i+3].material = stemMats[chosenLEDColors[i]];
            stemRends[i+6].material = stemMats[chosenLEDColors[i]];
            roofRends[i].material = roofMats[chosenLEDColors[i]];
            lights[i].color = lightCols[chosenLEDColors[i]];
            lights[i+3].color = lightCols[chosenLEDColors[i]];
            int state = UnityEngine.Random.Range(0, 2);
            if (state == 0)
            {
                initialStates[i] = true;
                currentStates[i] = true;
                correctStates[i] = true;
            }
            else
            {
                initialStates[i] = false;
                currentStates[i] = false;
                correctStates[i] = false;
            }
        }
        Debug.LogFormat("[3 LEDs #{0}] The Top LED (1) is colored {1}", moduleId, colorNames[chosenLEDColors[0]]);
        Debug.LogFormat("[3 LEDs #{0}] The Bottom Left LED (2) is colored {1}", moduleId, colorNames[chosenLEDColors[1]]);
        Debug.LogFormat("[3 LEDs #{0}] The Bottom Right LED (3) is colored {1}", moduleId, colorNames[chosenLEDColors[2]]);
        Debug.LogFormat("[3 LEDs #{0}] The Top LED (1) was initially in the {1} state", moduleId, initialStates[0] ? "on" : "off");
        Debug.LogFormat("[3 LEDs #{0}] The Bottom Left LED (2) was initially in the {1} state", moduleId, initialStates[1] ? "on" : "off");
        Debug.LogFormat("[3 LEDs #{0}] The Bottom Right LED (3) was initially in the {1} state", moduleId, initialStates[2] ? "on" : "off");
        if (checkContains())
        {
            Debug.LogFormat("[3 LEDs #{0}] The table in the manual contains the initial LED states", moduleId);
            for (int i = 0; i < 3; i++)
            {
                if (chosenLEDColors[i] == 0)
                {
                    Debug.LogFormat("[3 LEDs #{0}] LED {1} is white, therefore the opposite of the initial state is this LED's correct state", moduleId, i + 1);
                    if (correctStates[i] == false)
                    {
                        correctStates[i] = true;
                    }
                    else
                    {
                        correctStates[i] = false;
                    }
                }
                else if (chosenLEDColors[i] == 1)
                {
                    Debug.LogFormat("[3 LEDs #{0}] LED {1} is red, therefore this LED's correct state is the state of the LED in this position in the picture above the initial in the manual's table", moduleId, i + 1);
                    if (indexInTable > 2)
                    {
                        correctStates[i] = stateTable[indexInTable-3][i];
                    }
                    else
                    {
                        correctStates[i] = stateTable[indexInTable+3][i];
                    }
                }
                else if (chosenLEDColors[i] == 2)
                {
                    Debug.LogFormat("[3 LEDs #{0}] LED {1} is blue, therefore this LED's correct state is the state of the LED in this position in the picture below the initial in the manual's table", moduleId, i + 1);
                    if (indexInTable < 3)
                    {
                        correctStates[i] = stateTable[indexInTable+3][i];
                    }
                    else
                    {
                        correctStates[i] = stateTable[indexInTable-3][i];
                    }
                }
                else if (chosenLEDColors[i] == 3)
                {
                    Debug.LogFormat("[3 LEDs #{0}] LED {1} is green, therefore this LED's correct state is the state of the LED in this position in the picture to the left of the initial in the manual's table", moduleId, i + 1);
                    if (indexInTable == 0)
                    {
                        correctStates[i] = stateTable[2][i];
                    }
                    else if (indexInTable == 3)
                    {
                        correctStates[i] = stateTable[5][i];
                    }
                    else
                    {
                        correctStates[i] = stateTable[indexInTable][i];
                    }
                }
                else if (chosenLEDColors[i] == 4)
                {
                    Debug.LogFormat("[3 LEDs #{0}] LED {1} is yellow, therefore this LED's correct state is the state of the LED in this position in the picture to the right of the initial in the manual's table", moduleId, i + 1);
                    if (indexInTable == 2)
                    {
                        correctStates[i] = stateTable[0][i];
                    }
                    else if (indexInTable == 5)
                    {
                        correctStates[i] = stateTable[3][i];
                    }
                    else
                    {
                        correctStates[i] = stateTable[indexInTable][i];
                    }
                }
            }
        }
        else
        {
            Debug.LogFormat("[3 LEDs #{0}] The table in the manual does not contains the initial LED states", moduleId);
            List<int> digits = new List<int>();
            string lognums = "";
            string lognewnums = "";
            for (int i = 0; i < bomb.GetSerialNumberNumbers().Count(); i++)
            {
                int dig = bomb.GetSerialNumberNumbers().ElementAt(i);
                lognums += dig + " ";
                dig %= 3;
                dig++;
                lognewnums += dig + " ";
                digits.Add(dig);
            }
            lognums = lognums.Trim();
            lognewnums = lognewnums.Trim();
            Debug.LogFormat("[3 LEDs #{0}] The serial number's digits are {1}", moduleId, lognums);
            Debug.LogFormat("[3 LEDs #{0}] The serial number's digits after modification are {1}", moduleId, lognewnums);
            for (int i = 0; i < digits.Count; i++)
            {
                if (correctStates[digits[i]-1] == false)
                {
                    Debug.LogFormat("[3 LEDs #{0}] LED {1}: off -> on", moduleId, digits[i]);
                    correctStates[digits[i]-1] = true;
                }
                else
                {
                    Debug.LogFormat("[3 LEDs #{0}] LED {1}: on -> off", moduleId, digits[i]);
                    correctStates[digits[i]-1] = false;
                }
            }
        }
        for (int i = 0; i < 3; i++)
        {
            Debug.LogFormat("[3 LEDs #{0}] The correct state for LED {1} is {2}", moduleId, i+1, correctStates[i] ? "on" : "off");
        }
    }

    void OnActivate()
    {
        for (int i = 0; i < 3; i++)
        {
            lights[i].enabled = currentStates[i];
            lights[i+3].enabled = currentStates[i];
            if (colorblindActive)
                cbTexts[i].text = colorNames[chosenLEDColors[i]];
        }
    }

    void PressButton(KMSelectable pressed)
    {
        if (moduleSolved != true)
        {
            if (pressed == buttons[0])
            {
                pressed.AddInteractionPunch(0.25f);
                audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, pressed.transform);
                Debug.LogFormat("[3 LEDs #{0}] Submitted states of LEDs 1, 2, and 3 were {1}, {2}, and {3}", moduleId, currentStates[0] ? "on" : "off", currentStates[1] ? "on" : "off", currentStates[2] ? "on" : "off");
                if (checkCorrect())
                {
                    Debug.LogFormat("[3 LEDs #{0}] Correct states submitted, module solved!", moduleId);
                    moduleSolved = true;
                    GetComponent<KMBombModule>().HandlePass();
                }
                else
                {
                    Debug.LogFormat("[3 LEDs #{0}] Incorrect states submitted, resetting LEDs to initial states!", moduleId);
                    GetComponent<KMBombModule>().HandleStrike();
                    for (int i = 0; i < 3; i++)
                    {
                        currentStates[i] = initialStates[i];
                        lights[i].enabled = currentStates[i];
                        lights[i+3].enabled = currentStates[i];
                    }
                }
            }
            else
            {
                pressed.AddInteractionPunch(0.5f);
                if (currentStates[Array.IndexOf(buttons, pressed)-1] == false)
                {
                    audio.PlaySoundAtTransform("on", pressed.transform);
                    currentStates[Array.IndexOf(buttons, pressed)-1] = true;
                    lights[Array.IndexOf(buttons, pressed)-1].enabled = true;
                    lights[Array.IndexOf(buttons, pressed)+2].enabled = true;
                }
                else
                {
                    audio.PlaySoundAtTransform("off", pressed.transform);
                    currentStates[Array.IndexOf(buttons, pressed)-1] = false;
                    lights[Array.IndexOf(buttons, pressed)-1].enabled = false;
                    lights[Array.IndexOf(buttons, pressed)+2].enabled = false;
                }
            }
        }
    }

    private bool checkContains()
    {
        for (int i = 0; i < 6; i++)
        {
            bool val1 = false;
            bool val2 = false;
            bool val3 = false;
            if (stateTable[i][0] == initialStates[0])
            {
                val1 = true;
            }
            if (stateTable[i][1] == initialStates[1])
            {
                val2 = true;
            }
            if (stateTable[i][2] == initialStates[2])
            {
                val3 = true;
            }
            if (val1 && val2 && val3)
            {
                indexInTable = i;
                return true;
            }
        }
        return false;
    }

    private bool checkCorrect()
    {
        for (int i = 0; i < 3; i++)
        {
            if (currentStates[i] != correctStates[i])
            {
                return false;
            }
        }
        return true;
    }

    //twitch plays
    #pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} toggle <#> [Toggles the specified LED '#' (chainable with spaces between each LED)] | !{0} submit [Presses the submit button] | !{0} colorblind [Toggles colorblind mode] | Valid LEDs are 1-3";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)
    {
        if (Regex.IsMatch(command, @"^\s*colorblind\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (colorblindActive)
            {
                colorblindActive = false;
                for (int i = 0; i < 3; i++)
                {
                    cbTexts[i].text = "";
                }
            }
            else
            {
                colorblindActive = true;
                for (int i = 0; i < 3; i++)
                {
                    cbTexts[i].text = colorNames[chosenLEDColors[i]];
                }
            }
            yield break;
        }
        if (Regex.IsMatch(command, @"^\s*submit\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            buttons[0].OnInteract();
            yield break;
        }
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(parameters[0], @"^\s*toggle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the LED(s) you wish to toggle!";
            }
            else
            {
                string[] valids = new string[] { "1", "2", "3" };
                for (int i = 1; i < parameters.Length; i++)
                {
                    if (!valids.Contains(parameters[i]))
                    {
                        yield return "sendtochaterror The specified LED to toggle '" + parameters[i] + "' is invalid!";
                        yield break;
                    }
                }
                for (int i = 1; i < parameters.Length; i++)
                {
                    buttons[int.Parse(parameters[i])].OnInteract();
                    yield return new WaitForSeconds(0.1f);
                }
            }
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        for (int i = 0; i < 3; i++)
        {
            if (currentStates[i] != correctStates[i])
            {
                buttons[i+1].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }
        }
        buttons[0].OnInteract();
        yield return new WaitForSeconds(0.1f);
    }
}
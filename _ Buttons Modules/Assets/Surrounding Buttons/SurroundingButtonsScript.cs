using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class SurroundingButtonsScript : MonoBehaviour
{

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Buttons;
    public KMColorblindMode Colourblind;
    public TextMesh[] ColourblindTexts;

    //Red = 0, Yellow = 1, Green = 2, Blue = 3
    private List<int> Colours2 = new List<int>();
    private List<int> Currents = new List<int>();
    private List<int> PressedButtons2 = new List<int>();
    private int[] Colours = new int[12];
    private int CentralColour;
    private int StartingRow;
    private int StartingColumn;
    private int GoalSquare;
    private string[][] ColoursToString = new string[4][] { new string[4], new string[4], new string[4], new string[4] };
    private string[] CoordinateNames = { "a1", "b1", "c1", "d1", "a2", "b2", "c2", "d2", "a3", "b3", "c3", "d3", "a4", "b4", "c4", "d4" };
    private string ColourblindColours = "RYGB";
    private bool[] PressedButtons = new bool[12];
    private bool[] Correct = new bool[12];
    private bool[] Correct2 = new bool[12];
    private bool ColourblindEnabled;
    private bool Pressing;
    private bool Solved;
    private Color[] ColourValues = { new Color(1, .25f, .25f), new Color(1, 1, .25f), new Color(.25f, 1, .25f), new Color(.25f, .25f, 1) };

    private IEnumerable<bool> AdjacentVisitedSquares(int Row, int Column, bool[,] Visited)
    {
        if (Row != 0)
            yield return Visited[Row - 1, Column];
        if (Column != 3)
            yield return Visited[Row, Column + 1];
        if (Row != 3)
            yield return Visited[Row + 1, Column];
        if (Column != 0)
            yield return Visited[Row, Column - 1];
    }

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        ColourblindEnabled = Colourblind.ColorblindModeActive;
        for (int i = 0; i < ColourblindTexts.Length; i++)
        {
            ColourblindTexts[i].text = "";
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color();
        }
        for (int i = 0; i < 12; i++)
            Colours[i] = Rnd.Range(0, 4);
        CentralColour = Rnd.Range(0, 4);
        for (int i = 0; i < 12; i++)
            Colours2.Add(Colours[i]);
        Calculate();
        Module.OnActivate += delegate
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                int x = i;
                Buttons[i].OnInteract += delegate { if (!Pressing) StartCoroutine(ButtonPress(x)); return false; };
            }
            for (int i = 0; i < 12; i++)
                Buttons[i].GetComponent<MeshRenderer>().material.color = ColourValues[Colours[i]];
            Buttons[12].GetComponent<MeshRenderer>().material.color = ColourValues[CentralColour];
            if (ColourblindEnabled)
            {
                for (int i = 0; i < 12; i++)
                    ColourblindTexts[i].text = ColourblindColours[Colours[i]].ToString();
                ColourblindTexts[12].text = ColourblindColours[CentralColour].ToString();
            }
        };
    }

    void Calculate()
    {
        int Current = -1;
        for (int i = 0; i < 12; i++)
            Correct[i] = true;
        for (int i = 0; i < 6; i++)
        {
            Current += CentralColour + Colours[Current == -1 ? 0 : Current] + 2;
            Current = Current % 12;
            while (!Correct[Current])
                Current = (Current + 1) % 12;
            Correct[Current] = false;
            Currents.Add(Current + 1);
        }
        for (int i = 0; i < 12; i++)
            Correct2[i] = Correct[i];
        Debug.LogFormat("[Surrounding Buttons #{0}] The colours of the surrounding buttons, starting from the button in the top-left, clockwise, are: {1}.", _moduleID, Colours.Select(x => ColourblindColours[x]).Join(", "));
        Debug.LogFormat("[Surrounding Buttons #{0}] The centre button is {1}.", _moduleID, new[] { "red", "yellow", "green", "blue" }[CentralColour]);
        Debug.LogFormat("[Surrounding Buttons #{0}] The six visited buttons, in order, clockwise, are: {1}.", _moduleID, Currents.Join(", "));
    }

    private IEnumerator CheckSolve()
    {
        int Corrects = 0;
        for (int i = 0; i < 13; i++)
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color();
        for (int i = 0; i < 13; i++)
            ColourblindTexts[i].text = "";
        for (int i = 0; i < 12; i++)
        {
            if (Correct[i])
            {
                Audio.PlaySoundAtTransform("bleep", Buttons[i].transform);
                Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1);
                Corrects++;
            }
            else
            {
                Audio.PlaySoundAtTransform("bleep 2", Buttons[i].transform);
                Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(.25f, .25f, .25f);
            }
            yield return new WaitForSeconds(0.1f);
        }
        if (Corrects == 12)
        {
            Module.HandlePass();
            yield return "solve";
            Solved = true;
            Audio.PlaySoundAtTransform("solve", Buttons[12].transform);
            Debug.LogFormat("[Surrounding Buttons #{0}] You pressed buttons: {1}. That was correct. Module solved!", _moduleID, PressedButtons2.Select(x => x + 1).Join(", "));
            StartCoroutine(SolveAnim());
        }
        else
        {
            Module.HandleStrike();
            yield return "strike";
            Audio.PlaySoundAtTransform("strike", Buttons[12].transform);
            Debug.LogFormat("[Surrounding Buttons #{0}] You pressed button {1}, which was incorrect. Strike!", _moduleID, PressedButtons2.Select(x => x + 1).Join(", "));
            PressedButtons = new bool[12];
            for (int i = 0; i < 12; i++)
                Correct[i] = Correct2[i];
            for (int i = 0; i < 12; i++)
                Buttons[i].GetComponent<MeshRenderer>().material.color = ColourValues[Colours[i]];
            Buttons[12].GetComponent<MeshRenderer>().material.color = ColourValues[CentralColour];
            if (ColourblindEnabled)
            {
                for (int i = 0; i < 12; i++)
                    ColourblindTexts[i].text = ColourblindColours[Colours[i]].ToString();
                ColourblindTexts[12].text = ColourblindColours[CentralColour].ToString();
            }
            Pressing = false;
        }
    }

    private IEnumerator ButtonPress(int pos)
    {
        if (pos != 12)
        {
            if (!PressedButtons[pos])
                Audio.PlaySoundAtTransform("bleep", Buttons[pos].transform);
            else
                Audio.PlaySoundAtTransform("bleep 2", Buttons[pos].transform);
        }
        Buttons[pos].AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, Buttons[pos].transform);
        for (int i = 0; i < 3; i++)
        {
            Buttons[pos].transform.localPosition -= new Vector3(0, 0.002f, 0);
            yield return null;
        }
        if (pos != 12)
        {
            Correct[pos] = !Correct[pos];
            PressedButtons[pos] = !PressedButtons[pos];
            if (PressedButtons[pos])
                PressedButtons2.Add(pos);
            else
                PressedButtons2.Remove(pos);
            PressedButtons2.Sort();
            if (PressedButtons[pos])
            {
                Buttons[pos].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
                ColourblindTexts[pos].text = "";
            }
            else
            {
                Buttons[pos].GetComponent<MeshRenderer>().material.color = ColourValues[Colours[pos]];
                if (ColourblindEnabled)
                    ColourblindTexts[pos].text = ColourblindColours[Colours[pos]].ToString();
            }
        }
        else if (PressedButtons.Where(x => x == true).Count() == 6)
        {
            Pressing = true;
            StartCoroutine(CheckSolve());
        }
        else
            Audio.PlaySoundAtTransform("buzzer", Buttons[12].transform);
        for (int i = 0; i < 3; i++)
        {
            Buttons[pos].transform.localPosition += new Vector3(0, 0.002f, 0);
            yield return null;
        }
    }

    private IEnumerator SolveAnim()
    {
        for (int i = 0; i < 13; i++)
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 1, 0);
        yield return new WaitForSeconds(0.15f);
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 13; j++)
                Buttons[j].GetComponent<MeshRenderer>().material.color -= new Color(0, 0.2f, 0);
            yield return new WaitForSeconds(0.15f);
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} 1 2 3 s' to press buttons 1, 2 and 3 in clockwise order from the top-left button, then press the submit button. Use '!{0} colo(u)rblind' to enable / disable colourblind mode.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string[] CommandArray = command.Split(' ');
        string[] Numbers = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
        for (int i = 0; i < CommandArray.Length; i++)
        {
            if (!Numbers.Contains(CommandArray[i]) && command != "colourblind" && command != "colorblind" && CommandArray[i] != "s")
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
            yield return null;
            if (command == "colourblind" || command == "colorblind")
            {
                ColourblindEnabled = !ColourblindEnabled;
                if (ColourblindEnabled && !Pressing)
                {
                    for (int j = 0; j < 12; j++)
                        ColourblindTexts[j].text = ColourblindColours[Colours[j]].ToString();
                    ColourblindTexts[12].text = ColourblindColours[CentralColour].ToString();
                }
                else if (Pressing)
                {
                    yield return "sendtochaterror Please wait for the previous command to finish processing.";
                    yield break;
                }
                else
                    for (int j = 0; j < 13; j++)
                        ColourblindTexts[j].text = "";
            }
            else if (CommandArray[i] == "s")
                Buttons[12].OnInteract();
            else
                Buttons[int.Parse(CommandArray[i]) - 1].OnInteract();
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        for (int i = 0; i < 6; i++)
        {
            Buttons[Currents[i] - 1].OnInteract();
            yield return true;
            for (int j = 0; j < 4; j++)
                yield return null;
        }
        Buttons[12].OnInteract();
    }
}

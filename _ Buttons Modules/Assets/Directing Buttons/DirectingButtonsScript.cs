using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class DirectingButtonsScript : MonoBehaviour
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
    private int[,] Colours = new int[4, 4];
    private int StartingRow;
    private int StartingColumn;
    private int GoalSquare;
    private string[][] ColoursToString = new string[4][] { new string[4], new string[4], new string[4], new string[4] };
    private string[] CoordinateNames = { "a1", "b1", "c1", "d1", "a2", "b2", "c2", "d2", "a3", "b3", "c3", "d3", "a4", "b4", "c4", "d4" };
    private string ColourblindColours = "RYGB";
    private bool ColourblindEnabled;
    private bool Pressing;
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
        for (int i = 0; i < 16; i++)
        {
            ColourblindTexts[i].text = "";
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
        }
        Module.OnActivate += delegate
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                int x = i;
                Buttons[i].OnInteract += delegate { if (!Pressing) StartCoroutine(ButtonPress(x)); return false; };
            }
            Calculate();
            for (int i = 0; i < Buttons.Length; i++)
                Buttons[i].GetComponent<MeshRenderer>().material.color = ColourValues[Colours[i / 4, i % 4]];
            if (ColourblindEnabled)
                for (int i = 0; i < 16; i++)
                    ColourblindTexts[i].text = ColourblindColours[Colours[i / 4, i % 4]].ToString();
        };
    }

    void Calculate()
    {
        bool[,] VisitedSquares = new bool[4, 4];
        int StartingRow = Rnd.Range(0, 4);
        int StartingColumn = Rnd.Range(0, 4);
        int RandomDirection = 0;
        CalcSteps:
        {
            VisitedSquares = new bool[4, 4];
            int Row = StartingRow;
            int Column = StartingColumn;
            for (int i = 0; i < 15; i++)
            {
                if (AdjacentVisitedSquares(Row, Column, VisitedSquares).All(x => x))
                    goto CalcSteps;
                GetValidDirection:
                {
                    RandomDirection = Rnd.Range(0, 4);
                    if ((RandomDirection == 0 && Row == 0) || (RandomDirection == 1 && Column == 3) || (RandomDirection == 2 && Row == 3) || (RandomDirection == 3 && Column == 0))
                        goto GetValidDirection;
                    switch (RandomDirection)
                    {
                        case 0:
                            if (VisitedSquares[(Row + 3) % 4, Column])
                                goto GetValidDirection;
                            VisitedSquares[Row, Column] = true;
                            Colours[Row, Column] = 0;
                            Row = (Row + 3) % 4;
                            break;
                        case 1:
                            if (VisitedSquares[Row, (Column + 1) % 4])
                                goto GetValidDirection;
                            VisitedSquares[Row, Column] = true;
                            Colours[Row, Column] = 1;
                            Column = (Column + 1) % 4;
                            break;
                        case 2:
                            if (VisitedSquares[(Row + 1) % 4, Column])
                                goto GetValidDirection;
                            VisitedSquares[Row, Column] = true;
                            Colours[Row, Column] = 2;
                            Row = (Row + 1) % 4;
                            break;
                        default:
                            if (VisitedSquares[Row, (Column + 3) % 4])
                                goto GetValidDirection;
                            VisitedSquares[Row, Column] = true;
                            Colours[Row, Column] = 3;
                            Column = (Column + 3) % 4;
                            break;
                    }
                }
            }
            RandomDirection = Rnd.Range(0, 4);
            while ((RandomDirection == 0 && Row == 0) || (RandomDirection == 1 && Column == 3) || (RandomDirection == 2 && Row == 3) || (RandomDirection == 3 && Column == 0))
                RandomDirection = Rnd.Range(0, 4);
            Colours[Row, Column] = RandomDirection;
            switch (Colours[Row, Column])
            {
                case 0:
                    if (StartingRow == Row - 1)
                        goto CalcSteps;
                    break;
                case 1:
                    if (StartingColumn == Column + 1)
                        goto CalcSteps;
                    break;
                case 2:
                    if (StartingRow == Row + 1)
                        goto CalcSteps;
                    break;
                default:
                    if (StartingColumn == Column - 1)
                        goto CalcSteps;
                    break;
            }
            GoalSquare = (Row * 4) + Column;
        }
        for (int i = 0; i < 16; i++)
            ColoursToString[i / 4][i % 4] = Colours[i / 4, i % 4].ToString();
        Debug.LogFormat("[Directing Buttons #{0}] The grid of buttons:\n{1}", _moduleID, ColoursToString[0].Select(x => x == "0" ? x = "R" : x == "1" ? x = "Y" : x == "2" ? x = "G" : x = "B").Join() + "\n" + ColoursToString[1].Select(x => x == "0" ? x = "R" : x == "1" ? x = "Y" : x == "2" ? x = "G" : x = "B").Join() + "\n" + ColoursToString[2].Select(x => x == "0" ? x = "R" : x == "1" ? x = "Y" : x == "2" ? x = "G" : x = "B").Join() + "\n" + ColoursToString[3].Select(x => x == "0" ? x = "R" : x == "1" ? x = "Y" : x == "2" ? x = "G" : x = "B").Join());
        Debug.LogFormat("[Directing Buttons #{0}] The starting button is button {1}.", _moduleID, CoordinateNames[(StartingRow * 4) + StartingColumn].ToUpperInvariant());
    }

    private IEnumerator ButtonPress(int pos)
    {
        Buttons[pos].AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, Buttons[pos].transform);
        for (int i = 0; i < 3; i++)
        {
            Buttons[pos].transform.localPosition -= new Vector3(0, 0.002f, 0);
            yield return null;
        }
        StartCoroutine(CheckSolve(pos));
        for (int i = 0; i < 3; i++)
        {
            Buttons[pos].transform.localPosition += new Vector3(0, 0.002f, 0);
            yield return null;
        }
    }

    private IEnumerator CheckSolve(int pos)
    {
        Pressing = true;
        for (int i = 0; i < 16; i++)
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
        Buttons[pos].GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1);
        for (int i = 0; i < 16; i++)
            ColourblindTexts[i].text = "";
        int Runs = 0;
        bool[,] VisitedSquares = new bool[4, 4];
        while (!(Colours[pos / 4, pos % 4] == 0 && pos / 4 == 0) && !(Colours[pos / 4, pos % 4] == 1 && pos % 4 == 3) && !(Colours[pos / 4, pos % 4] == 2 && pos / 4 == 3) && !(Colours[pos / 4, pos % 4] == 3 && pos % 4 == 0))
        {
            bool Okay = true;
            Audio.PlaySoundAtTransform("bleep", Buttons[pos].transform);
            switch (Colours[pos / 4, pos % 4])
            {
                case 0:
                    if (VisitedSquares[(pos / 4) - 1, pos % 4])
                        Okay = false;
                    break;
                case 1:
                    if (VisitedSquares[pos / 4, (pos % 4) + 1])
                        Okay = false;
                    break;
                case 2:
                    if (VisitedSquares[(pos / 4) + 1, pos % 4])
                        Okay = false;
                    break;
                default:
                    if (VisitedSquares[pos / 4, (pos % 4) - 1])
                        Okay = false;
                    break;
            }
            Runs++;
            yield return new WaitForSeconds(0.1f);
            switch (Colours[pos / 4, pos % 4])
            {
                case 0:
                    pos -= 4;
                    break;
                case 1:
                    pos++;
                    break;
                case 2:
                    pos += 4;
                    break;
                default:
                    pos--;
                    break;
            }
            VisitedSquares[pos / 4, pos % 4] = true;
            Buttons[pos].GetComponent<MeshRenderer>().material.color = new Color(1, 1, 1);
            if (!Okay)
                break;
            if (Runs >= 16)
                break;
        }
        if (Runs >= 16 || (Runs == 15 && pos == GoalSquare))
        {
            Module.HandlePass();
            Audio.PlaySoundAtTransform("solve", Buttons[pos].transform);
            Debug.LogFormat("[Directing Buttons #{0}] You pressed button {1}, which was correct. Module solved!", _moduleID, CoordinateNames[pos].ToUpperInvariant());
            StartCoroutine(SolveAnim());
        }
        else
        {
            Module.HandleStrike();
            Audio.PlaySoundAtTransform("strike", Buttons[pos].transform);
            Debug.LogFormat("[Directing Buttons #{0}] You pressed button {1}, which was incorrect. Strike!", _moduleID, CoordinateNames[pos].ToUpperInvariant());
            for (int i = 0; i < 16; i++)
                Buttons[i].GetComponent<MeshRenderer>().material.color = ColourValues[Colours[i / 4, i % 4]];
            if (ColourblindEnabled)
                for (int i = 0; i < 16; i++)
                    ColourblindTexts[i].text = ColourblindColours[Colours[i / 4, i % 4]].ToString();
            Pressing = false;
        }
    }
    private IEnumerator SolveAnim()
    {
        for (int i = 0; i < 16; i++)
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 1, 0);
        yield return new WaitForSeconds(0.15f);
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 16; j++)
                Buttons[j].GetComponent<MeshRenderer>().material.color -= new Color(0, 0.2f, 0);
            yield return new WaitForSeconds(0.15f);
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} A2' to press the button in column 1, row 2 and use '!{0} colo(u)rblind' to enable / disable colourblind mode.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        if (!CoordinateNames.Contains(command.ToLowerInvariant()) && command != "colourblind" && command != "colorblind")
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        yield return null;
        if (command == "colourblind" || command == "colorblind")
        {
            ColourblindEnabled = !ColourblindEnabled;
            if (ColourblindEnabled && !Pressing)
                for (int i = 0; i < 16; i++)
                    ColourblindTexts[i].text = ColourblindColours[Colours[i / 4, i % 4]].ToString();
            else if (Pressing)
            {
                yield return "sendtochaterror Please wait for the previous command to finish processing.";
                yield break;
            }
            else
                for (int i = 0; i < 16; i++)
                    ColourblindTexts[i].text = "";
        }
        else
            Buttons[Array.IndexOf(CoordinateNames, command)].OnInteract();
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        Buttons[(StartingRow * 4) + StartingColumn].OnInteract();
    }
}

using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class FaultyButtonsScript : MonoBehaviour
{

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Buttons;

    //Red = 0, Yellow = 1, Green = 2, Blue = 3
    private int[] ReferredButtons = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
    private int[] BinaryNumbers = new int[4];
    private int[] ConvertedBinaryNumbers = new int[4];
    private int Answer;
    private int PressedButtonCount;
    private string[][] SoundsToString = new string[4][] { new string[4], new string[4], new string[4], new string[4] };
    private string[] CoordinateNames = { "a1", "b1", "c1", "d1", "a2", "b2", "c2", "d2", "a3", "b3", "c3", "d3", "a4", "b4", "c4", "d4" };
    private bool[] PressedButtons = new bool[16];
    private bool Solved;
    private bool Submitting;
    private Color[] ColourValues = { new Color(.25f, .25f, .25f), new Color(.75f, .75f, .75f) };

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        ReferredButtons.Shuffle();
        Debug.Log(ReferredButtons.Join());
        for (int i = 0; i < 16; i++)
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
        Module.OnActivate += delegate
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                int x = i;
                Buttons[i].OnInteract += delegate { if (!PressedButtons[x]) StartCoroutine(ButtonPress(x)); return false; };
            }
            //Calculate();
            StartCoroutine(Flicker());
        };
    }

    /*void Calculate()
    {
        for (int i = 0; i < 16; i++)
            Sounds[i] = Rnd.Range(1, 6);
        for (int i = 0; i < 16; i++)
            SoundsToString[i / 4][i % 4] = Sounds[i].ToString();
        for (int i = 0; i < 4; i++)
        {
            BinaryNumbers[i] = Sounds[(Sounds[i * 4] * 8) + (Sounds[(i * 4) + 1] * 4) + (Sounds[(i * 4) + 2] * 2) + Sounds[(i * 4) + 3]];
            ConvertedBinaryNumbers[i] = (Sounds[i * 4] * 8) + (Sounds[(i * 4) + 1] * 4) + (Sounds[(i * 4) + 2] * 2) + Sounds[(i * 4) + 3];
        }
        Debug.Log(BinaryNumbers.Join());
        Answer = (BinaryNumbers[0] * 8) + (BinaryNumbers[1] * 4) + (BinaryNumbers[2] * 2) + BinaryNumbers[3];
        Debug.LogFormat("[Faulty Buttons #{0}] The grid of buttons:\n{1}", _moduleID, SoundsToString[0].Select(x => x == "0" ? x = "K" : x = "W").Join() + "\n" + SoundsToString[1].Select(x => x == "0" ? x = "K" : x = "W").Join() + "\n" + SoundsToString[2].Select(x => x == "0" ? x = "K" : x = "W").Join() + "\n" + SoundsToString[3].Select(x => x == "0" ? x = "K" : x = "W").Join());
        Debug.LogFormat("[Faulty Buttons #{0}] The resulting button is button {1} in reading order, or button {2}.", _moduleID, (Answer + 1).ToString(), CoordinateNames[Answer].ToUpperInvariant());
    }*/

    void SubmitMode(int pos)
    {
        Audio.PlaySoundAtTransform("spark", Buttons[pos].transform);
        PressedButtons = new bool[16];
        PressedButtonCount = 0;
        Submitting = true;
    }

    void CheckSolve(int pos)
    {
        if (Answer == pos)
        {
            Module.HandlePass();
            Audio.PlaySoundAtTransform("solve", Buttons[pos].transform);
            Debug.LogFormat("[Faulty Buttons #{0}] You pressed button {1}, which was correct. Module solved!", _moduleID, CoordinateNames[pos].ToUpperInvariant());
            Solved = true;
            for (int i = 0; i < 16; i++)
                Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
        }
        else
        {
            Module.HandleStrike();
            Audio.PlaySoundAtTransform("strike", Buttons[pos].transform);
            Debug.LogFormat("[Faulty Buttons #{0}] You pressed button {1}, which was incorrect. Strike!", _moduleID, CoordinateNames[pos].ToUpperInvariant());
            PressedButtons = new bool[16];
            PressedButtonCount = 0;
            Submitting = false;
        }
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
        if (Submitting)
            Audio.PlaySoundAtTransform("bleep", Buttons[pos].transform);
        PressedButtons[pos] = true;
        PressedButtonCount++;
        if (PressedButtonCount == 16)
            SubmitMode(pos);
        for (int i = 0; i < 3; i++)
        {
            Buttons[pos].transform.localPosition += new Vector3(0, 0.002f, 0);
            yield return null;
        }
    }

    private IEnumerator Flicker()
    {
        while (!Solved)
        {
            float Random = Rnd.Range(0.1f, 0.15f);
            if (!Submitting)
            {
                for (int i = 0; i < 16; i++)
                {
                    if (!PressedButtons[i])
                        Buttons[ReferredButtons[i]].GetComponent<MeshRenderer>().material.color = new Color(Random, Random, Random);
                    else
                        Buttons[ReferredButtons[i]].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
                }
            }
            else
            {
                for (int i = 0; i < 16; i++)
                {
                    if (!PressedButtons[i])
                        Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(Random, Random, Random);
                    else
                        Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
                }
            }
            yield return null;
        }
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "Use '!{0} A2' to press the button in column 1, row 2 or use '!{0} 5' to press the fifth button in reading order.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string[] Numbers = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
        if (!CoordinateNames.Contains(command.ToLowerInvariant()) && !Numbers.Contains(command))
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
        yield return null;
        if (Numbers.Contains(command))
            Buttons[int.Parse(command) - 1].OnInteract();
        else
            Buttons[Array.IndexOf(CoordinateNames, command)].OnInteract();
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        Buttons[Answer].OnInteract();
    }
}

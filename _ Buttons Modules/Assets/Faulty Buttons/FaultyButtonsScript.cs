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
    
    private int[] ReferredButtons = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
    private int PressedButtonCount;
    private int PrevButton;
    private string[][] GridToString = { new string[4], new string[4], new string[4], new string[4] };
    private string[] CoordinateNames = { "a1", "b1", "c1", "d1", "a2", "b2", "c2", "d2", "a3", "b3", "c3", "d3", "a4", "b4", "c4", "d4" };
    private bool[] PressedButtons = new bool[16];
    private bool Solved;
    private bool Submitting;
    private bool EnteringShape;

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        ReferredButtons.Shuffle();
        for (int i = 0; i < 16; i++)
            GridToString[Mathf.FloorToInt(i / 4f)][i % 4] = (ReferredButtons[i] + 1).ToString();
        Debug.LogFormat("[Faulty Buttons #{0}] The referred buttons for each button in reading order are:\n{1}", _moduleID, GridToString[0].Join() + "\n" + GridToString[1].Join() + "\n" + GridToString[2].Join() + "\n" + GridToString[3].Join());
        for (int i = 0; i < 16; i++)
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
        Module.OnActivate += delegate
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                int x = i;
                Buttons[i].OnInteract += delegate { if (!PressedButtons[x]) StartCoroutine(ButtonPress(x)); return false; };
            }
            StartCoroutine(Flicker());
        };
    }

    void SubmitMode(int pos)
    {
        Audio.PlaySoundAtTransform("spark", Buttons[pos].transform);
        PressedButtons = new bool[16];
        PressedButtonCount = 0;
        Submitting = true;
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
        PressedButtons[pos] = true;
        PressedButtonCount++;
        if (Submitting)
        {
            Audio.PlaySoundAtTransform("bleep", Buttons[pos].transform);
            if (EnteringShape && ReferredButtons[PrevButton] != pos)
            {
                Module.HandleStrike();
                Audio.PlaySoundAtTransform("strike", Buttons[pos].transform);
                PressedButtons = new bool[16];
                PressedButtonCount = 0;
                Submitting = false;
                EnteringShape = false;
                ReferredButtons.Shuffle();
                Debug.LogFormat("[Faulty Buttons #{0}] You pressed button {1}, where button {2} was expected. Strike!", _moduleID, (pos + 1).ToString(), (ReferredButtons[PrevButton] + 1).ToString());
                for (int i = 0; i < 16; i++)
                    GridToString[Mathf.FloorToInt(i / 4f)][i % 4] = (ReferredButtons[i] + 1).ToString();
                Debug.LogFormat("[Faulty Buttons #{0}] The referred buttons for each button in reading order are:\n{1}", _moduleID, GridToString[0].Join() + "\n" + GridToString[1].Join() + "\n" + GridToString[2].Join() + "\n" + GridToString[3].Join());
            }
            else if (PressedButtons[ReferredButtons[pos]])
            {
                if (PressedButtonCount < 16)
                    Audio.PlaySoundAtTransform("spark", Buttons[pos].transform);
                EnteringShape = false;
                Debug.LogFormat("[Faulty Buttons #{0}] You pressed button {1} without any issues. That completes a shape.", _moduleID, (pos + 1).ToString());
            }
            else
            {
                EnteringShape = true;
                Debug.LogFormat("[Faulty Buttons #{0}] You pressed button {1} without any issues.", _moduleID, (pos + 1).ToString());
            }
        }
        if (PressedButtonCount == 16 && !Submitting)
            SubmitMode(pos);
        else if (PressedButtonCount == 16)
        {
            Module.HandlePass();
            Debug.LogFormat("[Uncoloured Buttons #{0}] All of the buttons have been successfully pressed. Module solved!", _moduleID);
            Solved = true;
            Audio.PlaySoundAtTransform("solve", Buttons[pos].transform);
            for (int i = 0; i < 16; i++)
                Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
        }
        PrevButton = pos;
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
    private string TwitchHelpMessage = "Use '!{0} A2' to press the button in column 1, row 2 or use '!{0} 5' to press the fifth button in reading order. You may string commands together, with spaces inbetween (eg. 'A1 B1 C1' or '1 2 3').";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.ToLowerInvariant();
        string[] CommandArray = command.Split(' ');
        string[] Numbers = { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12", "13", "14", "15", "16" };
        for (int i = 0; i < CommandArray.Length; i++)
        {
            if (!CoordinateNames.Contains(CommandArray[i].ToLowerInvariant()) && !Numbers.Contains(CommandArray[i]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
            yield return null;
            if (Numbers.Contains(CommandArray[i]))
                Buttons[int.Parse(CommandArray[i]) - 1].OnInteract();
            else
                Buttons[Array.IndexOf(CoordinateNames, CommandArray[i])].OnInteract();
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        yield return true;
        if (!Submitting)
        {
            for (int i = 0; i < 16; i++)
            {
                if (!PressedButtons[i])
                    Buttons[i].OnInteract();
                yield return true;
            }
        }
        for (int i = 0; i < 16; i++)
        {
            for (int j = 0; j < 16; j++)
            {
                if (!PressedButtons[j] && (!EnteringShape || ReferredButtons[PrevButton] == j))
                {
                    Buttons[j].OnInteract();
                    break;
                }
            }
            yield return true;
            for (int j = 0; j < 4; j++)
                yield return null;
        }
    }
}

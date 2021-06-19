using KModkit;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class UncolouredButtonsScript : MonoBehaviour
{

    static int _moduleIdCounter = 1;
    int _moduleID = 0;

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable[] Buttons;

    //Red = 0, Yellow = 1, Green = 2, Blue = 3
    private int[] Colours = new int[16];
    private int[] BinaryNumbers = new int[4];
    private int[] ConvertedBinaryNumbers = new int[4];
    private int Answer;
    private string[][] ColoursToString = new string[4][] { new string[4], new string[4], new string[4], new string[4] };
    private string[] CoordinateNames = { "a1", "b1", "c1", "d1", "a2", "b2", "c2", "d2", "a3", "b3", "c3", "d3", "a4", "b4", "c4", "d4" };
    private bool Pressing;
    private Color[] ColourValues = { new Color(.25f, .25f, .25f), new Color(.75f, .75f, .75f) };

    void Awake()
    {
        _moduleID = _moduleIdCounter++;
        for (int i = 0; i < 16; i++)
            Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
        Module.OnActivate += delegate
        {
            for (int i = 0; i < Buttons.Length; i++)
            {
                int x = i;
                Buttons[i].OnInteract += delegate { if (!Pressing) StartCoroutine(ButtonPress(x)); return false; };
            }
            Calculate();
            for (int i = 0; i < Buttons.Length; i++)
                Buttons[i].GetComponent<MeshRenderer>().material.color = ColourValues[Colours[i]];
        };
    }

    void Calculate()
    {
        for (int i = 0; i < 16; i++)
            Colours[i] = Rnd.Range(0, 2);
        for (int i = 0; i < 16; i++)
            ColoursToString[i / 4][i % 4] = Colours[i].ToString();
        for (int i = 0; i < 4; i++)
        {
            BinaryNumbers[i] = Colours[(Colours[i * 4] * 8) + (Colours[(i * 4) + 1] * 4) + (Colours[(i * 4) + 2] * 2) + Colours[(i * 4) + 3]];
            ConvertedBinaryNumbers[i] = (Colours[i * 4] * 8) + (Colours[(i * 4) + 1] * 4) + (Colours[(i * 4) + 2] * 2) + Colours[(i * 4) + 3];
        }
        Debug.Log(BinaryNumbers.Join());
        Answer = (BinaryNumbers[0] * 8) + (BinaryNumbers[1] * 4) + (BinaryNumbers[2] * 2) + BinaryNumbers[3];
        Debug.LogFormat("[Uncoloured Buttons #{0}] The grid of buttons:\n{1}", _moduleID, ColoursToString[0].Select(x => x == "0" ? x = "K" : x = "W").Join() + "\n" + ColoursToString[1].Select(x => x == "0" ? x = "K" : x = "W").Join() + "\n" + ColoursToString[2].Select(x => x == "0" ? x = "K" : x = "W").Join() + "\n" + ColoursToString[3].Select(x => x == "0" ? x = "K" : x = "W").Join());
        Debug.LogFormat("[Uncoloured Buttons #{0}] The resulting button is button {1} in reading order, or button {2}.", _moduleID, (Answer + 1).ToString(), CoordinateNames[Answer].ToUpperInvariant());
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
        for (int i = 0; i < 4; i++)
        {
            Buttons[ConvertedBinaryNumbers[i]].GetComponent<MeshRenderer>().material.color += new Color(.4f, .4f, .4f);
            Audio.PlaySoundAtTransform("bleep", Buttons[ConvertedBinaryNumbers[i]].transform);
            yield return new WaitForSeconds(0.1f);
        }
        for (int i = 0; i < 4; i++)
        {
            for (int j = 0; j < 4; j++)
                Buttons[(i * 4) + j].GetComponent<MeshRenderer>().material.color = new Color(1f, 1f, 1f);
            Audio.PlaySoundAtTransform("bleep", Buttons[ConvertedBinaryNumbers[i]].transform);
            yield return new WaitForSeconds(0.1f);
        }
        if (Answer == pos)
        {
            Module.HandlePass();
            Audio.PlaySoundAtTransform("solve", Buttons[pos].transform);
            Debug.LogFormat("[Uncoloured Buttons #{0}] You pressed button {1}, which was correct. Module solved!", _moduleID, CoordinateNames[pos].ToUpperInvariant());
            StartCoroutine(SolveAnim());
        }
        else
        {
            Module.HandleStrike();
            Audio.PlaySoundAtTransform("strike", Buttons[pos].transform);
            Debug.LogFormat("[Uncoloured Buttons #{0}] You pressed button {1}, which was incorrect. Strike!", _moduleID, CoordinateNames[pos].ToUpperInvariant());
            for (int i = 0; i < 16; i++)
                Buttons[i].GetComponent<MeshRenderer>().material.color = ColourValues[Colours[i]];
            Pressing = false;
        }
    }
    private IEnumerator SolveAnim()
    {
        for (int i = 0; i < 16; i++)
        {
            switch (Math.Abs(i / 4 - Answer / 4) + Math.Abs(i % 4 - Answer % 4))
            {
                case 0:
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 1f, 0);
                    break;
                case 1:
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0.8f, 0);
                    break;
                case 2:
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0.6f, 0);
                    break;
                case 3:
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0.4f, 0);
                    break;
                case 4:
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0.3f, 0);
                    break;
                case 5:
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0.2f, 0);
                    break;
                case 6:
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0.1f, 0);
                    break;
                default:
                    Buttons[i].GetComponent<MeshRenderer>().material.color = new Color(0, 0, 0);
                    break;
            }
        }
        yield return new WaitForSeconds(0.15f);
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 16; j++)
                Buttons[j].GetComponent<MeshRenderer>().material.color -= new Color(0, 0.2f, 0);
            yield return new WaitForSeconds(0.15f);
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

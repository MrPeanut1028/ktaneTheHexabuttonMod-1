using UnityEngine;
using KModkit;
using System.Collections;
using System.Linq;

public class hexabuttonScript : MonoBehaviour {

    public KMBombModule Module;
    public KMBombInfo Bomb;
    public KMAudio Audio;
    public KMSelectable btnSelectable;
    public TextMesh btnText;
    public Material stripMat;
    public MeshRenderer btnRenderer;
    public Texture[] litTextures;
    public Material normalMat;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool solved;

    private bool held;
    private bool released;
    private readonly string[] labels = { "Jump", "Boom", "Claim", "Button", "Hold", "Blue" };
    private readonly Color32[] actualBtnColors = { new Color32(0, 0, 0, 255), new Color32(69, 125, 195, 255), new Color32(195, 69, 69, 255), new Color32(195, 195, 69, 255), new Color32(45, 150, 45, 255) };
    private int labelNum;
    private readonly string[] btnColors = { "black", "blue", "red", "yellow", "green" };
    private readonly string[] stripColors = { "blue", "cyan", "gray", "green", "magenta", "purple", "white" };
    private int btnColorNum;
    private bool answerIsTap, answerCorrect = false;
    private int ruleApplied;
    private int typeOfLight, lightColor, letter;
    private readonly int[] solidLights = { 5, 1, 2 };
    private readonly int[] notSolidLights = { 0, 3, 4, 6 };
    private readonly int[] flickeringLights = { 3, 1, 4 };
    private readonly int[] notFlickeringLights = { 0, 2, 5, 6 };
    private readonly string[] typeStrings = { "solid", "flickering", "transmitting morse" };
    private readonly string[] morse = { ".-", "-...", "-.-.", "-..", ".", "..-.", "--.", "....", "..", ".---", "-.-", ".-..", "--",
                                        "-.", "---", ".--.", "--.-", ".-.", "...", "-", "..-", "...-", ".--", "-..-", "-.--", "--.." };
    private readonly string[] numbers = { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

    // Use this for initialization
    void Start () {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += SetUpButtons;

        GenerateModule();
    }

    void SetUpButtons()
    {
        btnSelectable.OnInteract += delegate ()
        {
            if (!solved)
                StartCoroutine("BtnHold");
            btnSelectable.AddInteractionPunch();
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, Module.transform);
            return false;
        };

        btnSelectable.OnInteractEnded += delegate ()
        {
            StopAllCoroutines();
            released = true;
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonRelease, Module.transform);
            if (solved)
                return;
            if (held)
                Release();
            else
                Tap();
        };
    }

    void GenerateModule()
    {
        labelNum = Random.Range(0, 6);
        btnText.text = labels[labelNum];

        DebugMsg("The button says ''" + labels[labelNum] + "''.");

        btnColorNum = Random.Range(0, 5);
        btnRenderer.material.color = actualBtnColors[btnColorNum];

        DebugMsg("The button is " + btnColors[btnColorNum] + ".");

        // calculate rules

        if (Bomb.GetIndicators().Contains("SND") || Bomb.GetIndicators().Contains("TRN"))
        {
            answerIsTap = false;
            DebugMsg("Rule 1 applied.");
        }

        else if (Bomb.GetBatteryCount() > 4)
        {
            answerIsTap = true;
            ruleApplied = 2;
            DebugMsg("Rule 2 applied.");
        }

        else if (Bomb.GetTwoFactorCounts() > 0)
        {
            answerIsTap = true;
            ruleApplied = 3;
            DebugMsg("Rule 3 applied.");
        }

        else if (labelNum == 4)
        {
            answerIsTap = true;
            ruleApplied = 4;
            DebugMsg("Rule 4 applied.");
        }

        else if (btnColorNum == 2 || btnColorNum == 4)
        {
            answerIsTap = true;
            ruleApplied = 5;
            DebugMsg("Rule 5 applied.");
        }

        else if (btnColorNum == 2 || labelNum == 0)
        {
            answerIsTap = false;
            DebugMsg("Rule 6 applied.");
        }

        else if (Bomb.GetIndicators().Count() > 4)
        {
            answerIsTap = true;
            ruleApplied = 7;
            DebugMsg("Rule 7 applied.");
        }

        else
        {
            answerIsTap = false;
            DebugMsg("Rule 8 applied.");
        }
    }

    IEnumerator BtnHold()
    {
        released = false;
        held = false;
        yield return new WaitForSeconds(1.3f);
        held = true;

        if (!released)
        {
            typeOfLight = Random.Range(0, 3);
            int lightColorNum = Random.Range(0, 4);

            if (typeOfLight == 0)
            {
                if (lightColorNum != 3)
                    lightColor = solidLights[lightColorNum];
                else
                    lightColor = notSolidLights[Random.Range(0, 4)];
                
                btnRenderer.material = stripMat;
                btnRenderer.material.mainTexture = litTextures[lightColor];
            }

            else if (typeOfLight == 1)
            {
                if (lightColorNum != 3)
                    lightColor = flickeringLights[lightColorNum];
                else
                    lightColor = notFlickeringLights[Random.Range(0, 4)];

                StartCoroutine(Flicker());
            }

            else
            {
                lightColor = 0;
                letter = Random.Range(0, 26);

                StartCoroutine(Morse());
            }

            DebugMsg("The button is now " + stripColors[lightColor] + " and " + typeStrings[typeOfLight] + ".");
        }
    }

    void Release()
    {
        DebugMsg("You released at " + Bomb.GetFormattedTime());
        btnRenderer.material = normalMat;
        btnRenderer.material.color = actualBtnColors[btnColorNum];

        if (answerIsTap)
        {
            Module.HandleStrike();
            DebugMsg("You were supposed to tap the button. STRIKE!");
            return;
        }

        if (typeOfLight == 0)
        {
            if (lightColor == 5)
            {
                if ((int)Bomb.GetTime() % 60 == 0)
                    answerCorrect = true;
            }

            else if (lightColor == 1)
            {
                if ((int)Bomb.GetTime() % 4 == 0)
                    answerCorrect = true;
            }

            else if (lightColor == 2)
            {
                if ((int)Bomb.GetTime() % Bomb.GetModuleNames().Count == 0 || Bomb.GetModuleNames().Count > 101)
                    answerCorrect = true;
            }
            
            else
            {
                int time = (int)Bomb.GetTime() % 300;
                int[] primes = { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101,
                                 103, 107, 109, 113, 127, 131, 137, 139, 149, 151, 157, 163, 167, 173, 179, 181, 191, 193, 197, 199,
                                 211, 223, 227, 229, 233, 239, 241, 251, 257, 263, 269, 271, 277, 281, 283, 293 };

                for (int i = -5; i < 6; i++)
                {
                    if (primes.Contains(time + i))
                    {
                        answerCorrect = true;
                        break;
                    }
                }
            }
        }

        else if (typeOfLight == 1)
        {
            if (lightColor == 3)
            {
                if (((int)Bomb.GetTime() / 60) % 2 != 0 || (int)Bomb.GetTime() / 60 == 0)
                    answerCorrect = true;
            }

            else if (lightColor == 1)
            {
                if ((int)Bomb.GetTime() % 7 == 0)
                    answerCorrect = true;
            }

            else if (lightColor == 4)
            {
                if (Bomb.GetFormattedTime().Contains("5") && Bomb.GetFormattedTime().Contains("0"))
                    answerCorrect = true;
            }

            else
            {
                if (Bomb.GetSolvableModuleNames().Count - 1 == Bomb.GetSolvedModuleNames().Count)
                    answerCorrect = true;

                if ((int)Bomb.GetTime() % 60 == (Bomb.GetSolvableModuleNames().Count - Bomb.GetSolvedModuleNames().Count) % 60)
                    answerCorrect = true;
            }
        }

        else
        {
            int releaseTime = letter + 1;

            if (Bomb.GetOnIndicators().Contains("BOB"))
                releaseTime += 11;
            if (Bomb.GetBatteryCount() > 0)
                releaseTime += 19;
            if (Bomb.GetPorts().Contains("USB") || Bomb.GetPorts().Contains("Serial"))
                releaseTime += 3;
            if (Bomb.GetSerialNumber().Contains("A") || Bomb.GetSerialNumber().Contains("E") || Bomb.GetSerialNumber().Contains("I") || Bomb.GetSerialNumber().Contains("O") || Bomb.GetSerialNumber().Contains("U"))
                releaseTime += 20;
            if (Bomb.GetOffIndicators().Count() == 0)
                releaseTime += 39;
            if (Bomb.GetOnIndicators().Contains("FRK"))
                releaseTime += 32;
            if (Bomb.GetModuleNames().Contains("Forget Me Not") || Bomb.GetModuleNames().Contains("Forget Everything"))
                releaseTime += 50;
            if (releaseTime == letter + 1)
                releaseTime += 1;

            releaseTime %= 60;
            if (releaseTime > 9)
                DebugMsg("You should release the button when the last two seconds digits on the timer are " + releaseTime + ".");
            else
                DebugMsg("You should release the button when the last two seconds digits on the timer are 0" + releaseTime + ".");

            if ((int)Bomb.GetTime() % 60 == releaseTime)
            {
                answerCorrect = true;
            }
        }

        if (answerCorrect)
        {
            DebugMsg("That was right!");
            Module.HandlePass();
            solved = true;
            StartCoroutine("SolveAnim");
        }

        else
        {
            DebugMsg("But that was wrong.");
            Module.HandleStrike();
        }
    }

    void Tap()
    {
        answerCorrect = false;
        DebugMsg("You tapped at " + Bomb.GetFormattedTime());

        if (!answerIsTap)
        {
            Module.HandleStrike();
            DebugMsg("You were supposed to hold the button. STRIKE!");
            return;
        }

        if (ruleApplied == 2)
        {
            if ((int)Bomb.GetTime() % 34 == 0)
                answerCorrect = true;
        }

        else if (ruleApplied == 3)
        {
            int[] twoFactors = new int[Bomb.GetTwoFactorCounts()];
            int arrayPos = 0;

            foreach (var twoFactor in Bomb.GetTwoFactorCodes())
            {
                twoFactors[arrayPos] = twoFactor % 100;
                arrayPos++;
            }

            if (twoFactors.Contains((int)Bomb.GetTime() % 60))
                answerCorrect = true;
        }

        else if (ruleApplied == 4)
        {
            string[] time = new string[Bomb.GetFormattedTime().Length];
            int occurences = 0;
            
            for (int i = 0; i < Bomb.GetFormattedTime().Length; i++)
            {
                time[i] = Bomb.GetFormattedTime()[i].ToString();
            }

            foreach (var number in time)
            {
                if (number != ":" && number != ".")
                {
                    occurences = 0;

                    foreach (var otherNumber in time)
                    {
                        if (otherNumber == number)
                        {
                            occurences++;
                        }
                    }

                    if (occurences >= 3)
                    {
                        answerCorrect = true;
                        break;
                    }
                }
            }
        }

        else if (ruleApplied == 5)
        {
            int sum = 0;

            foreach (var number in Bomb.GetSerialNumberNumbers())
                sum += number;

            sum %= 10;

            if (sum == 0)
            {
                answerCorrect = true;
            }

            else if ((int)Bomb.GetTime() % sum == 0)
            {
                answerCorrect = true;
            }

            DebugMsg("The sum of the serial number numbers is " + sum + ".");
        }

        else
        {
            if (((int)Bomb.GetTime() / 60) % 2 == 0 && (int)Bomb.GetTime() / 60 != 0)
            {
                answerCorrect = true;
            }
        }

        if (answerCorrect)
        {
            DebugMsg("That was right!");
            Module.HandlePass();
            solved = true;
            StartCoroutine("SolveAnim");
        }

        else
        {
            DebugMsg("But that was wrong.");
            Module.HandleStrike();
        }
    }

    void DebugMsg(string msg)
    {
        Debug.LogFormat("[The Hexabutton #{0}] {1}", _moduleId, msg.Replace("\n", " "));
    }

    IEnumerator Flicker()
    {
        bool btnIsColored = false;
        btnRenderer.material = normalMat;
        btnRenderer.material.color = actualBtnColors[0];

        while (!released)
        {
            if (btnIsColored)
            {
                btnRenderer.material = normalMat;
                btnRenderer.material.color = actualBtnColors[0];
                btnIsColored = false;
            }

            else
            {
                btnRenderer.material = stripMat;
                btnRenderer.material.mainTexture = litTextures[lightColor];
                btnIsColored = true;
            }

            yield return new WaitForSeconds(.15f);
        }
    }

    IEnumerator Morse()
    {
        string letterTransmitted = morse[letter];
        int messagePos = letterTransmitted.Length;
        DebugMsg("It's transmitting " + letterTransmitted + ", which has a value of " + (letter + 1) + ".");

        while (!released)
        {
            if (messagePos == letterTransmitted.Length)
            {
                messagePos = 0;
                btnRenderer.material = normalMat;
                btnRenderer.material.color = actualBtnColors[0];

                yield return new WaitForSeconds(1.25f);
            }

            else if (letterTransmitted[messagePos].ToString() == ".")
            {
                btnRenderer.material = stripMat;
                btnRenderer.material.mainTexture = litTextures[0];
                yield return new WaitForSeconds(.2f);
                btnRenderer.material = normalMat;
                btnRenderer.material.color = actualBtnColors[0];
                yield return new WaitForSeconds(.25f);

                messagePos++;
            }

            else
            {
                btnRenderer.material = stripMat;
                btnRenderer.material.mainTexture = litTextures[0];
                yield return new WaitForSeconds(1f);
                btnRenderer.material = normalMat;
                btnRenderer.material.color = actualBtnColors[0];
                yield return new WaitForSeconds(.25f);

                messagePos++;
            }
        }
    }

    IEnumerator SolveAnim()
    {
        bool whiteIsShown = true;
        btnRenderer.material = stripMat;

        for (int i = 0; i < 12; i++)
        {
            if (whiteIsShown)
            {
                whiteIsShown = false;
                btnRenderer.material = normalMat;
                btnRenderer.material.color = actualBtnColors[0];
                btnText.text = "SOLVED"[i / 2].ToString();
            }

            else
            {
                whiteIsShown = true;
                btnRenderer.material = stripMat;
            }

            yield return new WaitForSeconds(.075f);
        }

        btnRenderer.material.color = actualBtnColors[0];
        btnText.text = ":)";
    }

    bool TwitchShouldCancelCommand;
   public string TwitchHelpMessage = "Use !{0} tap X:XX / !{0} slap X:XX where X:XX is the last three digits of the time you want to tap the button on. Use !{0} hold to hold the button. Use !{0} release X:XX where X:XX is the last three digits of the time you want to release the button on.";
   
   IEnumerator ProcessTwitchCommand(string command)
   {
      string[] parameters = command.Trim().ToLowerInvariant().Split(' ');

      if (parameters.Length < 1)
         yield break;

      if ((parameters[0] == "tap" || parameters[0] == "slap" || parameters[0] == "release") && parameters.Length == 2)
      {
         yield return null;

         if (parameters[1].Length != 4)
         {
            yield return "sendtochaterror Invalid time!";
            yield break;
         }
         else if (!numbers.Contains(parameters[1][0].ToString()) || parameters[1][1] != ':' || !numbers.Contains(parameters[1][2].ToString()) || !numbers.Contains(parameters[1][3].ToString()))
         {
            yield return "sendtochaterror Invalid time!";
            yield break;
         }

         if ((held && parameters[0] != "release") || (!held && parameters[0] == "release"))
         {
            yield return string.Concat("sendtochaterror You can't ", parameters[0], " the button, ", (parameters[0] == "release" ? "it hasn't been held!" : "it's being held!"));
            yield break;
         }

         if (Bomb.GetFormattedTime().EndsWith(parameters[1]))
         {
            yield return "sendtochaterror Not enough time remaining to safely " + parameters[0] + " the button!";
            yield break;
         }

         while (!Bomb.GetFormattedTime().EndsWith(parameters[1]))
         {
            yield return "trycancel";
            yield return new WaitForSeconds(0.1f);
         }

         if (parameters[0] != "release")
         {
            btnSelectable.OnInteract();
            yield return new WaitForSeconds(0.1f);
         }
         btnSelectable.OnInteractEnded();
      }
      else if (parameters[0] == "hold" && parameters.Length == 1)
      {
         yield return null;

         if (held)
         {
            yield return "sendtochaterror Uh... it's already held.";
            yield break;
         }

         btnSelectable.OnInteract();
         yield return new WaitForSeconds(1.5f);
      }
   }
}

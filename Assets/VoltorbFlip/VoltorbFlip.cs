using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;
using System.Threading;
using PuzzleSolvers;

public class VoltorbFlip : MonoBehaviour
{
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMBombModule Module;

    public KMSelectable[] GridButtons;
    public MeshRenderer[] GridTiles;
    public Material[] Numbers;

    public KMSelectable ToggleButton;
    public TextMesh ToggleText;

    public TextMesh[] CoinCounts;
    public TextMesh[] VoltorbCounts;
    public TextMesh TotalCoins;

    // Solving info
    private int[] grid;
    private int[] rowSums;
    private int[] rowVol;
    private int[] colSums;
    private int[] colVol;

    private int coins = 0;
    private int displayedCoins = 0;
    private bool threadReady;
    private string error;

    private bool[] posPressed = new bool[25];
    private bool[] posMarked = new bool[25];
    private bool canPress = true;
    private bool marking = false;

    private bool addingCoins = false;

    private readonly string[] gridPositions = { "A1", "B1", "C1", "D1", "E1", "A2", "B2", "C2", "D2", "E2", "A3", "B3", "C3", "D3", "E3",
        "A4", "B4", "C4", "D4", "E4", "A5", "B5", "C5", "D5", "E5" };


    private readonly char[] letterValues = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '-', '-', '-', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    private readonly int[] portLetterCount = { 4, 8, 2, 2, 6, 9 };


    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;


    // Ran as bomb loads
    private void Awake()
    {
        moduleId = moduleIdCounter++;

        for (int i = 0; i < GridButtons.Length; i++)
        {
            int j = i;
            GridButtons[i].OnInteract += delegate () { GridButtonPress(j); return false; };
        }

        ToggleButton.OnInteract += delegate () { ToggleButtonPress(); return false; };
    }

    // Gets information
    private void Start()
    {
        StartCoroutine(CreateGrid());
    }


    // Creates the grid
    private IEnumerator CreateGrid()
    {
        var positions = EdgeworkPositions();
        var startSeed = UnityEngine.Random.Range(0, int.MaxValue);
        var thread = new Thread(() => RunAlgorithm(positions, startSeed));
        thread.Start();

        yield return new WaitUntil(() => threadReady);

        if (error != null)
        {
            Debug.LogFormat("[Vortorb Flip #{0}] {1}", moduleId, error);
            Module.HandlePass();
            yield break;
        }

        for (var i = 0; i < 5; i++)
        {
            CoinCounts[i].text = colSums[i].ToString("00");
            VoltorbCounts[i].text = colVol[i].ToString();
            CoinCounts[i + 5].text = rowSums[i].ToString("00");
            VoltorbCounts[i + 5].text = rowVol[i].ToString();
        }
        TotalCoins.text = "0000";

        // Logs the grid
        Debug.LogFormat("[Voltorb Flip #{0}] The grid on the module is as follows:", moduleId);
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[0]), V(grid[1]), V(grid[2]), V(grid[3]), V(grid[4]));
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[5]), V(grid[6]), V(grid[7]), V(grid[8]), V(grid[9]));
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[10]), V(grid[11]), V(grid[12]), V(grid[13]), V(grid[14]));
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[15]), V(grid[16]), V(grid[17]), V(grid[18]), V(grid[19]));
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[20]), V(grid[21]), V(grid[22]), V(grid[23]), V(grid[24]));
    }

    private bool isUnique(int[] rowSums, int[] rowVoltorbs, int[] colSums, int[] colVoltorbs, int[] givenCells)
    {
        var puzzle = new Puzzle(25, 0, 3);
        for (var i = 0; i < 5; i++)
        {
            puzzle.AddConstraint(new CombinationsConstraint(Enumerable.Range(0, 5).Select(j => j + 5 * i), PuzzleUtil.Combinations(0, 3, 5, true).Where(c => c.Sum() == rowSums[i] && c.Count(v => v == 0) == rowVoltorbs[i])));
            puzzle.AddConstraint(new CombinationsConstraint(Enumerable.Range(0, 5).Select(j => i + 5 * j), PuzzleUtil.Combinations(0, 3, 5, true).Where(c => c.Sum() == colSums[i] && c.Count(v => v == 0) == colVoltorbs[i])));
        }
        for (var i = 0; i < givenCells.Length; i++)
            puzzle.AddConstraint(new GivenConstraint(givenCells[i], 0));
        return puzzle.Solve().Take(2).Count() == 1;
    }

    private static T shuffle<T>(T list, System.Random random) where T : IList
    {
        if (list == null)
            throw new ArgumentNullException("list");
        for (int j = list.Count; j >= 1; j--)
        {
            int item = random.Next(0, j);
            if (item < j - 1)
            {
                var t = list[item];
                list[item] = list[j - 1];
                list[j - 1] = t;
            }
        }
        return list;
    }

    // This method runs in a separate thread — make sure not to interact with any Unity objects
    private void RunAlgorithm(int[] givens, int seed)
    {
        try
        {
            while (true)
            {
                var rnd = new System.Random(seed);
                var board = new List<int>();
                var numVoltorbs = rnd.Next(3, 7);
                for (var i = numVoltorbs; i >= 0; i--)
                    board.Add(0);
                for (var i = rnd.Next(3, 7); i >= 0; i--)
                    board.Add(2);
                for (var i = rnd.Next(3, 7); i >= 0; i--)
                    board.Add(3);
                while (board.Count < 25 - givens.Length)
                    board.Add(1);
                shuffle(board, rnd);
                for (var i = 0; i < givens.Length; i++)
                    board.Insert(givens[i], 0);

                rowSums = Enumerable.Range(0, 5).Select(row => Enumerable.Range(0, 5).Select(col => board[col + 5 * row]).Sum()).ToArray();
                rowVol = Enumerable.Range(0, 5).Select(row => Enumerable.Range(0, 5).Count(col => board[col + 5 * row] == 0)).ToArray();
                colSums = Enumerable.Range(0, 5).Select(col => Enumerable.Range(0, 5).Select(row => board[col + 5 * row]).Sum()).ToArray();
                colVol = Enumerable.Range(0, 5).Select(col => Enumerable.Range(0, 5).Count(row => board[col + 5 * row] == 0)).ToArray();

                if (isUnique(rowSums, rowVol, colSums, colVol, givens))
                {
                    grid = board.ToArray();
                    threadReady = true;
                    return;
                }
                seed = (seed + 1) % int.MaxValue;
            }
        }
        catch (Exception e)
        {
            error = string.Format("{0} ({1})", e.Message, e.GetType().FullName);
            threadReady = true;
        }
    }

    // Grid button is pressed
    private void GridButtonPress(int i)
    {
        GridButtons[i].AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, gameObject.transform);

        if (canPress && !posPressed[i])
        {
            if (!marking)
            {
                posPressed[i] = true;
                GridTiles[i].material = Numbers[grid[i]];

                if (grid[i] == 0)
                {
                    Debug.LogFormat("[Voltorb Flip #{0}] Revealed a Voltorb at {1}! You lost all your coins!", moduleId, gridPositions[i]);
                    coins = 0;
                    Audio.PlaySoundAtTransform("VF_Voltorb", transform);
                    GetComponent<KMBombModule>().HandleStrike();
                    StartCoroutine(IncrementCoins());
                }

                else
                {
                    if (coins == 0)
                    {
                        coins = grid[i];

                        switch (grid[i])
                        {
                            case 2: Audio.PlaySoundAtTransform("VF_Coin2", transform); break;
                            case 3: Audio.PlaySoundAtTransform("VF_Coin3", transform); break;
                            default: Audio.PlaySoundAtTransform("VF_Coin1", transform); break;
                        }

                        if (coins == 1)
                            Debug.LogFormat("[Voltorb Flip #{0}] Revealed a {2} at {1}! You now have 1 coin!", moduleId, gridPositions[i], grid[i]);

                        else
                            Debug.LogFormat("[Voltorb Flip #{0}] Revealed a {2} at {1}! You now have {3} coins!", moduleId, gridPositions[i], grid[i], coins);
                    }

                    else
                    {
                        coins = coins * grid[i];

                        if (grid[i] != 1)
                        {
                            Debug.LogFormat("[Voltorb Flip #{0}] Revealed a {2} at {1}! You now have {3} coins!", moduleId, gridPositions[i], grid[i], coins);

                            switch (grid[i])
                            {
                                case 2: Audio.PlaySoundAtTransform("VF_Coin2", transform); break;
                                case 3: Audio.PlaySoundAtTransform("VF_Coin3", transform); break;
                            }
                        }
                    }

                    if (!addingCoins)
                        StartCoroutine(IncrementCoins());
                }

                // All the 2s and 3s are revealed
                if (Enumerable.Range(0, 25).All(ix => grid[ix] < 2 || posPressed[ix]))
                {
                    Debug.LogFormat("[Voltorb Flip #{0}] Module solved! You win {1} coins!", moduleId, coins);
                    GetComponent<KMBombModule>().HandlePass();
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
                    canPress = false;
                    StartCoroutine(RevealGrid());
                }
            }

            // Marking a tile
            else
            {
                if (!posMarked[i])
                {
                    posMarked[i] = true;
                    GridTiles[i].material = Numbers[5];
                }

                else
                {
                    posMarked[i] = false;
                    GridTiles[i].material = Numbers[4];
                }
            }
        }
    }


    // Gets Voltorb positions from edgework
    private int[] EdgeworkPositions()
    {
        int[] pos = new int[2];

        // First position
        string serialNumber = Bomb.GetSerialNumber();
        pos[0] = 0;

        for (int i = 0; i < serialNumber.Length; i++)
        {
            for (int j = 0; j < letterValues.Length; j++)
            {
                if (serialNumber[i] == letterValues[j])
                {
                    if (j > 25) // Numbers
                        pos[0] += j - 30;

                    else // Letters
                        pos[0] += j + 1;

                    break;
                }
            }
        }

        pos[0] %= 25;

        if (pos[0] == 0)
            pos[0] = 24;

        else
            pos[0]--;


        // Second position
        int[] portCounts = new int[6];
        pos[1] = 0;

        portCounts[0] = Bomb.GetPortCount(Port.DVI);
        portCounts[1] = Bomb.GetPortCount(Port.Parallel);
        portCounts[2] = Bomb.GetPortCount(Port.PS2);
        portCounts[3] = Bomb.GetPortCount(Port.RJ45);
        portCounts[4] = Bomb.GetPortCount(Port.Serial);
        portCounts[5] = Bomb.GetPortCount(Port.StereoRCA);

        for (int i = 0; i < portCounts.Length; i++)
        {
            pos[1] += portCounts[i] * portLetterCount[i];
        }

        pos[1] %= 25;

        if (pos[1] == 0)
            pos[1] = 24;

        else
            pos[1]--;


        // If the two positions are the same
        if (pos[0] == pos[1])
        {
            pos[1]++;
            pos[1] %= 25;
        }

        return pos;
    }


    // Toggle button is pressed
    private void ToggleButtonPress()
    {
        ToggleButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);

        if (canPress)
        {
            if (!marking)
            {
                marking = true;
                ToggleText.text = "MARKING";
            }

            else
            {
                marking = false;
                ToggleText.text = "FLIPPING";
            }
        }
    }

    // Reveals the grid after the module solves
    private IEnumerator RevealGrid()
    {
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 5; i++)
        {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, gameObject.transform);

            for (int j = 0; j < 5; j++)
            {
                if (!posPressed[5 * j + i])
                {
                    posPressed[5 * j + i] = true;
                    GridTiles[5 * j + i].material = Numbers[grid[5 * j + i]];
                }
            }

            yield return new WaitForSeconds(0.25f);
        }
    }


    // Displays the coins
    private IEnumerator IncrementCoins()
    {
        addingCoins = true;
        int addedCoins = 0;

        if (displayedCoins > coins)
        {
            displayedCoins = coins;
            TotalCoins.text = displayedCoins.ToString("0000");
        }

        while (displayedCoins < coins)
        {
            displayedCoins++;
            addedCoins++;
            TotalCoins.text = displayedCoins.ToString("0000");

            yield return new WaitForSeconds(0.001f);
        }

        addingCoins = false;
    }


    // Checks if an area is a Voltorb for the logging
    private string V(int num)
    {
        if (num == 0)
            return "*";

        else
            return num.ToString();
    }


    // TP support - thanks to Danny7007


#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"Use !{0} flip A1 B2 C4 to flip those tiles. Use !{0} mark A1 B2 C4 to mark those tiles as having a Voltorb.";
#pragma warning restore 414
    private IEnumerator ProcessTwitchCommand(string Command)
    {
        Command = Command.Trim().ToUpperInvariant();
        List<string> parameters = Command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();
        if (Regex.IsMatch(Command, @"^(FLIP|MARK)\s*([A-E][1-5](\s*))+$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase) == true)
        {
            yield return null;
            bool toggling = (parameters.First() == "FLIP") ? marking : !marking;
            if (toggling)
            {
                ToggleButton.OnInteract();
                yield return new WaitForSeconds(0.15f);
            }
            parameters.Remove(parameters.First());
            foreach (string button in parameters)
            {
                GridButtons[Array.IndexOf(gridPositions, button)].OnInteract();
                yield return new WaitForSeconds(0.15f);
            }
        }
    }

    private IEnumerator TwitchHandleForcedSolve()
    {
        if (marking) { ToggleButton.OnInteract(); yield return new WaitForSeconds(0.15f); }
        for (int i = 0; i < 25; i++)
        {
            if (grid[i] > 1 && !posPressed[i])
            {
                GridButtons[i].OnInteract();
                yield return new WaitForSeconds(0.15f);
            }
        }
    }
}
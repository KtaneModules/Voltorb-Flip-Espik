using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using KModkit;
using PuzzleSolvers;
using RT.Util;
using RT.Util.ExtensionMethods;
using UnityEngine;

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
    public TextMesh CoinsText;

    public TextMesh[] CoinCounts;
    public TextMesh[] VoltorbCounts;
    public TextMesh TotalCoins;

    // Solving info
    private int[] grid;

    private int coins = 0;
    private int displayedCoins = 0;

    private bool threadReady;
    private string error;

    private bool[] posPressed = new bool[25];
    private bool[] posMarked = new bool[25];
    private bool canPress = true;
    private bool marking = false;
    
    private readonly int maximum = 99999;

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

        for (int i = 0; i < GridButtons.Length; i++)
            GridButtons[i].OnInteract += GridButtonPress(i);

        ToggleButton.OnInteract += delegate () { ToggleButtonPress(); return false; };

        for (var i = 0; i < 5; i++)
        {
            CoinCounts[i].text = Enumerable.Range(0, 5).Select(row => grid[i + 5 * row]).Sum().ToString("00");
            VoltorbCounts[i].text = Enumerable.Range(0, 5).Count(row => grid[i + 5 * row] == 0).ToString();
            CoinCounts[i + 5].text = Enumerable.Range(0, 5).Select(col => grid[col + 5 * i]).Sum().ToString("00");
            VoltorbCounts[i + 5].text = Enumerable.Range(0, 5).Count(col => grid[col + 5 * i] == 0).ToString();
        }

        TotalCoins.text = "0000";
        CoinsText.text = "Coins:";

        // Logs the grid
        Debug.LogFormat("[Voltorb Flip #{0}] Edgework positions: {1} and {2}", moduleId, gridPositions[positions[0]], gridPositions[positions[1]]);
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
        for (var gcIx = 0; gcIx < givenCells.Length; gcIx++)
            puzzle.AddConstraint(new GivenConstraint(givenCells[gcIx], 0));

        bool[] voltorbs = null;

        foreach (var solution in puzzle.Solve())
        {
            if (voltorbs == null)
                voltorbs = solution.Select(v => v == 0).ToArray();
            else
                for (var i = 0; i < solution.Length; i++)
                    if ((solution[i] == 0) != voltorbs[i])
                        return false;
        }
        return true;
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
    private void RunAlgorithm(int[] originalGivens, int seed)
    {
        var origRow1 = originalGivens[0] / 5;
        var origRow2 = originalGivens[1] / 5;
        var origCol1 = originalGivens[0] % 5;
        var origCol2 = originalGivens[1] % 5;

        var originalSameCol = origCol1 == origCol2;
        var originalSameRow = origRow1 == origRow2;

        try
        {
            for (; ; seed = (seed + 1) % int.MaxValue)
            {
                // Generate a random grid of 25 values (0 = voltorb)
                var rnd = new System.Random(seed);
                grid = new int[25];
                var ix = 0;

                var numVoltorbs = rnd.Next(6, 14);
                var num23s = rnd.Next(3, 11);
                var num2s = rnd.Next(0, num23s + 1);
                var num3s = num23s - num2s;

                if (numVoltorbs + num2s + num3s >= 25)
                    continue;

                for (var i = numVoltorbs; i > 0; i--)
                    grid[ix++] = 0;
                for (var i = num2s; i > 0; i--)
                    grid[ix++] = 2;
                for (var i = num3s; i > 0; i--)
                    grid[ix++] = 3;
                for (var i = ix; i < 25; i++)
                    grid[i] = 1;
                shuffle(grid, rnd);

                var rowSums = Enumerable.Range(0, 5).Select(row => Enumerable.Range(0, 5).Select(col => grid[col + 5 * row]).Sum()).ToArray();
                var rowVol = Enumerable.Range(0, 5).Select(row => Enumerable.Range(0, 5).Count(col => grid[col + 5 * row] == 0)).ToArray();
                var colSums = Enumerable.Range(0, 5).Select(col => Enumerable.Range(0, 5).Select(row => grid[col + 5 * row]).Sum()).ToArray();
                var colVol = Enumerable.Range(0, 5).Select(col => Enumerable.Range(0, 5).Count(row => grid[col + 5 * row] == 0)).ToArray();

                // Make sure that the puzzle is unique if all of the voltorbs were given
                var voltorbIxs = shuffle(grid.SelectIndexWhere(v => v == 0).ToArray(), rnd);
                if (!isUnique(rowSums, rowVol, colSums, colVol, voltorbIxs))
                    continue;

                // Find a minimal set of givens such that the puzzle is still unique
                var givens = Ut.ReduceRequiredSet(voltorbIxs, skipConsistencyTest: true,
                    test: state => isUnique(rowSums, rowVol, colSums, colVol, state.SetToTest.ToArray())).ToArray();

                // If there are exactly 2 required givens, we have found a potential candidate grid
                if (givens.Length != 2)
                    continue;

                var col1 = givens[0] % 5;
                var row1 = givens[0] / 5;
                var col2 = givens[1] % 5;
                var row2 = givens[1] / 5;

                var sameCol = col1 == col2;
                var sameRow = row1 == row2;

                // If the original givens are in the same row or column, we need our givens to also be in the same row or column.
                // Similarly, if the original givens are in different rows and columns, we also need our givens to be in different rows and columns.
                if ((originalSameRow || originalSameCol) != (sameRow || sameCol))
                    continue;

                // If the original givens are in the same row but ours are in the same column, or vice-versa, transpose the grid
                if ((sameCol && originalSameRow) || (sameRow && originalSameCol))
                {
                    grid = Ut.NewArray(25, i => grid[(i / 5) + 5 * (i % 5)]);
                    swap(ref row1, ref col1);
                    swap(ref row2, ref col2);
                }

                // Swap the rows and columns of the first givens
                grid = Ut.NewArray(25, i => grid[((i % 5 == origCol1) ? col1 : (i % 5 == col1) ? origCol1 : i % 5) + 5 * ((i / 5 == origRow1) ? row1 : (i / 5 == row1) ? origRow1 : i / 5)]);
                row2 = (row2 == origRow1) ? row1 : (row2 == row1) ? origRow1 : row2;
                col2 = (col2 == origCol1) ? col1 : (col2 == col1) ? origCol1 : col2;

                // Swap the rows and columns of the second givens
                grid = Ut.NewArray(25, i => grid[((i % 5 == origCol2) ? col2 : (i % 5 == col2) ? origCol2 : i % 5) + 5 * ((i / 5 == origRow2) ? row2 : (i / 5 == row2) ? origRow2 : i / 5)]);

                threadReady = true;
                return;
            }
        }
        catch (Exception e)
        {
            error = string.Format("{0} ({1})", e.Message, e.GetType().FullName);
            threadReady = true;
        }
    }

    private void swap(ref int item1, ref int item2)
    {
        var t = item1;
        item1 = item2;
        item2 = t;
    }

    // Grid button is pressed
    private KMSelectable.OnInteractHandler GridButtonPress(int i)
    {
        return delegate
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
            return false;
        };
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
    private void Update() {
        if (displayedCoins > coins) {
            displayedCoins = 0;
            TotalCoins.text = displayedCoins.ToString("0000");
        }
        
        if (displayedCoins < coins) {
            int coinsToDisplay = coins;

            if (coins > maximum)
                coinsToDisplay = maximum;


            else if (coinsToDisplay - displayedCoins > 10000 && displayedCoins >= 10000)
                displayedCoins += 10000;

            else if (coinsToDisplay - displayedCoins > 1000 && displayedCoins >= 1000)
                displayedCoins += 1000;

            else if (coinsToDisplay - displayedCoins > 100 && displayedCoins >= 100)
                displayedCoins += 100;

            else if (coinsToDisplay - displayedCoins > 10 && displayedCoins >= 10)
                displayedCoins += 10;

            else if (coinsToDisplay > displayedCoins)
                displayedCoins ++;

            if (displayedCoins < 10000)
                TotalCoins.text = displayedCoins.ToString("0000");

            else
                TotalCoins.text = displayedCoins.ToString();
        }
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
    IEnumerator Press(KMSelectable btn, float delay)
    {
        btn.OnInteract();
        yield return new WaitForSeconds(delay);
    }
    private IEnumerator ProcessTwitchCommand(string command)
    {
        command = command.Trim().ToUpperInvariant();
        Match m = Regex.Match(command, @"^(?:(FLIP|MARK)\s+)?((?:[A-E][1-5](?:\s+|$))+)$");
        if (m.Success)
        {
            yield return null;
            if ((m.Groups[1].Value == "FLIP" && marking) || (m.Groups[1].Value == "MARK" && !marking))
                yield return Press(ToggleButton, 0.15f);
            foreach (string coord in m.Groups[2].Value.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                yield return Press(GridButtons[(coord[0] - 'A') + 5 * (coord[1] - '1')], 0.15f);
        }
    }
    private IEnumerator TwitchHandleForcedSolve()
    {
        if (marking)
            yield return Press(ToggleButton, 0.15f);
        for (int i = 0; i < 25; i++)
            if (grid[i] > 1 && !posPressed[i])
                yield return Press(GridButtons[i], 0.15f);
    }
}
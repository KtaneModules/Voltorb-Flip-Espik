using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class VoltorbFlip : MonoBehaviour {
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
    private int[] grid = new int[25];
    private int coins = 0;
    private int displayedCoins = 0;
    private int tilesLeft = 0;

    private bool[] posPressed = new bool[25];
    private bool[] posMarked = new bool[25];
    private bool canPress = true;
    private bool marking = false;

    private bool addingCoins = false;

    private bool randomMode = true; // Change this to false once the algorithm is finished.

    private readonly string[] gridPositions = { "A1", "B1", "C1", "D1", "E1", "A2", "B2", "C2", "D2", "E2", "A3", "B3", "C3", "D3", "E3",
        "A4", "B4", "C4", "D4", "E4", "A5", "B5", "C5", "D5", "E5" };


    private readonly char[] letterValues = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '-', '-', '-', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
    private readonly int[] portLetterCount = { 4, 8, 2, 2, 6, 9 };


    // Logging info
    private static int moduleIdCounter = 1;
    private int moduleId;


    // Ran as bomb loads
    private void Awake() {
        moduleId = moduleIdCounter++;
        
        for (int i = 0; i < GridButtons.Length; i++) {
            int j = i;
            GridButtons[i].OnInteract += delegate () { GridButtonPress(j); return false; };
        }

        ToggleButton.OnInteract += delegate () { ToggleButtonPress(); return false; };
	}

    // Gets information
    private void Start() {
        for (int i = 0; i < grid.Length; i++) {
            grid[i] = -1;
            posPressed[i] = false;
            posMarked[i] = false;
        }

        CreateGrid();
        CalculateMarkers();
    }


    // Creates the grid
    private void CreateGrid() {
        // New solvable method
        if (!randomMode) {
            // Places preemptive Voltorb
            int[] positions = EdgeworkPositions();
            grid[positions[0]] = 0;
            grid[positions[1]] = 0;

            // New algorithm goes here. Remember to switch randomMode to false.
        }

        // Old random method
        else {
            int[] availablePostions = new int[25];
            int remainingPositions = 25;

            for (int i = 0; i < availablePostions.Length; i++)
                availablePostions[i] = i;

            // Counts of each tile (2, 3, Voltorb)
            int[] n = new int[3];

            int rand = UnityEngine.Random.Range(0, 40);

            // Gets the configuation
            switch (rand) {
            case 1: n[0] = 0; n[1] = 3; n[2] = 6; break;
            case 2: n[0] = 5; n[1] = 0; n[2] = 6; break;
            case 3: n[0] = 2; n[1] = 2; n[2] = 6; break;
            case 4: n[0] = 4; n[1] = 1; n[2] = 6; break;
            case 5: n[0] = 1; n[1] = 3; n[2] = 7; break;
            case 6: n[0] = 6; n[1] = 0; n[2] = 7; break;
            case 7: n[0] = 3; n[1] = 2; n[2] = 7; break;
            case 8: n[0] = 0; n[1] = 4; n[2] = 7; break;
            case 9: n[0] = 5; n[1] = 1; n[2] = 7; break;
            case 10: n[0] = 2; n[1] = 3; n[2] = 8; break;
            case 11: n[0] = 7; n[1] = 0; n[2] = 8; break;
            case 12: n[0] = 4; n[1] = 2; n[2] = 8; break;
            case 13: n[0] = 1; n[1] = 4; n[2] = 8; break;
            case 14: n[0] = 6; n[1] = 1; n[2] = 8; break;
            case 15: n[0] = 3; n[1] = 3; n[2] = 8; break;
            case 16: n[0] = 0; n[1] = 5; n[2] = 8; break;
            case 17: n[0] = 8; n[1] = 0; n[2] = 10; break;
            case 18: n[0] = 5; n[1] = 2; n[2] = 10; break;
            case 19: n[0] = 2; n[1] = 4; n[2] = 10; break;
            case 20: n[0] = 7; n[1] = 1; n[2] = 10; break;
            case 21: n[0] = 4; n[1] = 3; n[2] = 10; break;
            case 22: n[0] = 1; n[1] = 5; n[2] = 10; break;
            case 23: n[0] = 9; n[1] = 0; n[2] = 10; break;
            case 24: n[0] = 6; n[1] = 2; n[2] = 10; break;
            case 25: n[0] = 3; n[1] = 4; n[2] = 10; break;
            case 26: n[0] = 0; n[1] = 6; n[2] = 10; break;
            case 27: n[0] = 8; n[1] = 1; n[2] = 10; break;
            case 28: n[0] = 5; n[1] = 3; n[2] = 10; break;
            case 29: n[0] = 2; n[1] = 5; n[2] = 10; break;
            case 30: n[0] = 7; n[1] = 2; n[2] = 10; break;
            case 31: n[0] = 4; n[1] = 4; n[2] = 10; break;
            case 32: n[0] = 1; n[1] = 6; n[2] = 13; break;
            case 33: n[0] = 9; n[1] = 1; n[2] = 13; break;
            case 34: n[0] = 6; n[1] = 3; n[2] = 10; break;
            case 35: n[0] = 0; n[1] = 7; n[2] = 10; break;
            case 36: n[0] = 8; n[1] = 2; n[2] = 10; break;
            case 37: n[0] = 5; n[1] = 4; n[2] = 10; break;
            case 38: n[0] = 2; n[1] = 6; n[2] = 10; break;
            case 39: n[0] = 7; n[1] = 3; n[2] = 10; break;
            default: n[0] = 3; n[1] = 1; n[2] = 6; break;
            }

            // Places the tiles on the grid
            for (int i = 0; i < n[0]; i++) { // 2s
                rand = UnityEngine.Random.Range(0, remainingPositions);

                grid[availablePostions[rand]] = 2;
                tilesLeft++;

                for (int j = rand; j < remainingPositions - 1; j++)
                    availablePostions[j] = availablePostions[j + 1];

                remainingPositions--;
            }

            for (int i = 0; i < n[1]; i++) { // 3s
                rand = UnityEngine.Random.Range(0, remainingPositions);

                grid[availablePostions[rand]] = 3;
                tilesLeft++;

                for (int j = rand; j < remainingPositions - 1; j++)
                    availablePostions[j] = availablePostions[j + 1];

                remainingPositions--;
            }

            for (int i = 0; i < n[2]; i++) { // Voltorb
                rand = UnityEngine.Random.Range(0, remainingPositions);

                grid[availablePostions[rand]] = 0;

                for (int j = rand; j < remainingPositions - 1; j++)
                    availablePostions[j] = availablePostions[j + 1];

                remainingPositions--;
            }

            // Fills the remaining spots on the grid with 1s
            for (int i = 0; i < grid.Length; i++) {
                if (grid[i] == -1)
                    grid[i] = 1;
            }
        }
        

        // Logs the grid
        Debug.LogFormat("[Voltorb Flip #{0}] The grid on the module is as follows:", moduleId);
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[0]), V(grid[1]), V(grid[2]), V(grid[3]), V(grid[4]));
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[5]), V(grid[6]), V(grid[7]), V(grid[8]), V(grid[9]));
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[10]), V(grid[11]), V(grid[12]), V(grid[13]), V(grid[14]));
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[15]), V(grid[16]), V(grid[17]), V(grid[18]), V(grid[19]));
        Debug.LogFormat("[Voltorb Flip #{0}] {1} {2} {3} {4} {5}", moduleId, V(grid[20]), V(grid[21]), V(grid[22]), V(grid[23]), V(grid[24]));
    }

    // Determines the numbers on the sides of the rows and columns
    private void CalculateMarkers() {
        /* 0: Col A
         * 1: Col B
         * 2: Col C
         * 3: Col D
         * 4: Col E
         * 5: Row 1
         * 6: Row 2
         * 7: Row 3
         * 8: Row 4
         * 9: Row 5
         */

        int[] coinMarkings = new int[10];
        int[] voltorbMarkings = new int[10];

        // Columns
        for (int i = 0; i < 5; i++) {
            coinMarkings[i] = grid[i] + grid[i + 5] + grid[i + 10] + grid[i + 15] + grid[i + 20];

            for (int j = 0; j < 5; j++) {
                if (grid[5 * j + i] == 0)
                    voltorbMarkings[i]++;
            }
        }

        // Rows
        for (int i = 5; i < 10; i++) {
            int row = i - 5;

            coinMarkings[i] = grid[5 * row] + grid[5 * row + 1] + grid[5 * row + 2] + grid[5 * row + 3] + grid[5 * row + 4];

            for (int j = 0; j < 5; j++) {
                if (grid[5 * row + j] == 0)
                    voltorbMarkings[i]++;
            }
        }

        for (int i = 0; i < coinMarkings.Length; i++) {
            if (coinMarkings[i] < 10)
                CoinCounts[i].text = "0" + coinMarkings[i].ToString();

            else
                CoinCounts[i].text = coinMarkings[i].ToString();

            VoltorbCounts[i].text = voltorbMarkings[i].ToString();
        }
    }


    // Grid button is pressed
    private void GridButtonPress(int i) {
        GridButtons[i].AddInteractionPunch(0.5f);
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, gameObject.transform);

        if (canPress && !posPressed[i]) {
            if (!marking) {
                posPressed[i] = true;
                GridTiles[i].material = Numbers[grid[i]];

                if (grid[i] == 0) {
                    Debug.LogFormat("[Voltorb Flip #{0}] Revealed a Voltorb at {1}! You lost all your coins!", moduleId, gridPositions[i]);
                    coins = 0;
                    Audio.PlaySoundAtTransform("VF_Voltorb", transform);
                    GetComponent<KMBombModule>().HandleStrike();
                    StartCoroutine(IncrementCoins());
                }

                else {
                    if (coins == 0) {
                        coins = grid[i];

                        switch (grid[i]) {
                            case 2: Audio.PlaySoundAtTransform("VF_Coin2", transform); break;
                            case 3: Audio.PlaySoundAtTransform("VF_Coin3", transform); break;
                            default: Audio.PlaySoundAtTransform("VF_Coin1", transform); break;
                        }

                        if (coins == 1)
                            Debug.LogFormat("[Voltorb Flip #{0}] Revealed a {2} at {1}! You now have 1 coin!", moduleId, gridPositions[i], grid[i]);

                        else
                            Debug.LogFormat("[Voltorb Flip #{0}] Revealed a {2} at {1}! You now have {3} coins!", moduleId, gridPositions[i], grid[i], coins);
                    }

                    else {
                        coins = coins * grid[i];

                        if (grid[i] != 1) {
                            Debug.LogFormat("[Voltorb Flip #{0}] Revealed a {2} at {1}! You now have {3} coins!", moduleId, gridPositions[i], grid[i], coins);

                            switch (grid[i]) {
                                case 2: Audio.PlaySoundAtTransform("VF_Coin2", transform); break;
                                case 3: Audio.PlaySoundAtTransform("VF_Coin3", transform); break;
                            }
                        }
                    }

                    if (grid[i] != 1)
                        tilesLeft--;

                    if (!addingCoins)
                        StartCoroutine(IncrementCoins());
                }

                // All the 2s and 3s are revealed
                if (tilesLeft < 1) {
                    Debug.LogFormat("[Voltorb Flip #{0}] Module solved! You win {1} coins!", moduleId, coins);
                    GetComponent<KMBombModule>().HandlePass();
                    Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, gameObject.transform);
                    canPress = false;
                    StartCoroutine(RevealGrid());
                }
            }

            // Marking a tile
            else {
                if (!posMarked[i]) {
                    posMarked[i] = true;
                    GridTiles[i].material = Numbers[5];
                }

                else {
                    posMarked[i] = false;
                    GridTiles[i].material = Numbers[4];
                }
            }
        }
    }


    // Gets Voltorb positions from edgework
    private int[] EdgeworkPositions() {
        int[] pos = new int[2];

        // First position
        string serialNumber = Bomb.GetSerialNumber();
        pos[0] = 0;

        for (int i = 0; i < serialNumber.Length; i++) {
            for (int j = 0; j < letterValues.Length; j++) {
                if (serialNumber[i] == letterValues[j]) {
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

        for (int i = 0; i < portCounts.Length; i++) {
            pos[1] += portCounts[i] * portLetterCount[i];
        }

        pos[1] %= 25;

        if (pos[1] == 0)
            pos[1] = 24;

        else
            pos[1]--;


        // If the two positions are the same
        if (pos[0] == pos[1]) {
            pos[1]++;
            pos[1] %= 25;
        }

        return pos;
    }


    // Toggle button is pressed
    private void ToggleButtonPress() {
        ToggleButton.AddInteractionPunch();
        Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, gameObject.transform);

        if (canPress) {
            if (!marking) {
                marking = true;
                ToggleText.text = "MARKING";
            }

            else {
                marking = false;
                ToggleText.text = "FLIPPING";
            }
        }
    }

    // Reveals the grid after the module solves
    private IEnumerator RevealGrid() {
        yield return new WaitForSeconds(1.0f);
        for (int i = 0; i < 5; i++) {
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, gameObject.transform);

            for (int j = 0; j < 5; j++) {
                if (!posPressed[5 * j + i]) {
                    posPressed[5 * j + i] = true;
                    GridTiles[5 * j + i].material = Numbers[grid[5 * j + i]];
                }
            }

            yield return new WaitForSeconds(0.25f);
        }
    }


    // Displays the coins
    private IEnumerator IncrementCoins() {
        addingCoins = true;
        int addedCoins = 0;

        if (displayedCoins > coins) {
            displayedCoins = coins;
            TotalCoins.text = FormatCoins(displayedCoins);
        }

        while (displayedCoins < coins) {
            displayedCoins++;
            addedCoins++;
            TotalCoins.text = FormatCoins(displayedCoins);

            yield return new WaitForSeconds(0.001f);
        }

        addingCoins = false;
    }
        

    // Checks if an area is a Voltorb for the logging
    private string V(int num) {
        if (num == 0)
            return "*";

        else
            return num.ToString();
    }

    // Format the coin counts
    private string FormatCoins(int num) {
        if (num < 10)
            return "000" + num;

        else if (num < 100)
            return "00" + num;

        else if (num < 1000)
            return "0" + num;

        else return num.ToString();
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
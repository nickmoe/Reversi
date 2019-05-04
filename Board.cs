using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Board : MonoBehaviour
{
    public enum PlayerTurn
    {
        Start,
        Black,
        White,
        End
    }

    private enum PieceType
    {
        None,
        White,
        Black
    }

    PieceType[,] gameBoard;

    public PlayerTurn turn;

    private Text turnText;
    private Text whiteScoreText;
    private Text blackScoreText;

    int whiteScore;
    int blackScore;

    private int aiLevel;

    // Start is called before the first frame update
    void Start()
    {
        gameBoard = new PieceType[8, 8];

        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                gameBoard[i, j] = PieceType.None;
            }
        }

        gameBoard[3, 3] = PieceType.White;
        gameBoard[4, 4] = PieceType.White;
        gameBoard[3, 4] = PieceType.Black;
        gameBoard[4, 3] = PieceType.Black;

        turnText = GameObject.Find("PlayerTurnText").GetComponent<Text>();
        whiteScoreText = GameObject.Find("WhiteScoreText").GetComponent<Text>();
        blackScoreText = GameObject.Find("BlackScoreText").GetComponent<Text>();

        turn = PlayerTurn.Start;
        turnText.text = "Choose an AI Level";
    }

    public void setAI(int ai)
    {
        aiLevel = ai;

        // Remove buttons
        if(aiLevel > 0)
        {
            GameObject.Find("AI1").SetActive(false);
            GameObject.Find("AI2").SetActive(false);
            GameObject.Find("AI3").SetActive(false);
            GameObject.Find("AI4").SetActive(false);
            GameObject.Find("AI5").SetActive(false);
            GameObject.Find("Title").SetActive(false);
            GameObject.Find("Name").SetActive(false);

            turn = PlayerTurn.Black;
        }
    }

    private float dx = 0f;
    private bool canMove = false;

    // Update is called once per frame
    void Update()
    {
        // Updates score
        whiteScore = 0;
        blackScore = 0;

        for(int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (gameBoard[i,j] == PieceType.Black){ blackScore += 1; }
                else if(gameBoard[i,j] == PieceType.White) { whiteScore += 1; }
            }
        }
        whiteScoreText.text = "White: " + whiteScore;
        blackScoreText.text = "Black: " + blackScore;

        // Wait for button press
        if(turn == PlayerTurn.Start)
        {

        }
        // Player
        else if (turn == PlayerTurn.Black)
        {
            if (!canMove)
            {
                if (findValidSpace(turn))
                {
                    canMove = true;
                }
                else
                {
                    turn = PlayerTurn.White;
                    canMove = false;

                    if (isGameOver())
                        endgame();
                }
            }

            turnText.text = "Black";
        }
        // AI
        else if (turn == PlayerTurn.White)
        {
            if (!canMove)
            {
                if (findValidSpace(turn))
                {
                    canMove = true;
                }
                else
                {
                    turn = PlayerTurn.Black;
                    canMove = false;

                    if (isGameOver())
                        endgame();
                }
            }

            turnText.text = "White";

            // Allows the pieces to go to their place
            if (Time.time - dx >= 1.5f)
            {

                Tuple<int, Vector2Int> play = minimax(gameBoard, turn, aiLevel, 0);

                playTurn(play.Item2, turn);

                turn = PlayerTurn.Black;
            }
        }
        // End game
        else
        {

        }
    }

    private void OnMouseDown()
    {
        // Player
        if (turn == PlayerTurn.Black)
        {
            Vector3 mouse = Input.mousePosition;
            Ray castPoint = Camera.main.ScreenPointToRay(mouse);
            RaycastHit[] h = Physics.RaycastAll(castPoint);

            foreach (RaycastHit hit in h)
            {

                if (hit.collider.name.Equals("Model_Board"))
                {
                    
                    if(isValidSpace(calculateBoardCoord(hit.point), turn, this.gameBoard) > 0)
                    {
                        playTurn(calculateBoardCoord(hit.point), turn);

                        turn = PlayerTurn.White;

                        dx = Time.time;
                    }

                }
            }
        }
        // Place for player 2
        else
        {

        }
    }

    private bool findValidSpace(PlayerTurn t)
    {
        for(int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (isValidSpace(new Vector2(i, j), t, gameBoard) > 0)
                {
                    return true;
                }
            }
        }

        return false;
    }

    /**
     * Helper function to translate pieces to board coordinates
     */ 
    private Vector2 calculateBoardCoord(Vector3 point)
    {
        float x = (float) Math.Round(point.x + 0.0f, 0) * -1.0f;
        float y = (float) Math.Round(point.z - 0.0f, 0) * 1.0f;

        Vector2 ret = new Vector2(x, y);

        return ret;
    }

    private void playTurn(Vector2 space, PlayerTurn t)
    {
        Vector3Int[] linesXYCount = new Vector3Int[8];

        linesXYCount = calculateLines(space, gameBoard, t);

        int count = flipPieces(t, gameBoard, linesXYCount, space, true) + 1;

        GameObject.Find("/Piece[" + (int)space.x + "," + (int)space.y + "]").GetComponent<Piece>().activatePiece(t == PlayerTurn.Black);

        gameBoard[(int)space.x, (int)space.y] = chooseFriendlyPiece(t);
    }

    private void playTurn(Vector2 space, PlayerTurn t, PieceType[,] board)
    {
        //GameObject.Find("/Piece[" + space.x + "," + space.y + "]").GetComponent<Piece>().activatePiece(t == PlayerTurn.Black);

        Vector3Int[] linesXYCount = new Vector3Int[8];

        linesXYCount = calculateLines(space, board, t);

        int count = flipPieces(t, board, linesXYCount, space, false);

        board[(int)space.x, (int)space.y] = chooseFriendlyPiece(t);
    }

    /**
     * Determines if given space is a valid play for the player on the board.
     */ 
    private int isValidSpace(Vector2 space, PlayerTurn t, PieceType[,] board)
    {
        int ret = 0;

        if(board[(int) space.x, (int) space.y] == PieceType.None)
        {
            Vector3Int[] linesXYCount = new Vector3Int[8];

            linesXYCount = calculateLines(space, board, t);

            foreach(Vector3Int l in linesXYCount)
            {
                ret += l.z;
            }

            return ret;
        }

        return -1;
    }

    private PieceType[,] copyGameBoard(PieceType[,] board)
    {
        PieceType[,] ret = new PieceType[8, 8];

        for(int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                ret[i, j] = board[i, j];
            }
        }

        return ret;
    }

    private void endgame()
    {
        if (whiteScore > blackScore)
            turnText.text = "WHITE WINS!!";
        else if (blackScore > whiteScore)
            turnText.text = "BLACK WINS!!";
        else
            turnText.text = "DRAW!!";

        turn = PlayerTurn.End;
    }

    private bool isGameOver()
    {
        if(!findValidSpace(PlayerTurn.Black) && !findValidSpace(PlayerTurn.White))
        {
            endgame();

            return true;
        }
        return false;
    }

    private bool isGameOver(PieceType[,] board)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (board[i, j] == PieceType.None)
                {
                    if(isValidSpace(new Vector2(i, j), PlayerTurn.Black, board) + isValidSpace(new Vector2(i, j), PlayerTurn.White, board) > 0)
                    {
                        return false;
                    }
                }
            }
        }

        endgame();

        return true;
    }

    /**
     * Super simple evaluation function
     */ 
    private int eval(PlayerTurn t, PieceType[,] board)
    {
        int ret = 0;

        for(int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (board[i, j] == chooseFriendlyPiece(t))
                {
                    // Corners
                    if ((i == 0 && j == 0) ||
                        (i == 0 && j == 7) ||
                        (i == 7 && j == 0) ||
                        (i == 7 && j == 7))
                    {
                        ret += 4;
                    }
                    // Edges
                    else if (i == 0 || i == 7 || j == 0 || j == 7)
                    {
                        ret += 2;
                    }
                    // Regular Spaces
                    else
                    {
                        ret += 1;
                    }
                }
            }
        }

        return ret;
    }

    private PlayerTurn chooseEnemy(PlayerTurn t) { return t == PlayerTurn.Black ? PlayerTurn.White : PlayerTurn.Black; }

    private Tuple<int , Vector2Int> minimax(PieceType[,] board, PlayerTurn t, int maxDepth, int curDepth)
    {
        int bestScore;
        Vector2Int bestMove = new Vector2Int(-1, -1);
        
        if(isGameOver(board) || curDepth == maxDepth)
        {
            return Tuple.Create<int, Vector2Int>(eval(t, board), new Vector2Int(-1, -1));
        }

        if (t == turn)
        {
            bestScore = int.MinValue;
        }
        else
        {
            bestScore = int.MaxValue;
        }

        // Find possible spaces
        List<Vector2Int> possible = new List<Vector2Int>(8);

        for(int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                Vector2Int p = new Vector2Int(i, j);

                if(isValidSpace(p, t, board) > 0)
                {
                    possible.Add(p);
                }
            }
        }

        // Test each possible move
        foreach(Vector2Int p in possible)
        {
            PieceType[,] mm = copyGameBoard(board);
            playTurn(p, t, mm);

            Tuple<int, Vector2Int> current = minimax(mm, chooseEnemy(t), maxDepth, curDepth + 1);

            if(t == turn)
            {
                if(current.Item1 > bestScore)
                {
                    bestScore = current.Item1;
                    bestMove = p;
                }
            }
            else
            {
                if (current.Item1 < bestScore)
                {
                    bestScore = current.Item1;
                    bestMove = p;
                }
            }
        }

        return Tuple.Create<int, Vector2Int>(bestScore, bestMove);
    }

    /**
     * Finds the locations of lines to next piece
     */
    private Vector3Int[] calculateLines(Vector2 start, PieceType[,] board, PlayerTurn t)
    {
        Vector3Int[] ret = new Vector3Int[8];
        int countEnemy = 0;
        int direction = 1;

        // Up-Left
        ret[0] = new Vector3Int((int)start.x, (int)start.y, countEnemy);

        while ((int)start.x - direction >= 0 && (int)start.y - direction >= 0 && board[(int)start.x - direction, (int)start.y - direction] != PieceType.None)
        {
            // Count Enemy Tiles
            if (board[(int)start.x - direction, (int)start.y - direction] == chooseEnemyPiece(t))
            {
                countEnemy++;
            }
            // Found Friendly Tile
            else if (board[(int)start.x - direction, (int)start.y - direction] == chooseFriendlyPiece(t))
            {
                ret[0] = new Vector3Int((int)start.x - direction, (int)start.y - direction, countEnemy);

                break;
            }
            // Found Empty Tile
            else
            {
                ret[0] = new Vector3Int((int)start.x - direction, (int)start.y - direction, 0);

                break;
            }

            direction++;
        }

        countEnemy = 0;
        direction = 1;

        // Up
        ret[1] = new Vector3Int((int)start.x, (int)start.y, countEnemy);

        while ((int)start.y - direction >= 0 && board[(int)start.x, (int)start.y - direction] != PieceType.None)
        {
            // Count Enemy Tiles
            if (board[(int)start.x , (int)start.y - direction] == chooseEnemyPiece(t))
            {
                countEnemy++;
            }
            // Found Friendly Tile
            else if (board[(int)start.x , (int)start.y - direction] == chooseFriendlyPiece(t))
            {
                ret[1] = new Vector3Int((int)start.x , (int)start.y - direction, countEnemy);

                break;
            }
            // Found Empty Tile
            else
            {
                ret[1] = new Vector3Int((int)start.x , (int)start.y - direction, 0);

                break;
            }

            direction++;
        }

        countEnemy = 0;
        direction = 1;

        // Up-Right
        ret[2] = new Vector3Int((int)start.x, (int)start.y, countEnemy);

        while ((int)start.x + direction < 8 && (int)start.y - direction >= 0 && board[(int)start.x + direction, (int)start.y - direction] != PieceType.None)
        {
            // Count Enemy Tiles
            if (board[(int)start.x + direction, (int)start.y - direction] == chooseEnemyPiece(t))
            {
                countEnemy++;
            }
            // Found Friendly Tile
            else if (board[(int)start.x + direction, (int)start.y - direction] == chooseFriendlyPiece(t))
            {
                ret[2] = new Vector3Int((int)start.x + direction, (int)start.y - direction, countEnemy);

                break;
            }
            // Found Empty Tile
            else
            {
                ret[2] = new Vector3Int((int)start.x + direction, (int)start.y - direction, 0);

                break;
            }

            direction++;
        }

        countEnemy = 0;
        direction = 1;

        // Left
        ret[3] = new Vector3Int((int)start.x, (int)start.y, countEnemy);

        while ((int)start.x - direction >= 0 && board[(int)start.x - direction, (int)start.y] != PieceType.None)
        {
            // Count Enemy Tiles
            if (board[(int)start.x - direction, (int)start.y] == chooseEnemyPiece(t))
            {
                countEnemy++;
            }
            // Found Friendly Tile
            else if (board[(int)start.x - direction, (int)start.y] == chooseFriendlyPiece(t))
            {
                ret[3] = new Vector3Int((int)start.x - direction, (int)start.y, countEnemy);

                break;
            }
            // Found Empty Tile
            else
            {
                ret[3] = new Vector3Int((int)start.x - direction, (int)start.y, 0);

                break;
            }

            direction++;
        }

        countEnemy = 0;
        direction = 1;

        // Right (Runs into random Index out of bounds error,  no idea why)
        ret[4] = new Vector3Int((int)start.x, (int)start.y, countEnemy);

        while ((int)start.x + direction < 8 && board[(int)start.x + direction, (int)start.y] != PieceType.None)
        {
            // Count Enemy Tiles
            if (board[(int)start.x + direction, (int)start.y] == chooseEnemyPiece(t))
            {
                countEnemy++;
            }
            // Found Friendly Tile
            else if (board[(int)start.x + direction, (int)start.y] == chooseFriendlyPiece(t))
            {
                ret[4] = new Vector3Int((int)start.x + direction, (int)start.y, countEnemy);

                break;
            }
            // Found Empty Tile
            else
            {
                ret[4] = new Vector3Int((int)start.x + direction, (int)start.y, 0);

                break;
            }

            direction++;
        }

        countEnemy = 0;
        direction = 1;

        // Down-Left
        ret[5] = new Vector3Int((int)start.x, (int)start.y, countEnemy);

        while ((int)start.x - direction >= 0 && (int)start.y + direction < 8 && board[(int)start.x - direction, (int)start.y + direction] != PieceType.None)
        {
            // Count Enemy Tiles
            if (board[(int)start.x - direction, (int)start.y + direction] == chooseEnemyPiece(t))
            {
                countEnemy++;
            }
            // Found Friendly Tile
            else if (board[(int)start.x - direction, (int)start.y + direction] == chooseFriendlyPiece(t))
            {
                ret[5] = new Vector3Int((int)start.x - direction, (int)start.y + direction, countEnemy);

                break;
            }
            // Found Empty Tile
            else
            {
                ret[5] = new Vector3Int((int)start.x - direction, (int)start.y + direction, 0);

                break;
            }

            direction++;
        }

        countEnemy = 0;
        direction = 1;

        // Down
        ret[6] = new Vector3Int((int)start.x, (int)start.y, countEnemy);

        while ((int)start.y + direction < 8 && board[(int)start.x, (int)start.y + direction] != PieceType.None)
        {
            // Count Enemy Tiles
            if (board[(int)start.x, (int)start.y + direction] == chooseEnemyPiece(t))
            {
                countEnemy++;
            }
            // Found Friendly Tile
            else if (board[(int)start.x, (int)start.y + direction] == chooseFriendlyPiece(t))
            {
                ret[6] = new Vector3Int((int)start.x, (int)start.y + direction, countEnemy);

                break;
            }
            // Found Empty Tile
            else
            {
                ret[6] = new Vector3Int((int)start.x, (int)start.y + direction, 0);

                break;
            }

            direction++;
        }

        countEnemy = 0;
        direction = 1;

        // Down-Right
        ret[7] = new Vector3Int((int)start.x, (int)start.y, countEnemy);

        while ((int)start.x + direction < 8 && (int)start.y + direction < 8 && board[(int)start.x + direction, (int)start.y + direction] != PieceType.None)
        {
            // Count Enemy Tiles
            if (board[(int)start.x + direction, (int)start.y + direction] == chooseEnemyPiece(t))
            {
                countEnemy++;
            }
            // Found Friendly Tile
            else if (board[(int)start.x + direction, (int)start.y + direction] == chooseFriendlyPiece(t))
            {
                ret[7] = new Vector3Int((int)start.x + direction, (int)start.y + direction, countEnemy);

                break;
            }
            // Found Empty Tile
            else
            {
                ret[7] = new Vector3Int((int)start.x + direction, (int)start.y + direction, 0);

                break;
            }

            direction++;
        }
        
        return ret;
    }

    PieceType chooseFriendlyPiece(PlayerTurn t) { return t == PlayerTurn.Black ? PieceType.Black : PieceType.White; }
    PieceType chooseEnemyPiece(PlayerTurn t) { return t == PlayerTurn.Black ? PieceType.White : PieceType.Black; }

    private void flipPieces(Vector2[] pieces)
    {
        foreach(Vector2 p in pieces)
        {
            GameObject.Find("/Piece[" + p.x + "," + p.y + "]").GetComponent<Piece>().flipPiece();

            if(gameBoard[(int)p.x, (int)p.y] == PieceType.Black)
            {
                gameBoard[(int)p.x, (int)p.y] = PieceType.White;
            }
            else
            {
                gameBoard[(int)p.x, (int)p.y] = PieceType.Black;
            }
        }
    }
    private int flipPieces(PlayerTurn t, PieceType[,] board, Vector3Int[] until, Vector2 start, bool mainBoard)
    {
        int ret = 0;

        // Up-Left
        if (until[0].z > 0)
        {
            ret += until[0].z;

            for(int i = 1; (int)start.x - i > until[0].x && (int)start.y - i > until[0].y; i++)
            {
                if(mainBoard) GameObject.Find("/Piece[" + ((int)start.x - i) + "," + ((int)start.y - i) + "]").GetComponent<Piece>().flipPiece();

                board[(int)start.x - i, (int)start.y - i] = chooseFriendlyPiece(t);
            }

        }

        // Up
        if (until[1].z > 0)
        {
            ret += until[1].z;

            for (int i = 1; (int)start.y - i > until[1].y; i++)
            {
                if (mainBoard) GameObject.Find("/Piece[" + ((int)start.x) + "," + ((int)start.y - i) + "]").GetComponent<Piece>().flipPiece();

                board[(int)start.x, (int)start.y - i] = chooseFriendlyPiece(t);
            }
        }

        // Up-Right
        if (until[2].z > 0)
        {
            ret += until[2].z;

            for (int i = 1; (int)start.x + i < until[2].x && (int)start.y - i > until[2].y; i++)
            {
                if (mainBoard) GameObject.Find("/Piece[" + ((int)start.x + i) + "," + ((int)start.y - i) + "]").GetComponent<Piece>().flipPiece();
                board[(int)start.x + i, (int)start.y - i] = chooseFriendlyPiece(t);
            }
        }

        // Left
        if (until[3].z > 0)
        {
            ret += until[3].z;

            for (int i = 1; (int)start.x - i > until[3].x; i++)
            {
                if (mainBoard) GameObject.Find("/Piece[" + ((int)start.x - i) + "," + ((int)start.y) + "]").GetComponent<Piece>().flipPiece();

                board[(int)start.x - i, (int)start.y] = chooseFriendlyPiece(t);
            }
        }

        // Right
        if (until[4].z > 0)
        {
            ret += until[4].z;

            for (int i = 1; (int)start.x + i < until[4].x; i++)
            {
                if (mainBoard) GameObject.Find("/Piece[" + ((int)start.x + i) + "," + ((int)start.y) + "]").GetComponent<Piece>().flipPiece();

                board[(int)start.x + i, (int)start.y] = chooseFriendlyPiece(t);
            }
        }

        // Down-Left
        if (until[5].z > 0)
        {
            ret += until[5].z;

            for (int i = 1; (int)start.x - i > until[5].x && (int)start.y + i < until[5].y; i++)
            {
                if (mainBoard) GameObject.Find("/Piece[" + ((int)start.x - i) + "," + ((int)start.y + i) + "]").GetComponent<Piece>().flipPiece();

                board[(int)start.x - i, (int)start.y + i] = chooseFriendlyPiece(t);
            }
        }

        // Down
        if (until[6].z > 0)
        {
            ret += until[6].z;

            for (int i = 1; (int)start.y + i < until[6].y; i++)
            {
                if (mainBoard) GameObject.Find("/Piece[" + ((int)start.x) + "," + ((int)start.y + i) + "]").GetComponent<Piece>().flipPiece();

                board[(int)start.x, (int)start.y + i] = chooseFriendlyPiece(t);
            }
        }

        // Down-Right
        if (until[7].z > 0)
        {
            ret += until[7].z;

            for (int i = 1; (int)start.x + i < until[7].x && (int)start.y + i < until[7].y; i++)
            {
                if (mainBoard) GameObject.Find("/Piece[" + ((int)start.x + i) + "," + ((int)start.y + i) + "]").GetComponent<Piece>().flipPiece();

                board[(int)start.x + i, (int)start.y + i] = chooseFriendlyPiece(t);
            }
        }

        return ret;
    }
}

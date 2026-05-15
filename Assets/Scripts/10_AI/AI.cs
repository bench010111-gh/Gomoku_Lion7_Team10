using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

// 렌주룰 기반 오목 AI
// Minimax + Alpha-Beta Pruning + Iterative Deepening
public class AI : MonoBehaviour
{
    #region Variables
    const int BOARD_SIZE = 15;

    const int FIVE = 1000000;
    const int OPEN_FOUR = 100000;
    const int DOUBLE_FOUR = 100000;
    const int DOUBLE_OPEN_THREE = 100000;
    const int CLOSED_FOUR = 50000;
    const int OPEN_THREE = 5000;
    const int CLOSED_THREE = 500;
    const int OPEN_TWO = 100;
    const int CLOSED_TWO = 10;
    const int CENTER_BONUS = 5;

    const int FORBIDDEN_SCORE = -99999999;
    const int INF = int.MaxValue / 2;

    private readonly int[] dirX = { 1, 0, 1, 1 };
    private readonly int[] dirY = { 0, 1, -1, 1 };

    private long timeLimit;
    private bool isTimeOut;
    private Stopwatch stopwatch;
    private Vector2Int currentBestMove;

    public float defenseWeight = 1f;

    private readonly int[] LineBuffer = new int[BOARD_SIZE];
    private int[][] lineScoreMy = new int[4][];
    private int[][] lineScoreOp = new int[4][];
    private int cachedMyPosScore;
    private int cachedOpPosScore;
    private int cachedMyScore;
    private int cachedOpScore;
    private int aiPlayer;

    private int nodeCount;
    #endregion

    public Vector2Int GetBestMove(int[,] board, int player, long timeLimitMs = 3000, int maxDepth = 20)
    {
        timeLimit = timeLimitMs;
        stopwatch = Stopwatch.StartNew();
        isTimeOut = false;

        int opponent = Opponent(player);

        if (IsBoardEmpty(board))
            return new Vector2Int(BOARD_SIZE / 2, BOARD_SIZE / 2);

        var candidates = GetCandidates(board, 2);

        foreach (var c in candidates)
        {
            board[c.x, c.y] = player;
            bool win = IsFive(c.x, c.y, player, board);
            board[c.x, c.y] = 0;
            if (win) return c;
        }
        foreach (var c in candidates)
        {
            board[c.x, c.y] = opponent;
            bool win = IsFive(c.x, c.y, opponent, board);
            board[c.x, c.y] = 0;
            if (win) return c;
        }
        foreach (var c in candidates)
        {
            board[c.x, c.y] = player;
            bool winning = HasWinningPattern(c.x, c.y, player, board);
            board[c.x, c.y] = 0;
            if (winning) return c;
        }
        foreach (var c in candidates)
        {
            board[c.x, c.y] = opponent;
            bool winning = HasWinningPattern(c.x, c.y, opponent, board);
            board[c.x, c.y] = 0;
            if (winning) return c;
        }

        InitEvaluationCache(board, player);
        currentBestMove = GetFirstCandidate(candidates);

        for (int depth = 1; depth <= maxDepth && !isTimeOut; depth++)
        {
            if (isTimeOut) break;

            Vector2Int move = Vector2Int.zero;

            try
            {
                move = SearchRoot(board, player, depth, candidates);
                currentBestMove = move;
            }
            catch (TimeoutException)
            {
                isTimeOut = true;
            }

            if (!isTimeOut)
                Debug.Log($"Depth {depth} 완료 | BestMove = {move} | Nodes = {nodeCount} | Time = {stopwatch.ElapsedMilliseconds}ms");

            if (IsTimeOut())
            {
                Debug.Log($"Timeout at depth {depth} | BestMove = {move} | Nodes = {nodeCount} | Time = {stopwatch.ElapsedMilliseconds}ms");
                break;
            }
        }

        return currentBestMove;
    }

    private Vector2Int SearchRoot(int[,] board, int player, int depth, HashSet<Vector2Int> candidates)
    {
        nodeCount = 0;

        int opponent = Opponent(player);
        int alpha = -INF;
        int beta = INF;
        int bestScore = -INF;
        Vector2Int bestMove = currentBestMove;

        var sorted = SortCandidates(candidates, player, opponent, board, 30, true);

        int idx = sorted.FindIndex(e => e.pos == currentBestMove);
        if (idx > 0)
        {
            var prev = sorted[idx];
            sorted.RemoveAt(idx);
            sorted.Insert(0, prev);
        }

        foreach (var (pos, _) in sorted)
        {
            if (IsTimeOut()) throw new TimeoutException();  

            board[pos.x, pos.y] = player;
            UpdateScoreCache(pos.x, pos.y, board, player, true);
            candidates.Remove(pos);
            var added = AddNearby(pos, board, candidates);

            try
            {
                int score = -AlphaBeta(board, depth - 1, -beta, -alpha, opponent, candidates);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = pos;
                }

                alpha = Mathf.Max(alpha, score);

                if (alpha >= beta)
                    break;
            }

            finally
            {
                board[pos.x, pos.y] = 0;
                UpdateScoreCache(pos.x, pos.y, board, player, false);
                candidates.Add(pos);
                RemoveAdded(added, candidates);
            }
        }

        return bestMove;
    }

    #region Algorithm
    private int AlphaBeta(int[,] board, int depth, int alpha, int beta, int current, HashSet<Vector2Int> candidates)
    {
        nodeCount++;

        if (IsTimeOut()) throw new TimeoutException();

        if (depth == 0)
            return GetEvaluation(current);

        int opponent = Opponent(current);
        int topN = depth >= 5 ? 8 : (depth >= 3 ? 12 : 15);

        var sorted = SortCandidates(candidates, current, opponent, board, topN, true);
        if (sorted.Count == 0) return GetEvaluation(current);

        int bestScore = -INF;

        foreach (var (pos, _) in sorted)
        {
            if (IsTimeOut())
                throw new TimeoutException();

            board[pos.x, pos.y] = current;
            UpdateScoreCache(pos.x, pos.y, board, current, true);
            candidates.Remove(pos);
            var added = AddNearby(pos, board, candidates);

            try
            {
                int score;

                if (IsFive(pos.x, pos.y, current, board))
                    score = FIVE + depth;
                else if(current == 1 && IsForbidden(pos.x, pos.y, current, board))
                    score = FORBIDDEN_SCORE; 
                else
                    score = -AlphaBeta(board, depth - 1, -beta, -alpha, opponent, candidates);

                if (score > bestScore)
                    bestScore = score;

                alpha = Mathf.Max(alpha, score);

                if (alpha >= beta)
                    break;
            }
            catch (TimeoutException)
            {
                throw;
            }
            finally
            {
                board[pos.x, pos.y] = 0;
                UpdateScoreCache(pos.x, pos.y, board, current, false);
                candidates.Add(pos);
                RemoveAdded(added, candidates);
            }
        }

        return bestScore;
    }
    #endregion

    #region Evaluation
    private int ScanLine(int[] line, int len, int player)
    {
        int score = 0;

        for (int i = 0; i <= len - 5; i++)
        {
            int myCount = 0;
            bool blocked = false;

            for (int j = i; j < i + 5; j++)
            {
                if (line[j] == player) myCount++;
                else if (line[j] != 0) { blocked = true; break; }
            }

            if (blocked) continue;

            bool leftOpen = (i > 0) && (line[i - 1] == 0);
            bool rightOpen = (i + 5 < len) && (line[i + 5] == 0);
            bool isOverline = ((i > 0) && line[i - 1] == player)
                           || ((i + 5 < len) && line[i + 5] == player);

            score += myCount switch
            {
                5 => (player == 1 && isOverline) ? 0 : FIVE,
                4 => (leftOpen && rightOpen) ? OPEN_FOUR : CLOSED_FOUR,
                3 => (leftOpen && rightOpen) ? OPEN_THREE : CLOSED_THREE,
                2 => (leftOpen && rightOpen) ? OPEN_TWO : CLOSED_TWO,
                _ => 0
            };
        }

        return score;
    }

    private int QuickScore(int x, int y, int player, int[,] board)
    {
        int prev = board[x, y];
        board[x, y] = player;
        int score = 0;

        for (int d = 0; d < 4; d++)
        {
            int dx = dirX[d], dy = dirY[d];

            Span<int> buf = stackalloc int[9];
            for (int i = -4; i <= 4; i++)
            {
                int nx = x + dx * i, ny = y + dy * i;
                buf[i + 4] = IsValidPos(nx, ny) ? board[nx, ny] : -1;
            }

            for (int i = 0; i <= 4; i++)
            {
                int cnt = 0;
                bool blocked = false;
                for (int j = i; j < i + 5; j++)
                {
                    if (buf[j] == player) cnt++;
                    else if (buf[j] != 0) { blocked = true; break; }
                }
                if (blocked) continue;

                bool leftOpen = i > 0 && buf[i - 1] == 0;
                bool rightOpen = i + 5 < 9 && buf[i + 5] == 0;

                score += cnt switch
                {
                    5 => FIVE,
                    4 => (leftOpen && rightOpen) ? OPEN_FOUR : CLOSED_FOUR,
                    3 => (leftOpen && rightOpen) ? OPEN_THREE : CLOSED_THREE,
                    2 => (leftOpen && rightOpen) ? OPEN_TWO : CLOSED_TWO,
                    1 => CENTER_BONUS,
                    _ => 0
                };
            }
        }

        board[x, y] = prev;
        return score;
    }

    public int Evaluate(int x, int y, int player, int[,] board)
    {
        int prev = board[x, y];
        board[x, y] = player;

        int score;

        if (IsFive(x, y, player, board))
        {
            board[x, y] = prev;
            return FIVE;
        }

        if (player == 1)
        {
            if(IsForbidden(x, y, player, board))
            {
                board[x, y] = prev;
                return FORBIDDEN_SCORE;
            }
        }

        bool hasOpenFour = false;
        for (int i = 0; i < 4; i++)
            if (IsOpenFour(x, y, dirX[i], dirY[i], player, board))
            { hasOpenFour = true; break; }

        int fourCount = CountFour(x, y, player, board);
        int threeCount = CountOpenThree(x, y, player, board);

        if (hasOpenFour) score = OPEN_FOUR;
        else if (fourCount >= 2) score = DOUBLE_FOUR;
        else if (threeCount >= 2) score = DOUBLE_OPEN_THREE;
        else if (fourCount == 1) score = CLOSED_FOUR;
        else if (threeCount == 1) score = OPEN_THREE;
        else score = 0;

        int cx = Mathf.Abs(x - 7), cy = Mathf.Abs(y - 7);
        score += Mathf.Max(0, CENTER_BONUS - cx - cy);

        board[x, y] = prev;
        return score;
    }

    private int GetEvaluation(int current)
    {
        int posScore = cachedMyPosScore - cachedOpPosScore;
        if (current == aiPlayer)
            return cachedMyScore - (int)(cachedOpScore * defenseWeight) + posScore;
        else
            return cachedOpScore - (int)(cachedMyScore * defenseWeight) - posScore;
    }
    #endregion

    #region Candidates
    public HashSet<Vector2Int> GetCandidates(int[,] board, int range = 1)
    {
        var candidates = new HashSet<Vector2Int>();

        for (int y = 0; y < BOARD_SIZE; y++)
            for (int x = 0; x < BOARD_SIZE; x++)
            {
                if (board[x, y] == 0) continue;

                for (int dy = -range; dy <= range; dy++)
                    for (int dx = -range; dx <= range; dx++)
                    {
                        int nx = x + dx, ny = y + dy;
                        if (IsValidPos(nx, ny) && board[nx, ny] == 0)
                            candidates.Add(new Vector2Int(nx, ny));
                    }
            }

        if (candidates.Count == 0)
            candidates.Add(new Vector2Int(7, 7));

        return candidates;
    }

    private Vector2Int GetFirstCandidate(HashSet<Vector2Int> set)
    {
        foreach (var v in set) return v;
        return new Vector2Int(7, 7);
    }

    private List<Vector2Int> AddNearby(Vector2Int pos, int[,] board, HashSet<Vector2Int> candidates)
    {
        var added = new List<Vector2Int>();

        for (int dy = -2; dy <= 2; dy++)
            for (int dx = -2; dx <= 2; dx++)
            {
                var next = new Vector2Int(pos.x + dx, pos.y + dy);
                if (!IsValidPos(next.x, next.y)) continue;
                if (board[next.x, next.y] != 0) continue;
                if (candidates.Add(next))
                    added.Add(next);
            }

        return added;
    }

    private void RemoveAdded(List<Vector2Int> added, HashSet<Vector2Int> candidates)
    {
        foreach (var v in added) candidates.Remove(v);
    }

    private List<(Vector2Int pos, int score)> SortCandidates(
        HashSet<Vector2Int> candidates, int current, int opponent,
        int[,] board, int topN = 15, bool useEvaluate = false)
    {
        var list = new List<(Vector2Int, int score)>();

        foreach (var c in candidates)
        {
            if (current == 1)
            {
                board[c.x, c.y] = 1;
                bool forbidden = IsForbidden(c.x, c.y, 1, board); 
                board[c.x, c.y] = 0;
                if (forbidden) continue;
            }

            int myScore, opScore;
            if (useEvaluate)
            {
                myScore = Evaluate(c.x, c.y, current, board);
                opScore = Evaluate(c.x, c.y, opponent, board);
            }
            else
            {
                myScore = QuickScore(c.x, c.y, current, board);
                opScore = QuickScore(c.x, c.y, opponent, board);
            }

            list.Add((c, myScore + opScore));
        }

        list.Sort((a, b) => b.score.CompareTo(a.score));
        if (list.Count > topN) list = list.GetRange(0, topN);
        return list;
    }
    #endregion

    #region Counting
    bool IsFive(int x, int y, int player, int[,] board)
    {
        for (int i = 0; i < 4; i++)
        {
            int count = 1
                + CountConnected(x, y, dirX[i], dirY[i], player, board)
                + CountConnected(x, y, -dirX[i], -dirY[i], player, board);

            if (player == 1 && count == 5) return true;
            if (player == 2 && count >= 5) return true;
        }
        return false;
    }

    bool IsConnect6(int x, int y, int player, int[,] board)
    {
        for (int i = 0; i < 4; i++)
        {
            int count = 1
                + CountConnected(x, y, dirX[i], dirY[i], player, board)
                + CountConnected(x, y, -dirX[i], -dirY[i], player, board);
            if (count >= 6) return true;
        }
        return false;
    }

    bool IsOpenFour(int x, int y, int dx, int dy, int player, int[,] board)
    {
        int[] buf = new int[11];

        for (int i = 0; i < 11; i++)
        {
            int offset = i - 5;
            int nx = x + dx * offset;
            int ny = y + dy * offset;
            buf[i] = !IsValidPos(nx, ny) ? -1
                   : (offset == 0) ? player
                   : board[nx, ny];
        }

        for (int i = 0; i + 5 <= 10; i++)
        {
            if (buf[i] != 0 || buf[i + 5] != 0) continue;

            bool isFour = true;
            for (int j = i + 1; j <= i + 4; j++)
                if (buf[j] != player) { isFour = false; break; }

            if (!isFour) continue;

            if (player == 1)
            {
                bool leftValid = (i == 0) || buf[i - 1] != player;
                bool rightValid = (i + 6 > 10) || buf[i + 6] != player;
                if (leftValid && rightValid) return true;
            }
            else return true;
        }
        return false;
    }

    int CountFour(int x, int y, int player, int[,] board)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
            count += CountFourInDirection(x, y, dirX[i], dirY[i], player, board);
        return count;
    }

    int CountFourInDirection(int x, int y, int dx, int dy, int player, int[,] board)
    {
        int count = 0;
        Span<int> seenPatterns = stackalloc int[6]; 
        int seenCount = 0;

        for (int i = -4; i <= 0; i++)
        {
            int stoneMask = 0;
            int emptyCount = 0;
            bool possible = true;

            for (int j = 0; j < 5; j++)
            {
                int offset = i + j;
                int nx = x + dx * offset;
                int ny = y + dy * offset;

                if (!IsValidPos(nx, ny)) { possible = false; break; }

                int p = (offset == 0) ? player : board[nx, ny];
                if (p == player) stoneMask |= (1 << j);
                else if (p == 0) emptyCount++;
                else { possible = false; break; }
            }

            if (!possible || emptyCount != 1) continue;

            int stoneCount = 0;
            for (int b = 0; b < 5; b++) if ((stoneMask & (1 << b)) != 0) stoneCount++;
            if (stoneCount != 4) continue;

            if (player == 1)
            {
                int lx = x + dx * (i - 1), ly = y + dy * (i - 1);
                int rx = x + dx * (i + 5), ry = y + dy * (i + 5);
                bool leftExt = IsValidPos(lx, ly) && board[lx, ly] == player;
                bool rightExt = IsValidPos(rx, ry) && board[rx, ry] == player;
                if (leftExt || rightExt) continue;
            }

            int absoluteMask = 0;
            for (int j = 0; j < 5; j++)
                if ((stoneMask & (1 << j)) != 0) absoluteMask |= (1 << (i + j + 4));

            bool alreadySeen = false;
            for (int s = 0; s < seenCount; s++)
                if (seenPatterns[s] == absoluteMask) { alreadySeen = true; break; }

            if (alreadySeen) continue;

            if (seenCount < 6)
                seenPatterns[seenCount++] = absoluteMask;

            count++;
        }
        return count;
    }

    bool IsDoubleFour(int x, int y, int player, int[,] board) => CountFour(x, y, player, board) >= 2;

    bool IsOpenThree(int x, int y, int dx, int dy, int player, int[,] board)
    {
        var visited = new HashSet<Vector2Int> { new Vector2Int(x, y) };
        return IsOpenThreeCore(x, y, dx, dy, player, board, visited);
    }

    bool IsOpenThreeCore(int x, int y, int dx, int dy, int player, int[,] board, HashSet<Vector2Int> visited)
    {
        int[] buf = new int[11];
        for (int i = -5; i <= 5; i++)
        {
            int nx = x + dx * i;
            int ny = y + dy * i;
            buf[i + 5] = !IsValidPos(nx, ny) ? -1 : (i == 0) ? player : board[nx, ny];
        }

        for (int i = 0; i <= 5; i++)
        {
            if (buf[i] != 0 || buf[i + 5] != 0) continue;
            if (i > 0 && buf[i - 1] == player) continue;
            if (i + 6 < 11 && buf[i + 6] == player) continue;

            int count = 0, emptyIdx = -1, emptyCount = 0;
            for (int j = i + 1; j <= i + 4; j++)
            {
                if (buf[j] == player) count++;
                else if (buf[j] == 0) { emptyIdx = j; emptyCount++; }
            }

            if (count != 3 || emptyCount != 1) continue;

            int tx = x + dx * (emptyIdx - 5);
            int ty = y + dy * (emptyIdx - 5);
            var target = new Vector2Int(tx, ty);

            if (player == 1)
            {
                if (IsFakeOpenThree(tx, ty, player, board)) continue;

                if (!visited.Contains(target))
                {
                    visited.Add(target);
                    bool isDoubleThree = IsDoubleOpenThreeCore(tx, ty, player, board, visited);
                    visited.Remove(target);
                    if (isDoubleThree) continue;
                }
            }

            return true;
        }
        return false;
    }

    bool IsDoubleOpenThreeCore(int x, int y, int player, int[,] board, HashSet<Vector2Int> visited)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
            if (IsOpenThreeCore(x, y, dirX[i], dirY[i], player, board, visited))
                count++;
        return count >= 2;
    }

    bool IsFakeOpenThree(int x, int y, int player, int[,] board)
    {
        int prev = board[x, y];
        board[x, y] = player;
        bool fake = IsConnect6(x, y, player, board) || IsDoubleFour(x, y, player, board);
        board[x, y] = prev;
        return fake;
    }

    int CountOpenThree(int x, int y, int player, int[,] board)
    {
        int count = 0;
        for (int i = 0; i < 4; i++)
            if (IsOpenThree(x, y, dirX[i], dirY[i], player, board))
                count++;
        return count;
    }

    bool IsDoubleOpenThree(int x, int y, int player, int[,] board) => CountOpenThree(x, y, player, board) >= 2;

    int CountConnected(int x, int y, int dx, int dy, int player, int[,] board)
    {
        int count = 0;
        int nx = x + dx;
        int ny = y + dy;

        while (IsValidPos(nx, ny) && board[nx, ny] == player)
        {
            count++;
            nx += dx;
            ny += dy;
        }

        return count;
    }
    #endregion

    #region Helper
    bool IsForbidden(int x, int y, int player, int[,] board)
    {
        if (IsConnect6(x, y, player, board)) return true; 
        if(IsDoubleFour(x, y, player, board)) return true;
        if(IsDoubleOpenThree(x, y, player, board)) return true;

        return false; 
    }

    bool IsValidPos(int x, int y) => x >= 0 && y >= 0 && x < BOARD_SIZE && y < BOARD_SIZE;

    bool IsBoardEmpty(int[,] board)
    {
        for (int y = 0; y < BOARD_SIZE; y++)
            for (int x = 0; x < BOARD_SIZE; x++)
                if (board[x, y] != 0) return false;
        return true;
    }

    bool IsTimeOut()
    {
        if (isTimeOut) return true;
        if (stopwatch.ElapsedMilliseconds >= timeLimit)
        {
            isTimeOut = true;
            return true;
        }
        return false;
    }

    int Opponent(int player) => player == 1 ? 2 : 1;

    bool HasWinningPattern(int x, int y, int player, int[,] board)
    {
        for (int i = 0; i < 4; i++)
            if (IsOpenFour(x, y, dirX[i], dirY[i], player, board))
                return true;

        int fourCount = CountFour(x, y, player, board);
        int threeCount = CountOpenThree(x, y, player, board);

        if (player == 1)
            return fourCount == 1 && threeCount == 1;
        else
            return (fourCount >= 1 && threeCount >= 1)
                || (fourCount >= 2)
                || (threeCount >= 2);
    }
    #endregion

    #region Caching
    private void InitEvaluationCache(int[,] board, int player)
    {
        aiPlayer = player;
        cachedMyScore = 0;
        cachedOpScore = 0;
        cachedMyPosScore = 0;
        cachedOpPosScore = 0;

        int[] lineCounts = { 15, 15, 29, 29 };

        for (int dir = 0; dir < 4; dir++)
        {
            lineScoreMy[dir] = new int[lineCounts[dir]];
            lineScoreOp[dir] = new int[lineCounts[dir]];

            for (int lineId = 0; lineId < lineCounts[dir]; lineId++)
            {
                int len = FillLineBuffer(dir, lineId, board);
                if (len < 5) continue;

                lineScoreMy[dir][lineId] = ScanLine(LineBuffer, len, player);
                lineScoreOp[dir][lineId] = ScanLine(LineBuffer, len, Opponent(player));

                cachedMyScore += lineScoreMy[dir][lineId];
                cachedOpScore += lineScoreOp[dir][lineId];
            }
        }

        for (int x = 0; x < BOARD_SIZE; x++)
            for (int y = 0; y < BOARD_SIZE; y++)
            {
                if (board[x, y] == 0) continue;
                int dist = Mathf.Max(Mathf.Abs(x - 7), Mathf.Abs(y - 7));
                int posBonus = CENTER_BONUS * (7 - dist);
                if (board[x, y] == player) cachedMyPosScore += posBonus;
                else cachedOpPosScore += posBonus;
            }
    }

    private int FillLineBuffer(int dir, int lineId, int[,] board)
    {
        int len = 0;
        for (int i = 0; i < BOARD_SIZE; i++)
        {
            int x, y;
            switch (dir)
            {
                case 0: x = i; y = lineId; break;
                case 1: x = lineId; y = i; break;
                case 2: x = i; y = lineId - i; break;
                case 3: x = i; y = i - (lineId - 14); break;
                default: continue;
            }

            if (IsValidPos(x, y))
                LineBuffer[len++] = board[x, y];
        }
        return len;
    }

    private void UpdateScoreCache(int cx, int cy, int[,] board, int stonePlayer, bool placing)
    {
        for (int dir = 0; dir < 4; dir++)
        {
            int lineId = GetLineId(cx, cy, dir);
            int len = FillLineBuffer(dir, lineId, board);
            if (len < 5) continue;

            cachedMyScore -= lineScoreMy[dir][lineId];
            cachedOpScore -= lineScoreOp[dir][lineId];

            lineScoreMy[dir][lineId] = ScanLine(LineBuffer, len, aiPlayer);
            lineScoreOp[dir][lineId] = ScanLine(LineBuffer, len, Opponent(aiPlayer));

            cachedMyScore += lineScoreMy[dir][lineId];
            cachedOpScore += lineScoreOp[dir][lineId];
        }

        int dist = Mathf.Max(Mathf.Abs(cx - 7), Mathf.Abs(cy - 7));
        int posBonus = CENTER_BONUS * (7 - dist);
        int sign = placing ? 1 : -1;

        if (stonePlayer == aiPlayer) cachedMyPosScore += sign * posBonus;
        else cachedOpPosScore += sign * posBonus;
    }

    private int GetLineId(int x, int y, int dir) => dir switch
    {
        0 => y,
        1 => x,
        2 => x + y,
        3 => x - y + 14,
        _ => -1
    };
    #endregion
}

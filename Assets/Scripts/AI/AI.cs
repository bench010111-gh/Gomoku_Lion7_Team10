using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

// 렌주룰 기반 오목 AI
// Minimax + Alpha-Beta Pruning + Iterative Deepening
public class AI : MonoBehaviour
{
    const int BOARD_SIZE = 15;

    const int FIVE = 1000000;
    const int OPEN_FOUR = 100000;
    const int DOUBLE_FOUR = 100000;
    const int DOUBLE_OPEN_THREE = 100000;  
    const int CLOSED_FOUR = 50000; 
    const int OPEN_THREE = 5000;
    const int CLOSED_THREE = 500;
    const int OPEN_TWO = 50;
    const int CLOSED_TWO = 10;
    const int CENTER_BONUS = 5;

    const int FORBIDDEN_SCORE = -999999;
    const int INF = int.MaxValue / 2;

    // 4방향 벡터
    private readonly int[] dirX = { 1, 0, 1, 1 };
    private readonly int[] dirY = { 0, 1, -1, 1 };

    //  Iterative Deepening 상태
    private long timeLimit = 3000;
    private bool isTimeOut;
    private Stopwatch stopwatch; 
    private Vector2Int currentBestMove;

    // 성향 가중치 
    public float defenseWeight = 1.2f; 

    // 시간 제한(ms) 내에서 Iterative Deepening으로 최선의 수를 반환합니다.
    public Vector2Int GetBestMove(int[,] board, int player, long timeLimitMs = 3000, int maxDepth = 14)
    {
        timeLimit = timeLimitMs;
        stopwatch = Stopwatch.StartNew();
        isTimeOut = false;

        int opponent = Opponent(player);

        // 첫 수인 경우, 중앙 
        if (IsBoardEmpty(board))
            return new Vector2Int(BOARD_SIZE/2, BOARD_SIZE/2);

        var candidates = GetCandidates(board);

        // 승리가 가능한 경우, 즉 오목이 가능한 경우 바로 선택
        foreach (var c in candidates)
        {
            board[c.x, c.y] = player;
            bool win = IsFive(c.x, c.y, player, board);
            board[c.x, c.y] = 0;
            if (win) return c;
        }

        // 상대방이 오목
        foreach (var c in candidates)
        {
            board[c.x, c.y] = opponent;
            bool win = IsFive(c.x, c.y, opponent, board);
            board[c.x, c.y] = 0;
            if (win) return c;
        }

        // Iterative Deepening
        currentBestMove = FirstCandidate(candidates);

        for (int depth = 1; depth <= maxDepth && !isTimeOut; depth++)
        {
            Vector2Int move = SearchRoot(board, player, depth, candidates);
            if (!isTimeOut)
                currentBestMove = move;
        }

        return currentBestMove;
    }

    private Vector2Int SearchRoot(int[,] board, int player, int depth, HashSet<Vector2Int> candidates)
    {
        int opponent = Opponent(player);
        int bestScore = -INF;
        Vector2Int bestMove = currentBestMove;

        // 내 점수 + 상대 점수 합산. Maximizing이기 때문에 내림차 순 
        var sorted = SortedCandidates(candidates, player, opponent, board, maximizing:true);

        foreach (var (pos, _) in sorted)
        {
            if (IsTimeOut()) break;

            board[pos.x, pos.y] = player;
            candidates.Remove(pos);
            var added = AddNearby(pos, board, candidates);

            int score = AlphaBeta(board, depth - 1, -INF, INF,
                                  opponent, player, candidates);

            board[pos.x, pos.y] = 0;
            candidates.Add(pos);
            RemoveAdded(added, candidates);

            if (!isTimeOut && score > bestScore)
            {
                bestScore = score;
                bestMove = pos;
            }
        }

        return bestMove;
    }

    //  Alpha-Beta Minimax
    private int AlphaBeta(int[,] board, int depth, int alpha, int beta, int current, int aiPlayer, HashSet<Vector2Int> candidates)
    {
        if (IsTimeOut()) return 0;

        // 리프 노드: 전체 보드 평가
        if (depth == 0)
            return EvaluateBoard(board, aiPlayer);

        int opponent = Opponent(current);
        bool isMaximizing = (current == aiPlayer);

        // Maximizer: 내림차순, Minimizer: 오름차순 
        var sorted = SortedCandidates(candidates, current, opponent, board, isMaximizing);

        int bestScore = isMaximizing ? -INF : INF;

        foreach (var (pos, _) in sorted)
        {
            if (IsTimeOut()) break;

            board[pos.x, pos.y] = current;
            candidates.Remove(pos);
            var added = AddNearby(pos, board, candidates);

            int score = AlphaBeta(board, depth - 1, alpha, beta, opponent, aiPlayer, candidates);

            board[pos.x, pos.y] = 0;
            candidates.Add(pos);
            RemoveAdded(added, candidates);

            if (isMaximizing)
            {
                if (score > bestScore) bestScore = score;
                if (score > alpha) alpha = score;
            }
            else
            {
                if (score < bestScore) bestScore = score;
                if (score < beta) beta = score;
            }

            // Alpha-Beta 가지치기
            if (alpha >= beta) break;
        }

        return bestScore;
    }

    //  리프노드에 도착했을 때, 현재 보드판 평가 
    public int EvaluateBoard(int[,] board, int player)
    {
        int opponent = Opponent(player);
        int myScore = ScanAllLines(board, player);
        int opScore = ScanAllLines(board, opponent);

        return myScore - (int)(opScore * 1.2f);
    }

    // 전체 가로, 세로, 대각선 2방향 계산  
    private int ScanAllLines(int[,] board, int player)
    {
        int score = 0;

        // 가로
        for (int y = 0; y < BOARD_SIZE; y++)
        {
            int[] line = new int[BOARD_SIZE];
            for (int x = 0; x < BOARD_SIZE; x++) line[x] = board[x, y];
            score += ScanLine(line, player);
        }

        // 세로
        for (int x = 0; x < BOARD_SIZE; x++)
        {
            int[] line = new int[BOARD_SIZE];
            for (int y = 0; y < BOARD_SIZE; y++) line[y] = board[x, y];
            score += ScanLine(line, player);
        }

        // 대각선 (\)
        for (int d = -(BOARD_SIZE - 1); d < BOARD_SIZE; d++)
        {
            var line = new List<int>();
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                int x = i, y = i - d;
                if (IsValidPos(x, y)) line.Add(board[x, y]);
            }
            if (line.Count >= 5) 
                score += ScanLine(line.ToArray(), player);
        }

        // 대각선 (/)
        for (int s = 0; s <= 2 * (BOARD_SIZE - 1); s++)
        {
            var line = new List<int>();
            for (int i = 0; i < BOARD_SIZE; i++)
            {
                int x = i, y = s - i;
                if (IsValidPos(x, y)) line.Add(board[x, y]);
            }
            if (line.Count >= 5) 
                score += ScanLine(line.ToArray(), player);
        }

        return score;
    }

    // 5칸 슬라이딩 윈도우 스캔
    private int ScanLine(int[] line, int player)
    {
        int score = 0;
        int opponent = Opponent(player);
        int len = line.Length;

        for (int i = 0; i <= len - 5; i++)
        {
            int myCount = 0;
            int emptyCount = 0;
            bool blocked = false;

            for (int j = i; j < i + 5; j++)
            {
                if (line[j] == player) myCount++;
                else if (line[j] == 0) emptyCount++;
                else { blocked = true; break; }
            }

            if (blocked) continue;

            bool leftOpen = (i > 0) && (line[i - 1] == 0);
            bool rightOpen = (i + 5 < len) && (line[i + 5] == 0);

            score += myCount switch
            {
                5 => FIVE,
                4 => (leftOpen && rightOpen) ? OPEN_FOUR : CLOSED_FOUR,
                3 => (leftOpen && rightOpen) ? OPEN_THREE : CLOSED_THREE,
                2 => (leftOpen && rightOpen) ? OPEN_TWO : CLOSED_TWO,
                _ => 0
            };
        }

        return score;
    }

    //  단일 수 평가 
    public int Evaluate(int x, int y, int player, int[,] board)
    {
        int prev = board[x, y];
        board[x, y] = player;

        int score;

        // 5목
        if (IsFive(x, y, player, board))
        {
            board[x, y] = prev;
            return FIVE;
        }

        // 흑 금수 판별
        if (player == 1)
        {
            if (IsConnect6(x, y, player, board) ||
                IsDoubleFour(x, y, player, board) ||
                IsDoubleOpenThree(x, y, player, board))
            {
                board[x, y] = prev;
                return FORBIDDEN_SCORE;
            }
        }

        // 패턴별 점수
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

        // 중앙 가산
        int cx = Mathf.Abs(x - 7), cy = Mathf.Abs(y - 7);
        score += Mathf.Max(0, CENTER_BONUS - cx - cy);

        board[x, y] = prev;
        return score;
    }

    // 후보 생성 
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

    // 주변의 빈 후보 추가 
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

    // 후보를 (내 점수 + 상대 점수)로 정렬
    // maximizing=true → 내림차순, false → 오름차순
    private List<(Vector2Int pos, int score)> SortedCandidates(HashSet<Vector2Int> candidates, int current, int opponent,int[,] board, bool maximizing)
    {
        var list = new List<(Vector2Int, int score)>();

        foreach (var c in candidates)
        {
            int myScore = Evaluate(c.x, c.y, current, board);
            int opScore = Evaluate(c.x, c.y, opponent, board);
            list.Add((c, myScore + opScore));
        }

        if (maximizing)
            list.Sort((a, b) => b.score.CompareTo(a.score));  
        else
            list.Sort((a, b) => a.score.CompareTo(b.score));  

        return list;
    }

    bool IsFive(int x, int y, int player, int[,] board)
    {
        for (int i = 0; i < 4; i++)
        {
            int count = 1
                + CountConnected(x, y, dirX[i], dirY[i], player, board)
                + CountConnected(x, y, -dirX[i], -dirY[i], player, board);

            if (player == 1 && count == 5) return true;  // 흑: 정확히 5
            if (player == 2 && count >= 5) return true;  // 백: 5 이상 허용
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
    bool IsFour(int x, int y, int dx, int dy, int player, int[,] board)
    {
        int[] buf = new int[9];

        for (int i = -4; i <= 4; i++)
        {
            int nx = x + dx * i, ny = y + dy * i;
            buf[i + 4] = !IsValidPos(nx, ny) ? -1
                       : (i == 0) ? player
                       : board[nx, ny];
        }

        for (int i = 0; i <= 4; i++)
        {
            int cnt = 0, empty = 0, blocked = 0;
            for (int j = i; j < i + 5; j++)
            {
                if (buf[j] == player) cnt++;
                else if (buf[j] == 0) empty++;
                else blocked++;
            }

            if (blocked > 0) continue;
            if (cnt == 4 && empty == 1) return true;
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

            // 흑: 장목 방지 — 열린 사 양쪽에 추가 돌 없어야 함
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
            if (IsFour(x, y, dirX[i], dirY[i], player, board)) count++;
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

            buf[i + 5] = !IsValidPos(nx, ny) ? -1 : (i == 0) ? player: board[nx, ny];
        }

        for (int i = 0; i <= 5; i++)
        {
            // 양쪽이 열려 있어야 함
            if (buf[i] != 0 || buf[i + 5] != 0) continue;

            // 바깥쪽에 내 돌이 연장되지 않아야 함
            if (i > 0 && buf[i - 1] == player) continue;
            if (i + 6 < 11 && buf[i + 6] == player) continue;

            int count = 0, emptyIdx = -1, emptyCount = 0;
            for (int j = i + 1; j <= i + 4; j++)
            {
                if (buf[j] == player) count++;
                else if (buf[j] == 0) { emptyIdx = j; emptyCount++; }
            }

            if (count != 3 || emptyCount != 1) continue;

            // 빈 칸에 두었을 때 유효한 삼인지 확인
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
        // 장목 또는 쌍사가 되면 가짜 삼
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
        {
            if (IsOpenThree(x, y, dirX[i], dirY[i], player, board)) 
                count++;
        }
        return count;
    }
    bool IsDoubleOpenThree(int x, int y, int player, int[,] board) => CountOpenThree(x, y, player, board) >= 2;
    int CountConnected(int x, int y, int dx, int dy, int player, int[,] board)
    {
        int nx = x + dx;
        int ny = y + dy;
        if (!IsValidPos(nx, ny) || board[nx, ny] != player) return 0;
        return 1 + CountConnected(nx, ny, dx, dy, player, board);
    }

    bool IsValidPos(int x, int y) => x >= 0 && y >= 0 && x < BOARD_SIZE && y < BOARD_SIZE;
    int Opponent(int player) => player == 1 ? 2 : 1;
    bool IsBoardEmpty(int[,] board)
    {
        for (int y = 0; y < BOARD_SIZE; y++)
            for (int x = 0; x < BOARD_SIZE; x++)
                if (board[x, y] != 0) return false;
        return true;
    }
    Vector2Int FirstCandidate(HashSet<Vector2Int> set)
    {
        foreach (var v in set) return v;
        return new Vector2Int(7, 7);
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
}
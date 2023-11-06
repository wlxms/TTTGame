using System;
using System.Collections;
using System.Collections.Generic;
using Interfaces;
using UnityEngine;
using UnityEngine.UI;

public enum NodeState
{
    Empty,
    X,
    O
}

public enum UserType
{
    Computer = -1,
    Player = 1,
}

public enum WinnerType
{
    Draw,
    PlayerWin,
    ComputerWin
}


[ExecuteInEditMode]
public class TTTManager : MonoBehaviour
{
    private static TTTManager instance;

    public static TTTManager Instance => instance;

    private List<NodeState> nodeStates;
    private List<NodeRenderer> nodeRenderers;

    public Transform nodeContent;

    private int round = 0;
    private UserType currentUser;

    public UserType startState = UserType.Player;

    public Text logText;

    public Button btnRestart;

    private bool isGameEnd;


    private void Awake()
    {
        if (instance != null)
            DestroyImmediate(instance.gameObject);

        instance = this;

        if (nodeContent == null) throw new Exception("NodeContent is null");

        nodeStates = new List<NodeState>();
        nodeRenderers = new List<NodeRenderer>();

        for (int i = 0; i < 9; i++)
        {
            nodeStates.Add(NodeState.Empty);
        }

        var children = nodeContent.GetComponentsInChildren<NodeRenderer>();
        for (var index = 0; index < children.Length; index++)
        {
            var renderer = children[index];
            nodeRenderers.Add(renderer);
            renderer.SetRenderState(nodeStates[index]);
            var idx = index;

            var listenable = renderer as IListenable;

            listenable.AddListener((() => { OnNodeClick(idx); }));
        }

        btnRestart?.onClick.AddListener(RestartGame);

        RestartGame();
        
    }

    public void RestartGame()
    {
        isGameEnd = false;
        for (var index = 0; index < nodeStates.Count; index++)
        {
            SetState(index, NodeState.Empty);
        }

        currentUser = startState;
        round = 0;
        
        GameProcess();
    }

    private string GetWinnerText(WinnerType type)
    {
        if (type == WinnerType.PlayerWin)
            return "玩家胜利";

        if (type == WinnerType.ComputerWin)
            return "电脑胜利";

        return "平局";
    }

    private void GameProcess()
    {
        ShowRoundLog();
        
        if (IsAIRun())
        {
            AITurn();
            
            return;
        }
    }

    private void ShowRoundLog()
    {
        var text = $"回合：{round}\n轮次：{currentUser.ToString()}\n落子：{GetCurrentPlayerNodeState().ToString()}\n";

        if (IsAIRun())
            text += "\nAI 正在思考...";
        else
        {
            text += "\n轮到你下棋了";
        }

        SetLogText(text);
    }

    private bool IsAIRun()
    {
        if (startState == UserType.Computer)
        {
            return round % 2 == 0;
        }

        return round % 2 != 0;
    }

    private void OnNodeClick(int index)
    {
        if (isGameEnd)
        {
            RestartGame();
            return;
        }
        
        var nodeState = GetState(index);

        if (nodeState != NodeState.Empty) return;

        if (IsAIRun()) return;

        OperationNode(index);
    }

    private void OperationNode(int index)
    {
        SetState(index, GetCurrentPlayerNodeState());

        currentUser = (UserType)((int)currentUser * -1);

        var posLast = GetPos(index);

        isGameEnd = CheckIsEnd(posLast.x, posLast.y, out var winnerType);

        if (isGameEnd)
        {
            SetLogText(GetWinnerText(winnerType));

            return;
        }

        round++;
        
        GameProcess();
    }

    private void AITurn()
    {
        StartCoroutine(AITurnProcess());
    }

    private IEnumerator AITurnProcess() // 模拟AI思考
    {
        var index = GetMaxValueNodeIndex();

        yield return new WaitForSeconds(0.5f);
        
        OperationNode(index);        
    }

    private int GetMaxValueNodeIndex()
    {
        var dictScore = new Dictionary<int, int>();
        var aiState = startState == UserType.Computer ? NodeState.O : NodeState.X;

        var maxScore = int.MinValue;
        var maxIndex = -1;

        for (int x = 0; x < 3; x++)
        {
            for (int y = 0; y < 3; y++)
            {
                var index = GetIndex(x, y);

                if (GetState(index) != NodeState.Empty) continue;

                var listPath = GetNodeLinkPath(x, y);

                var scoreSum = 0;

                foreach (var path in listPath)
                {
                    var countSameAi = 0;
                    var countSameOther = 0;

                    var nodeScore = 0;

                    var lastIndexSameAI = index;
                    var lastIndexSameOther = index;

                    foreach (var nodeIndex in path)
                    {
                        var state = GetState(nodeIndex);

                        if (state == aiState)
                        {
                            nodeScore += Mathf.FloorToInt(Mathf.Pow(4, countSameAi + 1)) - GetNodeDistance(index,nodeIndex);
                            countSameAi++;
                            // lastIndexSameAI = nodeIndex;
                        }
                        else if (state != NodeState.Empty)
                        {
                            // lastIndexSameOther = nodeIndex;
                            nodeScore -= Mathf.FloorToInt(Mathf.Pow(3, countSameOther + 1)) - GetNodeDistance(index,nodeIndex);
                            
                            countSameOther++;
                        }
                    }

                    scoreSum += Math.Abs(nodeScore);
                }

                dictScore[index] = scoreSum;

                if (maxScore < scoreSum)
                {
                    maxScore = scoreSum;
                    maxIndex = index;
                }
            }
        }

        return maxIndex;
    }

    private int GetNodeDistance(int aIndex, int bIndex)
    {
        var posA = GetPos(aIndex);
        var posB = GetPos(bIndex);

        return Math.Max(Math.Abs(posA.x - posB.x), Math.Abs(posA.y - posB.y));
    }

    private bool CheckIsEnd(int x, int y, out WinnerType result)
    {
        result = WinnerType.Draw;
        var curState = GetState(GetIndex(x, y));
        if (curState == NodeState.Empty) return false;


        var listPath = GetNodeLinkPath(x, y);

        foreach (var path in listPath)
        {
            if (CheckPathIsSameState(path, curState))
            {
                result = GetWinner(curState);

                return true;
            }
        }

        return IsNoEmptyNode();
    }

    private bool IsNoEmptyNode()
    {
        foreach (var node in nodeStates)
        {
            if (node == NodeState.Empty) return false;
        }

        return true;
    }
    

    private WinnerType GetWinner(NodeState winnerNodeState)
    {
        if (winnerNodeState == NodeState.O)
            return startState == UserType.Player ? WinnerType.PlayerWin : WinnerType.ComputerWin;

        return startState != UserType.Player ? WinnerType.PlayerWin : WinnerType.ComputerWin;
    }

    private bool CheckPathIsSameState(List<int> path, NodeState state)
    {
        foreach (var index in path)
        {
            if (GetState(index) != state) return false;
        }

        return true;
    }

    private int GetIndex(int x, int y)
    {
        return y * 3 + x;
    }

    private Vector2Int GetPos(int index)
    {
        return new Vector2Int(index % 3, index / 3);
    }

    private List<List<int>> GetNodeLinkPath(int x, int y)
    {
        var res = new List<List<int>>();

        var range = 2;

        var listHor = new List<int>();

        for (int i = -range; i <= range; i++)
        {
            var newX = x + i;
            var newY = y;

            if (IsValidPos(newX, newY) == false) continue;

            var index = GetIndex(newX, newY);


            listHor.Add(index);
        }

        if (listHor.Count >= 3)
            res.Add(listHor);

        var listVer = new List<int>();

        for (int i = -range; i <= range; i++)
        {
            var newX = x;
            var newY = y + i;

            if (IsValidPos(newX, newY) == false) continue;
            var index = GetIndex(newX, newY);


            listVer.Add(index);
        }

        if (listVer.Count >= 3)
            res.Add(listVer);

        var listHypoA = new List<int>();

        for (int i = -range; i <= range; i++)
        {
            var newX = x + i;
            var newY = y + i;

            if (IsValidPos(newX, newY) == false) continue;
            var index = GetIndex(newX, newY);

            listHypoA.Add(index);
        }

        if (listHypoA.Count >= 3)
            res.Add(listHypoA);

        var listHypoB = new List<int>();

        for (int i = -range; i <= range; i++)
        {
            var newX = x + i;
            var newY = y - i;

            if (IsValidPos(newX, newY) == false) continue;
            var index = GetIndex(newX, newY);

            listHypoB.Add(index);
        }

        if (listHypoB.Count >= 3)
            res.Add(listHypoB);


        return res;
    }

    private bool IsValidPos(int x, int y)
    {
        return x is >= 0 and < 3 && y is >= 0 and < 3;
    }

    private NodeState GetCurrentPlayerNodeState()
    {
        if (round % 2 == 0)
            return NodeState.O;

        return NodeState.X;
    }

    public NodeRenderer GetRenderer(int index)
    {
        if (index < 0 || index > 8) throw new Exception("Index is out of range");

        return nodeRenderers[index];
    }

    public NodeState GetState(int index)
    {
        if (index < 0 || index > 8) throw new Exception("Index is out of range");

        return nodeStates[index];
    }

    public void SetState(int index, NodeState state)
    {
        if (index < 0 || index > 8) throw new Exception("Index is out of range");

        nodeStates[index] = state;
        GetRenderer(index)?.SetRenderState(state);
    }

    private void SetLogText(string text)
    {
        if (logText == null) return;

        logText.text = text;
    }

    private void OnDestroy()
    {
        instance = null;
    }
}
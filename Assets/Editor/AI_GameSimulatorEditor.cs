using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AI_GameSimulator))]
public class AI_GameSimulatorEditor : Editor
{
    const int BOARD_SIZE = 15;

    // 버튼 크기
    const int CELL = 28;

    // 클릭 시 순환: 0(빈칸) → 1(흑) → 2(백) → 0
    static readonly Color[] Colors =
    {
        new Color(0.75f, 0.65f, 0.4f),  // 0: 빈칸 (바둑판색)
        Color.black,                      // 1: 흑
        Color.white                       // 2: 백
    };

    static readonly string[] Labels = { "　", "●", "○" };

    public override void OnInspectorGUI()
    {
        AI_GameSimulator sim = (AI_GameSimulator)target;

        // 기본 필드 (ai, cellPrefab, timeLimit, maxDepth)
        DrawPropertiesExcluding(serializedObject, "presetBoard");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("프리셋 보드판", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("클릭: 빈칸 → 흑(●) → 백(○) → 빈칸", EditorStyles.miniLabel);
        EditorGUILayout.Space(4);

        // presetBoard 배열 크기 보정
        if (sim.presetBoard == null || sim.presetBoard.Length != BOARD_SIZE * BOARD_SIZE)
            sim.presetBoard = new int[BOARD_SIZE * BOARD_SIZE];

        // y=14(위) → y=0(아래) 순으로 그려서 실제 보드와 방향 일치
        for (int y = BOARD_SIZE - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();

            // 행 번호
            GUILayout.Label(y.ToString("D2"), GUILayout.Width(20));

            for (int x = 0; x < BOARD_SIZE; x++)
            {
                int idx = y * BOARD_SIZE + x;
                int val = sim.presetBoard[idx];

                // 버튼 색상 설정
                GUI.backgroundColor = Colors[val];

                GUIStyle style = new GUIStyle(GUI.skin.button)
                {
                    fontSize = 14,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = val == 1 ? Color.white : Color.black }
                };

                if (GUILayout.Button(Labels[val], style, GUILayout.Width(CELL), GUILayout.Height(CELL)))
                {
                    Undo.RecordObject(sim, "Preset Board Edit");
                    sim.presetBoard[idx] = (val + 1) % 3;
                    EditorUtility.SetDirty(sim);
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        // 열 번호
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("　", GUILayout.Width(20));
        for (int x = 0; x < BOARD_SIZE; x++)
            GUILayout.Label(x.ToString("D2"), GUILayout.Width(CELL));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        // 전체 초기화 버튼
        if (GUILayout.Button("보드 초기화", GUILayout.Height(28)))
        {
            Undo.RecordObject(sim, "Reset Preset Board");
            sim.presetBoard = new int[BOARD_SIZE * BOARD_SIZE];
            EditorUtility.SetDirty(sim);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
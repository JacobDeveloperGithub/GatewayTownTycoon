using System;
using UnityEngine;
using UnityEditor;

[Flags]
public enum DirectionMask : byte {
    None        = 0,
    North       = 1 << 0,
    South       = 1 << 1,
    East        = 1 << 2,
    West        = 1 << 3,
    NorthEast   = 1 << 4,
    NorthWest   = 1 << 5,
    SouthEast   = 1 << 6,
    SouthWest   = 1 << 7
}

public static class DirectionMaskUtil {
    public static DirectionMask FromBools(bool north, bool south, bool east, bool west, bool northEast, bool northWest, bool southEast, bool southWest) {
        DirectionMask m = DirectionMask.None;
        if (north)      m |= DirectionMask.North;
        if (south)      m |= DirectionMask.South;
        if (east)       m |= DirectionMask.East;
        if (west)       m |= DirectionMask.West;
        if (northEast)  m |= DirectionMask.NorthEast;
        if (northWest)  m |= DirectionMask.NorthWest;
        if (southEast)  m |= DirectionMask.SouthEast;
        if (southWest)  m |= DirectionMask.SouthWest;
        return m;
    }
}

[Serializable]
public sealed class RoadRule {
    public DirectionGrid Roads;
    public DirectionGrid Empty;
    public RoadNode prefab;
}

[Serializable]
public sealed class DirectionGrid {
    public bool north;
    public bool south;
    public bool east;
    public bool west;
    public bool northEast;
    public bool northWest;
    public bool southEast;
    public bool southWest;

    public DirectionMask GetMask() => DirectionMaskUtil.FromBools(north, south, east, west, northEast, northWest, southEast, southWest);
}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(RoadRule))]
public sealed class RoadRuleDrawer : PropertyDrawer {
    const float Cell = 16f;
    const float Gap = 2f;
    const float LabelH = 12f;
    const float GridPad = 2f;
    const float BetweenGrids = 8f;
    const float BetweenRows = 4f;
    const float PrefabMinW = 130f;

    static readonly GUIContent RoadsLabel = new GUIContent("Roads");
    static readonly GUIContent EmptyLabel = new GUIContent("Empty");
    static readonly GUIContent PrefabLabel = new GUIContent("Prefab");

    static float GridSide => 3 * Cell + 2 * Gap + 2 * GridPad; // total grid square side, including padding
    static float GridHeight => LabelH + GridSide;

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        float viewW = EditorGUIUtility.currentViewWidth - 20f; // rough indent/margin allowance
        float neededOneRow = GridSide + BetweenGrids + GridSide + BetweenGrids + PrefabMinW;
        bool wrap = viewW < neededOneRow;

        float topLine = EditorGUIUtility.singleLineHeight;
        if (!wrap)
            return topLine + Mathf.Max(GridHeight, EditorGUIUtility.singleLineHeight) + 2f;
        return topLine + GridHeight + BetweenRows + EditorGUIUtility.singleLineHeight + 2f;
    }

    public override void OnGUI(Rect pos, SerializedProperty property, GUIContent label) {
        EditorGUI.BeginProperty(pos, label, property);

        var roads = property.FindPropertyRelative(nameof(RoadRule.Roads));
        var empty = property.FindPropertyRelative(nameof(RoadRule.Empty));
        var prefab = property.FindPropertyRelative(nameof(RoadRule.prefab));

        pos.height = EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(pos, property.isExpanded, label, true);
        pos.y += pos.height + 2f;

        float neededOneRow = GridSide + BetweenGrids + GridSide + BetweenGrids + PrefabMinW;
        bool wrap = pos.width < neededOneRow;

        if (!wrap) {
            var gridR = new Rect(pos.x, pos.y, GridSide, GridHeight);
            var gridE = new Rect(gridR.xMax + BetweenGrids, pos.y, GridSide, GridHeight);
            var prefabRect = new Rect(gridE.xMax + BetweenGrids, pos.y, pos.width - (gridE.xMax + BetweenGrids), EditorGUIUtility.singleLineHeight);

            DrawGrid(gridR, roads, RoadsLabel);
            DrawGrid(gridE, empty, EmptyLabel);
            EditorGUI.PropertyField(prefabRect, prefab, PrefabLabel);
        } else {
            var gridR = new Rect(pos.x, pos.y, GridSide, GridHeight);
            var gridE = new Rect(gridR.xMax + BetweenGrids, pos.y, GridSide, GridHeight);
            DrawGrid(gridR, roads, RoadsLabel);
            DrawGrid(gridE, empty, EmptyLabel);
            var prefabRect = new Rect(pos.x, gridR.yMax + BetweenRows, pos.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.PropertyField(prefabRect, prefab, PrefabLabel);
        }

        EditorGUI.EndProperty();
    }

    static void DrawGrid(Rect rect, SerializedProperty gridProp, GUIContent caption) {
        var cap = new Rect(rect.x, rect.y, rect.width, LabelH);
        EditorGUI.LabelField(cap, caption);
        var box = new Rect(rect.x, rect.y + LabelH, rect.width, GridSide);
        EditorGUI.DrawRect(new Rect(box.x, box.y, box.width, box.height), new Color(0, 0, 0, 0.06f));
        var area = new Rect(box.x + GridPad, box.y + GridPad, box.width - 2 * GridPad, box.height - 2 * GridPad);

        for (int row = 0; row < 3; row++) {
            for (int col = 0; col < 3; col++) {
                var cell = new Rect(
                    area.x + col * (Cell + Gap),
                    area.y + row * (Cell + Gap),
                    Cell, Cell);

                if (row == 1 && col == 1) {
                    EditorGUI.DrawRect(cell, new Color(0, 0, 0, 0.08f));
                    var cross = Shrink(cell, 4f);
                    EditorGUI.DrawRect(new Rect(cross.x, cross.center.y - 0.5f, cross.width, 1f), new Color(0, 0, 0, 0.2f));
                    EditorGUI.DrawRect(new Rect(cross.center.x - 0.5f, cross.y, 1f, cross.height), new Color(0, 0, 0, 0.2f));
                    continue;
                }

                var sp = CellToProperty(gridProp, row, col);
                if (sp == null) continue;

                bool v = sp.boolValue;
                if (v) EditorGUI.DrawRect(cell, new Color(0.2f, 0.6f, 1f, 0.18f)); // light highlight

                var hit = Shrink(cell, 1f);
                EditorGUI.BeginChangeCheck();
                bool nv = EditorGUI.Toggle(hit, v);
                if (EditorGUI.EndChangeCheck())
                    sp.boolValue = nv;

                Handles.color = new Color(0, 0, 0, 0.2f);
                Handles.DrawAAPolyLine(1.5f, new Vector3(cell.x, cell.y), new Vector3(cell.xMax, cell.y), new Vector3(cell.xMax, cell.yMax), new Vector3(cell.x, cell.yMax), new Vector3(cell.x, cell.y));
            }
        }
    }

    static Rect Shrink(Rect r, float p) => new Rect(r.x + p, r.y + p, r.width - 2 * p, r.height - 2 * p);

    static SerializedProperty CellToProperty(SerializedProperty grid, int row, int col) {
        if (row == 1 && col == 1) return null;
        switch (row) {
            case 0:
                switch (col) {
                    case 0: return grid.FindPropertyRelative(nameof(DirectionGrid.northWest));
                    case 1: return grid.FindPropertyRelative(nameof(DirectionGrid.north));
                    case 2: return grid.FindPropertyRelative(nameof(DirectionGrid.northEast));
                }
                break;
            case 1:
                switch (col) {
                    case 0: return grid.FindPropertyRelative(nameof(DirectionGrid.west));
                    case 2: return grid.FindPropertyRelative(nameof(DirectionGrid.east));
                }
                break;
            case 2:
                switch (col) {
                    case 0: return grid.FindPropertyRelative(nameof(DirectionGrid.southWest));
                    case 1: return grid.FindPropertyRelative(nameof(DirectionGrid.south));
                    case 2: return grid.FindPropertyRelative(nameof(DirectionGrid.southEast));
                }
                break;
        }
        return null;
    }
}
#endif
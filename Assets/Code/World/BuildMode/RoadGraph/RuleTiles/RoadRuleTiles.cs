using UnityEngine;


public sealed class RoadRuleTiles : MonoBehaviour {
    [SerializeField] private RoadRule[] _rules;
    [SerializeField] private RoadNode _defaultTilePrefab;

    public RoadNode GetRoadRuleTilePrefab(Coordinate coord, Map map) {
        Coordinate n = new(coord.x, coord.y + 1);
        Coordinate e = new(coord.x + 1, coord.y);
        Coordinate s = new(coord.x, coord.y - 1);
        Coordinate w = new(coord.x - 1, coord.y);
        Coordinate ne = new(coord.x + 1, coord.y + 1);
        Coordinate nw = new(coord.x - 1, coord.y + 1);
        Coordinate se = new(coord.x + 1, coord.y - 1);
        Coordinate sw = new(coord.x - 1, coord.y - 1);
        
        MapObject objN = map.GetAt(n);
        MapObject objE = map.GetAt(e);
        MapObject objS = map.GetAt(s);
        MapObject objW = map.GetAt(w);
        MapObject objNE = map.GetAt(ne);
        MapObject objNW = map.GetAt(nw);
        MapObject objSE = map.GetAt(se);
        MapObject objSW = map.GetAt(sw);

        int roadMask = (int)DirectionMaskUtil.FromBools(
            objN != null  &&  objN is IConnectedToRoadGraph con0 && con0.IsValidConnectionSide(DirInto(coord, n)), 
            objS != null  &&  objS is IConnectedToRoadGraph con1 && con1.IsValidConnectionSide(DirInto(coord, s)), 
            objE != null  &&  objE is IConnectedToRoadGraph con2 && con2.IsValidConnectionSide(DirInto(coord, e)), 
            objW != null  &&  objW is IConnectedToRoadGraph con3 && con3.IsValidConnectionSide(DirInto(coord, w)), 
            objNE != null && objNE is IConnectedToRoadGraph con4 && con4.IsValidConnectionSide(DirInto(coord, ne)), 
            objNW != null && objNW is IConnectedToRoadGraph con5 && con5.IsValidConnectionSide(DirInto(coord, nw)), 
            objSE != null && objSE is IConnectedToRoadGraph con6 && con6.IsValidConnectionSide(DirInto(coord, se)), 
            objSW != null && objSW is IConnectedToRoadGraph con7 && con7.IsValidConnectionSide(DirInto(coord, sw)) 
        );

        RoadRule matchedRule = null;
        foreach (RoadRule rule in _rules) {
            int ruleEmpty = (int)rule.Empty.GetMask();
            int ruleRoad = (int)rule.Roads.GetMask();
            if (BitmaskContainsAll(roadMask, ruleRoad) && BitmaskContainsAll((~roadMask) & 0b11111111, ruleEmpty)) {
                matchedRule = rule;
                break;
            }
        }
        
        return matchedRule == null ? _defaultTilePrefab : matchedRule.prefab;
    }

    private Direction DirInto(Coordinate from, Coordinate to) {
        Coordinate dir = to - from;
        if (dir.Equals(new Coordinate(1,0))) return Direction.West;
        if (dir.Equals(new Coordinate(-1,0))) return Direction.East;
        if (dir.Equals(new Coordinate(0,1))) return Direction.South;
        if (dir.Equals(new Coordinate(0,-1))) return Direction.North;
        return Direction.North;
    }
    private bool BitmaskContainsAll(int mask, int rule) => (mask & rule) == rule;
}

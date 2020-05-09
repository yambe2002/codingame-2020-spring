﻿using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Grab the pellets as fast as you can!
 **/
class Player
{
    static void Main(string[] args)
    {
        var game = new Game();

        string[] inputs;
        inputs = Console.ReadLine().Split(' ');
        int width = int.Parse(inputs[0]); // size of the grid
        int height = int.Parse(inputs[1]); // top left corner is (x=0, y=0)
        var grid = new List<string>();
        for (int i = 0; i < height; i++)
        {
            string row = Console.ReadLine(); // one line of the grid: space " " is floor, pound "#" is wall
            grid.Add(row);
        }

        game.Width = width;
        game.Height = height;
        game.Grid = grid;

        // game loop
        while (true)
        {
            game.TurnStart();

            inputs = Console.ReadLine().Split(' ');
            int myScore = int.Parse(inputs[0]);
            int opponentScore = int.Parse(inputs[1]);
            game.MyScore = myScore;
            game.OpponentScore = opponentScore;

            int visiblePacCount = int.Parse(Console.ReadLine()); // all your pacs and enemy pacs in sight
            game.VisiblePacCount = visiblePacCount;

            for (int i = 0; i < visiblePacCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int pacId = int.Parse(inputs[0]); // pac number (unique within a team)
                bool mine = inputs[1] != "0"; // true if this pac is yours
                int x = int.Parse(inputs[2]); // position in the grid
                int y = int.Parse(inputs[3]); // position in the grid
                string typeId = inputs[4]; // unused in wood leagues
                int speedTurnsLeft = int.Parse(inputs[5]); // unused in wood leagues
                int abilityCooldown = int.Parse(inputs[6]); // unused in wood leagues

                game.SetPac(pacId, mine, x, y, typeId, speedTurnsLeft, abilityCooldown);
            }

            int visiblePelletCount = int.Parse(Console.ReadLine()); // all pellets in sight
            game.VisiblePelletCount = visiblePelletCount;

            for (int i = 0; i < visiblePelletCount; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int x = int.Parse(inputs[0]);
                int y = int.Parse(inputs[1]);
                int value = int.Parse(inputs[2]); // amount of points this pellet is worth

                game.SetPellet(x, y, value);
            }

            // Write an action using Console.WriteLine()
            // To debug: Console.Error.WriteLine("Debug messages...");

            Console.WriteLine(game.GetMove()); // MOVE <pacId> <x> <y>

        }
    }
}

public static class Util
{
    public static void Log(string val)
    {
        Console.Error.WriteLine(val);
    }

    public static void Log(CellItem[,] map)
    {
        for(int i=0; i < map.GetLength(0); i++)
        {
            for(int j=0; j<map.GetLength(1); j++)
            {
                Console.Error.Write(map[i, j].ToString());
            }
            Console.Error.WriteLine();
        }
        Console.Error.WriteLine();
    }

    public static int NegToINF(int val)
    {
        if (val < 0) return int.MaxValue;
        return val;
    }
}

public class CellItem
{
    public int X;
    public int Y;

    public bool IsPac => this is Pac;
    public bool IsPellet => this is Pellet;
    public bool IsWall => this is Wall;
    public bool IsFloor => this is Floor;

    public CellItem(int x, int y)
    {
        X = x;
        Y = y;
    }
}
public class Wall : CellItem
{
    public Wall(int x, int y) : base(x, y) { }
    public override string ToString()
    {
        return "#";
    }
}
public class Floor : CellItem
{
    public Floor(int x, int y) : base(x, y) { }
    public override string ToString()
    {
        return " ";
    }
}
public class Pac : CellItem
{
    public int ID;
    public bool Mine;
    public string TypeId;
    public int SpeedTurnsLeft;
    public int AbilityCoolDown;

    public Pac(int x, int y) : base(x, y) { }
    public override string ToString()
    {
        if (Mine) return "+";
        else return "^";
    }
}
public class Pellet : CellItem
{
    public int UncertaintyLevel;
    public int Value;
    public Pellet(int x, int y) : base(x, y) { }
    public override string ToString()
    {
        return Value == 10 ? "T" : Value.ToString();
    }
}

public class Game
{
    public int Width;
    public int Height;
    public List<string> Grid;

    public int OpponentScore;
    public int MyScore;
    public int VisiblePacCount;
    public int VisiblePelletCount;

    List<Pac> _pacs = new List<Pac>();
    List<Pac> _prevPacs = new List<Pac>();
    List<Pellet> _pellets = new List<Pellet>();

    public void TurnStart()
    {
        _prevPacs = _pacs.ToList();
        _pacs.Clear();
        _pellets.Clear();
    }
   
    public void SetPac(int pacId, bool mine, int x, int y, string typeId, int speedTurnsLeft, int abilityCooldown)
    {
        _pacs.Add(new Pac(x, y) { ID = pacId, Mine = mine, TypeId = typeId, SpeedTurnsLeft = speedTurnsLeft, AbilityCoolDown = abilityCooldown });
    }

    public void SetPellet(int x, int y, int value)
    {
        _pellets.Add(new Pellet(x, y) { Value = value });
    }

    CellItem[,] _prevMap;
    CellItem[,] _map;

    void CreateMap()
    {
        if (_prevMap == null) _prevMap = InitMap(Grid, Height, Width);

        _map = UpdateMap(_prevMap, Height, Width, _pacs, _pellets, _prevPacs);
        //Util.Log(_map);
        _prevMap = _map;
    }

    static CellItem[,] InitMap(List<string> grid, int height, int width)
    {
        var ret = new CellItem[height, width];
        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                if (grid[i][j] == '#') ret[i, j] = new Wall(j, i);
                else ret[i, j] = new Pellet(j, i);
            }
        }
        return ret;
    }

    static CellItem[,] UpdateMap(CellItem[,] prevMap, int height, int width, List<Pac> pacs, List<Pellet> pellets, List<Pac> prevPacs)
    {
        var ret = new CellItem[height, width];

        for (int i = 0; i < height; i++)
        {
            for (int j = 0; j < width; j++)
            {
                // wall remains as wall
                if (prevMap[i, j].IsWall) ret[i, j] = prevMap[i, j];
                // floor remains as floor
                else if (prevMap[i, j].IsFloor) ret[i, j] = prevMap[i, j];
                // pac becomes floor
                else if (prevMap[i, j].IsPac) ret[i, j] = new Floor(j, i);
                // pellet remains as pellet, but increase uncertainity
                else if (prevMap[i, j].IsPellet)
                {
                    var pellet = prevMap[i, j] as Pellet;
                    pellet.UncertaintyLevel++;
                    ret[i, j] = pellet;
                }
            }
        }

        // previous pac positions become floor
        if (prevPacs != null)
        {
            foreach (var prevPac in prevPacs)
            {
                ret[prevPac.Y, prevPac.X] = new Floor(prevPac.X, prevPac.Y);
            }
        }

        // overwrite with new information
        foreach (var pac in pacs)
            ret[pac.Y, pac.X] = pac;
        foreach (var pellet in pellets)
            ret[pellet.Y, pellet.X] = pellet;

        return ret;
    }

    (int, int) GetPrevPos(Pac pac)
    {
        var tgt = _prevPacs.FirstOrDefault(p => p.Mine == pac.Mine && p.ID == pac.ID);
        if (tgt == null) return (-1, -1);
        return (tgt.X, tgt.Y);
    }

    public string GetMove()
    {
        CreateMap();

        var ret = new StringBuilder();
        var avoid = new HashSet<(int, int)>();  // avoid moving to the same spot

        var myPacs = _pacs.Where(p => p.Mine);
        var enemyPacs = _pacs.Where(p => !p.Mine);
        // avoid moving to enemy
        foreach (var e in enemyPacs)
        {
            foreach(var n in GetAdjuscents(e)) avoid.Add(n);
        }

        // consider pacs close to large pellet and cluster pellet first
        myPacs = myPacs.OrderBy(p => Util.NegToINF(FindClosest(p, "PELLET_LARGE", avoid, true, true, (-1, -1), true).distance)).
            ThenBy(p => Util.NegToINF(FindClosest(p, "PELLET_LARGE_CLUSTER", avoid, true, true, (-1, -1), true).distance)).
            ThenBy(p => Util.NegToINF(FindClosest(p, "PELLET_CLUSTER", avoid, true, true, (-1, -1), true).distance)).
            ThenBy(p => Util.NegToINF(FindClosest(p, "PELLET_LARGE", avoid, true, false, (-1, -1), true).distance)).
            ThenBy(p => Util.NegToINF(FindClosest(p, "PELLET_LARGE_CLUSTER", avoid, true, false, (-1, -1), true).distance)).
            ThenBy(p => Util.NegToINF(FindClosest(p, "PELLET_CLUSTER", avoid, true, false, (-1, -1), true).distance));

        // MOVE
        foreach (var pac in myPacs)
        {
            Util.Log("ID: " + pac.ID + " - " + pac.X + "," + pac.Y);

            // avoid to go back
            var prevPos = GetPrevPos(pac);

            (int x, int y, int distance) tgt = (-1, -1, -1);

            foreach (var useAvoid in new bool[] { true, false })
            {
                foreach (var certainPellet in new bool[] { true, false })
                {
                    var smallPellet = FindClosest(pac, "PELLET_1", avoid, useAvoid, certainPellet, prevPos, false);
                    var largePellet = FindClosest(pac, "PELLET_LARGE", avoid, useAvoid, certainPellet, prevPos, false);
                    var largeclusterPellet = FindClosest(pac, "PELLET_LARGE_CLUSTER", avoid, useAvoid, certainPellet, prevPos, false);
                    var clusterPellet = FindClosest(pac, "PELLET_CLUSTER", avoid, useAvoid, certainPellet, prevPos, false);

                    Util.Log("large pellet: " + largePellet.x + "," + largePellet.y + "," + largePellet.distance);
                    Util.Log("large cluster pellet: " + largeclusterPellet.x + "," + largeclusterPellet.y + "," + largeclusterPellet.distance);
                    Util.Log("cluster pellet: " + clusterPellet.x + "," + clusterPellet.y + "," + clusterPellet.distance);
                    Util.Log("small pellet: " + smallPellet.x + "," + smallPellet.y + "," + smallPellet.distance);

                    // prefers large pellet
                    if (largePellet.x != -1 && smallPellet.x != -1 && largePellet.distance - smallPellet.distance <= 10)
                        tgt = largePellet;
                    if (largeclusterPellet.x != -1 && smallPellet.x != -1 && largeclusterPellet.distance - smallPellet.distance <= 10)
                        tgt = largePellet;
                    else if (clusterPellet.x != -1 && smallPellet.x != -1 && clusterPellet.distance - smallPellet.distance <= 5)
                        tgt = clusterPellet;
                    else
                        tgt = smallPellet;

                    if (tgt.x != -1) break;
                }
                
                // still no good target position found => goto the closest crossing
                if (tgt.x == -1)
                {
                    tgt = FindClosest(pac, "CROSSING", avoid, useAvoid, false, prevPos, false);
                    Util.Log("crossing: " + tgt.x + "," + tgt.y + "," + tgt.distance);
                }

                // still no good target position found => goto the closest curve
                if (tgt.x == -1)
                {
                    tgt = FindClosest(pac, "CURVE", avoid, useAvoid, false, prevPos, false);
                    Util.Log("curve: " + tgt.x + "," + tgt.y + "," + tgt.distance);
                }

                // still no? => just move to next floor
                if (tgt.x == -1)
                {
                    tgt = FindClosest(pac, "FLOOR", avoid, useAvoid, false, prevPos, false);
                    Util.Log("next floor: " + tgt.x + "," + tgt.y + "," + tgt.distance);
                }

                // still no? => just move to next floor, even previous position
                if (tgt.x == -1)
                {
                    tgt = FindClosest(pac, "FLOOR", avoid, useAvoid, false, prevPos, true);
                    Util.Log("next floor (allow prev): " + tgt.x + "," + tgt.y + "," + tgt.distance);
                }

                if (tgt.x != -1) break;
            }

            if (tgt.x == -1) Util.Log("could not find any move");

            if (tgt.x == -1) continue;  // should not happen
            if (ret.Length > 0) ret.Append(" | ");
            AddToAvoid((tgt.x, tgt.y), avoid);

            // avoid collision
            var adjs = GetAdjuscents(pac);
            foreach (var adj in adjs) avoid.Add(adj);

            ret.Append("MOVE " + pac.ID + " " + tgt.x + " " + tgt.y);
        }

        return ret.ToString();
    }

    void AddToAvoid((int x, int y) st, HashSet<(int, int)> avoid)
    {
        avoid.Add((st));
        if (!_map[st.y, st.x].IsPellet) return;

        // avoid so that multiple pacs go to the same pellet island, within dist=3
        var que = new Queue<(int, int)>();
        var passed = new HashSet<(int, int)>();
        que.Enqueue(st);
        passed.Add(st);
        var cnt = 0;
        while(que.Any())
        {
            var newQue = new Queue<(int, int)>();
            while (que.Any())
            {
                var item = que.Dequeue();
                avoid.Add(item);
                for (int d = 0; d < 4; d++)
                {
                    var nX = (item.Item1 + dir1[d] + Width) % Width; // side can go through
                    var nY = item.Item2 + dir2[d];
                    if (nY < 0 || nY >= Height) continue;
                    if (!_map[nY, nX].IsPellet) continue;
                    if (passed.Contains((nX, nY))) continue;
                    newQue.Enqueue((nX, nY));
                }
            }
            cnt++;
            if (cnt == 3) break;
            que = newQue;
        }
    }

    List<(int, int)> GetAdjuscents(CellItem item)
    {
        var ret = new List<(int, int)> { (item.X, item.Y) };
        for (int d = 0; d < 4; d++)
        {
            var nX = (item.X + dir1[d] + Width) % Width; // side can go through
            var nY = item.Y + dir2[d];
            if (nY < 0 || nY >= Height) continue;
            if (_map[nY, nX].IsWall) continue;
            ret.Add((nX, nY));
        }
        return ret;
    }

    int[] dir1 = new int[] { 1, -1, 0, 0 };
    int[] dir2 = new int[] { 0, 0, 1, -1 };

    (int x, int y, int distance) FindClosest(CellItem item, string tgtType, HashSet<(int, int)> avoid, bool useAvoid, bool certainPellet,
        (int, int) prevPos, bool allowPrevPos)
    {
        var passed = new HashSet<(int, int)>();
        var que = new Queue<(int, int, int)>();
        que.Enqueue((item.X, item.Y, 0));
        passed.Add((item.X, item.Y));

        while (que.Any())
        {
            var cur = que.Dequeue();
            if (useAvoid && avoid.Contains((cur.Item1, cur.Item2))) continue;
            if (!allowPrevPos && cur.Item1 == prevPos.Item1 && cur.Item2 == prevPos.Item2) continue;

            if (cur.Item1 != item.X || cur.Item2 != item.Y)
            {
                if (IsTarget(_map[cur.Item2, cur.Item1], tgtType, certainPellet))
                    return cur;
            }

            for (int d = 0; d < 4; d++)
            {
                var nX = (cur.Item1 + dir1[d] + Width) % Width; // side can go through
                var nY = cur.Item2 + dir2[d];
                if (nY < 0 || nY >= Height) continue;
                if (_map[nY, nX].IsWall) continue;
                if (_map[nY, nX].IsPac) continue;       // avoid collision
                if (passed.Contains((nX, nY))) continue;
                que.Enqueue((nX, nY, cur.Item3 + 1));
                passed.Add((nX, nY));
            }
        }

        return (-1, -1, -1);
    }

    bool IsTarget(CellItem item, string tgtType, bool certainPellet)
    {
        if (tgtType == "WALL")
            return item.IsWall;
        if (tgtType == "PELLET")
            return item.IsPellet && (!certainPellet || (item as Pellet).UncertaintyLevel < 10);
        if (tgtType == "FLOOR")
            return item.IsFloor;
        if (tgtType == "PAC")
            return item.IsPac;
        if (tgtType == "PAC_MINE")
            return item.IsPac && (item as Pac).Mine;
        if (tgtType == "PAC_ENEMY")
            return item.IsPac && !(item as Pac).Mine;

        if (tgtType == "PELLET_1")
            return item.IsPellet && (item as Pellet).Value == 1 && (!certainPellet || (item as Pellet).UncertaintyLevel < 10);
        if (tgtType == "PELLET_LARGE")
            return item.IsPellet && (item as Pellet).Value > 1 && (!certainPellet || (item as Pellet).UncertaintyLevel < 10);

        if (tgtType == "PELLET_CLUSTER")
            return IsCluster(item, "PELLET", certainPellet);
        if (tgtType == "PELLET_LARGE_CLUSTER")
            return IsCluster(item, "PELLET", certainPellet, 5, 6);

        if (tgtType == "CROSSING")
            return IsCrossing(item);

        if (tgtType == "CURVE")
            return IsCurve(item);

        return false;
    }

    // more than count targets within maxDist from the cell
    bool IsCluster(CellItem item, string tgtType, bool certainPellet, int maxDist=3, int count=4)
    {
        if (!IsTarget(item, "PELLET", certainPellet)) return false;
        var cnt = (_map[item.Y, item.X] as Pellet).Value;
        var adjs = GetAdjuscents(item);
        foreach (var n in adjs)
        {
            var wk = _map[n.Item2, n.Item1];
            if (!wk.IsPellet) continue;
            cnt += (wk as Pellet).Value;
        }
        return cnt >= 3;
    }

    bool IsNextTo(CellItem item, string tgtType, bool certainPellet)
    {
        var adjs = GetAdjuscents(item);
        foreach(var n in adjs)
        {
            if (IsTarget(_map[n.Item2, n.Item1], tgtType, certainPellet))
                return true;
        }
        return false;
    }

    bool IsCrossing(CellItem item)
    {
        var cnt = 0;
        var adjs = GetAdjuscents(item);
        foreach (var n in adjs)
        {
            if (!IsTarget(_map[n.Item2, n.Item1], "WALL", false))
                cnt++;
        }
        return cnt >= 3;
    }

    bool IsCurve(CellItem item)
    {
        var floors = new List<(int, int)>();
        var adjs = GetAdjuscents(item);
        foreach (var n in adjs)
        {
            if (!IsTarget(_map[n.Item2, n.Item1], "WALL", false))
                floors.Add((n.Item1, n.Item2));
        }

        if (floors.Count() != 2) return false;
        if (floors[0].Item1 == floors[1].Item1) return false;
        if (floors[0].Item2 == floors[1].Item2) return false;
        return true;
    }
}
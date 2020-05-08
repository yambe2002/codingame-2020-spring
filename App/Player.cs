using System;
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
    public bool IsUnknown => this is Unknown;

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
public class Unknown : CellItem
{
    public Unknown(int x, int y) : base(x, y) { }
    public override string ToString()
    {
        return "?";
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
    public int Value;
    public Pellet(int x, int y) : base(x, y) { }
    public override string ToString()
    {
        return Value.ToString();
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
    List<Pellet> _pellets = new List<Pellet>();

    public void TurnStart()
    {
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
        // for wood leage, no need to use _prevMap
        _map = GetWoodLeagueMap();
        //Util.Log(_map);

        _prevMap = _map;
    }

    CellItem[,] GetWoodLeagueMap()
    {
        var ret = new CellItem[Height, Width];
        for (int i = 0; i < Height; i++)
        {
            for (int j = 0; j < Width; j++)
            {
                if (Grid[i][j] == '#') ret[i, j] = new Wall(j, i);
                else ret[i, j] = new Floor(j, i);
            }
        }
        foreach (var pac in _pacs)
            ret[pac.Y, pac.X] = pac;
        foreach (var pellet in _pellets)
            ret[pellet.Y, pellet.X] = pellet;

        return ret;
    }

    public string GetMove()
    {
        CreateMap();

        var ret = new StringBuilder();
        var avoid = new HashSet<(int, int)>();  // avoid moving to the same spot

        var myPacs = _pacs.Where(p => p.Mine);

        // consider pacs close to large pellet and cluster pellet first
        myPacs = myPacs.OrderBy(p => Util.NegToINF(FindClosest(p, "PELLET_LARGE", avoid, true).distance)).
            ThenBy(p => Util.NegToINF(FindClosest(p, "PELLET_CLUSTER", avoid, true).distance));

        foreach(var pac in myPacs)
        {
            (int x, int y, int distance) tgt = (-1, -1, -1);

            foreach (var useAvoid in new bool[] { true, false })
            {
                var largePellet = FindClosest(pac, "PELLET_LARGE", avoid, useAvoid);
                var clusterPellet = FindClosest(pac, "PELLET_CLUSTER", avoid, useAvoid);
                var smallPellet = FindClosest(pac, "PELLET_1", avoid, useAvoid);

                // prefers large pellet
                if (largePellet.x != -1 && smallPellet.x != -1 && largePellet.distance - smallPellet.distance <= 10)
                    tgt = largePellet;
                else if (clusterPellet.x != -1 && smallPellet.x != -1 && clusterPellet.distance - smallPellet.distance <= 5)
                    tgt = clusterPellet;
                else
                    tgt = smallPellet;

                // NOTE: not used in wood league
                // no good target position found => goto position next to unkwon
                if(tgt.x == -1)
                    tgt = FindClosest(pac, "NEXT_TO_UNKNOWN", avoid, useAvoid);

                // still no good target position found => goto the closest crossing
                if (tgt.x == -1)
                    tgt = FindClosest(pac, "CROSSING", avoid, useAvoid);

                // still no good target position found => goto the closest curve
                if (tgt.x == -1)
                    tgt = FindClosest(pac, "CURVE", avoid, useAvoid);

                // still no? => just move to next cell
                if (tgt.x == -1)
                    tgt = FindClosest(pac, "FLOOR", avoid, useAvoid);

                if (tgt.x != -1) break;
            }

            if (tgt.x == -1) continue;  // should not happen
            if (ret.Length > 0) ret.Append(" | ");
            avoid.Add((tgt.x, tgt.y));
            ret.Append("MOVE " + pac.ID + " " + tgt.x + " " + tgt.y);
        }

        return ret.ToString();
    }

    int[] dir1 = new int[] { 1, -1, 0, 0 };
    int[] dir2 = new int[] { 0, 0, 1, -1 };

    (int x, int y, int distance) FindClosest(CellItem item, string tgtType, HashSet<(int, int)> avoid, bool useAvoid)
    {
        var passed = new HashSet<(int, int)>();
        var que = new Queue<(int, int, int)>();
        que.Enqueue((item.X, item.Y, 0));
        passed.Add((item.X, item.Y));

        while (que.Any())
        {
            var cur = que.Dequeue();
            if (IsTarget(_map[cur.Item2, cur.Item1], tgtType) && (!useAvoid || !avoid.Contains((cur.Item1, cur.Item2))))
                return cur;

            for (int d = 0; d < 4; d++)
            {
                var nX = (cur.Item1 + dir1[d] + Width) % Width; // side can go through
                var nY = cur.Item2 + dir2[d];
                if (nY < 0 || nY >= Height) continue;
                if (_map[nY, nX].IsWall) continue;
                if (_map[nY, nX].IsUnknown) continue;   // conservative
                if (_map[nY, nX].IsPac) continue;       // avoid collision
                if (passed.Contains((nX, nY))) continue;
                que.Enqueue((nX, nY, cur.Item3 + 1));
                passed.Add((nX, nY));
            }
        }

        return (-1, -1, -1);
    }

    bool IsTarget(CellItem item, string tgtType)
    {
        if (tgtType == "PELLET_1")
            return item.IsPellet && (item as Pellet).Value == 1;
        if (tgtType == "PELLET_LARGE")
            return item.IsPellet && (item as Pellet).Value > 1;
        if (tgtType == "PELLET")
            return item.IsPellet;
        if (tgtType == "FLOOR")
            return item.IsFloor;

        if (tgtType == "PELLET_CLUSTER")
        {

        }

        if (tgtType == "NEXT_TO_UNKNOWN")
        {

        }
        if (tgtType == "CROSSING")
        {

        }
        if (tgtType == "CURVE")
        {

        }

        return false;
    }
}
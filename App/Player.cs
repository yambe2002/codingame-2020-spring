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
        Console.Error.WriteLine();
    }

    public static void Log(string title, (int x, int y, int distance, int tgtX, int tgtY) tpl)
    {
        Console.Error.Write(title);
        Console.Error.Write(" x:" + tpl.x);
        Console.Error.Write(" y:" + tpl.y);
        Console.Error.Write(" dist:" + tpl.distance);
        Console.Error.Write(" tgtX:" + tpl.tgtX);
        Console.Error.Write(" tgtY:" + tpl.tgtY);
        Console.Error.WriteLine();
    }

    public static int NegToINF(int val)
    {
        if (val < 0) return int.MaxValue;
        return val;
    }

    public static void Log(List<Action> actions)
    {
        foreach (var action in actions)
            Log(action);
    }

    public static void Log(Action action)
    {
        Log("ID: " + action.Pac.ID + "  Target: " + action.TargetX + "," + action.TargetY + " Next: " + action.NextX + "," + action.NextY + " ExpScore:" + action.ExpScore +
            (action.IsAttack ? " *Attach*" : "") + (action.IsRun ? " *Run" : ""));
    }

    public static void Log(List<(int, int)> path)
    {
        foreach (var p in path)
            Console.Error.Write(p.Item1 + "," + p.Item2 + "|");
        Console.Error.WriteLine();
    }
}

public class CellItem
{
    public int X;
    public int Y;

    public bool IsPac => this is Pac;
    public bool IsEnemyPac => IsPac && !(this as Pac).Mine;
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
    // ROCK or PAPER or SCISSORS
    public static string ROCK = "ROCK";
    public static string PAPER = "PAPER";
    public static string SCISSORS = "SCISSORS";

    public int ID;
    public bool Mine;
    public string TypeId;
    public int SpeedTurnsLeft;
    public int AbilityCoolDown;

    public bool IsDead => TypeId == "DEAD";

    public bool AtSpeed => SpeedTurnsLeft > 0;
    public bool CanSwitch => SpeedTurnsLeft == 0 && AbilityCoolDown == 0;
    public int UncertainityLevel;

    public int TotalTimeUntilSwitchable
    {
        get
        {
            if (!AtSpeed) return AbilityCoolDown;
            return SpeedTurnsLeft + 10;
        }
    }

    public Pac(int x, int y) : base(x, y) { }
    public override string ToString()
    {
        if (Mine) return "+";
        else return "^";
    }

    public bool Wins(Pac other)
    {
        if (TypeId == ROCK) return other.TypeId == SCISSORS;
        if (TypeId == SCISSORS) return other.TypeId == PAPER;
        if (TypeId == PAPER) return other.TypeId == ROCK;
        return false;
    }

    public bool Evens(Pac other)
    {
        return TypeId == other.TypeId;
    }

    public static string TypeForWin(Pac other)
    {
        return TypeForWin(other.TypeId);
    }

    public static string TypeForWin(string othetTypeId)
    {
        if (othetTypeId == ROCK) return PAPER;
        if (othetTypeId == SCISSORS) return ROCK;
        if (othetTypeId == PAPER) return SCISSORS;
        return null;
    }

    // when both me and other change their type at the same time
    public string TypeForWin_Cross(Pac other)
    {
        var changedOtherType = TypeForWin(this);
        return TypeForWin(changedOtherType);
    }

}
public class Pellet : CellItem
{
    public int UncertaintyLevel;
    public int Value;
    public Pellet(int x, int y) : base(x, y) { }
    public override string ToString()
    {
        return Value == 10 ? "T" : UncertaintyLevel == 0 ? "I" : Value.ToString();
    }
}

public class Action
{
    public Pac Pac;

    // SPEED
    public bool IsSpeed;

    // SWITCH
    public bool IsSwitch;
    public string NextType;

    // MOVE
    public bool IsMove;
    public int NextX = -1;
    public int NextY = -1;
    public int TargetX;
    public int TargetY;
    public int Distance;
    public List<(int x, int y)> Path;
    public double ExpScore;
    public bool IsAttack;
    public bool IsRun;

    public override string ToString()
    {
        if (IsSpeed) return "SPEED " + Pac.ID;
        if (IsSwitch) return "SWITCH " + Pac.ID + " " + NextType;
        if (IsMove) return "MOVE " + Pac.ID + " " + NextX + " " + NextY;
        return "";
    }

    public static Action GetMove(Pac pac, List<(int x, int y)> path, double score)
    {
        var nextX = path[1].x;
        var nextY = path[1].y;
        if(pac.SpeedTurnsLeft > 0 && path.Count() > 2)
        {
            nextX = path[2].x;
            nextY = path[2].y;
        }
        var targetX = path.Last().x;
        var targetY = path.Last().y;

        return new Action
        {
            Pac = pac,
            IsMove = true,
            NextX = nextX,
            NextY = nextY,
            Path = path,
            TargetX = targetX,
            TargetY = targetY,
            Distance = path.Count(),
            ExpScore = score
        };
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
    List<Pac> _prevprevPacs = new List<Pac>();
    List<Pellet> _pellets = new List<Pellet>();

    public void TurnStart()
    {
        _prevprevPacs = _prevPacs;
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

        _map = UpdateMap(_prevMap, Height, Width, _pacs, _pellets, _prevPacs, _prevprevPacs);
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
                else ret[i, j] = new Pellet(j, i) { Value = 1 };
            }
        }
        return ret;
    }

    CellItem[,] UpdateMap(CellItem[,] prevMap, int height, int width, List<Pac> pacs, List<Pellet> pellets, List<Pac> prevPacs, List<Pac> prevPrevPacs)
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

        // cleanout visible cell with floor
        foreach(var pac in pacs.Where(p => p.Mine))
        {
            var visibleCells = GetVisibleCells(pac, prevMap, width, height);
            foreach (var vCell in visibleCells)
                ret[vCell.Item2, vCell.Item1] = new Floor(vCell.Item1, vCell.Item2);
        }

        // overwrite with new information for visible cells
        foreach (var pac in pacs)
        {
            if (pac.IsDead)
                ret[pac.Y, pac.X] = new Floor(pac.X, pac.Y);
            else
                ret[pac.Y, pac.X] = pac;
        }
        foreach (var pellet in pellets)
            ret[pellet.Y, pellet.X] = pellet;

        // make a guess for invisible enemy pacs
        GuessEnemies(ret, height, width, pacs, prevPacs, prevPrevPacs);

        return ret;
    }

    void GuessEnemies(CellItem[,] ret, int height, int width, List<Pac> pacs, List<Pac> prevPacs, List<Pac> prevPrevPacs)
    {
        if (prevPacs?.Any() != true) return;    // no information for guess
        foreach(var prevEnemy in prevPacs.Where(p => !p.Mine))
        {
            if (pacs.Any(p => !p.Mine && p.ID == prevEnemy.ID)) continue;   // already have real information
            if (prevEnemy.UncertainityLevel >= 5) continue; // too vague info to guess

            // let's guess
            var lowPosiblePositions = new HashSet<(int, int)>();

            var prevPrevEnemy = prevPrevPacs?.FirstOrDefault(p => !p.Mine && p.ID == prevEnemy.ID);
            if (prevPrevEnemy != null) lowPosiblePositions.Add((prevPrevEnemy.X, prevPrevEnemy.Y));

            var newEnemyPac = new Pac(prevEnemy.X, prevEnemy.Y)
            {
                ID = prevEnemy.ID, Mine = false, TypeId = prevEnemy.TypeId, AbilityCoolDown = prevEnemy.AbilityCoolDown, SpeedTurnsLeft = prevEnemy.SpeedTurnsLeft,
                UncertainityLevel = prevEnemy.UncertainityLevel + 1
            };
            if (newEnemyPac.AbilityCoolDown > 0) newEnemyPac.AbilityCoolDown--;
            if (newEnemyPac.SpeedTurnsLeft > 0) newEnemyPac.SpeedTurnsLeft--;

            var newX = -1;
            var newY = -1;

            // assume he made the best score move
            var superPellectPath = FindClosest(newEnemyPac, "PELLET_LARGE", null, false, false, new (int, int)[0], true);
            if (superPellectPath.x != -1 && superPellectPath.distance < 8)
            {
                Util.Log("going to the closest super pellet!");

                var path = FindPath(ret, width, height, newEnemyPac, ret[superPellectPath.tgtY, superPellectPath.tgtX], false, null, false, false);
                Util.Log("  -> " + superPellectPath.x + "," + superPellectPath.y);

                if (path.path != null && path.path.Count() >= 2)
                {
                    Util.Log("  -> " + path.path[1].x + "," + path.path[1].y);
                    newX = path.path[1].x;
                    newY = path.path[1].y;
                }
            }
            else
            {
                Util.Log("making reasonable scoring move!");

                var act = GetBestScoreAction(prevEnemy, lowPosiblePositions, new HashSet<(int, int)>());
                if (act != null && act.IsMove && act.Path != null && act.Path.Count() >= 2)
                {
                    newX = act.Path[1].x;
                    newY = act.Path[1].y;
                }
            }

            // assign to the map
            if(newX != -1)
            {
                newEnemyPac.X = newX;
                newEnemyPac.Y = newY;
                ret[newEnemyPac.Y, newEnemyPac.X] = newEnemyPac;
            }
        }
    }

    static List<(int, int)> GetVisibleCells(Pac pac, CellItem[,] map, int width, int height)
    {
        var ret = new HashSet<(int, int)>();

        // right
        var x = (pac.X + 1 + width) % width;
        var y = pac.Y;
        while (!map[y, x].IsWall)
        {
            if (ret.Contains((x, y))) break;
            ret.Add((x, y));
            x = (x + 1 + width) % width;
        }

        // left
        x = (pac.X - 1 + width) % width;
        y = pac.Y;
        while (!map[y, x].IsWall)
        {
            if (ret.Contains((x, y))) break;
            ret.Add((x, y));
            x = (x - 1 + width) % width;
        }

        // top
        x = pac.X;
        y = pac.Y - 1;
        while(y >= 0 && !map[y, x].IsWall)
        {
            ret.Add((x, y));
            y--;
        }

        // bottom
        x = pac.X;
        y = pac.Y + 1;
        while (y < height && !map[y, x].IsWall)
        {
            ret.Add((x, y));
            y++;
        }

        return ret.ToList();
    }

    (int, int) GetPrevPos(Pac pac, List<Pac> prevs)
    {
        var tgt = prevs?.FirstOrDefault(p => p.Mine == pac.Mine && p.ID == pac.ID);
        if (tgt == null) return (-1, -1);
        return (tgt.X, tgt.Y);
    }

    (int, int)[] GetPrevPos(Pac pac)
    {
        return new (int, int)[] { GetPrevPos(pac, _prevPacs), GetPrevPos(pac, _prevprevPacs) };
    }

    public string GetMove()
    {
        CreateMap();
        
        var actions = new List<Action>();
        var zeroScores = new HashSet<(int, int)>();
        var nexts = new HashSet<(int, int)>();
        var dontSpeetup = new HashSet<Pac>();

        // action when there are enemy close
        AddActions_ChangeTypeOrRunAway(actions, zeroScores, nexts, dontSpeetup);

        // better make speed up?
        AddActions_SpeedUp(actions, zeroScores, nexts);

        // trys to get super pellets if possible
        AddActions_SuperPellet(actions, zeroScores, nexts);

        // find path for the best score in several steps
        AddActions_BestScoreExp(actions, zeroScores, nexts);

        Util.Log(actions);
        return GetAns(actions);
    }

    string GetAns(List<Action> actions)
    {
        var ret = new StringBuilder();
        for (int i = 0; i < actions.Count(); i++)
        {
            if (ret.Length > 0) ret.Append(" | ");
            ret.Append(actions[i].ToString());
        }
        return ret.ToString();
    }

    void AddActions_ChangeTypeOrRunAway(List<Action> actions, HashSet<(int, int)> zeroScores, HashSet<(int, int)> nexts, HashSet<Pac> dontSpeetup)
    {
        var enemyPacs = _pacs.Where(p => !p.Mine && !p.IsDead);

        foreach(var enemy in enemyPacs)
        {
            var usedMyPacs = actions.Select(a => a.Pac).ToHashSet();
            var myPacs = _pacs.Where(p => p.Mine && !p.IsDead).Where(p => !usedMyPacs.Contains(p));

            var dists = new List<(List<(int x, int y)> path, Pac pac)>();
            foreach(var myPac in myPacs)
            {
                var path = FindPath(_map, Width, Height, myPac, enemy, true, null, false, false);
                if (path.path == null) continue;
                dists.Add((path.path, myPac));
            }
            foreach(var tgtDist in dists.OrderBy(d => d.path.Count()))
            {
                AddActions_ChangeTypeOrRunAway(tgtDist.pac, enemy, tgtDist.path, actions, zeroScores, nexts, dontSpeetup);
            }
        }
    }

    void AddActions_ChangeTypeOrRunAway(Pac myPac, Pac enemy, List<(int, int)> pathToEnemy, List<Action> actions, 
        HashSet<(int, int)> zeroScores, HashSet<(int, int)> nexts, HashSet<Pac> dontSpeedUp)
    {
        var distance = pathToEnemy.Count() - 1;

        // very close
        if (distance == 1 || (distance == 2 && enemy.AtSpeed))
        {
            // winning
            if (myPac.Wins(enemy))
            {                
                if (!enemy.CanSwitch)   // enemy cannot switch his type
                {
                    // attack only if I can win in high posibility
                    if(myPac.AtSpeed && !enemy.AtSpeed)
                        AddActions_Attack(myPac, enemy, pathToEnemy, actions, zeroScores, nexts);
                }
            }
            else if(myPac.Evens(enemy))
            {
                if(enemy.CanSwitch && !myPac.CanSwitch)  // enemy can switch but I cannot => run away, no time to speed up!
                {
                    dontSpeedUp.Add(myPac);
                }
            }
            else
            {
                // losing
                // change type only if enemy is at speed and I'm not => urgent
                if (!myPac.AtSpeed && enemy.AtSpeed)
                {
                    var nextType = Pac.TypeForWin(enemy);
                    AddActions_ChangeType(myPac, nextType, actions, zeroScores, nexts);
                }
            }
        }
    }

    void AddActions_ChangeType(Pac myPac, string nextType, List<Action> actions, HashSet<(int, int)> zeroScores, HashSet<(int, int)> nexts)
    {
        var act = new Action { Pac = myPac, NextType = nextType, IsSwitch = true };
        actions.Add(act);
        nexts.Add((myPac.X, myPac.Y));
    }

    void AddActions_Attack(Pac myPac, Pac enemy, List<(int, int)> pathToEnemy, List<Action> actions, HashSet<(int, int)> zeroScores, HashSet<(int, int)> nexts)
    {
        if (pathToEnemy == null) pathToEnemy = FindPath(_map, Width, Height, myPac, enemy, false, null, false, true).path;
        if (pathToEnemy?.Any() != true) return;

        var nextToEnemy = pathToEnemy.Count() >= 2 ? pathToEnemy[pathToEnemy.Count() - 2] : pathToEnemy[0];
        var invalids = new HashSet<(int, int)> { nextToEnemy, (myPac.X, myPac.Y) };

        foreach(var n in GetAdjuscents(enemy))
        {
            if (invalids.Contains(n)) continue;
            if (_map[n.Item2, n.Item1].IsWall) continue;

            // found good target

            var act = new Action { Pac = myPac, NextX = n.Item1, NextY = n.Item2, TargetX = n.Item1, TargetY = n.Item2, Path = pathToEnemy, IsMove = true, IsAttack = true };
            actions.Add(act);
            nexts.Add((pathToEnemy[1].Item1, pathToEnemy[1].Item2));
            zeroScores.Add((pathToEnemy[1].Item1, pathToEnemy[1].Item2));
            return;
        }
    }

    void AddActions_SpeedUp(List<Action> actions, HashSet<(int, int)> zeroScores, HashSet<(int, int)> nexts)
    {
        var usedMyPacs = actions.Select(a => a.Pac).ToHashSet();
        var myPacs = _pacs.Where(p => p.Mine && !p.IsDead).Where(p => !usedMyPacs.Contains(p)).Where(p => p.AbilityCoolDown == 0);
        if (!myPacs.Any()) return;

        var enemyPacs = _pacs.Where(p => !p.Mine && !p.IsDead);
        var superPellets = _pellets.Where(p => p.Value > 1);
        var doSpeedUp = false;

        // if there are any super pellets, better speed up
        if (superPellets.Any()) doSpeedUp = true;

        // do always if possible
        doSpeedUp = true;

        if (doSpeedUp)
        {
            foreach (var myPac in myPacs)
                actions.Add(new Action { Pac = myPac, IsSpeed = true });
        }
    }

    void AddActions_SuperPellet(List<Action> actions, HashSet<(int, int)> zeroScores, HashSet<(int, int)> nexts)
    {
        var superPellets = _pellets.Where(p => p.Value > 1);
        // no super pellets
        if (!superPellets.Any()) return;

        var usedMyPacs = actions.Select(a => a.Pac).ToHashSet();
        var myPacs = _pacs.Where(p => p.Mine && !p.IsDead).Where(p => !usedMyPacs.Contains(p)).ToList();
        if (!myPacs.Any()) return;
        var enemyPacs = _pacs.Where(p => !p.Mine && !p.IsDead);

        // (distance, pellet, pac)
        var info = new List<((List<(int x, int y)> path, double score) pathInfo, Pellet pellet, Pac pac)>();
        foreach(var pellet in superPellets)
        {
            foreach(var pac in myPacs)
            {
                var path = FindPath(_map, Width, Height, pac, pellet, false, null, false, true); // ignore score
                if (path.path == null) continue;
                info.Add((path, pellet, pac));
            }
        }

        // from shorter path 
        info = info.OrderBy(i => i.pathInfo.path.Count()).ToList();

        // up to ONE pacs will be assinged
        var assignment = new Dictionary<Pellet, List<(Pac pac, int nextX, int nextY, int distance, List<(int, int)> willingPath)>>();
        foreach (var p in superPellets) assignment[p] = new List<(Pac pac, int nextX, int nextY, int distance, List<(int, int)> willingPath)>();

        var used = new HashSet<Pac>();
        for (int cnt = 0; cnt < 1; cnt++)
        {
            for (int i = 0; i < info.Count(); i++)
            {
                var pathInfo = info[i].pathInfo;
                var pellet = info[i].pellet;
                var pac = info[i].pac;
                if (used.Contains(pac)) continue;
                if (assignment[pellet].Count() > cnt) continue;
                // collision
                if (nexts.Contains((pathInfo.path[1].x, pathInfo.path[1].y))) continue;
                assignment[pellet].Add((pac, pathInfo.path[1].x, pathInfo.path[1].y, pathInfo.path.Count(), pathInfo.path));
                nexts.Add((pathInfo.path[1].x, pathInfo.path[1].y));
                used.Add(pac);
            }
        }
        
        // make move
        foreach(var pellet in assignment.Keys)
        {
            foreach(var tpl in assignment[pellet])
            {
                var action = Action.GetMove(tpl.pac, tpl.willingPath, -1);
                actions.Add(action);
                AddToZeroScores(action, zeroScores);
            }
        }
    }

    void AddActions_BestScoreExp(List<Action> actions, HashSet<(int, int)> zeroScores, HashSet<(int, int)> nexts)
    {
        var usedMyPacs = actions.Select(a => a.Pac).ToHashSet();
        var myPacs = _pacs.Where(p => p.Mine && !p.IsDead).Where(p => !usedMyPacs.Contains(p)).ToList();

        foreach (var mypac in myPacs)
        {
            var act = GetBestScoreAction(mypac, zeroScores, nexts);
            if (act == null) continue;
            actions.Add(act);
            AddToZeroScores(act, zeroScores);
        }
    }

    Action GetBestScoreAction(Pac pac, HashSet<(int, int)> zeroScores, HashSet<(int, int)> nexts)
    {
        int max_path_size = 20;
        int path_size = 10;

        var prevPos = GetPrevPos(pac, _prevPacs);
        var que = new Queue<(double score, int x, int y)>();

        var passed = new HashSet<(int x, int y)>();
        var prev = new Dictionary<(int, int), (int, int)>();

        passed.Add((pac.X, pac.Y));
        que.Enqueue((0, pac.X, pac.Y));

        var dist = 0;
        var score = -1.0;
        (int X, int Y) tgt = (-1, -1);
        while (que.Count() != 0)
        {
            dist++;
            if (dist >= path_size && score > 0) break;
            if (dist >= max_path_size) break;

            var newQueue = new Queue<(double score, int x, int y)>();
            while (que.Count() != 0)
            {
                var cur = que.Dequeue();
                for (int d = 0; d < 4; d++)
                {
                    var nX = (cur.x + dir1[d] + Width) % Width; // side can go through
                    var nY = cur.y + dir2[d];
                    if (nY < 0 || nY >= Height) continue;
                    if (_map[nY, nX].IsWall) continue;
                    if (passed.Contains((nX, nY))) continue;
                    if (IsNextTo(_map[nY, nX], "PAC_ENEMY", false)) continue;  // avoid enemy
                    if (_map[nY, nX].IsPac) continue;       // avoid collision
                    // other pac is trying to move to this space
                    if (dist == 1 && nexts.Contains((nX, nY))) continue;

                    var newScore = cur.score + GetScore(_map, nX, nY, zeroScores, true, dist);

                    // if its first move and going back to previous, apply penalty
                    //if (dist == 1 && nX == prevPos.Item1 && nY == prevPos.Item2)
                    //    newScore -= 15;

                    passed.Add((nX, nY));
                    prev[(nX, nY)] = (cur.x, cur.y);

                    if (newScore > score)
                    {
                        score = newScore;
                        tgt = (nX, nY);
                    }
                    newQueue.Enqueue((newScore, nX, nY));
                }
            }
            que = newQueue;
        }

        // impossible
        if (tgt.X == -1) return null;

        // get path
        var path = new List<(int, int)>();
        var coord = (tgt.X, tgt.Y);
        while (prev.ContainsKey(coord))
        {
            path.Add(coord);
            coord = prev[coord];
        }
        path.Add(coord);
        path.Reverse();

        // score decreased
        // (path, score) = GetBestScoreAction_TakeSinglePellet(pac, zeroScores, nexts, path, score);

        var act = Action.GetMove(pac, path, score);
        nexts.Add((act.NextX, act.NextY));
        return act;
    }

    (List<(int, int)> path, double score) GetBestScoreAction_TakeSinglePellet(Pac pac, HashSet<(int, int)> zeroScores, HashSet<(int, int)> nexts, 
        List<(int, int)> path, double score)
    {
        for (int i = 0; i < path.Count(); i++)
        {
            var prevCell = i == 0 ? GetPrevPos(pac, _prevPacs) : path[i - 1];
            var pathCell = path[i];

            // find a pell with just one distance => take it!
            foreach (var n in GetAdjuscents(_map[pathCell.Item2, pathCell.Item1]))
            {
                if (n.Item1 == prevCell.Item1 && n.Item2 == prevCell.Item2) continue;
                if (zeroScores.Contains(n)) continue;
                if (nexts.Contains(n)) continue;
                if (!_map[n.Item2, n.Item1].IsPellet) continue;
                // close to enemy
                if (IsNextTo(_map[n.Item2, n.Item1], "PAC_ENEMY", true)) continue;

                var tgt = n.Item1 == 30 && n.Item2 == 3;

                var pelletsAround = GetAdjuscents(_map[n.Item2, n.Item1]).Where(x => x.Item1 != pathCell.Item1 || x.Item2 != pathCell.Item2)
                        .Where(x => _map[x.Item2, x.Item1].IsPellet);

                if (pelletsAround.Count() == 0)
                {
                    Util.Log("Pac " + pac.ID + " - found single pellet close by -> Just take it");
                    return FindPath(_map, Width, Height, pac, _map[n.Item2, n.Item1], true, zeroScores, true, false);                    
                }
            }
        }

        return (path, score); // no change
    }

    HashSet<(int, int)> GetZeroScores(List<Action> actions)
    {
        var ret = new HashSet<(int, int)>();
        foreach (var act in actions) AddToZeroScores(act, ret);        
        return ret;
    }

    void AddToZeroScores(Action act, HashSet<(int, int)> zeroScores)
    {
        if (!act.IsMove) return;
        zeroScores.Add((act.NextX, act.NextY));
        zeroScores.Add((act.TargetX, act.TargetY));
        foreach (var p in act.Path)
            zeroScores.Add((p.x, p.y));
    }

    static (List<(int x, int y)> path, double score) FindPath(CellItem[,] map, int width, int height,
        CellItem st, CellItem tgt, bool ignoreEnemy,
        HashSet<(int, int)> zeroScores, bool applyUncertainityForScore, bool ignoreEnemyIfWin)
    {
        var cmp = new ScoreTplComparer<(double score, int x, int y)>();
        var que = new PriorityQueue<(double score, int x, int y)>(cmp);   // score should come first

        var passed = new HashSet<(int x, int y)>();
        var prev = new Dictionary<(int, int), (int, int)>();

        passed.Add((st.X, st.Y));
        que.Push((0, st.X, st.Y));

        var dist = 0;
        var score = -1.0;
        var done = false;
        while (!done && que.Count() != 0)
        {
            dist++;
            var newQueue = new PriorityQueue<(double score, int x, int y)>(cmp);
            while (que.Count() != 0)
            {
                var cur = que.Pop();

                for (int d = 0; d < 4; d++)
                {
                    var nX = (cur.x + dir1[d] + width) % width; // side can go through
                    var nY = cur.y + dir2[d];
                    if (nY < 0 || nY >= height) continue;
                    if (map[nY, nX].IsWall) continue;
                    if (passed.Contains((nX, nY))) continue;
                    if (!ignoreEnemy && map[nY, nX].IsEnemyPac)
                    {
                        if(!ignoreEnemyIfWin) continue;       // avoid collision
                        if (!st.IsPac) continue;
                        var myPac = st as Pac;
                        if (!myPac.Mine) continue;
                        if (!myPac.Wins(map[nY, nX] as Pac)) continue;  // lose case
                    }
                    var newScore = cur.score + GetScore(map, nX, nY, zeroScores, applyUncertainityForScore, dist);

                    passed.Add((nX, nY));
                    prev[(nX, nY)] = (cur.x, cur.y);

                    // goal!
                    if (map[nY, nX] == tgt)
                    {
                        score = newScore;
                        done = true;
                        break;
                    }
                    // not goal
                    newQueue.Push((newScore, nX, nY));
                }
            }
            que = newQueue;
        }

        // impossible
        if (!done) return (null, -1);

        // get path
        var path = new List<(int, int)>();
        var coord = (tgt.X, tgt.Y);
        while(prev.ContainsKey(coord))
        {
            path.Add(coord);
            coord = prev[coord];
        }
        path.Add(coord);
        path.Reverse();

        return (path, score);
    }

    static double GetScore(CellItem[,] map, int x, int y, HashSet<(int, int)> zeroScores, bool applyUncertainityForScore, int additionalUnsercaionity)
    {
        if (zeroScores != null && zeroScores.Contains((x, y))) return 0;
        if (!map[y, x].IsPellet) return 0;
        var pellet = map[y, x] as Pellet;
        var ret = pellet.Value;
        if (!applyUncertainityForScore) return ret;
        if (pellet.UncertaintyLevel + additionalUnsercaionity <= 0) return ret;

        return ret * (Math.Pow(0.9, pellet.UncertaintyLevel + additionalUnsercaionity));
    }

    List<(int, int)> GetAdjuscents(CellItem item)
    {
        var ret = new List<(int, int)>();
        for (int d = 0; d < 4; d++)
        {
            var nX = (item.X + dir1[d] + Width) % Width; // pac can go through side edges
            var nY = item.Y + dir2[d];
            if (nY < 0 || nY >= Height) continue;
            if (_map[nY, nX].IsWall) continue;
            ret.Add((nX, nY));
        }
        return ret;
    }

    static int[] dir1 = new int[] { 1, -1, 0, 0 };
    static int[] dir2 = new int[] { 0, 0, 1, -1 };

    (int x, int y, int distance, int tgtX, int tgtY) FindClosest(CellItem item, string tgtType, HashSet<(int, int)> avoid, bool useAvoid, bool certainPellet,
        (int, int)[] prevPos, bool allowPrevPos)
    {
        var passed = new HashSet<(int, int)>();
        var que = new Queue<(int, int, int)>();
        que.Enqueue((item.X, item.Y, 0));
        passed.Add((item.X, item.Y));
        var prev = new Dictionary<(int, int), (int, int)>();

        while (que.Any())
        {
            var cur = que.Dequeue();
            if (cur.Item1 != item.X || cur.Item2 != item.Y) // not a start
            {
                if (useAvoid && avoid.Contains((cur.Item1, cur.Item2)))
                    continue;
                if (!allowPrevPos && prevPos.Any(p => p.Item1 == cur.Item1 && p.Item2 == cur.Item2))
                    continue;
                if (IsTarget(_map[cur.Item2, cur.Item1], tgtType, certainPellet))
                    return GetNextMove((item.X, item.Y), cur, prev);
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
                prev[(nX, nY)] = (cur.Item1, cur.Item2);
            }
        }

        return (-1, -1, -1, -1, -1);
    }

    (int x, int y, int distance, int tgtX, int tgtY) GetNextMove((int x, int y) st, (int x, int y, int distance) tgt, Dictionary<(int, int), (int, int)> prev)
    {
        var x = tgt.x;
        var y = tgt.y;
        while(true)
        {
            var p = prev[(x, y)];
            if (p.Item1 == st.x && p.Item2 == st.y) break;
            x = p.Item1;
            y = p.Item2;
        }
        return (x, y, tgt.distance, tgt.x, tgt.y);
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
            return IsCluster(item, certainPellet);
        if (tgtType == "PELLET_LARGE_CLUSTER")
            return IsCluster(item, certainPellet, 5, 6);

        if (tgtType == "CROSSING")
            return IsCrossing(item);

        if (tgtType == "CURVE")
            return IsCurve(item);

        return false;
    }

    // more than thres pellets within maxDist from the cell
    bool IsCluster(CellItem item, bool certainPellet, int maxDist=5, int thres=3)
    {
        if (!IsTarget(item, "PELLET", certainPellet)) return false;
        var cnt = 0;
        var que = new Queue<(int, int)>();
        var passed = new HashSet<(int, int)>();
        que.Enqueue((item.X, item.Y));
        passed.Add((item.X, item.Y));
        var dist = 0;
        while(que.Any())
        {
            var newQueue = new Queue<(int, int)>();
            while (que.Any())
            {
                var cur = que.Dequeue();
                var curItem = _map[cur.Item2, cur.Item1] as Pellet;
                cnt += curItem.Value;
                if (cnt >= thres) return true;

                for (int d = 0; d < 4; d++)
                {
                    var nX = (cur.Item1 + dir1[d] + Width) % Width; // side can go through
                    var nY = cur.Item2 + dir2[d];
                    if (nY < 0 || nY >= Height) continue;
                    if (!IsTarget(_map[nY, nX], "PELLET", certainPellet)) continue;
                    newQueue.Enqueue((nX, nY));
                    passed.Add((nX, nY));
                }

            }
            que = newQueue;
            dist++;
            if (dist == maxDist) break;
        }

        return false;
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

public class ScoreTplComparer<T> : IComparer<T>
{
    public int Compare(T x, T y)
    {
        var tx = (x as (double score, int x, int y)?).Value;
        var ty = (y as (double score, int x, int y)?).Value;
        return ty.score.CompareTo(tx.score);
    }
}

public class PriorityQueue<T> where T : IComparable
{
    private IComparer<T> _comparer = null;
    private int _type = 0;

    private T[] _heap;
    private int _sz = 0;

    private int _count = 0;

    /// <summary>
    /// Priority Queue with custom comparer
    /// </summary>
    public PriorityQueue(IComparer<T> comparer)
    {
        _heap = new T[128];
        _comparer = comparer;
    }

    /// <summary>
    /// Priority queue
    /// </summary>
    /// <param name="type">0: asc, 1:desc</param>
    public PriorityQueue(int type = 0)
    {
        _heap = new T[128];
        _type = type;
    }

    private int Compare(T x, T y)
    {
        if (_comparer != null) return _comparer.Compare(x, y);
        return _type == 0 ? x.CompareTo(y) : y.CompareTo(x);
    }

    public void Push(T x)
    {
        _count++;
        if (_count > _heap.Length)
        {
            var newheap = new T[_heap.Length * 2];
            for (int n = 0; n < _heap.Length; n++) newheap[n] = _heap[n];
            _heap = newheap;
        }

        //node number
        var i = _sz++;

        while (i > 0)
        {
            //parent node number
            var p = (i - 1) / 2;

            if (Compare(_heap[p], x) <= 0) break;

            _heap[i] = _heap[p];
            i = p;
        }

        _heap[i] = x;
    }

    public T Pop()
    {
        _count--;

        T ret = _heap[0];
        T x = _heap[--_sz];

        int i = 0;
        while (i * 2 + 1 < _sz)
        {
            //children
            int a = i * 2 + 1;
            int b = i * 2 + 2;

            if (b < _sz && Compare(_heap[b], _heap[a]) < 0) a = b;

            if (Compare(_heap[a], x) >= 0) break;

            _heap[i] = _heap[a];
            i = a;
        }

        _heap[i] = x;

        return ret;
    }

    public int Count()
    {
        return _count;
    }

    public T Peek()
    {
        return _heap[0];
    }

    public bool Contains(T x)
    {
        for (int i = 0; i < _sz; i++) if (x.Equals(_heap[i])) return true;
        return false;
    }

    public void Clear()
    {
        while (this.Count() > 0) this.Pop();
    }

    public IEnumerator<T> GetEnumerator()
    {
        var ret = new List<T>();

        while (this.Count() > 0)
        {
            ret.Add(this.Pop());
        }

        foreach (var r in ret)
        {
            this.Push(r);
            yield return r;
        }
    }

    public T[] ToArray()
    {
        T[] array = new T[_sz];
        int i = 0;

        foreach (var r in this)
        {
            array[i++] = r;
        }

        return array;
    }
}
namespace Program
{
    public class Map
    {
        private enum CellType { Empty, Dirt, Obstacle, Cleaned };
        private CellType[,] _grid;
        public int Width { get; private set; }
        public int Height { get; private set; }

        public Map(int width, int height)
        {
            Width = width;
            Height = height;
            _grid = new CellType[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _grid[x, y] = CellType.Empty;
        }

        public bool IsInBounds(int x, int y)
            => x >= 0 && x < Width && y >= 0 && y < Height;

        public bool IsDirt(int x, int y)
            => IsInBounds(x, y) && _grid[x, y] == CellType.Dirt;

        public bool IsObstacle(int x, int y)
            => IsInBounds(x, y) && _grid[x, y] == CellType.Obstacle;

        public void AddObstacle(int x, int y)
        {
            if (IsInBounds(x, y)) _grid[x, y] = CellType.Obstacle;
        }

        public void AddDirt(int x, int y)
        {
            if (IsInBounds(x, y)) _grid[x, y] = CellType.Dirt;
        }

        public void Clean(int x, int y)
        {
            if (IsInBounds(x, y) && _grid[x, y] == CellType.Dirt)
                _grid[x, y] = CellType.Cleaned;
        }

        public int CountRemainingDirt()
        {
            int count = 0;
            for (int x = 0; x < Width; x++)
                for (int y = 0; y < Height; y++)
                    if (_grid[x, y] == CellType.Dirt) count++;
            return count;
        }

        public void PlaceRandomObstacles(int count)
        {
            var rand = new Random();
            int placed = 0;
            int cells = Width * Height;
            count = Math.Min(count, cells);

            while (placed < count)
            {
                int x = rand.Next(Width);
                int y = rand.Next(Height);
                if (_grid[x, y] == CellType.Empty)
                {
                    _grid[x, y] = CellType.Obstacle;
                    placed++;
                }
            }
        }

        public void PlaceRandomDirt(int count)
        {
            var rand = new Random();
            int placed = 0;
            int cells = Width * Height;
            count = Math.Min(count, cells);

            while (placed < count)
            {
                int x = rand.Next(Width);
                int y = rand.Next(Height);
                if (_grid[x, y] == CellType.Empty)
                {
                    _grid[x, y] = CellType.Dirt;
                    placed++;
                }
            }
        }

        public void Display(int robotX, int robotY)
        {
            Console.Clear();
            Console.WriteLine("Vacuum Cleaner Robot Simulation");
            Console.WriteLine("------------------------------------");
            Console.WriteLine("Legends: #=Obstacle  D=Dirt  .=Empty  R=Robot  C=Cleaned");
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (x == robotX && y == robotY)
                        Console.Write("R ");
                    else
                    {
                        switch (_grid[x, y])
                        {
                            case CellType.Empty: Console.Write(". "); break;
                            case CellType.Dirt: Console.Write("D "); break;
                            case CellType.Obstacle: Console.Write("# "); break;
                            case CellType.Cleaned: Console.Write("C "); break;
                        }
                    }
                }
                Console.WriteLine();
            }
            Console.WriteLine();
            Thread.Sleep(50);
        }
    }

    public class Vacuum
    {
        private readonly Map _map;
        public int X { get; private set; }
        public int Y { get; private set; }

        private static readonly Dictionary<string, (int dx, int dy)> Directions = new()
        {
            ["E"] = (1, 0),
            ["W"] = (-1, 0),
            ["S"] = (0, 1),
            ["N"] = (0, -1),
            ["SE"] = (1, 1),
            ["NE"] = (1, -1),
            ["SW"] = (-1, 1),
            ["NW"] = (-1, -1)
        };

        public Vacuum(Map map, int startX = 0, int startY = 0)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
            X = startX;
            Y = startY;
            _map.Display(X, Y);
        }

        public Dictionary<string, (int X, int Y)> Moves()
        {
            var available = new Dictionary<string, (int X, int Y)>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in Directions)
            {
                string name = kv.Key;
                int nx = X + kv.Value.dx;
                int ny = Y + kv.Value.dy;
                if (_map.IsInBounds(nx, ny) && !_map.IsObstacle(nx, ny))
                    available[name] = (nx, ny);
            }
            return available;
        }

        public List<string> AllowedMoves()
            => Moves().Keys.ToList();

        public bool IsDirtAt(int x, int y) => _map.IsDirt(x, y);

        public bool Move(string directionName)
        {
            if (string.IsNullOrWhiteSpace(directionName)) return false;
            if (!Directions.TryGetValue(directionName.ToUpperInvariant(), out var delta)) return false;

            int nx = X + delta.dx;
            int ny = Y + delta.dy;
            if (!(_map.IsInBounds(nx, ny) && !_map.IsObstacle(nx, ny))) return false;

            X = nx;
            Y = ny;
            _map.Display(X, Y);

            CleanCurrentSpot();
            return true;
        }

        public void CleanCurrentSpot()
        {
            if (_map.IsDirt(X, Y))
            {
                _map.Clean(X, Y);
                _map.Display(X, Y);
            }
        }

        public List<(int X, int Y)> GetAllDirtLocations()
        {
            var list = new List<(int X, int Y)>();
            for (int x = 0; x < _map.Width; x++)
            {
                for (int y = 0; y < _map.Height; y++)
                {
                    if (_map.IsDirt(x, y))
                        list.Add((x, y));
                }
            }
            return list;
        }

        public int[,] ComputeDistanceMap()
        {
            int w = _map.Width;
            int h = _map.Height;
            var dist = new int[w, h];
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    dist[i, j] = int.MaxValue;

            var q = new Queue<(int x, int y)>();
            dist[X, Y] = 0;
            q.Enqueue((X, Y));

            (int dx, int dy)[] nbrs = new (int, int)[]
            {
            (1,0), (-1,0), (0,1), (0,-1), (1,1), (1,-1), (-1,1), (-1,-1)
            };

            while (q.Count > 0)
            {
                var (cx, cy) = q.Dequeue();
                int cd = dist[cx, cy];
                foreach (var (dx, dy) in nbrs)
                {
                    int nx = cx + dx;
                    int ny = cy + dy;
                    if (!_map.IsInBounds(nx, ny)) continue;
                    if (_map.IsObstacle(nx, ny)) continue;
                    if (dist[nx, ny] <= cd + 1) continue;
                    dist[nx, ny] = cd + 1;
                    q.Enqueue((nx, ny));
                }
            }

            return dist;
        }

        public int MapWidth => _map.Width;
        public int MapHeight => _map.Height;
        public bool IsInBoundsAt(int x, int y) => _map.IsInBounds(x, y);
        public bool IsObstacleAt(int x, int y) => _map.IsObstacle(x, y);
    }

    public abstract class Strategy
    {
        public abstract string ChooseMove(Vacuum robot);
        public bool Execute(Vacuum robot)
        {
            var dir = ChooseMove(robot) ?? "0";
            if (dir == "0" || string.IsNullOrWhiteSpace(dir)) return false;
            return robot.Move(dir);
        }
    }

    public class SStrategy : Strategy
    {
        private bool _goingRight = true;

        public override string ChooseMove(Vacuum robot)
        {
            var moves = robot.AllowedMoves();

            if (_goingRight && moves.Contains("E"))
                return "E";
            if (!_goingRight && moves.Contains("W"))
                return "W";

            if (moves.Contains("S"))
            {
                _goingRight = !_goingRight;
                return "S";
            }

            return "0";
        }
    }

    public class RandomStrategy : Strategy
    {
        private static readonly Random _rand = new();

        public override string ChooseMove(Vacuum robot)
        {
            var allowed = robot.AllowedMoves();
            if (allowed.Count == 0)
                return "0";

            return allowed[_rand.Next(allowed.Count)];
        }
    }

    public class WaterfallStrategy : Strategy
    {
        public override string ChooseMove(Vacuum robot)
        {
            var dirtList = robot.GetAllDirtLocations();
            if (dirtList.Count == 0)
                return "0";

            var dist = robot.ComputeDistanceMap();

            int bestDist = int.MaxValue;
            (int tx, int ty) bestTarget = (-1, -1);
            foreach (var (dx, dy) in dirtList)
            {
                int d = dist[dx, dy];
                if (d < bestDist)
                {
                    bestDist = d;
                    bestTarget = (dx, dy);
                }
            }

            if (bestDist == int.MaxValue)
                return "0";

            var allowed = robot.Moves();
            if (allowed.Count == 0) return "0";

            int w = robot.MapWidth;
            int h = robot.MapHeight;
            var distFromTarget = new int[w, h];
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                    distFromTarget[i, j] = int.MaxValue;

            var q = new Queue<(int x, int y)>();
            distFromTarget[bestTarget.tx, bestTarget.ty] = 0;
            q.Enqueue((bestTarget.tx, bestTarget.ty));

            (int dxn, int dyn)[] nbrs = new (int, int)[]
            {
            (1,0), (-1,0), (0,1), (0,-1), (1,1), (1,-1), (-1,1), (-1,-1)
            };

            while (q.Count > 0)
            {
                var (cx, cy) = q.Dequeue();
                int cd = distFromTarget[cx, cy];
                foreach (var (dxn2, dyn2) in nbrs)
                {
                    int nx = cx + dxn2;
                    int ny = cy + dyn2;
                    if (!robot.IsInBoundsAt(nx, ny)) continue;
                    if (robot.IsObstacleAt(nx, ny)) continue;
                    if (distFromTarget[nx, ny] <= cd + 1) continue;
                    distFromTarget[nx, ny] = cd + 1;
                    q.Enqueue((nx, ny));
                }
            }

            int bestNeighborDist = int.MaxValue;
            string bestMove = "0";
            foreach (var kv in allowed)
            {
                string dir = kv.Key;
                var (nx, ny) = kv.Value;
                int nd = distFromTarget[nx, ny];
                if (nd < bestNeighborDist)
                {
                    bestNeighborDist = nd;
                    bestMove = dir;
                }
            }

            if (bestMove == "0")
                return "0";

            return bestMove;
        }
    }

    class ProgramEntry
    {
        static void Main(string[] args)
        {
            var map = new Map(10, 10);
            map.PlaceRandomObstacles(15);
            map.PlaceRandomDirt(20);

            var robot = new Vacuum(map, 0, 0);

            Strategy strategy;
            while (true)
            {
                Console.Clear();
                Console.WriteLine("Choose strategy:");
                Console.WriteLine("  1) SStrategy");
                Console.WriteLine("  2) RandomStrategy");
                Console.WriteLine("  3) WaterfallStrategy");
                Console.Write(": ");
                var key = Console.ReadKey(true).KeyChar;
                switch (char.ToLower(key))
                {
                    case '1':
                        strategy = new SStrategy();
                        break;
                    case '2':
                        strategy = new RandomStrategy();
                        break;
                    case '3':
                        strategy = new WaterfallStrategy();
                        break;
                    default:
                        Console.WriteLine("Invalid selection. Press any key to try again...");
                        Console.ReadKey(true);
                        continue;
                }
                break;
            }

            Console.Clear();
            Console.WriteLine("Press 'q' during the run to quit early. Running strategy...");
            Thread.Sleep(800);

            const int MaxConsecutiveNoOps = 40;
            int consecutiveNoOps = 0;

            while (true)
            {
                if (Console.KeyAvailable)
                {
                    var k = Console.ReadKey(true);
                    if (k.Key == ConsoleKey.Q)
                    {
                        Console.WriteLine("Quitting by user request...");
                        break;
                    }

                    if (k.KeyChar == '1')
                    {
                        strategy = new SStrategy();
                        Console.WriteLine("Switched to SStrategy.");
                        Thread.Sleep(300);
                    }
                    else if (k.KeyChar == '2')
                    {
                        strategy = new RandomStrategy();
                        Console.WriteLine("Switched to RandomStrategy.");
                        Thread.Sleep(300);
                    }
                    else if (k.KeyChar == '3')
                    {
                        strategy = new WaterfallStrategy();
                        Console.WriteLine("Switched to WaterfallStrategy.");
                        Thread.Sleep(300);
                    }
                }

                Thread.Sleep(120);

                bool moved = strategy.Execute(robot);

                var possibleMoves = robot.Moves();
                if (possibleMoves.Count == 0)
                {
                    Console.WriteLine("No legal moves available. Stopping.");
                    break;
                }

                if (!moved)
                {
                    consecutiveNoOps++;
                    if (consecutiveNoOps >= MaxConsecutiveNoOps)
                    {
                        Console.WriteLine("Strategy did not move for too long. Stopping.");
                        break;
                    }
                }
                else
                {
                    consecutiveNoOps = 0;
                }

                if (map.CountRemainingDirt() == 0)
                {
                    Console.Clear();
                    map.Display(robot.X, robot.Y);
                    Console.WriteLine("All dirt cleaned!");
                    break;
                }
            }
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
        }
    }
}

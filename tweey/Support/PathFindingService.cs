namespace Tweey.Support;

class PathFindingService
{
    class Node
    {
        public Node(Node? parent, Vector2i position, byte weight) =>
            (Parent, Position, Weight, TotalCost) =
                (parent, position, weight, (ushort)((parent?.TotalCost ?? 0) + weight));

        public Node? Parent { get; set; }
        public Vector2i Position { get; }
        public byte Weight { get; set; }
        public ushort TotalCost { get; set; }
    }

    static public PathFindingResult Calculate(World world, Vector2i startPosition, Vector2i goalPosition)
    {
        var width = world.TerrainCells!.GetLength(0);
        var height = world.TerrainCells!.GetLength(1);
        var cells = new byte[height * width];

        for (int y = 0; y < height; ++y)
            for (int x = 0; x < width; ++x)
                cells[y * width + x] = world.TerrainCells![x, y].BuildingTemplate?.IsBlocking == true ? (byte)255
                    : (byte)(255 - Math.Round(Math.Min(world.TerrainCells![x, y].GroundMovementModifier, world.TerrainCells![x, y].AboveGroundMovementModifier) * 255));
        cells[goalPosition.X + goalPosition.Y * width] = 0;

        var open = new PriorityQueue<Node, float>();
        var openSet = new HashSet<Vector2i>();
        var closedSet = new HashSet<Vector2i>();
        Node? goalNode = null;

        float getDistance(Vector2i position) =>
            Math.Abs(position.X - goalPosition.X) + Math.Abs(position.Y - goalPosition.Y);

        open.Enqueue(new Node(null, startPosition, cells[startPosition.X + startPosition.Y * width]), getDistance(startPosition));
        openSet.Add(startPosition);

        while (open.Count != 0 && !closedSet.Contains(goalPosition))
        {
            var currentNode = open.Dequeue();
            openSet.Remove(currentNode.Position);
            closedSet.Add(currentNode.Position);

            void process(Vector2i delta)
            {
                var newPosition = currentNode.Position + delta;
                if (!closedSet.Contains(newPosition) && cells[newPosition.X + newPosition.Y * width] is { } weight && weight < byte.MaxValue && !openSet.Contains(newPosition))
                {
                    var newNode = new Node(currentNode, newPosition, weight);
                    open.Enqueue(newNode, getDistance(newPosition) + newNode.TotalCost / 255f);
                    openSet.Add(newPosition);

                    if (newNode.Position == goalPosition)
                        goalNode = newNode;
                }
            }

            if (currentNode.Position.X > 0) process(new(-1, 0));
            if (currentNode.Position.Y > 0) process(new(0, -1));
            if (currentNode.Position.X < width - 1) process(new(1, 0));
            if (currentNode.Position.Y < height - 1) process(new(0, 1));
        }

        if (!closedSet.Contains(goalPosition))
            return new() { IsComplete = true, IsValid = false };

        var result = new PathFindingResult { IsComplete = true, IsValid = true, Positions = new() };

        while (goalNode is not null)
        {
            result.Positions.Add(goalNode.Position);
            goalNode = goalNode.Parent;
        }
        result.Positions.Reverse();

        return result;
    }
}

class PathFindingResult
{
    public required bool IsComplete { get; init; }
    public required bool IsValid { get; init; }
    public List<Vector2i>? Positions { get; init; }
}

using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameWindow : Node2D
{
    public SnakeHead Head;
    public Vector2 Headposition;
    private Timer _timer;
    private List<Vector2> _foodPositions;
    private IEnumerable<SnakePiece> _snakeParts;
    private AStarGrid2D _astarGrid;

    public override void _Ready()
    {
        _timer = GetNode<Timer>("Timer");
        _timer.WaitTime = 0.3f;
        _timer.Start();
        _timer.Timeout += OnTimerTimeout;

        Head = GetNode<SnakeHead>("SnakeHead");
        Head.Collision += GameOver;

        _foodPositions = new List<Vector2>();
        SpawnFood();
        SpawnFood();
        SpawnFood();


        Head.MoveHead();
        _astarGrid = new AStarGrid2D();
        _astarGrid.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan;
        _astarGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
        _astarGrid.Size = new Vector2I(27, 16);
        _astarGrid.CellSize = new Vector2I(16, 16);
        _astarGrid.Update();
    }

    private void OnTimerTimeout()
    {
        if (isFoodEaten())
        {
            AddSnakePiece();
            SpawnFood();
        }
        Head.MoveHead();
        MoveSnakePiece();
        HasSnakeBittenItself();
        Headposition = Head.Position;
        var nextPostition = GetNextPosition(Headposition, FindShortestDistance(Headposition));
        var nextDirection  = (nextPostition - Headposition).Normalized();
        if (nextDirection == - Head.Direction) 
        {
            nextDirection = nextDirection.Orthogonal();
        }
        Head.ChangeDirection(nextDirection);
    }

    public Vector2 FindShortestDistance(Vector2 target)
    {
        if (_foodPositions.Count == 0)
        {
            throw new ArgumentException("The points list cannot be empty.");
        }

        float shortestDistance = target.DistanceTo(_foodPositions[0]);
        int smallestIndex = 0 ;
        for (int i = 1; i < _foodPositions.Count; i++)
        {
            float distance = target.DistanceTo(_foodPositions[i]);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                smallestIndex = i;
            }
        }

        return _foodPositions[smallestIndex];
    }

    public void SpawnFood()
    {
        var foodScene = GD.Load<PackedScene>("res://scenes/food.tscn");
        Vector2 ScreenSize = GetViewportRect().Size;
        var foodPiece = foodScene.Instantiate() as Food;
        GetNode<Node>("FoodHolder").AddChild(foodPiece);
        var rand = new Random();
        var foodPosition = new Vector2();
        foodPosition.X = rand.Next(2, 24);
        foodPosition.Y = rand.Next(2, 14);
        foodPosition = foodPosition * new Vector2(16,16);

        if (_snakeParts == null) 
        {
            foodPiece.Position = foodPosition;
            _foodPositions.Add(foodPosition);
            return;
        }
        
        int k = 0;
        while (k < 10000) 
        {
            foodPosition.X = rand.Next(2, 24);
            foodPosition.Y = rand.Next(2, 14);
            foodPosition = foodPosition * new Vector2(16,16);
            foodPiece.Position = foodPosition;
            var Foodbox = GetHitBox<Food>(foodPiece);
            foreach (var item in _snakeParts)
            {
                if (!Foodbox.Intersects(GetHitBox<SnakePiece>(item)))
                {
                    _foodPositions.Add(foodPosition);
                    return;
                }   
            }
            k ++;
        }
        
    }



    private void GameOver()
    {
        GetNode<Window>("GameOverWindow").Show();
        GetTree().Paused = true;
    }
    
    public void AddSnakePiece()
    {
        var snakePieceScene = GD.Load<PackedScene>("res://scenes/snake_piece.tscn");
        var snakePiece = snakePieceScene.Instantiate() as SnakePiece;
        
        var SnakePieces = GetNode<Node>("SnakePieces").GetChildren().OfType<SnakePiece>();
        if (SnakePieces.Count() == 0) {
            Vector2 offset = -Head.PerviousDirection * 16;
            var newPosition = Head.PerviousPosition + offset;
            snakePiece.Position = newPosition;
            snakePiece.Direction = Head.PerviousDirection;
        } else
        {
            Vector2 offset = -SnakePieces.ElementAt(SnakePieces.Count()-1).Direction * 16;
            snakePiece.Position = SnakePieces.ElementAt(SnakePieces.Count()-1).Position - offset; 
            snakePiece.Direction  = SnakePieces.ElementAt(SnakePieces.Count()-1).Direction;
        }
        GetNode<Node>("SnakePieces").AddChild(snakePiece);
    }

    public void MoveSnakePiece()
    {
        _snakeParts = GetNode<Node>("SnakePieces").GetChildren().OfType<SnakePiece>();
        var PerviousPosition = Head.PerviousPosition - Head.PerviousDirection; 
        var PerviousDirection = Head.PerviousDirection;
        var TempPosition = Head.Position;
        var TempDirection = Head.Direction;
        
        for(int index = 0; index < _snakeParts.Count(); index++) 
        {
            TempPosition = _snakeParts.ElementAt(index).Position;
            _snakeParts.ElementAt(index).Position = PerviousPosition;
            PerviousPosition = TempPosition;

            TempDirection = _snakeParts.ElementAt(index).Direction;
            _snakeParts.ElementAt(index).Direction = PerviousDirection;
            PerviousDirection = TempDirection;
        }
    }

    private bool isFoodEaten()
    {
        var foodPieces = GetNode<Node>("FoodHolder").GetChildren().OfType<Food>();
        Rect2 Headbox = GetHitBox<SnakeHead>(Head);
        for(int index = 0 ; index < foodPieces.Count(); index++)
        {
            Rect2 Foodbox = GetHitBox<Food>(foodPieces.ElementAt(index));
            if (Headbox.Intersects(Foodbox) )
            {
                _foodPositions.Remove(foodPieces.ElementAt(index).Position);
                GetNode<Node>("FoodHolder").GetChild(index).QueueFree();
                return true;
            }
        }
        return false;
    }

    private bool HasSnakeBittenItself() 
    {
        var Headbox = GetHitBox<SnakeHead>(Head);

        for(int index = 0; index < _snakeParts.Count(); index++) 
        {
            Rect2 SnakePiecebox = GetHitBox<SnakePiece>(_snakeParts.ElementAt(index));
            if (Headbox.Intersects(SnakePiecebox))
            {
                GameOver();
                return true;
            }
        }
        return false;
    }

    private Rect2 GetHitBox<T>(T Target) where T:  Node2D
    {
        var Hitbox = new Rect2();
        Hitbox.Position = Target.Position + new Vector2(8,8); 
        Hitbox.Size = new Vector2(16,16); 
        return Hitbox;
    }


    private Vector2 GetNextPosition( Vector2 beginning, Vector2  end )
    {
        _astarGrid.Clear();
        _astarGrid.Size = new Vector2I(27, 16);
        _astarGrid.CellSize = new Vector2I(16, 16);
        _astarGrid.Update();
        if (_snakeParts != null)
        {
            foreach (var item in _snakeParts)
            {
                var point  = (Vector2I) (item.Position/new Vector2(16, 16)).Round();
              //  GD.Print("point:" +point);
                _astarGrid.SetPointSolid(point, true);
            }
        }
        var path = _astarGrid.GetPointPath((Vector2I) beginning/new Vector2I(16, 16), (Vector2I) end/new Vector2I(16, 16));
        return (Vector2) path[1];
    }
}

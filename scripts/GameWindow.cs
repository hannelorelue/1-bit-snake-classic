using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameWindow : Node2D
{
    public SnakeHead Head;
    public Vector2 Headposition;
    private Timer timer;
    private List<Vector2> FoodPositions;
    private IEnumerable<SnakePiece> SnakePositions;
    private AStarGrid2D astarGrid;

    public override void _Ready()
    {
        timer = GetNode<Timer>("Timer");
        timer.WaitTime = 0.3f;
        timer.Start();
        timer.Timeout += OnTimerTimeout;

        Head = GetNode<SnakeHead>("SnakeHead");
        Head.Collision += GameOver;

        FoodPositions = new List<Vector2>();
        SpawnFood();
        SpawnFood();
        SpawnFood();


        Head.MoveHead();
        astarGrid = new AStarGrid2D();
        astarGrid.DefaultComputeHeuristic = AStarGrid2D.Heuristic.Manhattan;
        astarGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never;
        astarGrid.Size = new Vector2I(27, 16);
        astarGrid.CellSize = new Vector2I(16, 16);
        astarGrid.Update();
    }

    public override void _PhysicsProcess(double delta)
    { 
        if (isFoodEaten())
        {
            AddSnakePiece();
            SpawnFood();
        }
    }

    private void OnTimerTimeout()
    {
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
        if (FoodPositions.Count == 0)
        {
            throw new ArgumentException("The points list cannot be empty.");
        }

        float shortestDistance = target.DistanceTo(FoodPositions[0]);
        int smallestIndex = 0 ;
        for (int i = 1; i < FoodPositions.Count; i++)
        {
            float distance = target.DistanceTo(FoodPositions[i]);

            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                smallestIndex = i;
            }
        }

        return FoodPositions[smallestIndex];
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

        if (SnakePositions == null) 
        {
            foodPiece.Position = foodPosition;
            FoodPositions.Add(foodPosition);
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
            foreach (var item in SnakePositions)
            {
                if (!Foodbox.Intersects(GetHitBox<SnakePiece>(item)))
                {
                    FoodPositions.Add(foodPosition);
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
           // var newPosition = Head.PerviousPosition + Head.PerviousDirection * (-1) * new Vector2(16,16); 
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
        SnakePositions = GetNode<Node>("SnakePieces").GetChildren().OfType<SnakePiece>();
        var PerviousPosition = Head.PerviousPosition - Head.PerviousDirection; 
        var PerviousDirection = Head.PerviousDirection;
        var TempPosition = Head.Position;
        var TempDirection = Head.Direction;
        
        for(int index = 0; index < SnakePositions.Count(); index++) 
        {
            TempPosition = SnakePositions.ElementAt(index).Position;
            SnakePositions.ElementAt(index).Position = PerviousPosition;
            PerviousPosition = TempPosition;

            TempDirection = SnakePositions.ElementAt(index).Direction;
            SnakePositions.ElementAt(index).Direction = PerviousDirection;
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
                FoodPositions.Remove(foodPieces.ElementAt(index).Position);
                GetNode<Node>("FoodHolder").GetChild(index).QueueFree();
                return true;
            }
        }
        return false;
    }

    private bool HasSnakeBittenItself() 
    {
        var Headbox = GetHitBox<SnakeHead>(Head);

        for(int index = 0; index < SnakePositions.Count(); index++) 
        {
            Rect2 SnakePiecebox = GetHitBox<SnakePiece>(SnakePositions.ElementAt(index));
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
        astarGrid.Clear();
        astarGrid.Size = new Vector2I(27, 16);
        astarGrid.CellSize = new Vector2I(16, 16);
        astarGrid.Update();
        if (SnakePositions != null)
        {
            foreach (var item in SnakePositions)
            {
                var point  = (Vector2I) (item.Position/new Vector2(16, 16)).Round();
              //  GD.Print("point:" +point);
                astarGrid.SetPointSolid(point, true);
            }
        }
        var path = astarGrid.GetPointPath((Vector2I) beginning/new Vector2I(16, 16), (Vector2I) end/new Vector2I(16, 16));
        return (Vector2) path[1];
    }
}

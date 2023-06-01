using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class GameWindow : Node2D
{
    public SnakeHead Head;
    public Vector2 Headposition;
    private Timer timer;

    public override void _Ready()
    {
        timer = GetNode<Timer>("Timer");
        timer.WaitTime = 0.3f;
        timer.Start();
        timer.Timeout += OnTimerTimeout;
        Head = GetNode<SnakeHead>("SnakeHead");
        Head.Collision += GameOver;
        SpawnFood();
        SpawnFood();
        SpawnFood();
        Head.MoveHead();
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
        foodPiece.Position = foodPosition * new Vector2(16,16);
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
            var newPosition = Head.PerviousPosition + Head.PerviousDirection * (-1) * new Vector2(16,16); 
            snakePiece.Position = newPosition;
            snakePiece.Direction = Head.PerviousDirection;
        } else
        {
            snakePiece.Position = SnakePieces.ElementAt(SnakePieces.Count()-1).Position + SnakePieces.ElementAt(SnakePieces.Count()-1).Direction * (-1) * new Vector2(16,16); 
            snakePiece.Direction  = SnakePieces.ElementAt(SnakePieces.Count()-1).Direction;
        }
        GetNode<Node>("SnakePieces").AddChild(snakePiece);
    }

    public void MoveSnakePiece()
    {
        var SnakePieces = GetNode<Node>("SnakePieces").GetChildren().OfType<SnakePiece>();
        var PerviousPosition = Head.PerviousPosition + Head.PerviousDirection * (-1); 
        var PerviousDirection = Head.PerviousDirection;
        var TempPosition = Head.Position;
        var TempDirection = Head.Direction;
        
        for(int index = 0; index < SnakePieces.Count(); index++) 
        {
            TempPosition = SnakePieces.ElementAt(index).Position;
            SnakePieces.ElementAt(index).Position = PerviousPosition;
            PerviousPosition = TempPosition;
        }

    }

    private bool isFoodEaten()
    {
        var foodPieces = GetNode<Node>("FoodHolder").GetChildren().OfType<Food>();
        var Headbox = GetHitBox<SnakeHead>(Head);
        for(int index = 0 ; index < foodPieces.Count(); index++)
        {
            var Foodbox = GetHitBox<Food>(foodPieces.ElementAt(index));
            if (Headbox.Intersects(Foodbox) )
            {
                GetNode<Node>("FoodHolder").GetChild(index).QueueFree();
                return true;
            }
        }
        return false;
    }

    private bool HasSnakeBittenItself() 
    {
        var SnakePieces = GetNode<Node>("SnakePieces").GetChildren().OfType<SnakePiece>();
        var Headbox = GetHitBox<SnakeHead>(Head);

        for(int index = 0; index < SnakePieces.Count(); index++) 
        {
            var SnakePiecebox = GetHitBox<SnakePiece>(SnakePieces.ElementAt(index));
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
}

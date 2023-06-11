using Godot;
using System;

public partial class SnakeHead : Node2D
{

    [Export]
    public int Speed { get; set; } = 1; 

    [Signal]
    public delegate void CollisionEventHandler();

    public Vector2 ScreenSize;
    public Vector2 Direction = Vector2.Right; 
    public Vector2 PerviousPosition; 
    public Vector2 PerviousDirection; 
    public Sprite2D Sprite;
    public Texture2D RightSprite;
    public Texture2D DownSprite;

    public override void _Ready()
    {
        RightSprite = GD.Load("res://assets/snake_head_right.png") as Texture2D;
        DownSprite = GD.Load("res://assets/snake_head_down.png") as Texture2D;;
        ScreenSize = GetViewportRect().Size;
        Sprite = GetNode<Sprite2D>("Sprite2D");
        Position = new Vector2(160,160);
    }


    public override void _PhysicsProcess(double delta)
    {
        PerviousDirection = Direction;
        PerviousPosition = Position;
        if (Input.IsActionPressed("move_right"))
        {
            ChangeDirection(Vector2.Right);
        }
        if (Input.IsActionPressed("move_left"))
        {
            ChangeDirection(Vector2.Left);
        }
        if (Input.IsActionPressed("move_down"))
        {
            ChangeDirection(Vector2.Down);
        }
        if (Input.IsActionPressed("move_up"))
        {
            ChangeDirection(Vector2.Up);
        }
    }

    public void MoveHead()
    {
        Position += Direction * new Vector2(16,16) * Speed;
        Position = new Vector2(
            x: Mathf.Clamp(Position.X, 0, ScreenSize.X),
            y: Mathf.Clamp(Position.Y, 0, ScreenSize.Y)
        );
        if (Position.X == 0 || Position.X == ScreenSize.X || Position.Y == 0 || Position.Y == ScreenSize.Y )
        {
            EmitSignal(SignalName.Collision);
        }
    }

    public void ChangeDirection(Vector2 input)
    {

        if (input == Vector2.Right && Direction != Vector2.Left)
        {
            Direction = Vector2.Right;
            Sprite.Texture = RightSprite;
            Sprite.FlipH = false;
        }
        if (input == Vector2.Left &&  Direction != Vector2.Right)
        {
            Direction = Vector2.Left;
            Sprite.Texture = RightSprite;
            Sprite.FlipH = true;
        }
        if (input == Vector2.Down &&  Direction != Vector2.Up)
        {
            Direction = Vector2.Down;
            Sprite.Texture = DownSprite;
            Sprite.FlipV = false;
        }
        if (input == Vector2.Up && Direction != Vector2.Down )
        {
            Direction = Vector2.Up;
            Sprite.Texture = DownSprite;
            Sprite.FlipV = true;
        }

    }
}

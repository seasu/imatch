namespace Game.Core
{
    public enum TileColor { Red, Blue, Green, Yellow, Purple, Orange }

    public enum TileKind { Normal, Rocket, Bomb, Disco, Obstacle, Empty }

    public enum ObstacleKind { Crate, Stone }

    public enum OverlayKind { Ice }

    public enum RocketOrientation { Horizontal, Vertical }

    public enum GameState { Idle, Running, Won, Lost }

    public enum LevelType { Normal, BossAttack }

    public enum NodeType { BuildNode, PuzzleGate }

    public enum NodeState { Locked, Available, Completed }

    public enum SwapResult { Accepted, Rejected }

    public enum LevelResult { Win, Lose }

    public enum GoalKind { CollectColor, ClearTiles, DestroyObstacles, Score }

    public enum BoosterType { Hammer, StartRocket, StartBomb }
}

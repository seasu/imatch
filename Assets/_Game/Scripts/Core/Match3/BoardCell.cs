namespace Game.Core
{
    public class BoardCell
    {
        public TileKind Kind = TileKind.Empty;
        public TileColor Color;
        public ObstacleKind ObstacleKind;
        public int ObstacleHp;
        public OverlayKind? Overlay;
        public int OverlayHp;
        public RocketOrientation RocketOrientation;

        public bool IsEmpty => Kind == TileKind.Empty;
        public bool IsObstacle => Kind == TileKind.Obstacle;
        public bool IsSwappable => !IsEmpty && !IsObstacle;
        public bool HasOverlay => Overlay.HasValue && OverlayHp > 0;

        public void Clear()
        {
            Kind = TileKind.Empty;
            Overlay = null;
            OverlayHp = 0;
        }

        public void SetNormal(TileColor color)
        {
            Kind = TileKind.Normal;
            Color = color;
        }

        public void SetSpecial(TileKind kind, TileColor color = default,
                               RocketOrientation orientation = default)
        {
            Kind = kind;
            Color = color;
            RocketOrientation = orientation;
        }

        public void SetObstacle(ObstacleKind kind, int hp)
        {
            Kind = TileKind.Obstacle;
            ObstacleKind = kind;
            ObstacleHp = hp;
        }

        public BoardCell Clone()
        {
            return new BoardCell
            {
                Kind = Kind, Color = Color, ObstacleKind = ObstacleKind,
                ObstacleHp = ObstacleHp, Overlay = Overlay, OverlayHp = OverlayHp,
                RocketOrientation = RocketOrientation
            };
        }
    }
}

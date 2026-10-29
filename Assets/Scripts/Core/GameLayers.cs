namespace Parcial2.Core
{
    /// <summary>Layer indices must match ProjectSettings/TagManager.asset.</summary>
    public static class GameLayers
    {
        public const int Obstacle = 8;
        public const int Boid = 9;
        public const int Hunter = 10;
        public const int Wolf = 11;
        public const int Interest = 12;

        public const int ObstacleMask = 1 << Obstacle;
        public const int BoidMask = 1 << Boid;
        public const int HunterMask = 1 << Hunter;
        public const int WolfMask = 1 << Wolf;
        public const int InterestMask = 1 << Interest;
    }
}

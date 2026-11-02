namespace Parcial2.Wolves
{
    public sealed class PatrolCursor
    {
        private readonly PatrolMode mode;
        private readonly int count;
        private int direction = 1;

        public PatrolCursor(PatrolMode mode, int count)
        {
            this.mode = mode;
            this.count = count;
        }

        public int Index { get; private set; }

        public void Reset(int index)
        {
            Index = count == 0 ? 0 : ((index % count) + count) % count;
            direction = 1;
        }

        public int Advance()
        {
            if (count <= 1)
                return Index;

            if (mode == PatrolMode.Loop)
            {
                Index = (Index + 1) % count;
                return Index;
            }

            int next = Index + direction;
            if (next < 0 || next >= count)
            {
                direction = -direction;
                next = Index + direction;
            }

            Index = next;
            return Index;
        }
    }
}

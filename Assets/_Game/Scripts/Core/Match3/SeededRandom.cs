namespace Game.Core
{
    /// Xorshift64 PRNG – deterministic, no Unity dependency.
    public class SeededRandom
    {
        private ulong _state;

        public SeededRandom(ulong seed)
        {
            _state = seed == 0 ? 1UL : seed;
        }

        public ulong NextULong()
        {
            _state ^= _state << 13;
            _state ^= _state >> 7;
            _state ^= _state << 17;
            return _state;
        }

        /// Returns [0, max)
        public int NextInt(int max)
        {
            if (max <= 0) return 0;
            return (int)(NextULong() % (ulong)max);
        }

        /// Weighted selection – returns index into weights array.
        public int WeightedSelect(int[] weights)
        {
            int total = 0;
            foreach (var w in weights) total += w;
            if (total == 0) return NextInt(weights.Length);

            int roll = (int)(NextULong() % (ulong)total);
            int cumulative = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                cumulative += weights[i];
                if (roll < cumulative) return i;
            }
            return weights.Length - 1;
        }
    }
}

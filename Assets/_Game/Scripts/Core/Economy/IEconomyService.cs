namespace Game.Core
{
    public interface IEconomyService
    {
        int Coins { get; }
        int Gems { get; }
        int Lives { get; }
        int MaxLives { get; }
        int GetBoosterCount(BoosterType booster);

        bool TrySpendCoins(int amount);
        bool TrySpendGems(int amount);
        void AddCoins(int amount);
        void AddGems(int amount);
        bool TrySpendLife();
        void AddLife(int amount);
        bool TrySpendBooster(BoosterType booster);
        void AddBooster(BoosterType booster, int amount);
        bool TryBuyBooster(BoosterType booster); // spend coins

        void Initialize(EconomyConfig config, EconomySaveData save);
        EconomySaveData GetSaveData();
    }
}

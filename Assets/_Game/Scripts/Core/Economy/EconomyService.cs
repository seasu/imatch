using System.Collections.Generic;

namespace Game.Core
{
    public class EconomyService : IEconomyService
    {
        private EconomyConfig _config;
        private EconomySaveData _save;

        public int Coins => _save.Coins;
        public int Gems => _save.Gems;
        public int Lives => _save.Lives;
        public int MaxLives => _config.MaxLives;

        public void Initialize(EconomyConfig config, EconomySaveData save)
        {
            _config = config;
            _save = save;
        }

        public int GetBoosterCount(BoosterType booster)
        {
            _save.BoostersInventory.TryGetValue(booster.ToString(), out var count);
            return count;
        }

        public bool TrySpendCoins(int amount)
        {
            if (_save.Coins < amount) return false;
            _save.Coins -= amount;
            return true;
        }

        public bool TrySpendGems(int amount)
        {
            if (_save.Gems < amount) return false;
            _save.Gems -= amount;
            return true;
        }

        public void AddCoins(int amount) => _save.Coins += amount;
        public void AddGems(int amount) => _save.Gems += amount;

        public bool TrySpendLife()
        {
            if (_save.Lives <= 0) return false;
            _save.Lives--;
            return true;
        }

        public void AddLife(int amount) =>
            _save.Lives = System.Math.Min(_save.Lives + amount, _config.MaxLives);

        public bool TrySpendBooster(BoosterType booster)
        {
            var key = booster.ToString();
            _save.BoostersInventory.TryGetValue(key, out var count);
            if (count <= 0) return false;
            _save.BoostersInventory[key] = count - 1;
            return true;
        }

        public void AddBooster(BoosterType booster, int amount)
        {
            var key = booster.ToString();
            _save.BoostersInventory.TryGetValue(key, out var count);
            _save.BoostersInventory[key] = count + amount;
        }

        public bool TryBuyBooster(BoosterType booster)
        {
            if (!_config.Boosters.TryGetValue(booster.ToString(), out var entry)) return false;
            if (!TrySpendCoins(entry.CoinPrice)) return false;
            AddBooster(booster, 1);
            return true;
        }

        public EconomySaveData GetSaveData() => _save;
    }
}

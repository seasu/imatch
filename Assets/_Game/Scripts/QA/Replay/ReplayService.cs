using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Game.QA
{
    using Game.Core;

    public class ReplayService : IReplayService
    {
        private ReplayRecord _current;
        private ReplayRecord _lastCompleted;

        public ReplayRecord StartRecording(string levelId, uint seed, int attemptIndex)
        {
            _current = new ReplayRecord
            {
                LevelId = levelId,
                Seed = seed,
                AttemptIndex = attemptIndex
            };
            return _current;
        }

        public void RecordAction(ReplayAction action)
        {
            if (_current == null) return;
            action.T = _current.Actions.Count;
            _current.Actions.Add(action);
        }

        public ReplayRecord StopRecording()
        {
            _lastCompleted = _current;
            _current = null;
            return _lastCompleted;
        }

        public async Task<ReplayRecord> LoadReplayAsync(string replayId)
        {
            var address = $"qa:replay:{replayId}";
            var handle = Addressables.LoadAssetAsync<TextAsset>(address);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
                throw new Exception($"[ReplayService] Failed to load replay: {address}");

            var json = handle.Result.text;
            Addressables.Release(handle);

            return JsonUtility.FromJson<ReplayRecord>(json);
        }

        public ReplayRunResult RunReplay(IMatch3Game game, ReplayRecord replay, LevelConfig level)
        {
            game.Initialize(level, replay.Seed);

            foreach (var action in replay.Actions)
            {
                switch (action.Type)
                {
                    case "swap":
                        var a = new Vector2Int(action.A[0], action.A[1]);
                        var b = new Vector2Int(action.B[0], action.B[1]);
                        var result = game.TrySwap(a, b);
                        if (result != SwapResult.Accepted)
                        {
                            return new ReplayRunResult
                            {
                                Passed = false,
                                FailReason = $"Swap rejected at action t={action.T}: {a}->{b}"
                            };
                        }
                        break;

                    case "hammer":
                        var cell = new Vector2Int(action.Cell[0], action.Cell[1]);
                        if (!game.UseBooster(BoosterType.Hammer, cell))
                        {
                            return new ReplayRunResult
                            {
                                Passed = false,
                                FailReason = $"Hammer rejected at action t={action.T}: cell={cell}"
                            };
                        }
                        break;

                    default:
                        return new ReplayRunResult
                        {
                            Passed = false,
                            FailReason = $"Unknown action type: {action.Type}"
                        };
                }
            }

            var endResult = game.GetEndResult();
            if (replay.Expected == null)
            {
                return new ReplayRunResult
                {
                    Passed = true,
                    ActualResult = endResult
                };
            }

            // Validate expectations
            if (replay.Expected.Result != "incomplete" && endResult != null)
            {
                bool expectWin = replay.Expected.Result == "win";
                bool actualWin = endResult.Result == LevelResult.Win;
                if (expectWin != actualWin)
                {
                    return new ReplayRunResult
                    {
                        Passed = false,
                        FailReason = $"Expected result={replay.Expected.Result}, got={endResult.Result}",
                        ActualResult = endResult
                    };
                }

                if (replay.Expected.MovesUsed > 0 && endResult.MovesUsed != replay.Expected.MovesUsed)
                {
                    return new ReplayRunResult
                    {
                        Passed = false,
                        FailReason = $"Expected movesUsed={replay.Expected.MovesUsed}, got={endResult.MovesUsed}",
                        ActualResult = endResult
                    };
                }
            }

            return new ReplayRunResult { Passed = true, ActualResult = endResult };
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using AncientFactory.Core.Data;
using AncientFactory.Core.Types;

namespace AncientFactory.Features.Tiles
{
    public class SettlementTile : BaseTile
    {
        public List<ItemStack> Demands { get; }
        public BlueprintDefinition UnlockedBlueprint { get; }

        public int Population { get; set; } = 10;
        public int Level { get; set; } = 1;
        public int Experience { get; set; }

        public bool IsSatisfied
        {
            get
            {
                foreach (var demand in Demands)
                {
                    if (!demand.IsValid) continue;
                    if (Inventory.Get(demand.Item) < demand.Amount)
                        return false;
                }
                return true;
            }
        }

        public SettlementTile(Vector3Int cellPosition, List<ItemStack> demands = null, BlueprintDefinition unlockedBlueprint = null)
            : base(cellPosition, TileType.Settlement)
        {
            Demands = demands ?? new List<ItemStack>();
            UnlockedBlueprint = unlockedBlueprint;
        }

        public int GetDemandProgress(ItemDefinition item)
        {
            var demand = Demands.Find(d => d.Item == item);
            if (!demand.IsValid) return 0;
            return Inventory.Get(item);
        }

        public int GetDemandTotal(ItemDefinition item)
        {
            var demand = Demands.Find(d => d.Item == item);
            return demand.IsValid ? demand.Amount : 0;
        }
    }
}

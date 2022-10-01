using Helpers;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace Manhunters
{
    class ManhunterPartyVisitSettlementToBuyFood : CampaignBehaviorBase
    {
        public override void RegisterEvents()
        {
            CampaignEvents.AiHourlyTickEvent.AddNonSerializedListener(this, AiHourlyTick);
        }

        public override void SyncData(IDataStore dataStore)
        {

        }

        public void AiHourlyTick(MobileParty mobileParty, PartyThinkParams p)
        {
            if (mobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                ManhuntersCampaignBehavior manhuntersCampaign = Campaign.Current.GetCampaignBehavior<ManhuntersCampaignBehavior>();

                if (mobileParty.GetNumDaysForFoodToLast() <= 1 
                    && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners
                    && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.BuyingFood
                    )
                {
                    GoToVillageToBuyFood(mobileParty, p, manhunterPartyComponent);
                }
            }
        }

        private void GoToVillageToBuyFood(MobileParty manhunterParty, PartyThinkParams p, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement nearestSettlement = SettlementHelper.FindNearestSettlement(settlement => (settlement.IsTown || settlement.IsVillage)
                && DoesSettlementHaveFood(settlement)
                && DoesPartyHaveGoldForFood(manhunterParty, settlement)
                );
            if (nearestSettlement != null)
            {
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood;
                AIBehaviorTuple key = new AIBehaviorTuple(nearestSettlement, AiBehavior.GoToSettlement);
                p.Reset(manhunterParty);
                if (p.AIBehaviorScores.ContainsKey(key))
                {
                    p.AIBehaviorScores[key] = 20f;
                }
                else
                {
                    p.AIBehaviorScores.Add(key, 30f);
                }
            }
            else
            {
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
            }
        }

        private bool DoesSettlementHaveFood(Settlement settlement)
        {
            bool hasFood = false;
            foreach (ItemRosterElement item in settlement.ItemRoster.ToList())
            {
                if (item.EquipmentElement.Item.IsFood)
                {
                    hasFood = true;
                }
            }
            return hasFood;
        }

        private bool DoesPartyHaveGoldForFood(MobileParty party, Settlement settlement)
        {
            var afforableFoods = settlement.ItemRoster.Where(item => item.EquipmentElement.Item.IsFood
                && (party.PartyTradeGold > settlement.Village?.GetItemPrice(item.EquipmentElement.Item)
                || party.PartyTradeGold > settlement.Town?.GetItemPrice(item.EquipmentElement.Item)));
            if (afforableFoods.Any())
            {
                return true;
            }
            return false;
        }

    }
}

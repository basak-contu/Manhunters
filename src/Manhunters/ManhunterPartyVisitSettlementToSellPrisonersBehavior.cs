using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace Manhunters
{
    class ManhunterPartyVisitSettlementToSellPrisonersBehavior : CampaignBehaviorBase
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
                if(mobileParty.PrisonRoster.TotalManCount > 0
                    && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood
                    && !manhuntersCampaign.ManhunterPartiesAndBetrayedLeader.ContainsValue(mobileParty))
                {
                    GoToTownToSellPrisoners(mobileParty, p, manhunterPartyComponent);
                }
            }
        }

        public void GoToTownToSellPrisoners(MobileParty manhunterParty, PartyThinkParams p, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement nearestTown = SettlementHelper.FindNearestTown(settlement => true);
            AIBehaviorTuple key = new AIBehaviorTuple(nearestTown, AiBehavior.GoToSettlement);
            p.Reset(manhunterParty);
            if (p.AIBehaviorScores.ContainsKey(key))
            {
                p.AIBehaviorScores[key] = 20f;
            }
            else
            {
                p.AIBehaviorScores.Add(key, 20f);
            }
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners;
        }
    }
}

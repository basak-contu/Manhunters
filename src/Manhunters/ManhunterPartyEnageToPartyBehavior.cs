using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace Manhunters
{
    class ManhunterPartyEnageToPartyBehavior : CampaignBehaviorBase
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
                ManhuntersCampaignBehavior manhuntersCampaignBehavior = Campaign.Current.GetCampaignBehavior<ManhuntersCampaignBehavior>();
                
                if(!manhuntersCampaignBehavior.ManhunterPartiesAndBetrayedLeader.ContainsValue(mobileParty)
                    && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood
                    && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.BuyingFood
                    && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners
                    )
                {
                    EngageToBanditPartyBehavior(mobileParty, p, manhunterPartyComponent, manhuntersCampaignBehavior);
                }

                if(manhuntersCampaignBehavior.ManhunterPartiesAndBetrayedLeader.ContainsValue(mobileParty)
                    && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood)
                {
                    EngageToPlayerParty(mobileParty, p, manhunterPartyComponent);
                } 
            }
        }
        
        private void EngageToPlayerParty(MobileParty manhunterParty, PartyThinkParams p, ManhunterPartyComponent manhunterPartyComponent)
        {
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToPlayer;
            MobileParty playerParty = MobileParty.MainParty;
            FactionManager.DeclareWar(Clan.PlayerClan, manhunterParty.ActualClan, true);
            if (playerParty.CurrentSettlement != null)
            { 
                AiBehavior aiBehavior = AiBehavior.PatrolAroundPoint;
                AIBehaviorTuple key = new AIBehaviorTuple(playerParty.CurrentSettlement, aiBehavior, false);
                p.Reset(manhunterParty);
                if (p.AIBehaviorScores.ContainsKey(key))
                {
                    p.AIBehaviorScores[key] = 20f;
                }
                else
                {
                    p.AIBehaviorScores.Add(key, 20f);
                }
            }
            else
            {
                AiBehavior aiBehavior = AiBehavior.GoAroundParty;
                AIBehaviorTuple key = new AIBehaviorTuple(playerParty, aiBehavior, false);
                p.Reset(manhunterParty);
                if (p.AIBehaviorScores.ContainsKey(key))
                {
                    p.AIBehaviorScores[key] = 20f;
                }
                else
                {
                    p.AIBehaviorScores.Add(key, 20f);
                }
            }
        }
        
        public void EngageToBanditPartyBehavior(MobileParty manhunterParty, PartyThinkParams p,
            ManhunterPartyComponent manhunterPartyComponent,ManhuntersCampaignBehavior manhuntersCampaignBehavior)
        {
            MobileParty targetBanditParty = manhuntersCampaignBehavior.FindNearestBanditParty(manhunterParty);
            if (targetBanditParty != null)
            {
                AiBehavior aiBehavior = AiBehavior.GoAroundParty;
                AIBehaviorTuple key = new AIBehaviorTuple(targetBanditParty, aiBehavior, false);
                p.Reset(manhunterParty);
                if (p.AIBehaviorScores.ContainsKey(key))
                {
                    p.AIBehaviorScores[key] = 20f;
                }
                else
                {
                    p.AIBehaviorScores.Add(key, 20f);
                }
            }
        }
    }
}

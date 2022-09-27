﻿using Helpers;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace Manhunters
{
    public partial class ManhuntersCampaignBehavior : CampaignBehaviorBase
    {
        private void GoToTownToSellPrisoners(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement nearestTown = SettlementHelper.FindNearestTown(settlement => true);
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners;
            manhunterParty.SetMoveGoToSettlement(nearestTown);
        }

        private void GoToVillageToBuyFood(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement nearestFood = SettlementHelper.FindNearestSettlement(settlement => (settlement.IsTown || settlement.IsVillage) &&
            DoesSettlementHaveFood(settlement) && DoesPartyHaveGoldForFood(manhunterParty, settlement));
            if(nearestFood != null)
            {
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood;
                manhunterParty.SetMoveGoToSettlement(nearestFood);
            }
        }

        private void EngageToPlayerParty(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToPlayer;
            MobileParty playerParty = MobileParty.MainParty;
            if(playerParty.CurrentSettlement != null)
            {
                manhunterParty.SetMovePatrolAroundSettlement(MobileParty.MainParty.CurrentSettlement);
            }
            else
            {
                manhunterParty.SetMoveEngageParty(MobileParty.MainParty);
            }
        }

        public void EngageToBanditParty(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            MobileParty targetBanditParty = FindNearestBanditParty(manhunterParty);
            if (targetBanditParty != null)
            {
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                manhunterParty.SetMoveEngageParty(targetBanditParty);
            }
        }

        public MobileParty FindNearestBanditParty(MobileParty manhunterParty)
        {
            var banditParties = MobileParty.AllBanditParties
                .OrderBy(x => x.GetPosition2D.Distance(manhunterParty.Position2D)).ToList();
            if (banditParties.Count > 0)
            {
                return banditParties[0];
            }
            return null;
        }

        public bool IsThereBanditPartyInSight(MobileParty manhunterParty)
        {
            var banditPartiesInSight = MobileParty.AllBanditParties.Where(x => x.GetPosition2D.Distance(manhunterParty.Position2D) <= manhunterParty.SeeingRange).ToList();
            if (banditPartiesInSight.Count > 0)
            {
                return true;
            }
            return false;
        }

        public void TakeAction(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners || manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.BuyingFood)
            {
                return;
            }
            
            if (manhunterParty.GetNumDaysForFoodToLast() <= 1)
            {
                GoToVillageToBuyFood(manhunterParty, manhunterPartyComponent);
            }

            if ( manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood
              && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners && !_manhunterPartiesAndBetrayedLeader.ContainsKey(manhunterParty))
            {
                EngageToBanditParty(manhunterParty, manhunterPartyComponent);
            }

            if (manhunterParty.PrisonRoster.TotalManCount > 0 && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood && 
                !_manhunterPartiesAndBetrayedLeader.ContainsKey(manhunterParty))
            {
                GoToTownToSellPrisoners(manhunterParty, manhunterPartyComponent);
            }

            if (_manhunterPartiesAndBetrayedLeader.ContainsKey(manhunterParty) && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood )
            {
                if (!manhunterParty.PrisonRoster.Contains(CharacterObject.PlayerCharacter))
                {
                    EngageToPlayerParty(manhunterParty, manhunterPartyComponent);
                }
                else
                {
                    EngageToBanditParty(manhunterParty, manhunterPartyComponent);
                }
            }
        }

        public void SendManhuntersAfterPlayer(Hero betrayed_leader)
        {
            Hero manhunterHero = HeroCreator.CreateSpecialHero(CharacterObject.Find("manhunter_character"), faction: Clan.All.Where(clan => clan.StringId == "cs_manhunters").First());
            Settlement manhunterSettlement = betrayed_leader.HomeSettlement;
            TextObject partyName = new TextObject(betrayed_leader.Name.ToString() + "'s Manhunter Party");
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(partyName.ToStringWithoutClear(), manhunterHero, manhunterSettlement.GatePosition, 2, manhunterSettlement);

            int manhunterPartySizeLimit = (int)new ManhunterPartySizeLimitModel().GetPartyMemberSizeLimit(manhunterMobileParty.Party).LimitMaxValue;
            int manhunterPartySize = manhunterMobileParty.MemberRoster.TotalManCount;
            int playerPartySize = MobileParty.MainParty.MemberRoster.TotalManCount;

            if (manhunterPartySize < manhunterPartySizeLimit && manhunterPartySize < playerPartySize)
            {
                int numberofTroopsToAdd = Math.Min(manhunterPartySizeLimit, manhunterPartySizeLimit) - manhunterPartySize;
                CharacterObject troops = CharacterObject.Find(ManhunterCharacterStringId);
                manhunterMobileParty.MemberRoster.AddToCounts(troops, numberofTroopsToAdd);
            }

            manhunterMobileParty.SetCustomName(partyName);
            MBTextManager.SetTextVariable("BETRAYED_LEADER", betrayed_leader.Name.ToString());
            _manhunterPartiesAndBetrayedLeader.Add(manhunterMobileParty, betrayed_leader);
        }
    }
}

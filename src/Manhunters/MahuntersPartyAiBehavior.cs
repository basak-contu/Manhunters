using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace Manhunters
{
    public partial class ManhuntersCampaignBehavior : CampaignBehaviorBase
    {


        private void GoToTownToSellPrisoners(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement nearestTown = SettlementHelper.FindNearestTown(settlement => true);
            //InformationManager.DisplayMessage(new InformationMessage(manhunterParty.Name.ToString() + " party is " + "GOING TO " + nearestTown.Name.ToString() + " TO SELL PRISONERS"));
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners;
            manhunterParty.SetMoveGoToSettlement(nearestTown);
        }


        private void GoToVillageToBuyFood(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            //Settlement nearestVillage = SettlementHelper.FindNearestVillage(settlement => true);
            Settlement nearestFood = SettlementHelper.FindNearestSettlement(settlement => (settlement.IsTown || settlement.IsVillage) && DoesSettlementHaveFood(settlement));
            //InformationManager.DisplayMessage(new InformationMessage(manhunterParty.Name.ToString() + " party is " + "GOING TO " + nearestFood.Name.ToString() + " TO BUY FOOD"));
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood;
            manhunterParty.SetMoveGoToSettlement(nearestFood);
        }

        private void EngageToPlayerParty(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToPlayer;
            MobileParty playerParty = MobileParty.MainParty;
            if(playerParty.CurrentSettlement != null)
            {
                manhunterParty.SetMovePatrolAroundSettlement(MobileParty.MainParty.CurrentSettlement);
                //InformationManager.DisplayMessage(new InformationMessage(manhunterParty.Name.ToString() + " party is " + "WAITING FOR PLAYER"));
            }
            else
            {
                manhunterParty.SetMoveEngageParty(MobileParty.MainParty);
                //InformationManager.DisplayMessage(new InformationMessage(manhunterParty.Name.ToString() + " party is " + "ENGAGIN TO MAIN PARTY"));
            }
           

        }


        public void EngageToBanditParty(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            MobileParty targetBanditParty = FindNearestBanditParty(manhunterParty);
            if (targetBanditParty != null)
            {
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                manhunterParty.SetMoveEngageParty(targetBanditParty);
                //InformationManager.DisplayMessage(new InformationMessage(manhunterParty.Name.ToString() + " party is " + "ENGAGIN TO BANDIT PARTY"));
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
            /*
            if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.BuyingFood)
            {
                BuyFoodForNDays(manhunterParty, manhunterPartyComponent, BuyFoodForMinDays, BuyFoodForMaxDays);
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;

            }
            if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners)
            {
                SellPrisonersAction.ApplyForAllPrisoners(manhunterParty, manhunterParty.PrisonRoster, manhunterParty.CurrentSettlement, true);
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;

            } */
            
            if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners || manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.BuyingFood)
            {
                return;
            }
            

            if (manhunterParty.GetNumDaysForFoodToLast() <= 1)
            {
                GoToVillageToBuyFood(manhunterParty, manhunterPartyComponent);
            }
            /*
            if (IsThereBanditPartyInSight(manhunterParty) && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood
                && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners && !manhunterPartyComponent.IsAfterPlayer)
            {
                EngageToBanditParty(manhunterParty, manhunterPartyComponent);
            } */
            if ( manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood
              && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners && !_betrayedLeaderAndManhunter.ContainsKey(manhunterParty))
            {
                EngageToBanditParty(manhunterParty, manhunterPartyComponent);
            }

            if (manhunterParty.PrisonRoster.TotalManCount > 0 && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood && 
                !_betrayedLeaderAndManhunter.ContainsKey(manhunterParty))
            {
                GoToTownToSellPrisoners(manhunterParty, manhunterPartyComponent);
            }

            /*
            if (manhunterPartyComponent.IsAfterPlayer && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood)
            {
                EngageToPlayerParty(manhunterParty, manhunterPartyComponent);
            } */
            if (_betrayedLeaderAndManhunter.ContainsKey(manhunterParty) && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood )
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
            manhunterMobileParty.SetCustomName(partyName);

            MBTextManager.SetTextVariable("BETRAYED_LEADER", betrayed_leader.Name.ToString());

            //InformationManager.DisplayMessage(new InformationMessage(betrayed_leader.Name.ToString() + " sent manhunters from " + betrayed_leader.HomeSettlement.Name.ToString()));
            _betrayedLeaderAndManhunter.Add(manhunterMobileParty, betrayed_leader);
        }
    }
}

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.ObjectSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Library;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Encounters;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using System;
using TaleWorlds.Localization;

namespace Manhunters
{
    internal class ManhuntersBehaviour : CampaignBehaviorBase
    {
        private CharacterObject ManhunterCharacter;
        private Hero ManhunterClanLeader;
        private Clan ManhunterClan;

        private List<MobileParty> ManhunterParties = new List<MobileParty>();

        private MobileParty manhunterPartyForDebug;

        private int ProbabilityOfHidoutSpawn = 50;

        private int ProbabilityOfSendingManhuntersAfterPlayer = 50;

        private static int _manhunterHireCost = 100;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, ManhunterPartyHourlyTick);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
        }

        private void OnQuestCompleted(QuestBase questBase, QuestBase.QuestCompleteDetails questCompleteDetails)
        {
            if(questCompleteDetails == QuestBase.QuestCompleteDetails.FailWithBetrayal)
            {
                //InformationManager.DisplayMessage(new InformationMessage("YOU BETRAYED " + questBase.QuestGiver.Name.ToString()));

                int chance = MBRandom.RandomInt(0, 101);
                //if (chance > probabilityOfSendingManhuntersAfterPlayer)
                SendManhuntersAfterPlayer(questBase.QuestGiver);
            }
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            CreateManhunterCharacter();
            CreateManhunterClanLeader();
            GetManhunterClan();
            AddDialogs(campaignGameStarter);
            SpawnManhunterMobileParty();
        }


        private void OnNewGameCreated(CampaignGameStarter obj)
        {
            /*
            CreateManhunterCharacter();
            CreateManhunterClanLeader();
            GetManhunterClan(); 
            SpawnManhunterMobileParty();*/
        }

        private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
        {
            if (destroyedParty.PartyComponent is ManhunterPartyComponent)
            {
                SpawnManhunterMobileParty();
            }

            if (destroyerParty != null)
            {
                if (destroyerParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
                {
                    TakePrisoners(destroyerParty.MobileParty, manhunterPartyComponent);

                    destroyerParty.MemberRoster.RemoveIf((obj) => obj.Character.StringId != "manhunter_character" && obj.Character.StringId != destroyerParty.LeaderHero.CharacterObject.StringId);
                }
            }
        }


        private void ManhunterPartyHourlyTick(MobileParty mobileParty)
        {
            if (mobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                if(mobileParty.MemberRoster.TotalManCount < manhunterPartyComponent.MinPartySize && mobileParty.MapEvent == null)
                {
                    mobileParty.IsActive = false;
                }
                TakeAction(mobileParty, manhunterPartyComponent);
                
            }
        }

        private void OnHourlyTick()
        {   
            if (manhunterPartyForDebug == null)
            {
                //manhunterPartyForDebug = SpawnManhunterPartyAtPos(new Vec2(490, 290));
            }
        }
    

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {

            if (attackerParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                manhunterPartyComponent.DidEncounteredWithPlayer = true;
                if(manhunterPartyComponent.potentialPrisoners == null)
                {
                    manhunterPartyComponent.potentialPrisoners = TroopRoster.CreateDummyTroopRoster();
                }
                manhunterPartyComponent.potentialPrisoners.Add(defenderParty.PrisonRoster);
            }

        }

        private void DeclareWarBetweenManhuntersAndBandits()
        {
            foreach (Clan clan in Clan.BanditFactions)
            {
                FactionManager.DeclareWar(clan, ManhunterClan, true);
            }


        }
        private void OnDailyTick()
        {
            
            if (ManhunterParties.Count < GetTotalBanditParties())
            {
                SpawnManhunterMobileParty();
            }

            // Sends manhunters after player, if relation is less than 0
            //CheckRelationsWithHero();
        }


        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {

            campaignGameStarter.AddDialogLine("neutral_manhunter_dialog_1",
                    "start", "close_window",
                     "{=*}We are manhunters, law enforcers and we hunt the Looters and other Bandits.",
                    is_talking_to_neutral_manhunter,
                    manhunter_encounter_consequence);

            DialogFlow dialog = DialogFlow.CreateDialogFlow("start")
                .PlayerLine(new TextObject("Why are you following me? Who sent you"))
                .Condition(is_talking_to_enemy_manhunters)
                .NpcLine(new TextObject("{betrayed_leader} sent us to take you down. He did not forget that you betrayed him and you will pay for this."))
                .BeginPlayerOptions()
                    .PlayerOption(new TextObject("Whatever he is paying I will pay you double to leave me alone."))
                    .NpcLine(new TextObject("Can you afford " + (_manhunterHireCost * 2).ToString() + " gold ?"))
                    .Condition(is_talking_to_enemy_manhunters)
                    .CloseDialog()
                    .BeginPlayerOptions()
                        .PlayerOption(new TextObject("Yes, take this and get out of my sight. (-" + (_manhunterHireCost * 2).ToString() + " gold)"))
                        .Condition(can_give_bribe_condition)
                        .Consequence(enemy_manhunter_give_bribe_consequence)
                        .CloseDialog()
                        .PlayerOption(new TextObject("Nevermind I don't need to pay you I can take down all of you."))
                        .Condition(is_talking_to_enemy_manhunters)
                        .NpcLine(new TextObject("Suit yourself."))
                        .CloseDialog()
                    .EndPlayerOptions()
                    .PlayerOption(new TextObject("Fine let's get this over with."))
                    .CloseDialog()
                .EndPlayerOptions();
                
                

            Campaign.Current.ConversationManager.AddDialogFlow(dialog);


        }



        private void CreateManhunterClanLeader()
        {
            ManhunterClanLeader = HeroCreator.CreateSpecialHero(ManhunterCharacter, faction: ManhunterClan);
        }


        private void GetManhunterClan()
        {
            foreach (Clan clan in Clan.All)
            {
                if (clan.StringId == "cs_manhunters")
                {
                    if (ManhunterClanLeader != null)
                    {
                        clan.SetLeader(ManhunterClanLeader);
                    }
                    else
                    {

                    }
                    ManhunterClan = clan;
                }
            }

            ManhunterClan.IsRebelClan = true;
            DeclareWarBetweenManhuntersAndBandits();
        }

        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero arg3)
        {
            if (mobileParty != null && mobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {       
                if(manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners)
                {
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners;
                    SellPrisoners(mobileParty, manhunterPartyComponent);
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                }
                
                if(manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood && settlement.IsVillage)
                {
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.BuyingFood;
                    BuyFoodForNDays(mobileParty, manhunterPartyComponent, 2, 4);
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                }
            }
        }

        public override void SyncData(IDataStore dataStore)
        {

        }


        private void SpawnManhunterMobileParty()
        {
            int randomSettlementIndex = MBRandom.RandomInt(0, Settlement.All.Count);
            //Settlement randomSettlement = Settlement.All[randomSettlementIndex];
            // Settlement randomSettlement = SettlementHelper.FindRandomHideout(true);

            Settlement randomSettlement;
            int chance = MBRandom.RandomInt(0, 101);
            if(chance > ProbabilityOfHidoutSpawn)
            {
                randomSettlement = SettlementHelper.FindRandomHideout(settlement => true);
            }
            else
            {
                randomSettlement = SettlementHelper.FindRandomSettlement(settlement => !settlement.IsHideout);
            }
            Hero manhunterHero = HeroCreator.CreateSpecialHero(ManhunterCharacter, faction: ManhunterClan);
            
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(ManhunterParties.Count.ToString(), manhunterHero, randomSettlement.GatePosition, 2, randomSettlement, manhunterHero, false);
            //manhunterMobileParty.GetNumDaysForFoodToLast
            
            ManhunterParties.Add(manhunterMobileParty);
            
        }

        // For testing
        private MobileParty SpawnManhunterPartyAtPos(Vec2 pos)
        {
            int randomSettlementIndex = MBRandom.RandomInt(0, Settlement.All.Count);
            Settlement randomSettlement = Settlement.All[randomSettlementIndex];
            Hero manhunterHero = HeroCreator.CreateSpecialHero(ManhunterCharacter, faction: ManhunterClan);
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(ManhunterParties.Count.ToString(), manhunterHero, pos, 2, randomSettlement, manhunterHero, false);
            ManhunterParties.Add(manhunterMobileParty);
            return manhunterMobileParty;
        }


        public void CreateManhunterCharacter()
        {
            ManhunterCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("manhunter_character");
        }

        public int GetTotalBanditParties()
        {
            return MobileParty.AllBanditParties.Count;
        }

        private void GoToTownToSellPrisoners(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement nearestTown = SettlementHelper.FindNearestTown(settlement => true);
            //InformationManager.DisplayMessage(new InformationMessage(manhunterParty.LeaderHero.Name.ToString() + " party is " + "GOING TO " + nearestTown.Name.ToString() + " TO SELL PRISONERS"));
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners;
            manhunterParty.SetMoveGoToSettlement(nearestTown);
        }


        private void GoToVillageToBuyFood(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement nearestVillage = SettlementHelper.FindNearestVillage(settlement => true);
            //InformationManager.DisplayMessage(new InformationMessage(manhunterParty.LeaderHero.Name.ToString() + " party is " + "GOING TO " + nearestVillage.Name.ToString() + " TO BUY FOOD"));
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood;
            manhunterParty.SetMoveGoToSettlement(nearestVillage);
        }

        public void TakePrisoners(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            if (manhunterPartyComponent == null || manhunterParty == null)
            {
                return;
            }

            else
            {
                // Add prisoners 
                manhunterParty.PrisonRoster.Add(manhunterPartyComponent.potentialPrisoners);
                foreach (var prisoner in manhunterParty.PrisonRoster.GetTroopRoster().ToList())
                {
                    InformationManager.DisplayMessage(new InformationMessage(prisoner.Character.Name.ToString() + " has been taken prisoner by " + manhunterParty.LeaderHero.Name.ToString() + " of the Manhunters."));
                }
                // Reset potential prisoners
                manhunterPartyComponent.potentialPrisoners = TroopRoster.CreateDummyTroopRoster();
            }
        }

        private void EngageToPlayerParty(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToPlayer;
            manhunterParty.SetMoveEngageParty(MobileParty.MainParty);
            //InformationManager.DisplayMessage(new InformationMessage(manhunterParty.LeaderHero.Name.ToString() + " party is " + "ENGAGIN TO MAIN PARTY"));
        }


        public void EngageToBanditParty(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            MobileParty targetBanditParty = FindNearestBanditParty(manhunterParty);
            if (targetBanditParty != null)
            {
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                manhunterParty.SetMoveEngageParty(targetBanditParty);
                //InformationManager.DisplayMessage(new InformationMessage(manhunterParty.LeaderHero.Name.ToString() + " party is " + "ENGAGIN TO BANDIT PARTY"));
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
            if(banditPartiesInSight.Count > 0)
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

            if (IsThereBanditPartyInSight(manhunterParty) && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood
                && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners && !manhunterPartyComponent.IsAfterPlayer)
            {
                EngageToBanditParty(manhunterParty, manhunterPartyComponent);
            }

            if(manhunterParty.PrisonRoster.TotalManCount > 0)
            {
                GoToTownToSellPrisoners(manhunterParty, manhunterPartyComponent);
            }

            if (manhunterPartyComponent.IsAfterPlayer && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood)
            {
                EngageToPlayerParty(manhunterParty, manhunterPartyComponent);
            }

        }

        public void SendManhuntersAfterPlayer(Hero betrayed_leader)
        {
            Hero manhunterHero = HeroCreator.CreateSpecialHero(CharacterObject.Find("manhunter_character"), faction: Clan.All.Where(clan => clan.StringId == "cs_manhunters").First());

            Settlement manhunterSettlement = betrayed_leader.HomeSettlement;

            TextObject partyName = new TextObject(betrayed_leader.Name.ToString() + "'s Manhunter Party");
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(partyName.ToStringWithoutClear(), manhunterHero, manhunterSettlement.GatePosition, 2, manhunterSettlement, manhunterHero, true);
            manhunterMobileParty.SetCustomName(partyName);
            ((ManhunterPartyComponent)manhunterMobileParty.PartyComponent).SentFrom = betrayed_leader;

            betrayed_leader.ChangeHeroGold(-_manhunterHireCost);

            MBTextManager.SetTextVariable("betrayed_leader", betrayed_leader.Name.ToString());

            ManhunterParties.Add(manhunterMobileParty);
        }

      
        private void SellPrisoners(MobileParty party, ManhunterPartyComponent component)
        {
            if (party.PrisonRoster.TotalManCount > 0)
            {
                var gold = 0;
                foreach (var prisoner in party.PrisonRoster.GetTroopRoster())
                {
                    gold += Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(prisoner.Character);
                    InformationManager.DisplayMessage(new InformationMessage(prisoner.Character.Name.ToString() + " has been sold to " + party.CurrentSettlement.Name.ToString()));
                }

                SellPrisonersAction.ApplyForAllPrisoners(party, party.PrisonRoster, party.CurrentSettlement, false);
                component.Gold += gold;
                party.PartyTradeGold += gold;
            }
        }


        private bool is_talking_to_neutral_manhunter()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;

            if (encounteredParty == null) return false;

            if (encounteredParty.MobileParty == null) return false;

            if (Hero.OneToOneConversationHero == null) return false;

            if (encounteredParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent && Hero.OneToOneConversationHero.Clan.StringId == "cs_manhunters")
            {
                if (!manhunterPartyComponent.IsAfterPlayer)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }

        private bool is_talking_to_enemy_manhunters()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;

            if(encounteredParty == null) return false;

            if (encounteredParty.MobileParty == null) return false;

            if (Hero.OneToOneConversationHero == null) return false;

            
            if (encounteredParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent && Hero.OneToOneConversationHero.Clan.StringId == "cs_manhunters")
            {
                if (manhunterPartyComponent.IsAfterPlayer && !manhunterPartyComponent.DidEncounteredWithPlayer)
                {
                    if(manhunterPartyComponent.SentFrom != null)
                        MBTextManager.SetTextVariable("betrayed_leader", manhunterPartyComponent.SentFrom.Name.ToString());
                    return true;
                }
                else
                    return false;
            }

            else
                return false;
        }

        private void manhunter_encounter_consequence()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (encounteredParty != null)
            {
                PlayerEncounter.LeaveEncounter = true;
            }
        }

        private void enemy_manhunter_give_bribe_consequence()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (encounteredParty != null)
            {
                if(encounteredParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
                {
                    manhunterPartyComponent.DidEncounteredWithPlayer = true;
                    manhunterPartyComponent.IsAfterPlayer = false;
                    Hero.MainHero.ChangeHeroGold(-_manhunterHireCost * 2);
                    EngageToBanditParty(encounteredParty.MobileParty, manhunterPartyComponent);
                    PlayerEncounter.LeaveEncounter = true;
                }
            }
        }

        private bool can_give_bribe_condition()
        {
            if (!is_talking_to_enemy_manhunters())
            {
                return false;
            }


            if(Hero.MainHero.Gold >= _manhunterHireCost * 2)
            {
                return true;
            }
            return false;
        }

        private void BuyFoodForNDays(MobileParty party, ManhunterPartyComponent manhunterPartyComponent, float daysMin, float daysMax)
        {
            /*
            InformationManager.DisplayMessage(new InformationMessage("BEFORE BUYING FOOD"));
            foreach (var item in party.ItemRoster)
            {
                InformationManager.DisplayMessage(new InformationMessage(item.GetType().Name.ToString()));
            }
            */

            var dailyFoodConsumption = Math.Abs(party.FoodChange);

            var days = MBRandom.RandomFloatRanged(daysMin, daysMax);

            Settlement settlement = party.CurrentSettlement;
            if (days * dailyFoodConsumption > party.Food)
            {
                var foodRequirement = Math.Ceiling((days * dailyFoodConsumption) - party.Food);

                float cost = 0f;
                var startIndex = MBRandom.RandomInt(0, settlement.ItemRoster.Count);
                for (int i = startIndex; i < settlement.ItemRoster.Count + startIndex && foodRequirement > 0; i++)
                {
                    var currentIndex = i % settlement.ItemRoster.Count;
                    var itemRosterElement = settlement.ItemRoster.GetElementCopyAtIndex(currentIndex);

                    if (!itemRosterElement.IsEmpty &&
                        itemRosterElement.EquipmentElement.Item.IsFood)
                    {
                        float effectiveAmount = (float)Math.Min(itemRosterElement.Amount, foodRequirement);
                        cost += settlement.Village.GetItemPrice(itemRosterElement.EquipmentElement.Item) * effectiveAmount;
                        foodRequirement -= effectiveAmount;
                        party.ItemRoster.AddToCounts(itemRosterElement.EquipmentElement.Item, (int)effectiveAmount);
                    }
                }

                manhunterPartyComponent.Gold -= (int)cost;
            }

            /*
            InformationManager.DisplayMessage(new InformationMessage("AFTER BUYING FOOD"));
            foreach (var item in party.ItemRoster)
            {
                InformationManager.DisplayMessage(new InformationMessage(item.GetType().Name.ToString()));
            }
            */
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("remove_food_from_manhunters", "manhunters")]
        private static string RemoveManhuntersFood(List<string> strings)
        {
            var allManhunterParties = MobileParty.All.Where(x => x.PartyComponent is ManhunterPartyComponent);

            foreach (MobileParty party in allManhunterParties.ToList())
            {
                foreach(ItemRosterElement item in party.ItemRoster)
                {
                    if (item.EquipmentElement.Item.IsFood)
                    {
                        party.ItemRoster.Remove(item);
                    }
                }
            }

            return "removed food from manhunters ";

        }


        [CommandLineFunctionality.CommandLineArgumentFunction("get_relations", "manhunters")]
        private static string CheckRelations(List<string> strings)
        {

            string output = "";
            var allHeroes = Hero.AllAliveHeroes;
            foreach (var hero in allHeroes.ToList())
            {
                if (Hero.MainHero.GetRelation(hero) < 0)
                {
                    //SendManhuntersAfterPlayer(hero);
                    output += hero.Name.ToString() + ": " + Hero.MainHero.GetRelation(hero).ToString();
                }
            }

            return output;
        }


        [CommandLineFunctionality.CommandLineArgumentFunction("decrease_relation_with_random_hero", "manhunters")]
        private static string DecreaseRelationWithRandomHero(List<string> strings)
        {
            Hero mainHero = Hero.MainHero;
            Hero leader = Hero.AllAliveHeroes.GetRandomElement();
            int relation = mainHero.GetRelation(leader);

            ChangeRelationAction.ApplyPlayerRelation(leader, -(relation + 1));

            return "relation with " + leader.Name.ToString() + " decreased to " + mainHero.GetRelation(leader).ToString();

        }


        [CommandLineFunctionality.CommandLineArgumentFunction("send_manhunters_after_player", "manhunters")]
        private static string SendManhuntersAfterPlayerCommand(List<string> strings)
        {
            Settlement nearestTown = SettlementHelper.FindNearestTown(settlement => true);
            Hero leader = nearestTown.OwnerClan.Leader;

            Settlement manhunterSettlement = leader.HomeSettlement;


            Hero manhunterHero = HeroCreator.CreateSpecialHero(CharacterObject.Find("manhunter_character"), faction: Clan.All.Where(clan => clan.StringId == "cs_manhunters").First());

            TextObject partyName = new TextObject(leader.Name.ToString() + "'s Manhunter Party");
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(partyName.ToStringWithoutClear(), manhunterHero, manhunterSettlement.GatePosition, 2, manhunterSettlement, manhunterHero, true);
            manhunterMobileParty.SetCustomName(partyName);
            ((ManhunterPartyComponent)manhunterMobileParty.PartyComponent).SentFrom = leader;

            leader.ChangeHeroGold(-_manhunterHireCost);

            //MBTextManager.SetTextVariable("betrayed_leader", leader.Name.ToString());


            return manhunterMobileParty.Name.ToString() + " spawned from " + manhunterSettlement.Name.ToString();
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("spawn_manhunter_and_bandit", "manhunters")]
        public static string SpawnManhunterAndBandit(List<string> strings)
        {
            Vec2 pos = MobileParty.MainParty.Position2D + new Vec2(1, 1);

            CharacterObject manhunterCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("manhunter_character");

            Hero manhunterClanLeader = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: null);
            

            Clan manhaunterClan = null;

            foreach (Clan clan in Clan.All)
            {
                if (clan.StringId == "cs_manhunters")
                {
                    if (manhunterClanLeader != null)
                    {
                        clan.SetLeader(manhunterClanLeader);
                    }
                    else
                    {

                    }
                    manhaunterClan = clan;
                }
            }
            manhunterClanLeader.Clan = manhaunterClan;
            int randomSettlementIndex = MBRandom.RandomInt(0, Settlement.All.Count);
            Settlement randomSettlement = Settlement.All[randomSettlementIndex];
            Hero manhunterHero = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: manhaunterClan);
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty("manhunter party test", manhunterHero, pos, 2, randomSettlement, manhunterHero, false);



            Hideout hideout = Hideout.All.ToList().ElementAt(0);
            Clan banditClan = null;
            foreach (Clan clan in Clan.BanditFactions)
            {
                if (hideout.Settlement.Culture == clan.Culture)
                {
                    banditClan = clan;
                    break;
                }
            }
            MobileParty banditParty = BanditPartyComponent.CreateBanditParty("takehideouts_party", banditClan, hideout, false);

            TroopRoster memberRoster = new TroopRoster(banditParty.Party);
            CharacterObject troop = banditClan.Culture.BanditChief;
            CharacterObject prisoner = banditClan.Culture.BasicTroop;
            memberRoster.AddToCounts(troop, MBRandom.RandomInt(6, 14));

            TroopRoster prisonerRoster = new TroopRoster(banditParty.Party);
            prisonerRoster.AddToCounts(prisoner, 10);


            banditParty.InitializeMobilePartyAtPosition(memberRoster, prisonerRoster, hideout.Settlement.Position2D);
            banditParty.Position2D = pos;
            banditParty.InitializePartyTrade(300);
            banditParty.Party.Visuals.SetMapIconAsDirty();

            return "Spawned both manhunters and bandits";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("spawn_manhunter_party", "manhunters")]
        public static string SpawnManhunterPartyCommand(List<string> strings)
        {
            CharacterObject manhunterCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("manhunter_character");
            Hero manhunterClanLeader = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: null);

            Clan manhaunterClan = null;

            foreach (Clan clan in Clan.All)
            {
                if (clan.StringId == "cs_manhunters")
                {
                    if (manhunterClanLeader != null)
                    {
                        clan.SetLeader(manhunterClanLeader);
                    }
                    else
                    {

                    }
                    manhaunterClan = clan;
                }
            }
            manhunterClanLeader.Clan = manhaunterClan;

            //Vec2 pos = new Vec2(490, 290);
            Vec2 pos = MobileParty.MainParty.Position2D;

            int randomSettlementIndex = MBRandom.RandomInt(0, Settlement.All.Count);
            Settlement randomSettlement = Settlement.All[randomSettlementIndex];
            Hero manhunterHero = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: manhaunterClan);
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty("manhunter party test", manhunterHero, pos, 2, randomSettlement, manhunterHero, false);
            return "spawned manhunter party";
        }

        // For testing
        [CommandLineFunctionality.CommandLineArgumentFunction("spawn_bandit_party", "manhunters")]
        public static string SpawnBanditPartyCommand(List<string> strings)
        {
            //Vec2 pos = new Vec2(490, 290);
            Vec2 pos = MobileParty.MainParty.Position2D ;


            Hideout hideout = Hideout.All.ToList().ElementAt(0);
            Clan banditClan = null;
            foreach (Clan clan in Clan.BanditFactions)
            {
                if (hideout.Settlement.Culture == clan.Culture)
                {
                    banditClan = clan;
                    break;
                }
            }
            MobileParty banditParty = BanditPartyComponent.CreateBanditParty("takehideouts_party", banditClan, hideout, false); 

            TroopRoster memberRoster = new TroopRoster(banditParty.Party);
            CharacterObject troop = banditClan.Culture.BanditChief;
            CharacterObject prisoner = banditClan.Culture.BasicTroop;
            memberRoster.AddToCounts(troop, MBRandom.RandomInt(6, 14));

            TroopRoster prisonerRoster = new TroopRoster(banditParty.Party);
            prisonerRoster.AddToCounts(prisoner, 10);


            banditParty.InitializeMobilePartyAtPosition(memberRoster, prisonerRoster, hideout.Settlement.Position2D);
            banditParty.Position2D = pos;
            banditParty.InitializePartyTrade(300);
            banditParty.Party.Visuals.SetMapIconAsDirty();

            return "spawned bandit party";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("fail_quest_with_betrayel", "manhunters")]
        public static string CompleteQuestWithBetrayel(List<string> strings)
        {
            
            QuestBase questBase = Campaign.Current.QuestManager.Quests.Where(q => q.QuestGiver != null).First();
            questBase.CompleteQuestWithBetrayal();

            return questBase.QuestGiver.Name.ToString() + " sent manhunters from " + questBase.QuestGiver.HomeSettlement.Name.ToString();
        }


    }
}
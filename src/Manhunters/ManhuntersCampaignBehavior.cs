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
using TaleWorlds.CampaignSystem.Actions;
using System.Collections.Generic;
using System.Linq;
using Helpers;
using System;
using TaleWorlds.Localization;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Conversation;

namespace Manhunters
{
    public partial class ManhuntersCampaignBehavior : CampaignBehaviorBase
    {

        private const float ProbabilityOfHidoutSpawn = 0.5f;
        private const float ProbabilityOfSendingManhuntersAfterPlayer = 0.5f;

        private const string ManhunterClanStringId = "cs_manhunters";
        private const string ManhunterCharacterStringId = "manhunter_character";

        [CachedData]
        private CharacterObject _manhunterCharacter;

        [CachedData]
        private Clan _manhunterClan;

        [CachedData]
        private Hero _manhunterClanLeader;
        
        [CachedData]
        private List<MobileParty> _manhunterPartiesCache = new List<MobileParty>();

        private const int ManhunterHireCost = 100;

        private const int BuyFoodForMinDays = 2;
        private const int BuyFoodForMaxDays = 4;

        private const int MinFoodNumber = 15;
        private const int MaxFoodNumber = 20;

        private static Dictionary<MobileParty, Hero> _betrayedLeaderAndManhunter =
                     new Dictionary<MobileParty, Hero>();

        private MobileParty manhunterPartyThatCapturedPlayer;
        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.MapEventStarted.AddNonSerializedListener(this, OnMapEventStarted);
            CampaignEvents.MapEventEnded.AddNonSerializedListener(this, OnMapEventEnded);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, ManhunterPartyHourlyTick);
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, MobilePartyCreated);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
            CampaignEvents.OnPlayerBattleEndEvent.AddNonSerializedListener(this, OnPlayerBattleEndEvent);
            CampaignEvents.HeroPrisonerReleased.AddNonSerializedListener(this, OnHeroPrisonerReleased);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, HeroPrisonerTaken);
        }

        private void HeroPrisonerTaken(PartyBase partyBase, Hero hero)
        {
            if(partyBase.MobileParty.PartyComponent is ManhunterPartyComponent && hero == Hero.MainHero)
            {
                manhunterPartyThatCapturedPlayer = partyBase.MobileParty;
            }
        }

        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            
            //CreateManhunterCharacter();
            //CreateManhunterClanLeader();

            //GetManhunterClan(); 
            CacheManhunterVariables();
            AddDialogs(campaignGameStarter);
            SpawnManhunterMobileParty();
        }

        private void CacheManhunterVariables()
        {
            _manhunterCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(ManhunterCharacterStringId);

            _manhunterClanLeader = HeroCreator.CreateSpecialHero(_manhunterCharacter, faction: _manhunterClan);

            foreach (Clan clan in Clan.All)
            {
                if (clan.StringId == ManhunterClanStringId)
                {
                    if (_manhunterClanLeader != null)
                    {
                        clan.SetLeader(_manhunterClanLeader);
                    }
                    _manhunterClan = clan;
                }

            }
        }

        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero arg3)
        {
            if (mobileParty != null && mobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners)
                {
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners;
                    SellPrisonersAction.ApplyForAllPrisoners(mobileParty, mobileParty.PrisonRoster, settlement, true);
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                }

                if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood && settlement.IsVillage)
                {
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.BuyingFood;
                    //BuyFoodForNDays(mobileParty, manhunterPartyComponent, BuyFoodForMinDays, BuyFoodForMaxDays);
                    BuyFood(mobileParty, manhunterPartyComponent, MBRandom.RandomInt(MinFoodNumber, MaxFoodNumber));
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                }
            }
        }

        private void OnNewGameCreated(CampaignGameStarter obj)
        {
            /*
            CreateManhunterCharacter();
            CreateManhunterClanLeader();
            GetManhunterClan(); 
            SpawnManhunterMobileParty();*/
            //CacheManhunterVariables();
            InitializeMenhunterClan();
        }

        private void OnGameLoaded(CampaignGameStarter obj)
        {

            foreach(MobileParty mobileParty in MobileParty.All.ToList())
            {
                if(mobileParty.PartyComponent is ManhunterPartyComponent)
                {
                    _manhunterPartiesCache.Add(mobileParty);
                }
            }
        }

        private void OnDailyTick()
        {

            if (_manhunterPartiesCache.Count < MobileParty.AllBanditParties.Count)
            {
                SpawnManhunterMobileParty();
            }
            /*
            if (!Hero.MainHero.IsPrisoner && Clan.PlayerClan.IsAtWarWith(_manhunterClan))
            {
                if (manhunterPartyThatCapturedPlayer != null && _betrayedLeaderAndManhunter.ContainsKey(manhunterPartyThatCapturedPlayer))
                {
                    _betrayedLeaderAndManhunter.Remove(manhunterPartyThatCapturedPlayer);
                    MakePeaceAction.Apply(Clan.PlayerClan, _manhunterClan);
                }
            } */
        }


        private void OnHourlyTick()
        {
            
            if (!Hero.MainHero.IsPrisoner && Clan.PlayerClan.IsAtWarWith(_manhunterClan))
            {
                if (manhunterPartyThatCapturedPlayer != null && _betrayedLeaderAndManhunter.ContainsKey(manhunterPartyThatCapturedPlayer))
                {
                    if(manhunterPartyThatCapturedPlayer.MapEvent == null)
                    {
                        _betrayedLeaderAndManhunter.Remove(manhunterPartyThatCapturedPlayer);
                        MakePeaceAction.Apply(Clan.PlayerClan, _manhunterClan);
                    }

                }
            } 
        }


        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {
            /*
            if (attackerParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                manhunterPartyComponent.DidEncounteredWithPlayer = true;
            }
            */
        }
        private void OnMapEventEnded(MapEvent obj)
        {
            //MakePeaceAction.Apply(_manhunterClan, Clan.PlayerClan);
            //FactionManager.DeclareAlliance(attackerParty.ActualClan, defenderParty.ActualClan);
           
        }

        private void ManhunterPartyHourlyTick(MobileParty mobileParty)
        {
            if (mobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                if (mobileParty.MemberRoster.TotalManCount < manhunterPartyComponent.MinPartySize && mobileParty.MapEvent == null)
                {
                    //mobileParty.IsActive = false;
                    DestroyPartyAction.Apply(null, mobileParty);
                }
                else{
                    TakeAction(mobileParty, manhunterPartyComponent);
                }

            }
        }

        private void MobilePartyCreated(MobileParty party)
        {
            if(party.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                _manhunterPartiesCache.Add(party);
            }
        }


        private void OnMobilePartyDestroyed(MobileParty destroyedParty, PartyBase destroyerParty)
        {
            if (destroyedParty.PartyComponent is ManhunterPartyComponent)
            {
                SpawnManhunterMobileParty();
                if(destroyerParty.MobileParty == MobileParty.MainParty)
                {
                    MakePeaceAction.Apply(_manhunterClan, Clan.PlayerClan);
                }
            }

            if (destroyerParty != null)
            {
                if (destroyerParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
                {
                    //destroyerParty.MemberRoster.RemoveIf((obj) => obj.Character.StringId != "manhunter_character" && obj.Character.StringId != destroyerParty.LeaderHero.CharacterObject.StringId);
                    //destroyerParty.MemberRoster.RemoveIf((obj) => obj.Character.StringId != "manhunter_character" );
                    destroyerParty.MemberRoster.RemoveIf((obj) => obj.Character.StringId != ManhunterCharacterStringId );
                }
                
            }
        }

        private void OnQuestCompleted(QuestBase questBase, QuestBase.QuestCompleteDetails questCompleteDetails)
        {
            if (questCompleteDetails == QuestBase.QuestCompleteDetails.FailWithBetrayal)
            {
                //InformationManager.DisplayMessage(new InformationMessage("YOU BETRAYED " + questBase.QuestGiver.Name.ToString()));

                //int chance = MBRandom.RandomInt(0, 101);
                float chance = MBRandom.RandomFloatRanged(0f, 1f);
                //if(chance > _probabilityOfSendingManhuntersAfterPlayer)
                //if (chance > probabilityOfSendingManhuntersAfterPlayer)
                SendManhuntersAfterPlayer(questBase.QuestGiver);
            }
        }


        private void OnPlayerBattleEndEvent(MapEvent mapEvent)
        {
            MobileParty attackerParty = mapEvent.AttackerSide.LeaderParty.MobileParty;
            MobileParty defenderParty = mapEvent.DefenderSide.LeaderParty.MobileParty;
            if (mapEvent.Winner.LeaderParty.MobileParty == MobileParty.MainParty && attackerParty.PartyComponent is ManhunterPartyComponent)
            {
                /*
                var item = _betrayedLeaderAndManhunter.First(kvp => kvp.Value == attackerParty);
                _betrayedLeaderAndManhunter.Remove(item.Key); */
                
                
            } 
        }


        private void OnHeroPrisonerReleased(Hero released, PartyBase releasedFrom, IFaction capturer, EndCaptivityDetail detail)
        {
            InformationManager.DisplayMessage(new InformationMessage("HERO RELEASED"));
            if (released == Hero.MainHero && releasedFrom.MobileParty.PartyComponent is ManhunterPartyComponent)
            {
                InformationManager.DisplayMessage(new InformationMessage("HERO RELEASED"));
               
                MakePeaceAction.Apply(Clan.PlayerClan, _manhunterClan);
            }
        }
 
        private void DeclareWarBetweenManhuntersAndBandits()
        {
            foreach (Clan clan in Clan.BanditFactions)
            {
                FactionManager.DeclareWar(clan, _manhunterClan, true);
            }
        }



        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {

            campaignGameStarter.AddDialogLine("neutral_manhunter_dialog_1",
                    "start", "close_window",
                     "{=*}We are manhunters, law enforcers and we hunt the Looters and other Bandits.",
                    talking_to_neutral_manhunter_condition,
                    manhunter_encounter_consequence);

            DialogFlow dialog = DialogFlow.CreateDialogFlow("start")
                .PlayerLine(new TextObject("{=*}Why are you following me? Who sent you?"))
                .Condition(talking_to_enemy_manhunters_condition)
                .NpcLine(new TextObject("{=*}{BETRAYED_LEADER} sent us to take you down. He did not forget that you betrayed him and you will pay for this."))
                .BeginPlayerOptions()
                    .PlayerOption(new TextObject("{=*}Whatever he is paying I will pay you double to leave me alone."))
                    .NpcLine(new TextObject("{=*}Can you afford {BRIBE_AMOUNT}{GOLD_ICON}?"))
                    .Condition(talking_to_enemy_manhunters_condition)
                    .CloseDialog()
                    .BeginPlayerOptions()
                        .PlayerOption(new TextObject("{=*}Yes, take this and get out of my sight. (- {BRIBE_AMOUNT}{GOLD_ICON})"))
                        .Condition(talking_to_enemy_manhunters_condition)
                        .ClickableCondition(new ConversationSentence.OnClickableConditionDelegate(this.BribeClickableConditions))
                        .Consequence(enemy_manhunter_give_bribe_consequence)
                        .CloseDialog()
                        .PlayerOption(new TextObject("{=*}Nevermind I don't need to pay you I can take down all of you."))
                        .Condition(talking_to_enemy_manhunters_condition)
                        .Consequence(enemy_manhuner_fight_consequence)
                        .NpcLine(new TextObject("{=*}Suit yourself."))
                        .CloseDialog()
                    .EndPlayerOptions()
                    .PlayerOption(new TextObject("{=*}Fine let's get this over with."))
                    .Consequence(enemy_manhuner_fight_consequence)
                    .CloseDialog()
                .EndPlayerOptions();
                
                

            Campaign.Current.ConversationManager.AddDialogFlow(dialog);


        }


        
        private void CreateManhunterClanLeader()
        {
            _manhunterClanLeader = HeroCreator.CreateSpecialHero(_manhunterCharacter, faction: _manhunterClan);
        }
        

        
        private void GetManhunterClan()
        {
            foreach (Clan clan in Clan.All)
            {
                if (clan.StringId == ManhunterClanStringId)
                {
                    if (_manhunterClanLeader != null)
                    {
                        clan.SetLeader(_manhunterClanLeader);
                    }
                    _manhunterClan = clan;
                }
            }

            _manhunterClan.IsRebelClan = true;
            DeclareWarBetweenManhuntersAndBandits();
        }

        private void InitializeMenhunterClan()
        {
            if(_manhunterClan == null)
            {
                _manhunterClan = Clan.All.Where(clan => clan.StringId == ManhunterClanStringId).First();
            }
            _manhunterClan.IsRebelClan = true;
            DeclareWarBetweenManhuntersAndBandits();
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
            float chance = MBRandom.RandomFloatRanged(0f, 1f);
            if(chance > ProbabilityOfHidoutSpawn)
            {
                randomSettlement = SettlementHelper.FindRandomHideout(settlement => true);
            }
            else
            {
                randomSettlement = SettlementHelper.FindRandomSettlement(settlement => !settlement.IsHideout);
            }
            Hero manhunterHero = HeroCreator.CreateSpecialHero(_manhunterCharacter, faction: _manhunterClan);
            
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(_manhunterPartiesCache.Count.ToString(), manhunterHero, randomSettlement.GatePosition, 2, randomSettlement);
            
            //_manhunterPartiesCache.Add(manhunterMobileParty);
            
        }




        public void CreateManhunterCharacter()
        {
            _manhunterCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(ManhunterCharacterStringId);
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
                //component.Gold += gold;
                party.PartyTradeGold += gold;
            }
        }

        private bool DoesSettlementHaveFood(Settlement settlement)
        {
            bool hasFood = false;
            foreach(ItemRosterElement item in settlement.ItemRoster.ToList())
            {
                if (item.EquipmentElement.Item.IsFood)
                {
                    hasFood = true;
                }
            }
            return hasFood;
        }
        private bool talking_to_neutral_manhunter_condition()
        {
            /*
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
            } */

            MobileParty conversationParty = MobileParty.ConversationParty;
            if (conversationParty == null)
            {
                return false;
            }
            if (conversationParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                //if (!manhunterPartyComponent.IsAfterPlayer)
                if (!_betrayedLeaderAndManhunter.ContainsKey(conversationParty))
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private bool talking_to_enemy_manhunters_condition()
        {
            /*
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;

            if(encounteredParty == null) return false;

            if (encounteredParty.MobileParty == null) return false;

            if (Hero.OneToOneConversationHero == null) return false;

            
            if (encounteredParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent && Hero.OneToOneConversationHero.Clan.StringId == "cs_manhunters")
            {
                if (manhunterPartyComponent.IsAfterPlayer )
                {
                    if(manhunterPartyComponent.SentFrom != null)
                        MBTextManager.SetTextVariable("betrayed_leader", manhunterPartyComponent.SentFrom.Name.ToString());
                    return true;
                }
                else
                    return false;
            }

            else
                return false; */


            MobileParty conversationParty = MobileParty.ConversationParty;
            if (conversationParty == null)
            {
                return false;
            }
            if (conversationParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                /*
                if (manhunterPartyComponent.IsAfterPlayer)
                {
                    if (manhunterPartyComponent.SentFrom != null)
                        MBTextManager.SetTextVariable("betrayed_leader", manhunterPartyComponent.SentFrom.Name.ToString());
                    return true;
                } */
                if (_betrayedLeaderAndManhunter.ContainsKey(conversationParty))
                {
                    /*
                    if (manhunterPartyComponent.SentFrom != null)
                        MBTextManager.SetTextVariable("betrayed_leader", manhunterPartyComponent.SentFrom.Name.ToString());*/
                    MBTextManager.SetTextVariable("BETRAYED_LEADER", _betrayedLeaderAndManhunter[conversationParty]);
                    MBTextManager.SetTextVariable("BRIBE_AMOUNT", (ManhunterHireCost * 2).ToString());
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void manhunter_encounter_consequence()
        {
            if (PlayerEncounter.EncounteredParty != null)
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
                    //manhunterPartyComponent.DidEncounteredWithPlayer = true;
                    //manhunterPartyComponent.IsAfterPlayer = false;
                    /*var item = _betrayedLeaderAndManhunter.First(kvp => kvp.Value == encounteredParty.MobileParty);
                    _betrayedLeaderAndManhunter.Remove(item.Key);*/
                    _betrayedLeaderAndManhunter.Remove(encounteredParty.MobileParty);

                    Hero.MainHero.ChangeHeroGold(-ManhunterHireCost * 2);
                    EngageToBanditParty(encounteredParty.MobileParty, manhunterPartyComponent);
                    PlayerEncounter.LeaveEncounter = true;
                    if (Clan.PlayerClan.IsAtWarWith(_manhunterClan))
                    {
                        MakePeaceAction.Apply(_manhunterClan, Clan.PlayerClan);
                    }
                }
            }
        }

        private void enemy_manhuner_fight_consequence()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (encounteredParty != null)
            {
                if (encounteredParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
                {
                    EngageToBanditParty(encounteredParty.MobileParty, manhunterPartyComponent);
                }

            }
        }

        private bool give_bribe_condition()
        {
            /*
            if (!talking_to_enemy_manhunters_condition())
            {
                return false;
            }
            */

            if(Hero.MainHero.Gold >= ManhunterHireCost * 2)
            {
                return true;
            }
            return false;
        }

        private bool BribeClickableConditions(out TextObject explanation)
        {
            if(Hero.MainHero.Gold >= ManhunterHireCost * 2)
            {
                explanation = TextObject.Empty;
                return true;
            }
            explanation = new TextObject("{=*}You don't have enough {GOLD_ICON}.", null);
            return false;
        }

        private void BuyFood(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent, int numberOfFoodItems)
        {

            for (var i = 0; i < numberOfFoodItems; i++)
            {
                Campaign.Current.Models.PartyFoodBuyingModel.FindItemToBuy(manhunterParty, manhunterParty.CurrentSettlement, out var itemRosterElement, out var price);
                if (itemRosterElement.EquipmentElement.Item != null)
                {
                    if(price <= manhunterParty.PartyTradeGold)
                    {
                        SellItemsAction.Apply(manhunterParty.CurrentSettlement.Party, manhunterParty.Party, itemRosterElement, 1);
                    }
                    
                }
                else
                {
                    break;
                }
            }
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
                party.PartyTradeGold -= (int)cost;
                //manhunterPartyComponent.Gold -= (int)cost;
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
            //MakePeaceAction.Apply(Hero.MainHero.MapFaction, allManhunterParties.ToList()[0].ActualClan);
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





        [CommandLineFunctionality.CommandLineArgumentFunction("send_manhunters_after_player", "manhunters")]
        private static string SendManhuntersAfterPlayerCommand(List<string> strings)
        {
            Settlement nearestTown = SettlementHelper.FindNearestTown(settlement => true);
            Hero leader = nearestTown.OwnerClan.Leader;

            Settlement manhunterSettlement = leader.HomeSettlement;


            Hero manhunterHero = HeroCreator.CreateSpecialHero(CharacterObject.Find(ManhunterCharacterStringId), faction: Clan.All.Where(clan => clan.StringId == ManhunterClanStringId).First());

            TextObject partyName = new TextObject(leader.Name.ToString() + "'s Manhunter Party");
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(partyName.ToStringWithoutClear(), manhunterHero, manhunterSettlement.GatePosition, 2, manhunterSettlement);
            manhunterMobileParty.SetCustomName(partyName);
            //((ManhunterPartyComponent)manhunterMobileParty.PartyComponent).SentFrom = leader;

            leader.ChangeHeroGold(-200);

            //MBTextManager.SetTextVariable("betrayed_leader", leader.Name.ToString());

            _betrayedLeaderAndManhunter.Add(manhunterMobileParty, leader);

            return manhunterMobileParty.Name.ToString() + " spawned from " + manhunterSettlement.Name.ToString();
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("spawn_manhunter_and_bandit", "manhunters")]
        public static string SpawnManhunterAndBandit(List<string> strings)
        {
            Vec2 pos = MobileParty.MainParty.Position2D + new Vec2(1, 1);

            CharacterObject manhunterCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(ManhunterCharacterStringId);

            Hero manhunterClanLeader = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: null);
            

            Clan manhaunterClan = null;

            foreach (Clan clan in Clan.All)
            {
                if (clan.StringId == ManhunterClanStringId)
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
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty("manhunter party test", manhunterHero, pos, 2, randomSettlement);



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
            CharacterObject manhunterCharacter = MBObjectManager.Instance.GetObject<CharacterObject>(ManhunterCharacterStringId);
            Hero manhunterClanLeader = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: null);

            Clan manhaunterClan = null;

            foreach (Clan clan in Clan.All)
            {
                if (clan.StringId == ManhunterClanStringId)
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
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty("manhunter party test", manhunterHero, pos, 2, randomSettlement);
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
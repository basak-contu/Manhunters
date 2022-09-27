using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Conversation;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;

namespace Manhunters
{
    public partial class ManhuntersCampaignBehavior : CampaignBehaviorBase
    {

        private const float ProbabilityOfHidoutSpawn = 0.5f;
        private const float ProbabilityOfSendingManhuntersAfterPlayer = 0.5f;

        private const string ManhunterClanStringId = "cs_manhunters";
        private const string ManhunterCharacterStringId = "manhunter_character";

        private const int ManhunterHireCost = 100;

        private const int BuyFoodForMinDays = 2;
        private const int BuyFoodForMaxDays = 4;

        private CharacterObject _manhunterCharacter;

        private Clan _manhunterClan;

        private Hero _manhunterClanLeader;
        
        private List<MobileParty> _manhunterPartiesCache = new List<MobileParty>();

        private Dictionary<MobileParty, Hero> _manhunterPartiesAndBetrayedLeader =
                     new Dictionary<MobileParty, Hero>();

        private List<Hero> _betrayedHeros = new List<Hero>();

        private MobileParty _manhunterPartyThatCapturedPlayer;

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            CampaignEvents.SettlementEntered.AddNonSerializedListener(this, OnSettlementEntered);
            CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
            CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
            CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            CampaignEvents.HourlyTickPartyEvent.AddNonSerializedListener(this, MobilePartyHourlyTick);
            CampaignEvents.MobilePartyCreated.AddNonSerializedListener(this, MobilePartyCreated);
            CampaignEvents.MobilePartyDestroyed.AddNonSerializedListener(this, OnMobilePartyDestroyed);
            CampaignEvents.OnQuestCompletedEvent.AddNonSerializedListener(this, OnQuestCompleted);
            CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, HeroPrisonerTaken);
        }


        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            CacheManhunterVariables();
            AddDialogs(campaignGameStarter);
            SpawnManhunterMobileParty();
        }

        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero arg3)
        {
            if (mobileParty != null && mobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners)
                {
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners;
                }

                if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementToBuyFood && settlement.IsVillage)
                {
                    manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.BuyingFood;
                }
            }
        }

        private void OnNewGameCreated(CampaignGameStarter obj)
        {
            InitializeManhunterClan();
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

            if (_betrayedHeros.Any())
            {
                foreach(Hero hero in _betrayedHeros.ToList())
                {
                    float chance = MBRandom.RandomFloat;
                    if (chance >= ProbabilityOfSendingManhuntersAfterPlayer)
                    {
                        SendManhuntersAfterPlayer(hero);
                        _betrayedHeros.Remove(hero);
                    }
                    else
                    {
                        float chanceOfNotSending = MBRandom.RandomFloat;
                        if(chanceOfNotSending < 0.3)
                        {
                            _betrayedHeros.Remove(hero);
                        }
                    }
                }
            }
        }

        private void OnHourlyTick()
        {
            if (!Hero.MainHero.IsPrisoner && Clan.PlayerClan.IsAtWarWith(_manhunterClan))
            {
                if (_manhunterPartyThatCapturedPlayer != null && _manhunterPartiesAndBetrayedLeader.ContainsKey(_manhunterPartyThatCapturedPlayer))
                {
                    if(_manhunterPartyThatCapturedPlayer.MapEvent == null)
                    {
                        _manhunterPartiesAndBetrayedLeader.Remove(_manhunterPartyThatCapturedPlayer);
                        MakePeaceAction.Apply(Clan.PlayerClan, _manhunterClan);
                    }
                }
            } 
        }

        private void MobilePartyHourlyTick(MobileParty mobileParty)
        {
            if (mobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                if (mobileParty.MemberRoster.TotalManCount < ManhunterPartyComponent.MinPartySize && mobileParty.MapEvent == null)
                {
                    mobileParty.IsActive = false;
                    DestroyPartyAction.Apply(null, mobileParty);
                }

                else{

                    if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.BuyingFood)
                    {
                        BuyFood(mobileParty, manhunterPartyComponent);
                        manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                    }

                    if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners)
                    {
                        SellPrisonersAction.ApplyForAllPrisoners(mobileParty, mobileParty.PrisonRoster, mobileParty.CurrentSettlement, true);
                        manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                    }

                    else
                    {
                        TakeAction(mobileParty, manhunterPartyComponent);
                    }
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
                _manhunterPartiesCache.Remove(destroyedParty);
                if(destroyerParty?.MobileParty == MobileParty.MainParty)
                {
                    MakePeaceAction.Apply(_manhunterClan, Clan.PlayerClan);
                }
            }

            if (destroyerParty != null)
            {
                if (destroyerParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
                {
                    destroyerParty.MemberRoster.RemoveIf((obj) => obj.Character.StringId != ManhunterCharacterStringId );
                }
            }
        }

        private void OnQuestCompleted(QuestBase questBase, QuestBase.QuestCompleteDetails questCompleteDetails)
        {
            if (questCompleteDetails == QuestBase.QuestCompleteDetails.FailWithBetrayal)
            {
                _betrayedHeros.Add(questBase.QuestGiver);
            }
        }

        private void HeroPrisonerTaken(PartyBase partyBase, Hero hero)
        {
            if (partyBase?.MobileParty?.PartyComponent is ManhunterPartyComponent && hero == Hero.MainHero)
            {
                _manhunterPartyThatCapturedPlayer = partyBase.MobileParty;
            }
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
                        .Condition(give_bribe_condition)
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

        private void InitializeManhunterClan()
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

            Settlement randomSettlement;
            float chance = MBRandom.RandomFloat;
            if(chance > ProbabilityOfHidoutSpawn)
            {
                randomSettlement = SettlementHelper.FindRandomHideout(settlement => true);
            }
            else
            {
                randomSettlement = SettlementHelper.FindRandomSettlement(settlement => !settlement.IsHideout);
            }
            Hero manhunterHero = HeroCreator.CreateSpecialHero(_manhunterCharacter, faction: _manhunterClan);
            
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty("manhunter_party_" + _manhunterPartiesCache.Count.ToString(), manhunterHero, randomSettlement.GatePosition, 2, randomSettlement);
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
        
        private bool DoesPartyHaveGoldForFood(MobileParty party, Settlement settlement)
        {
            var afforableFoods = settlement.ItemRoster.Where(item => item.EquipmentElement.Item.IsFood &&
            (party.PartyTradeGold > settlement.Village?.GetItemPrice(item.EquipmentElement.Item) || party.PartyTradeGold > settlement.Town?.GetItemPrice(item.EquipmentElement.Item)));
            if (afforableFoods.Any())
            {
                return true;
            }
            return false;
        }

        private bool talking_to_neutral_manhunter_condition()
        {
            MobileParty conversationParty = MobileParty.ConversationParty;
            if (conversationParty?.PartyComponent is ManhunterPartyComponent manhunterPartyComponent
                && !_manhunterPartiesAndBetrayedLeader.ContainsKey(conversationParty))
            {
                return true;
            }
            return false;
        }

        private bool talking_to_enemy_manhunters_condition()
        {
            MobileParty conversationParty = MobileParty.ConversationParty;
            if(conversationParty?.PartyComponent is ManhunterPartyComponent manhunterPartyComponent
                && _manhunterPartiesAndBetrayedLeader.ContainsKey(conversationParty))
            {
                MBTextManager.SetTextVariable("BETRAYED_LEADER", _manhunterPartiesAndBetrayedLeader[conversationParty]);
                MBTextManager.SetTextVariable("BRIBE_AMOUNT", (ManhunterHireCost * 2).ToString());
                return true;
            }
            return false;
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
            if(encounteredParty?.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {
                _manhunterPartiesAndBetrayedLeader.Remove(encounteredParty.MobileParty);
                GiveGoldAction.ApplyForCharacterToParty(Hero.MainHero, encounteredParty, ManhunterHireCost * 2);
                EngageToBanditParty(encounteredParty.MobileParty, manhunterPartyComponent);
                PlayerEncounter.LeaveEncounter = true;
                if (Clan.PlayerClan.IsAtWarWith(_manhunterClan))
                {
                    MakePeaceAction.Apply(_manhunterClan, Clan.PlayerClan);
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
            if(Hero.MainHero.Gold >= ManhunterHireCost * 2 && talking_to_enemy_manhunters_condition())
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

        private void BuyFood(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement currentSettlement = manhunterParty.CurrentSettlement;

            var dailyFoodConsumption = Math.Abs(manhunterParty.FoodChange);

            var days = MBRandom.RandomFloatRanged(BuyFoodForMinDays, BuyFoodForMaxDays);

            if (days * dailyFoodConsumption > manhunterParty.Food)
            {
                var foodRequirement = Math.Ceiling((days * dailyFoodConsumption) - manhunterParty.Food);

                var startIndex = MBRandom.RandomInt(0, currentSettlement.ItemRoster.Count);
                for (int i = startIndex; i < currentSettlement.ItemRoster.Count + startIndex && foodRequirement > 0; i++)
                {
                    var currentIndex = i % currentSettlement.ItemRoster.Count;
                    var itemRosterElement = currentSettlement.ItemRoster.GetElementCopyAtIndex(currentIndex);

                    if (!itemRosterElement.IsEmpty &&
                        itemRosterElement.EquipmentElement.Item.IsFood)
                    {
                        float effectiveAmount = (float)Math.Min(itemRosterElement.Amount, foodRequirement);
                        foodRequirement -= effectiveAmount;
                        SellItemsAction.Apply(currentSettlement.Party, manhunterParty.Party, itemRosterElement, (int)effectiveAmount);
                    }
                }
            }
        }
    }
}
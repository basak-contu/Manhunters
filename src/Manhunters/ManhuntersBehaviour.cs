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

namespace Manhunters
{
    internal class ManhuntersBehaviour : CampaignBehaviorBase
    {
        private CharacterObject manhunterCharacter;
        private Hero manhunterClanLeader;

        private Clan manhaunterClan;

        private List<MobileParty> manhunterParties = new List<MobileParty>();

        private MobileParty manhunterPartyForDebug;

        private int probabilityOfHidoutSpawn = 50;
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
        }


        private void OnSessionLaunched(CampaignGameStarter campaignGameStarter)
        {
            CreateManhunterCharacter();
            CreateManhunterClanLeader();
            GetManhunterClan();
            AddDialogs(campaignGameStarter);
        }


        private void OnNewGameCreated(CampaignGameStarter obj)
        {
            CreateManhunterCharacter();
            CreateManhunterClanLeader();
            GetManhunterClan();
            SpawnManhunterMobileParty();
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
                if(mobileParty.MemberRoster.TotalManCount < manhunterPartyComponent.MinPartySize)
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
                manhunterPartyForDebug = SpawnManhunterPartyAtPos(new Vec2(490, 290));
                //CreateOwnedBanditPartyInHideout(new Vec2(490, 290));
            }
            /*
            if (manhunterParties.Count < GetTotalBanditParties())
            {
                SpawnManhunterMobileParty();
            } */
        }
    

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {

            if (attackerParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {             
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
                FactionManager.DeclareWar(clan, manhaunterClan, true);
            }


        }
        private void OnDailyTick()
        {
            
            if (manhunterParties.Count < GetTotalBanditParties())
            {
                SpawnManhunterMobileParty();
            } 
        }


        private void AddDialogs(CampaignGameStarter campaignGameStarter)
        {

            campaignGameStarter.AddDialogLine("manhunter_dialog_1",
                    "start", "close_window",
                     "{=*}We are manhunters, law enforcers and we hunt the Looters and other Bandits.",
                    is_talkint_to_manhunter,
                    manhunter_encounter_consequence);
        }



        private void CreateManhunterClanLeader()
        {
            manhunterClanLeader = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: manhaunterClan);
        }


        private void GetManhunterClan()
        {
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

            manhaunterClan.IsRebelClan = true;
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
            if(chance > probabilityOfHidoutSpawn)
            {
                randomSettlement = SettlementHelper.FindRandomHideout(settlement => true);
            }
            else
            {
                randomSettlement = SettlementHelper.FindRandomSettlement(settlement => !settlement.IsHideout);
            }
            Hero manhunterHero = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: manhaunterClan);
            
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(manhunterParties.Count.ToString(), manhunterHero, randomSettlement.Position2D, 2, randomSettlement, manhunterHero);
            //manhunterMobileParty.GetNumDaysForFoodToLast
            
            manhunterParties.Add(manhunterMobileParty);
        }

        // For testing
        private MobileParty SpawnManhunterPartyAtPos(Vec2 pos)
        {
            int randomSettlementIndex = MBRandom.RandomInt(0, Settlement.All.Count);
            Settlement randomSettlement = Settlement.All[randomSettlementIndex];
            Hero manhunterHero = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: manhaunterClan);
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(manhunterParties.Count.ToString(), manhunterHero, pos, 2, randomSettlement, manhunterHero);
            manhunterParties.Add(manhunterMobileParty);
            return manhunterMobileParty;
        }


        public void CreateManhunterCharacter()
        {
            manhunterCharacter = MBObjectManager.Instance.GetObject<CharacterObject>("manhunter_character");
        }

        public int GetTotalBanditParties()
        {
            return MobileParty.AllBanditParties.Count;
        }

        private void GoToTownToSellPrisoners(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement nearestTown = SettlementHelper.FindNearestTown(settlement => true);
            InformationManager.DisplayMessage(new InformationMessage(manhunterParty.LeaderHero.Name.ToString() + " party is " + "GOING TO " + nearestTown.Name.ToString() + " TO SELL PRISONERS"));
            manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners;
            manhunterParty.SetMoveGoToSettlement(nearestTown);
        }


        private void GoToVillageToBuyFood(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            Settlement nearestVillage = SettlementHelper.FindNearestVillage(settlement => true);
            InformationManager.DisplayMessage(new InformationMessage(manhunterParty.LeaderHero.Name.ToString() + " party is " + "GOING TO " + nearestVillage.Name.ToString() + " TO BUY FOOD"));
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

        public void EngageToBanditParty(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            MobileParty targetBanditParty = FindNearestBanditParty(manhunterParty);
            if (targetBanditParty != null)
            {
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingToBandits;
                manhunterParty.SetMoveEngageParty(targetBanditParty);
                InformationManager.DisplayMessage(new InformationMessage(manhunterParty.LeaderHero.Name.ToString() + " party is " + "ENGAGIN TO BANDIT PARTY"));
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
                && manhunterPartyComponent.State != ManhunterPartyComponent.ManhunterPartyState.GoingToSettlementForSellingPrisoners)
            {
                EngageToBanditParty(manhunterParty, manhunterPartyComponent);
            }

            if(manhunterParty.PrisonRoster.TotalManCount > 0)
            {
                GoToTownToSellPrisoners(manhunterParty, manhunterPartyComponent);
            }

            /*
            if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners)
            {
                return;
            }

            if(manhunterParty.GetNumDaysForFoodToLast() <= 1)
            {
                GoToVillageToBuyFood(manhunterParty, manhunterPartyComponent);
            }

            if (manhunterParty.PrisonRoster.TotalManCount > 0)
            {
                GoToTownToSellPrisoners(manhunterParty, manhunterPartyComponent);
            }

            if (manhunterParty.PrisonRoster.TotalManCount == 0)
            {

                MobileParty targetBanditParty = FindNearestBanditParty(manhunterParty);
                if (targetBanditParty != null)
                {
                    manhunterParty.SetMoveEngageParty(targetBanditParty);
                }
            } */
        }

        private void SendManhunterBehindPlayer()
        {
            
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


        private bool is_talkint_to_manhunter()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;

            if (Hero.OneToOneConversationHero == null) return false;

            if (Hero.OneToOneConversationHero.Clan.StringId == "cs_manhunters") return true;

            else
            {
                return false;
            }
        }

        private void manhunter_encounter_consequence()
        {
            PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
            if (encounteredParty != null)
            {
                PlayerEncounter.LeaveEncounter = true;
            }
        }

        private void BuyFoodForNDays(MobileParty party, ManhunterPartyComponent manhunterPartyComponent, float daysMin, float daysMax)
        {

            InformationManager.DisplayMessage(new InformationMessage("BEFORE BUYING FOOD"));
            foreach (var item in party.ItemRoster)
            {
                InformationManager.DisplayMessage(new InformationMessage(item.GetType().Name.ToString()));
            }


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

            InformationManager.DisplayMessage(new InformationMessage("AFTER BUYING FOOD"));
            foreach (var item in party.ItemRoster)
            {
                InformationManager.DisplayMessage(new InformationMessage(item.GetType().Name.ToString()));
            }
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
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty("manhunter party test", manhunterHero, pos, 2, randomSettlement, manhunterHero);



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
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty("manhunter party test", manhunterHero, pos, 2, randomSettlement, manhunterHero);
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

      
    }
}
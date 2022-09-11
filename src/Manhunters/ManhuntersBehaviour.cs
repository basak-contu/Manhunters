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

namespace Manhunters
{
    internal class ManhuntersBehaviour : CampaignBehaviorBase
    {
        private CharacterObject manhunterCharacter;
        private Hero manhunterClanLeader;

        private Clan manhaunterClan;

        private List<MobileParty> manhunterParties = new List<MobileParty>();

        private MobileParty manhunterPartyForDebug;

        private TroopRoster prisonersToAdd;

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
                CreateOwnedBanditPartyInHideout(new Vec2(490, 290));
            }
        }

        private void OnMapEventStarted(MapEvent mapEvent, PartyBase attackerParty, PartyBase defenderParty)
        {

            if (attackerParty.MobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {             
                prisonersToAdd = TroopRoster.CreateDummyTroopRoster();
                prisonersToAdd.Add(defenderParty.PrisonRoster);
                manhunterPartyComponent.potentialPrisoners.Add(defenderParty.PrisonRoster);
            }

        }

        private void DeclareWarBetweenManhuntersAndBandits()
        {
            foreach (Clan clan in Clan.BanditFactions)
            {
                FactionManager.DeclareWar(clan, manhaunterClan);
                FactionManager.DeclareWar(manhaunterClan, clan);
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
            DeclareWarBetweenManhuntersAndBandits();
        }

        private void OnSettlementEntered(MobileParty mobileParty, Settlement settlement, Hero arg3)
        {
            if (mobileParty != null && mobileParty.PartyComponent is ManhunterPartyComponent manhunterPartyComponent)
            {             
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners;
                SellPrisoners(mobileParty, manhunterPartyComponent);              
                manhunterPartyComponent.State = ManhunterPartyComponent.ManhunterPartyState.EngagingBandits;
            }
        }

        public override void SyncData(IDataStore dataStore)
        {

        }


        private void SpawnManhunterMobileParty()
        {
            int randomSettlementIndex = MBRandom.RandomInt(0, Settlement.All.Count);
            Settlement randomSettlement = Settlement.All[randomSettlementIndex];

            Hero manhunterHero = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: manhaunterClan);
            
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty(manhunterParties.Count.ToString(), manhunterHero, randomSettlement.Position2D, 2, randomSettlement, manhunterHero);
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

        private void GoToSettlementToSellPrisoners(MobileParty manhunterParty)
        {
            Settlement nearestSettlement = Settlement.GetFirst;
            List<Settlement> nearSettlements = Settlement.FindSettlementsAroundPosition(manhunterParty.Position2D, 100).OrderBy(x => x.GetPosition2D.Distance(manhunterParty.Position2D)).ToList();
            if (nearSettlements.Count > 0)
            {
                nearestSettlement = nearSettlements[0];
            }
            manhunterParty.SetMoveGoToSettlement(nearestSettlement);
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


        // For testing
        public static MobileParty CreateOwnedBanditPartyInHideout(Vec2 pos, int initialGold = 300, bool isBoss = false)
        {
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
            MobileParty banditParty = BanditPartyComponent.CreateBanditParty("takehideouts_party", banditClan, hideout, isBoss); 

            TroopRoster memberRoster = new TroopRoster(banditParty.Party);
            CharacterObject troop = banditClan.Culture.BanditChief;
            CharacterObject prisoner = banditClan.Culture.BasicTroop;
            memberRoster.AddToCounts(troop, MBRandom.RandomInt(6, 14));

            TroopRoster prisonerRoster = new TroopRoster(banditParty.Party);
            prisonerRoster.AddToCounts(prisoner, 10);


            banditParty.InitializeMobilePartyAtPosition(memberRoster, prisonerRoster, hideout.Settlement.Position2D);
            banditParty.Position2D = pos;
            banditParty.InitializePartyTrade(initialGold);
            banditParty.Party.Visuals.SetMapIconAsDirty();

            return banditParty;
        }

        public void TakePrisoners(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            if (manhunterPartyComponent == null || manhunterParty == null)
            {
                return;
            }
            else
            {
                manhunterParty.PrisonRoster.Add(manhunterPartyComponent.potentialPrisoners);
                manhunterPartyComponent.potentialPrisoners = TroopRoster.CreateDummyTroopRoster();
            }
        }

        public MobileParty FindNearestBanditParty(MobileParty manhunterParty)
        {

            var banditParties = MobileParty.AllBanditParties.OrderBy(x => x.GetPosition2D.Distance(manhunterParty.Position2D)).ToList();
            if(banditParties.Count > 0)
            {
                return banditParties[0];
            }
            return null;
        }

        public void TakeAction(MobileParty manhunterParty, ManhunterPartyComponent manhunterPartyComponent)
        {
            if (manhunterPartyComponent.State == ManhunterPartyComponent.ManhunterPartyState.SellingPrisoners)
            {
                return;
            }

            if (manhunterParty.PrisonRoster.TotalManCount > 0)
            {
                GoToSettlementToSellPrisoners(manhunterParty);
            }

            if (manhunterParty.PrisonRoster.TotalManCount == 0)
            {

                MobileParty targetBanditParty = FindNearestBanditParty(manhunterParty);
                if (targetBanditParty != null)
                {
                    manhunterParty.SetMoveEngageParty(targetBanditParty);
                }
            }
        }

        private void SellPrisoners(MobileParty party, ManhunterPartyComponent component)
        {
            if (party.PrisonRoster.TotalManCount > 0)
            {
                var gold = 0;
                foreach (var prisoner in party.PrisonRoster.GetTroopRoster())
                {
                    gold += Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(prisoner.Character);
                }

                SellPrisonersAction.ApplyForAllPrisoners(party, party.PrisonRoster, party.CurrentSettlement, false);
                component.Gold += gold;
                party.PartyTradeGold += gold;
            }
        }
    }
}
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.ObjectSystem;

namespace Manhunters
{
    public static class ManhunterCheats
    {

        [CommandLineFunctionality.CommandLineArgumentFunction("remove_food_from_manhunters", "manhunters")]
        private static string RemoveManhuntersFood(List<string> strings)
        {
            var allManhunterParties = MobileParty.All.Where(x => x.PartyComponent is ManhunterPartyComponent);

            foreach (MobileParty party in allManhunterParties.ToList())
            {
                foreach (ItemRosterElement item in party.ItemRoster)
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

        [CommandLineFunctionality.CommandLineArgumentFunction("spawn_manhunter_and_bandit", "manhunters")]
        public static string SpawnManhunterAndBandit(List<string> strings)
        {
            Vec2 pos = MobileParty.MainParty.Position2D + new Vec2(1, 1);

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
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty("manhunter party test", manhunterHero, pos, 2, randomSettlement);


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

            Vec2 pos = MobileParty.MainParty.Position2D;

            int randomSettlementIndex = MBRandom.RandomInt(0, Settlement.All.Count);
            Settlement randomSettlement = Settlement.All[randomSettlementIndex];
            Hero manhunterHero = HeroCreator.CreateSpecialHero(manhunterCharacter, faction: manhaunterClan);
            MobileParty manhunterMobileParty = ManhunterPartyComponent.CreateManhunterParty("manhunter party test", manhunterHero, pos, 2, randomSettlement);
            return "spawned manhunter party";
        }

        [CommandLineFunctionality.CommandLineArgumentFunction("spawn_bandit_party", "manhunters")]
        public static string SpawnBanditPartyCommand(List<string> strings)
        {
            Vec2 pos = MobileParty.MainParty.Position2D;

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
            if(!Campaign.Current.QuestManager.Quests.Where(q => q.QuestGiver != null).Any())
            {
                return "there is no suitable quest";
            }
            QuestBase questBase = Campaign.Current.QuestManager.Quests.Where(q => q.QuestGiver != null).First();
            questBase.CompleteQuestWithBetrayal();
            return questBase.QuestGiver.Name.ToString() + " sent manhunters from " + questBase.QuestGiver.HomeSettlement.Name.ToString();
        }

    }
}

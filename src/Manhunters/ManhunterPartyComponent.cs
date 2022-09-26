using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.ObjectSystem;
using TaleWorlds.SaveSystem;

namespace Manhunters
{
    public class ManhunterPartySaveableTypeDefiner : SaveableTypeDefiner
    {
        public ManhunterPartySaveableTypeDefiner() : base(2_543_135)
        {
        }

        protected override void DefineClassTypes()
        {
            base.DefineClassTypes();
            AddClassDefinition(typeof(ManhunterPartyComponent), 1);
        }

        protected override void DefineContainerDefinitions()
        {
            base.DefineContainerDefinitions();
            ConstructContainerDefinition(typeof(List<ManhunterPartyComponent>));
        }
    }

    public partial class ManhunterPartyComponent : WarPartyComponent
    {
        
        public enum ManhunterPartyState
        {
            Invalid,
            GoingToSettlementForSellingPrisoners,
            GoingToSettlementToBuyFood,
            EngagingToBandits,
            SellingPrisoners,
            BuyingFood,
            EngagingToPlayer
        }

        [CachedData]
        private TextObject _cachedName;

        public override Hero PartyOwner => Owner;

        [SaveableProperty(50)]
        public TextObject _customName { get; set; }

        public override TextObject Name => _cachedName ?? (_cachedName = ((Owner != null) ? GetPartyName() : new TextObject("{=!}unnamedMobileParty")));

        public override Settlement HomeSettlement => Owner.HomeSettlement;

        [SaveableProperty(20)]
        public Hero Owner
        {
            get;
            private set;
        }

        public const int MinPartySize = 5;

        private const string ManhunterClanStringId = "cs_manhunters";
        private const string ManhunterCharacterStringId = "manhunter_character";

        public ManhunterPartyState State { get; set; } = ManhunterPartyState.Invalid;
   

        public static MobileParty CreateManhunterParty(string stringId, Hero hero, Vec2 position, float spawnRadius, Settlement spawnSettlement)
        {
            return MobileParty.CreateParty(hero.CharacterObject.StringId + stringId, new ManhunterPartyComponent(hero), delegate (MobileParty mobileParty)
            {
                ((ManhunterPartyComponent)mobileParty.PartyComponent).InitializeManhunterPartyProperties(mobileParty, position, spawnRadius, spawnSettlement);
                
            });
        }

        protected internal ManhunterPartyComponent(Hero owner)
        {
            Owner = owner;
        }      


        public override void ClearCachedName()
        {
            _cachedName = null;
        }

        private TextObject GetPartyName()
        {
            TextObject textObject = new TextObject("manhunter party");
            textObject.SetCharacterProperties("TROOP", CharacterObject.Find(ManhunterCharacterStringId));
            return textObject;
        }

        private void InitializeManhunterPartyProperties(MobileParty mobileParty, Vec2 position, float spawnRadius, Settlement spawnSettlement)
        {
            mobileParty.ActualClan = Clan.FindFirst(clan => clan.StringId == ManhunterClanStringId);

            EquipmentElement saddleHorse = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_horse"));
            EquipmentElement sumpterHorse = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"));

            

            TroopRoster memberRoster = new TroopRoster(mobileParty.Party);

            CharacterObject troopsWithHorses = CharacterObject.Find(ManhunterCharacterStringId);
            troopsWithHorses.Equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Horse, 
                MBRandom.RandomInt(0, 2) == 0 ? saddleHorse : sumpterHorse);

            memberRoster.AddToCounts(troopsWithHorses, MBRandom.RandomInt(MinPartySize, (int)new ManhunterPartySizeLimitModel().GetPartyMemberSizeLimit(mobileParty.Party).ResultNumber));
            TroopRoster prisonerRoster = new TroopRoster(mobileParty.Party);
            ManhunterPartySizeLimitModel manhunterPartySizeLimitModel = new ManhunterPartySizeLimitModel();
            mobileParty.InitializeMobilePartyAroundPosition(memberRoster, prisonerRoster, position, spawnRadius, 0f);

            mobileParty.Aggressiveness = MBRandom.RandomFloatRanged(0f, 1f);

            mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Grain, MBRandom.RandomInt(15, 30)));

            
            if (spawnSettlement != null)
            {
                mobileParty.Ai.SetAIState(AIState.VisitingNearbyTown);
                mobileParty.SetMoveGoToSettlement(spawnSettlement);
            }
            mobileParty.Ai.SetAIState(AIState.VisitingVillage);
           
        }
    }

   
}

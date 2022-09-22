﻿using System;
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

        [SaveableField(30)]
        private Hero _leader;

        public override Hero PartyOwner => Owner;

        [SaveableProperty(50)]
        public TextObject _customName { get; set; }

        public override TextObject Name => _cachedName ?? (_cachedName = ((Owner != null) ? GetPartyName() : new TextObject("{=!}unnamedMobileParty")));

        public override Settlement HomeSettlement => Owner.HomeSettlement;

        //public bool DidEncounteredWithPlayer = false;

        //public Hero SentFrom;

        [SaveableProperty(20)]
        public Hero Owner
        {
            get;
            private set;
        }

        public override Hero Leader => _leader;

        public int MinPartySize { get; set; } = 5;
        public int MaxPartySize { get; set; } = 25;

        //public TroopRoster potentialPrisoners;

        //public bool IsAfterPlayer = false;

        public ManhunterPartyState State { get; set; } = ManhunterPartyState.Invalid;

        /*
        internal static void AutoGeneratedStaticCollectObjectsManhunterPartyComponent(object o, List<object> collectedObjects)
        {
            ((ManhunterPartyComponent)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
        }

        protected override void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
        {
            base.AutoGeneratedInstanceCollectObjects(collectedObjects);
            collectedObjects.Add(_leader);
            collectedObjects.Add(Owner);
        }
    
        
        internal static object AutoGeneratedGetMemberValueOwner(object o)
        {
            return ((ManhunterPartyComponent)o).Owner;
        }

        
        internal static object AutoGeneratedGetMemberValue_leader(object o)
        {
            return ((ManhunterPartyComponent)o)._leader;
        }
        */

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
            //_leader = leader;
            //this.IsAfterPlayer = isAfterPlayer;
            //potentialPrisoners = TroopRoster.CreateDummyTroopRoster();
        }      


        public override void ClearCachedName()
        {
            _cachedName = null;
        }

        private TextObject GetPartyName()
        {
            TextObject textObject = new TextObject("manhunter party");
            textObject.SetCharacterProperties("TROOP", Owner.CharacterObject);    
            return textObject;
        }

        private void InitializeManhunterPartyProperties(MobileParty mobileParty, Vec2 position, float spawnRadius, Settlement spawnSettlement)
        {
            mobileParty.AddElementToMemberRoster(Owner.CharacterObject, 1, insertAtFront: true);
            mobileParty.ActualClan = Owner.Clan;

            EquipmentElement saddleHorse = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("saddle_horse"));
            EquipmentElement sumpterHorse = new EquipmentElement(MBObjectManager.Instance.GetObject<ItemObject>("sumpter_horse"));

            

            TroopRoster memberRoster = new TroopRoster(mobileParty.Party);

            CharacterObject troopsWithHorses = MBObjectManager.Instance.GetObject<CharacterObject>("manhunter_character");
            troopsWithHorses.Equipment.AddEquipmentToSlotWithoutAgent(EquipmentIndex.Horse, 
                MBRandom.RandomInt(0, 2) == 0 ? saddleHorse : sumpterHorse);

            memberRoster.AddToCounts(troopsWithHorses, MBRandom.RandomInt(MinPartySize, MaxPartySize));

            TroopRoster prisonerRoster = new TroopRoster(mobileParty.Party);

            mobileParty.InitializeMobilePartyAroundPosition(memberRoster, prisonerRoster, position, spawnRadius, 0f);

            mobileParty.Aggressiveness = 0.9f + 0.1f * (float)Owner.GetTraitLevel(DefaultTraits.Valor) - 0.05f * (float)Owner.GetTraitLevel(DefaultTraits.Mercy);
            mobileParty.ItemRoster.Add(new ItemRosterElement(DefaultItems.Grain, MBRandom.RandomInt(15, 30)));
            //mobileParty.ItemRoster.Add(new ItemRosterElement)

            Owner.PassedTimeAtHomeSettlement = (int)(MBRandom.RandomFloat * 100f);
            
            if (spawnSettlement != null)
            {
                mobileParty.Ai.SetAIState(AIState.VisitingNearbyTown);
                mobileParty.SetMoveGoToSettlement(spawnSettlement);
            }
            mobileParty.Ai.SetAIState(AIState.VisitingVillage);
           
        }
    }

   
}

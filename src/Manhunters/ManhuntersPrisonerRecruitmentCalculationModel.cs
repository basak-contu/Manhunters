using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Recruitment;
using TaleWorlds.Library;

namespace Manhunters
{
    class ManhuntersPrisonerRecruitmentCalculationModel : DefaultPrisonerRecruitmentCalculationModel
    {
        public override bool ShouldPartyRecruitPrisoners(PartyBase party)
        {
            return false;
        }

        public override bool IsPrisonerRecruitable(PartyBase party, CharacterObject character, out int conformityNeeded)
        {
            if(party.MobileParty.PartyComponent is ManhunterPartyComponent manhunter)
            {
                conformityNeeded = 0;
                return false;
            }

            return base.IsPrisonerRecruitable(party, character, out conformityNeeded);
        }
    }
}

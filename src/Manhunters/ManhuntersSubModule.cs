using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party.PartyComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace Manhunters
{
    class ManhuntersSubModule : MBSubModuleBase
    {
        
        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);

            if (!(game.GameType is Campaign))
            {
                return;
            }

            if (game.GameType is Campaign)
            {
                var initializer = (CampaignGameStarter)gameStarterObject;

                initializer.AddBehavior(new ManhuntersBehaviour());
            }
        }
    }


}

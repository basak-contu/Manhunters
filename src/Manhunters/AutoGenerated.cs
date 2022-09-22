﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaleWorlds.CampaignSystem.Party.PartyComponents;

namespace Manhunters
{
    public partial class ManhunterPartyComponent : WarPartyComponent
    {
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
    }
}

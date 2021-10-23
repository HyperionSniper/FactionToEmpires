using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace empireMaker
{
    public partial class EmpireMaker
    {
        public enum Conversion
        {
            noConversion,
            empire,
            bugFix,
            forceConversion
        }

        public enum Relationship
        {
            basic,
            empire,
            ally,
            neutral,
            enemy,
            permanentEnemy
        }
        public enum WantsApparel
        {
            off,
            forcedRoyal,
            basic
        }
        public enum EmpireTechLevel
        {
            neolithic = 2,
            medieval = 3,
            industrial = 4,
            spacer = 5,
            ultra = 6,
        }

        public enum EmpireArchetype
        {
            Neolithic, // all neolithic
            Medieval, // all medieval
            IndustrialRaider, // industrial raider
            IndustrialOutlander, // industrial non-raider
            SpacerRaider, // spacer raider
            Spacer, // spacer non-raider
            Ultra // ultra
        }
    }
}

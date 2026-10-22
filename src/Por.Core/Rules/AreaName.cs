using System;

namespace Por.Core
{
    public static partial class AreaName
    {
        private const byte LAST_SCRIPT = 29;

        private static readonly string[] names = new string[LAST_SCRIPT + 1]
        {
            "NEW PHLAN",
            "BUCCANEER'S BASE",
            "TEXTILE HOUSE",
            "VALJEVO CASTLE",
            "VALJEVO CASTLE NE",
            "VALJEVO HEDGE MAZE",
            "VALJEVO CASTLE SW",
            "VALJEVO TOWER",
            "PHLAN CITY HALL",
            "STOJANOW GATE",
            "VALHINGEN GRAVES",
            "TRAINING HALL",
            "UNKNOWN AREA",
            "KOBOLD CAVES",
            "KOVEL MANSION",
            "MENDOR'S LIBRARY",
            "LIZARD MAN KEEP",
            "NOMAD CAMP",
            "PODAL PLAZA",
            "SILVER DRAGON DEN",
            "SLUMS",
            "SOKAL KEEP",
            "SORCERER'S ISLAND",
            "YARASH'S PYRAMID",
            "WEALTHY AREA",
            "WESTERN WILDERNESS",
            "CENTRAL WILDERNESS",
            "EASTERN WILDERNESS",
            "ZHENTIL OUTPOST",
            "KUTO'S WELL"
        };

        public static string AreaFor(byte scriptId, byte mapId)
        {
            if (scriptId > LAST_SCRIPT)
            {
                return "UNKNOWN AREA";
            }

            string result = names[scriptId];

            if (scriptId == 3 && mapId == 3)
            {
                result = "VALJEVO CASTLE NW";
            }
            if (scriptId == 3 && mapId == 5)
            {
                result = "VALJEVO CASTLE SE";
            }
            if (scriptId == 16 && mapId == 30)
            {
                result = "LIZARD CATACOMBS";
            }
            if (scriptId == 29 && mapId == 32)
            {
                result = "KUTO'S CATACOMBS";
            }
            if (scriptId == 24 && mapId == 24)
            {
                result = "TEMPLE OF BANE";
            }

            return result;
        }

        public static string CurrentArea()
        {
            byte scriptId = unchecked((byte)GDVars.zone0.previousScriptId);
            byte mapId = GDVars.zone0.mapId.lo;
            return AreaFor(scriptId, mapId);
        }
    }
}

// Desktop platform gaps: DOS facilities with no managed equivalent. Each gap shows an explicit in-game message and returns
// normally; no measurement or pointer value is invented. See docs/WIRING-REVIEW-2026-10-22.md for the per-item decision.
using static Por.Core.Types;
namespace Por.Core
{
    public static class PlatformGapBindings
    {
        public const string Message = "Not available in this desktop build";

        public static void Bind()
        {
            // memshow.pas reads Overlay.OvrGetBuf/OvrGetRetry and the TP heap's MemAvail/MaxAvail. The CLR has no overlay
            // manager and no TP heap, so the STING Ctrl-Y display shows the message instead of numbers.
            MemShow.UnavailableMessage = Message;
            // A packed equipped[] slot holds a DOS heap pointer from the session that wrote the record. It names no object in
            // this process. All 172 shipped MON*CHA records hold zero there and save loading clears the field, so this runs
            // only for altered data. The slot stays empty; CharHlp.LinkEquipped rebuilds slots from the item list.
            if (CharacterRecordCodec.ResolveEquippedItem == null) CharacterRecordCodec.ResolveEquippedItem = UnresolvedEquippedPointer;
        }

        internal static TItemRec UnresolvedEquippedPointer(uint pointer)
        {
            TextRend.DisplayAndPause(15, Message);
            return null;
        }
    }
}

// FILE: src/Por.Core/GdVars.cs
using System;
using System.Collections.Generic;
using System.IO;

namespace Por.Core
{
    public static partial class GDVars
    {
        // The video driver GameBoot installed for the video mode (Types.TVideoDriver).
        public static Types.TVideoDriver video = new Types.TVideoDriver();
        // the sound driver GameBoot installed
        public static Types.TSoundDriver sound = new Types.TSoundDriver();
        // Timed effects.
        // Creatures whose timed effects need processing, by list ordinal.
        public static byte[] effFlags = new byte[Types.ARENA_SLOTS + 1];
        // Play session
        // GameBoot writes 0/1, Intro tests zero
        public static bool fullIntro;
        // word bank
        public static Types.TGbvmZone0State zone0;
        // GBVM zone 1, indexed directly by GBVM address (the zone-1 GBVM_ constants)
        // word bank
        public static Types.TGbvmZone1 zone1;
        // word bank
        public static Types.TZone2 zone2;
        // script bytes
        public static Types.TZone3 zone3;
        // game speed, 0 = fastest 9 = slowest; a setting in POOLOPT.CFG, and SaveLoad writes it to the GBVM gameSpeed word
        public static byte gameDelay;
        // Five GBVM addresses loaded from the ECL entry header. [1] runs before movement and after on-load; [2] search/Look;
        // [3] pre-camp; [4] camp interruption; [5] on-load
        // ECL entry GBVM addresses
        public static ushort[] scriptEntryAddrs = new ushort[6];
        // ECL instruction pointer into zone3
        public static ushort eclIp;
        // Interface state, one of the IFC_ constants of C_Ifc and Ending
        public static byte ifcState;
        // nonzero suppresses Combat's Continue Battle prompt
        public static byte suppressContinuePrompt;
        // zero disables ViewChar's disk-3 portrait branch
        public static byte portraitsOn;
        // zero skips the close-up load when GameDat5.inEncMenu is nonzero
        public static byte monsterPics;
        // zero makes SoundDrv.PlayMusic do nothing
        public static byte musicOn;
        // the Sound setting: zero starts the game with sound off (soundMode disabled); GameIo.SetSound keeps it in step
        public static byte soundOn;
        // nonzero: PostCbt and SaveLoad.LoadChar take the party's characters off Quick after each fight and on loading
        public static byte quickEnds;
        // the Quick Mode Magic setting: CombatSt starts every fight with quickMagic set from it; Alt-M then turns quickMagic
        // over for that fight only
        public static byte quickMagicOn;
        // the Companion Tools setting: nonzero lets F6 open the Tools screen outside combat (phase 8)
        public static byte companionOn;
        // GameInit clears it; GameIo toggles it on STING Ctrl-D; nonzero enables PrintHpDebug
        public static byte stingHpFlag;
        // Started as START STING: the debug keys and cheats are on. GameBoot sets it before anything reads a key.
        public static bool stingArg;
        // STING Ctrl-L (#612): the script trace log is open; each opcode's mnemonic goes to debugFile (debug.txt), and Ctrl-Z
        // writes the creatures' effects there
        public static bool debugTraceEnabled;
        public static TextWriter debugFile;
        // overlay loads since the program started, counted by GameBoot's OvrReadBuf hook (#612)
        public static int ovrLoads;
        // the overlays load from EMS (GameBoot's OvrInitEMS succeeded) rather than from GAME.OVR on disk
        public static bool ovrInEms;
        // heap bytes held by pictures (Blit.AllocPic less FreePic), and the most they have reached; STING Ctrl-Y shows them
        public static int picBytes;
        public static int picPeak;
        // set when GameIo has written over the bottom line (the STING Ctrl-Y pop-up); MenuCore.HorizontalMenu redraws its
        // menu line and clears it
        public static bool bottomLineTaken;
        // main-menu load handoff; EclVm skips reload, EclMenu clears it
        public static byte loadedSave;
        // Party location type, previous (as below)
        public static byte locTypePrev;
        // Party location type, current
        public static byte gLocType;
        // Uninterrupted resting time when disturbable
        public static ushort restTicks;
        // Portrait head ID.
        public static byte dsCurrentPortraitHead;
        // Portrait body ID.
        public static byte dsCurrentPortraitHead2;
        public static bool partyDefeated;
        // Delay in printing message to message window
        public static byte msgDelayFlag;

        // Video registers, pages and modes; view and icon buffers, dirty spans and row tables
        // Register record handed to Dos.Intr for BIOS video calls; every unit calling the BIOS uses this one record.
        public static Types.TInterruptRegisters dsInterruptRegisters = new Types.TInterruptRegisters();
        // Other EGA page number (always 0)
        public static byte pageNum;
        // Currently displayed EGA page number (always 0)
        public static byte pageMain;
        // BIOS mode queried at init, then the mode active before the latest Blit.SetVideoMode call; RestoreVideoMode returns
        // to it.
        public static byte savedVideoMode;
        // BIOS mode queried at init, then the mode most recently requested through Blit.SetVideoMode.
        public static byte curVideoMode;

        // Off-screen 168x168 view buffer that the compositors draw into and Blit.FlushView copies to the screen; GameInit
        // allocates it.
        public static Types.TImgBuf dsViewImageBufferPtr;
        // Backdrop buffer that Blit.BlitTileClipped fills with the destination bytes it is about to cover (when the save flag
        // is set), so CharHlp can restore the background afterwards.
        public static Types.TImgBuf dsIconImageBufferPtr;
        // Dirty-span tables, one slot per view scanline (21 character rows x 8 = 168); GameInit initializes all four.
        // Nonzero when that view scanline holds changes that Blit.FlushView still has to copy to the screen.
        public static byte[] spanActive = new byte[168];
        // Horizontal byte offset in video memory where the dirty span begins; the screen-side counterpart of spanFirstOfs.
        public static ushort[] spanScreenOfs = new ushort[168];
        // Offset of the first dirty byte in the view buffer's image data. Reset to $FFFF when clean so any new span compares
        // as earlier.
        public static ushort[] spanFirstOfs = new ushort[168];
        // Offset of the last dirty byte (inclusive) in the view buffer's image data. Reset to 0 when clean so any new span
        // compares as later.
        public static ushort[] spanLastOfs = new ushort[168];
        // Video segment for each CGA scanline, precomputed so row writes need no bank arithmetic: even and odd rows interleave
        // between the B800 and BA00 banks.
        public static ushort[] cgaRowSeg = new ushort[200];
        // Offset into the EGA video segment for each scanline; rows are 40 bytes apart.
        public static ushort[] fbRowOfs = new ushort[200];
        // Video segment for each Tandy scanline, precomputed like cgaRowSeg.
        public static ushort[] tandyRowSeg = new ushort[200];
        // Nonzero makes Blit.BlitTileClipped clip against the destination bounds; zero trusts the caller to have kept the tile
        // inside. GameInit initializes it to 1.
        public static byte clipFlag;

        // Sound mode
        // Active sound device; GameBoot maps POOL.CFG line 2: 'T' = 0, 'P' = 1, else 2.
        public static byte soundMode;
        // Copy of soundMode taken at suspend so the configured mode is not lost.
        public static byte soundSaved;
        // Nonzero: SoundDrv.SoundCommand waits a fixed delay after each command, then silences the voices and removes its
        // timer. Zero leaves the timer driver resident for persistent playback.
        public static ushort soundAutoStop;
        // Disks, directories and the DAX buffer
        // current resource disk; saved by SaveLoad
        public static byte virtualDisk;
        // disk saved by DaxIo.PushState for PopState
        public static byte prevVirtualDisk;
        // Save directory from line 4 of POOL.CFG.
        public static string dsSaveDirectory = string.Empty;
        // POOL.CFG line 3, the game directory; gameDir[1] is its drive letter
        public static string gameDir = string.Empty;
        // Flag for suppressing insert disk prompt
        public static bool dsSuppressingInsertDiskFlag;
        // Set by SaveLoad once a save completes; HeapErr and the Exit to DOS loop use it to tell whether a save succeeded.
        public static byte recentSaved;
        // ITEMS catalogue
        // Base item types, indexed by TItemRec.itemType, on the heap; GameInit2 allocates and clears them before reading
        // ITEMS. The last entry, Types.BASE_ITEM_OVERFLOW, stays zero: it keeps the ITEMS read's request for 129 records in
        // bounds (the shipped ITEMS holds 128), and CharHlp.BaseItem gives it for an item type past the table.
        public static Types.TBaseItems baseItems;
        // Party pointers
        // Current creature.
        public static Types.TCharRecord curChar;
        // Pointer to linked list of creatures
        public static Types.TCharRecord creatureList;
        // last missile item added to the treasure list in combat
        public static Types.TItemRec lastMissileItem;
        // Font and text cursor
        // Pointer to font. Each entry in the font table is 8 bytes
        public static Types.TFontArr gameFont;
        // Text cursor shared by TextRend's message window and slow-text output
        // Current char column. (Equivalent of WhereX.)
        public static byte curCol;
        // Current char row. (Equivalent of WhereY.)
        public static byte curRow;
        // Symbol sets and map tile sets
        // the five 8x8 symbol-set images, by set id 0..4: Sym8x8.Load8x8Set loads an 8X8D picture into a slot and Put8x8Sym
        // draws from it
        public static Types.TImgBuf[] dsSymbolSetPtrs = new Types.TImgBuf[5];
        // Pointer to current 8x8 symbol image buffer
        public static Types.TImgBuf dsCurrent8x8SymbolPtr2;
        // 2D map tile set image buffer 1
        public static Types.TImgBuf ds2dMapTilePtr;
        // 2D map tile set image buffer 2; bit 7 of a tile id selects it
        public static Types.TImgBuf ds2dMapTilePtr2;
        // Icon images
        // Animation images for creature, death and cursor icons.
        public static Types.TImgBuf[,] dsIconAnimation = new Types.TImgBuf[26, 2];
        // Combat arena
        // next registration slot, inclusive scan limit
        public static byte arenaCount;
        // icon location and size per slot
        public static Types.TArenaLoc[] arenaLoc = new Types.TArenaLoc[Types.ARENA_SLOTS + 1];
        // view-relative X deltas
        public static sbyte[] gridX = new sbyte[Types.ARENA_SLOTS + 1];
        // view-relative Y deltas
        public static sbyte[] gridY = new sbyte[Types.ARENA_SLOTS + 1];
        // creature per slot
        public static Types.TCharRecord[] creatureArr = new Types.TCharRecord[Types.ARENA_SLOTS + 1];
        // corpses on the arena
        public static Types.TDownedRec[] downed = new Types.TDownedRec[9];
        // append and scan limit; cleared entries leave holes
        public static byte downedCount;
        // allocated combat viewport, borrowed by renderers
        public static Types.TArena arenaPtr;
        // Pathing's potential-target count
        public static byte targetCount;
        // sorted by distance and facing
        public static Types.TTargetList targetList = new Types.TTargetList();

        // Treasure
        // copper through jewelry
        public static Types.TMoney treasureMoney = new Types.TMoney();
        // consumers use PItemRec views
        public static Types.TItemRec treasureItems;
        // Combatants per side
        // combat ends when a side is empty
        public static byte[] teamCount = new byte[2];
        // Effect resolution
        // modified saving-throw roll; signed, so a penalty can take it below 0
        public static short gSaveRoll;
        // effect ID, cleared when immune
        public static byte pendingEffectId;
        // damage before hit-point subtraction
        public static byte pendingDamage;
        // C_Effect.ELEM_*: 1 fire, 2 cold, 4 electricity, 8 magic, 16 acid
        public static byte elementMask;
        // current attack *or* current movement times 2
        public static byte dsCurrentAttackCurrent;
        // spell being cast
        public static byte dsCurrentSpellBeing;
        // dice count of the current damage roll
        public static byte gDamageDice;
        // 1 while calculating movement
        public static byte dsCurrentCreatureCalculatingFlag;
        // suppress targeting
        public static byte dsCurrentSuppressTargetingFlag;
        // Effect lock.
        public static byte effectLock;
        // the spell affects multiple targets
        public static byte gMultiTarget;
        // redraw the inventory menu
        public static byte dsRedrawInventoryMenuFlag;
        // signed -- EffHndlr assigns -1, FxEngine guards >= 0
        public static sbyte gAttackRoll;
        public static byte[] hitsMade = new byte[3];
        // current morale
        public static byte dsCurrentMorale;
        // creature this one is interacting with
        public static Types.TCharRecord dsOtherCreatureCurrentPtr;
        // saving throw type, 0..4
        public static byte gSaveType;

        // Effect dispatch
        // effect handlers, indexed by effect id
        public static Types.TEffectHandler[] effectFns = new Types.TEffectHandler[C_Effect.RECENT_CURE + 1];
        // 3D view
        // the wall types' symbol slices, by wall type 1..15 (View3D.LoadWallSet fills a slot's five)
        public static Types.TWallSlices wallGfx;
        // four 256-byte GEO planes; View3D/Explore views
        public static Types.TGeoMap map3d;
        // Sky and ceiling color after palette mapping.
        public static byte dsColorSkyCeiling;
        // View3D band colors; MapView recomputes them from the ECL color selectors after a script changes them
        // (GameDat5.picRegionW)
        // 3D-view color register A (ceiling/horizon band)
        public static byte dsWallColorA;
        // 3D-view color register B (mid band)
        public static byte dsWallColorB;
        // 3D-view color register C (floor/wall band)
        public static byte dsWallColorC;
        // Map position, wall cache, animation and portrait cache
        // automap shown in place of the 3D view
        public static bool automapOn;
        // Current map column; signed for edge and wrap checks.
        public static sbyte gMapX;
        // Current map row.
        public static sbyte gMapY;
        // Current party facing direction.
        public static byte gFacing;
        // Wall at the current square in the facing direction.
        public static byte wallAhead;
        // Event byte at the party's square; bit 7 is a flag, not a wall
        public static byte eventAtCell;
        public static Types.TWallSetCacheSlot[] dsWallSetCache = new Types.TWallSetCacheSlot[4];
        // the picture animation showing (PIC or SPRIT)
        public static Types.TAnimRec animData = new Types.TAnimRec();
        // HEAD and BODY image caches.
        public static Types.TPortSlot[] dsPortraitCache = new Types.TPortSlot[3];
        // Picture archive and menu state
        // Archive of the cached picture: name and virtual-disk digit, as LoadAnimation opens it.
        public static string picArcName = string.Empty;
        // picture id loaded from picArcName; 255 invalidates the pair
        public static byte picArcId;
        // Zero-based position of the first visible list entry.
        public static short vertTop;
        // Current horizontal menu choice, starting at 1.
        public static byte horizChoice;

        // Spell casting and targeting
        // the cast-target menu: Magic.NoncombatCastTargetMenu, or CbtActs.CombatCastTargetMenu in combat
        public static Types.TCastUI castTargetMenu;
        // Spell handlers indexed by spell ID.
        public static Types.TSpellProc[] spellFns = new Types.TSpellProc[68];
        // Number of targeted creatures
        public static byte spellTargetCount;
        // Selected target creatures.
        public static Types.TCharRecord[] dsTargetCreaturePtr = new Types.TCharRecord[Types.ARENA_SLOTS + 1];
        // Remembered noncombat spell target, initially the caster.
        public static Types.TCharRecord castTarget;
        // combat arena column, 0..49
        public static sbyte gTargetX;
        // combat arena row, 0..24
        public static sbyte gTargetY;
        // Pointer to linked list of Stinking Clouds
        public static Types.TStinkRec stinkHead;
        public static bool usingItem;

        // Rest and healing
        // Remaining rest time as a seven-word GBVM clock; TClock-typed so it passes to Clock.ValidateClock as a var
        // parameter
        public static Types.TClock dsRestingTimeGbvm = new Types.TClock();
        // Time rested today, in five-minute ticks.
        public static ushort healTicks;
        // Suppress the window border.
        public static byte dsSuppressDrawingBorderFlag;

        // Wilderness animation
        // Party icon frame, 0..5: parity selects the icon frame, div 2 the icon animation.
        public static byte dsWildAnimFrame;

        // Door-opening flags
        // Door-opening flags: bash, pick and knock.
        public static byte[] doorOpen = new byte[3];

        // Combat round
        // Combat round counter, 0-indexed
        public static byte roundCounter;
        // Potential-target creature indices.
        public static byte[] dsPotentialTargetCreature = new byte[Types.ARENA_SLOTS + 1];
        // Hostile creatures' hit points, percent remaining
        public static byte hostileHpPct;
        // quick combat magic; toggled by Alt-M
        public static bool quickMagic;
        // Done flag for the acting creature's attacks; combat menus also return their extended-key flag here
        public static bool doneRoundFlag;
        // The combat moving-icon sheet: CombatSt allocates it, CharHlp copies icon facings in and draws from it, Combat frees
        // it
        public static Types.TImgBuf dsMoveIconSheet;
        // ECL colour selectors
        // Script-controlled wall color; script writes request a view redraw.
        public static byte wallColorSel;
        // Script-controlled ceiling and floor color; writes request a view redraw.
        public static byte ceilFloorColorSel;
        // ECL interpreter
        // Top ECL return-stack node.
        public static Types.TEclRet dsEclStackTopPtr;
        // Previous ECL return-stack node.
        public static Types.TEclRet dsEclStackSecondPtr;
        // eq, ne, lt, gt, le, ge; cleared together by FillChar
        public static bool[] cmpFlags = new bool[6];
        // Current ECL opcode
        public static byte curOpcode;
        // Current icon animation slot.
        public static byte gIconAnim;
        // ECL variables and strings
        // Decoded ECL variable types.
        public static byte[] dsGbvmType = new byte[65];
        // High bytes of decoded ECL variables.
        public static byte[] dsGbvmUpperByte = new byte[65];
        // Low bytes of decoded ECL variables.
        public static byte[] dsGbvmLowerByte = new byte[65];
        // Receives the aim result for the bronze-dragon breath attack.
        public static bool breathAim;
        // Script string buffers, on the heap; GameInit2 allocates and clears them.
        public static Types.TGbStrings gbStrings;
        // ECL monster and post-combat state
        // Monster display flags used by the ECL VM; EclVm and EclMenu clear both together
        public static Types.TMonFlags dsMonsterFlags = new Types.TMonFlags();
        // Sound effect selected by a script write (EclVars.WriteGbvm)
        public static ushort sndEffect;
        // Dump the creature at ECL LOAD CHARACTER; set when a script writes 0 to its trigger word (EclMenu.SpecialWriteZ1)
        public static bool dumpCr6C00;
        // Dump the creature at ECL LOAD CHARACTER; set when a script writes 0 to its trigger word (EclMenu.SpecialWriteZ1)
        public static bool dumpCr6B00;
        public static bool duelFlag;

        // ECL flags
        // ECL script ID
        public static byte eclScriptId;
        // Backup of the search-mode bit (C_Gbvm.SEARCH_FLAGS bit 0)
        public static byte dsSearchModeFlag;
        // ECL 3D CLEAR BOX executed flag
        public static byte dsEcl3dClearFlag;
        public static byte protNewLine;
        // ECL currently displaying graphics flag
        public static bool eclGraphics;
        // picture-drawn flag: gates ECL animation
        public static bool picDrawn;
        // Set when EclProtection moves its text into the 3D box (after a CLEAR BOX); EclExit then redraws the borders and
        // clears it
        public static byte protTextShown;
        // ECL displaying monster sprite flag
        public static bool sprShown;
        // 0 when an ECL script has written a message to the bottom text window; Explore.MapMainMenu clears that window on its
        // way out and sets it back to 1
        public static byte bottomMsgCleared;
        // Set when a script changes a view colour selector; MapView then applies them
        public static bool picRegionW;
        // ECL write moved party flag
        public static bool movedParty;
        // Redraw sky/ceiling in 3D view flag
        public static bool redrawSky;
        // Currently in ECL opcode 29 ENCOUNTER MENU flag
        public static byte inEncMenu;

        static GDVars()
        {
            for (int i = 0; i < arenaLoc.Length; i++)
                arenaLoc[i] = new Types.TArenaLoc();
            for (int i = 0; i < downed.Length; i++)
                downed[i] = new Types.TDownedRec();
            for (int i = 0; i < dsWallSetCache.Length; i++)
                dsWallSetCache[i] = new Types.TWallSetCacheSlot();
            for (int i = 0; i < dsPortraitCache.Length; i++)
                dsPortraitCache[i] = new Types.TPortSlot();
        }
    }
}

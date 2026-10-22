// Deterministic inverse of the pinned primary character layout; pointer fields are normalized to zero.
using System; namespace Por.Core { internal static class SaveRecordCodec {
internal static byte[] Encode(Types.TCharRecord c) { byte[] b=new byte[285]; int n=Math.Min(15,c.name.Length); b[0]=(byte)n; for(int i=0;i<n;i++)b[i+1]=unchecked((byte)c.name[i]);
unchecked {
b[16+0] = (byte)(c.strCur >> 0);
}
unchecked {
b[17+0] = (byte)(c.intCur >> 0);
}
unchecked {
b[18+0] = (byte)(c.wisCur >> 0);
}
unchecked {
b[19+0] = (byte)(c.dexCur >> 0);
}
unchecked {
b[20+0] = (byte)(c.conCur >> 0);
}
unchecked {
b[21+0] = (byte)(c.chaCur >> 0);
}
unchecked {
b[22+0] = (byte)(c.strExc >> 0);
}
for(int i=0;i<=20;i++) {
unchecked {
b[23+(i-0)*1+0] = (byte)(c.memorizedSpells[i] >> 0);
}
}
unchecked {
b[44+0] = (byte)(c.spellPrepTime >> 0);
}
unchecked {
b[45+0] = (byte)(c.thac0Base >> 0);
}
unchecked {
b[46+0] = (byte)(c.race >> 0);
}
unchecked {
b[47+0] = (byte)(c.charClass >> 0);
}
unchecked {
b[48+0] = (byte)(c.age >> 0);
b[48+1] = (byte)(c.age >> 8);
}
unchecked {
b[50+0] = (byte)(c.hpMax >> 0);
}
for(int i=1;i<=56;i++) {
unchecked {
b[51+(i-1)*1+0] = (byte)(c.spellsKnown[i] >> 0);
}
}
unchecked {
b[107+0] = (byte)(c.attackLevel >> 0);
}
unchecked {
b[108+0] = (byte)(c.iconDimensions >> 0);
}
for(int i=0;i<=4;i++) {
unchecked {
b[109+(i-0)*1+0] = (byte)(c.saves[i] >> 0);
}
}
unchecked {
b[114+0] = (byte)(c.movementBase >> 0);
}
unchecked {
b[115+0] = (byte)(c.levelHighest >> 0);
}
unchecked {
b[116+0] = (byte)(c.drainedLevels >> 0);
}
unchecked {
b[117+0] = (byte)(c.drainedHps >> 0);
}
unchecked {
b[118+0] = (byte)(c.levelUndead >> 0);
}
for(int i=1;i<=8;i++) {
unchecked {
b[119+(i-1)*1+0] = (byte)(c.thiefSkills[i] >> 0);
}
}
unchecked {
b[131+0] = (byte)(c.unknown083 >> 0);
}
unchecked {
b[132+0] = (byte)(c.npc >> 0);
}
unchecked {
b[133+0] = (byte)(c.npcTreasureShare >> 0);
}
unchecked {
b[134+0] = (byte)(c.unknown086 >> 0);
}
unchecked {
b[135+0] = (byte)(c.unknown087 >> 0);
}
for(int i=0;i<=6;i++) {
unchecked {
b[136+(i-0)*2+0] = (byte)(c.coins[i] >> 0);
b[136+(i-0)*2+1] = (byte)(c.coins[i] >> 8);
}
}
for(int i=0;i<=7;i++) {
unchecked {
b[150+(i-0)*1+0] = (byte)(c.classLevels[i] >> 0);
}
}
unchecked {
b[158+0] = (byte)(c.gender >> 0);
}
unchecked {
b[159+0] = (byte)(c.charType >> 0);
}
unchecked {
b[160+0] = (byte)(c.alignment >> 0);
}
unchecked {
b[161+0] = (byte)(c.attacks >> 0);
}
unchecked {
b[162+0] = (byte)(c.attacks2 >> 0);
}
for(int i=1;i<=2;i++) {
unchecked {
b[163+(i-1)*1+0] = (byte)(c.unarmedRollsA[i] >> 0);
}
}
for(int i=1;i<=2;i++) {
unchecked {
b[165+(i-1)*1+0] = (byte)(c.unarmedDiceA[i] >> 0);
}
}
for(int i=1;i<=2;i++) {
unchecked {
b[167+(i-1)*1+0] = (byte)(c.unarmedModA[i] >> 0);
}
}
unchecked {
b[169+0] = (byte)(c.acBase >> 0);
}
unchecked {
b[170+0] = (byte)(c.useStrength >> 0);
}
unchecked {
b[171+0] = (byte)(c.randByte >> 0);
}
unchecked {
b[172+0] = (byte)(c.experience >> 0);
b[172+1] = (byte)(c.experience >> 8);
b[172+2] = (byte)(c.experience >> 16);
b[172+3] = (byte)(c.experience >> 24);
}
unchecked {
b[176+0] = (byte)(c.itemLimits >> 0);
}
unchecked {
b[177+0] = (byte)(c.hpRolled >> 0);
}
for(int i=1;i<=3;i++) {
unchecked {
b[178+(i-1)*1+0] = (byte)(c.clericSpells[i] >> 0);
}
}
for(int i=1;i<=3;i++) {
unchecked {
b[181+(i-1)*1+0] = (byte)(c.mageSpells[i] >> 0);
}
}
unchecked {
b[184+0] = (byte)(c.xpAward >> 0);
b[184+1] = (byte)(c.xpAward >> 8);
}
unchecked {
b[186+0] = (byte)(c.xpBonusPerHp >> 0);
}
unchecked {
b[187+0] = (byte)(c.portraitHead >> 0);
}
unchecked {
b[188+0] = (byte)(c.portraitBody >> 0);
}
unchecked {
b[189+0] = (byte)(c.iconHead >> 0);
}
unchecked {
b[190+0] = (byte)(c.iconBody >> 0);
}
unchecked {
b[191+0] = (byte)(c.orderNumber >> 0);
}
unchecked {
b[192+0] = (byte)(c.iconSize >> 0);
}
for(int i=0;i<=5;i++) {
unchecked {
b[193+(i-0)*1+0] = (byte)(c.iconColors[i] >> 0);
}
}
unchecked {
b[199+0] = (byte)(c.numberOfItems >> 0);
}
unchecked {
b[256+0] = (byte)(c.handsEquipped >> 0);
}
unchecked {
b[257+0] = (byte)(c.saveBonus >> 0);
}
unchecked {
b[258+0] = (byte)(c.encumbrance >> 0);
b[258+1] = (byte)(c.encumbrance >> 8);
}
unchecked {
b[268+0] = (byte)(c.status >> 0);
}
unchecked {
b[269+0] = (byte)(c.enabled >> 0);
}
unchecked {
b[270+0] = (byte)(c.hostile >> 0);
}
unchecked {
b[271+0] = (byte)(c.quickfight >> 0);
}
unchecked {
b[272+0] = (byte)(c.thac0Cur >> 0);
}
unchecked {
b[273+0] = (byte)(c.acCur >> 0);
}
unchecked {
b[274+0] = (byte)(c.acBehind >> 0);
}
unchecked {
b[275+0] = (byte)(c.curAttacks >> 0);
}
unchecked {
b[276+0] = (byte)(c.curAttacks2 >> 0);
}
for(int i=1;i<=2;i++) {
unchecked {
b[277+(i-1)*1+0] = (byte)(c.curRollsA[i] >> 0);
}
}
for(int i=1;i<=2;i++) {
unchecked {
b[279+(i-1)*1+0] = (byte)(c.curDiceA[i] >> 0);
}
}
for(int i=1;i<=2;i++) {
unchecked {
b[281+(i-1)*1+0] = (byte)(c.curModA[i] >> 0);
}
}
unchecked {
b[283+0] = (byte)(c.hpCur >> 0);
}
unchecked {
b[284+0] = (byte)(c.movementCur >> 0);
}
return b; }
internal static void Decode(byte[] b,Types.TCharRecord c) { b=(byte[])b.Clone();
Array.Clear(b,127,4);
Array.Clear(b,200,4);
Array.Clear(b,204,52);
Array.Clear(b,260,4);
Array.Clear(b,264,4);
CharacterRecordCodec.DecodeMonster(b,0,c); }
} }

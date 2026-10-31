#!/usr/bin/env python3
"""Acceptance for isolated ranged, healing and Bless native actions (not campaign completion)."""
import argparse,base64,hashlib,json,pathlib
from PIL import Image

def actor(state,name):
    return next(c for c in state['party'] if c['name']==name)
def spells(c): return list(base64.b64decode(c['memorizedSpells']))
def ammo(c): return next(it['quantity'] for it in c['items'] if it['itemType']==73)
def validate(folder):
    folder=pathlib.Path(folder);r=json.loads((folder/'result.json').read_text())
    kind=r['action'];assert kind in ['ranged','heal','bless']
    assert r['scenario']=='six-pc-wilderness-'+kind
    assert r['accepted'] and r['native_actions'] and r['exit_code']==0
    assert r['original_media_unchanged'] and r['upstream_fixture_unchanged']
    assert not r['natural_campaign_progression']
    before,after=r['action_before'],r['action_after']
    name='MORRIGAN' if kind=='ranged' else 'GINS'
    a,b=actor(before,name),actor(after,name)
    assert before['activeCreature']==name
    assert before['round']==after['round']
    assert 'Combat.CombatMenu' in before['inputStack']
    assert 'Combat.ManualControl' in after['inputStack']
    assert any(f in after['inputStack'] for f in ['CombatAdapter.AimMenu','CombatAdapter.CastSpellNC','CombatAdapter.CastSpellCombat'])
    trace=json.loads((folder/'action-resolution.json').read_text())['capture']
    assert pathlib.Path(trace).name==trace and '-manual-' in trace
    assert json.loads((folder/'captures'/trace).read_text())==after
    target=json.loads((folder/'action-target.json').read_text())
    assert target==r['action_target'] and 'CbtActs.AimTargetMenu' in target['inputStack']
    selection=json.loads((folder/'action-selection.json').read_text())
    # AimTargetMenu's range test reuses the global scratch target list.
    # Use the captured initial aiming list, before that range test mutates it.
    aiming=json.loads((folder/'action-aim.json').read_text())
    chosen=aiming['aimCandidates'][selection['candidate_index']]
    assert chosen==selection['candidate']
    assert chosen['hostile'] != 0 if kind=='ranged' else chosen['name']==name
    assert before['partySize']==after['partySize']==6 and len(before['party'])==len(after['party'])
    assert [c['name'] for c in before['party']]==[c['name'] for c in after['party']]
    assert all(c['status']==old['status'] and c['enabled']==old['enabled'] for c,old in zip(after['party'],before['party']))
    # All action resource/effect checks occur before Quick AI is enabled.
    assert all(c['combat'] is not None for c in before['party'])
    if kind=='ranged':
        assert r['fixture_variant'] and r['fixture_variant'][0]['file']=='CHRDATG1.ITM'
        assert any(it['itemType']==43 and it['equipped']==1 for it in a['items'])
        assert any(it['itemType']==73 and it['equipped']==1 for it in a['items'])
        assert not any(it['itemType']==59 and it['equipped']==1 for it in a['items'])
        assert b['combat']['targetName']==chosen['name']
        selected=before['party'][chosen['slot']-1]
        assert [b['combat'][k] for k in ['targetX','targetY']]==[selected['combat'][k] for k in ['x','y']]
        assert 1<=ammo(a)-ammo(b)<=2
        assert [c['hp'] for c in before['party'][:6]]==[c['hp'] for c in after['party'][:6]]
        assert r['action_outcome'] in ['miss','hit']
        if r['action_outcome']=='hit':
            assert sum(c['hp']<old['hp'] for c,old in zip(after['party'][6:],before['party'][6:]))==1
            idx=chosen['slot']-1
            assert after['party'][idx]['hp']<before['party'][idx]['hp']
        else:
            assert [c['hp'] for c in after['party']]==[c['hp'] for c in before['party']]
        assert all(c['hp']<=old['hp'] for c,old in zip(after['party'][6:],before['party'][6:]))
        assert [c['memorizedSpells'] for c in before['party']]==[c['memorizedSpells'] for c in after['party']]
    else:
        spell=3 if kind=='heal' else 1
        sa,sb=spells(a),spells(b)
        assert sa.count(spell)==sb.count(spell)+1
        assert all(c['memorizedSpells']==old['memorizedSpells'] for c,old in zip(after['party'],before['party']) if c['name']!=name)
        assert [c['items'] for c in before['party']]==[c['items'] for c in after['party']]
        assert all(sa.count(i)==sb.count(i) for i in range(1,128) if i!=spell)
        assert after['spellTargets']
        if kind=='heal':assert after['spellTargets']==[name]
        assert 'Magic.SpellSelection' in json.loads((folder/'action-spell-menu.json').read_text())['inputStack']
        assert all(c['hp']==old['hp'] for c,old in zip(after['party'],before['party']) if c['name']!=name)
        if kind=='heal':
            assert r['fixture_variant'] and r['fixture_variant'][0]['file']=='CHRDATG2.SAV'
            assert 1<=b['hp']-a['hp']<=8
            assert a['effects']==b['effects']
        else:
            assert not r['fixture_variant'] and a['hp']==b['hp']
            assert not any(e['id']==1 for c in before['party'] for e in c['effects'])
            # Bless's casting delay lets the beasts approach. The source excludes
            # allies engaged in melee, so the caster/front line are NOT blessed.
            expected={'MORGAN','RAVEN','SHADOW'}
            assert {n for n in after['spellTargets'] if n is not None}==expected
            assert {c['name'] for c in after['party'] if any(e['id']==1 and e['duration']==6 for e in c['effects'])}==expected
            assert [c['hp'] for c in before['party']]==[c['hp'] for c in after['party']]
    assert r['outcome']['combatResult']==0 and not r['outcome']['partyDefeated'] and r['outcome']['round']>0
    assert any(f.startswith('PostCbt.') for f in r['outcome']['inputStack'])
    assert r['encounter_step']==len(r['steps'])+1 and r['deterministic_managed_seed']==1
    assert r['first']['arenaCount']>6
    initial,saved,loaded,altered=[r[k] for k in ['initial','saved','loaded','altered']]
    assert saved['party']==loaded['party'] and saved['clock']==loaded['clock']
    fields=['locationType','wildernessX','wildernessY','facing']
    assert [saved[k] for k in fields]==[loaded[k] for k in fields]
    assert [saved[k] for k in fields]!=[altered[k] for k in fields]
    for s in [initial,*r['steps'],saved,loaded,altered]:
        assert s['locationType']==3 and s['interfaceState']==3 and s['partySize']==len(s['party'])==6
        assert 'Explore.MapMainMenu' in s['inputStack']
        assert not any(f.startswith(('ViewChar.','Camp.','CompTool.','MapView.','PostCbt.')) for f in s['inputStack'])
        assert all(not c['combatRecord'] for c in s['party'])
    assert all(c['experience']>old['experience'] for c,old in zip(loaded['party'],initial['party']))
    if kind=='ranged':assert ammo(actor(loaded,name))<=ammo(b)
    else:assert spells(actor(loaded,name)).count(spell)<=spells(b).count(spell)
    if kind=='bless':
        assert r['outcome']['round']>=6
        assert all(not any(e['id']==1 for e in c['effects']) for c in loaded['party'])
    image=Image.open(folder/'loaded.png').convert('RGB')
    for row in range(6):
        assert sum(image.getpixel((x,y)) in [(255,255,255),(85,255,255)] for y in range(32+8*row,40+8*row) for x in range(136,232))>25
    for variant in r['fixture_variant']:
        raw=(folder/'output-saves'/variant['file']).read_bytes()
        assert hashlib.sha256(raw).hexdigest()==variant['after_sha256']
        restored=bytearray(raw)
        assert variant['changes']
        for change in variant['changes']:
            assert restored[change['offset']]==change['after']
            restored[change['offset']]=change['before']
        assert hashlib.sha256(restored).hexdigest()==variant['before_sha256']==r['fixture_sha256'][variant['file']]
    assert r['save_created'] and hashlib.sha256((folder/'output-saves/savgamQ.dat').read_bytes()).hexdigest()==r['save_sha256']
    assert r['item_spell_effect_sidecars']
    for name,digest in r['item_spell_effect_sidecars'].items():
        assert hashlib.sha256((folder/'output-saves'/name).read_bytes()).hexdigest()==digest
    return r
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('folders',type=pathlib.Path,nargs='+');a=p.parse_args()
    for folder in a.folders:
        r=validate(folder);print(r['action']+' isolated action / combat / persistence / exit PASS: '+str(folder))

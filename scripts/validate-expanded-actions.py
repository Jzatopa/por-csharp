#!/usr/bin/env python3
"""Independent acceptance for six extended native branches, not natural campaign completion."""
import argparse,base64,hashlib,json,pathlib
from PIL import Image
ACTIONS=['ranged-hit','ranged-empty','heal-ally','heal-cap','bandage','turn-ineligible']
def actor(state,name):return next(c for c in state['party'] if c['name']==name)
def spells(c):return list(base64.b64decode(c['memorizedSpells']))
def arrows(c):return sum(i['quantity'] for i in c['items'] if i['itemType']==73)
def validate(folder):
    folder=pathlib.Path(folder);r=json.loads((folder/'result.json').read_text());kind=r['action']
    assert kind in ACTIONS and r['scenario']=='six-pc-wilderness-'+kind
    assert r['accepted'] and r['native_actions'] and r['exit_code']==0
    assert r['original_media_unchanged'] and r['upstream_fixture_unchanged'] and not r['natural_campaign_progression']
    assert r['original_files_checked']==168 and len(r['fixture_sha256'])==28
    assert r['deterministic_managed_seed']==1 and r['encounter_step']==len(r['steps'])+1
    assert r['first']['partySize']==6 and r['first']['arenaCount']>6
    before,after=r['action_before'],r['action_after'];who=r['action_extra']['actor']
    assert who==('MORRIGAN' if kind.startswith('ranged-') or kind=='bandage' else 'GINS')
    assert before['activeCreature']==after['activeCreature']==who and before['round']==after['round']
    assert 'Combat.CombatMenu' in before['inputStack'] and 'Combat.ManualControl' in after['inputStack']
    assert all(c['quickfight']==0 for c in before['party'][:6])
    resolution=json.loads((folder/'action-resolution.json').read_text())['capture']
    assert pathlib.Path(resolution).name==resolution and '-manual-' in resolution
    assert json.loads((folder/'captures'/resolution).read_text())==after
    a,b=actor(before,who),actor(after,who)
    assert [c['name'] for c in before['party']]==[c['name'] for c in after['party']]
    assert r['fixture_variant'] if kind!='turn-ineligible' else not r['fixture_variant']
    for variant in r['fixture_variant']:
        raw=(folder/'output-saves'/variant['file']).read_bytes()
        assert hashlib.sha256(raw).hexdigest()==variant['after_sha256']
        restore=bytearray(raw);assert variant['changes']
        for change in variant['changes']:
            assert restore[change['offset']]==change['after'];restore[change['offset']]=change['before']
        assert hashlib.sha256(restore).hexdigest()==variant['before_sha256']==r['fixture_sha256'][variant['file']]
    if kind.startswith(('ranged-','heal-')):
        target=json.loads((folder/'action-target.json').read_text())
        assert target==r['action_target'] and 'CbtActs.AimTargetMenu' in target['inputStack']
        aim=json.loads((folder/'action-aim.json').read_text());selection=json.loads((folder/'action-selection.json').read_text())
        chosen=aim['aimCandidates'][selection['candidate_index']];assert chosen==selection['candidate']
    if kind.startswith('ranged-'):
        assert 'CombatAdapter.AimMenu' in after['inputStack']
        assert chosen['hostile']==1 and b['combat']['targetName']==chosen['name']
        idx=chosen['slot']-1;selected=before['party'][idx];assert selected['combat']['notPcNpc']==1
        assert [b['combat'][k] for k in ['targetX','targetY']]==[selected['combat'][k] for k in ['x','y']]
        assert any(i['itemType']==43 and i['plus']==5 and i['equipped']==1 for i in a['items'])
        assert any(i['itemType']==73 and i['equipped']==1 for i in a['items'])
        assert not any(i['itemType'] in [35,59] and i['equipped'] for i in a['items'])
        assert [c['hp'] for c in before['party'][:6]]==[c['hp'] for c in after['party'][:6]]
        assert [c['memorizedSpells'] for c in before['party']]==[c['memorizedSpells'] for c in after['party']]
        if kind=='ranged-hit':
            assert arrows(a)==40 and arrows(b)==38
            assert any(i['itemType']==73 and i['plus']==5 for i in a['items'])
            assert after['party'][idx]['hp']<selected['hp']
            assert sum(c['hp']<old['hp'] for c,old in zip(after['party'],before['party']))==1
            assert chosen['distance']//2<=7  # Source distance halves with integer truncation.
            plan=json.loads((folder/'ranged-movement-plan.json').read_text());assert len(plan)>0
            moved=json.loads((folder/'ranged-movement-finished.json').read_text())
            assert actor(moved,who)['combat']['movement']<actor(json.loads((folder/'ranged-movement-start.json').read_text()),who)['combat']['movement']
        else:
            assert arrows(a)==2 and not any(i['itemType']==73 for i in b['items'])
            assert len(a['items'])==len(b['items'])+1
            assert b['items']==[i for i in a['items'] if i['itemType']!=73]
    elif kind.startswith('heal-'):
        assert any(f in after['inputStack'] for f in ['CombatAdapter.CastSpellNC','CombatAdapter.CastSpellCombat'])
        assert spells(a).count(3)==spells(b).count(3)+1
        assert all(spells(a).count(i)==spells(b).count(i) for i in range(1,128) if i!=3)
        recipient='MORRIGAN' if kind=='heal-ally' else 'GINS'
        assert chosen['hostile']==0 and chosen['name']==recipient and after['spellTargets']==[recipient]
        old,new=actor(before,recipient),actor(after,recipient)
        assert 1<=new['hp']-old['hp']<=8 and new['hp']<=new['hpMax']
        if kind=='heal-ally':assert old['hpMax']-old['hp']==10 and actor(after,'GINS')['hp']==actor(before,'GINS')['hp']
        else:assert new['hp']==new['hpMax'] and old['hp']==old['hpMax']-2
        assert all(c['hp']==old['hp'] for c,old in zip(after['party'],before['party']) if c['name']!=recipient)
        assert all(c['items']==old['items'] and c['effects']==old['effects'] and c['status']==old['status'] and c['enabled']==old['enabled'] for c,old in zip(after['party'],before['party']))
        assert all(c['memorizedSpells']==old['memorizedSpells'] for c,old in zip(after['party'],before['party']) if c['name']!=who)
    elif kind=='bandage':
        assert 'Combat.DoneMenu' in after['inputStack']
        assert 'Combat.DoneMenu' in json.loads((folder/'action-done-menu.json').read_text())['inputStack']
        old,new=actor(before,'SHADOW'),actor(after,'SHADOW')
        assert old['status']==5 and new['status']==4 and old['hp']==new['hp']==0 and old['enabled']==new['enabled']==0
        assert new['combat']['overkill']==0
        assert all(c['status']==old['status'] for c,old in zip(after['party'],before['party']) if c['name']!='SHADOW')
        assert all(c['hp']==old['hp'] and c['items']==old['items'] and c['memorizedSpells']==old['memorizedSpells'] for c,old in zip(after['party'],before['party']))
    else:
        assert 'CombatAdapter.TurnUndead' in after['inputStack']
        assert a['combat']['usedTurnUndead']==0 and b['combat']['usedTurnUndead']==1
        assert all(c['levelUndead']==0 for c in before['party'][6:]) and len(before['party'])>6
        assert all(c['hp']==old['hp'] and c['status']==old['status'] and c['enabled']==old['enabled'] and c['effects']==old['effects'] and c['memorizedSpells']==old['memorizedSpells'] and c['items']==old['items'] for c,old in zip(after['party'],before['party']))
        nxt,rejected=r['action_extra']['next_turn'],r['action_extra']['rejected']
        assert nxt['round']>before['round'] and nxt['round']==rejected['round'] and nxt['party']==rejected['party']
        assert nxt['activeCreature']==rejected['activeCreature']=='GINS'
        assert actor(rejected,'GINS')['combat']['usedTurnUndead']==1
        assert 'Combat.CombatMenu' in rejected['inputStack'] and r['action_extra']['no_second_turn_event']
        assert len(list((folder/'captures').glob('*-manual-turn-resolved.json')))==1
    initial,saved,loaded,altered=[r[k] for k in ['initial','saved','loaded','altered']]
    for state in [initial,*r['steps'],saved,loaded,altered]:
        assert state['locationType']==3 and state['interfaceState']==3 and state['partySize']==len(state['party'])==6
        assert all(not c['combatRecord'] for c in state['party']) and 'Explore.MapMainMenu' in state['inputStack']
        assert not any(f.startswith(('ViewChar.','Camp.','CompTool.','MapView.','PostCbt.')) for f in state['inputStack'])
    assert saved['party']==loaded['party'] and saved['clock']==loaded['clock']
    world=['locationType','wildernessX','wildernessY','facing']
    assert [saved[k] for k in world]==[loaded[k] for k in world] and [saved[k] for k in world]!=[altered[k] for k in world]
    assert r['outcome']['combatResult']==0 and not r['outcome']['partyDefeated'] and r['outcome']['round']>0
    assert any(f.startswith('PostCbt.') for f in r['outcome']['inputStack'])
    assert any(c['experience']>old['experience'] for c,old in zip(loaded['party'],initial['party']))
    if kind=='ranged-empty':assert not any(i['itemType']==73 for i in actor(loaded,who)['items'])
    if kind=='ranged-hit':assert arrows(actor(loaded,who))<=arrows(b)
    if kind.startswith('heal-'):assert spells(actor(loaded,who)).count(3)<=spells(b).count(3)
    if kind=='bandage':assert actor(loaded,'SHADOW')['status']==4 and actor(loaded,'SHADOW')['enabled']==0 and actor(loaded,'SHADOW')['hp']==0
    assert r['save_created'] and hashlib.sha256((folder/'output-saves/savgamQ.dat').read_bytes()).hexdigest()==r['save_sha256']
    assert r['item_spell_effect_sidecars']
    for file,digest in r['item_spell_effect_sidecars'].items():assert hashlib.sha256((folder/'output-saves'/file).read_bytes()).hexdigest()==digest
    image=Image.open(folder/'loaded.png').convert('RGB')
    for row in range(6):
        # CharDisp uses LightRed for disabled names; borders and heading excluded.
        assert sum(image.getpixel((x,y)) in [(255,255,255),(85,255,255),(255,85,85)] for y in range(32+row*8,40+row*8) for x in range(136,232))>25
    return r
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('folders',nargs='+',type=pathlib.Path);args=p.parse_args()
    for folder in args.folders:print(validate(folder)['action']+' native action/cleanup/save-load/exit PASS')

#!/usr/bin/env python3
"""Summarize explicit static candidates and accepted native evidence, never infer campaign completion."""
import argparse,base64,json,pathlib
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--inventory',type=pathlib.Path,required=True)
p.add_argument('--evidence',type=pathlib.Path,nargs='*',default=[])
p.add_argument('--defeat-evidence',type=pathlib.Path,nargs='*',default=[])
p.add_argument('--extended-evidence',type=pathlib.Path,nargs='*',default=[])
p.add_argument('--wilderness-evidence',type=pathlib.Path,nargs='*',default=[])
p.add_argument('--expanded-evidence',type=pathlib.Path,nargs='*',default=[])
p.add_argument('--action-evidence',type=pathlib.Path,nargs='*',default=[])
p.add_argument('--output',type=pathlib.Path,default=root/'docs/campaign-combat-coverage.json')
a=p.parse_args();scan=json.loads(a.inventory.read_text());areas=[];scenarios=[]
for s in scan['scripts']:
 areas.append(dict(scriptId=s['scriptId'],disk=s['disk'],maps=[dict(mapId=m['mapId'],area=m['area'],static_fight_candidate_squares=len(m['fightCandidates'])) for m in s['maps']],native_victory_lifecycles=0,native_defeat_recovery_lifecycles=0,natural_campaign_route_validated=False))
for folder in a.evidence:
 result=json.loads((folder/'result.json').read_text());life=json.loads((folder/'lifecycle.json').read_text());initial=json.loads((folder/'combat-first-round.json').read_text())
 assert result['native_combat_entry'] and result['lifecycle_complete'] and result['original_media_unchanged']
 assert life['complete'] and life['saved']['party']==life['loaded']['party']
 assert not life['postcombat']['partyDefeated'] and len(life['postcombat']['party'])==1
 assert not life['postcombat']['party'][0]['combatRecord']
 assert life['postcombat']['party'][0]['experience']>initial['party'][0]['experience']
 assert life['postcombat']['interfaceState']==4 and life['loaded']['interfaceState']==4
 image=Image.open(folder/'post-load.png').convert('RGB');text_pixels=sum(image.getpixel((x,y))==(255,255,255) for y in range(28,40) for x in range(184,280));assert text_pixels>25,'Missing character-name text: '+str(folder)
 assert tuple(life['saved'][k] for k in ['script','x','y','facing'])==tuple(life['loaded'][k] for k in ['script','x','y','facing'])
 script=result['script'];area=next(r for r in areas if r['scriptId']==script);area['native_victory_lifecycles']+=1
 scenarios.append(dict(id=folder.name,script=script,position=result['map'],mode='synthetic-save real SDL/keyboard/AI',status='victory_cleanup_save_load_pass',natural_campaign_progression=False,hp=life['loaded']['party'][0]['hp'],xp=life['loaded']['party'][0]['experience'],coins=life['loaded']['party'][0]['coins'],inventory_items=len(life['loaded']['party'][0]['items']),party_name_text_pixels=text_pixels,evidence=str(folder.relative_to(root)) if folder.is_relative_to(root) else str(folder)))
for folder in a.defeat_evidence:
 result=json.loads((folder/'defeat-result.json').read_text());dead=result['defeated'];loaded=result['loaded']
 assert result['defeat_and_recovery'] and result['original_media_unchanged']
 assert dead['partyDefeated'] and dead['combatResult']==0x80 and dead['partySize']==0 and dead['party']==[]
 assert result['menu']['interfaceState']==0
 assert loaded['stage']=='explore' and loaded['interfaceState']==4 and loaded['partySize']==1
 assert loaded['party'][0]['hp']==20 and loaded['party'][0]['experience']==0 and not loaded['party'][0]['combatRecord']
 area=next(r for r in areas if r['scriptId']==dead['script']);area['native_defeat_recovery_lifecycles']+=1
 scenarios.append(dict(id=folder.name,script=dead['script'],position=[dead['x'],dead['y']],mode='synthetic-save real SDL/keyboard/AI',status='defeat_main_menu_recovery_pass',natural_campaign_progression=False,combat_result=dead['combatResult'],recovered_hp=loaded['party'][0]['hp'],evidence=str(folder)))
for folder in a.extended_evidence:
 result=json.loads((folder/'extended-result.json').read_text());kind=result['scenario'];initial=result['first'];outcome=result['outcome'];post=result['postcombat'];saved=result['saved'];loaded=result['loaded']
 assert result['accepted'] and result['native_actions'] and result['original_media_unchanged'] and not result['forced_combat_status_changes']
 assert not result['natural_campaign_progression']
 assert post['interfaceState']==4 and loaded['interfaceState']==4 and saved['party']==loaded['party']
 for state in [post,saved,loaded]:
  stack=state.get('inputStack',[])
  assert 'Explore.MapMainMenu' in stack and not any(f.startswith(('ViewChar.','Camp.','CompTool.','MapView.','PostCbt.')) for f in stack),'Not the actual exploration menu'
 assert result['seeded_save_sha256']!=result['written_save_sha256']
 assert result['altered']['facing']!=saved['facing'] and loaded['facing']==saved['facing']
 image=Image.open(folder/'loaded.png').convert('RGB');name_pixels=sum(image.getpixel((x,y))==(255,255,255) for y in range(28,40) for x in range(184,280));assert name_pixels>25,'Loaded character-name text missing'
 assert all(not p['combatRecord'] for p in post['party']) and not outcome['partyDefeated']
 assert [saved[k] for k in ['script','x','y','facing']]==[loaded[k] for k in ['script','x','y','facing']]
 area=next(r for r in areas if r['scriptId']==post['script'])
 if kind=='flee':
  assert not initial['duel'] and initial['party'][0]['status']==0 and outcome['combatResult']==129
  assert loaded['partySize']==1 and loaded['party'][0]['status']==0 and loaded['party'][0]['enabled']==1
  assert len(json.loads((folder/'planned-flee-path.json').read_text()))>0
  field='native_flee_lifecycles'
 elif kind=='party':
  assert initial['partySize']==2 and loaded['partySize']==2
  assert initial['party'][1]['name']=='REST ALLY' and initial['party'][1]['status']==4 and initial['party'][1]['hp']==1
  assert loaded['party'][1]['name']=='REST ALLY' and loaded['party'][1]['status']==0 and loaded['party'][1]['enabled']==1 and loaded['party'][1]['hp']==1
  field='native_seeded_downed_member_recovery_lifecycles'
 elif kind=='loot':
  acquired=json.loads((folder/'item-acquired.json').read_text())
  assert acquired['treasure'] or outcome['treasure']
  assert sum(len(p['items']) for p in loaded['party'])>sum(len(p['items']) for p in initial['party'] if p['combat']['notPcNpc']==0)
  field='native_dropped_item_acquisition_lifecycles'
 else:raise AssertionError('Unsupported extended scenario '+kind)
 area[field]=area.get(field,0)+1
 if kind!='flee':area['native_victory_lifecycles']+=1
 scenarios.append(dict(id=folder.name,script=post['script'],position=[post['x'],post['y']],mode='synthetic precombat fixture, real native input/combat/serialization',status=kind+'_cleanup_save_load_pass',natural_campaign_progression=False,initial_party_size=initial['partySize'],final_party_size=loaded['partySize'],combat_result=outcome['combatResult'],evidence=str(folder.relative_to(root)) if folder.is_relative_to(root) else str(folder)))
adventures=[]
for folder in a.wilderness_evidence:
 result=json.loads((folder/'result.json').read_text());initial=result['initial'];saved=result['saved'];loaded=result['loaded'];altered=result['altered']
 assert result['accepted'] and result['native_actions'] and result['exit_code']==0
 assert result['original_media_unchanged'] and result['upstream_fixture_unchanged'] and not result['natural_campaign_progression']
 assert result['save_created'] and 'SAVGAMG.DAT' in result['fixture_sha256']
 assert sum(name.endswith('.SAV') for name in result['fixture_sha256'])==6
 for state in [initial,*result['steps'],saved,altered,loaded]:
  stack=state['inputStack']
  assert state['locationType']==3 and state['interfaceState']==3 and 'Explore.MapMainMenu' in stack
  assert not any(f.startswith(('ViewChar.','Camp.','CompTool.','MapView.','PostCbt.')) for f in stack)
  assert state['partySize']==6 and len(state['party'])==6 and all(not c['combatRecord'] for c in state['party'])
 assert saved['party']==loaded['party'] and saved['clock']==loaded['clock']
 fields=['locationType','wildernessX','wildernessY','facing']
 assert [saved[k] for k in fields]==[loaded[k] for k in fields]
 assert [saved[k] for k in fields]!=[altered[k] for k in fields]
 explicit=result.get('scenario')=='upstream-six-pc-wilderness-spell'
 item=result.get('scenario')=='upstream-six-pc-wilderness-item'
 combat=result.get('scenario') in ['upstream-six-pc-wilderness-combat','upstream-six-pc-wilderness-spell','upstream-six-pc-wilderness-item']
 assert len(result['steps'])>=1 if combat else len(result['steps'])==3
 if combat:
  first=result['first'];outcome=result['outcome']
  assert first['interfaceState']==5 and first['partySize']==6 and first['arenaCount']>6
  assert any('PostCbt.' in f for f in outcome['inputStack'])
  assert first['locationType']==3 and outcome['round']>=1 and not outcome['partyDefeated'] and outcome['combatResult']==0
  assert result['encounter_step']==len(result['steps'])+1 and result['deterministic_managed_seed']==1
  assert [c['name'] for c in initial['party']]==[c['name'] for c in loaded['party']]
  assert all(c['experience']>old['experience'] for c,old in zip(loaded['party'],initial['party']))
 if explicit:
  assert result['explicit_spell']
  before_cast=result['spell_before'];after_cast=result['spell_after']
  assert before_cast['activeCreature']=='RAVEN' and before_cast['round']==after_cast['round']
  assert 'Combat.CombatMenu' in before_cast['inputStack'] and 'Combat.CombatMenu' in after_cast['inputStack']
  before_raven=next(c for c in before_cast['party'] if c['name']=='RAVEN')
  after_raven=next(c for c in after_cast['party'] if c['name']=='RAVEN')
  assert list(base64.b64decode(before_raven['memorizedSpells'])).count(15)==1
  assert list(base64.b64decode(after_raven['memorizedSpells'])).count(15)==0
  assert all(15 not in base64.b64decode(c['memorizedSpells']) for c in loaded['party'] if c['name']=='RAVEN')
  assert after_cast['spellTargets']==['DISPLACER BEAST']
  assert [c['hp'] for c in before_cast['party'][:6]]==[c['hp'] for c in after_cast['party'][:6]]
  assert sum(c['hp']<old['hp'] for c,old in zip(after_cast['party'],before_cast['party']) if c['combat']['notPcNpc']==1)==1
  menu=json.loads((folder/'explicit-spell-menu.json').read_text());aim=json.loads((folder/'explicit-spell-target.json').read_text())
  assert 'Magic.SpellSelection' in menu['inputStack'] and 'CbtActs.AimTargetMenu' in aim['inputStack']
 if item:
  assert result['explicit_item']
  traded=json.loads((folder/'item-traded.json').read_text())
  source_potion=next(it for it in initial['party'][0]['items'] if it['itemType']==84)
  assert not any(it['itemType']==84 for it in initial['party'][2]['items'])
  assert not any(it['itemType']==84 for it in traded['party'][0]['items'])
  assert next(it for it in traded['party'][2]['items'] if it['itemType']==84)==source_potion
  before_item=result['item_before'];after_item=result['item_after']
  assert before_item['activeCreature']=='BLAIR' and before_item['round']==after_item['round']
  assert 'Combat.CombatMenu' in before_item['inputStack'] and 'Combat.CombatMenu' in after_item['inputStack']
  actor_before=next(c for c in before_item['party'] if c['name']=='BLAIR')
  actor_after=next(c for c in after_item['party'] if c['name']=='BLAIR')
  potion=next(it for it in actor_before['items'] if it['itemType']==84)
  assert base64.b64decode(potion['variantData'])==bytes([1,82,0]) and potion['equipped']==0
  assert actor_after['items']==[it for it in actor_before['items'] if it['itemType']!=84]
  assert actor_before['strCur']<21 and actor_after['strCur']==21 and actor_after['strExc']==0
  assert not any(e['id']==38 for e in actor_before['effects'])
  assert any(e['id']==38 and e['duration']>0 for e in actor_after['effects'])
  assert [c['hp'] for c in before_item['party']]==[c['hp'] for c in after_item['party']]
  assert [c['memorizedSpells'] for c in before_item['party']]==[c['memorizedSpells'] for c in after_item['party']]
  actor_loaded=next(c for c in loaded['party'] if c['name']=='BLAIR')
  assert not any(it['itemType']==84 for it in actor_loaded['items']) and actor_loaded['strCur']==21
  assert any(e['id']==38 and e['duration']>0 for e in actor_loaded['effects'])
  menu=json.loads((folder/'explicit-item-menu.json').read_text());readied=json.loads((folder/'explicit-item-readied.json').read_text());announced=json.loads((folder/'explicit-item-announced.json').read_text())
  assert 'ViewChar.UseItemMenu' in menu['inputStack'] and 'ViewChar.UseItemMenu' in readied['inputStack']
  assert next(it for c in readied['party'] if c['name']=='BLAIR' for it in c['items'] if it['itemType']==84)['equipped']==1
  assert 'Magic.UseItemSpell' in announced['inputStack']
 before=initial
 for step,state in enumerate(result['steps']):
  dx,dy=[(0,-1),(1,0),(0,1),(-1,0)][step%4]
  assert [state['wildernessX'],state['wildernessY']]==[before['wildernessX']+dx,before['wildernessY']+dy]
  assert state['clock']!=before['clock']
  if not combat:assert state['party']==initial['party']
  before=state
 image=Image.open(folder/'loaded.png').convert('RGB')
 pixels=[sum(image.getpixel((x,y)) in [(255,255,255),(85,255,255)] for y in range(32+row*8,40+row*8) for x in range(136,232)) for row in range(6)]
 assert all(n>25 for n in pixels),'Missing loaded party names'
 assert result['item_spell_effect_sidecars']
 import hashlib
 for name,digest in result['item_spell_effect_sidecars'].items():
  assert hashlib.sha256((folder/'output-saves'/name).read_bytes()).hexdigest()==digest
 area=next(r for r in areas if r['scriptId']==loaded['script']);area['native_wilderness_navigation_save_load_runs']=area.get('native_wilderness_navigation_save_load_runs',0)+1
 if combat:
  area['native_victory_lifecycles']+=1
  area['native_wilderness_combat_lifecycles']=area.get('native_wilderness_combat_lifecycles',0)+1
  if item:area['native_explicit_giant_strength_item_lifecycles']=area.get('native_explicit_giant_strength_item_lifecycles',0)+1
  if explicit:area['native_explicit_magic_missile_lifecycles']=area.get('native_explicit_magic_missile_lifecycles',0)+1
  scenarios.append(dict(id=folder.name,script=loaded['script'],position=[loaded['wildernessX'],loaded['wildernessY']],mode='unchanged upstream six-PC fixture; managed fixed seed; real native movement/combat/serialization',status='wilderness_explicit_giant_strength_item_cleanup_save_load_pass' if item else 'wilderness_explicit_magic_missile_cleanup_save_load_pass' if explicit else 'wilderness_victory_cleanup_save_load_pass',natural_campaign_progression=False,initial_party_size=6,final_party_size=6,encounter_step=result['encounter_step'],arena_count=result['first']['arenaCount'],evidence=str(folder.relative_to(root)) if folder.is_relative_to(root) else str(folder)))
 adventures.append(dict(id=folder.name,script=loaded['script'],status='six_pc_wilderness_combat_save_load_exit_pass' if combat else 'six_pc_wilderness_navigation_save_load_exit_pass',mode='unchanged upstream save fixture; real native load/navigation/serialization',natural_campaign_progression=False,party_size=6,loaded_name_text_pixels=pixels,limits=result['limits'],evidence=str(folder.relative_to(root)) if folder.is_relative_to(root) else str(folder)))
# New action families have their own independent acceptance contract; seeded
# variants are never mislabeled as unchanged upstream inventories or natural play.
if a.action_evidence:
 import runpy
 validate_action=runpy.run_path(str(root/'scripts/validate-three-actions.py'))['validate']
 for folder in a.action_evidence:
  result=validate_action(folder);kind=result['action'];loaded=result['loaded']
  area=next(r for r in areas if r['scriptId']==loaded['script'])
  area['native_victory_lifecycles']+=1
  field='native_explicit_'+kind+('_'+result['action_outcome'] if kind=='ranged' else '')+'_lifecycles'
  area[field]=area.get(field,0)+1
  path=str(folder.relative_to(root)) if folder.is_relative_to(root) else str(folder)
  mode=('documented synthetic pre-load variant of six-PC upstream fixture' if result['fixture_variant'] else 'unchanged six-PC upstream fixture')+'; managed fixed seed; real native input/combat/serialization'
  scenarios.append(dict(id=folder.name,script=loaded['script'],position=[loaded['wildernessX'],loaded['wildernessY']],mode=mode,status='explicit_'+kind+'_'+result['action_outcome']+'_cleanup_save_load_pass',natural_campaign_progression=False,initial_party_size=6,final_party_size=6,encounter_step=result['encounter_step'],arena_count=result['first']['arenaCount'],evidence=path))
  adventures.append(dict(id=folder.name,script=loaded['script'],status='six_pc_explicit_'+kind+'_combat_save_load_exit_pass',mode=mode,natural_campaign_progression=False,party_size=6,limits=result['limits'],evidence=path))
if a.expanded_evidence:
 import runpy
 validate_expanded=runpy.run_path(str(root/'scripts/validate-expanded-actions.py'))['validate']
 for folder in a.expanded_evidence:
  result=validate_expanded(folder);kind=result['action'];loaded=result['loaded']
  area=next(r for r in areas if r['scriptId']==loaded['script'])
  area['native_victory_lifecycles']+=1
  field='native_explicit_'+kind.replace('-','_')+'_lifecycles'
  area[field]=area.get(field,0)+1
  path=str(folder.relative_to(root)) if folder.is_relative_to(root) else str(folder)
  mode=('documented synthetic pre-load six-PC fixture' if result['fixture_variant'] else 'unchanged six-PC fixture')+'; managed fixed seed; native keyboard/combat/serialization'
  scenarios.append(dict(id=folder.name,script=loaded['script'],position=[loaded['wildernessX'],loaded['wildernessY']],mode=mode,status=kind+'_cleanup_save_load_pass',natural_campaign_progression=False,initial_party_size=6,final_party_size=6,encounter_step=result['encounter_step'],arena_count=result['first']['arenaCount'],evidence=path))
  adventures.append(dict(id=folder.name,script=loaded['script'],status='six_pc_'+kind+'_combat_save_load_exit_pass',mode=mode,natural_campaign_progression=False,party_size=6,limits=result['limits'],evidence=path))
ledger=dict(schema=4,scope='Pool of Radiance C# campaign combat validation; incomplete',campaign_completion=False,static_scan=dict(scripts=len(areas),candidate_squares=sum(len(m['fightCandidates']) for s in scan['scripts'] for m in s['maps']),unresolved_event_squares=scan['scanTotals']['unresolvedEventSquares'],unresolved_geometry_references=len(scan['geometryIssues']),source=str(a.inventory),limits='Unpatched media, bounded static classifier and literal map ids only. Squares are NOT distinct encounters; dynamic/rest/wilderness/story alternatives not exhaustively identified.'),areas=sorted(areas,key=lambda s:s['scriptId']),runtime_scenarios=scenarios,native_adventure_scenarios=adventures,natural_campaign_runs=[],pending_gates=['additional defeat / natural knockout / bleed / naturally downed bandaging / revival variants','multi-PC fleeing / enemy morale / continue battle','additional loot types / NPC shares / currency redistribution','naturally downed-member recovery / further larger-party combat variants (six-PC wilderness victory covered)','additional ranged / spell / item / effect variants; native equipment acquisition; positive undead turning (accepted action branches tracked individually)','additional wilderness encounter types / large icons / full-capacity combat','early/middle/late story prerequisites and encounters','natural campaign progression and final battle completion'])
a.output.write_text(json.dumps(ledger,indent=2)+'\n')
print(f"{len(scenarios)} fixture encounter outcome runs accepted; {len(adventures)} upstream wilderness runs; natural campaign completion: NOT VALIDATED")

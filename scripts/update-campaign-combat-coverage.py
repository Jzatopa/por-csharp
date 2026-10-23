#!/usr/bin/env python3
"""Summarize explicit static candidates and accepted native evidence, never infer campaign completion."""
import argparse,json,pathlib
from PIL import Image
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser(description=__doc__)
p.add_argument('--inventory',type=pathlib.Path,required=True)
p.add_argument('--evidence',type=pathlib.Path,nargs='*',default=[])
p.add_argument('--defeat-evidence',type=pathlib.Path,nargs='*',default=[])
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
ledger=dict(schema=1,scope='Pool of Radiance C# campaign combat validation; incomplete',campaign_completion=False,static_scan=dict(scripts=len(areas),candidate_squares=sum(len(m['fightCandidates']) for s in scan['scripts'] for m in s['maps']),unresolved_event_squares=scan['scanTotals']['unresolvedEventSquares'],unresolved_geometry_references=len(scan['geometryIssues']),source=str(a.inventory),limits='Unpatched media, bounded static classifier and literal map ids only. Squares are NOT distinct encounters; dynamic/rest/wilderness/story alternatives not exhaustively identified.'),areas=sorted(areas,key=lambda s:s['scriptId']),runtime_scenarios=scenarios,natural_campaign_runs=[],pending_gates=['defeat and recovery','flee / morale / continue battle','nonempty loot acquisition and redistribution','multi-PC party ordering and downed members','ranged / spells / items / turn undead / effects','wilderness / large icons / full-capacity combat','early/middle/late story prerequisites and encounters','natural campaign progression and final battle completion'])
a.output.write_text(json.dumps(ledger,indent=2)+'\n')
print(f"{len(scenarios)} synthetic encounter outcome runs accepted; natural campaign completion: NOT VALIDATED")

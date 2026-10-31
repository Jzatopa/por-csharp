#!/usr/bin/env python3
"""Corrupt six native branch records consistently and exercise actual acceptance."""
import argparse,copy,json,pathlib,runpy,tempfile
ROOT=pathlib.Path(__file__).resolve().parents[1]
validate=runpy.run_path(str(ROOT/'scripts/validate-expanded-actions.py'))['validate']
p=argparse.ArgumentParser(description=__doc__);p.add_argument('folders',nargs='+',type=pathlib.Path);p.add_argument('--output',required=True,type=pathlib.Path);args=p.parse_args();results={}
def actor(s,name):return next(c for c in s['party'] if c['name']==name)
for source in args.folders:
 original=json.loads((source/'result.json').read_text());kind=original['action'];name=original['action_extra']['actor']
 def missing_resource(r):
  a,b=actor(r['action_before'],name),actor(r['action_after'],name)
  if kind.startswith('ranged-'):b['items']=copy.deepcopy(a['items'])
  elif kind.startswith('heal-'):b['memorizedSpells']=a['memorizedSpells']
  elif kind=='bandage':actor(r['action_after'],'SHADOW')['hp']=1
  else:b['combat']['usedTurnUndead']=0
 def missing_effect(r):
  if kind=='ranged-hit':
   idx=json.loads((source/'action-selection.json').read_text())['candidate']['slot']-1;r['action_after']['party'][idx]['hp']=r['action_before']['party'][idx]['hp']
  elif kind=='ranged-empty':
   arrow=copy.deepcopy(next(i for i in actor(r['action_before'],name)['items'] if i['itemType']==73));arrow['quantity']=0;actor(r['action_after'],name)['items'].append(arrow)
  elif kind.startswith('heal-'):
   recipient='MORRIGAN' if kind=='heal-ally' else 'GINS';actor(r['action_after'],recipient)['hp']=actor(r['action_before'],recipient)['hp']
  elif kind=='bandage':actor(r['action_after'],'SHADOW')['status']=5
  else:r['action_after']['party'][6]['hp']-=1
 def friendly(r):
  friend='GINS' if kind=='heal-ally' else 'MORRIGAN';actor(r['action_after'],friend)['hp']=actor(r['action_before'],friend)['hp']-1
 def restored(r):
  if kind=='turn-ineligible':
   for k in ['next_turn','rejected']:actor(r['action_extra'][k],name)['combat']['usedTurnUndead']=0
   return
  for k in ['saved','loaded']:
   c=actor(r[k],name);a=actor(r['action_before'],name)
   if kind.startswith('ranged-'):
    c['items']=[i for i in c['items'] if i['itemType']!=73]+[copy.deepcopy(next(i for i in a['items'] if i['itemType']==73))]
   elif kind.startswith('heal-'):c['memorizedSpells']=a['memorizedSpells']
   else:actor(r[k],'SHADOW')['status']=5
 def wrong_target(r):
  if kind.startswith('ranged-'):actor(r['action_after'],name)['combat']['targetName']='GINS'
  elif kind.startswith('heal-'):r['action_after']['spellTargets']=['DISPLACER BEAST']
  elif kind=='bandage':actor(r['action_after'],'RAVEN')['status']=4
  else:r['action_before']['party'][6]['levelUndead']=1
 mutations={
  'resource_or_flag_not_spent':missing_resource,'missing_or_wrong_effect':missing_effect,'friendly_damage':friendly,
  'changed_round':lambda r:r['action_after'].update(round=r['action_before']['round']+1),
  'not_manual':lambda r:r['action_before'].update(inputStack=['CombatAi.CombatAiControl']),
  'persisted_or_once_only_state_lost':restored,'wrong_target':wrong_target,
  'mislabeled_variant':lambda r:r.update(fixture_variant=[] if kind!='turn-ineligible' else [{'invented':True}]),
  'modified_originals':lambda r:r.update(original_media_unchanged=False),
 }
 with tempfile.TemporaryDirectory(prefix='por-expanded-oracle-') as temporary:
  tmp=pathlib.Path(temporary)
  for test,mutate in [('positive',None),*mutations.items()]:
   folder=tmp/test;folder.mkdir()
   for f in source.iterdir():
    if f.name not in ['result.json','captures']:(folder/f.name).symlink_to(f.resolve())
   r=copy.deepcopy(original)
   if mutate:mutate(r)
   (folder/'result.json').write_text(json.dumps(r))
   traces=folder/'captures';traces.mkdir();trace=json.loads((source/'action-resolution.json').read_text())['capture']
   for f in (source/'captures').iterdir():
    if f.name!=trace:(traces/f.name).symlink_to(f.resolve())
   (traces/trace).write_text(json.dumps(r['action_after']))
   try:validate(folder);accepted=True
   except (AssertionError,KeyError,IndexError,StopIteration):accepted=False
   assert accepted==(mutate is None),(kind,test,accepted)
   results[kind+'/'+test]=dict(accepted=accepted,expected_accept=mutate is None)
args.output.write_text(json.dumps(results,indent=2)+'\n')
print(f'{len(args.folders)} positive records accepted; {9*len(args.folders)} planted faults rejected')

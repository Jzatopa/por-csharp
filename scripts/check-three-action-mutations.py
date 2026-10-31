#!/usr/bin/env python3
"""Exercise actual native action acceptance with consistent, deliberately corrupted evidence."""
import argparse,base64,copy,json,pathlib,runpy,tempfile
ROOT=pathlib.Path(__file__).resolve().parents[1]
validate=runpy.run_path(str(ROOT/'scripts/validate-three-actions.py'))['validate']
p=argparse.ArgumentParser(description=__doc__);p.add_argument('folders',nargs='+',type=pathlib.Path);p.add_argument('--output',required=True,type=pathlib.Path);args=p.parse_args()
results={}
def actor(s,name):return next(c for c in s['party'] if c['name']==name)
for source in args.folders:
 original=json.loads((source/'result.json').read_text());kind=original['action'];name='MORRIGAN' if kind=='ranged' else 'GINS'
 def resource(r):
  a,b=actor(r['action_before'],name),actor(r['action_after'],name)
  if kind=='ranged':next(it for it in b['items'] if it['itemType']==73)['quantity']=next(it for it in a['items'] if it['itemType']==73)['quantity']
  else:b['memorizedSpells']=a['memorizedSpells']
 def effect(r):
  if kind=='ranged':r['action_after']['party'][6]['hp']-=1  # A miss cannot claim damage.
  elif kind=='heal':actor(r['action_after'],name)['hp']=actor(r['action_before'],name)['hp']
  else:
   for c in r['action_after']['party']:c['effects']=[e for e in c['effects'] if e['id']!=1]
 def friendly(r):
  friend='GINS' if kind=='ranged' else 'MORRIGAN';actor(r['action_after'],friend)['hp']=actor(r['action_before'],friend)['hp']-1
 def restored(r):
  for s in ['saved','loaded']:
   c=actor(r[s],name);old=actor(r['action_before'],name)
   if kind=='ranged':next(it for it in c['items'] if it['itemType']==73)['quantity']=next(it for it in old['items'] if it['itemType']==73)['quantity']
   else:c['memorizedSpells']=old['memorizedSpells']
 def target(r):
  if kind=='ranged':actor(r['action_after'],name)['combat']['targetName']='GINS'
  else:r['action_after']['spellTargets']=['DISPLACER BEAST']
 mutations={
  'resource_not_spent':resource,'wrong_or_missing_effect':effect,'friendly_damage':friendly,
  'changed_round':lambda r:r['action_after'].update(round=r['action_before']['round']+1),
  'not_manual':lambda r:r['action_before'].update(inputStack=['CombatAi.CombatAiControl']),
  'resource_restored_after_load':restored,'wrong_target':target,
  'variant_mislabeled':lambda r:r.update(fixture_variant=[] if kind!='bless' else [{'invented':True}]),
  'inputs_modified':lambda r:r.update(original_media_unchanged=False),
 }
 with tempfile.TemporaryDirectory(prefix='por-action-oracle-') as temporary:
  tmp=pathlib.Path(temporary)
  for test,mutate in [('positive',None),*mutations.items()]:
   folder=tmp/test;folder.mkdir()
   for file in source.iterdir():
    if file.name not in ['result.json','captures']:(folder/file.name).symlink_to(file.resolve())
   r=copy.deepcopy(original)
   if mutate:mutate(r)
   (folder/'result.json').write_text(json.dumps(r))
   # Keep duplicated diagnostic snapshots consistent, so rejection exercises
   # resource/effect/context assertions rather than a trivial duplicate mismatch.
   captures=folder/'captures';captures.mkdir()
   trace=json.loads((source/'action-resolution.json').read_text())['capture']
   for file in (source/'captures').iterdir():
    if file.name!=trace:(captures/file.name).symlink_to(file.resolve())
   (captures/trace).write_text(json.dumps(r['action_after']))
   try:validate(folder);accepted=True
   except (AssertionError,KeyError,IndexError,StopIteration):accepted=False
   assert accepted==(mutate is None),(kind,test,'unexpected oracle acceptance',accepted)
   results[kind+'/'+test]=dict(accepted=accepted,expected_accept=mutate is None)
args.output.write_text(json.dumps(results,indent=2)+'\n')
print(str(len(args.folders))+' positive records accepted; '+str(len(args.folders)*9)+' planted faults rejected')

#!/usr/bin/env python3
"""Native keyboard encounter replay; synthetic character and disposable state, original media read-only."""
import pathlib,subprocess,tempfile,time,os,json,argparse,hashlib,signal
root=pathlib.Path(__file__).resolve().parents[1];p=argparse.ArgumentParser();p.add_argument('--effects');p.add_argument('--scenario',choices=['flee','party','loot','inspect'],required=True);p.add_argument('--script',type=int,default=20);p.add_argument('--x',type=int,default=14);p.add_argument('--y',type=int,default=1);p.add_argument('--facing',type=int,choices=[0,2,4,6],default=6);p.add_argument('--data',default=str(pathlib.Path.home()/'Downloads/poolofrad'));p.add_argument('--output',default=str(root/'evidence/combat-entry-2026-10-22/native'));a=p.parse_args();out=pathlib.Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True);caps=out/'captures';caps.mkdir(exist_ok=True);
if any(caps.iterdir()):p.error('Use a fresh output directory; existing captures are never reused.')
dotnet=str(root/'scripts/dotnet.sh')
subprocess.run([dotnet,'build',str(root/'src/Por.Desktop'),'-v:q'],check=True,cwd=root)
original=pathlib.Path(a.data);hashes=lambda:{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in original.iterdir() if f.is_file()};before=hashes();game=server=None
with tempfile.TemporaryDirectory(prefix='por-adventure-native-') as temp:
 temp=pathlib.Path(temp);state=temp/'state';saves=state/'saves';saves.mkdir(parents=True);fixture=saves/'PASSHERO.cha';subprocess.run([dotnet,'run','--project',str(root/'tests/Por.Adventure.Tests'),'--','--combat-fixture',str(fixture)],check=True,cwd=root);fixture_hash=hashlib.sha256(fixture.read_bytes()).hexdigest()
 if a.scenario=='party':subprocess.run([dotnet,'run','--project',str(root/'tests/Por.Adventure.Tests'),'--','--downed-fixture',str(saves/'RESTALLY.cha')],check=True,cwd=root)
 try:
  with (temp/'display').open('w+') as display,(out/'xvfb.log').open('w') as log:
   server=subprocess.Popen(['Xvfb','-displayfd',str(display.fileno()),'-screen','0','1280x960x24','-nolisten','tcp'],pass_fds=(display.fileno(),),stdout=log,stderr=log)
   while not (temp/'display').read_text().strip():
    if server.poll()!=None:raise RuntimeError('Xvfb failed')
    time.sleep(.05)
  env=dict(os.environ,DISPLAY=':'+(temp/'display').read_text().strip())
  with (out/'run.log').open('w') as log:
   game=subprocess.Popen([str(root/'play-desktop.sh'),'--data',str(original),'--state',str(state),'--captures',str(caps),'--short-intro']+(['--effects',str(pathlib.Path(a.effects).resolve())] if a.effects else []),cwd=root,env=env,stdout=log,stderr=subprocess.STDOUT)
   def alive():
    if game.poll()!=None:raise RuntimeError((out/'run.log').read_text())
   def wait(test,timeout=45):
    limit=time.monotonic()+timeout
    while not test():
     alive()
     if time.monotonic()>limit:raise TimeoutError((out/'run.log').read_text()[-2500:])
     time.sleep(.05)
   def text():return (out/'run.log').read_text()
   wait(lambda:'READY menu' in text());window=subprocess.check_output(['xdotool','search','--pid',str(game.pid)],env=env,text=True).splitlines()[-1]
   def key(k,pause=.3,expect_alive=True):
    subprocess.run(['xdotool','key','--window',window,k],env=env,check=True);time.sleep(pause)
    if expect_alive:alive()
   def capture(label):
    old=len(list(caps.glob('*.json')));key('F12');wait(lambda:len(list(caps.glob('*.json')))>old);path=sorted(caps.glob('*.json'))[-1];value=json.loads(path.read_text());(out/(label+'.json')).write_text(json.dumps(value,indent=2)+'\n');(out/(label+'.png')).write_bytes(path.with_suffix('.png').read_bytes());return value
   def defeat_capture(label):
    # Capture the actual game-over read boundary even if a previously queued key dismissed it.
    events=sorted(caps.glob('*-party-defeat.json'))
    if events:return json.loads(events[-1].read_text())
    v=capture(label)
    events=sorted(caps.glob('*-party-defeat.json'))
    return json.loads(events[-1].read_text()) if events else v
   assert ('RECOVERY restored '+('2' if a.scenario=='party' else '1')+' orphan character(s)') in text();assert fixture_hash==hashlib.sha256(fixture.read_bytes()).hexdigest();recovered_roster=(saves/'charlist.txt').read_text();assert 'PASS HERO' in recovered_roster
   for k in (['a','Return','Down','Return','e','b'] if a.scenario=='party' else ['a','Return','e','b']):key(k,.5)
   for page in range(50):
    if 'READY explore' in text():break
    key('Return',.85)
   else:raise RuntimeError('Opening script never reached exploration')
   wait(lambda:'READY explore' in text());base=capture('exploration');assert base['interfaceState']==4 and base['partySize']==(2 if a.scenario=='party' else 1)
   # Use the real serializer, then seed a disposable encounter-position fixture.
   # This is not a claim that the natural travel route to this area was validated.
   key('F5',.8);key('Return',.3)
   save=saves/'savgamQ.dat';wait(save.exists)
   raw=bytearray(save.read_bytes())
   # SaveLoad: disk byte + 2048-byte zone0. GbvmViews.previousScriptId=0x49F2.
   offset=1+(0x49F2-0x4900)*2;raw[offset:offset+2]=(a.script).to_bytes(2,'little')
   # The eight trailer bytes precede MAX_PARTY (8) Pascal String[40] names.
   trailer=len(raw)-8*41-8;raw[trailer:trailer+3]=bytes([a.x,a.y,a.facing])
   save.write_bytes(raw);seeded_save_hash=hashlib.sha256(save.read_bytes()).hexdigest();key('F8',1)
   for prompt in range(8):
    v=capture('fixture-load-'+str(prompt))
    if v['interfaceState']==5 or v['script']==a.script and v['interfaceState']==4:break
    key('Return',.5)
   encountered=None
   for step in range(20):
    if v['interfaceState']==5:encountered=v;break
    key('Up',.7);v=capture('walk-'+str(step))
    if v['interfaceState']==5:encountered=v;break
    # Accept script text/encounter prompts, then continue walking.
    for prompt in range(2):
     key('Return',.4);v=capture('prompt-'+str(step)+'-'+str(prompt))
     if v['interfaceState']==5:encountered=v;break
    if encountered:break
   assert encountered is not None,'No encounter on eastward route'
   for attempt in range(8):
    key('Return',.4);v=capture('entry-'+str(attempt))
    if v['arenaCount']>0:break
   assert v['arenaCount']>0,'Battle never reached first round'
   first=capture('combat-first-round')
   if a.scenario=='inspect':
    print(json.dumps(first,indent=2));raise SystemExit(0)
   if a.scenario=='flee':
    assert not first['duel'],'Use regular combat, not a duel'
    from collections import deque
    dirs=[(0,-1,'Up'),(1,-1,'Prior'),(1,0,'Right'),(1,1,'Next'),(0,1,'Down'),(-1,1,'End'),(-1,0,'Left'),(-1,-1,'Home')]
    def escape_step(s):
     hero=next(p for p in s['party'] if p['name']=='PASS HERO');loc=hero['combat'];cost=s['combatMap']['costs'];occ=s['combatMap']['occupants'];start=(loc['x'],loc['y']);q=deque([(start,[])]);seen={start}
     while q:
      (x,y),path=q.popleft()
      for dx,dy,k in dirs:
       tx,ty=x+dx,y+dy
       if not(0<=tx<50 and 0<=ty<25) or cost[ty][tx]==0:return path+[k]
       if (tx,ty) not in seen and cost[ty][tx]<255 and (occ[ty][tx]==0 or (tx,ty)==start):seen.add((tx,ty));q.append(((tx,ty),path+[k]))
     raise RuntimeError('No flee boundary reachable')
    path=escape_step(first);(out/'planned-flee-path.json').write_text(json.dumps(path)+'\n');key('Escape');key('m')
    for action in range(150):
     v=capture('flee-before-'+str(action))
     if v['interfaceState']!=5:break
     hero=next(p for p in v['party'] if p['name']=='PASS HERO')
     if hero['status']==3:break
     if v['activeCreature']!='PASS HERO':time.sleep(.4);continue
     
     if not any('MoveMenu' in f for f in v['inputStack']):key('m');v=capture('flee-move-menu-'+str(action))
     path=escape_step(v);key(path[0],.3);step=capture('flee-move-'+str(action))
     if step['interfaceState']!=5:v=step;break
     hero2=next(p for p in step['party'] if p['name']=='PASS HERO')
     if hero2['combat'] and hero2['combat']['x']==hero['combat']['x'] and hero2['combat']['y']==hero['combat']['y']:
      # Off-arena movement asks Flee: Yes/No. Confirm only at a source-defined boundary.
      dx,dy,_=next(d for d in dirs if d[2]==path[0]);tx=hero['combat']['x']+dx;ty=hero['combat']['y']+dy
      if not(0<=tx<50 and 0<=ty<25) or v['combatMap']['costs'][ty][tx]==0:key('y',.5)
     # When movement is spent, finish the round through Done/Quit and reenter Move.
     observed=capture('flee-after-'+str(action))
     if observed['interfaceState']!=5:v=observed;break
     if next(p for p in observed['party'] if p['name']=='PASS HERO')['combat']['movement']<2:
      key('Return');key('Escape');key('d');key('q',.8);key('m')
    else:raise RuntimeError('Flee action budget exhausted')
    for prompt in range(8):
     v=capture('flee-summary-'+str(prompt))
     if v['interfaceState']!=5:break
     key('Return',.5)
    assert v['combatResult']==129 and not v['partyDefeated'] and v['partySize']==1,'No actual party flee result observed'
    outcome=v
   else:
    if a.scenario=='party':
     assert first['partySize']==2 and any(p['name']=='REST ALLY' and p['status']==4 and p['hp']==1 for p in first['party']),'Missing seeded unconscious member'
    key('Escape');key('Alt_L+q',.5)
    for action in range(150):
     v=capture('fight-'+str(action))
     if v['interfaceState']!=5:break
     key('Return',.3);key('Alt_L+q',.3)
    else:raise RuntimeError('Combat did not conclude')
    assert not v['partyDefeated'] and v['party'][0]['experience']>first['party'][0]['experience'],'No victory'
    outcome=v
    if a.scenario=='party':
     ally=next(p for p in v['party'] if p['name']=='REST ALLY');assert ally['status']==0 and ally['enabled']==1 and ally['hp']==1,'Unconscious positive-HP member did not recover'
   def exploring(s):
    stack=s.get('inputStack',[])
    return s['interfaceState']==4 and 'Explore.MapMainMenu' in stack and not any(f.startswith(('ViewChar.','Camp.','CompTool.','MapView.','PostCbt.')) for f in stack)
   # Summary -> loot; collect an actual dropped item when required.
   for boundary in range(12):
    v=capture('loot-boundary-'+str(boundary));stack=v['inputStack']
    if exploring(v):break
    if any('PostCombatSummary' in f for f in stack):key('Return',.5)
    elif any('ViewChar.' in f for f in stack):key('Escape');key('e')
    elif any('LootMenu' in f for f in stack):break
    else:key('Return',.5)
   v=capture('loot-screen')
   if a.scenario=='loot':
    assert v['treasure'],'Encounter did not drop an item; preserve this unaccepted attempt'
    initial_items=sum(len(p['items']) for p in v['party'])
    for take_step in range(12):
     v=capture('item-menu-'+str(take_step));stack=v['inputStack']
     if sum(len(p['items']) for p in v['party'])>initial_items:break
     if any('LootTakeItem' in f for f in stack):key('Return')
     elif any('LootTakeMenu' in f for f in stack):key('i')
     elif any('ViewChar.' in f for f in stack):key('Escape');key('e')
     else:key('t')
    assert sum(len(p['items']) for p in v['party'])>initial_items,'Loot item not transferred'
    (out/'item-acquired.json').write_text(json.dumps(v,indent=2)+'\n')
   for prompt in range(15):
    v=capture('postcombat-'+str(prompt))
    if exploring(v):break
    stack=v['inputStack']
    if any(f.startswith('ViewChar.') for f in stack):key('e')
    elif any('GoBackForIt' in f for f in stack):key('n')
    elif any('LootTakeItem' in f for f in stack):key('Escape')
    elif any('LootTakeMenu' in f for f in stack):key('e')
    elif any('LootMenu' in f for f in stack):key('e')
    else:key('Return')
   assert exploring(v) and all(not p['combatRecord'] for p in v['party']),'No actual exploration-menu cleanup'
   post=v;key('F5',.8);key('Return');saved=capture('saved');assert exploring(saved),'Save left an unexpected submenu'
   written_save_hash=hashlib.sha256(save.read_bytes()).hexdigest();assert written_save_hash!=seeded_save_hash,'F5 did not rewrite the seeded save'
   key('Right');altered=capture('altered');assert altered['facing']!=saved['facing'],'Turn did not change facing in actual exploration'
   n=text().count('READY explore');key('F8',1);wait(lambda:text().count('READY explore')>n);loaded=capture('loaded');assert exploring(loaded),'Load left an unexpected submenu'
   assert saved['party']==loaded['party'],'Multi-field party serialization mismatch'
   assert [saved[k] for k in ['script','x','y','facing']]==[loaded[k] for k in ['script','x','y','facing']]
   assert before==hashes()
   result={'scenario':a.scenario,'accepted':True,'synthetic_precombat_seed':True,'native_actions':True,'forced_combat_status_changes':False,'natural_campaign_progression':False,'first':first,'outcome':outcome,'postcombat':post,'saved':saved,'loaded':loaded,'altered':altered,'seeded_save_sha256':seeded_save_hash,'written_save_sha256':written_save_hash,'original_files_checked':len(before),'original_media_unchanged':True}
   (out/'extended-result.json').write_text(json.dumps(result,indent=2)+'\n');print(a.scenario,'native lifecycle PASS',flush=True)
 finally:
  if game is not None and game.poll()==None:game.terminate();game.wait(timeout=10)
  if server is not None:server.terminate();server.wait(timeout=10)

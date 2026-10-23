#!/usr/bin/env python3
"""Native keyboard encounter replay; synthetic character and disposable state, original media read-only."""
import pathlib,subprocess,tempfile,time,os,json,argparse,hashlib,signal
root=pathlib.Path(__file__).resolve().parents[1];p=argparse.ArgumentParser();p.add_argument('--complete',action='store_true');p.add_argument('--defeat',action='store_true');p.add_argument('--script',type=int,default=11);p.add_argument('--x',type=int,default=6);p.add_argument('--y',type=int,default=1);p.add_argument('--facing',type=int,choices=[0,2,4,6],default=2);p.add_argument('--data',default=str(pathlib.Path.home()/'Downloads/poolofrad'));p.add_argument('--output',default=str(root/'evidence/combat-entry-2026-10-22/native'));a=p.parse_args();out=pathlib.Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True);caps=out/'captures';caps.mkdir(exist_ok=True);
if a.defeat and not a.complete:p.error('--defeat requires --complete')
if any(caps.iterdir()):p.error('Use a fresh output directory; existing captures are never reused.')
dotnet=str(root/'scripts/dotnet.sh')
subprocess.run([dotnet,'build',str(root/'src/Por.Desktop'),'-v:q'],check=True,cwd=root)
original=pathlib.Path(a.data);hashes=lambda:{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in original.iterdir() if f.is_file()};before=hashes();game=server=None
with tempfile.TemporaryDirectory(prefix='por-adventure-native-') as temp:
 temp=pathlib.Path(temp);state=temp/'state';saves=state/'saves';saves.mkdir(parents=True);fixture=saves/'PASSHERO.cha';subprocess.run([dotnet,'run','--project',str(root/'tests/Por.Adventure.Tests'),'--','--combat-fixture' if a.complete and not a.defeat else '--fixture',str(fixture)],check=True,cwd=root);fixture_hash=hashlib.sha256(fixture.read_bytes()).hexdigest()
 try:
  with (temp/'display').open('w+') as display,(out/'xvfb.log').open('w') as log:
   server=subprocess.Popen(['Xvfb','-displayfd',str(display.fileno()),'-screen','0','1280x960x24','-nolisten','tcp'],pass_fds=(display.fileno(),),stdout=log,stderr=log)
   while not (temp/'display').read_text().strip():
    if server.poll()!=None:raise RuntimeError('Xvfb failed')
    time.sleep(.05)
  env=dict(os.environ,DISPLAY=':'+(temp/'display').read_text().strip())
  with (out/'run.log').open('w') as log:
   game=subprocess.Popen([str(root/'play-desktop.sh'),'--data',str(original),'--state',str(state),'--captures',str(caps),'--short-intro'],cwd=root,env=env,stdout=log,stderr=subprocess.STDOUT)
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
   assert 'RECOVERY restored 1 orphan character(s)' in text();assert fixture_hash==hashlib.sha256(fixture.read_bytes()).hexdigest();recovered_roster=(saves/'charlist.txt').read_text();assert 'PASS HERO' in recovered_roster
   for k in ['a','Return','e','b']:key(k,.5)
   for page in range(50):
    if 'READY explore' in text():break
    key('Return',.85)
   else:raise RuntimeError('Opening script never reached exploration')
   wait(lambda:'READY explore' in text());base=capture('exploration');assert base['interfaceState']==4 and base['partySize']==1
   # Use the real serializer, then seed a disposable encounter-position fixture.
   # This is not a claim that the natural travel route to this area was validated.
   key('F5',.8);key('Return',.3)
   save=saves/'savgamQ.dat';wait(save.exists)
   raw=bytearray(save.read_bytes())
   # SaveLoad: disk byte + 2048-byte zone0. GbvmViews.previousScriptId=0x49F2.
   offset=1+(0x49F2-0x4900)*2;raw[offset:offset+2]=(a.script).to_bytes(2,'little')
   # The eight trailer bytes precede MAX_PARTY (8) Pascal String[40] names.
   trailer=len(raw)-8*41-8;raw[trailer:trailer+3]=bytes([a.x,a.y,a.facing])
   save.write_bytes(raw);key('F8',1)
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
   if a.defeat:
    # Normal level1 fixture, real orc turns: do not force HP/status during the fight.
    for action in range(80):
     v=defeat_capture('defeat-step-'+str(action))
     if v['partyDefeated']:break
     # Observe after every action: an extra Return could dismiss game-over and reset defeat flags.
     for k in ['Escape','d','g']:
      key(k,.4);v=defeat_capture('defeat-action-'+str(action)+'-'+k)
      if v['partyDefeated']:break
     if v['partyDefeated']:break
    else:raise RuntimeError('Defeat not reached within 80 guard actions')
    assert v['partySize']==0 and v['party']==[] and v['combatResult']==0x80,'Defeated party/result not cleaned up'
    defeated=v;n=text().count('READY menu');menu=capture('defeat-current')
    if menu['interfaceState']!=0:
     key('Return',.5);wait(lambda:text().count('READY menu')>n)
    menu=capture('defeat-main-menu');assert menu['interfaceState']==0
    # The seeded quicksave still has the living pre-encounter hero; load it through the real main menu.
    key('l',.5);key('Return',.5);key('b',.7)
    for prompt in range(10):
     v=capture('defeat-reload-'+str(prompt))
     if v['interfaceState']==4 and v['partySize']==1:break
     key('Return',.4)
    assert v['interfaceState']==4 and v['partySize']==1 and v['party'][0]['hp']==20,'Recovery did not restore living saved party'
    assert v['party'][0]['experience']==0 and not v['party'][0]['combatRecord']
    assert before==hashes()
    (out/'defeat-result.json').write_text(json.dumps({'defeat_and_recovery':True,'defeated':defeated,'menu':menu,'loaded':v,'original_files_checked':len(before),'original_media_unchanged':True},indent=2)+'\n')
    print('Native defeat -> main menu -> saved-party recovery PASS',flush=True)
    raise SystemExit(0)
   # Battle announcement can still be waiting for its pause; advance to a combat menu.
   for attempt in range(8):
    key('Return',.6);v=capture('entry-'+str(attempt))
    if v['arenaCount']>0:break
   assert v['arenaCount']>0,'Battle never reached first round'
   first=capture('combat-first-round');initial=first['round']
   # Done -> Guard consumes a PC turn and lets real enemy AI/round scheduling run.
   key('Escape',.3) # Leave Move if a confirmation selected its default menu item.
   for turn in range(12):
    key('d',.4);key('g',1);v=capture('guard-'+str(turn))
    if v['round']>initial:break
    key('Return',.4)
   assert v['round']>initial,'Guarding did not advance a combat round'
   assert v['interfaceState']==5
   if a.complete:
    # Real combat/AI only. No victory cheat or enemy-state editing.
    key('Escape',.2);key('q',.4)
    for action in range(100):
     key('Return',.3);v=capture('complete-'+str(action))
     if v['interfaceState']!=5:break
     key('q',.2)
    else:raise RuntimeError('Combat did not conclude within 100 controller actions')
    summary=v
    assert not summary['partyDefeated'],'Synthetic fighter lost: preserve evidence, do not call it a victory'
    assert summary['party'][0]['experience']>first['party'][0]['experience'],'No victory XP observed (duel defeat is not a successful completion)'
    # Dismiss summary, leave loot and complete any post-encounter message.
    for prompt in range(12):
     key('Return',.3);key('e',.3);key('n',.3);v=capture('postcombat-'+str(prompt))
     if v['interfaceState']==4:break
    assert v['interfaceState']==4,'Postcombat never returned to exploration'
    assert len(v['party'])==1 and not v['party'][0]['combatRecord'],'Enemy/combat records not cleaned up'
    post=v
    for attempt in range(5):
     key('F5',.6);key('Return',.3)
     # The same quicksave path exists already; capture is the saved-state baseline.
     v=capture('post-save-'+str(attempt))
     if v['interfaceState']==4:break
    baseline=v;key('Right');key('F8',1);v=capture('post-load')
    assert v['party']==baseline['party'],'Postcombat HP/XP/coins/items did not survive serialization'
    assert (v['script'],v['x'],v['y'],v['facing'])==(baseline['script'],baseline['x'],baseline['y'],baseline['facing'])
    from PIL import Image
    image=Image.open(out/'post-load.png').convert('RGB');party_text_pixels=sum(image.getpixel((x,y))==(255,255,255) for y in range(28,40) for x in range(184,280));assert party_text_pixels>25,'Loaded character-name text is blank (border pixels excluded)'
    (out/'lifecycle.json').write_text(json.dumps({'complete':True,'fixture':'level8 synthetic fighter, sword and37platinum, strength18/100; real dice/AI, no victory cheat','postcombat':post,'saved':baseline,'loaded':v,'party_name_text_pixels':party_text_pixels},indent=2)+'\n')
   assert before==hashes()
   result={'native_combat_entry':True,'lifecycle_complete':a.complete,'initial_round':initial,'later_round':v['round'],'arena_count':v['arenaCount'],'script':v['script'],'map':[v['x'],v['y']],'original_files_checked':len(before),'original_media_unchanged':True,'scope':f'synthetic disposable quicksave at script{a.script} ({a.x},{a.y}); real SDL load, movement into encounter, guard and enemy turns; not travel-route/campaign validation'}
   (out/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2),flush=True)

 finally:
  if game is not None and game.poll()==None:game.terminate();game.wait(timeout=10)
  if server is not None:server.terminate();server.wait(timeout=10)

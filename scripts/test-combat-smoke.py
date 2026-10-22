#!/usr/bin/env python3
"""Native keyboard encounter replay; synthetic character and disposable state, original media read-only."""
import pathlib,subprocess,tempfile,time,os,json,argparse,hashlib,signal
root=pathlib.Path(__file__).resolve().parents[1];p=argparse.ArgumentParser();p.add_argument('--data',default=str(pathlib.Path.home()/'Downloads/poolofrad'));p.add_argument('--output',default=str(root/'evidence/combat-entry-2026-10-22/native'));a=p.parse_args();out=pathlib.Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True);caps=out/'captures';caps.mkdir(exist_ok=True);dotnet=str(root/'scripts/dotnet.sh')
subprocess.run([dotnet,'build',str(root/'src/Por.Desktop'),'-v:q'],check=True,cwd=root)
original=pathlib.Path(a.data);hashes=lambda:{f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in original.iterdir() if f.is_file()};before=hashes();game=server=None
with tempfile.TemporaryDirectory(prefix='por-adventure-native-') as temp:
 temp=pathlib.Path(temp);state=temp/'state';saves=state/'saves';saves.mkdir(parents=True);fixture=saves/'PASSHERO.cha';subprocess.run([dotnet,'run','--project',str(root/'tests/Por.Adventure.Tests'),'--','--fixture',str(fixture)],check=True,cwd=root);fixture_hash=hashlib.sha256(fixture.read_bytes()).hexdigest()
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
   offset=1+(0x49F2-0x4900)*2;raw[offset:offset+2]=(11).to_bytes(2,'little')
   # The eight trailer bytes precede MAX_PARTY (8) Pascal String[40] names.
   trailer=len(raw)-8*41-8;raw[trailer:trailer+3]=bytes([6,1,2])
   save.write_bytes(raw);key('F8',1)
   for prompt in range(8):
    v=capture('fixture-load-'+str(prompt))
    if v['interfaceState']==5 or v['script']==11 and v['interfaceState']==4:break
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
   assert before==hashes()
   result={'native_combat_entry':True,'initial_round':initial,'later_round':v['round'],'arena_count':v['arenaCount'],'script':v['script'],'map':[v['x'],v['y']],'original_files_checked':len(before),'original_media_unchanged':True,'scope':'synthetic disposable quicksave at script11 (6,1); real SDL load, movement into encounter, guard and enemy turns; not travel-route/campaign validation'}
   (out/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2),flush=True)

 finally:
  if game is not None and game.poll()==None:game.terminate();game.wait(timeout=10)
  if server is not None:server.terminate();server.wait(timeout=10)

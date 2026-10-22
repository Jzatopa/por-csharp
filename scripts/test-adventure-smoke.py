#!/usr/bin/env python3
"""Native launcher -> orphan roster recovery -> add -> begin/tour -> exploration -> quick save/load -> camp/rest. Synthetic character, disposable state."""
import pathlib,subprocess,tempfile,time,os,json,argparse,hashlib,signal
root=pathlib.Path(__file__).resolve().parents[1];p=argparse.ArgumentParser();p.add_argument('--data',default=str(pathlib.Path.home()/'Downloads/poolofrad'));p.add_argument('--output',default=str(root/'evidence/adventure-entry-2026-10-22/native'));a=p.parse_args();out=pathlib.Path(a.output).resolve();out.mkdir(parents=True,exist_ok=True);caps=out/'captures';caps.mkdir(exist_ok=True);dotnet=str(root/'scripts/dotnet.sh')
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
   key('Right');turned=capture('turned');assert turned['facing']==(base['facing']+2)%8
   key('Left');key('Up',1);moved=capture('moved');assert (moved['x'],moved['y'],moved['script'])!=(base['x'],base['y'],base['script'])
   # A boundary arrival may still be at the script's press-any-key message. Finish that message before retrying the hotkey.
   for attempt in range(5):
    key('F5',.8)
    if (saves/'savgamQ.dat').exists():break
    key('Return',.8)
   else:raise RuntimeError('F5 did not create a quicksave after arrival messages')
   key('Return');saved=capture('saved');assert (saves/'savgamQ.dat').stat().st_size>=13137
   key('Right');altered=capture('altered');assert altered['facing']!=saved['facing'];n=text().count('READY explore');key('F8',1);wait(lambda:text().count('READY explore')>n);loaded=capture('loaded');
   from PIL import Image
   image=Image.open(out/'loaded.png').convert('RGB');party_pixels=sum(image.getpixel((x,y))!=(0,0,0) for y in range(8,112) for x in range(136,312));assert party_pixels>100,'loaded party panel blank'
   assert (loaded['x'],loaded['y'],loaded['facing'],loaded['script'],loaded['partySize'],loaded['hp'])==(saved['x'],saved['y'],saved['facing'],saved['script'],saved['partySize'],saved['hp'])
   key('e');camp=capture('camp');assert camp['interfaceState']==2
   key('r');key('i');key('r',1);rested=capture('rested');assert rested['clock']!=camp['clock']
   # Preserve a valid quicksave, then leave through Camp's Save/quit route (cancel the second save slot dialog).
   key('s');key('Escape');key('y',.1,False);code=game.wait(timeout=10);assert code==0
   assert before==hashes();result={'exit_code':code,'orphan_roster_recovered_without_character_mutation':True,'opening_script_reached_exploration':True,'turned_and_crossed_map_boundary':True,'quick_save_load_restored_position_facing_party_and_hp':True,'camp_rest_advanced_clock':True,'loaded_party_panel_colored_pixels':party_pixels,'original_files_checked':len(before),'original_media_unchanged':True,'quicksave_world_bytes':(saves/'savgamQ.dat').stat().st_size,'scope':'synthetic saved character through real native UI; not a full game completion test'};(out/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2),flush=True)
 finally:
  if game is not None and game.poll()==None:game.terminate();game.wait(timeout=10)
  if server is not None:server.terminate();server.wait(timeout=10)

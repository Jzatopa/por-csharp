#!/usr/bin/env python3
"""Boot the real desktop on an isolated Xvfb, drive X11 keys and preserve capture/exit evidence. Requires local game media, SDL2, Xvfb and xdotool."""
import argparse,pathlib,subprocess,tempfile,time,os,json,hashlib,shutil
root=pathlib.Path(__file__).resolve().parents[1];p=argparse.ArgumentParser();p.add_argument('--data',default=str(pathlib.Path.home()/'Downloads/poolofrad'));p.add_argument('--output',default=str(root/'evidence/desktop-startup-2026-10-22/smoke'));args=p.parse_args();out=pathlib.Path(args.output).resolve();out.mkdir(parents=True,exist_ok=True);captures=out/'captures';captures.mkdir(exist_ok=True)
subprocess.run([str(root/'scripts/dotnet.sh'),'build',str(root/'src/Por.Desktop'),'-v:q'],check=True,cwd=root)
def digest_media():return {f.name:hashlib.sha256(f.read_bytes()).hexdigest() for f in pathlib.Path(args.data).iterdir() if f.is_file()}
before=digest_media();result={};game=None;server=None
with tempfile.TemporaryDirectory(prefix='por-desktop-smoke-') as temp:
 temp=pathlib.Path(temp)
 try:
  with (temp/'display').open('w+') as display,(out/'xvfb.log').open('w') as xvlog:
   server=subprocess.Popen(['Xvfb','-displayfd',str(display.fileno()),'-screen','0','1280x960x24','-nolisten','tcp'],pass_fds=(display.fileno(),),stdout=xvlog,stderr=xvlog)
   deadline=time.monotonic()+10
   while not (temp/'display').read_text().strip():
    if server.poll() is not None or time.monotonic()>deadline:raise RuntimeError('Xvfb failed')
    time.sleep(.05)
  env=dict(os.environ,DISPLAY=':'+(temp/'display').read_text().strip())
  with (out/'run.log').open('w') as log:
   game=subprocess.Popen([str(root/'play-desktop.sh'),'--data',args.data,'--state',str(temp/'state'),'--captures',str(captures)],cwd=root,env=env,stdout=log,stderr=subprocess.STDOUT)
   def wait_for(token,timeout=40):
    limit=time.monotonic()+timeout
    while token not in (out/'run.log').read_text():
     if game.poll() is not None:raise RuntimeError((out/'run.log').read_text())
     if time.monotonic()>limit:raise TimeoutError(token)
     time.sleep(.05)
   def key(name):subprocess.run(['xdotool','key','--window',window,name],env=env,check=True);time.sleep(.2)
   wait_for('READY intro');window=subprocess.check_output(['xdotool','search','--pid',str(game.pid),'--name','Pool of Radiance'],env=env,text=True).strip().splitlines()[-1]
   # Original intro: picture1 five seconds, then picture2 five seconds, then credits. No game call or fake input seam is replaced.
   time.sleep(5.8);key('F12');result['title2_capture']=sorted(captures.glob('*keyboard-f12.png'))[-1].name
   if shutil.which('import'):subprocess.run(['import','-window',window,str(out/'title-window.png')],env=env,check=True)
   wait_for('READY menu');
   if shutil.which('xwd'):subprocess.run(['xwd','-silent','-id',window,'-out',str(out/'main-menu-window.xwd')],env=env,check=True)
   key('g');key('Down');key('Right');key('F12');result['settings_capture']=sorted(captures.glob('*keyboard-f12.png'))[-1].name
   if shutil.which('import'):subprocess.run(['import','-window',window,str(out/'settings-window.png')],env=env,check=True)
   key('e');key('e');key('F12');result['quit_capture']=sorted(captures.glob('*keyboard-f12.png'))[-1].name
   key('n');assert game.poll() is None,'No should cancel quit';key('e');key('y');code=game.wait(timeout=10);assert code==0,code
   text=(out/'run.log').read_text();assert 'EXIT 0' in text and 'STATE data-built failed=False' in text
   assert (temp/'state/saves/POOLOPT.CFG').is_file(),'settings did not save into writable save folder'
   (out/'saved-options.txt').write_bytes((temp/'state/saves/POOLOPT.CFG').read_bytes())
   result.update(exit_code=code,settings_saved=True,quit_cancelled_then_confirmed=True,original_media_unchanged=before==digest_media(),original_files_checked=len(before),fresh_data_archives_built=True,x11_window_id=window,display=env['DISPLAY'])
   assert result['original_media_unchanged'];(out/'result.json').write_text(json.dumps(result,indent=2)+'\n');print(json.dumps(result,indent=2),flush=True)
 finally:
  if game is not None and game.poll() is None:game.terminate();game.wait(timeout=10)
  if server is not None:server.terminate();server.wait(timeout=10)

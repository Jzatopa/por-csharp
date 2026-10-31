#!/usr/bin/env python3
"""Build a fresh local Linux desktop release; no original data, saves, remote creation or upload."""
import argparse,datetime,hashlib,json,os,pathlib,re,subprocess,zipfile
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--version',required=True);p.add_argument('--output',type=pathlib.Path,required=True);a=p.parse_args()
if not re.fullmatch(r'[A-Za-z0-9._-]+',a.version):p.error('version must be a plain label')
out=a.output.resolve()
if out.exists():p.error('Use a fresh output directory')
if root==out or root.is_relative_to(out):p.error('Output must not contain the project')
out.mkdir(parents=True);app=out/'app'
subprocess.run([str(root/'scripts/dotnet.sh'),'publish',str(root/'src/Por.Desktop'),'-c','Release','-o',str(app),'-v:q'],check=True,cwd=root)
(app/'README.txt').write_text('Pool of Radiance C# desktop '+a.version+'\n\nLinux: .NET 8 and SDL2 required. Launch ./Por.Desktop --data /path/to/your/poolofrad\nOriginal game media and saves are not included. Writable state stays separate.\nSource attribution and third-party notices accompany the native library.\nNot SSI-identical DOS executables; not full-campaign completion certification.\n')
(app/'UPSTREAM.md').write_bytes((root/'docs/github/UPSTREAM.md').read_bytes())
files={str(f.relative_to(app)):hashlib.sha256(f.read_bytes()).hexdigest() for f in sorted(app.rglob('*')) if f.is_file()}
assert not any(pathlib.Path(name).suffix.lower() in ['.dax','.sav','.cha','.itm','.spc','.voc','.dat'] for name in files)
(app/'SHA256.txt').write_text(''.join(f'{digest}  {name}\n' for name,digest in files.items()))
(app/'MANIFEST.json').write_text(json.dumps(dict(version=a.version,platform='linux',original_media_included=False,files=files),indent=2)+'\n')
epoch=max(315532800,int(os.environ.get('SOURCE_DATE_EPOCH','315532800')));stamp=datetime.datetime.fromtimestamp(epoch,datetime.timezone.utc).timetuple()[:6];archive=out/('por-csharp-'+a.version+'-linux.zip')
with zipfile.ZipFile(archive,'x',compression=zipfile.ZIP_DEFLATED) as z:
 for f in sorted(app.rglob('*')):
  if f.is_file():
   item=zipfile.ZipInfo(str(pathlib.Path('por-csharp')/f.relative_to(app)),stamp);item.compress_type=zipfile.ZIP_DEFLATED;item.external_attr=(0o755 if os.access(f,os.X_OK) else 0o644)<<16;z.writestr(item,f.read_bytes())
print(json.dumps(dict(archive=str(archive),sha256=hashlib.sha256(archive.read_bytes()).hexdigest(),files=len(files),uploaded=False),indent=2))

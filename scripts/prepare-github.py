#!/usr/bin/env python3
"""Prepare a code-only, locally staged repository. Does not commit, create a remote or upload."""
import argparse, hashlib, json, pathlib, re, shutil, subprocess, tarfile

root = pathlib.Path(__file__).resolve().parents[1]
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--output', required=True, type=pathlib.Path)
p.add_argument('--media', type=pathlib.Path, help='Optional read-only media directory for exact-file hash exclusion checks')
a = p.parse_args()
out = a.output.resolve()
if out.exists():
    p.error('output already exists; choose a fresh path (existing work is never overwritten)')
if out == root or root.is_relative_to(out):
    p.error('output must not contain the working project')
media = a.media.resolve() if a.media else None
if media and (out == media or out.is_relative_to(media) or media.is_relative_to(out)):
    p.error('output and original media must be separate')

def transfer(source, relative):
    if source.is_symlink():
        raise ValueError('Refuse symlink input: '+str(relative))
    target = out/relative
    target.parent.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source, target)

# Production code and every C# test source, but never build outputs or local binaries.
for folder in ['src', 'tests']:
    for source in sorted((root/folder).rglob('*')):
        relative = source.relative_to(root)
        if any(part in ('bin', 'obj', '__pycache__') for part in relative.parts):
            continue
        if source.is_file() and (source.suffix in ('.cs', '.csproj') or source.name.endswith('.cs.in')):
            transfer(source, relative)
for relative in ['play-desktop.sh', '.gitignore', '.gitattributes', '.github/workflows/ci.yml',
 'scripts/dotnet.sh', 'scripts/test-portable.sh', 'scripts/recompare-source.py', 'scripts/prepare-github.py',
 'scripts/test-adventure-smoke.py', 'scripts/test-desktop-smoke.py', 'scripts/test-combat-smoke.py',
 'scripts/update-campaign-combat-coverage.py', 'docs/campaign-combat-coverage.json',
 'docs/TPC-MANAGED-REPLACEMENT.md']:
    transfer(root/relative, pathlib.Path(relative))
for name in ['README.md', 'UPSTREAM.md', 'AGENTS.md']:
    transfer(root/'docs/github'/name, pathlib.Path(name))
for name in ['TESTING.md', 'SOURCE-COMPARISON.md', 'COMBAT-ENTRY.md', 'CAMPAIGN-COMBAT.md']:
    transfer(root/'docs/github'/name, pathlib.Path('docs')/name)
# Keep the public templates so the code-only export can be reproduced from this snapshot.
for source in sorted((root/'docs/github').glob('*.md')):
    transfer(source, source.relative_to(root))

# Preserve public source integrity/dispositions, not user-directed chat prose or local evidence history.
ledger = json.loads((root/'docs/source-dispositions.json').read_text())
keys = ['source', 'source_sha256', 'disposition', 'targets', 'handled', 'compiled', 'source_tested', 'gameplay_runtime_validated']
public = dict(source_commit=ledger['source_commit'], inventory_total=ledger['inventory_total'],
 handled_current=ledger['handled_current'],
 policy='Source dispositions are not whole-game validation. Historical per-pass evidence is retained locally, not bundled.',
 files=[{k:row[k] for k in keys if k in row} for row in ledger['files']])
(out/'docs/source-dispositions.json').write_text(json.dumps(public, indent=2)+'\n')

original_hashes = set()
if media:
    original_hashes = {hashlib.sha256(f.read_bytes()).hexdigest() for f in media.iterdir() if f.is_file()}
patterns = [rb'(?i)(?:gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}|sk-[A-Za-z0-9_-]{24,})',
 rb'/ho'+rb'me/[^/\s]+/', rb'\b192\.168\.\d+\.\d+\b', rb'-----BEGIN (?:RSA |OPENSSH |EC )?PRIVATE KEY-----']
files = []
for file in sorted(out.rglob('*')):
    if not file.is_file():
        continue
    raw = file.read_bytes()
    # Decode every payload as text; no original binary/media payload is accepted.
    raw.decode('utf-8')
    # The scanner's own regex literals are not credentials or private paths.
    for pattern in patterns:
        if re.search(pattern, raw):
            raise ValueError('Publication scan requires review: '+str(file.relative_to(out)))
    digest = hashlib.sha256(raw).hexdigest()
    if digest in original_hashes:
        raise ValueError('Exact original-media file found: '+str(file.relative_to(out)))
    files.append(dict(path=str(file.relative_to(out)), bytes=len(raw), sha256=digest))
manifest = dict(source_commit=ledger['source_commit'], files=files, payload_files=len(files),
 production_files=sum(f['path'].startswith('src/') for f in files),
 test_source_files=sum(f['path'].startswith('tests/') for f in files),
 original_media_hashes_excluded=len(original_hashes),
 excluded_categories=['game media', 'personal saves', 'raw evidence/model exchanges', 'build outputs', 'SDK/native binaries', 'local historical orchestration'],
 limit='Pattern checks are not exhaustive secret detection. No redistribution rights are established by this snapshot.',
 manifest_self_excluded=True)
(out/'SNAPSHOT.json').write_text(json.dumps(manifest, indent=2)+'\n')
subprocess.run(['git', 'init', '-q', '-b', 'main', str(out)], check=True)
# Explicit file set, never a blanket add of the working project.
subprocess.run(['git', '-C', str(out), 'add', '--']+[f['path'] for f in files]+['SNAPSHOT.json'], check=True)
archive = out.with_name(out.name+'.tar.gz')
if archive.exists():
    raise FileExistsError(archive)
with tarfile.open(archive, 'x:gz') as tar:
    for entry in files+[dict(path='SNAPSHOT.json')]:
        tar.add(out/entry['path'], arcname='por-csharp/'+entry['path'])
print(json.dumps(dict(snapshot=str(out), archive=str(archive),
 archive_sha256=hashlib.sha256(archive.read_bytes()).hexdigest(),
 payload_files=len(files)+1, production_files=manifest['production_files'],
 test_source_files=manifest['test_source_files'], committed=False, uploaded=False), indent=2))

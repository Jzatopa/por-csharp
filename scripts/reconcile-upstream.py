#!/usr/bin/env python3
"""Audit every pinned tracked upstream file; explicit ports/replacements/resources, never campaign completion."""
import argparse,collections,hashlib,json,pathlib,shutil,subprocess
root=pathlib.Path(__file__).resolve().parents[1]
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--reference',type=pathlib.Path,required=True);p.add_argument('--write',action='store_true');a=p.parse_args();reference=a.reference.resolve()
ledger=json.loads((root/'docs/source-dispositions.json').read_text());commit=subprocess.check_output(['git','-C',str(reference),'rev-parse','HEAD'],text=True).strip();assert commit==ledger['source_commit']
tracked=subprocess.check_output(['git','-C',str(reference),'ls-tree','-r','--name-only','HEAD'],text=True).splitlines();by_source={r['source']:r for r in ledger['files']}
changes={
 'bench/sndbench.pas':('csharp-benchmark-port',['src/Por.UpstreamTools/SoundBenchmark.cs','src/Por.UpstreamTools/BenchmarkHosts.cs','tests/Por.Upstream.Tests/Program.cs'],'Three-state source loop/cost/order independently checked; real managed sequencer/RAM bus in both source modes; not DOS CPU timing equivalence.'),
 'bench/vidbench.pas':('csharp-benchmark-port',['src/Por.UpstreamTools/VideoBenchmark.cs','src/Por.UpstreamTools/BenchmarkHosts.cs','tests/Por.Upstream.Tests/Program.cs'],'Four source workloads/coordinate order/rate formula independently checked; real source primitives in C/E/T RAM graphics buses; not DOS/display timing equivalence.'),
 'tests/sndtrc.pas':('csharp-trace-harness-replacement',['src/Por.UpstreamTools/SoundTrace.cs','src/Por.Core/Sound/SoundDriver.cs','tests/Por.Upstream.Tests/Program.cs'],'Required trace API through real managed driver; 0..64-word capacity/reset/drop/port packing checked, plus driver golden and OPL reset consumers.'),
 'tests/tsndseq.pas':('csharp-test-replacement',['tests/Por.FinalThree.Tests/Program.cs','tests/Por.FinalThree.Tests/SoundGoldenRecords.cs'],'SoundGolden: all 58 upstream SSI-recorded per-tick write-count/CRC/tick-count tuples, no historical evidence file dependency.'),
 'tests/tsndopl.pas':('csharp-test-replacement',['tests/Por.FinalThree.Tests/AdlibChecks.cs'],'28 tunes x 400 ticks, tone pitch/level/key/noise checks, reset size and sounding/noisy sample thresholds. Actual existing implementation reverified; prior missing claim was stale.')}
for name in ['bench/TPC.CFG','tests/TPC.CFG']:
 changes[name]=('replaced-build-configuration',['docs/TPC-MANAGED-REPLACEMENT.md','src/Por.UpstreamTools/Por.UpstreamTools.csproj','tests/Por.Upstream.Tests/Por.Upstream.Tests.csproj'],'Every 12-line switch mapped to explicit managed build/runtime disposition. No claim of DOS far calls/stack/CPU identity.')
for name in tracked:
 if name.startswith('tests/scenarios/POOL') and name.endswith('.CFG'):
  changes[name]=('managed-config-input-replacement',['src/Por.UpstreamTools/LegacyTools.cs','tests/UpstreamScenarios/'+pathlib.Path(name).name,'tests/Por.Upstream.Tests/Program.cs'],'All five source config fields preserved/parsed; real C/E/T/V engine driver installed in tests. Desktop window remains VGA, not a new native CGA/EGA renderer.')
# Deterministic source-only extraction; never derive expected CRCs from production output.
import re
source=(reference/'tests/tsndseq.pas').read_text().split('EXPECTED:',1)[1].split('\nvar',1)[0]
records=re.findall(r'ticks:\s*(\d+);\s*writes:\s*(\d+);\s*crc:\s*\$([A-F0-9]+)',source)
assert len(records)==58
expected='// SSI-recorded constants from pinned tests/tsndseq.pas; not generated from the managed driver.\nstatic class SoundGoldenRecords { public static readonly string[][] Rows = new string[][] {\n'+''.join(' new[]{"%s","%s","%s","%s","%s"},\n'%(i//29,i%29,t,w,c) for i,(t,w,c) in enumerate(records))+'}; }\n'
constant_file=root/'tests/Por.FinalThree.Tests/SoundGoldenRecords.cs'
if a.write and not constant_file.exists():constant_file.parent.mkdir(parents=True,exist_ok=True);constant_file.write_text(expected)
assert constant_file.read_text()==expected,'Generated SSI records differ from pinned source; inspect, do not overwrite'
for name in tracked:
 if name.startswith('tests/scenarios/') and pathlib.Path(name).suffix.lower() in ['.txt','.cfg']:
  original=reference/name;target=root/'tests/UpstreamScenarios'/original.name
  if a.write and not target.exists():target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(original,target)
  assert target.read_bytes()==original.read_bytes(),('Scenario/config resource differs',name)

for name,(disposition,targets,scope) in changes.items():
 row=by_source[name]
 if a.write:row.update(disposition=disposition,targets=targets,handled=True,compiled=True,generated=disposition=='csharp-benchmark-port',source_tested=True,source_test_scope=scope,gameplay_runtime_validated=False);row.pop('blocker',None)
 else:assert row['handled'] and row['disposition']==disposition,(name,'not reconciled')
for name,row in by_source.items():
 assert hashlib.sha256((reference/name).read_bytes()).hexdigest()==row['source_sha256'],name
 if row['handled']:assert row['targets'] and all((root/t).exists() for t in row['targets']),('missing target',name)
ledger['handled_current']=sum(bool(r['handled']) for r in ledger['files'])
assert ledger['handled_current']==ledger['inventory_total']==165
if a.write:
 (root/'docs/source-dispositions.json').write_text(json.dumps(ledger,indent=2)+'\n')
 inventory=json.loads((root/'docs/source-inventory.json').read_text())
 for row in inventory['files']:
  match=by_source[row['source']]
  row['targets']=match['targets'];row['status']='handled' if match['handled'] else 'pending'
 (root/'docs/source-inventory.json').write_text(json.dumps(inventory,indent=2)+'\n')

# Entire original repository, including non-language files intentionally absent from the source-unit denominator.
special={
 'tools/sbsfx.py':('verified-recorded-pack-builder-replacement',['scripts/build-sbsfx.py','scripts/test-sbsfx.py']),
 'build/ovlsizes.py':('csharp-tool-port',['src/Por.UpstreamTools/LegacyTools.cs','tests/Por.Upstream.Tests/Program.cs']),
 'tests/ovlloads.py':('csharp-tool-port',['src/Por.UpstreamTools/LegacyTools.cs','tests/Por.Upstream.Tests/Program.cs']),
 'bench/sndbench.sh':('managed-benchmark-launcher-replacement',['src/Por.UpstreamTools/Program.cs','src/Por.UpstreamTools/SoundBenchmark.cs']),
 'bench/vidbench.sh':('managed-benchmark-launcher-replacement',['src/Por.UpstreamTools/Program.cs','src/Por.UpstreamTools/VideoBenchmark.cs']),
 'build.sh':('managed-build-replacement',['scripts/dotnet.sh','play-desktop.sh','scripts/test-portable.sh']),
 'build/build.bat':('managed-build-replacement',['src/Por.Core/Por.Core.csproj','src/Por.Desktop/Por.Desktop.csproj']),
 'build/toolchain':('managed-toolchain-replacement',['scripts/dotnet.sh','docs/TPC-MANAGED-REPLACEMENT.md']),
 'package.sh':('managed-release-package-replacement',['scripts/package-desktop.py']),
 '.forgejo/scripts/release.sh':('managed-draft-release-helper-replacement',['scripts/release-desktop.py']),
 '.forgejo/scripts/reproduce.sh':('managed-source-integrity-replacement',['scripts/recompare-source.py','scripts/reconcile-upstream.py','scripts/prepare-github.py']),
 '.forgejo/workflows/build.yml':('managed-ci-replacement',['.github/workflows/ci.yml','scripts/test-portable.sh']),
 'tests/regress.sh':('managed-native-regression-workflow-replacement',['scripts/test-adventure-smoke.py','scripts/test-combat-smoke.py','scripts/test-combat-extended.py','scripts/test-wilderness-native.py','src/Por.UpstreamTools/Scenario.cs','docs/REPOSITORY-CLOSURE-2026-10-23.md']),
 '.gitignore':('managed-build-ignore-replacement',['.gitignore']),
 '.gitattributes':('managed-source-attributes-replacement',['.gitattributes'])}
files=[]
for name in tracked:
 path=reference/name;raw=path.read_bytes();digest=hashlib.sha256(raw).hexdigest();row=dict(source=name,source_sha256=digest,bytes=len(raw),handled=True,gameplay_runtime_validated=False)
 if name in by_source:
  old=by_source[name];row.update(disposition=old['disposition'],targets=old['targets'],scope='source-inventory',verification=old.get('source_test_scope','See source-dispositions.json for per-unit verification and platform boundaries.'))
 elif name.startswith('tests/scenarios/saves/wild/'):
  row.update(disposition='external-unchanged-test-fixture',targets=['scripts/test-wilderness-native.py','docs/REPOSITORY-CLOSURE-2026-10-23.md'],scope='external-test-data',verification='All 28 fixture files were actually consumed by native six-PC load/navigation/save/reload test. Referenced by hash, not bundled as original game data; no language conversion applies.')
 else:
  preserved=pathlib.Path('references/upstream-pinned')/name
  if a.write:
   dest=root/preserved;dest.parent.mkdir(parents=True,exist_ok=True)
   if dest.exists():assert dest.read_bytes()==raw,('existing reference differs',name)
   else:shutil.copy2(path,dest)
  assert (root/preserved).read_bytes()==raw,('preserved reference missing or changed',name)
  if name in special:disposition,targets=special[name]
  elif name.startswith('tests/scenarios/') and path.suffix=='.txt':
   disposition='preserved-scenario-definition';targets=['tests/UpstreamScenarios/'+path.name,'src/Por.UpstreamTools/Scenario.cs','tests/Por.Upstream.Tests/Program.cs']
  else:
   assert path.suffix in ['.md','.txt'] or path.name in ['.gitignore','.gitattributes'],('unclassified original file',name)
   disposition='preserved-documentation-or-release-reference';targets=[str(preserved)]
  row.update(disposition=disposition,targets=targets,preserved_original=str(preserved),scope='repository-support',verification='Exact original bytes retained as historical reference; managed replacements are not SSI DOS binary reproduction or full scenario execution proof.')
 assert row['targets'] and all((root/t).exists() for t in row['targets']),('missing file disposition target',name)
 files.append(row)
full=dict(schema=1,source_commit=commit,tracked_upstream_files=len(files),handled=len(files),unclassified=0,source_inventory_inputs=165,source_inventory_handled=165,campaign_completion=False,policy='Handled is an explicit port, platform replacement, preserved text/scenario resource, or external unchanged test-fixture disposition. Not 227 C# translations, exhaustive assertion equivalence, SSI executable reproducibility, or full-campaign validation. DOS publishing helper is replaced with an explicit opt-in managed draft helper; no upload performed. Source fixture data remains external.',disposition_counts=dict(sorted(collections.Counter(r['disposition'] for r in files).items())),files=files)
output=root/'docs/repository-file-dispositions.json'
if a.write:output.write_text(json.dumps(full,indent=2)+'\n')
else:assert json.loads(output.read_text())==full,'Repository tracker drift; inspect before --write'
print(json.dumps({k:v for k,v in full.items() if k!='files'},indent=2))

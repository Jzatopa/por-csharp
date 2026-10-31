#!/usr/bin/env python3
"""Pinned-input integrity and routine-identifier audit, NOT semantic-equivalence proof."""
import argparse, hashlib, json, os, pathlib, re

root = pathlib.Path(__file__).resolve().parents[1]
p = argparse.ArgumentParser(description=__doc__)
p.add_argument('--reference', default=os.environ.get('POR_SOURCE_ROOT'))
p.add_argument('--output', type=pathlib.Path)
a = p.parse_args()
if not a.reference:
    p.error('provide --reference PINNED_CHECKOUT or POR_SOURCE_ROOT')
reference = pathlib.Path(a.reference).resolve()
ledger = json.loads((root/'docs/source-dispositions.json').read_text())
# Explicitly reviewed adaptations. Names below may disappear, but their behavior still needs tests.
adaptations = {
 'src/gameboot.pas': {'CountOvrRead': 'Initialize installs the counting OverlayReader closure; desktop replaces DOS overlays.'},
 'src/menucore.pas': {
  'ParseMenu': 'Inlined option-range parsing in HorizontalMenu.',
  'FirstNonBlank': 'Inlined leading-space scan in MenuVertical.Highlight.',
  'LastNonBlank': 'Inlined trailing-space scan in MenuVertical.Highlight.',
  'HighlightLine': 'MenuVertical.Highlight(at, true).',
  'UnhighlightLine': 'MenuVertical.Highlight(at, false).',
  'LastLabelBefore': 'Dropped discarded local q calculation in DrawPage; it has no observable source side effect.'},
 'src/options.pas': {'KeyName': 'Keys array.', 'OnOff': 'StoredValue/ShownValue flag expressions.',
  'Arrow': 'Inlined speedChoice/change-setting branches in OptionsMenu.',
  'MoveTo': 'Inlined redraw/selection branch in OptionsMenu.'},
 'src/sndsb.pas': {'Physical': 'Platform-supplied SoundDmaBlock.PhysicalAddress, including offset in DMA operations.'},
 'src/textrend.pas': {'FindNewIndex': 'Inlined post-wrap space skip in DisplayMessageWindow.'},
}
rows = []
for entry in ledger['files']:
    path = reference/entry['source']
    raw = path.read_bytes()
    assert hashlib.sha256(raw).hexdigest() == entry['source_sha256'], ('source changed', entry['source'])
    targets = [root/t for t in entry['targets']]
    if entry['handled']:
        assert targets and all(t.exists() for t in targets), ('missing target', entry['source'])
    if not entry['source'].startswith('src/'):
        rows.append(dict(source=entry['source'], source_sha256=entry['source_sha256'], handled=entry['handled'], routine_identifiers=0, adaptations={}, audit_scope='Auxiliary input hash/target existence only; not assertion equivalence'))
        continue
    names = set()
    if path.suffix.lower() == '.pas':
        text = re.sub(r"\{.*?\}|\(\*.*?\*\)|'(?:''|[^'])*'", ' ', raw.decode('cp437'), flags=re.S)
        names = set(re.findall(r'(?im)^\s*(?:procedure|function)\s+(\w+)', text))
    target = '\n'.join(t.read_text() for t in targets)
    target = re.sub(r'//[^\n]*|/\*.*?\*/|@?"(?:\\.|[^"\\])*"', ' ', target, flags=re.S)
    missing = sorted(n for n in names if not re.search(r'\b'+re.escape(n)+r'\b', target, re.I))
    known = adaptations.get(entry['source'], {})
    assert set(missing) == set(known), ('review identifier delta', entry['source'], missing, known)
    rows.append(dict(source=entry['source'], source_sha256=entry['source_sha256'],
      routine_identifiers=len(names), adaptations=known,
      targets={str(t.relative_to(root)):hashlib.sha256(t.read_bytes()).hexdigest() for t in targets}))
result = dict(source_commit=ledger['source_commit'], source_files_checked=len(rows),
  distinct_routine_identifiers=sum(r['routine_identifiers'] for r in rows),
  reviewed_adaptations=sum(len(r['adaptations']) for r in rows), rows=rows,
  limit='Identifier presence and pinned hashes are structural only; not every routine is behaviorally reverified by this scan.')
output = json.dumps(result, indent=2)+'\n'
if a.output:
    a.output.parent.mkdir(parents=True, exist_ok=True)
    a.output.write_text(output)
print(json.dumps({k:v for k,v in result.items() if k!='rows'}, indent=2))

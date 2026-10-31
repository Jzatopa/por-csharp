#!/usr/bin/env python3
"""Load an upstream six-PC save, with optional recorded variants, through the real SDL UI.

Requires an external pinned upstream checkout; no original media or save is bundled.
This is fixture-based wilderness navigation/serialization, not campaign completion.
"""
import argparse
import base64
import hashlib
import json
import os
import pathlib
import shutil
import subprocess
import tempfile
import time
import native_expanded_actions as expanded

ROOT = pathlib.Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--fixture', type=pathlib.Path, required=True)
parser.add_argument('--data', type=pathlib.Path, required=True)
parser.add_argument('--effects', type=pathlib.Path)
parser.add_argument('--combat', action='store_true', help='Walk fixed-seed wilderness until encounter, then complete native combat')
parser.add_argument('--spell', action='store_true', help='Explicitly cast the Raven memorized Magic Missile before Quick combat')
parser.add_argument('--item', action='store_true', help='Explicitly ready/use trade/ready/use the carried Potion of Giant Strength before Quick combat')
parser.add_argument('--action', choices=['ranged', 'heal', 'bless']+expanded.ACTIONS, help='Bounded explicit action; any disposable pre-load variant is recorded explicitly')
parser.add_argument('--output', type=pathlib.Path, required=True)
args = parser.parse_args()
assert sum(map(bool, [args.spell, args.item, args.action])) <= 1, 'Choose one explicit action family'
if args.spell or args.item or args.action: args.combat = True
out = args.output.resolve()
original = args.data.resolve()
fixture = args.fixture.resolve()
assert not out.exists(), 'Use a fresh evidence directory'
assert not any(out == p or out.is_relative_to(p) or p.is_relative_to(out)
               for p in [original, fixture]), 'Evidence must be separate from inputs'
out.mkdir(parents=True)
caps = out / 'captures'
caps.mkdir()

def hashes(directory):
    return {p.name: hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(directory.iterdir()) if p.is_file()}

media_before = hashes(original)
fixture_before = hashes(fixture)
assert 'SAVGAMG.DAT' in fixture_before
assert sum(name.endswith('.SAV') for name in fixture_before) == 6
subprocess.run([str(ROOT / 'scripts/dotnet.sh'), 'build',
                str(ROOT / 'src/Por.Desktop'), '-v:q'], cwd=ROOT, check=True)
game = server = None
with tempfile.TemporaryDirectory(prefix='por-wilderness-') as temporary:
    temp = pathlib.Path(temporary)
    state = temp / 'state'
    saves = state / 'saves'
    saves.mkdir(parents=True)
    for source in fixture.iterdir():
        if source.is_file():
            shutil.copy2(source, saves / source.name)
    # Seed only disposable copies, before native Load/G. Packed layouts are from
    # SaveRecordCodec (285 bytes) and Vault.ItemToBytes (63 bytes), not guessed.
    variant_changes = []
    def seed_file(name, edit):
        path = saves / name
        before = path.read_bytes(); after = bytearray(before)
        edit(after)
        changes = [dict(offset=i, before=a, after=b) for i,(a,b) in enumerate(zip(before,after)) if a != b]
        assert len(before) == len(after) and changes
        path.write_bytes(after)
        variant_changes.append(dict(file=name, before_sha256=hashlib.sha256(before).hexdigest(),
            after_sha256=hashlib.sha256(after).hexdigest(), changes=changes))
    if args.action in expanded.ACTIONS:
        expanded.seed(args.action, seed_file)
    elif args.action == 'ranged':
        def bow(data):
            assert len(data) % 63 == 0
            offset = next(i for i in range(0,len(data),63) if data[i+46] == 46)
            # Replace the incompatible crossbow with an unenchanted long bow.
            data[offset] = 0
            data[offset+46:offset+55] = bytes([43,43,0,0,0,0,0,0,0])
        seed_file('CHRDATG1.ITM', bow)
    elif args.action == 'heal':
        def injured_cleric(data):
            assert len(data) == 285 and data[50] > 10
            slot = next(i for i in range(23,44) if data[i] == 1)
            data[slot] = 3  # Replace one Bless with Cure Light Wounds.
            data[283] = data[50] - 10  # A real injury before native loading, not during combat.
        seed_file('CHRDATG2.SAV', injured_cleric)
    (out / 'fixture-variant.json').write_text(json.dumps(dict(action=args.action,
        synthetic_preload_variant=bool(variant_changes), changes=variant_changes), indent=2)+'\n')
    try:
        with (temp / 'display').open('w+') as display, (out / 'xvfb.log').open('w') as log:
            server = subprocess.Popen(['Xvfb', '-displayfd', str(display.fileno()),
                '-screen', '0', '1280x960x24', '-nolisten', 'tcp'],
                pass_fds=(display.fileno(),), stdout=log, stderr=log)
            deadline = time.monotonic() + 10
            while not (temp / 'display').read_text().strip():
                assert server.poll() is None and time.monotonic() < deadline
                time.sleep(.05)
        env = dict(os.environ, DISPLAY=':' + (temp / 'display').read_text().strip())
        with (out / 'run.log').open('w') as log:
            command = [str(ROOT / 'play-desktop.sh'), '--data', str(original),
                '--state', str(state), '--captures', str(caps), '--short-intro'] + (['--deterministic'] if args.combat else [])
            if args.action: command += ['--trace-combat-actions']
            if args.effects:
                command += ['--effects', str(args.effects.resolve())]
            game = subprocess.Popen(command, cwd=ROOT, env=env, stdout=log, stderr=subprocess.STDOUT)

            def text():
                return (out / 'run.log').read_text()

            def wait(condition, timeout=45):
                deadline = time.monotonic() + timeout
                while not condition():
                    assert game.poll() is None, text()
                    if time.monotonic() > deadline:
                        raise TimeoutError(text()[-2500:])
                    time.sleep(.05)

            wait(lambda: 'READY menu' in text())
            window = subprocess.check_output(['xdotool', 'search', '--pid', str(game.pid)],
                env=env, text=True).splitlines()[-1]

            def key(value, pause=.4, expect_alive=True):
                subprocess.run(['xdotool', 'key', '--window', window, value], env=env, check=True)
                time.sleep(pause)
                if expect_alive:
                    assert game.poll() is None, text()

            def capture(label):
                count = len(list(caps.glob('*.json')))
                key('F12', .2)
                wait(lambda: len(list(caps.glob('*.json'))) > count)
                path = sorted(caps.glob('*.json'))[-1]
                value = json.loads(path.read_text())
                (out / (label + '.json')).write_text(json.dumps(value, indent=2) + '\n')
                shutil.copy2(path.with_suffix('.png'), out / (label + '.png'))
                return value

            def exploring(value):
                stack = value['inputStack']
                return value['interfaceState'] == 3 and 'Explore.MapMainMenu' in stack and not any(
                    frame.startswith(('ViewChar.', 'Camp.', 'CompTool.', 'MapView.', 'PostCbt.'))
                    for frame in stack)

            def position(value):
                return [value[k] for k in ['locationType', 'wildernessX', 'wildernessY']]

            key('l')
            capture('load-menu')
            key('g', 1)
            wait(lambda: 'READY explore' in text())
            base = capture('loaded-upstream')
            assert exploring(base) and base['partySize'] == 6 and len(base['party']) == 6
            assert base['locationType'] == 3, 'Not central wilderness'
            assert all(not c['combatRecord'] for c in base['party'])
            if args.effects:
                assert base['audioMode'] == 'blaster'
            if args.item:
                # Unchanged fixture item84 is classMask8, so fighter Morrigan cannot
                # ready it; actual Trade hands it to Blair (fixture itemLimits13).
                key('v'); key('i'); capture('item-trade-inventory')
                index = next(i for i,it in enumerate(base['party'][0]['items']) if it['itemType']==84)
                for i in range(index): key('Down', .15)
                key('t'); capture('item-trade-recipient-menu')
                key('End'); key('End'); capture('item-trade-recipient')
                key('Return'); traded = capture('item-traded')
                assert not any(it['itemType']==84 for it in traded['party'][0]['items'])
                assert any(it['itemType']==84 for it in traded['party'][2]['items'])
                key('e'); key('e'); assert exploring(capture('item-trade-exploring'))
            steps = []
            encounter = first = outcome = None
            directions = [('Up', 0, -1), ('Right', 1, 0), ('Down', 0, 1), ('Left', -1, 0)]
            for step in range(160 if args.combat else 3):
                direction, dx, dy = directions[step % 4]
                before = base if not steps else steps[-1]
                key(direction, .8)
                value = capture('step-' + str(step + 1))
                if not exploring(value):
                    assert args.combat, 'Unexpected encounter in navigation replay'
                    encounter = value
                    break
                assert position(value) == [3, before['wildernessX'] + dx, before['wildernessY'] + dy]
                assert value['clock'] != before['clock'], 'Wilderness step did not advance time'
                steps.append(value)
            if args.combat:
                assert encounter is not None, 'No encounter within native movement budget'
                key('c', 1)
                first = capture('combat-entry')
                assert first['interfaceState'] == 5 and first['partySize'] == 6
                assert first['arenaCount'] > 6
                spell_before = spell_after = item_before = item_after = None
                if args.spell:
                    for turn in range(24):
                        value = capture('manual-turn-' + str(turn))
                        if value['activeCreature'] == 'RAVEN':
                            break
                        assert 'Combat.CombatMenu' in value['inputStack'], value['inputStack']
                        key('d'); key('q', .8)
                    else:
                        raise AssertionError('Raven did not receive manual turn')
                    spell_before = value
                    raven = next(c for c in value['party'] if c['name'] == 'RAVEN')
                    assert 15 in base64.b64decode(raven['memorizedSpells'])
                    key('c'); menu = capture('explicit-spell-menu')
                    assert 'Magic.SpellSelection' in menu['inputStack']
                    key('Return', .8)
                    aim = capture('explicit-spell-aim')
                    assert 'CbtActs.CombatAimMenu' in aim['inputStack'], aim['inputStack']
                    candidate = next(i for i,c in enumerate(aim['aimCandidates']) if c['hostile'] != 0)
                    for i in range(candidate): key('n', .3)
                    selected = capture('explicit-spell-target')
                    assert 'CbtActs.AimTargetMenu' in selected['inputStack']
                    key('t', 2)
                    spell_after = capture('explicit-spell-after')
                    raven_after = next(c for c in spell_after['party'] if c['name'] == 'RAVEN')
                    assert list(base64.b64decode(raven_after['memorizedSpells'])).count(15) == list(base64.b64decode(raven['memorizedSpells'])).count(15)-1
                    assert spell_after['round'] == spell_before['round']
                    assert spell_after['spellTargets'] == [aim['aimCandidates'][candidate]['name']]
                    assert [c['hp'] for c in spell_after['party'][:6]] == [c['hp'] for c in spell_before['party'][:6]], 'PC damaged instead of hostile'
                    assert sum(c['hp'] < old['hp'] for c,old in zip(spell_after['party'],spell_before['party']) if c['combat']['notPcNpc']==1) == 1, 'No unique hostile target damage'
                    assert 'Combat.CombatMenu' in spell_after['inputStack']
                if args.item:
                    for turn in range(24):
                        value = capture('item-manual-turn-' + str(turn))
                        if value['activeCreature'] == 'BLAIR':
                            break
                        assert 'Combat.CombatMenu' in value['inputStack'], value['inputStack']
                        key('d'); key('q', .8)
                    else:
                        raise AssertionError('Blair did not receive manual turn')
                    item_before = value
                    actor = next(c for c in value['party'] if c['name'] == 'BLAIR')
                    index = next(i for i,it in enumerate(actor['items'])
                        if it['itemType'] == 84 and base64.b64decode(it['variantData'])[1] == 82)
                    assert actor['items'][index]['equipped'] == 0
                    # Source ToggleReadyItem enforces two hands: release the carried shield,
                    # then return to the real CombatMenu before measuring potion use.
                    key('u'); inventory = capture('explicit-item-free-hand-menu')
                    assert 'ViewChar.UseItemMenu' in inventory['inputStack']
                    shield = next(i for i,it in enumerate(actor['items']) if it['itemType']==59 and it['equipped'])
                    for i in range(shield): key('Down', .15)
                    key('r'); capture('explicit-item-free-hand')
                    key('e'); item_before = capture('explicit-item-before')
                    assert 'Combat.CombatMenu' in item_before['inputStack']
                    actor = next(c for c in item_before['party'] if c['name']=='BLAIR')
                    assert actor['items'][shield]['equipped']==0
                    key('u'); inventory = capture('explicit-item-menu')
                    for i in range(index): key('Down', .15)
                    capture('explicit-item-selected')
                    key('r'); readied = capture('explicit-item-readied')
                    assert next(c for c in readied['party'] if c['name']=='BLAIR')['items'][index]['equipped'] == 1
                    key('u'); capture('explicit-item-announced')
                    time.sleep(1.5)
                    item_after = capture('explicit-item-after')
                    actor_after = next(c for c in item_after['party'] if c['name'] == 'BLAIR')
                    assert len(actor_after['items']) == len(actor['items']) - 1
                    assert not any(it['itemType']==84 for it in actor_after['items'])
                    assert actor_after['strCur'] == 21 and actor['strCur'] < 21
                    assert item_after['round'] == item_before['round']
                    assert 'Combat.CombatMenu' in item_after['inputStack']
                    assert [c['hp'] for c in item_after['party']] == [c['hp'] for c in item_before['party']]
                    assert any(e['id'] == 38 for e in actor_after['effects'])
                action_before = action_after = action_target = None
                action_extra = None
                if args.action in expanded.ACTIONS:
                    detail = expanded.perform(args.action,caps,out,key,capture,wait)
                    action_before,action_after,action_target,action_extra = (detail[k] for k in ['before','after','target','extra'])
                if args.action and args.action not in expanded.ACTIONS:
                    actor_name = 'MORRIGAN' if args.action == 'ranged' else 'GINS'
                    for turn in range(24):
                        value = capture('action-manual-turn-' + str(turn))
                        if value['activeCreature'] == actor_name:
                            break
                        assert 'Combat.CombatMenu' in value['inputStack'], value['inputStack']
                        key('d'); key('q', .8)
                    else:
                        raise AssertionError(actor_name + ' did not receive manual turn')
                    if args.action == 'ranged':
                        actor = next(c for c in value['party'] if c['name'] == actor_name)
                        # Free shield hand, then ready launcher and ammunition using real menus.
                        for item_type in [59,35,43,73]:
                            key('u'); capture('ranged-inventory-' + str(item_type))
                            index = next(i for i,it in enumerate(actor['items']) if it['itemType']==item_type)
                            for i in range(index): key('Down', .12)
                            key('r'); readied = capture('ranged-readied-' + str(item_type)); key('e')
                            actor = next(c for c in readied['party'] if c['name']==actor_name)
                        action_before = capture('action-before')
                        assert any(it['itemType']==43 and it['equipped'] for it in actor['items'])
                        key('a'); aim = capture('action-aim')
                    else:
                        action_before = capture('action-before')
                        key('c'); capture('action-spell-menu')
                        # The replaced first occupied slot is the first displayed spell.
                        key('Return', .8)
                        for boundary in range(60):
                            aim = capture('action-cast-boundary-' + str(boundary))
                            if 'CbtActs.CombatAimMenu' in aim['inputStack']: break
                            time.sleep(.2)
                        else: raise AssertionError('No spell targeting input boundary')
                        (out/'action-aim.json').write_text(json.dumps(aim, indent=2)+'\n')
                    assert 'CbtActs.CombatAimMenu' in aim['inputStack'], aim['inputStack']
                    if args.action == 'ranged':
                        candidate = next(i for i,c in enumerate(aim['aimCandidates']) if c['hostile'] != 0)
                    else:
                        candidate = next(i for i,c in enumerate(aim['aimCandidates']) if c['name']==actor_name)
                    for i in range(candidate): key('n', .3)
                    action_target = capture('action-target')
                    (out/'action-selection.json').write_text(json.dumps(dict(candidate_index=candidate, candidate=aim['aimCandidates'][candidate]), indent=2)+'\n')
                    old_traces = set(caps.glob('*-manual-*.json'))
                    key('t', 2)
                    def action_trace():
                        for path in sorted(set(caps.glob('*-manual-*.json')) - old_traces):
                            value = json.loads(path.read_text())
                            a = next(c for c in value['party'] if c['name']==actor_name)
                            old = next(c for c in action_before['party'] if c['name']==actor_name)
                            changed = a['items'] != old['items'] if args.action=='ranged' else a['memorizedSpells'] != old['memorizedSpells']
                            if changed: return path
                    wait(lambda: action_trace() is not None)
                    trace = action_trace()
                    action_after = json.loads(trace.read_text())
                    (out/'action-after.json').write_text(json.dumps(action_after, indent=2)+'\n')
                    shutil.copy2(trace.with_suffix('.png'),out/'action-after.png')
                    (out/'action-resolution.json').write_text(json.dumps(dict(capture=trace.name))+'\n')
                    assert 'Combat.ManualControl' in action_after['inputStack'], action_after['inputStack']
                    assert action_after['round'] == action_before['round']
                    old_actor = next(c for c in action_before['party'] if c['name']==actor_name)
                    new_actor = next(c for c in action_after['party'] if c['name']==actor_name)
                    if args.action == 'ranged':
                        assert next(it for it in new_actor['items'] if it['itemType']==73)['quantity'] < next(it for it in old_actor['items'] if it['itemType']==73)['quantity']
                        assert [c['hp'] for c in action_after['party'][:6]] == [c['hp'] for c in action_before['party'][:6]]
                    else:
                        spell_id = 3 if args.action == 'heal' else 1
                        assert list(base64.b64decode(new_actor['memorizedSpells'])).count(spell_id) == list(base64.b64decode(old_actor['memorizedSpells'])).count(spell_id)-1
                        if args.action == 'heal': assert new_actor['hp'] > old_actor['hp']
                        else: assert {c['name'] for c in action_after['party'] if any(e['id']==1 for e in c['effects'])} == {'MORGAN','RAVEN','SHADOW'}
                if args.action not in expanded.ACTIONS:
                    key('Escape')
                    key('Alt_L+q', .5)
                for action in range(400 if args.action in expanded.ACTIONS else 200):
                    value = capture('fight-' + str(action))
                    if value['interfaceState'] != 5:
                        break
                    if args.action in expanded.ACTIONS:
                        stack=value['inputStack']
                        if 'MenuCore.YesNoMenu' in stack and 'Combat.EndOfRound' in stack:key('n', .3)
                        elif 'Combat.CombatMenu' in stack:key('Alt_L+q', .3)
                        elif any(f.startswith('ViewChar.') for f in stack):key('e', .3)
                        elif any(f.startswith('Magic.SpellSelection') for f in stack):key('Escape', .3)
                        elif any('PauseOnly' in f for f in stack):key('Return', .3)
                        else:time.sleep(.3)
                    else:
                        key('Return', .3)
                        key('Alt_L+q', .3)
                else:
                    raise AssertionError('Combat exceeded action budget')
                outcome = value
                assert not value['partyDefeated'], 'Party lost wilderness combat'
                for boundary in range(30):
                    value = capture('cleanup-' + str(boundary))
                    if exploring(value):
                        break
                    stack = value['inputStack']
                    if any('GoBackForIt' in f for f in stack): key('n')
                    elif any('LootMenu' in f or 'LootTakeMenu' in f for f in stack): key('e')
                    elif any(f.startswith('ViewChar.') for f in stack): key('e')
                    else: key('Return')
                assert exploring(value), 'Not returned to wilderness exploration'
                assert all(not c['combatRecord'] for c in value['party'])
                assert any(c['experience'] > old['experience'] for c, old in zip(value['party'], base['party']))
            save = saves / 'savgamQ.dat'
            assert not save.exists()
            key('F5', .8)
            wait(save.exists)
            prompt = capture('save-confirmation')
            key('Return')
            saved = capture('saved')
            assert exploring(saved)
            written = hashes(saves)
            assert save.stat().st_size == 13137
            ready = text().count('READY explore')
            key('Up', .8)
            wait(lambda: text().count('READY explore') > ready)
            altered = capture('altered')
            assert exploring(altered) and position(altered) != position(saved)
            ready = text().count('READY explore')
            key('F8', 1)
            wait(lambda: text().count('READY explore') > ready)
            loaded = capture('loaded')
            assert exploring(loaded)
            assert loaded['party'] == saved['party']
            assert position(loaded) == position(saved) and loaded['facing'] == saved['facing']
            assert loaded['clock'] == saved['clock']
            # Check all six name rows, excluding borders and the heading.
            from PIL import Image
            image = Image.open(out / 'loaded.png').convert('RGB')
            name_pixels = [sum(image.getpixel((x, y)) in [(255, 255, 255), (85, 255, 255)]+([(255,85,85)] if loaded['party'][row]['enabled']==0 else [])
                for y in range(32 + row * 8, 40 + row * 8) for x in range(136, 232))
                for row in range(6)]
            assert all(n > 25 for n in name_pixels), name_pixels
            # Exercise another real write after reload; preserve every item/spell/effect sidecar.
            sidecars = {name: digest for name, digest in written.items()
                if name.upper().startswith('CHRDATQ') and pathlib.Path(name).suffix.lower() in ['.itm', '.mem', '.spc']}
            assert any(name.lower().endswith('.mem') for name in sidecars)
            assert any(name.lower().endswith('.spc') for name in sidecars)
            key('F5', .8)
            capture('resave-confirmation')
            key('Return')
            resaved = capture('resaved')
            assert exploring(resaved) and resaved['party'] == saved['party']
            assert all(hashes(saves).get(name) == digest for name, digest in sidecars.items())
            key('e')
            camp = capture('camp')
            assert camp['interfaceState'] == 2
            key('s', .8)
            capture('exit-save-dialog')
            key('Escape', .8)
            capture('exit-quit-prompt')
            key('y', .1, False)
            assert game.wait(timeout=10) == 0
            assert media_before == hashes(original) and fixture_before == hashes(fixture)
            result = dict(action_extra=action_extra if args.combat else None, action_outcome=('hit' if any(c['hp']<old['hp'] for c,old in zip(action_after['party'][6:],action_before['party'][6:])) else 'miss') if args.action=='ranged' else args.action, action=args.action, action_before=action_before if args.combat else None,
                action_after=action_after if args.combat else None, action_target=action_target if args.combat else None,
                fixture_variant=variant_changes, accepted=True, scenario='six-pc-wilderness-'+args.action if args.action else 'upstream-six-pc-wilderness-item' if args.item else 'upstream-six-pc-wilderness-spell' if args.spell else 'upstream-six-pc-wilderness-combat' if args.combat else 'upstream-six-pc-wilderness',
                explicit_item=bool(args.item), item_before=item_before if args.combat else None, item_after=item_after if args.combat else None,
                explicit_spell=bool(args.spell), spell_before=spell_before if args.combat else None, spell_after=spell_after if args.combat else None,
                encounter=encounter, first=first, outcome=outcome, encounter_step=len(steps)+1 if args.combat else None,
                deterministic_managed_seed=1 if args.combat else None,
                natural_campaign_progression=False, native_actions=True, exit_code=0,
                initial=base, steps=steps, saved=saved, altered=altered, loaded=loaded,
                save_created=True, save_sha256=written['savgamQ.dat'],
                loaded_name_pixels=name_pixels, item_spell_effect_sidecars=sidecars,
                original_media_unchanged=True,
                original_files_checked=len(media_before), upstream_fixture_unchanged=True,
                fixture_sha256=fixture_before,
                limits=('Documented disposable pre-load variant' if variant_changes else 'Unchanged upstream save')+'; explicit '+args.action+' action/native combat lifecycle; not natural campaign progression' if args.action else 'Unchanged upstream save; explicit Giant Strength potion consumption/effect and native wilderness combat lifecycle; ranged remains unvalidated' if args.item else 'Unchanged upstream save; explicit Magic Missile and native wilderness combat lifecycle; not exhaustive spell coverage or natural campaign completion' if args.spell else 'Unchanged upstream saved party; native wilderness combat lifecycle' if args.combat else 'Unchanged upstream saved party; navigation and serialization only; neither establishes natural campaign completion')
            (out / 'result.json').write_text(json.dumps(result, indent=2) + '\n')
            print('Six-character wilderness ' + ('combat lifecycle/' if args.combat else 'navigation/') + 'save-load/exit PASS', flush=True)
    finally:
        if game is not None and game.poll() is None:
            game.terminate()
            game.wait(timeout=10)
        if server is not None:
            server.terminate()
            server.wait(timeout=10)
        # Retain disposable output saves for byte-level review, never original media.
        shutil.copytree(saves, out / 'output-saves')

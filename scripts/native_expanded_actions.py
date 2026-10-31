"""Native controller helpers for six explicit fixture branches; no gameplay callbacks are replaced."""
import base64,heapq,json,shutil,time
ACTIONS=['ranged-hit','ranged-empty','heal-ally','heal-cap','bandage','turn-ineligible']

def seed(action,write):
    if action.startswith('ranged-'):
        def bow(data):
            offset=next(i for i in range(0,len(data),63) if data[i+46]==46)
            data[offset]=0;data[offset+46:offset+55]=bytes([43,43,0,0,5,0,0,0,0])
            arrow=next(i for i in range(0,len(data),63) if data[i+46]==73)
            if action=='ranged-empty':data[arrow+57]=2
            else:data[arrow+50]=5  # Explicit synthetic +5 bow/+5 ammunition fixture.
        write('CHRDATG1.ITM',bow)
    elif action.startswith('heal-'):
        def cure(data):
            slot=next(i for i in range(23,44) if data[i]==1);data[slot]=3
            if action=='heal-cap':data[283]=data[50]-2
        write('CHRDATG2.SAV',cure)
        if action=='heal-ally':
            def injured(data):data[283]=data[50]-10
            write('CHRDATG1.SAV',injured)
    elif action=='bandage':
        def dying(data):
            assert len(data)==285;data[268]=5;data[269]=0;data[283]=0
        write('CHRDATG6.SAV',dying)
    elif action!='turn-ineligible':raise ValueError(action)

def perform(action,caps,out,key,capture,wait):
    who='MORRIGAN' if action.startswith('ranged-') or action=='bandage' else 'GINS'
    def actor(s,name=who):return next(c for c in s['party'] if c['name']==name)
    def manual_actor(label):
        for turn in range(100):
            s=capture(label+'-'+str(turn))
            if 'Combat.CombatMenu' in s['inputStack']:
                if s['activeCreature']==who:return s
                key('d');key('q',.65)
            else:time.sleep(.3)
        raise AssertionError('No manual turn for '+who)
    before=manual_actor('expanded-turn')
    if action.startswith('ranged-'):
        for item_type in [59,35,43,73]:
            key('u');inventory=capture('expanded-ready-menu-'+str(item_type))
            index=next(i for i,it in enumerate(actor(inventory)['items']) if it['itemType']==item_type)
            for i in range(index):key('Down',.12)
            key('r');capture('expanded-readied-'+str(item_type));key('e')
        if action=='ranged-hit':
            # Use normal movement to establish short range before measuring the volley.
            start=capture('ranged-movement-start');loc=actor(start)['combat'];origin=(loc['x'],loc['y'])
            enemies=[c['combat'] for c in start['party'] if c['combat']['notPcNpc']==1]
            cost=start['combatMap']['costs'];occ=start['combatMap']['occupants']
            directions=[(0,-1,'Up'),(1,-1,'Prior'),(1,0,'Right'),(1,1,'Next'),(0,1,'Down'),(-1,1,'End'),(-1,0,'Left'),(-1,-1,'Home')]
            queue=[(0,origin,[])];seen=set();path=None
            while queue:
                spent,(x,y),route=heapq.heappop(queue)
                if (x,y) in seen:continue
                seen.add((x,y))
                distance=min(max(abs(x-e['x']),abs(y-e['y'])) for e in enemies)
                if 3<=distance<=5:path=route;break
                for dx,dy,k in directions:
                    nx,ny=x+dx,y+dy;newcost=spent+(3 if dx and dy else 2)
                    if newcost<=20 and 0<=nx<50 and 0<=ny<25 and 0<cost[ny][nx]<255 and occ[ny][nx]==0:
                        heapq.heappush(queue,(newcost,(nx,ny),route+[k]))
            assert path,'No short-range movement path within budget'
            (out/'ranged-movement-plan.json').write_text(json.dumps(path)+'\n')
            key('m')
            for i,k in enumerate(path):key(k,.3);capture('ranged-movement-'+str(i))
            key('Return');moved=capture('ranged-movement-finished')
            assert 'Combat.CombatMenu' in moved['inputStack'] and moved['activeCreature']==who
        before=capture('action-before');key('a');aim=capture('action-aim')
    elif action.startswith('heal-'):
        before=capture('action-before');key('c');capture('action-spell-menu');key('Return',.8)
        for boundary in range(60):
            aim=capture('expanded-cast-wait-'+str(boundary))
            if 'CbtActs.CombatAimMenu' in aim['inputStack']:break
            time.sleep(.2)
        else:raise AssertionError('No healing target boundary')
        (out/'action-aim.json').write_text(json.dumps(aim,indent=2)+'\n')
    else:
        before=capture('action-before');aim=None
    old_traces=set(caps.glob('*-manual-*.json'))
    target=None
    if aim is not None:
        recipient='MORRIGAN' if action=='heal-ally' else who
        idx=next(i for i,c in enumerate(aim['aimCandidates']) if (c['hostile']!=0 if action.startswith('ranged-') else c['name']==recipient))
        for i in range(idx):key('n',.3)
        target=capture('action-target')
        (out/'action-selection.json').write_text(json.dumps(dict(candidate_index=idx,candidate=aim['aimCandidates'][idx]),indent=2)+'\n')
        key('t',1)
    elif action=='bandage':
        key('d');capture('action-done-menu');key('b',1)
    else:key('t',1)
    label='manual-aim-resolved' if action.startswith('ranged-') else 'manual-cast-resolved' if action.startswith('heal-') else 'manual-bandage-resolved' if action=='bandage' else 'manual-turn-resolved'
    def trace():
        events=sorted(set(caps.glob('*-'+label+'.json'))-old_traces)
        return events[-1] if events else None
    wait(lambda:trace() is not None)
    path=trace();after=json.loads(path.read_text())
    (out/'action-after.json').write_text(json.dumps(after,indent=2)+'\n')
    shutil.copy2(path.with_suffix('.png'),out/'action-after.png')
    (out/'action-resolution.json').write_text(json.dumps(dict(capture=path.name))+'\n')
    assert before['round']==after['round']
    extra=dict(actor=who,resolution_capture=path.name)
    if action=='turn-ineligible':
        next_turn=manual_actor('turn-next-round')
        assert actor(next_turn)['combat']['usedTurnUndead']==1
        old=set(caps.glob('*-manual-turn-resolved.json'))
        key('t');rejected=capture('turn-second-rejected')
        assert rejected['activeCreature']==who and 'Combat.CombatMenu' in rejected['inputStack']
        assert old==set(caps.glob('*-manual-turn-resolved.json'))
        extra.update(next_turn=next_turn,rejected=rejected,no_second_turn_event=True)
    return dict(before=before,after=after,target=target,extra=extra)

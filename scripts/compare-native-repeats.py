#!/usr/bin/env python3
"""Compare native action replays, retaining raw differences in source-rendered item-name caches."""
import argparse,json,pathlib

def same_item_payload(a,b):
    if len(a)!=len(b) or len(a)%63:return False
    # Vault.ItemToBytes: Pascal String[40] is bytes0..40; payload41..62.
    # CharHlp.ItemLabel rewrites the name with/without a Yes/No readied label.
    return all(a[i]<=40 and b[i]<=40 and a[i+41:i+63]==b[i+41:i+63] for i in range(0,len(a),63))

def compare(primary,repeat):
    primary,repeat=pathlib.Path(primary),pathlib.Path(repeat)
    a=json.loads((primary/'result.json').read_text());b=json.loads((repeat/'result.json').read_text());assert a['action']==b['action']
    checks={field+'_party':a[field]['party']==b[field]['party'] for field in ['initial','first','action_before','action_after','outcome','saved','loaded']}
    checks.update({field:a[field]==b[field] for field in ['save_sha256','encounter_step','fixture_variant']})
    checks['rounds']=all(a[field]['round']==b[field]['round'] for field in ['first','action_before','action_after','outcome'])
    checks['loaded_world']=all(a['loaded'][field]==b['loaded'][field] for field in ['clock','locationType','wildernessX','wildernessY','facing'])
    checks['same_sidecar_set']=set(a['item_spell_effect_sidecars'])==set(b['item_spell_effect_sidecars'])
    cache=[]
    for file in a['item_spell_effect_sidecars']:
        x=(primary/'output-saves'/file).read_bytes();y=(repeat/'output-saves'/file).read_bytes()
        if x==y:continue
        assert file.lower().endswith('.itm') and same_item_payload(x,y),(a['action'],file,'noncache sidecar difference')
        cache.append(dict(file=file,classification='source-rendered Pascal item-name/readied-label cache ONLY',changed_offsets=[i for i,(u,v) in enumerate(zip(x,y)) if u!=v],primary_names=[x[i+1:i+1+x[i]].decode('cp437') for i in range(0,len(x),63)],repeat_names=[y[i+1:i+1+y[i]].decode('cp437') for i in range(0,len(y),63)]))
    checks['semantic_sidecars_equal']=True
    assert all(checks.values()),(a['action'],[k for k,v in checks.items() if not v])
    return dict(primary=str(primary),repeat=str(repeat),checks=checks,all_sidecar_bytes_equal=not cache,cached_label_differences=cache,save_sha256=b['save_sha256'])
if __name__=='__main__':
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--pair',nargs=3,action='append',required=True,metavar=('NAME','PRIMARY','REPEAT'));p.add_argument('--output',type=pathlib.Path,required=True);args=p.parse_args()
    # A changed quantity/bonus must never be hidden by cache classification.
    sample=bytearray(63);sample[0]=1;sample[1]=65;other=bytearray(sample);other[1]=66
    assert same_item_payload(sample,other)
    for offset in range(41,63):
        corrupt=bytearray(other);corrupt[offset]^=1;assert not same_item_payload(sample,corrupt)
    results={name:compare(first,second) for name,first,second in args.pair}
    args.output.write_text(json.dumps(results,indent=2)+'\n')
    print(str(len(results))+' repeats match action/outcome/party/world/main-save and item payloads; '+str(sum(not r['all_sidecar_bytes_equal'] for r in results.values()))+' retain documented name-cache-only differences')

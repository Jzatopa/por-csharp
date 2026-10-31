#!/usr/bin/env python3
"""Build a local SBSFX.DAT v2 from user-owned SSI recordings; no media is bundled.
Mappings/crop/gain follow pinned Talonius tools/sbsfx.py (7616920a).
Missing sources are fatal unless --allow-partial is explicitly supplied.
"""
import argparse,hashlib,json,pathlib,struct

# Source-defined tune -> SFXDQ.VOC type-1 block; tune9 uses Pools' pad2 crop.
DQ_TUNES={1:0,2:1,3:2,4:3,5:4,6:5,7:6,8:7,10:9,11:5}

def dax_blocks(data):
    if len(data)<2:raise ValueError('Short DAX header')
    header=struct.unpack_from('<H',data)[0];end=2+header
    if header%9 or end>len(data):raise ValueError('Invalid DAX directory')
    for at in range(2,end,9):
        ident,offset,size,packed=struct.unpack_from('<BiHH',data,at);start=end+offset
        if start<end or start+packed>len(data):raise ValueError('DAX payload outside file')
        src=memoryview(data)[start:start+packed];raw=bytearray();pos=0
        while pos<len(src) and len(raw)<size:
            control=src[pos];pos+=1
            if control<127:
                n=control+1
                if pos+n>len(src):raise ValueError('Truncated literal packet')
                raw.extend(src[pos:pos+n]);pos+=n
            elif control>128:
                if pos>=len(src):raise ValueError('Truncated repeat packet')
                raw.extend(bytes([src[pos]])*(256-control));pos+=1
        if len(raw)!=size:raise ValueError('DAX decoded length mismatch')
        yield ident,bytes(raw)

def pools_samples(data):
    result={}
    for ident,raw in dax_blocks(data):
        raw=bytes(x^128 for x in raw)
        if raw[:4]!=b'FORM' or raw[8:12]!=b'8SVX':continue
        end=8+struct.unpack_from('>I',raw,4)[0]
        if end>len(raw):raise ValueError('Truncated IFF FORM')
        name=body=None;pos=12
        while pos+8<=end:
            kind=raw[pos:pos+4];size=struct.unpack_from('>I',raw,pos+4)[0];start=pos+8
            if start+size>end:raise ValueError('Truncated IFF chunk')
            chunk=raw[start:start+size]
            if kind==b'NAME':name=chunk.rstrip(b'\0').decode('latin1')
            if kind==b'BODY':body=bytes(x^128 for x in chunk)
            pos=start+size+(size&1)
        if name and body is not None:result.setdefault(name,body)
    return result

def voc_blocks(data):
    if len(data)<26 or not data.startswith(b'Creative Voice File\x1a'):raise ValueError('Invalid VOC header')
    pos=struct.unpack_from('<H',data,20)[0];result=[]
    if pos<26 or pos>len(data):raise ValueError('Invalid VOC data offset')
    while pos<len(data) and data[pos]!=0:
        if pos+4>len(data):raise ValueError('Truncated VOC block header')
        kind=data[pos];size=int.from_bytes(data[pos+1:pos+4],'little');start=pos+4
        if start+size>len(data):raise ValueError('Truncated VOC block')
        if kind==1:
            if size<3:raise ValueError('Empty VOC sample')
            result.append((data[start],data[start+1],data[start+2:start+size]))
        pos=start+size
    return result

def first_step(sample):
    if len(sample)<297:raise ValueError('Pools pad2 too short for verified13-33ms crop')
    pcm=bytearray(round(128+(v-128)*.25) for v in sample[117:297])
    for k in range(18):
        pcm[k]=round(128+(pcm[k]-128)*k/18)
        i=len(pcm)-18+k;pcm[i]=round(128+(pcm[i]-128)*(18-k)/18)
    return 0,145,bytes(pcm)

def build(pools=None,dqk=None,allow_partial=False):
    entries={}
    if pools is not None:
        samples=pools_samples(pools)
        if 'pad2' not in samples:raise ValueError('No source-defined pad2 sample in Pools archive')
        entries[9]=first_step(samples['pad2'])
    if dqk is not None:
        blocks=voc_blocks(dqk)
        for tune,index in DQ_TUNES.items():
            if index>=len(blocks):raise ValueError('SFXDQ.VOC lacks mapped effect blocks')
            tc,fmt,payload=blocks[index]
            if fmt!=1 or len(payload)<2:raise ValueError('Mapped VOC effect is not Creative4-bit ADPCM with reference')
            entries[tune]=(1,tc,payload)
    missing=sorted(set(range(1,12))-entries.keys())
    if not entries or missing and not allow_partial:raise ValueError('Missing tune recordings: '+str(missing)+'; supply both sources or explicitly --allow-partial')
    payload=bytearray();offsets={};header=6+7*len(entries);table=bytearray(b'SBFX'+bytes([2,len(entries)]));metadata=[]
    for tune,(fmt,tc,sample) in sorted(entries.items()):
        key=(fmt,tc,sample)
        if key not in offsets:offsets[key]=header+len(payload);payload.extend(sample)
        table.extend(struct.pack('<BBBHH',tune,fmt,tc,len(sample),offsets[key]));metadata.append({'tune':tune,'format':fmt,'time_constant':tc,'bytes':len(sample),'sha256':hashlib.sha256(sample).hexdigest()})
    pack=bytes(table+payload)
    if len(pack)>0xFFF0:raise ValueError('Pack exceeds SndSb source allocation limit')
    return pack,{'complete_recorded_pack':not missing,'missing_tunes':missing,'entries':metadata,'sha256':hashlib.sha256(pack).hexdigest()}

def main():
    p=argparse.ArgumentParser(description=__doc__);p.add_argument('--pools',type=pathlib.Path);p.add_argument('--dqk',type=pathlib.Path);p.add_argument('--out',required=True,type=pathlib.Path);p.add_argument('--allow-partial',action='store_true');a=p.parse_args()
    pack,report=build(a.pools.read_bytes() if a.pools else None,a.dqk.read_bytes() if a.dqk else None,a.allow_partial)
    a.out.parent.mkdir(parents=True,exist_ok=True)
    if a.out.exists():p.error('Output exists; choose a fresh output (never overwrite source media)')
    a.out.write_bytes(pack);report['source_hashes']={key:hashlib.sha256(value.read_bytes()).hexdigest() for key,value in [('pools',a.pools),('dqk',a.dqk)] if value};a.out.with_suffix('.provenance.json').write_text(json.dumps(report,indent=2)+'\n');print(json.dumps(report,indent=2))
if __name__=='__main__':main()

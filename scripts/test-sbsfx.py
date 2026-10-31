#!/usr/bin/env python3
"""Media-free builder tests: literal/repeat DAX, IFF chunk framing, VOC blocks and pack layout."""
import importlib.util,pathlib,struct
path=pathlib.Path(__file__).with_name('build-sbsfx.py');spec=importlib.util.spec_from_file_location('builder',path);b=importlib.util.module_from_spec(spec);spec.loader.exec_module(b)
n=0
def check(test,name):
 global n;n+=1
 if not test:raise AssertionError(name)
def reject(call,name):
 try:call()
 except ValueError:check(True,name);return
 raise AssertionError(name)
def archive(raw):
 packets=b''.join(bytes([len(raw[k:k+126])-1])+raw[k:k+126] for k in range(0,len(raw),126));return struct.pack('<HBiHH',9,1,0,len(raw),len(packets))+packets
def chunk(kind,data):return kind+struct.pack('>I',len(data))+data+b'\0'*(len(data)&1)
def pool():
 data=b'8SVX'+chunk(b'NAME',b'pad2\0')+chunk(b'BODY',bytes(range(256))*2);form=b'FORM'+struct.pack('>I',len(data))+data;return archive(bytes(x^128 for x in form))
def voc():
 header=b'Creative Voice File\x1a'+struct.pack('<HHH',26,0x114,0x111F);parts=[]
 for i in range(13):parts.append(b'\1'+(5).to_bytes(3,'little')+bytes([156,1,128,i,0x70]))
 return header+b''.join(parts)+b'\0'
check(list(b.dax_blocks(archive(b'abc')))==[(1,b'abc')],'DAX literal')
check(list(b.dax_blocks(struct.pack('<HBiHH',9,1,0,5,2)+bytes([251,7])))==[(1,b'\7'*5)],'DAX repeat')
for bad in [b'',b'\x09\0',archive(b'abc')[:-1]]:reject(lambda:b.pools_samples(bad),'bad DAX rejected')
check(len(b.pools_samples(pool())['pad2'])==512,'IFF padded NAME and BODY')
check(len(b.voc_blocks(voc()))==13,'VOC block framing')
reject(lambda:b.voc_blocks(voc()[:-4]),'truncated VOC')
reject(lambda:b.build(pools=pool()),'missing source not disguised as complete')
pack,meta=b.build(pools=pool(),allow_partial=True);check(pack[:6]==b'SBFX\2\1' and pack[6]==9 and len(pack)==193,'explicit partial pack layout');check(meta['missing_tunes']==[1,2,3,4,5,6,7,8,10,11],'partial coverage')
pack,meta=b.build(pools=pool(),dqk=voc());check(meta['complete_recorded_pack'] and pack[5]==11,'complete fixture pack');entries={pack[i]:struct.unpack_from('<BBHH',pack,i+1) for i in range(6,83,7)};check(entries[6]==entries[11],'duplicate sample shares storage');check(entries[9][0]==0 and entries[1][0]==1,'PCM and ADPCM formats');check(pack[entries[1][3]:][:3]==bytes([128,0,0x70]),'ADPCM preserved without decode/reencode')
print(n,'recorded-effects builder checks PASS (synthetic fixtures, not recordings)')

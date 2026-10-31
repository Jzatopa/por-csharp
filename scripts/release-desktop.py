#!/usr/bin/env python3
"""Prepare a GitHub draft release plan. Network writes require explicit --execute and GH_TOKEN.

Attach new bytes under a temporary name before replacing an old asset. Never invoked by CI.
"""
import argparse,hashlib,json,os,pathlib,re,urllib.error,urllib.parse,urllib.request,uuid
p=argparse.ArgumentParser(description=__doc__);p.add_argument('--repository',required=True);p.add_argument('--tag',required=True);p.add_argument('--asset',type=pathlib.Path,required=True);p.add_argument('--execute',action='store_true');a=p.parse_args()
if not re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+',a.repository):p.error('Expected owner/repository')
raw=a.asset.read_bytes();digest=hashlib.sha256(raw).hexdigest();notes=f'Pool of Radiance managed desktop {a.tag}\n\nSHA-256: {digest}  {a.asset.name}\n\nOriginal media excluded. Not an SSI-identical DOS build or full-campaign certification.'
plan=dict(repository=a.repository,tag=a.tag,asset=a.asset.name,sha256=digest,draft=True,execute=a.execute)
if not a.execute:print(json.dumps(plan,indent=2));raise SystemExit(0)
token=os.environ.get('GH_TOKEN')
if not token:p.error('--execute requires GH_TOKEN')
base='https://api.github.com/repos/'+a.repository
headers={'Authorization':'Bearer '+token,'Accept':'application/vnd.github+json','X-GitHub-Api-Version':'2022-11-28','User-Agent':'por-csharp-release'}
def request(url,method='GET',body=None,binary=False):
 data=body if binary else (json.dumps(body).encode() if body is not None else None);h=dict(headers)
 if data is not None:h['Content-Type']='application/octet-stream' if binary else 'application/json'
 with urllib.request.urlopen(urllib.request.Request(url,data=data,headers=h,method=method),timeout=120) as r:
  payload=r.read();return json.loads(payload) if payload else None
try:release=request(base+'/releases/tags/'+urllib.parse.quote(a.tag,safe=''))
except urllib.error.HTTPError as error:
 if error.code!=404:raise
 release=request(base+'/releases','POST',dict(tag_name=a.tag,name='Pool of Radiance '+a.tag,body=notes,draft=True))
if not release.get('draft'):p.error('Refusing to mutate a published release; this helper manages drafts only')
old=[asset for asset in request(base+f"/releases/{release['id']}/assets") if asset['name']==a.asset.name]
name=a.asset.name+'.upload-'+uuid.uuid4().hex
url='https://uploads.github.com/repos/'+a.repository+f"/releases/{release['id']}/assets?name="+urllib.parse.quote(name,safe='')
new=request(url,'POST',raw,True)
assert new['size']==len(raw),'Upload size mismatch; prior asset retained'
for asset in old:request(base+f"/releases/assets/{asset['id']}",'DELETE')
request(base+f"/releases/assets/{new['id']}",'PATCH',dict(name=a.asset.name))
request(base+f"/releases/{release['id']}",'PATCH',dict(body=notes))
print(json.dumps(dict(plan,url=release['html_url'],uploaded=True),indent=2))

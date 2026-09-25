import json,shutil,re
from pathlib import Path
from PIL import Image
p=Path(__file__).resolve().parent
catalog=json.loads((p/'catalog.json').read_text(encoding='utf-8'));items=catalog['assets'];entries=[]
existing=json.loads((p/'manifest.json').read_text(encoding='utf-8')) if (p/'manifest.json').exists() else {'images':[]}
revisions={a['id']:a for a in existing['images'] if a.get('revision',1)>1}
for n,batch in [(1,items[:10]),(2,items[10:19]),(3,items[19:27]),(4,items[27:])]:
 log=p/f'batch_{n}.log'
 if not log.exists():continue
 calls={};text='';pending=None;index=0
 for line in log.read_text(encoding='utf-8',errors='replace').splitlines():
  try:r=json.loads(line)
  except:continue
  if r.get('type')=='text':
   text+=r.get('data','')
   matches=re.findall(r'ASSET_ID\s*:\s*([a-z_]+)',text)
   if matches and matches[-1] in [a['id'] for a in batch]:pending=matches[-1]
  if r.get('type')=='tool_call' and r.get('toolName')=='image_gen':
   asset=batch[min(index,len(batch)-1)] # Requests checked against catalog order; CLI text labels are not reliable.
   calls[r['toolCallId']]=(asset,r.get('rawInput',{}));index+=1
  out=r.get('rawOutput') or {}
  if out.get('type')=='ImageGen':
   asset,prompt=calls[r['toolCallId']];
   if asset['id'] in revisions:
    entries.append(revisions[asset['id']]);continue
   src=Path(out['path']);dest=p/'Originals'/asset['group']/(asset['id']+src.suffix.lower());dest.parent.mkdir(parents=True,exist_ok=True)
   shutil.copy2(src,dest)
   im=Image.open(dest);entries.append({'id':asset['id'],'group':asset['group'],'path':str(dest.relative_to(p)),'size':list(im.size),'source':str(src),'input':prompt,'batch':n})
(p/'manifest.json').write_text(json.dumps({'reference':catalog['reference'],'generator':'local GROK CLI / native image_gen','images':entries},ensure_ascii=False,indent=2),encoding='utf-8')
print('Archived',len(entries),'unique',len(set(a['id'] for a in entries)))
for n in range(1,5):print('batch',n,[a['id'] for a in entries if a['batch']==n])

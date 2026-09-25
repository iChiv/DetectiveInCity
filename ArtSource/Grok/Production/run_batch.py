import subprocess,shutil,sys
from pathlib import Path
p=Path(__file__).resolve().parent;n=sys.argv[1]
with (p/f'batch_{n}.log').open('w',encoding='utf-8') as log:
 r=subprocess.run([shutil.which('grok'),'--cwd',str(p),'--resume','01a0d3ad-05a1-7c10-b9c9-fed229b0bd9f','--prompt-file',str(p/f'batch_{n}.txt'),'--no-subagents','--disable-web-search','--allow','image_gen','--max-turns','24','--permission-mode','acceptEdits','--output-format','streaming-json'],stdout=log,stderr=subprocess.STDOUT)
print('BATCH',n,'EXIT',r.returncode)

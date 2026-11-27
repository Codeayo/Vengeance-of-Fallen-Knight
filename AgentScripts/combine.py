import sys,re
out=sys.argv[1]; files=sys.argv[2:]
usings=[];bodies=[]
pat=re.compile(r'^\s*(using\s+[\w\.]+(\s*=\s*[\w\.]+)?;\s*)+$')
for f in files:
    body=[]
    for l in open(f,encoding='utf-8').read().splitlines():
        if pat.match(l):
            for u in re.findall(r'using\s+[\w\.]+(?:\s*=\s*[\w\.]+)?;',l):
                if u not in usings: usings.append(u)
        else: body.append(l)
    bodies.append('\n'.join(body))
open(out,'w',encoding='utf-8').write('\n'.join(usings)+'\n'+'\n'.join(bodies))

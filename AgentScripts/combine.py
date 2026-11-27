import sys,re
out=sys.argv[1]; files=sys.argv[2:]
usings=[];bodies=[]
for f in files:
    lines=open(f,encoding='utf-8').read().splitlines()
    body=[]
    for l in lines:
        if re.match(r'^\s*using\s+[\w\.]+(\s*=\s*[\w\.]+)?;\s*$',l):
            if l.strip() not in usings: usings.append(l.strip())
        else: body.append(l)
    bodies.append('\n'.join(body))
open(out,'w',encoding='utf-8').write('\n'.join(usings)+'\n'+'\n'.join(bodies))

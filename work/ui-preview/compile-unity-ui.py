from pathlib import Path
import re, subprocess, sys
root=Path.cwd()
android='--android' in sys.argv
folder=root/('Logs/ProductionUI/CompileAndroid' if android else 'Logs/ProductionUI/Compile')
folder.mkdir(parents=True,exist_ok=True)
data=root/('Library/Bee/artifacts/1300b0aEDbg.dag' if android else 'Library/Bee/artifacts/1900b0aEDbg.dag')
exe=Path('C:/Program Files/Unity/Hub/Editor/6000.3.8f1/Editor/Data')
for assembly in ['Assembly-CSharp','Assembly-CSharp-Editor']:
    s=(data/(assembly+'.rsp')).read_text(encoding='utf-8-sig')
    for flag,suffix in [('out','.dll'),('refout','.ref.dll')]:
        s=re.sub(r'^-'+flag+r':.*$', '-'+flag+':"'+(folder/(assembly+suffix)).as_posix()+'"',s,flags=re.M)
    if assembly.endswith('Editor'):
        s=re.sub(r'^-r:"[^"]*/Assembly-CSharp.ref.dll"$', '-r:"'+(folder/'Assembly-CSharp.ref.dll').as_posix()+'"',s,flags=re.M)
    files=[p for p in (root/'Assets/Snowglow').rglob('*.cs') if ('Editor' in p.relative_to(root).parts)==assembly.endswith('Editor')]
    for p in files:
        name=p.relative_to(root).as_posix()
        if '"'+name+'"' not in s:s+='\n"'+name+'"\n'
    response=folder/(assembly+'.rsp');response.write_text(s,encoding='utf-8')
    result=subprocess.run([str(exe/'NetCoreRuntime/dotnet.exe'),str(exe/'DotNetSdkRoslyn/csc.dll'),'@'+str(response)],capture_output=True,text=True)
    (folder/(assembly+'.txt')).write_text(result.stdout+result.stderr,encoding='utf-8')
    print(assembly,result.returncode,(result.stdout+result.stderr)[-6000:])
    if result.returncode:raise SystemExit(result.returncode)

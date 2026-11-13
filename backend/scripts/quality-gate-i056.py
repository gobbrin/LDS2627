from pathlib import Path
import re, sys
p=Path(sys.argv[1] if len(sys.argv)>1 else 'artifacts/I056')
def load(name):
 f=p/name
 if not f.exists(): raise FileNotFoundError(str(f))
 b=f.read_bytes()
 return b.decode('utf-16') if b.startswith((b'\xff\xfe',b'\xfe\xff')) else b.decode('utf-8-sig','replace')
try:
 summary=load('resumo.txt'); build=load('dotnet-build.log'); restore=load('restore.log'); fmt=load('dotnet-format.log'); audit=load('audit.log'); sdk=load('sdk.txt')
 exits={name:int(re.search(r'(?m)^'+re.escape(name)+r'_exit_code=(\d+)\s*$', summary).group(1)) for name in ('restore','dotnet-build','dotnet-format','audit','test')}
 issues=[]
 for key,val in exits.items():
  if val!=0:issues.append(f'{key}: exit code {val}')
 if not sdk.strip().startswith('10.'):issues.append('SDK não é 10.x')
 if not re.search(r'(?im)^\s*Build succeeded\.',build):issues.append('Build succeeded não demonstrado')
 if re.search(r'(?im)^\s*[1-9]\d*\s+Error\(s\)',build):issues.append('Build com erros')
 if re.search(r'(?i)\berror\s+(?:CA|IDE|CS)\d+',build):issues.append('Diagnósticos de erro')
 for code in ('NU1903',):
  if any(re.search(r'\b'+code+r'\b',x) for x in (restore,build,audit)):issues.append(code+' presente')
 if not re.search(r'(?i)Format complete',fmt):issues.append('dotnet format sem conclusão comprovada')
 if re.search(r'(?im)\berror\s+(?:IMPORTS|WHITESPACE|IDE\d+)',fmt):issues.append('Formatação com diagnósticos')
 diags=sorted(set(re.findall(r'\b(?:CA|IDE)\d{3,5}\b',build)))
 if diags: issues.append('CA/IDE no build: '+', '.join(diags))
 if not re.search(r'(?i)(analyzers|analisadores)',fmt) and not re.search(r'(?i)(analyzers|analisadores)',build):issues.append('Execução de analisadores sem evidência no log')
 if re.search(r'(?i)\bNU1902\b',restore+build+audit):issues.append('NU1902 pendente de triagem explícita')
 print('SDK:',sdk.strip());print('Exit codes:',exits);print('Diagnósticos CA/IDE:',diags)
 print('QUALITY GATE:', 'FAIL' if issues else 'PASS')
 for i in issues:print(' - '+i)
 sys.exit(1 if issues else 0)
except (OSError,ValueError,AttributeError) as e:
 print('QUALITY GATE: INDETERMINADO — '+str(e));sys.exit(2)

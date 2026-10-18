"""Offline contract review against the user-supplied AADE v2 XSDs. No production startup."""
from pathlib import Path
import os, subprocess, shutil, json, base64
from decimal import Decimal
from lxml import etree

repo = Path(__file__).resolve().parents[2]
out = Path(__file__).parent / 'fixed-output'
out.mkdir(exist_ok=True)
binary = repo / 'tests/Invoicing.Mapping/bin/Release'
for source in binary.glob('*.dll'):
    shutil.copyfile(source, out / source.name)
vswhere = Path(os.environ['ProgramFiles(x86)'])/'Microsoft Visual Studio/Installer/vswhere.exe'
installation = subprocess.check_output([str(vswhere),'-latest','-products','*','-property','installationPath'],text=True).strip()
compiler = Path(installation)/'MSBuild/Current/Bin/Roslyn/csc.exe'
refs = ['System.dll','System.Core.dll','System.Xml.dll','System.Xml.Linq.dll','System.Web.Extensions.dll','System.Net.Http.dll']
refs += [str(p) for p in out.glob('*.dll')]
subprocess.run([str(compiler),'/nologo','/langversion:7.3','/target:exe','/main:ConformanceReview','/out:'+str(out/'Review.exe')]
               + ['/r:'+p for p in refs] + [str(Path(__file__).parent/'Cases.cs'),str(repo/'tests/Invoicing.Mapping/Program.cs')],check=True)
subprocess.run([str(out/'Review.exe')],cwd=out,check=True)
schema_path=repo/'Exedron.Invoicing.Adapters/Schemas/MyDataV2/InvoicesDoc-v2.0.0.xsd'
schema=etree.XMLSchema(etree.parse(str(schema_path)))
observations=[]
for path in sorted(out.glob('*.myDATA.xml')):
    valid=schema.validate(etree.parse(str(path)))
    errors=[str(e) for e in schema.error_log]
    observations.append({'file':path.name,'xsd_valid':valid,'errors':errors})
    print('XSD', 'PASS' if valid else 'FAIL',path.name,*errors,sep=' | ')
for path in sorted(out.glob('*.Ilyda.Direct.json')):
    data=json.loads(path.read_text(encoding='utf-8-sig'))
    doc=next(d for d in data['additionalSupportDocs'] if d['description']=='##InvoicesDoc##')
    xml=etree.fromstring(base64.b64decode(doc['reference']))
    valid=schema.validate(xml)
    errors=[str(e) for e in schema.error_log]
    observations.append({'file':path.name+' embedded XML','xsd_valid':valid,'errors':errors})
    print('XSD','PASS' if valid else 'FAIL',path.name+' embedded XML',*errors,sep=' | ')
(out/'xsd-results.json').write_text(json.dumps(observations,indent=2,ensure_ascii=False),encoding='utf-8')
print('XSD summary:',sum(v['xsd_valid'] for v in observations),'passed;',sum(not v['xsd_valid'] for v in observations),'failed. Business rules are reviewed separately.')
assert observations and all(v['xsd_valid'] for v in observations), 'An emitted document violates the supplied XSD'
for name in ['invalid-fuel-code','fuel-only-999','fuel-two-999-lines','fuel-999-exceeds-real-fuel','wrong-vat-arithmetic','excessive-series']:
    assert not list(out.glob(name+'.*.xml')) and not list(out.glob(name+'.iTimologia.json')) and not list(out.glob(name+'.Ilyda.Direct.json')), name+' was not blocked'
business=[]
ns={'i':'http://www.aade.gr/myDATA/invoice/v1.0'}
for path in sorted(out.glob('fuel-*.myDATA.xml')):
    xml=etree.parse(str(path))
    rows=xml.xpath('//i:invoiceDetails',namespaces=ns)
    values=[(r.findtext('i:fuelCode',namespaces=ns),Decimal(r.findtext('i:netValue',namespaces=ns))) for r in rows]
    other=[amount for code,amount in values if code=='999']
    real=[amount for code,amount in values if code not in (None,'999')]
    errors=[]
    if not real: errors.append(268)
    if any(amount>sum(real) for amount in other): errors.append(269)
    if len(other)>1: errors.append(270)
    business.append({'file':path.name,'generated_despite_aade_errors':errors,'fuel_lines':[(c,str(v)) for c,v in values]})
    print('BUSINESS RULE OBSERVATION',path.name,errors)
    assert not errors, (path.name,errors)
for path in sorted(out.glob('*.Ilyda.Direct.json')):
    data=json.loads(path.read_text(encoding='utf-8-sig'))
    # The supplied Ilyda test_b2b.json explicitly demonstrates this alternative
    # to native aadeData. Its absence is an observation, NOT a defect.
    business.append({'file':path.name,'has_native_aadeData':'aadeData' in data,
                     'uses_vendor_example_xml_attachment':any(x.get('description')=='##InvoicesDoc##' for x in data.get('additionalSupportDocs',[]))})
for name,expected in [('eu-zero-vat','K'),('non-eu-zero-vat','G')]:
    data=json.loads((out/(name+'.Ilyda.Direct.json')).read_text(encoding='utf-8-sig'))
    assert data['invoiceLines'][0]['lineVatInfo']['vatCategoryCode'] == expected
    assert data['vatBreakdowns'][0]['categoryCode'] == expected
    business.append({'file':name+'.Ilyda.Direct.json','expected_vat_category':expected,
                     'actual_line_vat_category':data['invoiceLines'][0]['lineVatInfo']['vatCategoryCode'],
                     'actual_breakdown_vat_category':data['vatBreakdowns'][0]['categoryCode']})
(out/'business-results.json').write_text(json.dumps(business,indent=2,ensure_ascii=False),encoding='utf-8')

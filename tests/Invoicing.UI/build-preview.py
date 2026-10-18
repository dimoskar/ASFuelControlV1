"""Compile real Designer and Step 9 UI code with inert host stubs. No production startup/DB."""
from pathlib import Path
import os, re, shutil, subprocess

repo = Path(__file__).resolve().parents[2]
out = Path(__file__).parent / 'output'
out.mkdir(exist_ok=True)
binary = Path(os.environ.get('ASFC_PREVIEW_BINARY', str(repo / 'ASFuelControl.Windows/bin/Release')))
forms = ['SettingForms/MainSettingsForm', 'SettingForms/PrinterSettings', 'Forms/InvoiceTypesForm', 'Forms/InvoiceFormEx']
sources = []
resources = set()
for form in forms:
    name = form.split('/')[-1]
    src = repo / ('ASFuelControl.Windows/UI/' + form + '.Designer.cs')
    code = src.read_text(encoding='utf-8-sig')
    namespace = re.search(r'namespace\s+([\w.]+)', code).group(1)
    resources.update(re.findall(r'global::ASFuelControl.Windows.Properties.Resources\.(\w+)', code))
    code = '\n'.join(line for line in code.splitlines() if not any(token in line for token in
        [' += ', '.DataBindings.Add(', '.DataSource = ', '.DataMember = ', '.Source = typeof(']))
    code = code.replace('System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof('+name+'));',
        'System.Resources.ResourceManager resources = Preview.Resources("'+namespace+'.'+name+'");')
    target = out / (name + '.Designer.cs')
    target.write_text(code, encoding='utf-8-sig')
    sources.append(target)
    sources.append(repo / ('ASFuelControl.Windows/UI/' + form + '.Invoicing.cs'))

resource_source = 'namespace ASFuelControl.Windows.Properties { internal static class Resources {\n' + '\n'.join(
    'public static System.Drawing.Bitmap '+name+' { get { return (System.Drawing.Bitmap)Preview.Resources("ASFuelControl.Windows.Properties.Resources").GetObject("'+name+'"); } }'
    for name in sorted(resources)) + '\n} }'
(out/'Resources.cs').write_text(resource_source, encoding='utf-8-sig')
sources += [out/'Resources.cs',Path(__file__).parent/'Preview.cs',
    repo/'ASFuelControl.Windows/UI/Controls/InvoicingFields.cs',repo/'ASFuelControl.Windows/UI/Controls/FiscalDocumentPanel.cs',
    repo/'ASFuelControl.Windows/UI/Controls/DoubleBufferTableLayout.cs',repo/'ASFuelControl.Windows/Threads/SharedInvoiceConfiguration.cs']
dlls = ['Telerik.WinControls.dll','Telerik.WinControls.GridView.dll','Telerik.WinControls.UI.dll','TelerikCommon.dll','TelerikData.dll',
    'Telerik.WinControls.Themes.TelerikMetroBlue.dll','ASFuelControl.Data.dll','ASFuelControl.Common.dll','Telerik.OpenAccess.dll',
    'Telerik.OpenAccess.35.Extensions.dll','Newtonsoft.Json.dll','ASFuelControl.Invoicing.dll','ASFuelControl.Invoicing.Mapping.dll',
    'ASFuelControl.Invoicing.Persistence.dll','Exedron.Invoicing.Core.dll','Exedron.Invoicing.Adapters.dll']
for dll in dlls:
    shutil.copyfile(binary/dll,out/dll)
vswhere = Path(os.environ['ProgramFiles(x86)'])/'Microsoft Visual Studio/Installer/vswhere.exe'
installation = subprocess.check_output([str(vswhere),'-latest','-products','*','-property','installationPath'],text=True).strip()
compiler = Path(installation)/'MSBuild/Current/Bin/Roslyn/csc.exe'
args = [str(compiler),'/nologo','/langversion:7.3','/target:exe','/platform:x86','/out:'+str(out/'Preview.exe')]
args += ['/r:'+name for name in ['System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Data.dll','System.Xml.dll','System.Net.Http.dll']]
args += ['/r:'+str(out/name) for name in dlls]
subprocess.run(args + [str(source) for source in sources],check=True)
subprocess.run([str(out/'Preview.exe'),str(binary/'ASFuelControl.Windows.exe')],check=True,cwd=out)

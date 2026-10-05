const vscode = require('vscode');
const fs = require('node:fs');
const path = require('node:path');
const { promisify } = require('node:util');
const execFile = promisify(require('node:child_process').execFile);

exports.run = async function () {
    const directory = vscode.workspace.workspaceFolders[0].uri.fsPath;
    const evidence = [];
    const output = path.join(directory, 'evidence.json');
    const record = (stage, value) => {
        evidence.push({ stage, time: new Date().toISOString(), ...value });
        fs.writeFileSync(output, JSON.stringify(evidence, null, 2));
    };
    record('environment', {
        vscode: vscode.version,
        extensions: vscode.extensions.all.filter(e => /dotnettools/.test(e.id)).map(e => ({ id: e.id, version: e.packageJSON.version }))
    });
    const document = await vscode.workspace.openTextDocument(path.join(directory, 'Program.cs'));
    await vscode.window.showTextDocument(document);
    const lines = document.getText().split('\n');
    const line = lines.findIndex(l => l.includes('Texts.Welcome'));
    const position = new vscode.Position(line, lines[line].indexOf('Texts.Welcome') + 6);
    const read = async () => {
        const completion = await vscode.commands.executeCommand('vscode.executeCompletionItemProvider', document.uri, position, '.', 100);
        const formatLine = lines.findIndex(l => l.includes('Texts.FormatWelcome'));
        const hover = await vscode.commands.executeCommand('vscode.executeHoverProvider', document.uri, new vscode.Position(formatLine, lines[formatLine].indexOf('FormatWelcome') + 2));
        const items = (completion?.items || []).filter(i => /Welcome|Added|Renamed/.test(typeof i.label === 'string' ? i.label : i.label.label));
        return {
            hover: (hover || []).flatMap(h => h.contents.map(c => c.value || c)),
            completion: items.map(i => ({ label: i.label, kind: i.kind, detail: i.detail, documentation: i.documentation?.value || i.documentation })),
            diagnostics: vscode.languages.getDiagnostics(document.uri).map(d => ({ code: typeof d.code === 'object' ? d.code.value : d.code, message: d.message, severity: d.severity }))
        };
    };
    const wait = async (stage, predicate) => {
        const deadline = Date.now() + 90000;
        let result;
        while (Date.now() < deadline) {
            result = await Promise.race([read(), new Promise(resolve => setTimeout(() => resolve({completion: [], diagnostics: [], timeout: true}), 10000))]);
            if (predicate(result)) { record(stage, { passed: true, ...result }); return true; }
            await new Promise(r => setTimeout(r, 1500));
        }
        record(stage, { passed: false, ...result });
        return false;
    };
    try {
        if (!await wait('initial', r => r.hover?.some(h => h.includes('FormatWelcome')) || r.completion.some(i => i.kind === vscode.CompletionItemKind.Method && JSON.stringify(i).includes('FormatWelcome')))) throw new Error('Initial generated API unavailable');
        fs.writeFileSync(path.join(directory, 'Resources/Texts.resx'), fs.readFileSync(path.join(directory, 'Resources/Texts.resx'), 'utf8').replace('{name@string}', '{person@string?}'));
        const detectsMismatch = r => r.diagnostics.some(d => (d.code === 'TRESX004' || d.message.includes('TRESX004')) && d.message.includes('person'));
        if (!await wait('reference-contract', detectsMismatch)) {
            if (process.env.TALBY_IDE_DESIGN_TIME_CONTROL === '1') {
                // A diagnostic control only: explicitly fails acceptance before forcing a design-time build.
                // This cannot count as automatic IDE refresh or substitute for human evidence.
                const control = await execFile('dotnet', ['msbuild', path.join(directory, 'Consumer.csproj'), '-t:Compile', '-p:DesignTimeBuild=true', '-p:BuildingProject=false', '-p:SkipCompilerExecution=true', '-p:ProvideCommandLineArgs=true', '-p:BuildProjectReferences=false', '-v:quiet'], { windowsHide: true, timeout: 120000 });
                record('manual-design-time-command', { output: control.stdout });
                await wait('manual-design-time-control', detectsMismatch);
            }
            throw new Error('Automatic reference refresh failed; subsequent acceptance scenarios were not run');
        }
        fs.writeFileSync(path.join(directory, 'Resources/Texts.es.resx'), fs.readFileSync(path.join(directory, 'Resources/Texts.es.resx'), 'utf8').replace('{name}', '{person}'));
        if (!await wait('localized-corrected', r => !r.timeout && !r.diagnostics.some(d => d.code === 'TRESX004' || d.message.includes('TRESX004')))) throw new Error('Automatic localized correction refresh failed');
    } finally {
        record('finished', {});
    }
};

// Verificação suplementar em DOM simulado; não substitui Chrome/Karma nem mede cobertura.
const { createRequire } = require('node:module');
const fs = require('node:fs');
const path = require('node:path');
const root = process.cwd();
const local = createRequire(path.join(root, 'package.json'));
const { JSDOM } = local('jsdom');
const dom = new JSDOM('<!doctype html><html><head><base href="/"></head><body></body></html>', {
  url: 'http://localhost:9876/',
  pretendToBeVisual: true,
});
for (const key of [
  'window',
  'Storage',
  'document',
  'Node',
  'HTMLElement',
  'Element',
  'Event',
  'MouseEvent',
  'KeyboardEvent',
  'ProgressEvent',
  'Document',
  'DocumentFragment',
  'Text',
  'Comment',
  'HTMLInputElement',
  'HTMLButtonElement',
  'HTMLAnchorElement',
  'HTMLSelectElement',
  'HTMLTextAreaElement',
  'MutationObserver',
  'sessionStorage',
  'localStorage',
  'getComputedStyle',
])
  if (dom.window[key]) global[key] = dom.window[key];
global.requestAnimationFrame = dom.window.requestAnimationFrame.bind(dom.window);
global.cancelAnimationFrame = dom.window.cancelAnimationFrame.bind(dom.window);
const originalFetch = global.fetch;
dom.window.fetch = originalFetch;
global.fetch = (...args) => dom.window.fetch(...args);
const jasmineCore = local('jasmine-core');
const j = jasmineCore.core(jasmineCore);
const env = j.getEnv();
Object.assign(global, jasmineCore.interface(j, env));
env.configure({ random: true, exitOnCompletion: false });
let total = 0,
  failed = 0;
env.addReporter({
  specDone: (r) => {
    total++;
    if (r.status === 'failed') {
      failed++;
      console.log('FAIL', r.fullName, ...r.failedExpectations.map((x) => x.message));
    }
  },
  jasmineDone: (r) => {
    console.log(
      'RESULT',
      JSON.stringify({ status: r.overallStatus, total, failed, errors: r.failedExpectations }),
    );
    process.exit(failed || r.overallStatus !== 'passed' ? 1 : 0);
  },
});
(async () => {
  await import(local.resolve('@angular/compiler'));
  const out = path.join(root, 'node_modules/.i069-verificacao/tests.mjs');
  fs.mkdirSync(path.dirname(out), { recursive: true });
  await local('esbuild').build({
    stdin: {
      contents:
        "import '@angular/compiler';import {getTestBed} from '@angular/core/testing';import {BrowserTestingModule,platformBrowserTesting} from '@angular/platform-browser/testing';getTestBed().initTestEnvironment(BrowserTestingModule,platformBrowserTesting());await import('./node_modules/.i069-verificacao/aot/core/core.spec.js');await import('./node_modules/.i069-verificacao/aot/app.spec.js');",
      resolveDir: root,
      sourcefile: 'i069-node.ts',
    },
    bundle: true,
    format: 'esm',
    platform: 'node',
    target: 'es2022',
    packages: 'external',
    outfile: out,
    tsconfig: path.join(root, 'tsconfig.json'),
  });
  await import(out);
  await env.execute();
})();

# I087 — automação web preparada

O ficheiro `src/app/features/testes-sistema-m4.spec.ts` acrescenta oito testes de componentes Angular com o contrato HTTP simulado. Verifica filtros/paginação inicial, falha de pesquisa, bloqueio da aceitação sem simulação, preservação da mensagem após erro, renderização literal de conteúdo, leitura de notificação e validação/publicação de avaliações.

```sh
npm ci
npm run format:check
npm run lint
npm run test:dom
npm test
npm run build
```

`test:dom` é uma verificação local suplementar sem browser. `npm test` executa Karma/Chrome com cobertura; ambos usam a mesma base de testes, mas o primeiro não demonstra cobertura nem compatibilidade com browsers reais.

## Relação com a especificação

Os testes novos verificam apenas partes de ST-WEB-F003-02/04, F004-09/11, F005-01/02, F006-04, F007-01/02 e TR-03. Não constituem a implementação de todos os 105 cenários da I086, nem uma suite Selenium com a API real.

Para concluir a I087: implementar as fixtures AUX-01–13 no ambiente isolado de teste, os restantes fluxos Selenium, dados únicos e limpeza de estado, esperas explícitas, a matriz de browsers e a recolha de evidências prevista no plano I085. Não criar operações administrativas de teste no backend público. Registar resultados reais na I097; os critérios condicionados por I112 mantêm a limitação até correção.

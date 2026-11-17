# Documentação do backend

Índice de `docs/backend/`. O índice geral está em [`docs/README.md`](../README.md). Os documentos nomeados `Ixxx` correspondem a itens de trabalho (issues). A documentação de projeto (proposta, âmbito, requisitos, modelo de dados, casos de uso, regulamento) vive nas pastas de documentação do grupo, fora deste repositório.

| Pasta | Conteúdo |
|---|---|
| [arquitetura/](arquitetura/) | [Guia do código do backend](arquitetura/GUIA-DO-CODIGO.md): camadas, fluxo de um pedido, onde está cada funcionalidade e armadilhas. |
| [entregas/](entregas/) | Documentos de entrega por item: I029, I034–I037, I041, I044. |
| [testes/](testes/) | [Plano de testes do backend, m4 v01](testes/m4-plano-testes-backend-v01.md) (I112); plano de m3 (I031, `.docx`); especificações, evidências e relatórios de testes (I050, I053, I054, I059) e coleções Postman: a [em vigor](testes/postman/CampusCarpool.postman_collection.json) (I112, todos os endpoints com a resposta esperada; ver [LEIA-ME](testes/LEIA-ME.md)) e as de m3 (I055, I034–I037). |
| [operacao/](operacao/) | Pipeline CI/CD (I058) e manual de instalação do backend (I064). |
| [qualidade/](qualidade/) | Análise estática (I056), plano de qualidade e DoR de m3. Revisões, auditorias e dívida técnica são locais, em `docs/qualidade/` (`.gitignore`). |
| [IMPLEMENTACAO-STATUS.md](IMPLEMENTACAO-STATUS.md) | Estado da implementação: m3, atualizado com a I112 (16/11/2026). Em caso de dúvida, vale o código. |

## Convenções

- Português europeu em nomes, textos e commits.
- Documentos novos entram na pasta temática correspondente; não criar ficheiros soltos na raiz de `docs/backend/`.
- Ficheiros `.pdf` são exportações dos `.md` homónimos; a fonte é o `.md`.

## Serviço de Mapas: HERE e OpenRouteService (I112, D-5)

A HERE é o fornecedor principal de tudo o que é mapa; o OpenRouteService é a reserva. Pormenores no [guia do código, §7](arquitetura/GUIA-DO-CODIGO.md#7-rotas-e-serviço-de-mapas).

| Chave | Para quê | Sem ela |
|---|---|---|
| `Here:ApiKey` | Geocodificação (Geocoding & Search), rota direta com a linha (Routing v8), matriz de tempos e distâncias (Matrix Routing v8) e linha com as paragens dos passageiros (Routing v8 com `via`) | Tudo vai ao OpenRouteService e o mapa recebe retas entre os pontos (`geometriaAproximada: true`) |
| `Here:TimeoutSeconds` | Tempo de cada pedido à HERE (5 por omissão), mais curto que o limite de 10 s da operação, para sobrar tempo para a reserva | — |
| `Maps:ApiKey` | OpenRouteService, a reserva | Sem a reserva: uma falha da HERE dá 502 ou 504; sem nenhuma das duas chaves, as operações com mapas dão 503 |
| `Maps:TimeoutSeconds` | Tempo de cada pedido ao OpenRouteService (10 por omissão) | — |

**Ordem em cada operação** (`ServicoMapasComReserva`): (1) HERE; (2) se a HERE falhar (erro HTTP, incluindo 401, 403, 429 e 5xx, resposta inválida ou sem resposta em 5 s) e ainda houver tempo dentro dos 10 s, OpenRouteService, com um aviso no log; (3) se as duas falharem, 502 ou 504, como antes. A linha do mapa tem uma reserva própria: sem HERE, retas entre os pontos. As chaves nunca vão para o repositório: User Secrets em desenvolvimento ou variáveis `Here__ApiKey` e `Maps__ApiKey` no servidor. Ferramentas, limites, preços e termos da HERE: `docs/.local-context/9-m4/16-here-ferramentas.md` (local).

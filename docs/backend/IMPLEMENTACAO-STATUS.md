# Estado da implementação do backend

**Referência:** backend entregue em m3 (`04-artefactos-tecnicos/04.12-backend/m3-v01`), commit `0205ac1`, versão v0.2.0 (notas de release, `04.19`). Situação a 06/11/2026, verificada no código e na execução da pipeline, **atualizada a 16/11/2026 com a I112** (correção e melhoria do backend, branch `fix/i112-corrigir-melhorar-backend`, ainda por integrar na `develop`).

Este documento resume o que está implementado. O estado de cada requisito, com o teste que o verifica e os defeitos conhecidos (`DEF-BE-nnn`), está na RTM v02 (`04.03-requisitos-e-rtm/m3-requirements-traceability-matrix-v02.xlsx`) e no registo de defeitos (`03.01.07-defeitos`).

## 1. Funcionalidades

| Funcionalidade | Implementado | Em falta ou com defeito |
| --- | --- | --- |
| F001 — Registo, autenticação e perfil | Registo com domínios académicos configuráveis, confirmação por email (Brevo) e reenvio, início e fim de sessão com JWT de 60 min e revogação do token, perfil, ligações externas, perfil público com classificação média | — (DEF-BE-045 e DEF-BE-046 corrigidos na I112: o reenvio valida o formato do email; registo e reenvio respondem sem esperar pelo email) |
| F002 — Criação e gestão de viagens | Criar, consultar, alterar (incluindo origem e destino), indisponibilizar, concluir de forma manual e automática, viagens ativas do condutor | — |
| F003 — Pesquisa e consulta | Pesquisa paginada sem distinção de acentos, com filtros na hora de Portugal; detalhes com condutor, pontos e estimativa de custo; passageiros de uma viagem | — |
| F004 — Pedido e gestão de participação | Pedido, aceitar, rejeitar e cancelar com controlo dos lugares, simulação, pedidos pendentes de uma viagem, atualização em tempo real (SignalR) | Simulação sem coordenadas (DEF-BE-042). DEF-BE-043 corrigido na I112: sem paragens novas, a rota guardada é reutilizada sem chamar os mapas |
| F005 — Comunicação | Avisos do condutor (até 100 caracteres, só em viagens Ativas), mensagens, conversa paginada por cursor (`antesDe`, `tamanho`) e lista de conversas | — (`Mensagens.ViagemId` e `Avisos.CondutorId` retirados na I112, DEF-BE-050/051) |
| F006 — Notificações | Notificações paginadas, marcar como lida uma ou todas, push Android (FCM) com `tipo`, `notificacaoId` e `viagemId` no campo `data`, desativação do dispositivo no fim de sessão, que também fecha as ligações ao hub | Aviso visível depois de cancelar (DEF-BE-049); nova mensagem sem remetente (DEF-BE-053) |
| F007 — Avaliações | Avaliar, alterar a avaliação, ler a própria avaliação (`/minha`), consultar a média e as avaliações recebidas | — (DEF-BE-041 corrigido na I112). Avaliações recebidas sem paginação |
| F008 — Mapas, rotas e custo | Geocodificação, rota guardada e recalculada só quando os pontos mudam, estimativa e simulação do custo (OpenRouteService); linha do mapa (`geometria`) pelo HERE Routing v8, guardada com a rota em `Viagem.RotaPontos`, com retas como reserva (`geometriaAproximada`); limite de 10 s por operação | — (DEF-BE-054 corrigido na I112) |
| F009 — Histórico | Histórico de viagens em qualquer estado, pedidos com os pontos propostos, avisos e notificações | Histórico sem paginação (AP-10). DEF-BE-024 corrigido na I112 |

Transversal: limites de 100 pedidos/min por utilizador ou IP e 10/min por IP na autenticação (atrás de um proxy configurado em `ProxyReverso:*`, por IP real do cliente), política global que exige sessão, CORS configurável, todos os erros só com `{ erro }` (exceto o 401 sem sessão, sem corpo), campos JSON desconhecidos recusados com 400, `/health` com verificação da base de dados, OpenAPI só em Development.

## 2. Base de dados

16 migrations EF Core em `src/CampusCarpool.Infrastructure/Migrations`, de `InitialCreate` a `I112_IndiceConversaECamposSemUso` (16/11: índice `IX_Mensagens_RemetenteId_DestinatarioId_EnviadoEm`, sem `Mensagens.ViagemId` nem `Avisos.CondutorId`). As 15 de m3 estão também como scripts SQL em `04.13-scripts-de-base-de-dados/m3-v01`. Não são aplicadas no arranque. As diferenças face ao modelo de dados v07 estão na secção 15 desse documento.

## 3. Testes e qualidade

| Medida | Valor (06/11/2026, commit `0205ac1`, run 42202915885) |
| --- | --- |
| Testes em SQLite, com os específicos de SQL Server | 674 execuções: 674 aprovadas, 0 falhadas, 0 saltadas (260 unitárias e 414 de integração) |
| Mesma suite em SQL Server real | 673 aprovadas, 0 falhadas, 1 saltada de propósito (`I110_15`, só faz sentido em SQLite) |
| Cobertura de linhas, sem migrations | 95,73 % (3052/3188); ramos 87,71 % |
| Pipeline GitHub Actions | Verde em todos os passos; bloqueia por erro de compilação, análise estática, pacote vulnerável, teste falhado e cobertura inferior a 80 % |
| Pacotes NuGet vulneráveis | Nenhum |
| I112, local (16/11/2026, commit `28b6579`, SQLite) | 754 aprovados, 0 falhados, 6 saltados (os de SQL Server); compilação sem avisos. Falta a execução na CI, com SQL Server real |

## 4. Limitações

- O registo depende do email (Brevo) para a confirmação: sem ele, a conta fica criada por confirmar e o registo responde 202 na mesma (o envio é em segundo plano). O manual explica como confirmar uma conta de teste.
- A API publicada no Azure é anterior à I110 e não corresponde a esta versão.
- As filas de push e de email são em memória (decisão G17): um reinício perde o que estiver por enviar.
- A `main` do repositório ainda não está protegida: uma execução vermelha da pipeline não impede tecnicamente um *merge*.

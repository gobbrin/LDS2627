# I065 — Notas de release do backend

**Projeto:** G15 — CampusCarpool
**Issue:** I065
**Executor:** Vítor Samuel Vieira Lopes · **Revisor:** Joana Francisca Pacheco Rodrigues · **Auditor:** Gil Gabriel da Cunha Leão

## 1. Identificação da versão

| Campo | Valor |
| --- | --- |
| Componente | Backend CampusCarpool (API .NET 10) |
| Versão | **v0.1.0** (pré-release / candidata à entrega) |
| Branch de origem | `develop` |
| Commit de referência | `2df18e6` — *Merge pull request #27 (fix/i061-compatibilidade-testes-mapas)* |
| Data de referência | 02/11/2026 |
| Plataforma | .NET 10, ASP.NET Core, EF Core 10.0.12, SQL Server |
| Instalação e configuração | [`../manual-de-instalacao/I064-MANUAL-INSTALACAO-BACKEND.md`](../manual-de-instalacao/I064-MANUAL-INSTALACAO-BACKEND.md) |

> O repositório não tem *tags*, ficheiro de versão nem `<Version>` nos `.csproj`. A designação **v0.1.0** e o commit acima são a referência adotada para esta entrega. A `main` continua no commit inicial `dfc1252`; a versão aqui descrita corresponde ao estado da `develop`.

## 2. Resumo

Primeira versão do backend com os fluxos principais de carpooling implementados e testados: autenticação com confirmação por email, viagens, pesquisa, pedidos de participação com atualização em tempo real, gestão de participações, avisos e notificações (incluindo push), mensagens, avaliações e histórico pessoal. Inclui pipeline de CI, testes unitários e de integração, coleção Postman e análise estática.

**Não está pronta para produção** (ver [secção 6](#6-problemas-conhecidos-e-limitações)).

## 3. Funcionalidades incluídas

| Área | Descrição | Endpoints principais (`/api/v1/…`) | Issues |
| --- | --- | --- | --- |
| Autenticação | Registo com validação de email académico, confirmação de conta por email, reenvio de confirmação, login JWT (60 min) | `autenticacao/registo`, `reenviar-confirmacao`, `confirmar`, `login`, `me` | — |
| Utilizadores | Consulta e atualização do próprio perfil | `utilizadores/me` | — |
| Viagens | Criação, edição, consulta, indisponibilizar e concluir; conclusão manual pelo condutor; controlo de concorrência otimista (coluna de versão) | `viagens`, `viagens/{id}`, `/indisponibilizar`, `/concluir` | — |
| Pesquisa | Pesquisa de viagens | `pesquisa/viagens` | — |
| Rotas e mapas | Geocodificação, cálculo de rota e ordenação de pontos (OpenRouteService); estimativa de custo e simulação | `rotas/geocodificar`, `rotas/calcular`, `rotas/viagens/{id}`, `viagens/{id}/simulacao` | I055 |
| Pedidos e participações | Pedido de participação, aceitar, rejeitar e cancelar, com consistência de lugares disponíveis em transação | `pedidos`, `participacoes/{id}/aceitar\|rejeitar\|cancelar` | I038, I041 |
| Tempo real | Atualização imediata da interface para pedidos via SignalR | hub `/hubs/pedidos` | I046 |
| Notificações push | Registo de dispositivos Android e envio de push para pedidos, com fila e trabalhador em segundo plano | `dispositivos` | I047 |
| Avisos e notificações | Aviso do condutor aos passageiros aceites (máx. 100 caracteres); notificações pessoais paginadas, marcar como lida | `viagens/{id}/avisos`, `notificacoes`, `notificacoes/{id}/ler`, `ler-todas` | I044 |
| Mensagens | Conversa entre dois utilizadores | `mensagens`, `mensagens/conversa/{id}` | — |
| Avaliações | Avaliação entre utilizadores por par | `avaliacoes`, `avaliacoes/utilizadores/{id}` | I048 |
| Histórico pessoal | Consulta do histórico de viagens do utilizador | `historico` | I049 |
| Transversal | Rate limiting global (60 pedidos/min por IP, 429), tratamento global de exceções (ProblemDetails), `/health` | — | — |

### Base de dados

Seis migrations EF Core em `Infrastructure/Migrations`:

1. `InitialCreate`
2. `I030_V2_Relacoes`
3. `I038_IndicePedidosAtivos`
4. `I044_AvisoConteudoMaxLength100`
5. `I048_AvaliacoesPorPar`
6. `AdicionarVersaoViagem`

## 4. Qualidade e verificação

| Verificação | Resultado |
| --- | --- |
| `dotnet test CampusCarpool.slnx` (02/11/2026) | 347 testes: **346 aprovados, 1 falhado** (IT023, ver secção 6) |
| Cobertura de linhas (I059, 28/10/2026) | 84,45 % (meta ≥ 80 % atingida); ramos 79,85 % |
| Testes de integração (I054) | 315 testes aprovados na data do relatório |
| Testes de API (I055) | Coleção e *environment* Postman em `docs/I055/` |
| Análise estática (I056) | Scripts e relatório em `docs/AnaliseEstatica/` |
| Pipeline CI (I058) | restore → build Release → `dotnet format` → testes com cobertura → gate (10 %) → publish |

A cobertura de 84,45 % foi medida a 28/10/2026 e não foi recalculada depois das alterações posteriores.

## 5. Correções incluídas

| Correção | Detalhe | Commit / PR |
| --- | --- | --- |
| Registo do `ServicoPush` | Passou a ser registado com `AddHttpClient` no `Program.cs`; removida a duplicação em `AddInfrastructureServices` | `9f1073c` |
| Integração de mapas (OpenRouteService) | Endpoints de geocodificação e direções corrigidos para `api.heigit.org`; chave sujeita a `Trim()` | `a899848` (I055) |
| Atualização em tempo real de pedidos | Ajustes ao `NotificadorPedidosSignalR` após revisão | `8a7b7b5` (I046) |
| Notificações push | Fila e trabalhador em segundo plano; falhas de push não interrompem a operação principal | `21b7a5a` (I047) |
| Testes unitários dos controllers | Cobertura alargada das validações | `2e60d8f` (I051) |
| Scripts de análise estática | Substituídos pelos validados na revisão | `fdcf62f` (I056) |
| Infraestrutura e testes de integração | Dependências e testes ajustados após a integração das branches | `9f1073c`, `0d2351a`, PR #13, PR #27 (I061) |

## 6. Problemas conhecidos e limitações

**Segurança**

- A `Maps:ApiKey` e uma `Jwt:Key` de exemplo estão versionadas em `appsettings.json` e no histórico do Git. A chave de mapas deve ser regenerada e ambas retiradas do ficheiro, passando para User Secrets ou variáveis de ambiente.
- Existem avisos de vulnerabilidade NuGet (`NU1902`/`NU1903`) em `Microsoft.OpenApi` 2.0.0 e `System.Security.Cryptography.Pkcs` 9.0.11.

**Funcionais e operacionais**

- As migrations **não são aplicadas no arranque nem no pipeline**; têm de ser aplicadas manualmente com `dotnet ef database update`. Sem isso a API arranca, mas os endpoints com base de dados falham.
- O registo depende do email (Brevo): sem ele a conta fica criada mas inutilizável (resposta 503).
- `/health` não verifica a base de dados.
- Uma falha no envio de push não é registada em log (`NotificacaoService`).
- Não existe endpoint de *logout*: a entidade `TokenRevogado` existe na base de dados, mas nenhum código a utiliza, pelo que os tokens só expiram aos 60 minutos. A conclusão automática de viagens não está implementada (não existe job no código).
- A publicação em ambiente público (I063) **não faz parte** desta versão.

**Testes**

- `IT023_DuasAceitacoes_UltimoLugar_ApenasUmaPersiste` falha em ambientes sem SQL Server LocalDB (liga-se a LocalDB fixo no código). Falha também fora de Windows e, provavelmente, no CI em `ubuntu-latest`.
- Os testes de integração usam SQLite em memória (`EnsureCreated`): comportamentos específicos do SQL Server e as migrations não são exercitados, à exceção do IT023.
- Cobertura de linhas medida a 28/10/2026; o gate do CI está em 10 %.
- ~280 avisos de compilação (analisadores `CA…`), sem impacto no *build* (`TreatWarningsAsErrors` desligado).

**Documentação**

- `CampusCarpool/README.md` refere `ConnectionStrings__CampusCarpool`; o código lê `ConnectionStrings:DefaultConnection`.
- `docs/IMPLEMENTACAO-STATUS.md` está desatualizado e não deve ser usado como referência do estado da versão.

## 7. Como atualizar para esta versão

1. `git checkout develop` (commit `2df18e6` ou posterior).
2. Seguir o [manual de instalação (I064)](../manual-de-instalacao/I064-MANUAL-INSTALACAO-BACKEND.md): restaurar, compilar, configurar `Jwt:Key`, `ConnectionStrings:DefaultConnection` e integrações.
3. Aplicar as migrations à base de dados (inclui `AdicionarVersaoViagem` e `I048_AvaliacoesPorPar`).
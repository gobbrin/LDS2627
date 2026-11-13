# Relatório de cobertura dos testes do backend

**Projeto:** G15 — CampusCarpool · **Artefacto:** `m3-relatorio-cobertura-testes-backend-v01.pdf` · **Milestone:** m3
**Versão medida:** backend entregue em `04.12-backend/m3-v01`, commit `0205ac1` · **Data da medição:** 06/11/2026

## 1. Objetivo

Apresentar a cobertura obtida pelos testes unitários e de integração do backend, o método de recolha, o âmbito medido, as exclusões, o limite definido e a interpretação dos resultados (regulamento da UC, §19, `03.01.04`).

## 2. Método de recolha

| Elemento | Valor |
| --- | --- |
| Ferramenta | Coverlet (`coverlet.collector`), recolha `XPlat Code Coverage`, formato Cobertura (`coverage.cobertura.xml`) |
| Comando | `dotnet test CampusCarpool.slnx -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage" --logger trx` (o mesmo do passo `Test with Coverage` da pipeline) |
| Testes incluídos | Unitários e de integração, numa só execução (674 execuções: 674 aprovadas, 0 saltadas) |
| Métrica do quality gate | Linhas cobertas / linhas mensuráveis, somadas em todos os projetos (`lines-covered` / `lines-valid`) |
| Execução | Pipeline GitHub Actions 42202915885, ubuntu-latest, .NET 10, com SQL Server em contentor. O quality gate mediu 95,73 % e passou |

## 3. Âmbito e exclusões

**Medido:** os quatro projetos de produção — `CampusCarpool.Api`, `CampusCarpool.Application`, `CampusCarpool.Domain` e `CampusCarpool.Infrastructure`.

**Excluído (`coverage.runsettings`):**

| Exclusão | Justificação |
| --- | --- |
| `**/Migrations/**` | Código gerado pelo EF Core (migrations e snapshot do modelo). Não contém lógica do projeto e inflacionava o total de linhas: com as migrations, a mesma suite media cerca de 34 %. As migrations são verificadas pela geração dos scripts SQL (`04.13`) |
| Projeto de testes (`CampusCarpool.Tests`) | O código dos testes não é código de produção |
| Código gerado pelo compilador | Atributo `[GeneratedCode]`/`[CompilerGenerated]`, conforme a configuração por omissão do Coverlet |

## 4. Limite definido

**80 % das linhas**, testes unitários e de integração em conjunto, sem migrations (requisito RNF-MAN-01 na versão v05 da especificação; decisão do grupo G14 de 04/11/2026; `MIN_COVERAGE: "80"` no workflow entregue). O limite resolve a diferença entre a especificação v04 (70 % de testes unitários) e o plano de qualidade e a DoD (80 %), adotando o valor mais exigente.

## 5. Resultados globais
| Indicador | Valor |
| --- | --- |
| Linhas cobertas | **95,73 %** (3052 de 3188) |
| Ramos cobertos | 87,72 % (857 de 977) |
| Limite (quality gate) | 80 % das linhas |
| Resultado | **Cumpre** |

@@PACOTES@@
| Projeto | Linhas | Linhas cobertas | % linhas | Ramos | Ramos cobertos | % ramos |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `CampusCarpool.Domain` | 307 | 305 | 99,35 % | 106 | 102 | 96,23 % |
| `CampusCarpool.Api` | 434 | 420 | 96,77 % | 170 | 134 | 78,82 % |
| `CampusCarpool.Application` | 1506 | 1430 | 94,95 % | 569 | 498 | 87,52 % |
| `CampusCarpool.Infrastructure` | 941 | 897 | 95,32 % | 132 | 123 | 93,18 % |

@@CLASSES@@
| Projeto | Classe | Linhas | Cobertas | % |
| --- | --- | ---: | ---: | ---: |
| Api | `Api.Configuration.LimitesPedidos` | 17 | 17 | 100,00 % |
| Api | `Api.Configuration.PoliticaCors` | 3 | 3 | 100,00 % |
| Api | `Api.Configuration.ValidacaoTokensRevogados` | 8 | 8 | 100,00 % |
| Api | `Api.Controllers.AutenticacaoController` | 32 | 32 | 100,00 % |
| Api | `Api.Controllers.AvaliacoesController` | 16 | 7 | 43,75 % |
| Api | `Api.Controllers.AvisosController` | 11 | 11 | 100,00 % |
| Api | `Api.Controllers.CorpoErro` | 1 | 1 | 100,00 % |
| Api | `Api.Controllers.DispositivosController` | 8 | 8 | 100,00 % |
| Api | `Api.Controllers.GestaoParticipacoesController` | 10 | 10 | 100,00 % |
| Api | `Api.Controllers.HistoricoController` | 8 | 8 | 100,00 % |
| Api | `Api.Controllers.MensagensController` | 15 | 15 | 100,00 % |
| Api | `Api.Controllers.NotificacoesController` | 13 | 13 | 100,00 % |
| Api | `Api.Controllers.NotificacoesMarcadas` | 1 | 1 | 100,00 % |
| Api | `Api.Controllers.PedidosController` | 11 | 11 | 100,00 % |
| Api | `Api.Controllers.PesquisaController` | 6 | 6 | 100,00 % |
| Api | `Api.Controllers.RespostaMensagem` | 1 | 1 | 100,00 % |
| Api | `Api.Controllers.RespostasHttp` | 16 | 15 | 93,75 % |
| Api | `Api.Controllers.RotasController` | 16 | 16 | 100,00 % |
| Api | `Api.Controllers.SessaoAtual` | 1 | 1 | 100,00 % |
| Api | `Api.Controllers.SimulacoesController` | 9 | 8 | 88,89 % |
| Api | `Api.Controllers.UtilizadorAutenticado` | 2 | 2 | 100,00 % |
| Api | `Api.Controllers.UtilizadoresController` | 31 | 28 | 90,32 % |
| Api | `Api.Controllers.ViagensController` | 29 | 29 | 100,00 % |
| Api | `Api.Hubs.EventoPedido` | 1 | 1 | 100,00 % |
| Api | `Api.Hubs.NotificadorPedidosSignalR` | 39 | 39 | 100,00 % |
| Api | `Api.Hubs.PedidosUserIdProvider` | 6 | 6 | 100,00 % |
| Api | `Api.Middleware.GlobalExceptionHandler` | 44 | 44 | 100,00 % |
| Api | `Program` | 79 | 79 | 100,00 % |
| Application | `Application.Abstractions.Integrations.MatrizRotas` | 3 | 3 | 100,00 % |
| Application | `Application.Abstractions.Integrations.NotificacaoPushPendente` | 1 | 1 | 100,00 % |
| Application | `Application.Abstractions.Integrations.ResultadoRota` | 5 | 5 | 100,00 % |
| Application | `Application.Abstractions.Integrations.TokenAcesso` | 1 | 1 | 100,00 % |
| Application | `Application.Abstractions.Persistence.ClassificacaoRecebida` | 1 | 1 | 100,00 % |
| Application | `Application.Abstractions.Persistence.PaginaViagens` | 1 | 1 | 100,00 % |
| Application | `Application.Abstractions.Persistence.ViagemEmCurso` | 1 | 1 | 100,00 % |
| Application | `Application.Abstractions.Persistence.ViagemPesquisada` | 10 | 10 | 100,00 % |
| Application | `Application.Avaliacoes.AvaliacaoPublica` | 1 | 1 | 100,00 % |
| Application | `Application.Avaliacoes.AvaliacaoRegistada` | 1 | 1 | 100,00 % |
| Application | `Application.Avaliacoes.AvaliacaoService` | 39 | 39 | 100,00 % |
| Application | `Application.Avaliacoes.ResumoAvaliacoes` | 1 | 1 | 100,00 % |
| Application | `Application.Comum.Erro` | 1 | 1 | 100,00 % |
| Application | `Application.Comum.Resultado` | 4 | 4 | 100,00 % |
| Application | `Application.Comum.Resultado`1` | 8 | 8 | 100,00 % |
| Application | `Application.Comunicacao.AvisoCriado` | 1 | 1 | 100,00 % |
| Application | `Application.Comunicacao.AvisoDaViagem` | 1 | 1 | 100,00 % |
| Application | `Application.Comunicacao.AvisoService` | 35 | 35 | 100,00 % |
| Application | `Application.Comunicacao.MensagemIndividual` | 7 | 7 | 100,00 % |
| Application | `Application.Comunicacao.MensagemService` | 46 | 43 | 93,48 % |
| Application | `Application.Comunicacao.ResumoConversa` | 5 | 5 | 100,00 % |
| Application | `Application.DependencyInjection.ApplicationServiceRegistration` | 18 | 18 | 100,00 % |
| Application | `Application.Historico.AvisoHistorico` | 6 | 6 | 100,00 % |
| Application | `Application.Historico.HistoricoResultado` | 8 | 8 | 100,00 % |
| Application | `Application.Historico.HistoricoService` | 15 | 15 | 100,00 % |
| Application | `Application.Historico.NotificacaoHistorico` | 9 | 9 | 100,00 % |
| Application | `Application.Historico.PedidoHistorico` | 11 | 11 | 100,00 % |
| Application | `Application.Historico.ViagemHistorico` | 10 | 10 | 100,00 % |
| Application | `Application.Identidade.AutenticacaoService` | 119 | 115 | 96,64 % |
| Application | `Application.Identidade.DispositivoService` | 11 | 11 | 100,00 % |
| Application | `Application.Identidade.LigacaoPerfil` | 1 | 1 | 100,00 % |
| Application | `Application.Identidade.OpcoesEmailAcademico` | 1 | 1 | 100,00 % |
| Application | `Application.Identidade.PerfilAtualizado` | 7 | 2 | 28,57 % |
| Application | `Application.Identidade.PerfilProprio` | 11 | 11 | 100,00 % |
| Application | `Application.Identidade.PerfilPublico` | 8 | 8 | 100,00 % |
| Application | `Application.Identidade.PerfilService` | 85 | 85 | 100,00 % |
| Application | `Application.Identidade.ValidadorEmailAcademico` | 10 | 10 | 100,00 % |
| Application | `Application.Notificacoes.NotificacaoPessoal` | 9 | 9 | 100,00 % |
| Application | `Application.Notificacoes.NotificacaoService` | 80 | 77 | 96,25 % |
| Application | `Application.Notificacoes.PaginaNotificacoes` | 7 | 7 | 100,00 % |
| Application | `Application.Participacoes.ParticipacaoService` | 285 | 273 | 95,79 % |
| Application | `Application.Participacoes.PassageiroAceite` | 1 | 1 | 100,00 % |
| Application | `Application.Participacoes.PedidoCriado` | 8 | 8 | 100,00 % |
| Application | `Application.Participacoes.PedidoPendente` | 1 | 1 | 100,00 % |
| Application | `Application.Participacoes.SimulacaoPedido` | 15 | 8 | 53,33 % |
| Application | `Application.Rotas.ChamadaMapas` | 7 | 7 | 100,00 % |
| Application | `Application.Rotas.ConsultaRotasService` | 75 | 49 | 65,33 % |
| Application | `Application.Rotas.EstimativaCustoService` | 29 | 29 | 100,00 % |
| Application | `Application.Rotas.OpcoesCustos` | 1 | 1 | 100,00 % |
| Application | `Application.Rotas.ResultadoEstimativaCusto` | 10 | 10 | 100,00 % |
| Application | `Application.Rotas.ResultadoRotaComPontos` | 6 | 6 | 100,00 % |
| Application | `Application.Rotas.RotaDaViagem` | 8 | 8 | 100,00 % |
| Application | `Application.Rotas.RotaGuardada` | 61 | 61 | 100,00 % |
| Application | `Application.Rotas.RotaService` | 68 | 66 | 97,06 % |
| Application | `Application.Rotas.SegmentoRota` | 6 | 6 | 100,00 % |
| Application | `Application.Viagens.ConclusaoViagensService` | 35 | 34 | 97,14 % |
| Application | `Application.Viagens.CondutorViagem` | 1 | 1 | 100,00 % |
| Application | `Application.Viagens.ConsultaViagensService` | 17 | 17 | 100,00 % |
| Application | `Application.Viagens.DetalhesViagem` | 14 | 14 | 100,00 % |
| Application | `Application.Viagens.FiltroPesquisaViagens` | 7 | 7 | 100,00 % |
| Application | `Application.Viagens.PesquisaService` | 18 | 18 | 100,00 % |
| Application | `Application.Viagens.ResultadoPesquisa` | 1 | 1 | 100,00 % |
| Application | `Application.Viagens.ViagemCriada` | 8 | 8 | 100,00 % |
| Application | `Application.Viagens.ViagemDoCondutor` | 7 | 3 | 42,86 % |
| Application | `Application.Viagens.ViagemEncontrada` | 10 | 10 | 100,00 % |
| Application | `Application.Viagens.ViagemService` | 208 | 199 | 95,67 % |
| Domain | `Domain.Entities.Avaliacao` | 14 | 14 | 100,00 % |
| Domain | `Domain.Entities.Aviso` | 5 | 5 | 100,00 % |
| Domain | `Domain.Entities.ConfirmacaoConta` | 11 | 11 | 100,00 % |
| Domain | `Domain.Entities.Coordenadas` | 3 | 3 | 100,00 % |
| Domain | `Domain.Entities.Dispositivo` | 4 | 4 | 100,00 % |
| Domain | `Domain.Entities.LigacaoExterna` | 5 | 5 | 100,00 % |
| Domain | `Domain.Entities.LocaisViagem` | 5 | 5 | 100,00 % |
| Domain | `Domain.Entities.Mensagem` | 6 | 6 | 100,00 % |
| Domain | `Domain.Entities.Notificacao` | 10 | 10 | 100,00 % |
| Domain | `Domain.Entities.Participacao` | 40 | 38 | 95,00 % |
| Domain | `Domain.Entities.PontoRota` | 18 | 18 | 100,00 % |
| Domain | `Domain.Entities.TokenRevogado` | 3 | 3 | 100,00 % |
| Domain | `Domain.Entities.Utilizador` | 14 | 14 | 100,00 % |
| Domain | `Domain.Entities.Viagem` | 78 | 78 | 100,00 % |
| Domain | `Domain.Exceptions.RegraDeNegocioException` | 4 | 4 | 100,00 % |
| Domain | `Domain.Rules.CalculadoraDeClassificacao` | 4 | 4 | 100,00 % |
| Domain | `Domain.Rules.CalculadoraDeCusto` | 4 | 4 | 100,00 % |
| Domain | `Domain.Rules.HoraDePortugal` | 5 | 5 | 100,00 % |
| Domain | `Domain.Rules.OrdenadorDePontos` | 61 | 61 | 100,00 % |
| Domain | `Domain.Rules.PoliticaDeVisibilidade` | 4 | 4 | 100,00 % |
| Domain | `Domain.Rules.RegraMesmoLocal` | 3 | 3 | 100,00 % |
| Domain | `Domain.Rules.TextoPesquisa` | 6 | 6 | 100,00 % |
| Infrastructure | `Infrastructure.BackgroundJobs.FilaNotificacoesPush` | 11 | 11 | 100,00 % |
| Infrastructure | `Infrastructure.BackgroundJobs.TrabalhadorConclusaoViagens` | 24 | 11 | 45,83 % |
| Infrastructure | `Infrastructure.BackgroundJobs.TrabalhadorNotificacoesPush` | 28 | 6 | 21,43 % |
| Infrastructure | `Infrastructure.DependencyInjection.InfrastructureServiceRegistration` | 41 | 41 | 100,00 % |
| Infrastructure | `Infrastructure.Integrations.CacheTokenFcm` | 14 | 14 | 100,00 % |
| Infrastructure | `Infrastructure.Integrations.ServicoEmail` | 34 | 34 | 100,00 % |
| Infrastructure | `Infrastructure.Integrations.ServicoMapasRotas` | 113 | 113 | 100,00 % |
| Infrastructure | `Infrastructure.Integrations.ServicoPush` | 90 | 86 | 95,56 % |
| Infrastructure | `Infrastructure.Persistence.CampusCarpoolDbContext` | 129 | 129 | 100,00 % |
| Infrastructure | `Infrastructure.Persistence.ConversorDataUtc` | 3 | 3 | 100,00 % |
| Infrastructure | `Infrastructure.Persistence.UnidadeDeTrabalhoEfCore` | 11 | 11 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioAvaliacoes` | 22 | 20 | 90,91 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioAvisos` | 8 | 8 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioConfirmacoesConta` | 16 | 16 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioDispositivo` | 22 | 19 | 86,36 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioHistorico` | 153 | 153 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioLigacoesExternas` | 12 | 12 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioMensagens` | 34 | 34 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioNotificacoes` | 24 | 24 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioParticipacoes` | 60 | 60 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioTokensRevogados` | 6 | 6 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioUtilizadores` | 19 | 19 | 100,00 % |
| Infrastructure | `Infrastructure.Repositories.RepositorioViagens` | 50 | 50 | 100,00 % |
| Infrastructure | `Infrastructure.Security.GeradorTokensJwt` | 12 | 12 | 100,00 % |
| Infrastructure | `Infrastructure.Security.HashPalavraPasse` | 5 | 5 | 100,00 % |

@@BAIXAS@@
| Projeto | Classe | Linhas | Cobertas | % |
| --- | --- | ---: | ---: | ---: |
| Infrastructure | `Infrastructure.BackgroundJobs.TrabalhadorNotificacoesPush` | 28 | 6 | 21,43 % |
| Application | `Application.Identidade.PerfilAtualizado` | 7 | 2 | 28,57 % |
| Application | `Application.Viagens.ViagemDoCondutor` | 7 | 3 | 42,86 % |
| Api | `Api.Controllers.AvaliacoesController` | 16 | 7 | 43,75 % |
| Infrastructure | `Infrastructure.BackgroundJobs.TrabalhadorConclusaoViagens` | 24 | 11 | 45,83 % |
| Application | `Application.Participacoes.SimulacaoPedido` | 15 | 8 | 53,33 % |
| Application | `Application.Rotas.ConsultaRotasService` | 75 | 49 | 65,33 % |

## 9. Evolução

| Data | Medição | Linhas | Âmbito |
| --- | --- | --- | --- |
| 28/10/2026 | I054 | 34,00 % | Com migrations |
| 28/10/2026 | I059 | 84,45 % (1467/1737) | Sem migrations |
| 04/11/2026 | Pipeline 41811030439 | 88,14 % | Sem migrations |
| 04/11/2026 | Pipeline 41820043932 | 89,46 % (2164/2419) | Sem migrations |
| 06/11/2026 | Pipeline 42198668936 (`c9b9ebc`) | 95,58 % (3047/3188) | Sem migrations |
| 06/11/2026 | Pipeline 42202915885, esta medição (`0205ac1`) | **95,73 %** (3052/3188) | Sem migrations |

O total de linhas mensuráveis passou de 1737 para 3188 com o código acrescentado depois de 28/10 (I060, I061 e I110), e a cobertura subiu porque cada correção trouxe testes de regressão.

## 10. Interpretação

- **O limite é cumprido** com margem: 95,73 % das linhas contra 80 % exigidos; 87,71 % dos ramos.
- As classes abaixo de 80 % (secção 8) correspondem a: trabalhadores em segundo plano (`TrabalhadorNotificacoesPush`, `TrabalhadorConclusaoViagens`), em que o ciclo do temporizador não é executado nos testes (a lógica de conclusão é testada diretamente no serviço, `ConclusaoViagensTests`); repositórios usados só por alguns fluxos; registos (*records*) de leitura do histórico, cujas propriedades não são todas lidas; e ramos de erro de controllers (`AvaliacoesController`, `RotasController`, `AutenticacaoController`) em que só alguns códigos de erro são testados.
- **Cobertura não é prova de correção.** Uma linha coberta foi executada, não necessariamente verificada por uma asserção. A ligação entre requisitos e testes, e os requisitos ainda sem teste, estão na RTM v02 (`04.03`); os defeitos conhecidos estão no registo de defeitos (`03.01.07`).
- Esta medição inclui os testes específicos de SQL Server. Numa máquina sem SQL Server o valor desce para 95,51 % (3045/3188), porque esses testes ficam saltados.

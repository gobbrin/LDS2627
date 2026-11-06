# Resultados dos testes de integração do backend

**Projeto:** G15 — CampusCarpool · **Artefacto:** `m3-resultados-testes-integracao-backend-v01.pdf` · **Milestone:** m3
**Especificação:** I053 (`m3-especificacao-testes-integracao-backend-v01.md`) · **Implementação e primeira execução:** I054 (`04.20-documentacao-complementar/m3-evidencias-testes-integracao-backend-v01.md`, 28/10/2026)
**Execução registada neste documento:** 06/11/2026, sobre a versão entregue do backend (preparação da entrega, I068)

## 1. Objetivo

Registar a execução, os resultados, as falhas, as evidências e a versão testada dos testes de integração do backend, e a conclusão. A execução aqui registada é a da versão entregue em `04.12-backend/m3-v01`, com o mesmo comando usado no passo `Test with Coverage` da pipeline.

## 2. Execuções anteriores

| Data | Execução | Resultado | Fonte |
| --- | --- | --- | --- |
| 28/10/2026 | I054, branch `fix/i054-testes-integracao`, suite completa | 315 aprovados, 0 falhados, 0 saltados (unitários e de integração em conjunto) | `m3-evidencias-testes-integracao-backend-v01.md` |
| 04/11/2026 09:58 UTC | Pipeline GitHub Actions 41811030439 (`e8c73d5`) | 351 aprovados, 0 falhados, 1 saltado | `04.16-ci-cd/m3-relatorio-evidencias-pipeline-ci-cd-v01.md` |
| 04/11/2026 11:20 UTC | Pipeline GitHub Actions 41820043932 (`977ffcd`) | 375 aprovados, 0 falhados, 1 saltado | idem |
| 04/11/2026 | Execução local de `ee689ee` | 375 aprovados, 0 falhados, 1 saltado | Pacote congelado de 05/11 |
| 05/11/2026 | Pipeline GitHub Actions 42122412474 (`304462a`) | 432 aprovados, 0 falhados, 0 saltados, com SQL Server real | `04.16-ci-cd/m3-relatorio-evidencias-pipeline-ci-cd-v01.md` |
| 06/11/2026 | Pipeline GitHub Actions 42198668936 (`c9b9ebc`) | 661 aprovados em SQLite; 660 aprovados e 1 saltado na repetição em SQL Server | idem |
| 06/11/2026 | **Pipeline GitHub Actions 42202915885, versão entregue (`0205ac1`)** | **674 aprovados, 0 falhados, 0 saltados**, dos quais 414 de integração | este documento |

O aumento de 315 para 674 execuções vem dos testes acrescentados depois da I054: I060, I061 e os testes de regressão, HTTP e de SQL Server da I110.

## 3. Identificação da execução
| Campo | Valor |
| --- | --- |
| Versão testada | Backend entregue em `04.12-backend/m3-v01`, commit `0205ac1` |
| Execução | 06/11/2026 10:30 UTC, pipeline, `dotnet test CampusCarpool.slnx -c Release --settings coverage.runsettings --collect:"XPlat Code Coverage" --logger trx` |
| Ambiente | GitHub Actions, ubuntu-latest, .NET SDK 10 (`global.json`), SQLite em memória e SQL Server 2022 em contentor |
| Correspondência com a pipeline | Esta é a própria execução da pipeline: 42202915885 do GitHub Actions, com todos os passos verdes. A repetição da suite em SQL Server real deu 673 aprovados, 0 falhados e 1 saltado de propósito (`I110_15`, só faz sentido em SQLite) |
| Indicador | Resultado |
| --- | --- |
| Execuções de teste | 414 |
| Aprovadas | 414 |
| Falhadas | 0 |
| Saltadas | 0 |
| Classes de teste | 56 |

### Resultados por classe

| Classe | Execuções | Aprovadas | Falhadas | Saltadas |
| --- | ---: | ---: | ---: | ---: |
| `ApiIntegrationTests` | 12 | 12 | 0 | 0 |
| `AutenticacaoFluxoIntegrationTests` | 1 | 1 | 0 | 0 |
| `AutenticacaoRealHttpTests` | 19 | 19 | 0 | 0 |
| `AvaliacoesHttpIntegrationTests` | 5 | 5 | 0 | 0 |
| `AvisosIntegrationTests` | 20 | 20 | 0 | 0 |
| `CoberturaMensagensCenariosAdicionaisTests` | 2 | 2 | 0 | 0 |
| `CoberturaRepositoriosMensagensTests` | 4 | 4 | 0 | 0 |
| `ConclusaoViagensTests` | 8 | 8 | 0 | 0 |
| `ConcorrenciaSqlServerIntegrationTests` | 1 | 1 | 0 | 0 |
| `ConfiguracaoSegurancaTests` | 5 | 5 | 0 | 0 |
| `ConfirmacaoContaHttpTests` | 4 | 4 | 0 | 0 |
| `ConsultasViagemTests` | 15 | 15 | 0 | 0 |
| `ContratoRespostasHttpTests` | 4 | 4 | 0 | 0 |
| `CriacaoViagemIntegrationTests` | 1 | 1 | 0 | 0 |
| `DispositivosAtivosIntegrationTests` | 2 | 2 | 0 | 0 |
| `DispositivosHttpIntegrationTests` | 6 | 6 | 0 | 0 |
| `EndpointsAdditionalTests` | 30 | 30 | 0 | 0 |
| `EstadoViagemTests` | 7 | 7 | 0 | 0 |
| `EstimativaCustoTests` | 11 | 11 | 0 | 0 |
| `FalhaTransacaoIntegrationTests` | 1 | 1 | 0 | 0 |
| `HistoricoHttpIntegrationTests` | 3 | 3 | 0 | 0 |
| `HistoricoIntegrationTests` | 12 | 12 | 0 | 0 |
| `I054IntegridadeAdicionalIntegrationTests` | 5 | 5 | 0 | 0 |
| `IndisponibilizacaoHistoricoTests` | 4 | 4 | 0 | 0 |
| `IntegridadeRelacionalAdditionalTests` | 7 | 7 | 0 | 0 |
| `LigacoesExternasTests` | 7 | 7 | 0 | 0 |
| `MapasAndValidationTests` | 4 | 4 | 0 | 0 |
| `MensagensControllerTests` | 8 | 8 | 0 | 0 |
| `MensagensNotificacoesIntegrationTests` | 1 | 1 | 0 | 0 |
| `MensagensServicoRealHttpTests` | 9 | 9 | 0 | 0 |
| `MigracoesSqlServerTests` | 4 | 4 | 0 | 0 |
| `NotificacoesMesmaGravacaoTests` | 6 | 6 | 0 | 0 |
| `NotificacoesSoNaAplicacaoHttpTests` | 3 | 3 | 0 | 0 |
| `ParticipacoesGestaoIntegrationTests` | 19 | 19 | 0 | 0 |
| `PedidosHubIntegrationTests` | 4 | 4 | 0 | 0 |
| `PedidosParticipacaoIntegrationTests` | 21 | 21 | 0 | 0 |
| `PerfilPublicoTests` | 3 | 3 | 0 | 0 |
| `PesquisaHoraPortugalHttpTests` | 9 | 9 | 0 | 0 |
| `PipelineHttpTests` | 6 | 6 | 0 | 0 |
| `ProntoParaFrontendHttpTests` | 10 | 10 | 0 | 0 |
| `ProtecaoDeDadosHttpTests` | 7 | 7 | 0 | 0 |
| `RegistoResilienteTests` | 10 | 10 | 0 | 0 |
| `RegistoServicosTests` | 3 | 3 | 0 | 0 |
| `RegrasSqlServerTests` | 3 | 3 | 0 | 0 |
| `RegrasViagemHttpTests` | 6 | 6 | 0 | 0 |
| `RepositorioParticipacoesCoberturaTests` | 7 | 7 | 0 | 0 |
| `RestricoesBaseDeDadosTests` | 10 | 10 | 0 | 0 |
| `RevisaoFinalHttpTests` | 4 | 4 | 0 | 0 |
| `RotaGuardadaTests` | 13 | 13 | 0 | 0 |
| `RotasParticipacoesIntegrationTests` | 11 | 11 | 0 | 0 |
| `RotasProtegidasHttpTests` | 3 | 3 | 0 | 0 |
| `ServicoMapasEmFalhaHttpTests` | 6 | 6 | 0 | 0 |
| `SimulacoesIntegrationTests` | 17 | 17 | 0 | 0 |
| `TerminarSessaoHttpTests` | 6 | 6 | 0 | 0 |
| `ViagensEstimativaIntegrationTests` | 3 | 3 | 0 | 0 |
| `ViagensNotificacoesIntegrationTests` | 2 | 2 | 0 | 0 |

### Lista integral das execuções

| # | Classe | Método (com parâmetros) | Resultado | ms |
| ---: | --- | --- | --- | ---: |
| 1 | ApiIntegrationTests | CT008_RegistoEmailNaoAcademico_Devolve400 | Aprovado | 49 |
| 2 | ApiIntegrationTests | CT009_RegistoPasswordCurta_Devolve400 | Aprovado | 28 |
| 3 | ApiIntegrationTests | CT010_RegistoDuplicado_Devolve409 | Aprovado | 33 |
| 4 | ApiIntegrationTests | CT013_TokenConfirmacaoInvalido_Devolve400 | Aprovado | 29 |
| 5 | ApiIntegrationTests | CT016_LoginPasswordErrada_Devolve401 | Aprovado | 152 |
| 6 | ApiIntegrationTests | CT019_PerfilProprio_NaoExpoePasswordHash | Aprovado | 36 |
| 7 | ApiIntegrationTests | CT020_AtualizarPerfil_AlteraSoCamposPermitidos | Aprovado | 34 |
| 8 | ApiIntegrationTests | CT022_NomeVazio_Devolve400 | Aprovado | 35 |
| 9 | ApiIntegrationTests | CT026_ConsultarViagemInexistente_Devolve404 | Aprovado | 28 |
| 10 | ApiIntegrationTests | CT027_OutroCondutor_NaoPodeEditar | Aprovado | 42 |
| 11 | ApiIntegrationTests | CT034_PesquisaExcluiViagensInativas | Aprovado | 53 |
| 12 | ApiIntegrationTests | CT038_PaginacaoInvalida_Devolve400 | Aprovado | 35 |
| 13 | AutenticacaoFluxoIntegrationTests | IT001_IT003_IT004_RegistoConfirmacaoLogin_FluxoValido | Aprovado | 318 |
| 14 | AutenticacaoRealHttpTests | I110_74_TokenDaApi_EAceite | Aprovado | 275 |
| 15 | AutenticacaoRealHttpTests | I110_75_TokenExpirado_Devolve401 | Aprovado | 255 |
| 16 | AutenticacaoRealHttpTests | I110_76_TokenExpiradoDentroDaTolerancia_EAceite | Aprovado | 288 |
| 17 | AutenticacaoRealHttpTests | I110_77_TokenInvalido_Devolve401(caso: "assinatura") | Aprovado | 252 |
| 18 | AutenticacaoRealHttpTests | I110_77_TokenInvalido_Devolve401(caso: "audiencia") | Aprovado | 237 |
| 19 | AutenticacaoRealHttpTests | I110_77_TokenInvalido_Devolve401(caso: "emissor") | Aprovado | 269 |
| 20 | AutenticacaoRealHttpTests | I110_77_TokenInvalido_Devolve401(caso: "lixo") | Aprovado | 284 |
| 21 | AutenticacaoRealHttpTests | I110_77_TokenInvalido_Devolve401(caso: "sem-assinatura") | Aprovado | 257 |
| 22 | AutenticacaoRealHttpTests | I110_77_TokenInvalido_Devolve401(caso: "sem-jti") | Aprovado | 256 |
| 23 | AutenticacaoRealHttpTests | I110_78_Hub_AceitaTokenNaQueryString | Aprovado | 268 |
| 24 | AutenticacaoRealHttpTests | I110_79_Hub_SemTokenValido_Devolve401(caso: "assinatura") | Aprovado | 233 |
| 25 | AutenticacaoRealHttpTests | I110_79_Hub_SemTokenValido_Devolve401(caso: "lixo") | Aprovado | 251 |
| 26 | AutenticacaoRealHttpTests | I110_79_Hub_SemTokenValido_Devolve401(caso: "sem-jti") | Aprovado | 225 |
| 27 | AutenticacaoRealHttpTests | I110_79_Hub_SemTokenValido_Devolve401(caso: null) | Aprovado | 267 |
| 28 | AutenticacaoRealHttpTests | I110_80_Hub_RecusaTokenRevogado | Aprovado | 275 |
| 29 | AutenticacaoRealHttpTests | I110_81_TokenNaQueryString_ForaDoHub_Devolve401 | Aprovado | 251 |
| 30 | AutenticacaoRealHttpTests | I110_82_PedidoSubmetido_ChegaAoCondutorPeloHub | Aprovado | 770 |
| 31 | AutenticacaoRealHttpTests | I110_83_Login_NaoRevelaQualDosCamposEstaErrado | Aprovado | 518 |
| 32 | AutenticacaoRealHttpTests | I110_84_Registo_ConfirmacaoELogin_PelaApiReal | Aprovado | 758 |
| 33 | AvaliacoesHttpIntegrationTests | IT034_AvaliacaoValida_Devolve201EPersiste | Aprovado | 171 |
| 34 | AvaliacoesHttpIntegrationTests | IT035_AvaliacaoDuplicada_Devolve409 | Aprovado | 204 |
| 35 | AvaliacoesHttpIntegrationTests | IT035_ClassificacaoInvalida_Devolve400 | Aprovado | 165 |
| 36 | AvaliacoesHttpIntegrationTests | IT035_ParticipacaoNaoAceite_Devolve409 | Aprovado | 189 |
| 37 | AvaliacoesHttpIntegrationTests | IT035_SemAutenticacao_Devolve401 | Aprovado | 131 |
| 38 | AvisosIntegrationTests | CondutorCriaAviso_NotificaApenasPassageirosAceites | Aprovado | 34 |
| 39 | AvisosIntegrationTests | ConteudoComCemCaracteres_EValido | Aprovado | 19 |
| 40 | AvisosIntegrationTests | ConteudoComMaisDeCemCaracteres_EInvalido | Aprovado | 22 |
| 41 | AvisosIntegrationTests | ConteudoInvalido_NaoCriaAviso(conteudo: "   ") | Aprovado | 17 |
| 42 | AvisosIntegrationTests | ConteudoInvalido_NaoCriaAviso(conteudo: "") | Aprovado | 22 |
| 43 | AvisosIntegrationTests | ConteudoInvalido_NaoCriaAviso(conteudo: null) | Aprovado | 18 |
| 44 | AvisosIntegrationTests | I110_ViagemNaoAtiva_RecusaOAvisoSemGravarNada(estado: Concluida) | Aprovado | 31 |
| 45 | AvisosIntegrationTests | I110_ViagemNaoAtiva_RecusaOAvisoSemGravarNada(estado: Indisponibilizada) | Aprovado | 20 |
| 46 | AvisosIntegrationTests | MarcarTodasComoLidasSemIdentidade_Devolve401 | Aprovado | 20 |
| 47 | AvisosIntegrationTests | NotificacaoDoProprioUtilizadorPodeSerMarcadaComoLida | Aprovado | 78 |
| 48 | AvisosIntegrationTests | NotificacoesSaoPessoaisEOrdenadasPorChegada | Aprovado | 51 |
| 49 | AvisosIntegrationTests | NotificacoesSemIdentidade_Devolve401 | Aprovado | 29 |
| 50 | AvisosIntegrationTests | NotificacoesSuportamPaginacao | Aprovado | 26 |
| 51 | AvisosIntegrationTests | OutroUtilizadorNaoPodeCriarAviso | Aprovado | 19 |
| 52 | AvisosIntegrationTests | PassageiroAceitePodeConsultarAvisos | Aprovado | 62 |
| 53 | AvisosIntegrationTests | PassageiroPendenteNaoPodeConsultarAvisos | Aprovado | 24 |
| 54 | AvisosIntegrationTests | UtilizadorNaoPodeMarcarNotificacaoDeOutroComoLida | Aprovado | 44 |
| 55 | AvisosIntegrationTests | UtilizadorNaoRelacionadoNaoPodeConsultarAvisos | Aprovado | 19 |
| 56 | AvisosIntegrationTests | UtilizadorPodeMarcarTodasAsNotificacoesComoLidas | Aprovado | 69 |
| 57 | AvisosIntegrationTests | ViagemInexistenteNaoCriaAviso | Aprovado | 24 |
| 58 | CoberturaMensagensCenariosAdicionaisTests | ObterConversa_ComMesmaData_OrdenaPorIdentificadorESemTracking | Aprovado | 50 |
| 59 | CoberturaMensagensCenariosAdicionaisTests | ObterConversa_SemMensagens_DevolveListaVazia | Aprovado | 27 |
| 60 | CoberturaRepositoriosMensagensTests | EntidadesDeConfirmacaoELigacao_PreservamPropriedades | Aprovado | 2 |
| 61 | CoberturaRepositoriosMensagensTests | ExcecaoDeRegraDeNegocio_PreservaMensagemECausa | Aprovado | 0 |
| 62 | CoberturaRepositoriosMensagensTests | ObterConversa_DevolveApenasMensagensEntreDoisUtilizadores_Ordenadas | Aprovado | 41 |
| 63 | CoberturaRepositoriosMensagensTests | UnidadeDeTrabalho_ConfirmaAlteracoesPendentes | Aprovado | 38 |
| 64 | ConclusaoViagensTests | I110_11_ViagemTerminada_ConcluidaPendentesCanceladosAceitesMantidos | Aprovado | 53 |
| 65 | ConclusaoViagensTests | I110_12_ViagemPartidaMasDentroDaDuracao_ContinuaAtiva | Aprovado | 38 |
| 66 | ConclusaoViagensTests | I110_13_ViagemSemDuracaoEstimada_ConcluidaAHoraDePartida | Aprovado | 35 |
| 67 | ConclusaoViagensTests | I110_14_ViagensFuturasOuNaoAtivas_NaoSaoAlteradas | Aprovado | 63 |
| 68 | ConclusaoViagensTests | I110_15_FalhaNumaViagem_NaoImpedeAsRestantes | Aprovado | 40 |
| 69 | ConclusaoViagensTests | I110_16_Temporizador_IntervaloConfiguravel(valor: "0", segundosEsperados: 1) | Aprovado | 34 |
| 70 | ConclusaoViagensTests | I110_16_Temporizador_IntervaloConfiguravel(valor: "300", segundosEsperados: 300) | Aprovado | 35 |
| 71 | ConclusaoViagensTests | I110_16_Temporizador_IntervaloConfiguravel(valor: null, segundosEsperados: 60) | Aprovado | 30 |
| 72 | ConcorrenciaSqlServerIntegrationTests | IT023_DuasAceitacoes_UltimoLugar_ApenasUmaPersiste | Aprovado | 1865 |
| 73 | ConfiguracaoSegurancaTests | I110_01_AppSettingsVersionado_NaoTemJwtKeyNemMapsApiKey | Aprovado | 0 |
| 74 | ConfiguracaoSegurancaTests | I110_02_AppSettingsVersionado_ConfirmUrlApontaParaConfirmarConta | Aprovado | 1 |
| 75 | ConfiguracaoSegurancaTests | I110_03_AppSettingsVersionado_NaoTemSecaoEmailMorta | Aprovado | 0 |
| 76 | ConfiguracaoSegurancaTests | I110_04_ArranqueSemJwtKeyValida_Recusado(chave: "chave-curta") | Aprovado | 6 |
| 77 | ConfiguracaoSegurancaTests | I110_04_ArranqueSemJwtKeyValida_Recusado(chave: null) | Aprovado | 4 |
| 78 | ConfirmacaoContaHttpTests | I110_114_LigacaoExpirada_DizQueExpirou | Aprovado | 276 |
| 79 | ConfirmacaoContaHttpTests | I110_115_LigacaoJaUsada_DizQueFoiUsada | Aprovado | 2284 |
| 80 | ConfirmacaoContaHttpTests | I110_116_LigacaoDesconhecida_DizQueEInvalida | Aprovado | 176 |
| 81 | ConfirmacaoContaHttpTests | I110_117_Reenvio_InvalidaAsLigacoesAnteriores | Aprovado | 359 |
| 82 | ConsultasViagemTests | I110_46_ViagemNaoPesquisavel_SoParaOCondutorEQuemTemPedido(estado: Ativa, lugares: 0) | Aprovado | 29 |
| 83 | ConsultasViagemTests | I110_46_ViagemNaoPesquisavel_SoParaOCondutorEQuemTemPedido(estado: Concluida, lugares: 2) | Aprovado | 47 |
| 84 | ConsultasViagemTests | I110_46_ViagemNaoPesquisavel_SoParaOCondutorEQuemTemPedido(estado: Indisponibilizada, lugares: 2) | Aprovado | 38 |
| 85 | ConsultasViagemTests | I110_47_ViagemPesquisavel_QualquerUtilizadorConsulta | Aprovado | 35 |
| 86 | ConsultasViagemTests | I110_48_DetalhesEPesquisa_TrazemOCondutorEOsPontosAdicionais | Aprovado | 53 |
| 87 | ConsultasViagemTests | I110_49_Passageiros_SoParaOCondutorEOsAceites | Aprovado | 47 |
| 88 | ConsultasViagemTests | I110_50_PedidosPendentes_SoParaOCondutor | Aprovado | 53 |
| 89 | ConsultasViagemTests | I110_51_MinhasViagens_SoAsAtivasDoCondutor | Aprovado | 53 |
| 90 | ConsultasViagemTests | I110_52_Pesquisa_NaoDistingueMaiusculasNemAcentos(origem: "Famalicao", destino: null) | Aprovado | 46 |
| 91 | ConsultasViagemTests | I110_52_Pesquisa_NaoDistingueMaiusculasNemAcentos(origem: "FAMALICÃO", destino: null) | Aprovado | 50 |
| 92 | ConsultasViagemTests | I110_52_Pesquisa_NaoDistingueMaiusculasNemAcentos(origem: "famalicão", destino: null) | Aprovado | 60 |
| 93 | ConsultasViagemTests | I110_52_Pesquisa_NaoDistingueMaiusculasNemAcentos(origem: null, destino: "acores") | Aprovado | 51 |
| 94 | ConsultasViagemTests | I110_52_Pesquisa_NaoDistingueMaiusculasNemAcentos(origem: null, destino: "AÇORES") | Aprovado | 39 |
| 95 | ConsultasViagemTests | I110_53_AlterarOrigem_AtualizaOTextoDePesquisa | Aprovado | 39 |
| 96 | ConsultasViagemTests | I110_58_ListaDeConversas_UmaEntradaPorInterlocutor | Aprovado | 66 |
| 97 | ContratoRespostasHttpTests | I110_63_Viagens_MantemOsCamposDasRespostas | Aprovado | 545 |
| 98 | ContratoRespostasHttpTests | I110_64_Comunicacao_MantemOsCamposDasRespostas | Aprovado | 578 |
| 99 | ContratoRespostasHttpTests | I110_65_Autenticacao_MantemOsCamposDasRespostas | Aprovado | 551 |
| 100 | ContratoRespostasHttpTests | I110_66_Erros_TemSoOCampoErro | Aprovado | 406 |
| 101 | CriacaoViagemIntegrationTests | IT008_CriarViagemValida_Devolve201EPersiste | Aprovado | 1907 |
| 102 | DispositivosAtivosIntegrationTests | IT039_TokenDesativado_NaoApareceNosDispositivosAtivos | Aprovado | 59 |
| 103 | DispositivosAtivosIntegrationTests | IT039_TokenDesativado_NaoOriginaEnvioPush | Aprovado | 35 |
| 104 | DispositivosHttpIntegrationTests | I047_OutroUtilizador_NaoPodeDesativarToken | Aprovado | 219 |
| 105 | DispositivosHttpIntegrationTests | I047_RegistarMesmoToken_NaoCriaDuplicados | Aprovado | 375 |
| 106 | DispositivosHttpIntegrationTests | I047_RegistarToken_Devolve204EPersisteAtivo | Aprovado | 189 |
| 107 | DispositivosHttpIntegrationTests | I047_RemoverToken_Devolve204EDesativa | Aprovado | 180 |
| 108 | DispositivosHttpIntegrationTests | I047_SemAutenticacao_Devolve401 | Aprovado | 1974 |
| 109 | DispositivosHttpIntegrationTests | I047_TokenInvalido_Devolve400 | Aprovado | 170 |
| 110 | EndpointsAdditionalTests | AtualizarPerfilInexistente_Devolve404 | Aprovado | 34 |
| 111 | EndpointsAdditionalTests | AtualizarPerfilNomeMuitoLongo_Devolve400 | Aprovado | 70 |
| 112 | EndpointsAdditionalTests | CalcularOrigemVazia_Devolve400 | Aprovado | 2 |
| 113 | EndpointsAdditionalTests | ConcluirViagemFutura_Devolve409 | Aprovado | 38 |
| 114 | EndpointsAdditionalTests | ConcluirViagemPassada_AlteraEstado | Aprovado | 41 |
| 115 | EndpointsAdditionalTests | ConfirmarTokenVazio_Devolve400 | Aprovado | 25 |
| 116 | EndpointsAdditionalTests | ConsultarViagemExistente_Devolve200 | Aprovado | 36 |
| 117 | EndpointsAdditionalTests | CriarViagemContaNaoConfirmada_Devolve403 | Aprovado | 139 |
| 118 | EndpointsAdditionalTests | CriarViagemDadosInvalidos_Devolve400(origem: "", destino: "Braga", lugares: 2) | Aprovado | 41 |
| 119 | EndpointsAdditionalTests | CriarViagemDadosInvalidos_Devolve400(origem: "Porto", destino: "Braga", lugares: 0) | Aprovado | 40 |
| 120 | EndpointsAdditionalTests | CriarViagemDadosInvalidos_Devolve400(origem: "Porto", destino: "Braga", lugares: 9) | Aprovado | 31 |
| 121 | EndpointsAdditionalTests | CriarViagemDadosInvalidos_Devolve400(origem: "Porto", destino: "Porto", lugares: 2) | Aprovado | 26 |
| 122 | EndpointsAdditionalTests | CriarViagemSemIdentidade_Devolve401 | Aprovado | 25 |
| 123 | EndpointsAdditionalTests | CriarViagemSemMaps_Devolve503 | Aprovado | 31 |
| 124 | EndpointsAdditionalTests | EditarViagemAtiva_AtualizaLugares | Aprovado | 159 |
| 125 | EndpointsAdditionalTests | EditarViagemInativa_Devolve409 | Aprovado | 18 |
| 126 | EndpointsAdditionalTests | EditarViagemInexistente_Devolve404 | Aprovado | 63 |
| 127 | EndpointsAdditionalTests | GeocodificarSemChave_Devolve503 | Aprovado | 2 |
| 128 | EndpointsAdditionalTests | IndisponibilizarViagem_AlteraEstado | Aprovado | 54 |
| 129 | EndpointsAdditionalTests | IndisponibilizarViagemDeOutroCondutor_Devolve403 | Aprovado | 29 |
| 130 | EndpointsAdditionalTests | LoginContaNaoConfirmada_Devolve403 | Aprovado | 402 |
| 131 | EndpointsAdditionalTests | LoginSemCredenciais_Devolve401 | Aprovado | 18 |
| 132 | EndpointsAdditionalTests | MeAutenticacao_DevolveIdDoToken | Aprovado | 27 |
| 133 | EndpointsAdditionalTests | PerfilSemIdentidade_Devolve401 | Aprovado | 25 |
| 134 | EndpointsAdditionalTests | PerfilUtilizadorInexistente_Devolve404 | Aprovado | 24 |
| 135 | EndpointsAdditionalTests | PesquisaExcluiViagensSemLugaresEPassadas | Aprovado | 44 |
| 136 | EndpointsAdditionalTests | PesquisaFiltraOrigemDestino | Aprovado | 62 |
| 137 | EndpointsAdditionalTests | PesquisaPaginacaoRespeitaTamanho | Aprovado | 47 |
| 138 | EndpointsAdditionalTests | ReenviarConfirmacaoEmailDesconhecido_RespostaNeutra | Aprovado | 32 |
| 139 | EndpointsAdditionalTests | ReenviarConfirmacaoEmailVazio_Devolve400 | Aprovado | 32 |
| 140 | EstadoViagemTests | I110_05_EstadoDeEstadoViagem_AceiteNaBaseDeDados(estado: Ativa) | Aprovado | 58 |
| 141 | EstadoViagemTests | I110_05_EstadoDeEstadoViagem_AceiteNaBaseDeDados(estado: Concluida) | Aprovado | 39 |
| 142 | EstadoViagemTests | I110_05_EstadoDeEstadoViagem_AceiteNaBaseDeDados(estado: Indisponibilizada) | Aprovado | 34 |
| 143 | EstadoViagemTests | I110_06_EstadoForaDeEstadoViagem_RecusadoPeloCheck(estado: "") | Aprovado | 37 |
| 144 | EstadoViagemTests | I110_06_EstadoForaDeEstadoViagem_RecusadoPeloCheck(estado: "Cancelada") | Aprovado | 24 |
| 145 | EstadoViagemTests | I110_06_EstadoForaDeEstadoViagem_RecusadoPeloCheck(estado: "Concluída") | Aprovado | 22 |
| 146 | EstadoViagemTests | I110_07_Migration_ConverteConcluidaComAcentoAntesDoCheck | Aprovado | 33 |
| 147 | EstimativaCustoTests | AlterarParticipantes_AtualizaEstimativa | Aprovado | 0 |
| 148 | EstimativaCustoTests | DistanciaInvalida_DevolveErro(distancia: 0) | Aprovado | 0 |
| 149 | EstimativaCustoTests | DistanciaInvalida_DevolveErro(distancia: -1) | Aprovado | 0 |
| 150 | EstimativaCustoTests | ExemploOficial_30Km_DoisPassageiros | Aprovado | 0 |
| 151 | EstimativaCustoTests | MesmoInput_ProduzMesmoResultado | Aprovado | 1 |
| 152 | EstimativaCustoTests | PassageirosInvalidos_DevolveErro(passageiros: -1) | Aprovado | 0 |
| 153 | EstimativaCustoTests | PassageirosInvalidos_DevolveErro(passageiros: 9) | Aprovado | 0 |
| 154 | EstimativaCustoTests | PrecoNaoConfigurado_DevolveErro | Aprovado | 1 |
| 155 | EstimativaCustoTests | PrecoZero_DevolveErro | Aprovado | 0 |
| 156 | EstimativaCustoTests | SemPassageiros_CondutorSuportaEstimativa | Aprovado | 0 |
| 157 | EstimativaCustoTests | TresPassageiros_DividePorQuatro | Aprovado | 0 |
| 158 | FalhaTransacaoIntegrationTests | IT040_FalhaNaGravacao_NaoPersisteAlteracoesParciais | Aprovado | 66 |
| 159 | HistoricoHttpIntegrationTests | IT033_ComAutenticacao_Devolve200 | Aprovado | 356 |
| 160 | HistoricoHttpIntegrationTests | IT033_Isolamento_DevolveApenasDadosDoUtilizador | Aprovado | 250 |
| 161 | HistoricoHttpIntegrationTests | IT033_SemToken_Devolve401 | Aprovado | 129 |
| 162 | HistoricoIntegrationTests | Historico_AvisosRespeitamParticipacaoAceite | Aprovado | 51 |
| 163 | HistoricoIntegrationTests | Historico_IncluiPedidosEfetuadosERecebidos | Aprovado | 41 |
| 164 | HistoricoIntegrationTests | Historico_MostraApenasNotificacoesPessoais | Aprovado | 58 |
| 165 | HistoricoIntegrationTests | HistoricoCondutor_DevolveViagemPassada | Aprovado | 51 |
| 166 | HistoricoIntegrationTests | HistoricoCondutor_MantemViagemIndisponibilizada | Aprovado | 37 |
| 167 | HistoricoIntegrationTests | HistoricoCondutor_OrdenaPorDataDecrescente | Aprovado | 61 |
| 168 | HistoricoIntegrationTests | HistoricoNaoMostraViagensDeOutros | Aprovado | 48 |
| 169 | HistoricoIntegrationTests | HistoricoPassageiro_DevolveViagemAceite | Aprovado | 44 |
| 170 | HistoricoIntegrationTests | HistoricoPassageiro_NaoIncluiParticipacoesNaoAceites(estado: Cancelada) | Aprovado | 41 |
| 171 | HistoricoIntegrationTests | HistoricoPassageiro_NaoIncluiParticipacoesNaoAceites(estado: Pendente) | Aprovado | 40 |
| 172 | HistoricoIntegrationTests | HistoricoPassageiro_NaoIncluiParticipacoesNaoAceites(estado: Rejeitada) | Aprovado | 50 |
| 173 | HistoricoIntegrationTests | HistoricoSemViagens_DevolveListasVazias | Aprovado | 35 |
| 174 | I054IntegridadeAdicionalIntegrationTests | IT026_MensagemValida_PersisteEConservaParticipantes | Aprovado | 53 |
| 175 | I054IntegridadeAdicionalIntegrationTests | IT034_AvaliacaoValida_PersisteLigacoes | Aprovado | 42 |
| 176 | I054IntegridadeAdicionalIntegrationTests | IT036_AvaliacaoComAvaliadoInexistente_FalhaChaveEstrangeira | Aprovado | 39 |
| 177 | I054IntegridadeAdicionalIntegrationTests | IT036_MensagemComDestinatarioInexistente_FalhaChaveEstrangeira | Aprovado | 35 |
| 178 | I054IntegridadeAdicionalIntegrationTests | IT036_NotificacaoComUtilizadorInexistente_FalhaChaveEstrangeira | Aprovado | 58 |
| 179 | IndisponibilizacaoHistoricoTests | I110_08_Indisponibilizar_ViagemContinuaNoHistoricoDoPassageiroAceite | Aprovado | 29 |
| 180 | IndisponibilizacaoHistoricoTests | I110_09_ParticipacaoCanceladaPeloPassageiro_ContinuaNoHistorico | Aprovado | 44 |
| 181 | IndisponibilizacaoHistoricoTests | I110_10_Migration_PreencheAceiteEmDasParticipacoesAceites | Aprovado | 26 |
| 182 | IndisponibilizacaoHistoricoTests | I110_43_ViagemAtivaFutura_ApareceNoHistorico | Aprovado | 27 |
| 183 | IntegridadeRelacionalAdditionalTests | AvaliacaoDuplicadaParaMesmaViagemEAvaliadorERejeitada | Aprovado | 37 |
| 184 | IntegridadeRelacionalAdditionalTests | EmailDeUtilizadorTemDeSerUnico | Aprovado | 34 |
| 185 | IntegridadeRelacionalAdditionalTests | HashDeConfirmacaoTemDeSerUnico | Aprovado | 45 |
| 186 | IntegridadeRelacionalAdditionalTests | NaoPermiteEliminarCondutorComViagemAssociada | Aprovado | 29 |
| 187 | IntegridadeRelacionalAdditionalTests | ParticipacaoComPassageiroInexistenteERejeitada | Aprovado | 36 |
| 188 | IntegridadeRelacionalAdditionalTests | TokenDeDispositivoTemDeSerUnico | Aprovado | 52 |
| 189 | IntegridadeRelacionalAdditionalTests | TokenRevogadoJtiTemDeSerUnico | Aprovado | 34 |
| 190 | LigacoesExternasTests | I110_54_AdicionarLigacao_ApareceNoPerfil | Aprovado | 30 |
| 191 | LigacoesExternasTests | I110_55_LigacaoQueNaoEHttp_Devolve400(url: "") | Aprovado | 35 |
| 192 | LigacoesExternasTests | I110_55_LigacaoQueNaoEHttp_Devolve400(url: "ftp://exemplo.pt") | Aprovado | 51 |
| 193 | LigacoesExternasTests | I110_55_LigacaoQueNaoEHttp_Devolve400(url: "github.com/exemplo") | Aprovado | 41 |
| 194 | LigacoesExternasTests | I110_55_LigacaoQueNaoEHttp_Devolve400(url: "javascript:alert(1)") | Aprovado | 27 |
| 195 | LigacoesExternasTests | I110_56_MaximoDeCincoLigacoes | Aprovado | 40 |
| 196 | LigacoesExternasTests | I110_57_RemoverLigacao_SoODono | Aprovado | 56 |
| 197 | MapasAndValidationTests | CT030_GeocodificarTextoVazio_Devolve400 | Aprovado | 1 |
| 198 | MapasAndValidationTests | CT032_MapasSemChave_Devolve503 | Aprovado | 2 |
| 199 | MapasAndValidationTests | Custo_DivisaoPorCondutorEMaisUmPassageiro | Aprovado | 0 |
| 200 | MapasAndValidationTests | Custo_RejeitaPassageirosNegativos | Aprovado | 0 |
| 201 | MensagensControllerTests | I043_01_EnviarMensagemValida_UsaRemetenteDoJwtERetorna201 | Aprovado | 2 |
| 202 | MensagensControllerTests | I043_02_EnviarSemIdentificadorAutenticado_Retorna401 | Aprovado | 1 |
| 203 | MensagensControllerTests | I043_03_ConsultarSemIdentificadorAutenticado_Retorna401 | Aprovado | 1 |
| 204 | MensagensControllerTests | I043_04_ConteudoInvalido_RejeitadoPeloServico_Retorna400(conteudo: "   ") | Aprovado | 2 |
| 205 | MensagensControllerTests | I043_04_ConteudoInvalido_RejeitadoPeloServico_Retorna400(conteudo: "") | Aprovado | 0 |
| 206 | MensagensControllerTests | I043_05_DestinatarioInexistente_RejeitadoPeloServico_Retorna400 | Aprovado | 1 |
| 207 | MensagensControllerTests | I043_06_Consulta_UsaIdentidadeAutenticadaERetornaHistorico | Aprovado | 1 |
| 208 | MensagensControllerTests | I043_07_ConsultaInvalida_RejeitadaPeloServico_Retorna400 | Aprovado | 2 |
| 209 | MensagensNotificacoesIntegrationTests | EnviarMensagem_CriaNotificacaoNovaMensagemParaDestinatario | Aprovado | 60 |
| 210 | MensagensServicoRealHttpTests | I110_93_LimiteDeCaracteres(tamanho: 1, esperado: Created) | Aprovado | 403 |
| 211 | MensagensServicoRealHttpTests | I110_93_LimiteDeCaracteres(tamanho: 1000, esperado: Created) | Aprovado | 444 |
| 212 | MensagensServicoRealHttpTests | I110_93_LimiteDeCaracteres(tamanho: 1001, esperado: BadRequest) | Aprovado | 672 |
| 213 | MensagensServicoRealHttpTests | I110_94_MensagemVazia_Devolve400SemGravar(conteudo: "   ") | Aprovado | 288 |
| 214 | MensagensServicoRealHttpTests | I110_94_MensagemVazia_Devolve400SemGravar(conteudo: "") | Aprovado | 342 |
| 215 | MensagensServicoRealHttpTests | I110_95_MensagemParaSiProprio_Devolve400 | Aprovado | 285 |
| 216 | MensagensServicoRealHttpTests | I110_96_DestinatarioInexistenteOuPorConfirmar_Devolve400 | Aprovado | 318 |
| 217 | MensagensServicoRealHttpTests | I110_97_Conversa_SoMostraMensagensDosDoisIntervenientes | Aprovado | 364 |
| 218 | MensagensServicoRealHttpTests | I110_98_Conversa_EAMesmaParaOsDoisIntervenientes | Aprovado | 446 |
| 219 | MigracoesSqlServerTests | I110_133_MigrationsComDados_PreenchemAsColunasDePesquisa | Aprovado | 3731 |
| 220 | MigracoesSqlServerTests | I110_134_IndiceDasAvaliacoes_EODoPar | Aprovado | 5 |
| 221 | MigracoesSqlServerTests | I110_59_Migrations_AplicamSeAUmaBaseDeDadosVazia | Aprovado | 2022 |
| 222 | MigracoesSqlServerTests | I110_73_ModeloDasEntidades_NaoTemAlteracoesPorMigrar | Aprovado | 54 |
| 223 | NotificacoesMesmaGravacaoTests | I110_67_Aceitar_GravaDecisaoENotificacoesDeUmaVez | Aprovado | 5 |
| 224 | NotificacoesMesmaGravacaoTests | I110_68_Rejeitar_GravaDecisaoENotificacaoDeUmaVez | Aprovado | 5 |
| 225 | NotificacoesMesmaGravacaoTests | I110_69_FalhaAoGravar_NaoFicaNadaNemHaAnuncios | Aprovado | 28 |
| 226 | NotificacoesMesmaGravacaoTests | I110_70_ClienteSaiDepoisDeGravar_PushETempoRealSaemNaMesma | Aprovado | 4 |
| 227 | NotificacoesMesmaGravacaoTests | I110_71_Mensagem_GravaMensagemENotificacaoDeUmaVez | Aprovado | 4 |
| 228 | NotificacoesMesmaGravacaoTests | I110_72_MarcarComoLida_DeOutroUtilizador_NaoEncontrada | Aprovado | 2 |
| 229 | NotificacoesSoNaAplicacaoHttpTests | I110_103_NovaMensagem_NaoGeraPush | Aprovado | 500 |
| 230 | NotificacoesSoNaAplicacaoHttpTests | I110_104_AlteracoesDaViagem_NaoGeramPush | Aprovado | 677 |
| 231 | NotificacoesSoNaAplicacaoHttpTests | I110_105_AvisoDoCondutor_NaoGeraPush | Aprovado | 454 |
| 232 | ParticipacoesGestaoIntegrationTests | AceitacaoSemCapacidade_ERejeitadaENaoAlteraPedido | Aprovado | 53 |
| 233 | ParticipacoesGestaoIntegrationTests | AceitacaoValida_AlteraEstadoEReduzLugar | Aprovado | 43 |
| 234 | ParticipacoesGestaoIntegrationTests | AceitarPedido_NotificaRestantesPassageirosAceites | Aprovado | 52 |
| 235 | ParticipacoesGestaoIntegrationTests | CancelamentoAceite_NotificaRestantesPassageirosAceites | Aprovado | 63 |
| 236 | ParticipacoesGestaoIntegrationTests | CancelamentoAceiteAntesDaPartida_LibertaLugar | Aprovado | 24 |
| 237 | ParticipacoesGestaoIntegrationTests | CancelamentoAceiteDepoisDaPartida_ERejeitadoENaoAlteraDados | Aprovado | 35 |
| 238 | ParticipacoesGestaoIntegrationTests | CancelamentoPendente_EPermitidoMesmoDepoisDaPartida | Aprovado | 42 |
| 239 | ParticipacoesGestaoIntegrationTests | CapacidadeInconsistente_ERejeitadaSemCorrigirSilenciosamente | Aprovado | 27 |
| 240 | ParticipacoesGestaoIntegrationTests | ConcluirViagem_CancelaPedidosPendentes | Aprovado | 53 |
| 241 | ParticipacoesGestaoIntegrationTests | I110_23_CondutorNaoCancelaParticipacao(estado: Aceite) | Aprovado | 25 |
| 242 | ParticipacoesGestaoIntegrationTests | I110_23_CondutorNaoCancelaParticipacao(estado: Pendente) | Aprovado | 22 |
| 243 | ParticipacoesGestaoIntegrationTests | I110_24_AceitarDepoisDaPartida_EPermitidoEnquantoViagemAtiva | Aprovado | 26 |
| 244 | ParticipacoesGestaoIntegrationTests | IT038_AceitarPedido_EnfileiraPushAoPassageiro | Aprovado | 35 |
| 245 | ParticipacoesGestaoIntegrationTests | IT038_CancelamentoDePushNaoAfetaAceitacaoJaConcluida | Aprovado | 28 |
| 246 | ParticipacoesGestaoIntegrationTests | IT038_RejeitarPedido_EnfileiraPushAoPassageiro | Aprovado | 34 |
| 247 | ParticipacoesGestaoIntegrationTests | PassageiroNaoPodeAceitarNemRejeitar | Aprovado | 34 |
| 248 | ParticipacoesGestaoIntegrationTests | RejeicaoValida_AlteraEstadoSemAlterarLugares | Aprovado | 31 |
| 249 | ParticipacoesGestaoIntegrationTests | TransicoesInvalidas_DevolvemConflito | Aprovado | 38 |
| 250 | ParticipacoesGestaoIntegrationTests | UtilizadorNaoAutorizado_NaoPodeAceitarRejeitarOuCancelar | Aprovado | 39 |
| 251 | PedidosHubIntegrationTests | DecisaoPedido_DeveSerEntregueApenasAoDestinatario(nomeEvento: "PedidoAceite") | Aprovado | 34 |
| 252 | PedidosHubIntegrationTests | DecisaoPedido_DeveSerEntregueApenasAoDestinatario(nomeEvento: "PedidoRejeitado") | Aprovado | 51 |
| 253 | PedidosHubIntegrationTests | LigacaoSemAutenticacao_DeveSerRejeitada | Aprovado | 25 |
| 254 | PedidosHubIntegrationTests | PedidoRecebido_DeveSerEntregueApenasAoDestinatario | Aprovado | 281 |
| 255 | PedidosParticipacaoIntegrationTests | CondutorNaoPodePedirNaPropriaViagem | Aprovado | 41 |
| 256 | PedidosParticipacaoIntegrationTests | ContaNaoConfirmada_Devolve403 | Aprovado | 33 |
| 257 | PedidosParticipacaoIntegrationTests | I110_25_PedidoGuardaCoordenadasDoPontoIndicado | Aprovado | 35 |
| 258 | PedidosParticipacaoIntegrationTests | I110_26_PedidoSemPontos_Devolve400 | Aprovado | 26 |
| 259 | PedidosParticipacaoIntegrationTests | I110_27_PontosNoMesmoLocal_Devolve400(recolha: " braga ", destino: null) | Aprovado | 21 |
| 260 | PedidosParticipacaoIntegrationTests | I110_27_PontosNoMesmoLocal_Devolve400(recolha: "Maia", destino: "maia") | Aprovado | 32 |
| 261 | PedidosParticipacaoIntegrationTests | I110_27_PontosNoMesmoLocal_Devolve400(recolha: "Maia", destino: "Mesmo sítio da Maia") | Aprovado | 47 |
| 262 | PedidosParticipacaoIntegrationTests | I110_27_PontosNoMesmoLocal_Devolve400(recolha: null, destino: "PORTO") | Aprovado | 20 |
| 263 | PedidosParticipacaoIntegrationTests | PedidoPendenteDuplicado_Devolve409 | Aprovado | 39 |
| 264 | PedidosParticipacaoIntegrationTests | PedidosAtivosDuplicados_BaseDadosRejeitaSegundo | Aprovado | 49 |
| 265 | PedidosParticipacaoIntegrationTests | PedidoValido_CriaParticipacaoPendente | Aprovado | 56 |
| 266 | PedidosParticipacaoIntegrationTests | PontoInexistente_Devolve422 | Aprovado | 34 |
| 267 | PedidosParticipacaoIntegrationTests | PontoRecolhaInvalido_Devolve400 | Aprovado | 35 |
| 268 | PedidosParticipacaoIntegrationTests | PontosOpcionais_AceitaSoRecolhaOuSoDestino | Aprovado | 37 |
| 269 | PedidosParticipacaoIntegrationTests | SemAutenticacao_Devolve401 | Aprovado | 44 |
| 270 | PedidosParticipacaoIntegrationTests | SemChaveMapas_ComPonto_Devolve503 | Aprovado | 35 |
| 271 | PedidosParticipacaoIntegrationTests | SemLugaresDisponiveis_Devolve409 | Aprovado | 40 |
| 272 | PedidosParticipacaoIntegrationTests | ServicoMapasIndisponivel_Devolve502 | Aprovado | 38 |
| 273 | PedidosParticipacaoIntegrationTests | ViagemIndisponivel_Devolve409 | Aprovado | 25 |
| 274 | PedidosParticipacaoIntegrationTests | ViagemInexistente_Devolve404 | Aprovado | 39 |
| 275 | PedidosParticipacaoIntegrationTests | ViagemPassada_Devolve409 | Aprovado | 28 |
| 276 | PerfilPublicoTests | I110_20_PerfilPublico_DevolveCamposDeP10ComMedia | Aprovado | 76 |
| 277 | PerfilPublicoTests | I110_21_PerfilPublicoSemAvaliacoes_MediaNula | Aprovado | 22 |
| 278 | PerfilPublicoTests | I110_22_PerfilPublicoInexistente_Devolve404 | Aprovado | 23 |
| 279 | PesquisaHoraPortugalHttpTests | I110_118_HoraMinima_NoVerao_UsaORelogioDePortugal | Aprovado | 568 |
| 280 | PesquisaHoraPortugalHttpTests | I110_119_HoraMinima_NoInverno_CoincideComUtc | Aprovado | 427 |
| 281 | PesquisaHoraPortugalHttpTests | I110_120_Dia_PertoDaMeiaNoite_UsaODiaDePortugal | Aprovado | 428 |
| 282 | PesquisaHoraPortugalHttpTests | I110_121_UltimoDiaPossivel_NaoRebenta | Aprovado | 393 |
| 283 | PesquisaHoraPortugalHttpTests | I110_122_MinutoDoDia_ContaODesvioDaEstacao(ano: 2030, mes: 1, dia: 15, desvio: 0) | Aprovado | 353 |
| 284 | PesquisaHoraPortugalHttpTests | I110_122_MinutoDoDia_ContaODesvioDaEstacao(ano: 2030, mes: 7, dia: 15, desvio: 60) | Aprovado | 355 |
| 285 | PesquisaHoraPortugalHttpTests | I110_123_DataLidaDaBaseDeDados_SaiComZ | Aprovado | 397 |
| 286 | PesquisaHoraPortugalHttpTests | I110_145_QualquerDia_NaoRebenta(dia: "0001-01-01") | Aprovado | 383 |
| 287 | PesquisaHoraPortugalHttpTests | I110_145_QualquerDia_NaoRebenta(dia: "1977-03-27") | Aprovado | 381 |
| 288 | PipelineHttpTests | I110_88_ExcecaoInesperada_Devolve500SemDetalhesInternos | Aprovado | 299 |
| 289 | PipelineHttpTests | I110_89_RegraDeNegocio_Devolve409ComAMensagem | Aprovado | 222 |
| 290 | PipelineHttpTests | I110_90_CorpoQueNaoEJson_Devolve400 | Aprovado | 116 |
| 291 | PipelineHttpTests | I110_91_EstadoDoServico_RespondeSemToken(caminho: "/") | Aprovado | 127 |
| 292 | PipelineHttpTests | I110_91_EstadoDoServico_RespondeSemToken(caminho: "/health") | Aprovado | 143 |
| 293 | PipelineHttpTests | I110_92_RotaInexistente_Devolve404ComSessaoE401Sem | Aprovado | 214 |
| 294 | ProntoParaFrontendHttpTests | I110_135_OrigemDoFrontend_PassaAVerificacaoDoBrowser | Aprovado | 81 |
| 295 | ProntoParaFrontendHttpTests | I110_136_OutraOrigem_NaoEAutorizada | Aprovado | 174 |
| 296 | ProntoParaFrontendHttpTests | I110_137_RespostaDeErro_TambemTrazOCabecalhoCors | Aprovado | 123 |
| 297 | ProntoParaFrontendHttpTests | I110_138_ForaDeDesenvolvimento_RespostaHttpsTrazHsts | Aprovado | 91 |
| 298 | ProntoParaFrontendHttpTests | I110_139_BaseDeDadosEmBaixo_HealthDevolve503 | Aprovado | 86 |
| 299 | ProntoParaFrontendHttpTests | I110_140_DemasiadosPedidos_Devolve429ComMensagem | Aprovado | 910 |
| 300 | ProntoParaFrontendHttpTests | I110_141_Hub_FechaQuandoOTokenExpira | Aprovado | 83 |
| 301 | ProntoParaFrontendHttpTests | I110_142_Notificacao_TemTituloParaOUtilizador | Aprovado | 260 |
| 302 | ProntoParaFrontendHttpTests | I110_143_DuasNotificacoes_PedemUmSoTokenOAuth | Aprovado | 804 |
| 303 | ProntoParaFrontendHttpTests | I110_146_TokenFcmRecusado_PedeOutroENaoPerdeANotificacao | Aprovado | 536 |
| 304 | ProtecaoDeDadosHttpTests | I110_109_PayloadsDeInjecao_SaoTratadosComoTexto(payload: "' OR '1'='1") | Aprovado | 746 |
| 305 | ProtecaoDeDadosHttpTests | I110_109_PayloadsDeInjecao_SaoTratadosComoTexto(payload: "'; DROP TABLE Utilizadores; --") | Aprovado | 880 |
| 306 | ProtecaoDeDadosHttpTests | I110_109_PayloadsDeInjecao_SaoTratadosComoTexto(payload: "\" OR \"\"=\"") | Aprovado | 1001 |
| 307 | ProtecaoDeDadosHttpTests | I110_109_PayloadsDeInjecao_SaoTratadosComoTexto(payload: "x' UNION SELECT PasswordHash FROM Utilizadores --") | Aprovado | 2124 |
| 308 | ProtecaoDeDadosHttpTests | I110_110_CaracteresEspeciaisDaPesquisa_SaoTexto(filtro: "%") | Aprovado | 523 |
| 309 | ProtecaoDeDadosHttpTests | I110_110_CaracteresEspeciaisDaPesquisa_SaoTexto(filtro: "_") | Aprovado | 2466 |
| 310 | ProtecaoDeDadosHttpTests | I110_111_Logs_NaoContemPalavrasPasseTokensNemMensagens | Aprovado | 822 |
| 311 | RegistoResilienteTests | I110_124_DoisRegistosSimultaneos_OSegundoRecebe409 | Aprovado | 172 |
| 312 | RegistoResilienteTests | I110_125_DoisRegistosDoMesmoDispositivo_NaoFalha | Aprovado | 89 |
| 313 | RegistoResilienteTests | I110_126_FalhaNoEnvioDoEmail_Devolve503EAContaFica(caso: "configuracao") | Aprovado | 146 |
| 314 | RegistoResilienteTests | I110_126_FalhaNoEnvioDoEmail_Devolve503EAContaFica(caso: "recusa") | Aprovado | 161 |
| 315 | RegistoResilienteTests | I110_126_FalhaNoEnvioDoEmail_Devolve503EAContaFica(caso: "tempo") | Aprovado | 190 |
| 316 | RegistoResilienteTests | I110_127_LoginDeEmailInexistente_TambemCalculaUmHash | Aprovado | 32 |
| 317 | RegistoResilienteTests | I110_144_ReenvioComEmailEmBaixo_MantemALigacaoAnterior | Aprovado | 167 |
| 318 | RegistoResilienteTests | I110_147_DoisLogoutsSimultaneos_ComOMesmoToken_NaoFalham | Aprovado | 35 |
| 319 | RegistoResilienteTests | I110_148_Logout_SeABdRecusarSemHaverRevogacao_NaoFingeQueTerminou | Aprovado | 49 |
| 320 | RegistoResilienteTests | I110_149_Limpeza_GuardaOsTokensAindaAceitesPelaMargemDoRelogio | Aprovado | 59 |
| 321 | RegistoServicosTests | DominiosAcademicos_VemDaSeccaoEmailAcademico | Aprovado | 51 |
| 322 | RegistoServicosTests | PrecoPorKm_VemDaSeccaoCustos | Aprovado | 68 |
| 323 | RegistoServicosTests | TodosOsControllers_SaoResolvidos | Aprovado | 84 |
| 324 | RegrasSqlServerTests | I110_112_ConflitoDeVersao_NoServicoReal_NaoGravaNada | Aprovado | 3467 |
| 325 | RegrasSqlServerTests | I110_113_PedidoRepetido_ORepositorioReconheceOIndiceFiltrado | Aprovado | 2121 |
| 326 | RegrasSqlServerTests | I110_128_DecisaoSimultaneaNoMesmoPedido_ASegundaRecebeConflito | Aprovado | 2021 |
| 327 | RegrasViagemHttpTests | I110_100_LugaresAbaixoDosAceites_Devolve409 | Aprovado | 649 |
| 328 | RegrasViagemHttpTests | I110_101_PartidaQueNaoEstaEmUtc_Devolve400(resto: "-07-01T09:00:00") | Aprovado | 373 |
| 329 | RegrasViagemHttpTests | I110_101_PartidaQueNaoEstaEmUtc_Devolve400(resto: "-07-01T09:00:00+01:00") | Aprovado | 254 |
| 330 | RegrasViagemHttpTests | I110_102_PartidaEmUtc_FicaGravadaEmUtc(resto: "-07-01T08:00:00Z", mes: 7, hora: 8) | Aprovado | 299 |
| 331 | RegrasViagemHttpTests | I110_102_PartidaEmUtc_FicaGravadaEmUtc(resto: "-12-01T09:00:00Z", mes: 12, hora: 9) | Aprovado | 422 |
| 332 | RegrasViagemHttpTests | I110_99_PartidaNoPassado_Devolve400SemCriarViagem | Aprovado | 300 |
| 333 | RepositorioParticipacoesCoberturaTests | ExisteAtiva_ConsideraApenasPendenteOuAceite(estado: Aceite, esperado: True) | Aprovado | 26 |
| 334 | RepositorioParticipacoesCoberturaTests | ExisteAtiva_ConsideraApenasPendenteOuAceite(estado: Cancelada, esperado: False) | Aprovado | 43 |
| 335 | RepositorioParticipacoesCoberturaTests | ExisteAtiva_ConsideraApenasPendenteOuAceite(estado: Pendente, esperado: True) | Aprovado | 53 |
| 336 | RepositorioParticipacoesCoberturaTests | ExisteAtiva_ConsideraApenasPendenteOuAceite(estado: Rejeitada, esperado: False) | Aprovado | 28 |
| 337 | RepositorioParticipacoesCoberturaTests | ListarPorPassageiro_DevolveSoAsSuasParticipacoes | Aprovado | 38 |
| 338 | RepositorioParticipacoesCoberturaTests | ListarPorViagem_FiltraEstadoEIgnoraOutrasViagens | Aprovado | 41 |
| 339 | RepositorioParticipacoesCoberturaTests | ObterPorId_DevolveParticipacaoRastreadaEOuNull | Aprovado | 133 |
| 340 | RestricoesBaseDeDadosTests | I110_129_ValorForaDoModelo_ABaseDeDadosRecusa(sql: "UPDATE Participacoes SET Estado = 'Outro'") | Aprovado | 358 |
| 341 | RestricoesBaseDeDadosTests | I110_129_ValorForaDoModelo_ABaseDeDadosRecusa(sql: "UPDATE Participacoes SET RecolhaLatitude = 1, Reco"···) | Aprovado | 371 |
| 342 | RestricoesBaseDeDadosTests | I110_129_ValorForaDoModelo_ABaseDeDadosRecusa(sql: "UPDATE Viagens SET DestinoLatitude = OrigemLatitud"···) | Aprovado | 368 |
| 343 | RestricoesBaseDeDadosTests | I110_129_ValorForaDoModelo_ABaseDeDadosRecusa(sql: "UPDATE Viagens SET LugaresDisponiveis = -1") | Aprovado | 391 |
| 344 | RestricoesBaseDeDadosTests | I110_129_ValorForaDoModelo_ABaseDeDadosRecusa(sql: "UPDATE Viagens SET LugaresDisponiveis = 4") | Aprovado | 349 |
| 345 | RestricoesBaseDeDadosTests | I110_129_ValorForaDoModelo_ABaseDeDadosRecusa(sql: "UPDATE Viagens SET LugaresOferecidos = 0, LugaresD"···) | Aprovado | 401 |
| 346 | RestricoesBaseDeDadosTests | I110_129_ValorForaDoModelo_ABaseDeDadosRecusa(sql: "UPDATE Viagens SET LugaresOferecidos = 9, LugaresD"···) | Aprovado | 355 |
| 347 | RestricoesBaseDeDadosTests | I110_130_NotificacaoComTipoDesconhecido_ABaseDeDadosRecusa | Aprovado | 354 |
| 348 | RestricoesBaseDeDadosTests | I110_131_LocalAcimaDoTamanhoDaColuna_Devolve400 | Aprovado | 352 |
| 349 | RestricoesBaseDeDadosTests | I110_132_TokenDeDispositivoAcimaDoTamanhoDaColuna_Devolve400 | Aprovado | 390 |
| 350 | RevisaoFinalHttpTests | I110_150_ViagemIndisponibilizadaDuranteAGeocodificacao_RecusaOPedido | Aprovado | 450 |
| 351 | RevisaoFinalHttpTests | I110_151_PaginaDasNotificacoesDemasiadoGrande_Devolve400 | Aprovado | 309 |
| 352 | RevisaoFinalHttpTests | I110_152_Avaliacoes_SoARecusaDoParContaComoConflito | Aprovado | 465 |
| 353 | RevisaoFinalHttpTests | I110_154_PedidoLigaANotificacaoAViagem_MostraOsPontos_EOPerfilTemClassificacao | Aprovado | 510 |
| 354 | RotaGuardadaTests | I110_31_ConsultaLeARotaGuardada_SemChamarOServicoDeMapas | Aprovado | 31 |
| 355 | RotaGuardadaTests | I110_32_Aceitar_RecalculaARotaEMantemADuracaoDeP04 | Aprovado | 39 |
| 356 | RotaGuardadaTests | I110_33_AceitarSemServicoDeMapas_ERecusadoENadaMuda | Aprovado | 45 |
| 357 | RotaGuardadaTests | I110_34_Cancelar_RecalculaARotaSemOsPontosDoPassageiro | Aprovado | 37 |
| 358 | RotaGuardadaTests | I110_35_CancelarSemServicoDeMapas_NuncaERecusadoEARotaFicaPorRecalcular | Aprovado | 62 |
| 359 | RotaGuardadaTests | I110_36_ARotaDepoisDeAceitarCoincideComASimulacao | Aprovado | 52 |
| 360 | RotaGuardadaTests | I110_37_Indisponibilizar_RetiraOsPontosDaRota | Aprovado | 35 |
| 361 | RotaGuardadaTests | I110_38_AlterarOrigem_RecalculaARotaENotificaOPassageiro | Aprovado | 41 |
| 362 | RotaGuardadaTests | I110_39_AlterarSemServicoDeMapas_ERecusadoENadaMuda | Aprovado | 42 |
| 363 | RotaGuardadaTests | I110_40_AlterarParaOMesmoLocal_Devolve400(origem: " braga ", destino: null) | Aprovado | 36 |
| 364 | RotaGuardadaTests | I110_40_AlterarParaOMesmoLocal_Devolve400(origem: "Lisboa", destino: "Capital") | Aprovado | 36 |
| 365 | RotaGuardadaTests | I110_40_AlterarParaOMesmoLocal_Devolve400(origem: "Lisboa", destino: "LISBOA") | Aprovado | 37 |
| 366 | RotaGuardadaTests | I110_42_CancelarParticipacaoAceite_NotificaOCondutor | Aprovado | 39 |
| 367 | RotasParticipacoesIntegrationTests | OutroUtilizador_VeARotaSemOsPontosDasParticipacoes | Aprovado | 45 |
| 368 | RotasParticipacoesIntegrationTests | ParticipacaoAceite_AdicionaPontosNaRota | Aprovado | 37 |
| 369 | RotasParticipacoesIntegrationTests | ParticipacaoAceiteSemPontos_UsaRotaDireta | Aprovado | 37 |
| 370 | RotasParticipacoesIntegrationTests | ParticipacaoPendente_NaoEntraNaRota | Aprovado | 41 |
| 371 | RotasParticipacoesIntegrationTests | RotaDireta_DevolveDistanciaEDuracaoCorretas | Aprovado | 46 |
| 372 | RotasParticipacoesIntegrationTests | SemAutenticacao_Devolve401 | Aprovado | 34 |
| 373 | RotasParticipacoesIntegrationTests | SemParticipacoesAceites_CalculaRotaDireta | Aprovado | 43 |
| 374 | RotasParticipacoesIntegrationTests | ServicoMapasIndisponivel_Devolve502 | Aprovado | 34 |
| 375 | RotasParticipacoesIntegrationTests | VariosPassageirosAceites_OrdenaPontosESomaSegmentos | Aprovado | 41 |
| 376 | RotasParticipacoesIntegrationTests | ViagemInexistente_Devolve404 | Aprovado | 33 |
| 377 | RotasParticipacoesIntegrationTests | ViagemNaoConsultavel_Devolve404 | Aprovado | 46 |
| 378 | RotasProtegidasHttpTests | I110_85_RotasSemToken_SaoAsDoRequisito | Aprovado | 123 |
| 379 | RotasProtegidasHttpTests | I110_86_SemToken_TodasAsRotasProtegidasDevolvem401 | Aprovado | 156 |
| 380 | RotasProtegidasHttpTests | I110_87_ComTokenInvalido_TodasAsRotasProtegidasDevolvem401 | Aprovado | 170 |
| 381 | ServicoMapasEmFalhaHttpTests | I110_106_FuncionalidadesSemMapas_ContinuamDisponiveis(semResposta: False) | Aprovado | 794 |
| 382 | ServicoMapasEmFalhaHttpTests | I110_106_FuncionalidadesSemMapas_ContinuamDisponiveis(semResposta: True) | Aprovado | 881 |
| 383 | ServicoMapasEmFalhaHttpTests | I110_107_CriarViagemSemMapas_InformaEDeixaTudoComoEstava(semResposta: False, esperado: BadGateway) | Aprovado | 402 |
| 384 | ServicoMapasEmFalhaHttpTests | I110_107_CriarViagemSemMapas_InformaEDeixaTudoComoEstava(semResposta: True, esperado: GatewayTimeout) | Aprovado | 428 |
| 385 | ServicoMapasEmFalhaHttpTests | I110_108_RotaGuardada_ContinuaDisponivelSemMapas(semResposta: False) | Aprovado | 572 |
| 386 | ServicoMapasEmFalhaHttpTests | I110_108_RotaGuardada_ContinuaDisponivelSemMapas(semResposta: True) | Aprovado | 552 |
| 387 | SimulacoesIntegrationTests | I040_01_SemIdentificacao_Devolve401 | Aprovado | 24 |
| 388 | SimulacoesIntegrationTests | I040_02_ApenasCondutorPodeSimular | Aprovado | 20 |
| 389 | SimulacoesIntegrationTests | I040_03_PedidoInexistente_Devolve404 | Aprovado | 20 |
| 390 | SimulacoesIntegrationTests | I040_04_ViagemInexistente_Devolve404 | Aprovado | 20 |
| 391 | SimulacoesIntegrationTests | I040_05_PedidoDeOutraViagem_Devolve404 | Aprovado | 20 |
| 392 | SimulacoesIntegrationTests | I040_06_PedidoNaoPendente_Devolve409(estado: Aceite) | Aprovado | 19 |
| 393 | SimulacoesIntegrationTests | I040_06_PedidoNaoPendente_Devolve409(estado: Cancelada) | Aprovado | 27 |
| 394 | SimulacoesIntegrationTests | I040_06_PedidoNaoPendente_Devolve409(estado: Rejeitada) | Aprovado | 20 |
| 395 | SimulacoesIntegrationTests | I040_07_ViagemInativa_Devolve409 | Aprovado | 29 |
| 396 | SimulacoesIntegrationTests | I040_08_SemPontos_Devolve400 | Aprovado | 20 |
| 397 | SimulacoesIntegrationTests | I040_09_MapasNaoConfigurados_Devolve503 | Aprovado | 27 |
| 398 | SimulacoesIntegrationTests | I040_10_FalhaHttpMapas_Devolve502 | Aprovado | 23 |
| 399 | SimulacoesIntegrationTests | I040_11_SimulacaoValida_NaoAlteraPersistencia | Aprovado | 30 |
| 400 | SimulacoesIntegrationTests | I040_12_P20_PontoOpcional(soRecolha: False) | Aprovado | 29 |
| 401 | SimulacoesIntegrationTests | I040_12_P20_PontoOpcional(soRecolha: True) | Aprovado | 20 |
| 402 | SimulacoesIntegrationTests | I040_13_PassageirosAceitesEntramNaRotaOriginal | Aprovado | 22 |
| 403 | SimulacoesIntegrationTests | I110_30_ViagemAtivaJaPartida_PodeSerSimulada | Aprovado | 26 |
| 404 | TerminarSessaoHttpTests | I110_17_TerminarSessao_TokenDeixaDeSerAceite | Aprovado | 473 |
| 405 | TerminarSessaoHttpTests | I110_18_TerminarSessao_NaoAfetaOutraSessaoDoMesmoUtilizador | Aprovado | 554 |
| 406 | TerminarSessaoHttpTests | I110_19_TerminarSessaoSemToken_Devolve401 | Aprovado | 267 |
| 407 | TerminarSessaoHttpTests | I110_41_TerminarSessao_DesativaSoODispositivoIndicado | Aprovado | 361 |
| 408 | TerminarSessaoHttpTests | I110_44_LimiteDeAutenticacao_Devolve429AoDecimoPrimeiroPedido | Aprovado | 1488 |
| 409 | TerminarSessaoHttpTests | I110_45_LimiteGlobal_Devolve429AoCentesimoPrimeiroPedidoDoUtilizador | Aprovado | 521 |
| 410 | ViagensEstimativaIntegrationTests | DistanciaEmFalta_NaoInventaEstimativa | Aprovado | 41 |
| 411 | ViagensEstimativaIntegrationTests | DoisPassageirosAceites_UtilizaRotaCompletaEDividePorTres | Aprovado | 43 |
| 412 | ViagensEstimativaIntegrationTests | SemPassageiros_UsaDistanciaGuardadaECalculaTresEuros | Aprovado | 53 |
| 413 | ViagensNotificacoesIntegrationTests | EditarViagem_NotificaPassageirosAceitesMasNaoPendentes | Aprovado | 50 |
| 414 | ViagensNotificacoesIntegrationTests | IndisponibilizarViagem_CancelaENotificaPassageirosAceitesEPendentes | Aprovado | 60 |

## 5. Correspondência com a especificação I053

A especificação I053 define os casos IT-001 a IT-040. A correspondência caso a caso com os métodos acima (por nome do método e leitura das asserções) dá **31 casos cobertos, 9 parciais e 0 sem teste**. Os parciais são, sobretudo, variantes não testadas de um cenário testado: token inválido além de ausente (IT-006), partida no passado (IT-009), timeout do serviço de mapas (IT-030), notificação na criação do pedido (IT-024). A ligação de cada requisito aos testes está na RTM v02 (`04-artefactos-tecnicos/04.03-requisitos-e-rtm`).

## 6. Falhas e defeitos

- **Nenhuma execução falhou.**
- **Nenhuma execução saltada.** A pipeline fornece SQL Server real, por isso os testes de concorrência e de migrations foram executados. Numa máquina sem `CAMPUSCARPOOL_TESTES_SQLSERVER`, esses 6 testes ficam saltados, sem que isso seja uma falha.
- Defeitos encontrados pelos testes ou pela revisão: `03.01.07-defeitos/m3-registo-defeitos-backend-v01.xlsx`.

## 7. Limitações

- Esta execução usa SQLite em memória, criada a partir do modelo EF (`EnsureCreated`), mais os testes específicos em SQL Server real. A repetição completa em SQL Server, com o esquema das migrations, faz parte da mesma execução da pipeline.
- Os serviços externos (email Brevo, push FCM, OpenRouteService) são substituídos por *fakes*.
- Parte dos testes HTTP usa um esquema de autenticação de teste; `AplicacaoReal` exercita o pipeline completo, com JwtBearer, limites de pedidos e hub.

## 8. Conclusão

Os 414 testes de integração da versão entregue foram aprovados, sem falhas e sem saltos. O código testado é o de `04.12-backend/m3-v01`.
